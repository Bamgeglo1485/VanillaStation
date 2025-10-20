using Content.Shared.Temperature.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Vanilla.Archon;
using Content.Shared.Bed.Sleep;

using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.Vanilla.Archon;

public sealed class SleepOnTemperatureSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SleepingSystem _sleep = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleepOnTemperatureComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SleepOnTemperatureComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime < comp.NextUpdate)
                continue;

            if (!TryComp<MobStateComponent>(uid, out var mobState))
                return;

            if (!TryComp<TemperatureComponent>(uid, out var temp))
                return;

            comp.NextUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(3);

            if (temp.CurrentTemperature > comp.MaxTemp || temp.CurrentTemperature < comp.MinTemp)
            {
                if (TryComp<SleepingComponent>(uid, out var sleep))
                    _sleep.TryWaking((uid, sleep), true);

                return;
            }

            _sleep.TrySleeping((uid, mobState));
        }
    }

    private void OnMapInit(EntityUid uid, SleepOnTemperatureComponent comp, ref MapInitEvent args)
    {
        if (!comp.RandomTemp)
            return;

        if (_random.Prob(0.5f))
        {
            comp.MaxTemp = 1000f;
            comp.MinTemp = _random.NextFloat(313f, 353f);
        }
        else
        {
            comp.MaxTemp = _random.NextFloat(193f, 276f);
            comp.MinTemp = -1000f;
        }
    }
}
