using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Components;

[RegisterComponent]
public sealed partial class ArchonComponent : Component
{

    /// <summary>
    /// Класс архона, его опасность и возможность сбежать. Enum ArchonClass
    /// </summary>
    [DataField]
    public ArchonClass Class = ArchonClass.Safe;

    /// <summary>
    /// Уровень опасности
    /// </summary>
    [DataField]
    public int Danger = 0;

    /// <summary>
    /// Уровень опасности
    /// </summary>
    [DataField]
    public int Escape = 0;

    /// <summary>
    /// Уровень опасности для класса Кетер
    /// </summary>
    [ViewVariables]
    public int DangerLimit = 5;

    /// <summary>
    /// Уровень опасности для класса Евклид
    /// </summary>
    [ViewVariables]
    public int EscapeLimit = 5;

    /// <summary>
    /// Уничтожаемость объекта, эффекты после смерти. Enum ArchonDestructibility
    /// </summary>
    [DataField]
    public ArchonDestructibility Destructibility;

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

public enum ArchonClass : byte
{
    Safe, // Если объект можно безопасно содержать, и оно не будет пытаться вырваться или приносить вред.
    Euclid, // Если объект не будет сбегать с коробки, но будет приносить пассивный вред или потенциальный.
    Keter, // Если объект может сбежать, но не обязательно приносить вред.
    Thaumiel // Если объект и приносит вред, и сбегает.
}

public enum ArchonDestructibility : byte
{
    Normal, // Прочность от 10 до 200, если разумный стамина 100-200
    Hard, // От 300 до 700, стамина 400-800
    Invincible, // от 5000 до 10000
    DeathEffect // После уничтожения имеет эффект, типо бабах, перерождение ввиде чего то, оставление предмета с каким то эффектом
}
