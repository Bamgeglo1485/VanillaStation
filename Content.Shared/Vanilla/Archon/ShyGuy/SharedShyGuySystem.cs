using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Content.Shared.Examine;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Content.Shared.Mobs.Components;
using Content.Shared.Eye.Blinding.Components;
using System.Linq;

namespace Content.Shared.Vanilla.Archon.ShyGuy;

public abstract class SharedShyGuySystem : EntitySystem
{

    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }
    //true если в рейдже
    public bool IsRaged(EntityUid uid, ShyGuyComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.State == ShyGuyState.Rage;
    }

    //возвращает список всех посмотревших на скромника чувачков в радиусе
    public IEnumerable<EntityUid> GetNearbyObservers(Entity<ShyGuyComponent?> ent, float range)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return Array.Empty<EntityUid>();

        var nearby = _lookup
            .GetEntitiesInRange<MobStateComponent>(_xform.GetMapCoordinates(ent), range)
            .Select(e => e.Owner);

        return nearby.Where(victim => ent.Comp.Targets.Contains(victim));
    }

    protected bool IsReachable(EntityUid uid, EntityUid user, ShyGuyComponent comp)
    {
        if (user == uid)
            return false;

        if (comp.Targets.Contains(user))
            return false;

        if (!HasComp<MobStateComponent>(user))
            return false;

        if (TryComp<BlindableComponent>(user, out var blind) && blind.IsBlind)
            return false;

        if (!_mobStateSystem.IsAlive(user) || !_mobStateSystem.IsAlive(uid))
            return false;

        if (!_examine.InRangeUnOccluded(user, uid, 16f))
            return false;

        return true;
    }
}
