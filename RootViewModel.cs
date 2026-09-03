using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Threading;
using VoiceTrigger.Audio;
using VoiceTrigger.Logging;
using VoiceTrigger.VTS;
using VoiceTrigger.VTS.Events;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger;

public sealed partial class Model : ObservableObject
{
    [ObservableProperty] public partial string? ModelID { get; set; }
    [ObservableProperty] public partial string? ModelName { get; set; }
}

// TODO: Serialize states. Move Advanced settings in a separate .cfg file.
// TODO: Go through the logic and find out - maybe we can make the logic funnier to use? More engaging, etc.
// TODO: Add support for Warudo as well.
public sealed partial class RootViewModel : ObservableObject
{
    const long MinimumJumpSpacingMs = 300;

    // Audio input:
    [ObservableProperty] public partial double AudioVolume { get; set; }
    [ObservableProperty] public partial bool AudioCaptureEnabled { get; set; }
    [ObservableProperty] public partial SamplingRate[] SamplingRates { get; set; } = [];
    [ObservableProperty] public partial SamplingRate? SelectedSamplingRate { get; set; }

    // Hotkeys & Avatar state.
    [ObservableProperty] public partial ModelHotkey[] Hotkeys { get; set; } = [new()];
    [ObservableProperty] public partial ModelHotkey? SelectedHotkey { get; set; }
    [ObservableProperty] public partial ModelExpression[] Expressions { get; set; } = [];
    [ObservableProperty] public partial ModelExpression? SelectedExpression { get; set; }
    [ObservableProperty] public partial Model? CurrentModel { get; set; }
    [MemberNotNullWhen(true, nameof(CurrentModel))]
    [ObservableProperty] public partial bool ModelLoaded { get; set; }
    [ObservableProperty] public partial bool IsActivated { get; set; } = false;
    [ObservableProperty] public partial bool IsTriggered { get; set; } = false;
    [ObservableProperty] public partial AvatarFlags AvatarFlags { get; set; }
    [ObservableProperty] public partial Brush VoiceMeterBrush { get; set; } = Brushes.Lime;
    [ObservableProperty] public partial Brush ProgressBrush { get; set; } = Brushes.Yellow;

    // Trigger controls.
    /// <summary>
    /// Generally, for the entire feature.
    /// </summary>
    [ObservableProperty] public partial bool EnableFreezing { get; set; }
    /// <summary>
    /// Whether system inputs is frozen. Happens when you manually switch the states.
    /// </summary>
    /// <remarks>
    /// Will not unfreeze unless you manually return back to 
    /// </remarks>
    [ObservableProperty] public partial bool IsFrozen { get; set; }
    /// <summary>
    /// For how long to freeze after manual state activation. In seconds.
    /// </summary>
    [ObservableProperty] public partial double FreezeDuration { get; set; }
    /// <summary>
    /// Whether to instantly unfreeze when manually returned to a normal state.
    /// </summary>
    [ObservableProperty] public partial bool InstantUnfreezeOnManualNormal { get; set; }
    /// <summary>
    /// Enables/Disables system unfreezing while Kobi.
    /// </summary>
    //[ObservableProperty] public partial bool AllowUnfreezeWhileNormal { get; set; } = true;
    //[ObservableProperty] public partial double NormalUnfreezeDelay { get; set; } = 15;
    /// <summary>
    /// Enables/Disables system unfreezing while IBOK.
    /// </summary>
    //[ObservableProperty] public partial bool AllowUnfreezeWhileTriggered { get; set; } = false;
    //[ObservableProperty] public partial double TriggeredUnfreezeDelay { get; set; } = 30;

