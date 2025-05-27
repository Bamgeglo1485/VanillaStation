using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Vanilla.Audio;

[Serializable, NetSerializable]
public sealed class PlayGlobalMusicEvent : EntityEventArgs
{
    public ResolvedSoundSpecifier Specifier { get; }
    public AudioParams? AudioParams { get; }

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
