using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Прототип с данными о возможных для добавления компонентов в архон. Чтобы компонент можно было разделить на классы и типы
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
    [DataField]
    public string Tag = "Generic";

    /// <summary>
    /// Тип архонта для меньшей хаотичности. Enum ArchonType
    /// </summary>
    [DataField]
    public List<ArchonType> Types = new();

    /// <summary>
    /// Уровень опасности архона
    /// </summary>
    [DataField]
    public int Danger = 0;

    /// <summary>
    /// Уровень возможности побега архона
    /// </summary>
    [DataField]
    public int Escape = 0;

    /// <summary>
    /// Компонент с определённым шансом
    /// </summary>
    [DataField]
    public List<ChancedComponent> ChancedComponents = new();

    /// <summary>
    /// Если true, то этим компонентом не может владеть гуманоидный
    /// </summary>
    [DataField]
    public bool HumanoidBlacklist = false;

    /// <summary>
    /// Если true, то этим компонентом может владеть только гуманоид
    /// </summary>
    [DataField]
    public bool HumanoidWhitelist = false;

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

    public ChancedComponent()
    {
        Components = new ComponentRegistry();
        Chance = 0.3f;
        Danger = 0;
        Escape = 0;
    }
}
