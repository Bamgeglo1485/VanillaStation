using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Components;

[RegisterComponent]
public sealed partial class ArchonComponent : Component
{

    [DataField]
    public bool RandomType = true;

    [DataField]
    public bool GenerateComponents = true;

    /// <summary>
    /// Диапазон количества типов
    /// </summary>
    [DataField]
    public int MinTypes = 1;

    [DataField]
    public int MaxTypes = 2;

    /// <summary>
    /// Диапазон количества добавляемых компонентов
    /// </summary>
    [DataField]
    public int MinComponents = 2;

    [DataField]
    public int MaxComponents = 3;

    /// <summary>
    /// Диапазон количества триггеров компонентов. Типо OnTrigger и TriggerOn
    /// </summary>
    [DataField]
    public int MinTriggers = 1;

    [DataField]
    public int MaxTriggers = 2;

    /// <summary>
    /// Шанс быть разумным гуманоидоподобным, ну не совсем гуманоид, типо NPC
    /// </summary>
    [DataField]
    public float CanBeHumanoidChance = 0.15f;

    /// <summary>
    /// Шанс изменения физических характеристик
    /// </summary>
    [DataField]
    public float PhysicsChangeChance = 0.15f;

}