    /// <summary>
    /// Audio-meter activation threshold [0.0 - 1.0]
    /// </summary>
    [ObservableProperty] public partial double ActivationThreshold { get; set; } = 0.15;
    /// <summary>
    /// [0.0 - 1.0] Describes how fast progress bar will move, depending on how loud you are.
    /// </summary>
    /// <remarks>
    /// First, from current audio meter, it calculates remaining delta from <see cref="ActivationThreshold"/>.
    /// Then, it calculates "relative position" (RP), using <see cref="ActivationThreshold"/> as origin.
    /// Then, power:
    /// With RP of 10% (0.1) - it will move 
    /// </remarks>
    [ObservableProperty] public partial double ActivationPower { get; set; } = 2;
    /// <summary>
    /// In seconds.
    /// </summary>
    [ObservableProperty] public partial double NormalActivationDuration { get; set; } = 6;
    /// <summary>
    /// In seconds.
    /// </summary>
    [ObservableProperty] public partial double TriggeredReleaseDuration { get; set; } = 60;
    /// <summary>
    /// Immediate trigger progress jump from crossing the <see cref="ActivationThreshold"/>.
    /// </summary>
    /// <remarks>
    /// Jump can only activate with an interval of <see cref="MinimumJumpSpacingMs"/>.
    /// </remarks>
    [ObservableProperty] public partial double NormalActivationJump { get; set; } = 0.05;
    /// <summary>
    /// Immediate triggered state progress gain from crossing the <see cref="ActivationThreshold"/>.
    /// </summary>
    [ObservableProperty] public partial double TriggeredActivationJump { get; set; } = 0.025;
    [ObservableProperty] public partial TriggeredResistance[] Resistances { get; set; } = [];
    [ObservableProperty] public partial TriggeredResistance? SelectedResistance { get; set; }
    /// <summary>
    /// Current activation/calming progress.
    /// </summary>
    /// <remarks>
    /// [0.0 - 1.0+]
    /// </remarks>
    [ObservableProperty] public partial double Progress { get; set; }

    readonly DispatcherTimer AudioCaptureTimer = new();
    long LastAudioCaptureTick;
    //long FrozenNormalTotalTicks;
    //long FrozenTriggeredTotalTicks;
    long TotalFrozenTicks;
    long LastJumpTick;

    public RootViewModel()
    {
        ActivationPower = Roaming.ActivationPower;
        NormalActivationJump = Roaming.NormalActivationJump;
        TriggeredReleaseDuration = Roaming.TriggeredReleaseDuration;
        NormalActivationDuration = Roaming.NormalActivationDuration;
        TriggeredActivationJump = Roaming.TriggeredActivationJump;

        EnableFreezing = Roaming.EnableFreezing;
        FreezeDuration = Roaming.FreezeDuration;
        InstantUnfreezeOnManualNormal = Roaming.InstantUnfreezeOnManualNormal;
        //AllowUnfreezeWhileNormal = Roaming.AllowUnfreezeWhileNormal;
        //NormalUnfreezeDelay = Roaming.NormalUnfreezeDelay;
        //AllowUnfreezeWhileTriggered = Roaming.AllowUnfreezeWhileTriggered;
        //TriggeredUnfreezeDelay = Roaming.TriggeredUnfreezeDelay;

        var hotkey = Roaming.SelectedHotkey;
        if (hotkey is not null)
        {
            Hotkeys = [hotkey];
            SelectedHotkey = hotkey;
        }
        else
        {
            Hotkeys = [];
            SelectedHotkey = null;
        }

        var expression = Roaming.SelectedExpression;
        if (expression is not null)
        {
            Expressions = [expression];
            SelectedExpression = expression;
        }
        else
        {
            Expressions = [];
            SelectedExpression = null;
        }

        int index;
        SamplingRates = [
            new(15),
            new(30),
            new(45),
            new(60),
            new(75),
            new(90),
            new(105),
            new(120),
            new(144),
            new(180),
            new(240),
        ];
        index = Local.SelectedSamplingRateIndex;
        if (index < 0 || index >= SamplingRates.Length)
            SelectedSamplingRate = SamplingRates[7];
        else
            SelectedSamplingRate = SamplingRates[index];

        Resistances = [
            new("Lowest", 1),
            new("Low", 1.4),
            new("Normal", 1.75),
            new("High", 2.25),
            new("Higher", 3),
            new("\"Mid\"", 4.2),
        ];
        index = Roaming.SelectedResistanceIndex;
        if (index < 0 || index >= Resistances.Length)
            SelectedResistance = Resistances[2];
        else
            SelectedResistance = Resistances[index];

        LastAudioCaptureTick = Environment.TickCount64;
        AudioCaptureTimer.Tick += SampleTick;
        AudioCaptureTimer.Interval = IntervalFromSampleRate(SelectedSamplingRate);
        AudioCaptureEnabled = Local.IsAudioCaptureActive;
        AudioCaptureTimer.IsEnabled = AudioCaptureEnabled;

        VTubeStudio.Instance.Events.Track<VTSHotkeyTriggeredEvent>(HandleHotkeyTriggered);
        VTubeStudio.Instance.Events.Track<VTSModelLoadedEvent>(HandleModelLoaded);
        // TODO: Sub to trigger events.
        //  Calculate triggered state based on event feedbacks (unless VTS is disabled?)
        //VTubeStudio.Instance.OnAuthenticated //...
        VTubeStudio.Instance.OnAuthenticated += () => _ = UpdateStates();
        VTubeStudio.Instance.OnUnauthenticated += UnlinkStates;
    }

