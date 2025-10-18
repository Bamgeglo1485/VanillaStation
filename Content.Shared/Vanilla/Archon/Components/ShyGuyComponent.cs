using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Vanilla.Archon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ShyGuyComponent : Component
{
    [AutoNetworkedField]
    [DataField]
    public List<EntityUid> Targets = new();

    [AutoNetworkedField]
    [DataField]
    public ShyGuyState State = ShyGuyState.Calm;

    [AutoNetworkedField]
    [DataField]
    public TimeSpan RagingDelay = TimeSpan.FromSeconds(50);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan RagingEnd = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier? RagingSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096raging.ogg");

    [DataField]
    public SoundSpecifier? StingerSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096trigger.ogg");

    [DataField]
    public SoundSpecifier? RageAmbient = new SoundPathSpecifier("/Audio/Vanilla/Ambience/096rage.ogg");

    [DataField]
    public SoundSpecifier? CalmAmbient = new SoundPathSpecifier("/Audio/Vanilla/Ambience/096cry");
}

public enum ShyGuyState : byte
{
    Calm, 
    Raging, 
    Rage
}

public sealed class OutlineHoverEvent : EntityEventArgs
{
    public EntityUid? User { get; set; }

    public OutlineHoverEvent(EntityUid? user)
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

