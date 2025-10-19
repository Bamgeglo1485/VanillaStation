using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Vanilla.Archon.ShyGuy;

public abstract class SharedShyGuySystem : EntitySystem
{

    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    
    public override void Initialize()
    {
        base.Initialize();
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }
}