    private void UnlinkStates()
    {
        IsFrozen = false;
        TotalFrozenTicks = 0;
    }

    private void HandleModelLoaded(VTSModelLoadedEvent e)
    {
        if (e.Data is null) return;

        ModelLoaded = e.Data.ModelLoaded;
        if (ModelLoaded)
        {
            _ = UpdateStates();
        }
    }

    partial void OnExpressionsChanged(ModelExpression[] oldValue, ModelExpression[] newValue)
    {
        $"Expressions changed! From ({oldValue?.Length}) to ({newValue?.Length})".Out();
    }

    partial void OnEnableFreezingChanged(bool value)
    {
        Roaming.EnableFreezing = value;
    }

    partial void OnFreezeDurationChanged(double value)
    {
        Roaming.FreezeDuration = value;
    }

    partial void OnInstantUnfreezeOnManualNormalChanged(bool value)
    {
        Roaming.InstantUnfreezeOnManualNormal = value;
        if (value && !IsTriggered)
            IsFrozen = false;
    }

    //partial void OnAllowUnfreezeWhileNormalChanged(bool value)
    //{
    //    Roaming.AllowUnfreezeWhileNormal = value;
    //}

    //partial void OnNormalUnfreezeDelayChanged(double value)
    //{
    //    Roaming.NormalUnfreezeDelay = value;
    //}

    //partial void OnAllowUnfreezeWhileTriggeredChanged(bool value)
    //{
    //    Roaming.AllowUnfreezeWhileTriggered = value;
    //}

    //partial void OnTriggeredUnfreezeDelayChanged(double value)
    //{
    //    Roaming.TriggeredUnfreezeDelay = value;
    //}

    partial void OnSelectedResistanceChanged(TriggeredResistance? value)
    {
        Roaming.SelectedResistanceIndex = Array.IndexOf(Resistances, value);
    }

    partial void OnActivationPowerChanged(double value)
    {
        Roaming.ActivationPower = value;
    }

    partial void OnNormalActivationDurationChanged(double value)
    {
        Roaming.NormalActivationDuration = value;
    }

    partial void OnTriggeredReleaseDurationChanged(double value)
    {
        Roaming.TriggeredReleaseDuration = value;
    }

    partial void OnNormalActivationJumpChanged(double value)
    {
        Roaming.NormalActivationJump = value;
    }

    partial void OnTriggeredActivationJumpChanged(double value)
    {
        Roaming.TriggeredActivationJump = value;
    }

