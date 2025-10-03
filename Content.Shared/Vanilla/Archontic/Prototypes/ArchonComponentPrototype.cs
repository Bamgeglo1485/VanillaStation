using Content.Shared.Archontic.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Prototypes;

/// <summary>
/// Прототип с данными о возможных для добавления компонентов в архонт
/// </summary>
[Prototype("archonComponent")]
public sealed partial class ArchonComponentPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Добавляемые компоненты
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Тэг для разделения компонентов
    /// </summary>
    [DataField(required: true)]
    public List<string> Tags = new();

    /// <summary>
    /// Нужны ли все теги у архонта для выбора
    /// </summary>
    [DataField]
    public bool RequireAllTags = false;

    /// <summary>
    /// Описание действий
    /// </summary>
    [DataField(required: true)]
    public string Desc = "Какает";

    /// <summary>
    /// Тип архонта для меньшей хаотичности. Enum ArchonType
    /// </summary>
    [DataField]
    public List<ArchonType> Types = new();

    /// <summary>
    /// Уровень опасности архонта
    /// </summary>
    [DataField]
    public int Danger = 0;

    /// <summary>
    /// Уровень возможности побега архонта
    /// </summary>
    [DataField]
    public int Escape = 0;

    /// <summary>
    /// Компонент с определённым шансом
    /// </summary>
    [DataField]
    public List<ChancedComponent> ChancedComponents = new();

}

[DataDefinition]
public partial struct ChancedComponent
{
    [DataField]
    public ComponentRegistry Components { get; set; }

    [DataField]
    public float Chance { get; set; }

    [DataField]
    public int Danger { get; set; }

    [DataField]
    public int Escape { get; set; }

    [DataField]
    public int AdditiveDanger { get; private set; }

    [DataField]
    public int AdditiveEscape { get; private set; }

    public ChancedComponent()
    {
        Components = new ComponentRegistry();
        Chance = 0.3f;
        Danger = 0; // Работает как перезапись опасности
        Escape = 0;
        AdditiveDanger = 0; // Работает как "Опасность основных компонентов + опасность дополнительного
        AdditiveEscape = 0;
    }
}
