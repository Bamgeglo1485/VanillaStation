using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NodeContainer.Nodes;

using Content.Shared.Atmos.Components;
using Content.Shared.NodeContainer;

using Robust.Shared.Timing;

namespace Content.Server.Vanilla.Atmos.EntitySystems;

public sealed class PressureExplosionSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _boom = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        var currentTime = (float)_timing.CurTime.TotalSeconds;
        var query = EntityQueryEnumerator<PressureExplosionComponent, NodeContainerComponent>();

        while (query.MoveNext(out var uid, out var comp, out var nodeContainer))
        {
            if (NextCheckTime == 0f)
            {
                NextCheckTime = currentTime + CheckInterval;
                return;
            }

            if (currentTime < comp.NextCheckTime)
                continue;

            comp.NextCheckTime = currentTime + comp.CheckInterval;

            if (!Transform(uid).Anchored)
                continue;

            foreach (var node in nodeContainer.Nodes.Values)
            {
                if (node is not PipeNode pipeNode)
                    continue;

                var mixture = pipeNode.Air;
                if (mixture.Pressure < comp.PressureLimit)
                    continue;

                _boom.QueueExplosion(
                    uid,
                    comp.ExplosionPrototype,
                    mixture.Pressure / 1000f * comp.ExplosionMultiplier,
                    10f,
                    400f);

                break;
            }
        }
    }
}
