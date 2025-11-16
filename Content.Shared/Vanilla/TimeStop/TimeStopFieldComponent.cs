using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Vanilla.TimeStop;
using Robust.Shared.Timing;

[RegisterComponent]
public sealed partial class TimeStopFieldComponent : Component
{

    [DataField]
    public float Radius = 2f;

    [DataField]
    public bool IgnoreImmunity = false;

    [DataField]
    public HashSet<EntityUid> TimeStoppedEntities { get; set; } = new();

    [DataField]
    public HashSet<EntityUid> TimeStopIgnored { get; set; } = new();

    [DataField]
    public TimeSpan UpdateSpeed = TimeSpan.FromSeconds(0.05);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;
}
