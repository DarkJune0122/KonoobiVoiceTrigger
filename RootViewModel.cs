using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using VoiceTrigger.VTS;
using VoiceTrigger.VTS.Events;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger;

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
    [ObservableProperty] public partial ExpressionHotkey[] Hotkeys { get; set; } = [];
    [ObservableProperty] public partial bool IsActivated { get; set; }
    [ObservableProperty] public partial bool IsTriggered { get; set; }

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
    [ObservableProperty] public partial double ActivationThreshold { get; set; } = 0.12;
    /// <summary>
    /// [0.0 - 1.0] Describes how fast progress bar will move, depending on how loud you are.
    /// </summary>
    /// <remarks>
    /// First, from current audio meter, it calculates remaining delta from <see cref="ActivationThreshold"/>.
    /// Then, it calculates "relative position" (RP), using <see cref="ActivationThreshold"/> as origin.
    /// Then, power:
    /// With RP of 10% (0.1) - it will move 
    /// </remarks>
    [ObservableProperty] public partial double ActivationPower { get; set; } = 0.2;
    /// <summary>
    /// In seconds.
    /// </summary>
    [ObservableProperty] public partial double NormalActivationDuration { get; set; } = 6;
    /// <summary>
    /// In seconds.
    /// </summary>
    [ObservableProperty] public partial double TriggeredReleaseDuration { get; set; } = 12;
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
        Hotkeys = [
            new(), new(), new(),
            new(), new(), new(),
            new(), new(), new(),
        ];

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
        // TODO: Retrieve from config.
        SelectedSamplingRate = SamplingRates[1];

        Resistances = [
            new("Lowest", 0.7),
            new("Low", 1.1),
            new("Normal", 1.8),
            new("Higher", 2.5),
            new("High", 3.2),
            new("\"Mid\"", 5),
        ];
        // TODO: Retrieve from config.
        SelectedResistance = Resistances[2];

        LastAudioCaptureTick = Environment.TickCount64;
        AudioCaptureTimer.Tick += SampleTick;
        AudioCaptureTimer.Interval = IntervalFromSampleRate(SelectedSamplingRate);
        AudioCaptureTimer.IsEnabled = AudioCaptureEnabled;
        Activate();

        VTubeStudio.Instance.Events.Track<VTSHotkeyTriggeredEvent>(HandleHotkeyTriggered);
        // TODO: Sub to trigger events.
        //  Calculate triggered state based on event feedbacks (unless VTS is disabled?)
        //VTubeStudio.Instance.OnAuthenticated //...
    }

    /// <summary>
    /// Updates all hotkeys and expressions for currently selected model.
    /// </summary>
    /// <remarks>
    /// Primarily for debugging.
    /// </remarks>
    [RelayCommand]
    public async Task UpdateParameters()
    {
        $"Updating hotkeys and expressions...".Out();

        // TODO: Implement.
        await Task.CompletedTask;

        $"Update complete!".Out();
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

        // TODO: Update hotkey states.
        var hotkey = Hotkeys.FirstOrDefault(static h => h.State == HotkeyState.Active);
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
        if (!e.Data.HotkeyTriggeredByAPI)
        {
            // Used manually triggered the transition.
            FrozenNormalTotalTicks = 0;
            FrozenTriggeredTotalTicks = 0;
            IsFrozen = true;
        }

        IsTriggered = !IsTriggered;
        var hotkey = Hotkeys.FirstOrDefault(static h => h.State == HotkeyState.Active);
        if (hotkey is null) return;
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

        if (IsActivated = active)
        {
            // Mitigates division by zero exceptions.
            if (ActivationPower < double.Epsilon || ActivationThreshold >= 1 - double.Epsilon)
            {
                Progress = 1;
            }
            else
            {
                // Indicates how close AudioVolume is to the max possible volume [0.0:1.0], using ActivationThreshold as origin.
                double relative = (AudioVolume - ActivationThreshold) / (1 - ActivationThreshold);
                double speed = Math.Pow(relative, (1 - ActivationPower) / ActivationPower);
                double multiplier = relative * speed;
                if (IsTriggered)
                {
                    var resistance = SelectedResistance ?? TriggeredResistance.Default;
                    double direction = 1 + multiplier - resistance.Resistance;
                    Progress = Math.Clamp(Progress + (direction / TriggeredReleaseDuration), 0, 1);
                }
                else
                {
                    Progress = Math.Clamp(Progress + (multiplier / NormalActivationDuration), 0, 1);
                }
            }
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
        $"IsTriggered changed to: {value}".Out(ConsoleColor.Cyan);
    }

    partial void OnSelectedSamplingRateChanged(SamplingRate? value)
    {
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
    }

    [RelayCommand] public void Activate() => Application.Current.Dispatcher.Invoke(ActivateImmediate);
    private void ActivateImmediate()
    {
        if (IsActivated) return;
        try
        {
            IsActivated = true;
            RefreshInputDevicesImmediate();
            //_ = RefreshExpressions();
            //VTubeStudio.Instance.OnAuthenticated += HandleAuthenticated;
            //if (VTubeStudio.Instance.Authenticated)
            //    HandleAuthenticated();
        }
        catch { IsActivated = false; throw; }
    }

    [RelayCommand] public void Deactivate() => Application.Current.Dispatcher.Invoke(DeactivateImmediate);
    private void DeactivateImmediate()
    {
        if (!IsActivated) return;
        IsActivated = false;
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
            ActiveAudioCaptureDevice = collection[0];
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
}
