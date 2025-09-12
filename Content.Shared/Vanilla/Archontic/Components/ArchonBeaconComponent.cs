using Content.Shared.Archontic.Systems;

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Стационарная штука, которая приносит очки, если архонт в радиусе действия
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArchonSystem))]
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

    [DataField("updateSpeed")]
    public int UpdateSpeed = 5;
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;
}

public sealed class BeaconPointAddEvent : EntityEventArgs
{
    public int Points = 5;

    public BeaconPointAddEvent(int points)
    {
        Points = points;
    }
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
