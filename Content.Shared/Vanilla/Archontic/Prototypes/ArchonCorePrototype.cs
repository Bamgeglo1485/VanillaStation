using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.StoryGen;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Прототип с данными о ядре архонта
/// </summary>
[Prototype("archonCore")]
public sealed partial class ArchonCorePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Тэги архонта
    /// </summary>
    [DataField(required: true)]
    public List<ArchonTag> Tags = new();

    /// <summary>
    /// Название ядра
    /// </summary>
    [DataField(required: true)]
    public string Name = "ГеглоЛучший";

    /// <summary>
    /// Доступные типы Архонта
    /// </summary>
    [DataField]
    public List<ArchonType> Types = new();

    /// <summary>
    /// Случайная прочность
    /// </summary>
    [DataField]
    public bool RandomDestructibility = true;

    /// <summary>
    /// Случайный урон
    /// </summary>
    [DataField]
    public bool RandomDamage = false;

    /// <summary>
    /// Случайная скорость
    /// </summary>
    [DataField]
    public bool RandomMovementSpeed = false;

    /// <summary>
    /// Гуманоиден ли архонт
    /// </summary>
    [DataField]
    public bool Humanoid = false;

    /// <summary>
    /// Используем систему рандомных историй из книг для описания пон
    /// </summary>
    [DataField]
    public ProtoId<StoryTemplatePrototype> Template;

}

[DataDefinition]
public partial struct ArchonTag
{
    [DataField]
    public string Tag = "generic";

    [DataField]
    public int Max = 3;

    [DataField]
    public int Min = 1;
}
