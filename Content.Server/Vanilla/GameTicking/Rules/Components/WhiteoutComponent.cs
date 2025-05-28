using Content.Server.Vanilla.GameTicking.Rules;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Weather;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(WhiteoutRuleSystem))]
public sealed partial class WhiteoutRuleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool PlanetMap = false;


    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WhiteoutLength = 90;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WhiteoutFinalLength = 90f;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WhiteoutPrepareTime = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WhiteoutTemp = 123.15f;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WhiteoutFinalTemp = 23.15f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WhiteoutStrength = 0.035f;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WhiteoutFinalModifier = 2f;

    [DataField]
    public string Weather = "SnowfallHeavy";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId WhiteoutPrepareAnnouncement = "whiteout-prepare-announcement";
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId WhiteoutAnnouncement = "whiteout-announcement";
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId WhiteoutFinalAnnouncement = "whiteout-announcement-final";
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId WhiteoutEndAnnouncement = "whiteout-announcement-end";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier WhiteoutSoundAnnouncement = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/announcement.ogg");
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier WhiteoutFinalSoundAnnouncement = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/whiteout_siren.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier WhiteoutMusic = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/whiteout.ogg");
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier WhiteoutFinalMusic = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/whiteout_final.ogg");

    public float TimeActive;
    public TimeSpan NextUpdate;
}
