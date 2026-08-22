namespace VoiceTrigger;

[Flags]
public enum AvatarFlags
{
    Normal = 0b00,
    Active = 0b01,
    TriggeredNormal = 0b10,
    TriggeredActive = 0b11,
}
