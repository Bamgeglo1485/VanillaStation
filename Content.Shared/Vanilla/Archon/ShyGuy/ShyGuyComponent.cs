using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Vanilla.Archon.ShyGuy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ShyGuyComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public List<EntityUid> Targets = new();

    [DataField]
    [AutoNetworkedField]
    public ShyGuyState State = ShyGuyState.Calm;

    // Длительность промежутка между спокойным состоянием и погоней за целью
    [DataField]
    public TimeSpan PreparingTime = TimeSpan.FromSeconds(35);

    // Время за которую скромник перестанет преследование одной цели
    [DataField]
    public TimeSpan OneTargetChaseTime = TimeSpan.FromSeconds(140);

    [DataField]
    [AutoPausedField]
    public TimeSpan TargetChaseEnd = TimeSpan.Zero;
    [DataField]
    [AutoPausedField]
    public TimeSpan PreparingEnd = TimeSpan.Zero;

    [DataField]
    public float WalkModifier = 3f;

    [DataField]
    public float SprintModifier = 5f;

    [DataField]
    public SoundSpecifier? PreparingSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096raging.ogg");

    [DataField]
    public SoundSpecifier? StingerSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096trigger.ogg");

    //ачо это нигде не используется
    [DataField]
    public SoundSpecifier? ChaseSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096chase.ogg");

    [DataField]
    public SoundSpecifier? RageAmbient = new SoundPathSpecifier("/Audio/Vanilla/Effects/Archon/096raging.ogg");

    [DataField]
    public SoundSpecifier? CalmAmbient = new SoundPathSpecifier("/Audio/Vanilla/Ambience/096cry.ogg");
}

public enum ShyGuyState : byte
{
    Calm,
    Preparing,
    Rage
}