    /// <summary>
    /// Updates all hotkeys and expressions for currently selected model.
    /// </summary>
    /// <remarks>
    /// Primarily for debugging.
    /// </remarks>
    [RelayCommand]
    public async Task UpdateStates()
    {
        if (!VTubeStudio.Instance.Authenticated)
        {
            $"Cannot update! Not authenticated!".Out(ConsoleColor.Yellow);
            return;
        }

        $"Updating hotkeys and expressions...".Out();
        try
        {
            {
                var model = await VTubeStudio.Instance.Request<VTSCurrentModelResponse>(VTSCurrentModelRequest.Instance);
                if (model.ResolveSuccess(out var response) && response.Data is not null)
                {
                    ModelLoaded = response.Data.ModelLoaded;
                    CurrentModel = new()
                    {
                        ModelID = response.Data.ModelID,
                        ModelName = response.Data.ModelName,
                    };
                }
                else
                {
                    $"Cannot retrieve current model! State update failed!".Out(ConsoleColor.Red);
                    ModelLoaded = false;
                    CurrentModel = null;
                }
            }

            if (ModelLoaded)
            {
                var hotkeys = await VTubeStudio.Instance.Request<VTSModelHotkeysResponse>(new VTSModelHotkeysRequest()
                {
                    Data = new()
                    {
                        ModelID = string.Empty,
                        Live2DItemFileName = string.Empty,
                    },
                });
                if (hotkeys.ResolveSuccess(out var result) && result.Data is not null)
                {
                    if (result.Data.AvailableHotkeys is null || result.Data.AvailableHotkeys.Count == 0)
                    {
                        if (SelectedHotkey is not null)
                            Hotkeys = [SelectedHotkey];
                        else
                            Hotkeys = [];
                    }
                    else
                    {
                        var list = result.Data.AvailableHotkeys.Where(static h => !string.IsNullOrWhiteSpace(h.File))
                            .Select(h => new ModelHotkey()
                            {
                                ModelName = CurrentModel.ModelName,
                                ModelID = CurrentModel.ModelID,
                                HotkeyID = h.HotkeyID,
                                HotkeyName = h.Name,
                                ExpressionFile = h.File,
                                LinkState = HotkeyLinkState.Dormant,
                            }).ToList();
                        if (SelectedHotkey is not null)
                        {
                            int index = list.FindIndex(h => h.HotkeyID == SelectedHotkey.HotkeyID);
                            if (index != -1) list[index] = SelectedHotkey;
                            else list.Add(SelectedHotkey);
                        }

                        Hotkeys = [.. list];
                    }

                    var expressions = Hotkeys.Select(h => new ModelExpression()
                    {
                        ExpressionFile = h.ExpressionFile,
                        LinkState = HotkeyLinkState.Dormant,
                    }).ToList();
                    if (SelectedExpression is not null)
                    {
                        int index = expressions.FindIndex(h => h.ExpressionFile == SelectedExpression.ExpressionFile);
                        if (index != -1) expressions[index] = SelectedExpression;
                        else expressions.Add(SelectedExpression);
                    }

                    Expressions = [.. expressions];

                    if (SelectedExpression is not null)
                    {
                        if (string.IsNullOrWhiteSpace(SelectedExpression.ExpressionFile))
                        {
                            $"Omitting expression state update - selected expression registered with an empty file name!".Out(ConsoleColor.Yellow);
                        }
                        else
                        {
                            SyncExpressionState(SelectedExpression.ExpressionFile);
                        }
                    }
                }
                else
                {
                    $"Cannot update hotkey listing! State update failed!".Out(ConsoleColor.Red);
                }
            }
            else
            {
                $"No model is loaded. Resetting hotkeys.".Out();
                if (SelectedHotkey is not null)
                    Hotkeys = [SelectedHotkey];
                else
                    Hotkeys = [];

                if (SelectedExpression is not null)
                    Expressions = [SelectedExpression];
                else
                    Expressions = [];
            }

            $"Update complete!".Out();
        }
        catch (Exception ex) { ex.Out($"State update failed!\n"); }
    }

    [RelayCommand]
    public async Task TriggerVTSHotkey()
    {
        IsTriggered = !IsTriggered;
        if (IsTriggered)
            Progress = 1;
        else
            Progress = 0;

        // TODO: Update hotkey states.
        var hotkey = Hotkeys.FirstOrDefault(static h => h.LinkState == HotkeyLinkState.Active);
        if (hotkey is null)
        {
            $"No active hotkey is found!".Out();
            // TODO: Toast this warning.
            return;
        }

        await VTubeStudio.Instance.Request(new VTSHotkeyTriggerRequest()
        {
            Data = new()
            {
                HotkeyID = hotkey.HotkeyID,
                ItemInstanceID = null,
            }
        });
    }

    private void HandleHotkeyTriggered(VTSHotkeyTriggeredEvent e)
    {
        if (e.Data is null) return;
        var hotkey = Hotkeys.FirstOrDefault(static h => h.LinkState == HotkeyLinkState.Active);
        if (hotkey is null)
        {
            $"No active hotkey! Hotkey event will be ignored.".Out();
            return;
        }

        if (hotkey.HotkeyID != e.Data.HotkeyID)
        {
            $"Received hotkey trigger doesn't match target hotkey ID. Target Hotkey: {hotkey.HotkeyName}".Out();
            return;
        }

        if (!e.Data.HotkeyTriggeredByAPI)
        {
            IsTriggered = !IsTriggered; // API hotkeys already predict future state.
            if (IsTriggered)
                Progress = 1;
            else
                Progress = 0;

            // Used manually triggered the transition.
            if (EnableFreezing)
            {
                TotalFrozenTicks = 0;
                if (IsTriggered)
                    IsFrozen = true;
                else
                    IsFrozen = !InstantUnfreezeOnManualNormal;
            }
        }

        if (string.IsNullOrWhiteSpace(hotkey.ExpressionFile))
        {
            $"Omitting expression state request - selected hotkey has an empty expression file!".Out(ConsoleColor.Yellow);
            SyncExpressionState(hotkey.ExpressionFile ?? string.Empty);
        }
    }

