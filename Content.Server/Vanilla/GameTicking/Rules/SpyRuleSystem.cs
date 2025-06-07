using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Humanoid;

namespace Content.Server.GameTicking.Rules;

public sealed class SpyRuleSystem : GameRuleSystem<SpyRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpyRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<SpyRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }
    
    // Greeting upon spy activation
    private void AfterAntagSelected(Entity<SpyRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<SpyRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var briefing = Loc.GetString("spy-role-briefing");

            briefing += "\n \n" + Loc.GetString("spy-role-briefing-equipment") + "\n";

        return briefing;
    }
}
