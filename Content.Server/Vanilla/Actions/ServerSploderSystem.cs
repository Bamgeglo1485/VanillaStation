using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Robust.Shared.Random;
using Content.Shared.Throwing;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Effects;
using Robust.Shared.Player;
using Content.Shared.Vanilla.Actions.Components;
using Content.Shared.Vanilla.Actions.Events;
using Content.Server.Explosion.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.Damage;
using Content.Shared.Item;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Jittering;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Vanilla.Actions;

public sealed class SploderSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly ExplosionSystem _boom = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SplodeMarkingEvent>(OnSplodeMarkingEvent); 
        SubscribeLocalEvent<SelfSplodingEvent>(OnSelfSploding);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SplodingComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            switch (comp.State)
            {
                case SplodingState.Standing:

                    _jitter.AddJitter(uid);

                    var meta = MetaData(uid);
                    _popup.PopupEntity($"{meta.EntityName} начинает переполняться энергией!", uid, PopupType.SmallCaution);

                    if (comp.SplodeSound != null)
                        _audioSystem.PlayPvs(comp.SplodeSound, uid);

                    comp.State = SplodingState.Charging;

                    break;

                case SplodingState.Charging:

                    if (currentTime >= comp.Timer)
                    {

                        comp.State = SplodingState.Exploding;
                        return;
                    }

                    if (currentTime < comp.NextUpdate)
                        return;

                    comp.NextUpdate = currentTime + comp.UpdateInterval;

                    var progress = Math.Clamp(
                        (float)(currentTime - comp.StartTime).TotalSeconds /
                        (float)(comp.Timer - comp.StartTime).TotalSeconds,
                        0f, 1f
                    );

                    EnsureComp<PointLightComponent>(uid);
                    _light.SetEnabled(uid, true);
                    _light.SetColor(uid, Color.Orange);
                    _light.SetRadius(uid, MathHelper.Lerp(1f, 4f, progress));
                    _light.SetEnergy(uid, MathHelper.Lerp(0.1f, 7f, progress));

                    _color.RaiseEffect(Color.Orange, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));

                    break;

                case SplodingState.Exploding:

                    _boom.QueueExplosion(
                        uid,
                        comp.ExplosionType,
                        comp.ExplodeIntensity * comp.Modifier,
                        comp.ExplodeDropoff * comp.Modifier,
                        comp.ExplodeMaxIntensity * comp.Modifier);

                    if (comp.Gib)
                    {
                        var damageSpec = new DamageSpecifier
                        {
                            DamageDict = new()
                            {
                                ["Blunt"] = FixedPoint2.New(1000)
                            }
                        };
                        _damageable.TryChangeDamage(uid, damageSpec, true);
                    }

                    QueueDel(uid);

                    break;
            }
        }
    }

    private void OnSplodeMarkingEvent(SplodeMarkingEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;

        args.Handled = true;

        if (!_handsSystem.TryGetActiveItem(uid, out var activeItem))
        {
            _popup.PopupEntity("Ваши руки потрескивают, но вы ничего не держите", uid, uid);
            return;
        }

        if (!TryComp<ItemComponent>(activeItem, out var item) || (item.Size == "Huge" || item.Size == "Ginormous"))
        {
            _popup.PopupEntity("Объект слишком большой!", uid, uid);
            return;
        }

        if (HasComp<SplodingComponent>(activeItem))
        {
            _popup.PopupEntity("Этот предмет уже заряжен", uid, uid);
            return;
        }

        float modifier = 1f;

        if (item.Size == "Tiny")
        {
            modifier = 0.3f;
        }
        else if (item.Size == "Small")
        {
            modifier = 0.6f;
        }
        else if (item.Size == "Large")
        {
            modifier = 1.5f;
        }

        var splodeComp = EnsureComp<SplodingComponent>(activeItem.Value);
        splodeComp.StartTime = _gameTiming.CurTime; 
        splodeComp.Timer += splodeComp.StartTime;
        splodeComp.Modifier = modifier;

    }

    private void OnSelfSploding(SelfSplodingEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;

        if (!TryComp<DamageableComponent>(uid, out var damageable) ||
            !TryComp<MobStateComponent>(uid, out var mobState))
        {
            return;
        }

        if (mobState.CurrentState != MobState.Alive)
        {
            _popup.PopupEntity("Вы не можете коснуться скелета, будучи не в сознании", uid, uid);
            return;
        }

        var SlashDamage = damageable.Damage.DamageDict.GetValueOrDefault("Slash", FixedPoint2.Zero);
        var PiercingDamage = damageable.Damage.DamageDict.GetValueOrDefault("Piercing", FixedPoint2.Zero);
        var totalDamage = SlashDamage + PiercingDamage;

        if (totalDamage < FixedPoint2.New(20))
        {
            _popup.PopupEntity("Вы недостаточно ранены, чтобы коснуться своего скелета", uid, uid);
            return;
        }

        args.Handled = true;

        var splodeComp = EnsureComp<SplodingComponent>(uid);
        splodeComp.StartTime = _gameTiming.CurTime;
        splodeComp.Timer = splodeComp.StartTime + TimeSpan.FromSeconds(3);
        splodeComp.ExplodeIntensity = 600;
        splodeComp.ExplodeMaxIntensity = 600;
        splodeComp.ExplodeDropoff = 2;
        splodeComp.ExplosionType = "DemolitionCharge";
        splodeComp.Gib = true;
        splodeComp.SplodeSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Actions/SelfSplodeCharge.ogg");

    }
}
