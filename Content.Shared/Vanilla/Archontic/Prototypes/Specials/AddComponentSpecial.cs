using Content.Shared.Archontic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Archontic.Prototypes;

public sealed partial class AddComponentSpecial : ArchonSpecial
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    [DataField]
    public float Chance = 1f;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(mob, Components, removeExisting: true);
    }
}
