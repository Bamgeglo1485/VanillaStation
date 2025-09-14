using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

[RegisterComponent]
public sealed partial class ArchonGenerateComponent : Component
{

    [DataField]
    public bool RandomType = true;

    [DataField]
    public bool GenerateComponents = true;

    [DataField]
    public bool TriggerComponents = true;

    /// <summary>
    /// Тэг для базовых компонентов
    /// </summary>
    [DataField]
    public string GenericTag = "Generic";

    /// <summary>
    /// Дополнительный тэг
    /// </summary>
    [DataField]
    public string? AdditiveTag;

    /// <summary>
    /// Диапазон количества типов
    /// </summary>
    [DataField]
    public int MinTypes = 2;

    [DataField]
    public int MaxTypes = 3;

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

}