    private async void SyncExpressionState(string expressionFile)
    {
        if (string.IsNullOrWhiteSpace(expressionFile))
        {
            $"Provided empty expression state! Expression state sync omitted!".Out(ConsoleColor.Yellow);
            return;
        }

        var result = await VTubeStudio.Instance.Request<VTSExpressionStateResponse>(new VTSExpressionStateRequest()
        {
            Data = new()
            {
                Details = false,
                ExpressionFile = expressionFile,
            }
        });

        if (result.ResolveSuccess(out var response) && response.Data is not null)
        {
            if (response.Data.Expressions is null)
            {
                $"Expressions collection is null! Cannot update current triggered state!".Out(ConsoleColor.Yellow);
                return;
            }
            if (response.Data.Expressions.Count != 1)
            {
                $"Multiple expressions listed under one file! Triggered state might be inaccurate.".Out(ConsoleColor.Yellow);
            }

            bool currentState = response.Data.Expressions.Any(static e => e.Active);
            if (IsTriggered != currentState)
            {
                IsTriggered = currentState;
                if (IsTriggered)
                    Progress = 1;
                else
                    Progress = 0;
            }

        }
        else
        {
            $"Expression state request failed! Current triggered state might be out of sync.".Out(ConsoleColor.Yellow);
        }
    }

    private void SampleTick(object? sender, EventArgs e)
    {
        long tick = Environment.TickCount64;
        double delta = TimeSpan.FromMilliseconds(tick - LastAudioCaptureTick).TotalSeconds;

        // Handles unfreeze.
        if (IsFrozen)
        {
            long elapsed = tick - LastAudioCaptureTick;
            TotalFrozenTicks += elapsed;
            if (TotalFrozenTicks >= (long)TimeSpan.FromSeconds(FreezeDuration).TotalMilliseconds)
                IsFrozen = false;
        }

        // Input volume update.
        if (AudioCaptureEnabled && AudioCaptureService.Instance.SelectedAudioDevice is { IsActive: true })
        {
            var channels = AudioCaptureService.Instance.SelectedAudioDevice.Device.AudioMeterInformation.PeakValues;
            if (channels.Count == 0)
            {
                AudioVolume = 0;
            }
            else
            {
                float max = channels[0];
                for (int i = 1; i < channels.Count; i++)
                {
                    max = Math.Max(max, channels[i]);
                }
                AudioVolume = max;
            }
        }
        else
        {
            AudioVolume = 0;
        }

        // Mitigates division by zero exceptions.
        double power = Math.Clamp(ActivationPower, 0.001, 1000);
        double threshold = Math.Clamp(ActivationThreshold, 0.001, 1);

        //$"Audio volume: {AudioVolume}".Out();
        bool active = AudioVolume >= threshold;
        if (!IsActivated && active)
        {
            // Handling jump.
            long elapsed = tick - LastJumpTick;
            if (elapsed > MinimumJumpSpacingMs)
            {
                LastJumpTick = tick;
                Progress = Math.Clamp(Progress + (IsTriggered ? TriggeredActivationJump : NormalActivationJump), 0, 1);
            }
        }

        IsActivated = active;

        // Indicates how close AudioVolume is to the max possible volume [0.0:1.0], using threshold as origin.
        double result;
        if (active)
        {
            double relative = AudioVolume / threshold;
            result = Math.Pow(relative, 0.5) * power; // Values close to 0 are multiplied by ~2-3.
        }
        else
        {
            double relative = Math.Clamp((AudioVolume - threshold) / (1 - threshold), 0, 1);
            result = -Math.Pow(relative, 0.5) * 0.3; // Values close to 0 are multiplied by ~2-3.
        }

        if (!double.IsFinite(result))
            result = 0;

        //double relative = active
        //        ? Math.Clamp((AudioVolume - threshold) / (1 - threshold), 0, 1)
        //        : Math.Clamp(AudioVolume / threshold, 0, 1);
        //double speed = active ? power : 1;
        ////double speed = Math.Pow(relative, (1 - power) / power);

        //double multiplier = relative * speed;
        if (IsTriggered)
        {
            var resistance = SelectedResistance ?? TriggeredResistance.Default;
            double relativeActivation = Math.Clamp((1 - AudioVolume) / (1 - threshold), 0, 1);
            double direction = (-1 * relativeActivation) + (Math.Max(result, 0) * resistance.Resistance);
            Progress = Math.Clamp(Progress + (direction / TriggeredReleaseDuration * delta), 0, 1);
        }
        else
        {
            double direction = result;
            Progress = Math.Clamp(Progress + (direction / NormalActivationDuration * delta), 0, 1);
        }

        // Only visual updates while frozen.
        if (!IsFrozen)
        {
            if (IsTriggered)
            {
                if (Progress <= double.Epsilon)
                    _ = TriggerVTSHotkey();
            }
            else
            {
                if (Progress >= 1 - double.Epsilon)
                    _ = TriggerVTSHotkey();
            }
        }

        LastAudioCaptureTick = tick;
    }

