using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Стационарная штука, которая приносит очки, если архонт в радиусе действия
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArchonBeaconComponent : Component
{

    /// <summary>
    /// Привязанный Архонт
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedArchon;

    /// <summary>
    /// Радиус содержания архонта
    /// </summary>
    [DataField]
    public int Radius = 5;

    /// <summary>
    /// Зарабатываемые очки исследований
    /// </summary>
    [DataField]
    public int ResearchPointsPerSecond = 5;

    /// <summary>
    /// Зарабатываемые очки будут напрямую зависеть от класса
    /// </summary>
    [DataField]
    public bool ModificatePointsByClass = true;

    /// <summary>
    /// Сбежал ли архонт
    /// </summary>
    [DataField]
    public bool Breached = false;

}

[Serializable, NetSerializable]
public enum ArchonBeaconVisuals : byte
{
    Classes
}

[Serializable, NetSerializable]
public enum ArchonBeaconClasses : byte
{
    Safe,
    Keter,
    Euclid,
    Thaumiel,
    Breach,
    None,
    NonPowered
}

public sealed class ArchonBreachEvent : EntityEventArgs
{
}
