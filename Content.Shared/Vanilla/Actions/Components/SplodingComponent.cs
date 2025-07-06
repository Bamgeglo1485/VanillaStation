using Robust.Shared.Audio;

namespace Content.Shared.Vanilla.Actions.Components;

public enum SplodingState
{
    Standing,    // Ready/initial state
    Charging,    // Currently charging up
    Exploding    // In process of explosion
}

[RegisterComponent]
public sealed partial class SplodingComponent : Component
{
    [DataField]
    public SplodingState State = SplodingState.Standing;

    [DataField]
    public bool Gib = false;

    [DataField]
    public string ExplosionType = "FireBomb";

    [DataField]
    public float ExplodeIntensity = 30f;

    [DataField]
    public float ExplodeMaxIntensity = 30f;

    [DataField]
    public float ExplodeDropoff = 3f;

    [DataField]
    public float Modifier = 1f;

    [DataField]
    public SoundSpecifier? SplodeSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/Actions/SplodeCharge.ogg");

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan Timer = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan StartTime;

    public TimeSpan NextUpdate;
}
