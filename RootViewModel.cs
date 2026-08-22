using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using VoiceTrigger.Services;
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
public sealed partial class RootViewModel : ObservableObject
{
    const long MinimumJumpSpacingMs = 300;

    // Audio input:
    [ObservableProperty] public partial double AudioVolume { get; set; }
    [ObservableProperty] public partial bool AudioCaptureEnabled { get; set; }
    [ObservableProperty] public partial DeviceViewModel[] AudioCaptureDevices { get; set; } = [];
    [ObservableProperty] public partial DeviceViewModel? SelectedAudioCaptureDevice { get; set; }
    [ObservableProperty] public partial SamplingRate[] SamplingRates { get; set; } = [];
    [ObservableProperty] public partial SamplingRate? SelectedSamplingRate { get; set; }

    // Hotkeys & Avatar state.
    [ObservableProperty] public partial ModelHotkey[] Hotkeys { get; set; } = [];
    [ObservableProperty] public partial ModelHotkey? SelectedHotkey { get; set; }
    [ObservableProperty] public partial ModelExpression[] Expressions { get; set; } = [];
    [ObservableProperty] public partial ModelExpression? SelectedExpression { get; set; }
    [ObservableProperty] public partial Model CurrentModel { get; set; }
    [ObservableProperty] public partial bool ModelLoaded { get; set; }
    [ObservableProperty] public partial bool IsActivated { get; set; } = false;
    [ObservableProperty] public partial bool IsTriggered { get; set; } = false;
    [ObservableProperty] public partial AvatarFlags AvatarFlags { get; set; }
    [ObservableProperty] public partial Brush ProgressBrush { get; set; } = Brushes.Yellow;

    // Trigger controls.
    /// <summary>
    /// Whether system inputs is frozen. Happens when you manually switch the states.
    /// </summary>
    /// <remarks>
    /// Will not unfreeze unless you manually return back to 
    /// </remarks>
    [ObservableProperty] public partial bool IsFrozen { get; set; }
    /// <summary>
    /// Enables/Disables system unfreezing while Kobi.
    /// </summary>
    [ObservableProperty] public partial bool AllowUnfreezeWhileNormal { get; set; } = true;
    [ObservableProperty] public partial double NormalUnfreezeDelay { get; set; } = 15;
    /// <summary>
    /// Enables/Disables system unfreezing while IBOK.
    /// </summary>
    [ObservableProperty] public partial bool AllowUnfreezeWhileTriggered { get; set; } = false;
    [ObservableProperty] public partial double TriggeredUnfreezeDelay { get; set; } = 30;

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
    [ObservableProperty] public partial double NormalActivationJump { get; set; } = 0;
    /// <summary>
    /// Immediate triggered state progress gain from crossing the <see cref="ActivationThreshold"/>.
    /// </summary>
    [ObservableProperty] public partial double TriggeredActivationJump { get; set; } = 0;
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
    MMDevice? ActiveAudioCaptureDevice;
    long LastAudioCaptureTick;
    long FrozenNormalTotalTicks;
    long FrozenTriggeredTotalTicks;
    long LastJumpTick;

