using Content.Shared.Archontic.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArchonComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ArchonComponent> ent, ref MapInitEvent args)
    {
        GenerateArchon(ent);
    }

    /// <summary>
    /// Метод генерирует сам архонт, добавляет компоненты, определяет живучесть и класс
    /// </summary>
    private void GenerateArchon(Entity<ArchonComponent> ent)
    {
        var comp = ent.Comp;

        // TODO Создание гуманоида или изменение физики

        // Добавление компонентов
        AddComponents(ent, "Generic", comp.MinComponents, comp.MaxComponents);

        // TODO живучесть

        // Устанавливает класс объекта
        SetArchonClass(ent);
    }

    /// <summary>
    /// Ищет прототипы по тегу и добавляет их компоненты
    /// </summary>
    private void AddComponents(Entity<ArchonComponent> ent, string tag, int minCount, int maxCount)
    {
        var validPrototypes = GetArchonPrototypesByTag(tag);
        if (validPrototypes.Count == 0)
            return;

        var count = _random.Next(minCount, maxCount + 1);

        for (var i = 0; i < count; i++)
        {
            var randomProto = _random.Pick(validPrototypes);
            var usedChanced = false;

            // Проверяем шанс компонентов
            if (_random.Prob(randomProto.ChancedComponentChance) && randomProto.ChancedComponents.Count > 0)
            {
                usedChanced = true;

                foreach (var (_, component) in randomProto.ChancedComponents)
                {
                    EntityManager.AddComponent(ent, component);
                }

                ent.Comp.Danger += randomProto.ChancedDanger;
                ent.Comp.Escape += randomProto.ChancedEscape;

                if (randomProto.ChancedComponentReplaceMain)
                    continue;
            }

            if (!usedChanced || !randomProto.ChancedComponentReplaceMain)
            {
                foreach (var (_, component) in randomProto.Components)
                {
                    EntityManager.AddComponent(ent, component);
                }

                ent.Comp.Danger += randomProto.Danger;
                ent.Comp.Escape += randomProto.Escape;
            }
        }
    }

    /// <summary>
    /// Возвращает все прототипы компонентов с указанным тегом.
    /// </summary>
    private List<ArchonComponentPrototype> GetArchonPrototypesByTag(string tag)
    {
        var prototypes = new List<ArchonComponentPrototype>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<ArchonComponentPrototype>())
        {
            if (proto.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
            {
                prototypes.Add(proto);
            }
        }

        return prototypes;
    }

    /// <summary>
    /// Определяет класс архонта на основе опасности и способности к побегу.
    /// </summary>
    private void SetArchonClass(Entity<ArchonComponent> ent)
    {
        var danger = ent.Comp.Danger;
        var escape = ent.Comp.Escape;

        // Логика определения класса
        if (danger >= ent.Comp.DangerLimit && escape >= ent.Comp.EscapeLimit)
        {
            ent.Comp.Class = ArchonClass.Thaumiel;
        }
        else if (escape >= ent.Comp.EscapeLimit)
        {
            ent.Comp.Class = ArchonClass.Keter;
        }
        else if (danger >= ent.Comp.DangerLimit)
        {
            ent.Comp.Class = ArchonClass.Euclid;
        }
        else
        {
            ent.Comp.Class = ArchonClass.Safe;
        }
    }
}
