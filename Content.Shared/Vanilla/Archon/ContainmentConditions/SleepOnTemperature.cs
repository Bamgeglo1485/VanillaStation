using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Atmos;
using Robust.Shared.Timing;

namespace Content.Shared.Vanilla.Archon;

[RegisterComponent]
public sealed partial class SleepOnTemperatureComponent : Component
{

    [DataField]
    public float MaxTemp = 253f;

    [DataField]
    public float MinTemp = -1000f;

    [DataField]
    public bool RandomTemp = true;

    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;
}

