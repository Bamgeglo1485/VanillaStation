using Content.Shared.Vanilla.Archon.ShyGuy;
using Content.Shared.Movement.Systems;
using Content.Shared.Jittering;
using Content.Shared.Damage;
using Content.Shared.Audio;
using Content.Shared.CombatMode.Pacification;

using Robust.Shared.Timing;
using Robust.Shared.Map;

using Robust.Server.GameObjects;

using System.Numerics;

namespace Content.Server.Vanilla.Archon.ShyGuy;

public sealed class ShyGuySystem : SharedShyGuySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShyGuyComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeNetworkEvent<ShyGuyGazeEvent>(OnGaze);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        if (curTime < _nextUpdate)
            return;

        _nextUpdate = curTime + TimeSpan.FromSeconds(1);

        var query = EntityQueryEnumerator<ShyGuyComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (curTime >= comp.TargetChaseEnd)
                SetCalm(uid, comp);

            if (comp.State == ShyGuyState.Preparing && curTime >= comp.PreparingEnd)
                SetRage(uid, comp);
        }
    }
    
    private void OnGaze(ShyGuyGazeEvent ev)
    {
        var shyGuy = GetEntity(ev.ShyGuy);
        var user = GetEntity(ev.User);

        if (!TryComp<ShyGuyComponent>(shyGuy, out var comp))
            return;

        //клиенту доверяй но проверяй
        if (!IsReachable(shyGuy, user, comp))
            return;

        comp.Targets.Add(user);
        Dirty(shyGuy, comp);
        SetPreparing(shyGuy, comp);
        var baseTime = comp.PreparingEnd > _timing.CurTime ? comp.PreparingEnd : _timing.CurTime;
        comp.TargetChaseEnd = baseTime + comp.OneTargetChaseTime;
    }

    public void SetPreparing(EntityUid uid, ShyGuyComponent comp)
    {
        if (comp.State != ShyGuyState.Calm)
            return;

        comp.PreparingEnd = _timing.CurTime + comp.PreparingTime;
        comp.State = ShyGuyState.Preparing;

        _jitter.AddJitter(uid, 20, 20);
        _ambient.SetAmbience(uid, false);
        _audio.PlayPvs(comp.PreparingSound, uid);
    }

    public void SetCalm(EntityUid uid, ShyGuyComponent comp)
    {
        if (comp.State == ShyGuyState.Calm)
            return;
        EnsureComp<PacifiedComponent>(uid);
        comp.State = ShyGuyState.Calm;
        comp.PreparingEnd = TimeSpan.Zero;
        comp.TargetChaseEnd = _timing.CurTime;

        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        RemCompDeferred<JitteringComponent>(uid);
        comp.Targets.Clear();

        if (comp.CalmAmbient != null)
        {
            _ambient.SetSound(uid, comp.CalmAmbient);
            _ambient.SetAmbience(uid, true);
        }
    }

    public void SetRage(EntityUid uid, ShyGuyComponent comp)
    {
        if (comp.State == ShyGuyState.Rage)
            return;

        _jitter.AddJitter(uid, 10, 10);

        comp.State = ShyGuyState.Rage;
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        RemComp<PacifiedComponent>(uid);
        if (comp.RageAmbient != null)
        {
            _ambient.SetSound(uid, comp.RageAmbient);
            _ambient.SetAmbience(uid, true);
        }
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ShyGuyComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.State != ShyGuyState.Rage)
            return;

        args.ModifySpeed(component.WalkModifier, component.SprintModifier);
    }
}