    public RootViewModel()
    {
        ActivationPower = ConfigurationService.Roaming.ActivationPower;
        NormalActivationJump = ConfigurationService.Roaming.NormalActivationJump;
        TriggeredReleaseDuration = ConfigurationService.Roaming.TriggeredReleaseDuration;
        NormalActivationDuration = ConfigurationService.Roaming.NormalActivationDuration;
        TriggeredActivationJump = ConfigurationService.Roaming.TriggeredActivationJump;

        var hotkey = ConfigurationService.Roaming.SelectedHotkey;
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

        var expression = ConfigurationService.Roaming.SelectedExpression;
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
        index = ConfigurationService.Roaming.SelectedSamplingRateIndex;
        if (index < 0 || index > SamplingRates.Length)
            SelectedSamplingRate = SamplingRates[2];
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
        index = ConfigurationService.Roaming.SelectedResistanceIndex;
        if (index < 0 || index > Resistances.Length)
            SelectedResistance = Resistances[2];
        else
            SelectedResistance = Resistances[index];

        LastAudioCaptureTick = Environment.TickCount64;
        AudioCaptureTimer.Tick += SampleTick;
        AudioCaptureTimer.Interval = IntervalFromSampleRate(SelectedSamplingRate);
        AudioCaptureEnabled = ConfigurationService.Local.IsAudioCaptureActive;
        AudioCaptureTimer.IsEnabled = AudioCaptureEnabled;
        SelectedAudioCaptureDevice = null;
        Activate();

        VTubeStudio.Instance.Events.Track<VTSHotkeyTriggeredEvent>(HandleHotkeyTriggered);
        VTubeStudio.Instance.Events.Track<VTSModelLoadedEvent>(HandleModelLoaded);
        // TODO: Sub to trigger events.
        //  Calculate triggered state based on event feedbacks (unless VTS is disabled?)
        //VTubeStudio.Instance.OnAuthenticated //...
        VTubeStudio.Instance.OnAuthenticated += () => _ = UpdateStates();
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

    partial void OnSelectedResistanceChanged(TriggeredResistance? value)
    {
        ConfigurationService.Roaming.SelectedResistanceIndex = Array.IndexOf(Resistances, value);
    }

    partial void OnActivationPowerChanged(double value)
    {
        ConfigurationService.Roaming.ActivationPower = value;
    }

    partial void OnNormalActivationDurationChanged(double value)
    {
        ConfigurationService.Roaming.NormalActivationDuration = value;
    }

    partial void OnTriggeredReleaseDurationChanged(double value)
    {
        ConfigurationService.Roaming.TriggeredReleaseDuration = value;
    }

    partial void OnNormalActivationJumpChanged(double value)
    {
        ConfigurationService.Roaming.NormalActivationJump = value;
    }

    partial void OnTriggeredActivationJumpChanged(double value)
    {
        ConfigurationService.Roaming.TriggeredActivationJump = value;
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

        // TODO: Implement.
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
                    if (result.Data.AvailableHotkeys is null || result.Data.AvailableHotkeys.Length == 0)
                    {
                        if (SelectedHotkey is not null)
                            Hotkeys = [SelectedHotkey];
                        else
                            Hotkeys = [];
                    }
                    else
                    {
                        var list = result.Data.AvailableHotkeys.Select(h => new ModelHotkey()
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
        // Debug hotkey trigger test: 
        //await VTubeStudio.Instance.Request(new VTSHotkeyTriggerRequest()
        //{
        //    Data = new()
        //    {
        //        HotkeyID = "158eb62bdd5d438ca5175516154131dc",
        //        ItemInstanceID = null,
        //    },
        //});

        IsTriggered = !IsTriggered;

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

    private async void HandleHotkeyTriggered(VTSHotkeyTriggeredEvent e)
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
            // Used manually triggered the transition.
            FrozenNormalTotalTicks = 0;
            FrozenTriggeredTotalTicks = 0;
            IsFrozen = true;
            IsTriggered = !IsTriggered; // API hotkeys already predict future state.
        }

        var result = await VTubeStudio.Instance.Request<VTSExpressionStateResponse>(new VTSExpressionStateRequest()
        {
            Data = new()
            {
                Details = false,
                ExpressionFile = hotkey.ExpressionFile,
            }
        });

        if (result.ResolveSuccess(out var response) && response.Data is not null)
        {
            if (response.Data.Expressions is null)
            {
                $"Expressions collection is null! Cannot update current triggered state!".Out(ConsoleColor.Yellow);
                return;
            }
            if (response.Data.Expressions.Length != 1)
            {
                $"Multiple expressions listed under one file! Triggered state might be inaccurate.".Out(ConsoleColor.Yellow);
            }
            IsTriggered = response.Data.Expressions.Any(static e => e.Active);
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
            if (IsTriggered)
            {
                if (AllowUnfreezeWhileTriggered)
                {
                    long elapsed = tick - LastAudioCaptureTick;
                    FrozenTriggeredTotalTicks += elapsed;
                    if (FrozenTriggeredTotalTicks >= (long)TimeSpan.FromSeconds(TriggeredUnfreezeDelay).TotalMilliseconds)
                        IsFrozen = false;
                }
            }
            else
            {
                if (AllowUnfreezeWhileNormal)
                {
                    long elapsed = tick - LastAudioCaptureTick;
                    FrozenNormalTotalTicks += elapsed;
                    if (FrozenNormalTotalTicks >= (long)TimeSpan.FromSeconds(NormalUnfreezeDelay).TotalMilliseconds)
                        IsFrozen = false;
                }
            }
        }

        // Input volume update.
        if (!AudioCaptureEnabled || ActiveAudioCaptureDevice is null)
        {
            AudioVolume = 0;
        }
        else
        {
            var channels = ActiveAudioCaptureDevice.AudioMeterInformation.PeakValues;
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

        $"Audio volume: {AudioVolume}".Out();
        bool active = AudioVolume >= ActivationThreshold;
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

        // Mitigates division by zero exceptions.
        double power = Math.Clamp(ActivationPower, 0.001, 1000);
        double threshold = Math.Clamp(ActivationThreshold, 0.001, 1);

        // Indicates how close AudioVolume is to the max possible volume [0.0:1.0], using ActivationThreshold as origin.
        double from, to, value, result;
        if (active)
        {
            value = AudioVolume;
            from = threshold;
            to = 1;
            result = (value - from) / (to - from);
            result = Math.Pow(result, 0.5) * power; // Values close to 0 are multiplied by ~2-3.
        }
        else
        {
            value = AudioVolume;
            from = threshold;
            to = 0;
            result = (value - from) / (to - from);
            result = -Math.Pow(result, 0.5) * 0.3; // Values close to 0 are multiplied by ~2-3.
        }


        //double relative = active
        //        ? Math.Clamp((AudioVolume - threshold) / (1 - threshold), 0, 1)
        //        : Math.Clamp(AudioVolume / threshold, 0, 1);
        //double speed = active ? power : 1;
        ////double speed = Math.Pow(relative, (1 - power) / power);

        //double multiplier = relative * speed;
        if (IsTriggered)
        {
            var resistance = SelectedResistance ?? TriggeredResistance.Default;
            $"Resistance: {resistance.Resistance}".Out();
            value = AudioVolume;
            from = 1;
            to = threshold;
            double relativeActivation = Math.Clamp((value - from) / (to - from), 0, 1);
            double direction = (-1 * relativeActivation) + (Math.Max(result, 0) * resistance.Resistance);
            Progress = Math.Clamp(Progress + (direction / TriggeredReleaseDuration * delta), 0, 1);
        }
        else
        {
            double direction = result;
            Progress = Math.Clamp(Progress + (direction / NormalActivationDuration * delta), 0, 1);
        }

        if (IsTriggered)
        {
            if (Progress <= double.Epsilon)
            {
                IsTriggered = false;
                // Send state to the remote.
                // Do not send if IsFrozen.
            }
        }
        else
        {
            if (Progress >= 1 - double.Epsilon)
            {
                IsTriggered = true;
                // Send state to remote.
                // Do not send if IsFrozen.
            }
        }

        LastAudioCaptureTick = tick;
    }

    partial void OnIsTriggeredChanged(bool value)
    {
        AvatarFlags = (IsActivated ? AvatarFlags.Active : default(AvatarFlags))
            | (IsTriggered ? AvatarFlags.TriggeredNormal : default(AvatarFlags));
        $"IsTriggered changed to: {value}".Out(ConsoleColor.Cyan);
        ProgressBrush = IsTriggered ? Brushes.Red : Brushes.Yellow;
    }

    partial void OnIsActivatedChanged(bool value)
    {
        AvatarFlags = (IsActivated ? AvatarFlags.Active : default(AvatarFlags))
            | (IsTriggered ? AvatarFlags.TriggeredNormal : default(AvatarFlags));
    }

    partial void OnSelectedSamplingRateChanged(SamplingRate? value)
    {
        ConfigurationService.Roaming.SelectedSamplingRateIndex = Array.IndexOf(SamplingRates, value);
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
            AudioVolume = 0;
            Progress = 0;
        }
        else
        {
            FrozenNormalTotalTicks = 0;
            FrozenTriggeredTotalTicks = 0;
            LastAudioCaptureTick = Environment.TickCount64;
            LastJumpTick = 0;
        }
        AudioCaptureTimer.IsEnabled = value;
        ConfigurationService.Local.IsAudioCaptureActive = value;
    }

    bool IsFocused;
    [RelayCommand] public void Activate() => Application.Current.Dispatcher.Invoke(ActivateImmediate);
    private void ActivateImmediate()
    {
        if (IsFocused) return;
        try
        {
            IsFocused = true;
            RefreshInputDevicesImmediate();
            //_ = RefreshExpressions();
            //VTubeStudio.Instance.OnAuthenticated += HandleAuthenticated;
            //if (VTubeStudio.Instance.Authenticated)
            //    HandleAuthenticated();
        }
        catch { IsFocused = false; throw; }
    }

    [RelayCommand] public void Deactivate() => Application.Current.Dispatcher.Invoke(DeactivateImmediate);
    private void DeactivateImmediate()
    {
        if (!IsFocused) return;
        IsFocused = false;
        //VTubeStudio.Instance.OnAuthenticated -= HandleAuthenticated;
    }

    [RelayCommand] public void RefreshInputDevices() => Application.Current.Dispatcher.Invoke(RefreshInputDevicesImmediate);
    private void RefreshInputDevicesImmediate()
    {
        $"Refreshing input devices...".Out();
        var collection = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        if (collection.Count == 0)
        {
            SelectedAudioCaptureDevice = null;
            ActiveAudioCaptureDevice = null;
            AudioCaptureDevices = [];
            return;
        }

        string id;
        AudioCaptureDevices = [.. collection.Select(static d => new DeviceViewModel() { ID = d.ID, DisplayName = d.FriendlyName })];
        if (SelectedAudioCaptureDevice is null)
        {
            var device = collection.FirstOrDefault(static d => string.Equals(d.ID, ConfigurationService.Roaming.SelectedAudioDeviceID));
            if (device is null)
                ActiveAudioCaptureDevice = collection[0];
            else
                ActiveAudioCaptureDevice = device;
        }
        else
        {
            id = SelectedAudioCaptureDevice.ID;
            ActiveAudioCaptureDevice = collection.FirstOrDefault(d => d.ID == id) ?? collection[0];
        }

        id = ActiveAudioCaptureDevice.ID;
        SelectedAudioCaptureDevice = AudioCaptureDevices.FirstOrDefault(d => d.ID == id);
        $"Input devices refreshed! Found: {AudioCaptureDevices.Length}".Out();
    }

    partial void OnSelectedAudioCaptureDeviceChanged(DeviceViewModel? value)
    {
        ConfigurationService.Roaming.SelectedAudioDeviceID = value?.ID ?? string.Empty;
    }

    //Task? RefreshTask;

    //[RelayCommand]
    //public Task RefreshExpressions()
    //{
    //    if (RefreshTask is null || RefreshTask.IsCompleted)
    //    {
    //        return RefreshTask = RefreshExpressionsInternal();
    //    }
    //    else return RefreshTask;
    //}
    //async Task RefreshExpressionsInternal()
    //{
    //    $"Refreshing model expression list..".Out();
    //    if (VTubeStudio.Instance.Status != VTSStatus.Authenticated)
    //    {
    //        $"VTS not authenticated (Status: {VTubeStudio.Instance.Status}). Resetting expression list.".Out();
    //        Application.Current.Dispatcher.Invoke(ResetExpressions);
    //        return;
    //    }
    //    var result = await VTubeStudio.Instance.Request<VTSExpressionStateResponse>(new VTSExpressionStateRequest
    //    {
    //        Data = new()
    //        {
    //            Details = false,
    //            ExpressionFile = string.Empty,
    //        }
    //    });
    //    if (result.ResolveSuccess(out var response) && response.Data is not null)
    //    {
    //        if (!response.Data.ModelLoaded || response.Data.Expressions is null)
    //        {
    //            Application.Current.Dispatcher.Invoke(ResetExpressions);
    //        }
    //        else
    //        {
    //            var list = response.Data.Expressions.Select(e => new ExpressionViewModel()
    //            {
    //                ModelID = response.Data.ModelID ?? string.Empty,
    //                ModelName = response.Data.ModelName ?? string.Empty,
    //                Name = e.Name ?? string.Empty,
    //                DisplayName = e.Name ?? string.Empty,
    //                Exists = true,
    //            }).ToList();
    //            Application.Current.Dispatcher.Invoke(() => SetExpressions(list));
    //        }
    //        $"Expression list refreshed successfully!".Out();
    //    }
    //    else
    //    {
    //        $"Cannot refresh model parameters! Received:\n{result}".Out(ConsoleColor.Yellow);
    //    }

    //    void ResetExpressions() => SetExpressions([]);
    //    void SetExpressions(List<ExpressionViewModel> expressions)
    //    {
    //        if (expressions.Count == 0)
    //        {
    //            if (SelectedModelExpression is not null)
    //            {
    //                SelectedModelExpression.Exists = false;
    //                ModelExpressions = [SelectedModelExpression];
    //            }
    //            else ModelExpressions = [];
    //            return;
    //        }

    //        if (SelectedModelExpression is not null)
    //        {
    //            var selected = SelectedModelExpression;
    //            if (expressions.Contains(selected))
    //            {
    //                selected.Exists = true;
    //            }
    //            else
    //            {
    //                var similar = expressions.Find(ex => ex.Name == selected.Name);
    //                if (similar is not null)
    //                {
    //                    selected = similar;
    //                    selected.Exists = true;
    //                }
    //                else
    //                {
    //                    expressions.Add(selected);
    //                    selected.Exists = false;
    //                }
    //            }

    //            SelectedModelExpression = null;
    //            ModelExpressions = [.. expressions];
    //            SelectedModelExpression = selected;
    //        }
    //        else
    //        {
    //            SelectedModelExpression = null;
    //            ModelExpressions = [.. expressions];
    //        }
    //    }
    //}

    //partial void OnSelectedModelExpressionChanged(ExpressionViewModel? value)
    //{
    //    SelectedModelExpressionExists = value is not null && value.Exists;

    //    // Don't clean-up unless selected a valid expression.
    //    // Makes sure you can select non-existing expressions if you have any from a previous model.
    //    if (!SelectedModelExpressionExists) return;
    //    if (Array.Exists(ModelExpressions, static e => !e.Exists))
    //    {
    //        ModelExpressions = ModelExpressions.Where(static e => e.Exists).ToArray();
    //    }
    //}

    partial void OnHotkeysChanged(ModelHotkey[] value) => UpdateActive();
    partial void OnSelectedHotkeyChanged(ModelHotkey? value)
    {
        ConfigurationService.Roaming.SelectedHotkey = value;
        UpdateActive();
    }
    partial void OnSelectedExpressionChanged(ModelExpression? value)
    {
        ConfigurationService.Roaming.SelectedExpression = value;
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
