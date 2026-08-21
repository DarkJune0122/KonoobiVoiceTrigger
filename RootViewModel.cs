using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using System.Drawing;
using System.Windows.Threading;
using VoiceTrigger.VTS;
using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger;

public sealed partial class VTSHotkeyViewModel : ObservableObject
{
    public const string DefaultName = "Unknown";
    public const string DefaultType = "";
    public const string DefaultDescription = "";
    public const string DefaultHotkeyID = "";

    [ObservableProperty] public required partial string HotkeyID { get; set; } = DefaultHotkeyID;
    [ObservableProperty] public required partial string Name { get; set; } = DefaultName;
    [ObservableProperty] public required partial string Type { get; set; } = DefaultType;
    [ObservableProperty] public required partial string Description { get; set; } = DefaultDescription;
}

public sealed partial class VTSModelViewModel : ObservableObject
{
    public const string DefaultName = "Unknown";
    public const string DefaultModelID = "";

    [ObservableProperty] public required partial string ModelID { get; set; } = DefaultModelID;
    [ObservableProperty] public required partial string Name { get; set; } = DefaultName;
    [ObservableProperty] public required partial VTSHotkeyViewModel[]? Hotkeys { get; set; }


}

public sealed partial class TriggerViewModel(RootViewModel root) : ObservableObject
{
    public readonly RootViewModel Root = root;

    [ObservableProperty] public partial VTSModelViewModel? SelectedModel { get; set; }
    [ObservableProperty] public partial VTSHotkeyViewModel[]? SelectedModelHotkeys { get; set; }
    [ObservableProperty] public partial VTSHotkeyViewModel? SelectedHotkey { get; set; }

    partial void OnSelectedModelChanged(VTSModelViewModel? value)
    {
        if (value is null || value.Hotkeys is null)
        {
            SelectedModelHotkeys = null;
            SelectedHotkey = null;
            return;
        }

        SelectedModelHotkeys = value.Hotkeys;
        if (Array.IndexOf(SelectedModelHotkeys, value) == -1)
        {
            SelectedModel = null;
        }
    }
}

// TODO: Serialize states. Move Advanced settings in a separate .cfg file.
// TODO: Go through the logic and find out - matbe we can make the logic funnier to use? More engaging, etc.
public sealed partial class RootViewModel : ObservableObject
{
    const long MinBoostSpacingMs = 1000;

    [ObservableProperty] public partial double AudioGain { get; set; }
    [ObservableProperty] public partial double AudioVolume { get; set; }
    [ObservableProperty] public partial double LeftAudioVolume { get; set; }
    [ObservableProperty] public partial double RightAudioVolume { get; set; }
    [ObservableProperty] public partial bool AudioCaptureEnabled { get; set; }
    [ObservableProperty] public partial DeviceViewModel[] AudioCaptureDevices { get; set; } = [];
    [ObservableProperty] public partial DeviceViewModel? SelectedAudioCaptureDevice { get; set; }
    [ObservableProperty] public partial ExpressionViewModel[] ModelExpressions { get; set; } = [];
    [ObservableProperty] public partial ExpressionViewModel? SelectedModelExpression { get; set; }
    [ObservableProperty] public partial bool SelectedModelExpressionExists { get; set; }
    [ObservableProperty] public partial VTSModelViewModel[] VTSModels { get; set; } = [];
    [ObservableProperty] public partial double ActivationThreshold { get; set; } = 0.12;
    [ObservableProperty] public partial bool IsVolumeActivated { get; set; }
    [ObservableProperty] public partial bool Triggered { get; set; }
    [ObservableProperty] public partial bool Frozen { get; set; }
    [ObservableProperty] public partial Color IndicatorColor { get; set; }
    [ObservableProperty] public partial Brush IndicatorBrush { get; set; } = Brushes.LimeGreen;
    [ObservableProperty] public partial double TriggerProgress { get; set; }
    [ObservableProperty] public partial double TriggerChargeBoost { get; set; } = 0.2;
    [ObservableProperty] public partial double ChargeTime { get; set; } = 3.2;
    [ObservableProperty] public partial double DischargeTime { get; set; } = 16.0;

    readonly DispatcherTimer CaptureTimer = new();
    MMDevice? ActiveAudioCaptureDevice;
    long LastTick = Environment.TickCount64;
    long LastBoostTick;
    long TotalTicks;

    public RootViewModel()
    {
        CaptureTimer.Tick += HandleCaptureTick;
        CaptureTimer.Interval = TimeSpan.FromMicroseconds(100); // 10
        CaptureTimer.IsEnabled = AudioCaptureEnabled;
        Activate();
        // TODO: Sub to trigger events.
        //  Calculate triggered state based on event feedbacks (unless VTS is disabled?)
        //VTubeStudio.Instance.OnAuthenticated //...
    }

    partial void OnTriggeredChanged(bool value)
    {
        // TODO: Send state to Live2D.
        //var result =
    }

