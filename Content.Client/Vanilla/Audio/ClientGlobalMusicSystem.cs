using Content.Shared.Vanilla.Audio;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Vanilla.Audio;

public sealed class ClientGlobalMusicSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private bool _musicEnabled = true;
    private EntityUid? _currentMusicStream;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, CCVars.EventMusicEnabled, ToggleMusic, true);
        SubscribeNetworkEvent<PlayGlobalMusicEvent>(OnPlayGlobalMusic);
        SubscribeNetworkEvent<StopGlobalMusicEvent>(OnStopGlobalMusic);
    }

    private void OnPlayGlobalMusic(PlayGlobalMusicEvent ev)
    {
        if (!_musicEnabled) return;

        if (_currentMusicStream.HasValue)
        {
            _audio.Stop(_currentMusicStream);
        }

        var stream = _audio.PlayGlobal(
            ev.Specifier,
            Filter.Local(),
            false,
            ev.AudioParams);

        _currentMusicStream = stream?.Entity;
    }

    private void OnStopGlobalMusic(StopGlobalMusicEvent ev)
    {
        if (_currentMusicStream.HasValue)
        {
            _audio.Stop(_currentMusicStream);
            _currentMusicStream = null;
        }
    }

    private void ToggleMusic(bool enabled)
    {
        _musicEnabled = enabled;

        if (!_musicEnabled && _currentMusicStream.HasValue)
        {
            _audio.Stop(_currentMusicStream);
            _currentMusicStream = null;
        }
    }
}
