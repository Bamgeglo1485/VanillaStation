using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Content.Shared.Vanilla.Archon.ShyGuy;

public sealed class OutlineHoverEvent : EntityEventArgs
{
    public EntityUid User { get; set; }

    public OutlineHoverEvent(EntityUid user)
    {
        User = user;
    }
}

[Serializable, NetSerializable]
public sealed class ShyGuyGazeEvent : EntityEventArgs
{
    public NetEntity ShyGuy;
    public NetEntity User;

    public ShyGuyGazeEvent(NetEntity shyGuy, NetEntity user)
    {
        ShyGuy = shyGuy;
        User = user;
    }
}
