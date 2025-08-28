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
    public string Tag = "Generic"

    /// <summary>
    /// Уровень опасности архона, 3 это кетер, скорее всего позже перемещу границы в компонент
    /// </summary>
    [DataField]
    public int Danger = 0;

    /// <summary>
    /// Уровень возможности побега архона, 3 это евклид, скорее всего позже перемещу границы в компонент
    /// </summary>
    [DataField]
    public int Escape = 0;

    /// <summary>
    /// Компонент с определённым шансом
    /// </summary>
    [DataField]
    public ComponentRegistry ChancedComponents = new();

    [DataField]
    public float ChancedComponentChance = 0.3f;

    /// <summary>
    /// Не добавляет обычные Components если срабатывает шанс
    /// </summary>
    [DataField]
    public bool ChancedComponentReplaceMain = true;

    [DataField]
    public int ChancedDanger = 0;

    [DataField]
    public int ChancedEscape = 0;

}


