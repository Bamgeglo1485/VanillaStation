using Robust.Shared.Physics;

using Content.Shared.Damage;

using System.Numerics;

namespace Content.Shared.Vanilla.TimeStop;

[RegisterComponent]
public sealed partial class TimeStoppedComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier StoredDamage = new();

    [DataField]
    public float StoredStaminaDamage;

    [DataField]
    public BodyType BodyType;

    [DataField]
    public int TimeStops;

    [DataField]
    public Vector2 LinearVelocity;

    [DataField]
    public float AngularVelocity;
}
