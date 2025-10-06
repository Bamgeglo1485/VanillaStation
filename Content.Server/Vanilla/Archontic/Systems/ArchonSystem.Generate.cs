using Content.Shared.Archontic.Components;
using Content.Shared.Archontic.Prototypes;
using Content.Shared.Movement.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Vanilla.Warmer;
using Content.Shared.Weapons.Melee;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.FixedPoint;
using Content.Shared.StoryGen;
using Content.Shared.Damage;
using Content.Shared.Paper;
using Content.Shared.Atmos;
using Content.Shared.Mobs;

using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

using Content.Server.Spawners.Components;
using Content.Server.Destructible;
using Content.Server.Antag;
using Content.Server.Roles;
using System.Linq;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{

    [Dependency] private readonly StoryGeneratorSystem _storyGen = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    private int EscapeLimit = 8;
    private int DangerLimit = 8;

    /// <summary>
    /// Базовые действия при появлении Архонта
    /// </summary>
    private void OnMapInit(Entity<ArchonGenerateComponent> ent, ref MapInitEvent args)
    {
        var dataComp = EnsureComp<ArchonDataComponent>(ent);

        GenerateArchon(ent, dataComp);
    }

    /// <summary>
    /// Генерирует сам архонт
    /// </summary>
    public void GenerateArchon(Entity<ArchonGenerateComponent> ent, ArchonDataComponent dataComp)
    {
        var comp = ent.Comp;

        if (comp.RandomType)
        {
            SetRandomArchonType(ent, dataComp);
        }

        if (TryComp<MobThresholdsComponent>(ent, out var thresComp) && comp.RandomHealth)
            SetRandomHealth((ent, thresComp));

        if (comp.RandomDamage)
            SetRandomDamage(ent);

        if (comp.RandomMovementSpeed)
            SetRandomMovementSpeed(ent);

        foreach (var tag in comp.AddingTags)
            SetFeatures(ent, dataComp, comp, tag, false);

        foreach (var secretTag in comp.SecretTags)
            SetFeatures(ent, dataComp, comp, secretTag, true);

        SetRandomDescription(ent);

        SetArchonClass(dataComp);

        Dirty(ent, dataComp);
    }

    /// <summary>
    /// Добавляет свойства по тэгам
    /// </summary>
    public void SetFeatures(EntityUid ent, ArchonDataComponent dataComp, ArchonGenerateComponent comp, string addingTag, bool secret)
    {
        var validPrototypes = GetArchonPrototypes(ent, dataComp, comp, addingTag);
        if (validPrototypes.Count == 0)
            return;

        var count = _random.Next(comp.MinComponents, comp.MaxComponents + 1);
        for (var i = 0; i < count; i++)
        {
            var availablePrototypes = validPrototypes
                .Where(p => !comp.AddedPrototypes.Any(added => added.ID == p.ID))
                .ToList();

            if (availablePrototypes.Count == 0)
                break;

            var proto = _random.Pick(availablePrototypes);

            int totalDanger = proto.Danger;
            int totalEscape = proto.Escape;

            AddComponents(ent, dataComp, secret, proto.Components, proto.Danger, proto.Escape);

            foreach (var chancedComponent in proto.ChancedComponents)
            {
                if (_random.Prob(chancedComponent.Chance))
                {
                    AddComponents(ent, dataComp, secret, chancedComponent.Components, chancedComponent.Danger, chancedComponent.Escape);

                    totalDanger += chancedComponent.Danger + chancedComponent.AdditiveDanger;
                    totalEscape += chancedComponent.Escape + chancedComponent.AdditiveEscape;
                    break;
                }
            }

            dataComp.Danger += totalDanger;
            dataComp.Escape += totalEscape;

            comp.AddedPrototypes.Add(proto);
        }
    }

    /// <summary>
    /// Добавляет компоненты по тэгам
    /// </summary>
    public void AddComponents(EntityUid ent, ArchonDataComponent dataComp, bool secret, ComponentRegistry components, int danger, int escape)
    {
        if (!secret)
        {
            EntityManager.AddComponents(ent, components);

            dataComp.Danger += danger;
            dataComp.Escape += escape;
        }
        else if (TryComp<ArchonComponent>(ent, out var archonComp))
            archonComp.SecretFeatures.Add(new SecretFeatures(components, _random.Next(3, 8), danger, escape, false));
    }

    /// <summary>
    /// Получает прототипы компонентов, которые подходят для архонта и содержат указанный тег
    /// </summary>
    public List<ArchonComponentPrototype> GetArchonPrototypes(EntityUid ent, ArchonDataComponent dataComp, ArchonGenerateComponent genComp, string addingTag)
    {
        var prototypes = new List<ArchonComponentPrototype>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<ArchonComponentPrototype>())
        {
            if (!CheckTagsCompatibility(proto.Tags, genComp.Tags, proto.RequireAllTags, addingTag))
                continue;

            if (proto.Types.Count > 0 && !proto.Types.Any(type => dataComp.Types.Contains(type)))
                continue;

            prototypes.Add(proto);
        }

        return prototypes;
    }

    /// <summary>
    /// Проверяет совместимость тегов прототипа с тегами архонта
    /// </summary>
    private bool CheckTagsCompatibility(List<string> protoTags, List<string> archonTags, bool requireAll, string addingTag)
    {
        if (!protoTags.Contains(addingTag))
            return false;

        var tagsToCheck = protoTags.Where(t => t != addingTag).ToList();

        if (tagsToCheck.Count == 0)
            return true;

        if (requireAll)
        {
            return tagsToCheck.All(tag => archonTags.Contains(tag));
        }
        else
        {
            return tagsToCheck.Any(tag => archonTags.Contains(tag));
        }
    }

    /// <summary>
    /// Устанавливает класс Архонта на основе уровня опасности и сбегаемости
    /// </summary>
    public void SetArchonClass(ArchonDataComponent dataComp)
    {
        var danger = dataComp.Danger;
        var escape = dataComp.Escape;

        if (danger >= DangerLimit && escape >= EscapeLimit)
        {
            dataComp.Class = ArchonClass.Thaumiel;
        }
        else if (escape >= EscapeLimit)
        {
            dataComp.Class = ArchonClass.Keter;
        }
        else if (danger >= DangerLimit)
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
    public void SetRandomArchonType(Entity<ArchonGenerateComponent> ent, ArchonDataComponent dataComp)
    {
        var comp = ent.Comp;
        var types = Enum.GetValues<ArchonType>().ToList();

        var count = Math.Min(_random.Next(comp.MinTypes, comp.MaxTypes + 1), types.Count);

        dataComp.Types = types
            .OrderBy(x => _random.Next())
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Даёт архонту случайный урон, скорость удара, которая зависит от показателя опасности
    /// </summary>
    public void SetRandomDamage(Entity<ArchonGenerateComponent> ent)
    {
        var meleeComponent = new MeleeWeaponComponent
        {
            Damage = new DamageSpecifier(),
            AttackRate = _random.NextFloat(0.8f, 1.5f)
        };

        // Добавил урон радиацией, асфиксией и кровотечением, ибо почему бы нет, достаточно аномально
        var damageTypes = new[] { "Blunt", "Slash", "Piercing", "Radiation", "Burn", "Caustic", "Cold", "Asphyxiation", "Bloodloss" };
        var damageType = _random.Pick(damageTypes);

        meleeComponent.Damage.DamageDict[damageType] = (int)(_random.Next(10, 35));

        EntityManager.AddComponent(ent, meleeComponent, overwrite: true);
    }

    /// <summary>
    /// Даёт архонту случайную скорость, которая зависит от показателя сбегаемости
    /// </summary>
    public void SetRandomMovementSpeed(Entity<ArchonGenerateComponent> ent)
    {

        _movement.ChangeBaseSpeed(
            ent,
            baseWalkSpeed: _random.NextFloat(1.5f, 2.0f),
            baseSprintSpeed: _random.NextFloat(2.0f, 10.0f),
            acceleration: _random.NextFloat(2.0f, 4.0f));

        _movement.RefreshMovementSpeedModifiers(ent);
    }

    /// <summary>
    /// Устанавливает описание Архонта
    /// </summary>
    public void SetRandomDescription(Entity<ArchonGenerateComponent> ent)
    {
        if (!_storyGen.TryGenerateStoryFromTemplate(ent.Comp.Template, out var story))
            return;

        var meta = MetaData(ent);

        _metaData.SetEntityDescription(ent, story, meta);
    }

    public void GenerateBriefing(Entity<ArchonRoleComponent> ent, ref ComponentStartup args)
    {
        if (!_storyGen.TryGenerateStoryFromTemplate(ent.Comp.Template, out var story) || ent.Comp.Briefing != null)
            return;

        _antag.SendBriefing(ent, story, null, null);
        ent.Comp.Briefing = story;
    }

    private void OnGetBriefing(EntityUid uid, ArchonRoleComponent comp, ref GetBriefingEvent args)
    {
        if (comp.Briefing != null)
            args.Append(comp.Briefing);
    }

    /// <summary>
    /// Даёт архонту случайную прочность и стамину
    /// </summary>
    public void SetRandomHealth(Entity<MobThresholdsComponent> ent)
    {
        var comp = ent.Comp;

        int health = _random.Next(50, 600);
        int dangerHealth = (health / 600) * 3;

        if (TryComp<ArchonDataComponent>(ent, out var dataComp))
            dataComp.Danger += dangerHealth;

        _mobThresSystem.SetMobStateThreshold(ent, FixedPoint2.New(health), MobState.Dead,
        ent.Comp);

        RemComp<DestructibleComponent>(ent);
        EnsureComp<DamageableComponent>(ent);

        if (TryComp<StaminaComponent>(ent, out var staminaComp))
        {
            int stamina = _random.Next(20, 250);
            int dangerStamina = (stamina / 250) * 3;

            if (TryComp<ArchonDataComponent>(ent, out var dataComp2))
                dataComp2.Danger += dangerStamina;

            staminaComp.CritThreshold = stamina;
        }
    }
}
