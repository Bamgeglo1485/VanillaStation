using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

[RegisterComponent]
public sealed partial class ArchonComponent : Component
{

    [DataField]
    public bool RandomType = true;

    [DataField]
    public bool RandomDestructibility = true;

    [DataField]
    public bool GenerateComponents = true;

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

    /// <summary>
    /// Я не понял как редактировать Destructible
    /// </summary>
    [DataField]
    public float Health = 0f;

    [DataField]
    public string RebirthPrototype = "EffectArchonDeath";

}

public sealed class DirtyArchonEvent : EntityEventArgs
{
}
