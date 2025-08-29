using Content.Shared.Archontic.Components;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();
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
            SetComponents(ent, dataComp, "AI", 1, 1);
            SetComponents(ent, dataComp, "Damage", 0, 1);
            SetComponents(ent, dataComp, "Movement", 0, 1); // щиткод да и всё равно

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
    }

    /// <summary>
    /// Добавляет компоненты, которые соответствуют типам архонта
    /// </summary>
    private void SetComponents(Entity<ArchonComponent> ent, ArchonDataComponent dataComp, string tag, int minCount, int maxCount)
    {
        var validPrototypes = GetArchonPrototypes(ent, dataComp, tag);
        if (validPrototypes.Count == 0)
            return;

        var count = _random.Next(minCount, maxCount + 1);
        for (var i = 0; i < count; i++)
        {
            var proto = _random.Pick(validPrototypes);
            var chance = proto.ChancedComponents.Count > 0 && _random.Prob(proto.ChancedComponentChance);

            if (chance)
            {
                foreach (var (compType, component) in proto.ChancedComponents)
                {
                    EntityManager.AddComponent(ent, component, overwrite: true);
                }

                dataComp.Danger += proto.ChancedDanger;
                dataComp.Escape += proto.ChancedEscape;

                if (proto.ChancedComponentReplaceMain)
                    continue;
            }

            if (!chance || !proto.ChancedComponentReplaceMain)
            {
                foreach (var (compType, component) in proto.Components)
                {
                    EntityManager.AddComponent(ent, component, overwrite: true);
                }

                dataComp.Danger += proto.Danger;
                dataComp.Escape += proto.Escape;
            }
        }
    }

    /// <summary>
    /// Получает компоненты, который соответствуют типам архонта
    /// </summary>
    private List<ArchonComponentPrototype> GetArchonPrototypes(Entity<ArchonComponent> ent, ArchonDataComponent dataComp, string tag)
    {
        var prototypes = new List<ArchonComponentPrototype>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<ArchonComponentPrototype>())
        {
            if (!proto.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                continue;

            var hasMatch = false;
            foreach (var archonType in dataComp.Types)
            {
                if (proto.Types.Contains(archonType))
                {
                    hasMatch = true;
                    break;
                }
            }

            if (hasMatch || dataComp.Types.Count == 0)
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

            var random= _random.Next(types.Count);
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
            AttackRate = _random.NextFloat(0.8f, 1.5f) * modifier,
            Range = _random.NextFloat(1.0f, 2.0f) * modifier
        };

        // Добавил урон радиацией, асфиксией и кровотечением, ибо почему бы нет, достаточно аномально
        var damageTypes = new[] { "Blunt", "Slash", "Piercing", "Radiation", "Burn", "Caustic", "Cold", "Asphyxiation", "Bloodloss",  }; 
        var damageType = _random.Pick(damageTypes);

        meleeComponent.Damage.DamageDict[damageType] = (int)(_random.Next(5, 25) * modifier);

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
}
