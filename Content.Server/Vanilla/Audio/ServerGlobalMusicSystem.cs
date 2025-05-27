using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Audio;

public sealed class ServerGlobalMusicSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public void PlayGlobalMusic(ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    {
        var msg = new PlayGlobalMusicEvent(specifier, audioParams);
        RaiseNetworkEvent(msg, Filter.Broadcast());
    }

    public void StopGlobalMusic()
    {
        RaiseNetworkEvent(new StopGlobalMusicEvent(), Filter.Broadcast());
    }
}
