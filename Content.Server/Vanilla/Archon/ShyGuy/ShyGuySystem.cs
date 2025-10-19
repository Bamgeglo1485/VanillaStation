using Content.Server.NPC.Systems;
using Content.Server.NPC;

using Content.Shared.Vanilla.Archon.ShyGuy;
using Content.Shared.Jittering;
using Content.Shared.Audio;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using Robust.Shared.Map;

using Robust.Server.GameObjects;

using System.Numerics;

namespace Content.Server.Vanilla.Archon.ShyGuy;

public sealed class ShyGuySystem : SharedShyGuySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NPCSystem _npc = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ShyGuyGazeEvent>(OnGaze);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        if (curTime < _nextUpdate)
            return;

        _nextUpdate = curTime + TimeSpan.FromSeconds(1);

        var query = EntityQueryEnumerator<ShyGuyComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            switch (comp.State)
            {
                case ShyGuyState.Raging:
                    _jitter.AddJitter(uid, 20, 20);

                    if (curTime >= comp.RagingEnd)
                    {
                        comp.State = ShyGuyState.Rage;

                        if (comp.RageAmbient != null)
                            _ambient.SetSound(uid, comp.RageAmbient);
                            
                        _ambient.SetAmbience(uid, true);
                    }
                    break;
                case ShyGuyState.Rage:
                    _jitter.AddJitter(uid, 10, 10);

                    if (comp.Targets.Count == 0)
                    {
                        Calm(uid, comp);

                        if (comp.CalmAmbient != null)
                            _ambient.SetSound(uid, comp.CalmAmbient);
                        _ambient.SetAmbience(uid, true);
                    }
                    break;
            }
        }
    }

    private void OnGaze(ShyGuyGazeEvent ev)
    {
        var shyGuy = GetEntity(ev.ShyGuy);
        var user = GetEntity(ev.User);

        if (!TryComp<ShyGuyComponent>(shyGuy, out var comp))
            return;

        if (user == null)
            return;

        if (!comp.Targets.Contains(user))
        {
            comp.Targets.Add(user);
        }
        else
            return;

        Dirty(shyGuy, comp);

        Rage(shyGuy, comp, user);
    }

    public void Rage(EntityUid uid, ShyGuyComponent comp, EntityUid? target = null)
    {
        if (comp.State != ShyGuyState.Calm || target == null)
            return;

        comp.RagingEnd = _timing.CurTime + comp.RagingDelay;
        comp.State = ShyGuyState.Raging;

        _ambient.SetAmbience(uid, false);

        _popup.PopupEntity("Он начинает трястись в конвульсиях...", uid, PopupType.SmallCaution);

        if (comp.RagingSound != null)
            _audio.PlayPvs(comp.RagingSound, uid);
    }

    public void Calm(EntityUid uid, ShyGuyComponent comp)
    {
        if (comp.State == ShyGuyState.Calm)
            return;

        comp.State = ShyGuyState.Calm;
        comp.Targets.Clear();
        comp.RagingEnd = TimeSpan.Zero;
    }
}
