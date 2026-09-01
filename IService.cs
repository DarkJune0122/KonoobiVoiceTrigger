namespace VoiceTrigger;

public interface IService
{
    /// <summary>
    /// Initializes service state and its resources.
    /// Loads saves from <see cref="Configuration.ConfigurationService"/>, if stateful.
    /// </summary>
    public void Initialize();
    /// <summary>
    /// Unloads all held resources.
    /// State might not be saved to <see cref="Configuration.ConfigurationService"/>, as it is usually stored immediate on change.
    /// </summary>
    public void Terminate();
}
