namespace VoiceTrigger.Audio;

public sealed class AudioDeviceDescriptor(string id, string name)
{
    public string ID = id;
    public string FriendlyName = name;
}
