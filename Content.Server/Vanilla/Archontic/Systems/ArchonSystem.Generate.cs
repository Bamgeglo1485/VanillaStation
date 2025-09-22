using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Archontic.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Vanilla.Warmer;
using Content.Shared.Weapons.Melee;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.StoryGen;
using Content.Shared.Damage;
using Content.Shared.Paper;
using Content.Shared.Atmos;

using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

using Content.Server.Spawners.Components;
using Content.Server.Destructible;

using System.Linq;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{

    [Dependency] private readonly StoryGeneratorSystem _storyGen = default!;

    /// <summary>
    /// Базовые действия при появлении Архонта
    /// </summary>
    private void OnMapInit(Entity<ArchonGenerateComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        if (comp.GenerateComponents == false)
            return;

        var dataComp = EnsureComp<ArchonDataComponent>(ent);

        GenerateArchon(ent, dataComp);

    }

    /// <summary>
    /// Генерирует сам архонт
    /// </summary>
    public void GenerateArchon(Entity<ArchonGenerateComponent> ent, ArchonDataComponent dataComp)
    {
        var comp = ent.Comp;

        // Ищем прототип ядра
        if (!_prototypeManager.TryIndex<ArchonCorePrototype>(comp.Core, out var core))
            return;

        // Устанавливаем переменные
        dataComp.Humanoid = core.Humanoid;
        dataComp.Tags = core.Tags.Select(t => t.Tag).ToList();

        // Устанавливаем тип архонта
        if (comp.RandomType)
        {
            SetRandomArchonType(ent, dataComp, core.Types);
        }

        // Добавляем компоненты
        foreach (var tag in core.Tags)
        {
            SetComponents(ent, dataComp, comp, core, tag);
        }

        // Устанавливаем прочность архонта
        if (TryComp<ArchonHealthComponent>(ent, out var health) && core.RandomDestructibility)
            SetDestructibility((ent, health), dataComp);

        // Урон
        if (core.RandomDamage)
            SetRandomDamage(ent, dataComp);

        // Скорость
        if (core.RandomMovementSpeed)
            SetRandomMovementSpeed(ent, dataComp);

        // Рандомизируем переменные некоторых компонентов
        if (TryComp<RandomizeArchonComponentsComponent>(ent, out _))
            RandomizeComponent(ent, dataComp);

        // Делаем рандомное описание
        SetDescription((ent, dataComp), core);

        // На основе побега и опасности устанавливаем класс
        SetArchonClass(dataComp);

        Dirty(ent, dataComp);
    }

    /// <summary>
    /// Добавляет компоненты, которые соответствуют типам архонта
    /// </summary>
    public void SetComponents(EntityUid ent, ArchonDataComponent dataComp, ArchonGenerateComponent comp,
                             ArchonCorePrototype core, ArchonTag tag)
    {
        var validPrototypes = GetArchonPrototypes(ent, dataComp, tag.Tag);
        if (validPrototypes.Count == 0)
            return;

        var count = _random.Next(tag.Min, tag.Max + 1);
        for (var i = 0; i < count; i++)
        {
            var availablePrototypes = validPrototypes
                .Where(p => !comp.AddedComponents.Contains(p.ID))
                .ToList();

            if (availablePrototypes.Count == 0)
                break;

            var proto = _random.Pick(availablePrototypes);

            int addingDanger = proto.Danger;
            int addingEscape = proto.Escape;
            int additiveDanger = 0;
            int additiveEscape = 0;

            // Сначала добавляем основные компоненты
            foreach (var (compType, component) in proto.Components)
            {
                EntityManager.AddComponent(ent, component, overwrite: true);
            }

            // Затем проверяем и добавляем ChancedComponents
            foreach (var chancedComponent in proto.ChancedComponents)
            {
                if (_random.Prob(chancedComponent.Chance))
                {
                    foreach (var (compType, component) in chancedComponent.Components)
                    {
                        EntityManager.AddComponent(ent, component, overwrite: true);
                    }

                    addingDanger = chancedComponent.Danger;
                    addingEscape = chancedComponent.Escape;
                    additiveDanger += chancedComponent.AdditiveDanger;
                    additiveEscape += chancedComponent.AdditiveEscape;

                    break;
                }
            }

            dataComp.Danger += addingDanger + additiveDanger;
            dataComp.Escape += addingEscape + additiveEscape;

            comp.AddedComponents.Add(proto.ID);
            comp.AddedPrototypes.Add(proto);
        }
    }

    /// <summary>
    /// Получает компоненты, который соответствуют типам архонта
    /// </summary>
    public List<ArchonComponentPrototype> GetArchonPrototypes(EntityUid ent, ArchonDataComponent dataComp, string tag)
    {
        var prototypes = new List<ArchonComponentPrototype>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<ArchonComponentPrototype>())
        {
            // Соответствует ли тэг прототипа целевому тэгу
            if (!proto.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                continue;

            // Если архонт гуманоидный и стоит блеклист на них, то пропускаем, либо наоборот
            if ((dataComp.Humanoid && proto.HumanoidBlacklist) ||
                (!dataComp.Humanoid && proto.HumanoidWhitelist))
                continue;

            if (proto.Types.Count > 0)
            {
                if (dataComp.Types.Count == 0 || !dataComp.Types.Any(archonType => proto.Types.Contains(archonType)))
                    continue;
            }

            prototypes.Add(proto);
        }

        return prototypes;
    }

    /// <summary>
    /// Устанавливает класс Архонта на основе уровня опасности и сбегаемости
    /// </summary>
    public void SetArchonClass(ArchonDataComponent dataComp)
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
    public void SetRandomArchonType(Entity<ArchonGenerateComponent> ent, ArchonDataComponent dataComp, List<ArchonType> coreTypes)
    {
        var comp = ent.Comp;

        if (coreTypes == null || coreTypes.Count == 0)
            coreTypes = Enum.GetValues<ArchonType>().ToList();

        var count = Math.Min(_random.Next(comp.MinTypes, comp.MaxTypes + 1), coreTypes.Count);

        dataComp.Types = coreTypes
            .OrderBy(x => _random.Next()) 
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Даёт архонту случайный урон, скорость удара, которая зависит от показателя опасности
    /// </summary>
    public void SetRandomDamage(Entity<ArchonGenerateComponent> ent, ArchonDataComponent dataComp)
    {
        float modifier;

        if (dataComp.Danger != 0)
        {
            modifier = dataComp.Danger / dataComp.DangerLimit;
            modifier = MathHelper.Clamp(modifier, 0.5f, 1.5f);
        }
        else
        {
            modifier = 0.5f;
        }

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
    public void SetRandomMovementSpeed(Entity<ArchonGenerateComponent> ent, ArchonDataComponent dataComp)
    {
        float modifier;

        if (dataComp.Escape != 0)
        {
            modifier = dataComp.Escape / dataComp.EscapeLimit;
            modifier = MathHelper.Clamp(modifier, 0.5f, 1.5f);
        }
        else
            modifier = 0.5f;

        _movement.ChangeBaseSpeed(
            ent,
            baseWalkSpeed: _random.NextFloat(1.5f, 2.0f) * modifier,
            baseSprintSpeed: _random.NextFloat(2.0f, 10.0f) * modifier,
            acceleration: _random.NextFloat(2.0f, 4.0f));

        _movement.RefreshMovementSpeedModifiers(ent);
    }

    /// <summary>
    /// Устанавливает описание Архонта
    /// </summary>
    public void SetDescription(Entity<ArchonDataComponent> ent, ArchonCorePrototype core)
    {
        if (!_storyGen.TryGenerateStoryFromTemplate(core.Template, out var story))
            return;

        var meta = MetaData(ent);

        _metaData.SetEntityDescription(ent, story, meta);
    }

    /// <summary>
    /// Даёт архонту случайную прочность
    /// </summary>
    public void SetDestructibility(Entity<ArchonHealthComponent> ent, ArchonDataComponent dataComp)
    {
        var comp = ent.Comp;

        if (comp.RandomDestructibility)
        {
            var list = Enum.GetValues<ArchonDestructibility>().ToList();

            var destructibility = _random.Pick(list);
            dataComp.Destructibility = destructibility;
        }

        float health = dataComp.Destructibility switch
        {
            ArchonDestructibility.Normal => _random.NextFloat(100f, 200f),
            ArchonDestructibility.Hard => _random.NextFloat(300f, 500f),
            ArchonDestructibility.Invincible => _random.NextFloat(1000f, 1500f),
            ArchonDestructibility.Rebirth => _random.NextFloat(50f, 300f),
            _ => _random.NextFloat(100f, 200f),
        };

        comp.Health = health;

        RemComp<DestructibleComponent>(ent);
        EnsureComp<DamageableComponent>(ent);

        if (dataComp.Humanoid)
        {
            float stamina = dataComp.Destructibility switch
            {
                // Стамина маленькая для Secure Contain Protect
                ArchonDestructibility.Normal => _random.NextFloat(20f, 50f),
                ArchonDestructibility.Hard => _random.NextFloat(50f, 100f),
                ArchonDestructibility.Invincible => _random.NextFloat(150f, 200f),
                ArchonDestructibility.Rebirth => _random.NextFloat(50f, 150f),
                _ => _random.NextFloat(20f, 50f),
            };

            var staminaComp = new StaminaComponent { CritThreshold = stamina };
            EntityManager.AddComponent(ent, staminaComp, overwrite: true);
        }
    }

    /// <summary>
    /// Рандомизация некоторых компонентов
    /// </summary>
    public void RandomizeComponent(Entity<ArchonGenerateComponent> ent, ArchonDataComponent dataComp)
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
