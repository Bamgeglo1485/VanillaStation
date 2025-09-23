using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Спавнит архонт в рандомном месте
/// </summary>
[RegisterComponent, Access(typeof(ArchonSpawnRule))]
public sealed partial class ArchonSpawnRuleComponent : Component
{
    [DataField("SpawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnerPrototype = "RandomArchonSpawnerQuaranted";
}
