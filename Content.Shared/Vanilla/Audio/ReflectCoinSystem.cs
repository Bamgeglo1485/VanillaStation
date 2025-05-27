using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Vanilla.Audio;

[Serializable, NetSerializable]
public sealed class PlayGlobalMusicEvent : EntityEventArgs
{
    public ResolvedSoundSpecifier Specifier;
    public AudioParams? AudioParams;
    public int Priority;

    public PlayGlobalMusicEvent(ResolvedSoundSpecifier specifier, int priority = 0, AudioParams? audioParams = null)
    {
        Specifier = specifier;
        AudioParams = audioParams;
        Priority = priority;
    }
}

[Serializable, NetSerializable]
public sealed class StopGlobalMusicEvent : EntityEventArgs
{
    public int Priority;
    public bool ForceStop;

    public StopGlobalMusicEvent(int priority = 0, bool forceStop = false)
    {
        Priority = priority;
        ForceStop = forceStop;
    }
}
