using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Archontic.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Vanilla.Warmer;
using Content.Shared.Weapons.Melee;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.StoryGen;
using Content.Shared.Damage;
using Content.Shared.Paper;
using Content.Shared.Atmos;

using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

using Content.Server.Spawners.Components;
using Content.Server.Destructible;
using Content.Server.Antag;
using Content.Server.Roles;
using System.Linq;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{

    [Dependency] private readonly StoryGeneratorSystem _storyGen = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

}
