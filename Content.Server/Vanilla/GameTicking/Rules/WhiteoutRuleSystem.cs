using Content.Server.GameTicking.Rules.Components;
using Content.Server.Administration.Managers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Light.EntitySystems;
using Content.Server.GameTicking.Rules;
using Content.Server.Tesla.Components;
using Content.Server.Cargo.Components;
using Content.Server.Atmos.Components;
using Content.Server.Light.Components;
using Content.Server.Vanilla.Audio;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Power.SMES;
using Content.Server.RoundEnd;
using Content.Server.Weather;
using Content.Server.Damage;
using Content.Server.Resist;
using Content.Server.Audio;

using Content.Shared.GameTicking.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Weather;
using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Maps;
using Content.Shared.Tag;

using Robust.Shared.Map.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;

using Robust.Server.Player;

using System.Linq;

namespace Content.Server.Vanilla.GameTicking.Rules;

public sealed class WhiteoutRuleSystem : GameRuleSystem<WhiteoutRuleComponent>
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ServerGlobalMusicSystem _music = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly WeatherSystem _weather = default!;
    [Dependency] private readonly ExplosionSystem _boom = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;


    private enum WhiteoutState
    {
        Preparing,
        Active,
        FinalPhase,
        Ended
    }

    private WhiteoutState _state = WhiteoutState.Ended;
    private MapId _activeMapId = MapId.Nullspace;
    private EntityUid _activeMapUid = EntityUid.Invalid;
    private readonly HashSet<EntityUid> _processedLights = new();
    private readonly HashSet<EntityUid> _processedWindows = new();
    private readonly HashSet<EntityUid> _processedSmes = new();
    private readonly HashSet<EntityUid> _processedHardsuits = new();
    private TimeSpan _nextGlassBreak;

    public override void Initialize()
    {
        base.Initialize();
    }

    // Базовые действия при начале геймрула
    protected override void Started(EntityUid uid, WhiteoutRuleComponent comp, GameRuleComponent rule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, rule, args);

        comp.TimeActive = 0f;
        comp.NextUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(1);

        _state = WhiteoutState.Preparing;
        _processedLights.Clear();
        _processedWindows.Clear();
        _processedHardsuits.Clear();
        _nextGlassBreak = TimeSpan.Zero;

        if (_activeMapId == MapId.Nullspace)
        {
            var xform = Transform(uid);
            _activeMapId = xform.MapID;
            if (_activeMapId == MapId.Nullspace)
            {
                _activeMapId = _mapManager.GetAllMapIds().FirstOrDefault(id => id != MapId.Nullspace);
                if (_activeMapId == MapId.Nullspace)
                    return;
            }
            _activeMapUid = _mapManager.GetMapEntityId(_activeMapId);
        }

        RemoveTradeStation();

        Announce(comp.WhiteoutPrepareAnnouncement, comp.WhiteoutSoundAnnouncement);
    }

    // Ну тут понятно
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<WhiteoutRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var comp, out var rule))
        {
            if (currentTime < comp.NextUpdate) continue;

            comp.NextUpdate = currentTime + TimeSpan.FromSeconds(1);
            comp.TimeActive += 1f;
            ProcessWhiteout(uid, comp, rule);
        }
    }

    // Сам вайтаут
    private void ProcessWhiteout(EntityUid uid, WhiteoutRuleComponent comp, GameRuleComponent rule)
    {
        switch (_state)
        {
            case WhiteoutState.Preparing:
                if (comp.TimeActive >= comp.WhiteoutPrepareTime)
                {
                    StartWhiteout(uid, comp, _activeMapId);
                    _state = WhiteoutState.Active;
                }
                break;

            case WhiteoutState.Active:
            case WhiteoutState.FinalPhase:
                var isFinal = _state == WhiteoutState.FinalPhase;
                var (temp, strength) = GetWhiteoutParams(comp, isFinal);
                Freeze(temp, strength, _activeMapId);

                // Ломания
                if (_gameTiming.CurTime >= _nextGlassBreak)
                {
                    var entities = _lookup.GetEntitiesInRange(_activeMapUid, 1000f);
                    foreach (var entity in entities)
                    {
                        if (!Exists(entity) || Deleted(entity)) continue;

                        // Лампы 
                        if (!_processedLights.Contains(entity) &&
                            TryComp<PoweredLightComponent>(entity, out var light) &&
                            CheckTileTemperature(entity, 183.15f) &&
                            RobustRandom.Prob(0.3f))
                        {
                            _poweredLight.TryDestroyBulb(entity, light);
                            _processedLights.Add(entity);
                        }

                        // Шкафчики. Иначе можно в них переждать бурю
                        if (TryComp<ResistLockerComponent>(entity, out _) &&
                            CheckTileTemperature(entity, 133.15f) &&
                            RobustRandom.Prob(0.5f))
                        {
                            var damage = new DamageSpecifier();
                            damage.DamageDict.Add("Blunt", FixedPoint2.New(50));
                            _damageable.TryChangeDamage(entity, damage);
                        }

                        // Окна
                        if (isFinal &&
                            _tagSystem.HasTag(entity, "Window") &&
                            RobustRandom.Prob(0.8f))
                        {
                            var damage = new DamageSpecifier();
                            damage.DamageDict.Add("Blunt", FixedPoint2.New(130));
                            _damageable.TryChangeDamage(entity, damage);
                        }
                    }

                    // Смэсы
                    ExplodeSmes();
                    ChangeTiles();
                    _nextGlassBreak = _gameTiming.CurTime + TimeSpan.FromSeconds(5);
                }

                // Переход между фазами
                if (!isFinal && comp.TimeActive >= comp.WhiteoutPrepareTime + comp.WhiteoutLength)
                {
                    MakeAtmos(comp.WhiteoutFinalTemp, comp.PlanetMap);
                    _state = WhiteoutState.FinalPhase;
                    _music.PlayGlobalMusic(_audio.ResolveSound(comp.WhiteoutFinalMusic));
                    Announce(comp.WhiteoutFinalAnnouncement, comp.WhiteoutFinalSoundAnnouncement);
                }
                if (isFinal && comp.TimeActive >= comp.WhiteoutPrepareTime + comp.WhiteoutLength + comp.WhiteoutFinalLength - 60f)
                {
                    _roundEnd.RequestRoundEnd(TimeSpan.FromMinutes(1), uid, false, "whiteout-evac", "department-CentralCommand");
                }
                if (isFinal && comp.TimeActive >= comp.WhiteoutLength + comp.WhiteoutFinalLength)
                {
                    EndWhiteout(uid, comp, rule, _activeMapId);
                    _state = WhiteoutState.Ended;
                }
                break;
        }
    }

    // Действия при начале
    private void StartWhiteout(EntityUid uid, WhiteoutRuleComponent comp, MapId mapId)
    {
        comp.TimeActive = 0f;

        MakeAtmos(comp.WhiteoutTemp, comp.PlanetMap);

        _music.PlayGlobalMusic(_audio.ResolveSound(comp.WhiteoutMusic));

        if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.Weather, out var weatherProto))
            return;

        _weather.SetWeather(mapId, weatherProto, TimeSpan.FromMinutes(30));
        RemoveHardsuitProtection();


        Announce(comp.WhiteoutAnnouncement, comp.WhiteoutSoundAnnouncement);
    }

    // Действия при конце
    private void EndWhiteout(EntityUid uid, WhiteoutRuleComponent comp, GameRuleComponent rule, MapId mapId)
    {
        GameTicker.EndGameRule(uid, rule);
        _weather.SetWeather(mapId, null, TimeSpan.FromMinutes(1));

        RemComp<MapAtmosphereComponent>(_activeMapUid);

        Announce(comp.WhiteoutEndAnnouncement, comp.WhiteoutSoundAnnouncement);
    }

    // Заморозка газов
    private void Freeze(float targetTemp, float strength, MapId mapId)
    {
        var query = EntityQueryEnumerator<GridAtmosphereComponent, TransformComponent>();
        while (query.MoveNext(out var gridUid, out var grid, out var gridXform))
        {
            if (gridXform.MapID != mapId)
                continue;

            foreach (var tile in grid.Tiles.Values)
            {
                if (tile.Air is not { Pressure: >= 1f, TotalMoles: > 0f } mixture)
                    continue;

                var heatCap = _atmosphere.GetHeatCapacity(mixture, false);
                if (heatCap <= 0)
                    continue;

                var temp = mixture.Temperature;
                if (temp <= targetTemp)
                    continue;

                float delta = (temp - targetTemp) * strength;
                mixture.Temperature = Math.Max(targetTemp, temp - delta);
            }
        }
    }

    // Убирание резистов скафов для баланса
    private void RemoveHardsuitProtection()
    {
        var query = _lookup.GetEntitiesInRange(_activeMapUid, 1000f);
        foreach (var uid in query)
        {
            if (_processedHardsuits.Contains(uid))
                continue;

            if (HasComp<TemperatureProtectionComponent>(uid) && HasComp<PressureProtectionComponent>(uid) && HasComp<TransformComponent>(uid) && _tagSystem.HasTag(uid, "Hardsuit"))
            {
                RemComp<TemperatureProtectionComponent>(uid);
                RemComp<PressureProtectionComponent>(uid);
                _processedHardsuits.Add(uid);
            }
        }
    }

    // Удаление компонента трейдпоста у поста для усложнения
    private void RemoveTradeStation()
    {
        var query = EntityQueryEnumerator<TradeStationComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            RemComp<TradeStationComponent>(uid);
        }
    }

    // Взрыв смэсов
    private void ExplodeSmes()
    {
        if (_activeMapUid == null || !Exists(_activeMapUid))
            return;

        var query = EntityQueryEnumerator<SmesComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var smes, out var transform))
        {
            if (!CheckTileTemperature(uid, 133.15f))
                    continue;

            _boom.QueueExplosion( uid, "Cryo", 200, 10, 200 );
        }
    }

    // Проверка температуры тайла энтити, для проведения действий
    private bool CheckTileTemperature(EntityUid uid, float threshold)
    {
        if (!Exists(uid) || Deleted(uid))
            return false;

        if (!TryComp<TransformComponent>(uid, out var transform) || transform.GridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return false;

        var tile = grid.TileIndicesFor(transform.Coordinates);
        var atmosphere = _atmosphere.GetTileMixture(transform.GridUid, null, tile, true);

        return atmosphere?.Temperature < threshold;
    }

    // Смена тайлов на снежочек
    private void ChangeTiles(float temp = 122f)
    {
        if (!_prototypeManager.TryIndex<ContentTileDefinition>("FloorSnowPlating", out var iceTile))
            return;

        var query = EntityQueryEnumerator<GridAtmosphereComponent, MapGridComponent>();
        while (query.MoveNext(out var gridUid, out var gridAtmos, out var grid))
        {
            if (Transform(gridUid).MapID != _activeMapId)
                continue;

            foreach (var (tile, atmosTile) in gridAtmos.Tiles)
            {
                if (atmosTile.Air == null || atmosTile.Air.Temperature >= temp)
                    continue;

                var tileRef = grid.GetTileRef(tile);
                var tileDef = tileRef.Tile.GetContentTileDefinition();

                if (tileRef.Tile.IsEmpty ||
                    tileDef.ID == "FloorSnowPlating" ||
                    tileDef.ID == "Lattice")
                    continue;

                if (RobustRandom.Prob(0.3f))
                {
                    grid.SetTile(tile, new Tile(iceTile.TileId));
                }
            }
        }
    }

    // Создание атмосферы при начале бури, реалистик и +сложность
    private void MakeAtmos(float temp, bool planet)
    {
        if (!_mapManager.MapExists(_activeMapId))
            return;

        var moles = new float[Atmospherics.AdjustedNumberOfGases];

        if (planet == true)
        {
            moles[(int)Gas.Oxygen] = 43.649558f;
            moles[(int)Gas.Nitrogen] = 164.20624f;
        }
        else
        {
            moles[(int)Gas.Oxygen] = 126f;
            moles[(int)Gas.Frezon] = 141f;
            moles[(int)Gas.NitrousOxide] = 132f;
        }

        var mixture = new GasMixture(moles, temp);

        if (Exists(_activeMapUid))
        {
            var mapAtmos = EnsureComp<MapAtmosphereComponent>(_activeMapUid);
            _atmosphere.SetMapAtmosphere(_activeMapUid, false, mixture);
        }
    }

    // Объявление
    private void Announce(string message, SoundSpecifier sound)
    {
        _audio.PlayGlobal(sound, Filter.Broadcast(), true);
        _chat.DispatchGlobalAnnouncement(Loc.GetString(message), playSound: false, colorOverride: Color.Red);
    }

    // Вычисления всякие умные
    private (float Temp, float Strength) GetWhiteoutParams(WhiteoutRuleComponent comp, bool isFinal)
        => isFinal
            ? (comp.WhiteoutFinalTemp, comp.WhiteoutStrength * (comp.TimeActive / (comp.WhiteoutLength + comp.WhiteoutFinalLength)) * comp.WhiteoutFinalModifier) 
            : (comp.WhiteoutTemp, comp.WhiteoutStrength * (comp.TimeActive / (comp.WhiteoutLength + comp.WhiteoutFinalLength))); // Сила охлаждение зависит от близости к концу
}
