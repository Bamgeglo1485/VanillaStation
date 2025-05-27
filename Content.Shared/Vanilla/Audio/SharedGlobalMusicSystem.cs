using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio;

[Serializable, NetSerializable]
public sealed class PlayGlobalMusicEvent : EntityEventArgs
{
    public ResolvedSoundSpecifier Specifier;
    public AudioParams? AudioParams;

    public PlayGlobalMusicEvent(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    {
        Specifier = specifier;
        AudioParams = audioParams;
    }
}

[Serializable, NetSerializable]
public sealed class StopGlobalMusicEvent : EntityEventArgs
{
}