    partial void OnIsTriggeredChanged(bool value)
    {
        AvatarFlags = (IsActivated ? AvatarFlags.Active : default(AvatarFlags))
            | (IsTriggered ? AvatarFlags.TriggeredNormal : default(AvatarFlags));
        $"IsTriggered changed to: {value}".Out(ConsoleColor.Cyan);
        UpdateBrushes();
    }

    partial void OnIsFrozenChanged(bool value)
    {
        $"IsFrozen changed to: {value}".Out(ConsoleColor.Cyan);
        UpdateBrushes();
    }

    void UpdateBrushes()
    {
        ProgressBrush = IsFrozen ? Brushes.Cyan : IsTriggered ? Brushes.Red : Brushes.Yellow;
        VoiceMeterBrush = IsFrozen ? Brushes.Cyan : Brushes.Lime;
    }

    partial void OnIsActivatedChanged(bool value)
    {
        AvatarFlags = (IsActivated ? AvatarFlags.Active : default(AvatarFlags))
            | (IsTriggered ? AvatarFlags.TriggeredNormal : default(AvatarFlags));
    }

    partial void OnSelectedSamplingRateChanged(SamplingRate? value)
    {
        Local.SelectedSamplingRateIndex = Array.IndexOf(SamplingRates, value);
        AudioCaptureTimer.Interval = IntervalFromSampleRate(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static TimeSpan IntervalFromSampleRate(SamplingRate? rate)
    {
        rate ??= SamplingRate.Default;
        return TimeSpan.FromSeconds(1.0 / rate.SamplesPerSecond);
    }

    [RelayCommand] public void ToggleCapture() => AudioCaptureEnabled = !AudioCaptureEnabled;
    partial void OnAudioCaptureEnabledChanged(bool value)
    {
        if (!value)
        {
            // Note: it might be beneficial to let progress go down while mic input is off.
            Progress = 0;
            AudioVolume = 0;
        }
        else
        {
            //FrozenNormalTotalTicks = 0;
            //FrozenTriggeredTotalTicks = 0;
            TotalFrozenTicks = 0;
            LastAudioCaptureTick = Environment.TickCount64;
            LastJumpTick = 0;
        }
        AudioCaptureTimer.IsEnabled = value;
        Local.IsAudioCaptureActive = value;
    }

    partial void OnHotkeysChanged(ModelHotkey[] value) => UpdateActive();
    partial void OnSelectedHotkeyChanged(ModelHotkey? value)
    {
        Roaming.SelectedHotkey = value;
        UpdateActive();
    }
    partial void OnSelectedExpressionChanged(ModelExpression? value)
    {
        Roaming.SelectedExpression = value;
        UpdateActive();
    }
    void UpdateActive()
    {
        if (SelectedExpression is null || SelectedHotkey is null)
        {
            if (Hotkeys is not null)
                Array.ForEach(Hotkeys, static h => h.LinkState = HotkeyLinkState.Dormant);
            if (Expressions is not null)
                Array.ForEach(Expressions, static e => e.LinkState = HotkeyLinkState.Dormant);
        }
        else
        {
            if (Hotkeys is not null)
                foreach (var hotkey in Hotkeys)
                    hotkey.LinkState = hotkey.ExpressionFile == SelectedExpression.ExpressionFile ? HotkeyLinkState.Active : HotkeyLinkState.Dormant;
            if (Expressions is not null)
                foreach (var expression in Expressions)
                    expression.LinkState = expression == SelectedExpression ? HotkeyLinkState.Active : HotkeyLinkState.Dormant;
        }
    }
}