    [RelayCommand]
    public static async Task ToggleModelColor()
    {
        await VTubeStudio.Instance.Request(new VTSHotkeyTriggerRequest()
        {
            Data = new()
            {
                HotkeyID = "158eb62bdd5d438ca5175516154131dc",
                ItemInstanceID = null,
            },
        });
    }
    [RelayCommand] public void ToggleCapture() => AudioCaptureEnabled = !AudioCaptureEnabled;
    partial void OnAudioCaptureEnabledChanged(bool value)
    {
        CaptureTimer.IsEnabled = value;
        if (!value)
        {
            LastBoostTick = 0;
            LeftAudioVolume = 0;
            RightAudioVolume = 0;
            IsVolumeActivated = false;
            TriggerProgress = 0;
            Triggered = false;
        }
        else
        {
            LastTick = Environment.TickCount64;
        }
    }
    private void HandleCaptureTick(object? sender, EventArgs e)
    {
        Console.WriteLine(nameof(HandleCaptureTick));
        CaptureTickImmediate();
    }

    const long MaxTickCount = 128_000_000_000; // Large enough to never hit, but small enough to never overflow when exceeded.
    [RelayCommand] public void CaptureTick() => Application.Current.Dispatcher.Invoke(CaptureTickImmediate);
    private void CaptureTickImmediate()
    {
        if (ActiveAudioCaptureDevice is null)
        {
            LeftAudioVolume = 0;
            RightAudioVolume = 0;
            Console.WriteLine($"[{DateTime.Now}] Missing input device.");
            return;
        }
        if (ActiveAudioCaptureDevice.State != DeviceState.Active)
        {
            LeftAudioVolume = 0;
            RightAudioVolume = 0;
            Console.WriteLine($"[{DateTime.Now}] Selected input device is not active anymore. Resetting...");
            ActiveAudioCaptureDevice = null;
            return;
        }

        var channels = ActiveAudioCaptureDevice.AudioMeterInformation.PeakValues;
        switch (channels.Count)
        {
            case 0:
                AudioVolume = 0f;
                LeftAudioVolume = AudioVolume;
                RightAudioVolume = AudioVolume;
                Console.WriteLine($"[{DateTime.Now}][0]: {LeftAudioVolume} (s: {TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds})");
                break;
            case 1:
                AudioVolume = channels[0];
                LeftAudioVolume = AudioVolume;
                RightAudioVolume = AudioVolume;
                Console.WriteLine($"[{DateTime.Now}][1]: {LeftAudioVolume} (s: {TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds})");
                break;
            case 2:
                LeftAudioVolume = channels[0];
                RightAudioVolume = channels[1];
                AudioVolume = Math.Max(LeftAudioVolume, RightAudioVolume);
                Console.WriteLine($"[{DateTime.Now}][2]: {LeftAudioVolume} + {RightAudioVolume} (s: {TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds})");
                break;
            default:
                if (channels.Count < 0) goto case 0;
                else goto case 2;
        }

        IsVolumeActivated = AudioVolume > ActivationThreshold;

        long tick = Environment.TickCount64;
        long delta = tick - LastTick;
        if (!Triggered)
        {
            if (IsVolumeActivated)
            {
                TotalTicks = Math.Clamp(TotalTicks + delta, 0, MaxTickCount);
            }
            else
            {
                // Reduces timer to extend the transformation once triggered.
                TotalTicks = Math.Clamp(TotalTicks - delta, 0, MaxTickCount);
            }

            double total = TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds;
            if (total >= ChargeTime)
            {
                Triggered = true;
                TriggerProgress = 1;
                TotalTicks = 0;
            }
            else
            {
                TriggerProgress = total / ChargeTime;
            }
        }
        else
        {
            if (IsVolumeActivated)
            {
                // Reduces timer to extend the transformation once triggered.
                TotalTicks = Math.Clamp(TotalTicks - delta, 0, MaxTickCount);
            }
            else
            {
                TotalTicks = Math.Clamp(TotalTicks + delta, 0, MaxTickCount);
            }

            double total = TimeSpan.FromMilliseconds(TotalTicks).TotalSeconds;
            if (total >= DischargeTime)
            {
                Triggered = false;
                TriggerProgress = 1;
                TotalTicks = 0;
            }
            else
            {
                TriggerProgress = total / DischargeTime;
            }
        }
        LastTick = Environment.TickCount64;
    }

    partial void OnIsVolumeActivatedChanged(bool value)
    {
        long tick = Environment.TickCount64;
        if (!Triggered && value && TimeSpan.FromMilliseconds(tick - LastBoostTick).TotalMilliseconds >= MinBoostSpacingMs)
        {
            LastBoostTick = tick;
            TotalTicks = Math.Clamp(TotalTicks + (long)TimeSpan.FromSeconds(TriggerChargeBoost).TotalMilliseconds, 0, MaxTickCount);
        }
    }

