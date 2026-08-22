namespace VoiceTrigger;

public enum HotkeyLinkState
{
    /// <summary>
    /// Defaults to inactive when VTS is disconnected.
    /// </summary>
    //Disconnected,
    /// <summary>
    /// Not in use at the moment.
    /// Returned when hotkey belongs to an currently unloaded model.
    /// </summary>
    Dormant,
    /// <summary>
    /// In active use.
    /// </summary>
    Active,
    /// <summary>
    /// Conflicts with another hotkey.
    /// Used when multiple hotkeys from the same model use different expressions.
    /// </summary>
    /// <remarks>
    /// Both conflicting hotkeys will be ignored.
    /// </remarks>
    //Conflicting,
}
