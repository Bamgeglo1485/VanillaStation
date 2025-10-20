using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Vanilla.Archon.ShyGuy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ShyGuyComponent : Component
{

    [AutoNetworkedField]
    [DataField]
    public List<EntityUid> Targets = new();

    [AutoNetworkedField]
    [DataField]
    public ShyGuyState State = ShyGuyState.Calm;

    // Длительность промежутка между спокойным состоянием и погоней за целью
    [AutoNetworkedField]
    [DataField]
    public TimeSpan RagingDelay = TimeSpan.FromSeconds(35);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan RagingEnd = TimeSpan.Zero;

    // Время за которую скромник перестанет преследование одной цели
    [AutoNetworkedField]
    [DataField]
    public TimeSpan OneTargetChaseTime = TimeSpan.FromSeconds(140);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan TargetChaseEnd = TimeSpan.Zero;

    // Сколько нужно урона нанести цели, чтобы скромник успокоился
    [DataField]
    public float DamageToCalm = 300f;

    [AutoNetworkedField, ViewVariables]
    public float WalkModifier = 3f;

    [AutoNetworkedField, ViewVariables]
    public float SprintModifier = 5f;

    [DataField]
    public SoundSpecifier? RagingSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096raging.ogg");

    [DataField]
    public SoundSpecifier? StingerSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096trigger.ogg");

    [DataField]
    public SoundSpecifier? ChaseSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096chase.ogg");

    [DataField]
    public SoundSpecifier? RageAmbient = new SoundPathSpecifier("/Audio/Vanilla/Ambience/096rage.ogg");

    [DataField]
    public SoundSpecifier? CalmAmbient = new SoundPathSpecifier("/Audio/Vanilla/Ambience/096cry.ogg");
}

public enum ShyGuyState : byte
{
    Calm,
    Raging,
    Rage
}

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