    bool IsActivated;
    [RelayCommand] public void Activate() => Application.Current.Dispatcher.Invoke(ActivateImmediate);
    private void ActivateImmediate()
    {
        if (IsActivated) return;
        try
        {
            IsActivated = true;
            RefreshInputDevicesImmediate();
            _ = RefreshExpressions();
            VTubeStudio.Instance.OnAuthenticated += HandleAuthenticated;
            if (VTubeStudio.Instance.Authenticated)
                HandleAuthenticated();
        }
        catch { IsActivated = false; throw; }
    }

    [RelayCommand] public void Deactivate() => Application.Current.Dispatcher.Invoke(DeactivateImmediate);
    private void DeactivateImmediate()
    {
        if (!IsActivated) return;
        IsActivated = false;
        VTubeStudio.Instance.OnAuthenticated -= HandleAuthenticated;
    }

    private void HandleAuthenticated() => RefreshVTS();

    [RelayCommand] public void RefreshVTS() => Application.Current.Dispatcher.Invoke(RefreshVTSImmediate);
    private async void RefreshVTSImmediate()
    {
        var result = await VTubeStudio.Instance.Request<VTSAvailableModelsResponse>(VTSAvailableModelsRequest.Instance);
        if (result.ResolveSuccess(out var response) && response.Data?.AvailableModels?.Length > 0)
        {
            var array = response.Data.AvailableModels;
            var models = new VTSModelViewModel[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                var model = array[i];
                models[i] = new VTSModelViewModel()
                {
                    ModelID = model.ModelID ?? VTSModelViewModel.DefaultModelID,
                    Name = model.VTSModelName ?? VTSModelViewModel.DefaultName,
                    Hotkeys = null,
                };
            }
        }
        else
        {
            $"Model request failed. Nothing will be updated.".Out(ConsoleColor.Yellow);
        }
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

    Task? RefreshTask;

    [RelayCommand]
    public Task RefreshExpressions()
    {
        if (RefreshTask is null || RefreshTask.IsCompleted)
        {
            return RefreshTask = RefreshExpressionsInternal();
        }
        else return RefreshTask;
    }
    async Task RefreshExpressionsInternal()
    {
        $"Refreshing model expression list..".Out();
        if (VTubeStudio.Instance.Status != VTSStatus.Authenticated)
        {
            $"VTS not authenticated (Status: {VTubeStudio.Instance.Status}). Resetting expression list.".Out();
            Application.Current.Dispatcher.Invoke(ResetExpressions);
            return;
        }
        var result = await VTubeStudio.Instance.Request<VTSExpressionStateResponse>(new VTSExpressionStateRequest
        {
            Data = new()
            {
                Details = false,
                ExpressionFile = string.Empty,
            }
        });
        if (result.ResolveSuccess(out var response) && response.Data is not null)
        {
            if (!response.Data.ModelLoaded || response.Data.Expressions is null)
            {
                Application.Current.Dispatcher.Invoke(ResetExpressions);
            }
            else
            {
                var list = response.Data.Expressions.Select(e => new ExpressionViewModel()
                {
                    ModelID = response.Data.ModelID ?? string.Empty,
                    ModelName = response.Data.ModelName ?? string.Empty,
                    Name = e.Name ?? string.Empty,
                    DisplayName = e.Name ?? string.Empty,
                    Exists = true,
                }).ToList();
                Application.Current.Dispatcher.Invoke(() => SetExpressions(list));
            }
            $"Expression list refreshed successfully!".Out();
        }
        else
        {
            $"Cannot refresh model parameters! Received:\n{result}".Out(ConsoleColor.Yellow);
        }

        void ResetExpressions() => SetExpressions([]);
        void SetExpressions(List<ExpressionViewModel> expressions)
        {
            if (expressions.Count == 0)
            {
                if (SelectedModelExpression is not null)
                {
                    SelectedModelExpression.Exists = false;
                    ModelExpressions = [SelectedModelExpression];
                }
                else ModelExpressions = [];
                return;
            }

            if (SelectedModelExpression is not null)
            {
                var selected = SelectedModelExpression;
                if (expressions.Contains(selected))
                {
                    selected.Exists = true;
                }
                else
                {
                    var similar = expressions.Find(ex => ex.Name == selected.Name);
                    if (similar is not null)
                    {
                        selected = similar;
                        selected.Exists = true;
                    }
                    else
                    {
                        expressions.Add(selected);
                        selected.Exists = false;
                    }
                }

                SelectedModelExpression = null;
                ModelExpressions = [.. expressions];
                SelectedModelExpression = selected;
            }
            else
            {
                SelectedModelExpression = null;
                ModelExpressions = [.. expressions];
            }
        }
    }

    partial void OnSelectedModelExpressionChanged(ExpressionViewModel? value)
    {
        SelectedModelExpressionExists = value is not null && value.Exists;

        // Don't clean-up unless selected a valid expression.
        // Makes sure you can select non-existing expressions if you have any from a previous model.
        if (!SelectedModelExpressionExists) return;
        if (Array.Exists(ModelExpressions, static e => !e.Exists))
        {
            ModelExpressions = ModelExpressions.Where(static e => e.Exists).ToArray();
        }
    }
}
