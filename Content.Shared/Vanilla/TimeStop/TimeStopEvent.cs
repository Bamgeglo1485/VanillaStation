using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

using Content.Shared.Actions;

namespace Content.Shared.Vanilla.TimeStop;

public sealed partial class TimeStopEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId Prototype;
}
