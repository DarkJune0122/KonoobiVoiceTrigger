namespace VoiceTrigger;

public interface IAsyncService
{
    /// <summary>
    /// Initializes service state and its resources asyncronously.
    /// Loads saves from <see cref="Configuration.ConfigurationService"/>, if stateful.
    /// </summary>
    Task Initialize();
    /// <summary>
    /// Unloads all held resources asyncronously.
    /// State might not be saved to <see cref="Configuration.ConfigurationService"/>, as it is usually stored immediate on change.
    /// </summary>
    Task Terminate();
}