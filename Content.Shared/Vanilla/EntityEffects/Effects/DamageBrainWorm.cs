using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Vanilla.Entities.BrainWorm;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vanilla.EntityEffects.Effects;

/// <summary>
/// Наносит урон мозговому червю в теле носителя
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DamageBrainWormEntityEffectSystem : EntityEffectSystem<MetaDataComponent, DamageBrainWorm>
{
    [Dependency] private readonly DamageableSystem _dmg = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<DamageBrainWorm> args)
    {
        if (TryComp<BrainWormHostComponent>(entity, out var hostcomp))
        {
            DamageSpecifier dmg = new()
            {
                DamageDict = new()
                {
                    { "Poison", args.Effect.Amount }
                }
            };
            _dmg.TryChangeDamage(
                hostcomp.HostedBrainWorm,
                dmg);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DamageBrainWorm : EntityEffectBase<DamageBrainWorm>
{
    /// <summary>
    /// Сколько урона наносим червю.
    /// </summary>
    [DataField("amount")]
    public float Amount = 0.5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-brainworm-damage", ("amount", Amount));
}
