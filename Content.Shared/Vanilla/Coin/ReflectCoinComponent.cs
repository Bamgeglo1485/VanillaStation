using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;

namespace Content.Shared.Vanilla.Coin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectCoinComponent : Component
{

    [DataField, AutoNetworkedField]
    public float DamageModifier = 1.5f;

    [DataField, AutoNetworkedField]
    public float FlashingDamageModifier = 2.5f;

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> Faction = new();

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
}
