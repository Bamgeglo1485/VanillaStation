using Content.Shared.Archontic.Components;
using Content.Shared.Vanilla.Warmer;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Audio.Systems;
using Content.Server.Destructible;
using Content.Server.Spawners.Components;
using System.Linq;
using Content.Shared.Atmos;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArchonComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<ArchonComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Базовые действия при появлении Архонта
    /// </summary>
    private void OnMapInit(Entity<ArchonComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        if (comp.GenerateComponents == false)
            return;

        var dataComp = EntityManager.EnsureComponent<ArchonDataComponent>(ent);

        if (_random.Prob(comp.CanBeHumanoidChance))
        {

            dataComp.Humanoid = true;

            SetComponents(ent, dataComp, "AI", 1, 1);
            SetComponents(ent, dataComp, "Damage", 0, 1);
            SetComponents(ent, dataComp, "Movement", 0, 1);

            SetRandomDamage(ent, dataComp);
            SetRandomMovementSpeed(ent, dataComp);
        }

        GenerateArchon(ent);

    }

    /// <summary>
    /// Генерирует сам архонт
    /// </summary>
    private void GenerateArchon(Entity<ArchonComponent> ent)
    {
        var comp = ent.Comp;
        var dataComp = EntityManager.EnsureComponent<ArchonDataComponent>(ent);

        if (comp.RandomType)
            SetRandomArchonType(ent, dataComp);

        SetComponents(ent, dataComp, "Generic", comp.MinComponents, comp.MaxComponents);
        SetArchonClass(ent, dataComp);
        SetDestructibility(ent, dataComp);

        if (TryComp<RandomizeArchonComponentsComponent>(ent, out _))
            RandomizeComponent(ent, dataComp);
    }

    /// <summary>
    /// Добавляет компоненты, которые соответствуют типам архонта
    /// </summary>
    private void SetComponents(EntityUid ent, ArchonDataComponent dataComp, string tag, int minCount, int maxCount)
    {
        var validPrototypes = GetArchonPrototypes(ent, dataComp, tag);
        if (validPrototypes.Count == 0)
            return;

        var count = _random.Next(minCount, maxCount + 1);
        for (var i = 0; i < count; i++)
        {
            var availablePrototypes = validPrototypes
                .Where(p => !dataComp.AddedComponents.Contains(p.ID))
                .ToList();

            if (availablePrototypes.Count == 0)
                break;

            var proto = _random.Pick(availablePrototypes);

            // Сначала добавляем основные компоненты
            foreach (var (compType, component) in proto.Components)
            {
                EntityManager.AddComponent(ent, component, overwrite: true);
            }

            dataComp.Danger += proto.Danger;
            dataComp.Escape += proto.Escape;

            // Затем проверяем и добавляем ChancedComponents
            foreach (var chancedComponent in proto.ChancedComponents)
            {
                if (_random.Prob(chancedComponent.Chance))
                {
                    foreach (var (compType, component) in chancedComponent.Components)
                    {
                        EntityManager.AddComponent(ent, component, overwrite: true);
                    }

                    dataComp.Danger += chancedComponent.Danger;
                    dataComp.Escape += chancedComponent.Escape;
                    break;
                }
            }

            dataComp.AddedComponents.Add(proto.ID);
        }
    }

    /// <summary>
    /// Получает компоненты, который соответствуют типам архонта
    /// </summary>
    private List<ArchonComponentPrototype> GetArchonPrototypes(EntityUid ent, ArchonDataComponent dataComp, string tag)
    {
        var prototypes = new List<ArchonComponentPrototype>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<ArchonComponentPrototype>())
        {
            if (!proto.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                continue;

            if (dataComp.Humanoid && proto.HumanoidBlacklist)
                continue;

            if (dataComp.Humanoid == false && proto.HumanoidWhitelist)
                continue;

            if (proto.Types.Count == 0)
            {
                prototypes.Add(proto);
                continue;
            }

            if (dataComp.Types.Count == 0)
                continue;

            var hasCommonType = false;
            foreach (var archonType in dataComp.Types)
            {
                if (proto.Types.Contains(archonType))
                {
                    hasCommonType = true;
                    break;
                }
            }

            if (hasCommonType)
            {
                prototypes.Add(proto);
            }
        }

        return prototypes;
    }

    /// <summary>
    /// Устанавливает класс Архонта на основе уровня опасности и сбегаемости
    /// </summary>
    private void SetArchonClass(Entity<ArchonComponent> ent, ArchonDataComponent dataComp)
    {
        var danger = dataComp.Danger;
        var escape = dataComp.Escape;

        if (danger >= dataComp.DangerLimit && escape >= dataComp.EscapeLimit)
        {
            dataComp.Class = ArchonClass.Thaumiel;
        }
        else if (escape >= dataComp.EscapeLimit)
        {
            dataComp.Class = ArchonClass.Keter;
        }
        else if (danger >= dataComp.DangerLimit)
        {
            dataComp.Class = ArchonClass.Euclid;
        }
        else
        {
            dataComp.Class = ArchonClass.Safe;
        }
    }

    /// <summary>
    /// Устанавливает случайные типы Архонта
    /// </summary>
    private void SetRandomArchonType(Entity<ArchonComponent> ent, ArchonDataComponent dataComp)
    {
        var comp = ent.Comp;

        var types = new List<ArchonType>();
        var archonTypes = System.Enum.GetValues(typeof(ArchonType));
        for (var i = 0; i < archonTypes.Length; i++)
        {
            types.Add((ArchonType)archonTypes.GetValue(i)!);
        }

        var typeCount = _random.Next(comp.MinTypes, comp.MaxTypes + 1);

        dataComp.Types = new List<ArchonType>();

        for (var i = 0; i < typeCount; i++)
        {
            if (types.Count == 0)
                break;

            var random = _random.Next(types.Count);
            var randomType = types[random];

            dataComp.Types.Add(randomType);
            types.RemoveAt(random);
        }
    }

    /// <summary>
    /// Даёт архонту случайный урон, скорость удара, которая зависит от показателя опасности
    /// </summary>
    private void SetRandomDamage(Entity<ArchonComponent> ent, ArchonDataComponent dataComp)
    {
        float modifier;

        if (dataComp.Danger != 0)
        {
            modifier = dataComp.Danger / dataComp.DangerLimit;
            modifier = MathHelper.Clamp(modifier, 0.3f, 2.0f);
        }
        else
            modifier = 0.3f;

        var meleeComponent = new MeleeWeaponComponent
        {
            Damage = new DamageSpecifier(),
            AttackRate = _random.NextFloat(0.8f, 1.5f) * modifier
        };

        // Добавил урон радиацией, асфиксией и кровотечением, ибо почему бы нет, достаточно аномально
        var damageTypes = new[] { "Blunt", "Slash", "Piercing", "Radiation", "Burn", "Caustic", "Cold", "Asphyxiation", "Bloodloss" };
        var damageType = _random.Pick(damageTypes);

        meleeComponent.Damage.DamageDict[damageType] = (int)(_random.Next(10, 35) * modifier);

        EntityManager.AddComponent(ent, meleeComponent, overwrite: true);
    }

    /// <summary>
    /// Даёт архонту случайную скорость, которая зависит от показателя сбегаемости
    /// </summary>
    private void SetRandomMovementSpeed(Entity<ArchonComponent> ent, ArchonDataComponent dataComp)
    {
        float modifier;

        if (dataComp.Escape != 0)
        {
            modifier = dataComp.Escape / dataComp.EscapeLimit;
            modifier = MathHelper.Clamp(modifier, 0.5f, 2.0f);
        }
        else
            modifier = 0.5f;

        _movement.ChangeBaseSpeed(
            ent,
            baseWalkSpeed: _random.NextFloat(1.5f, 2.0f) * modifier,
            baseSprintSpeed: _random.NextFloat(2.0f, 10.0f) * modifier,
            acceleration: _random.NextFloat(2.0f, 4.0f)
        );

        _movement.RefreshMovementSpeedModifiers(ent);
    }

    /// <summary>
    /// Даёт архонту случайную прочность
    /// </summary>
    private void SetDestructibility(Entity<ArchonComponent> ent, ArchonDataComponent dataComp)
    {
        var comp = ent.Comp;

        if (comp.RandomDestructibility)
        {
            var list = new List<ArchonDestructibility>
            {
                ArchonDestructibility.Normal,
                ArchonDestructibility.Hard,
                ArchonDestructibility.Invincible,
                ArchonDestructibility.Rebirth
            };

            var destructibility = _random.Pick(list);
            dataComp.Destructibility = destructibility;
        }

        float health = dataComp.Destructibility switch
        {
            ArchonDestructibility.Normal => _random.NextFloat(100f, 200f),
            ArchonDestructibility.Hard => _random.NextFloat(300f, 500f),
            ArchonDestructibility.Invincible => _random.NextFloat(1000f, 1500f),
            ArchonDestructibility.Rebirth => _random.NextFloat(50f, 300f),
            _ => 100f
        };

        comp.Health = health;

        RemComp<DestructibleComponent>(ent);
        EnsureComp<DamageableComponent>(ent);

        if (TryComp<MobMoverComponent>(ent, out _))
        {
            float stamina = dataComp.Destructibility switch
            {
                // Стамина маленькая для Secure Contain Protect
                ArchonDestructibility.Normal => _random.NextFloat(20f, 50f),
                ArchonDestructibility.Hard => _random.NextFloat(50f, 100f),
                ArchonDestructibility.Invincible => _random.NextFloat(150f, 200f),
                ArchonDestructibility.Rebirth => _random.NextFloat(50f, 150f),
                _ => 100f
            };

            var staminaComp = new StaminaComponent { CritThreshold = stamina };
            EntityManager.AddComponent(ent, staminaComp, overwrite: true);
        }
    }

    /// <summary>
    /// Действия при смерти Архонта, чучут щиткод
    /// </summary>
    private void OnDamage(Entity<ArchonComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } delta)
            return;

        var totalDamage = delta.GetTotal();
        var comp = ent.Comp;

        if (totalDamage < comp.Health)
            return;

        var dataComp = EntityManager.EnsureComponent<ArchonDataComponent>(ent);

        if (dataComp.Destructibility == ArchonDestructibility.Rebirth)
        {
            SetDestructibility(ent, dataComp);
            SetComponents(ent, dataComp, "Generic", 2, 4);
        }
        else
        {
            QueueDel(ent);
        }
    }

    /// <summary>
    /// Рандомизация некоторых компонентов
    /// </summary>
    private void RandomizeComponent(Entity<ArchonComponent> ent, ArchonDataComponent dataComp)
    {
        if (TryComp<TimedSpawnerComponent>(ent, out var spawner))
        {
            spawner.Chance = _random.NextFloat(0.5f, 1.0f);
            spawner.IntervalSeconds = TimeSpan.FromSeconds(_random.NextFloat(90f, 340f));
            spawner.MinimumEntitiesSpawned = _random.Next(1, 4);
            spawner.MaximumEntitiesSpawned = spawner.MinimumEntitiesSpawned + _random.Next(1, 4);
        }

        if (TryComp<WarmerComponent>(ent, out var warmer))
        {
            var gasList = new[]
            {
            Gas.Oxygen,
            Gas.Nitrogen,
            Gas.Ammonia,
            Gas.NitrousOxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
            Gas.CarbonDioxide
        };

            var gas = _random.Pick(gasList);

            if (gas == Gas.NitrousOxide)
                dataComp.Escape += 3;

            warmer.TileHeatStrength = _random.NextFloat(0f, 40f);
            warmer.HeatMaxTemp = _random.NextFloat(213f, 433f);
            warmer.MoleRatio = _random.NextFloat(0f, 1f);
            warmer.GasType = gas;
        }
    }
}
