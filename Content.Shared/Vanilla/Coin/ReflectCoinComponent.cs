using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;

namespace Content.Shared.Vanilla.Coin;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectCoinComponent : Component
{

    [DataField, AutoNetworkedField]
    public float DamageModifier = 2f;

    [DataField, AutoNetworkedField]
    public float FlashingDamageModifier = 3f;

    [ViewVariables]
    public DamageSpecifier? StoredDamage;

    [ViewVariables]
    public bool Flashing = false;

    [ViewVariables]
    public TimeSpan? FlashingStartTime { get; set; }

    [ViewVariables]
    public TimeSpan? FlashingEndTime { get; set; }

    [DataField("flashEffect")]
    public string FlashEffectPrototype = "CoinFlash";

    [DataField("flashSound")]
    public SoundSpecifier FlashSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/R1/Coin.ogg");

    [DataField("reflectSound")]
    public SoundSpecifier ReflectSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/R1/Ricochet.ogg");
}
