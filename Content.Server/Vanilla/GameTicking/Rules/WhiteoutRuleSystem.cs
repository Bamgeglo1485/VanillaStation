using Content.Server.GameTicking.Rules.Components;
using Content.Server.Administration.Managers;
using Content.Server.Temperature.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Light.EntitySystems;
using Content.Server.Cargo.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Atmos.Components;
using Content.Server.Light.Components;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Weather;
using Content.Server.Damage;
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
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly WeatherSystem _weather = default!;
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

        if (comp.WhiteoutPrepareAnnouncement != null)
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
            if (currentTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = currentTime + TimeSpan.FromSeconds(1);
            comp.TimeActive += 1f;

            ProcessWhiteout(uid, comp, rule);
        }
    }

    // Действия при буре
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

                // Заморозка газов на всех гридах карты
                Freeze(temp, strength, _activeMapId);

                // Ломание лампочек, окон(при финале) и превращение обычных тайлов в снег
                var currentTime = _gameTiming.CurTime;
                if (currentTime >= _nextGlassBreak)
                {
                    BreakLights();
                    ChangeTiles();
                    _nextGlassBreak = currentTime + TimeSpan.FromSeconds(5);

                    if (isFinal)
                    {
                        DamageWithTag("Window", 130);
                    }
                }

                // Переход в финальчик
                if (!isFinal && comp.WhiteoutLength - comp.TimeActive <= comp.WhiteoutFinalLength)
                {
                    MakeAtmos(comp.WhiteoutFinalTemp);
                    _state = WhiteoutState.FinalPhase;

                    // Объявление
                    if (comp.WhiteoutFinalAnnouncement != null)
                    {
                        _audio.PlayGlobal("/Audio/Vanilla/StationEvents/whiteout_final.ogg", Filter.Broadcast(), true);
                        Announce(comp.WhiteoutFinalAnnouncement, comp.WhiteoutFinalSoundAnnouncement);
                    }
                }

                // Конец
                if (comp.TimeActive >= comp.WhiteoutPrepareTime + comp.WhiteoutLength)
                {
                    EndWhiteout(uid, comp, rule, _activeMapId);
                    _state = WhiteoutState.Ended;
                }
                break;

            case WhiteoutState.Ended:
            default:
                break;
        }
    }

    // Действия при начале
    private void StartWhiteout(EntityUid uid, WhiteoutRuleComponent comp, MapId mapId)
    {
        comp.TimeActive = 0f;

        MakeAtmos(comp.WhiteoutTemp);

        _audio.PlayGlobal("/Audio/Vanilla/StationEvents/whiteout.ogg", Filter.Broadcast(), true);

        if (!_prototypeManager.TryIndex<WeatherPrototype>(comp.Weather, out var weatherProto))
            return;

        _weather.SetWeather(mapId, weatherProto, TimeSpan.FromMinutes(30));
        RemoveHardsuitProtection();

        if (comp.WhiteoutAnnouncement != null)
            Announce(comp.WhiteoutAnnouncement, comp.WhiteoutSoundAnnouncement);
    }

    // Действия при конце
    private void EndWhiteout(EntityUid uid, WhiteoutRuleComponent comp, GameRuleComponent rule, MapId mapId)
    {
        GameTicker.EndGameRule(uid, rule);
        _weather.SetWeather(mapId, null, TimeSpan.FromMinutes(1));
        _roundEnd.RequestRoundEnd(TimeSpan.FromMinutes(1), uid);

        RemComp<MapAtmosphereComponent>(_activeMapUid);

        if (comp.WhiteoutEndAnnouncement != null)
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

            if (HasComp<TemperatureProtectionComponent>(uid) && HasComp<PressureProtectionComponentt>(uid) && _tagSystem.HasTag(uid, "Hardsuit"))
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

    // Ломание лампочек для красоты, если -90 и ниже
    private void BreakLights()
    {
        var query = _lookup.GetEntitiesInRange(_activeMapUid, 1000f);
        foreach (var uid in query)
        {
            if (!Exists(uid) || Deleted(uid) || _processedLights.Contains(uid))
                continue;

            if (!CheckTileTemperature(uid, 183.15f))
                continue;

            if (TryComp<PoweredLightComponent>(uid, out var light))
            {
                if (RobustRandom.Prob(0.3f))
                {
                    _poweredLight.TryDestroyBulb(uid, light);
                    _processedLights.Add(uid);
                }
            }
        }
    }

    // Нанесение урона если у предмета тэг, ну окнам
    private void DamageWithTag(string tag, float damageAmount)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", FixedPoint2.New(damageAmount));

        var query = _lookup.GetEntitiesInRange(_activeMapUid, 1000f);
        foreach (var uid in query)
        {
            if (_processedWindows.Contains(uid))
                continue;

            if (_tagSystem.HasTag(uid, tag))
            {
                if (RobustRandom.Prob(0.8f))
                {
                    _damageable.TryChangeDamage(uid, damage);
                    _processedWindows.Add(uid);
                }
            }
        }
    }

    // Проверка температуры тайла энтити, для проведения действий
    private bool CheckTileTemperature(EntityUid uid, float threshold)
    {
        if (!Exists(uid) || Deleted(uid))
            return false;

        var transform = Transform(uid);
        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return false;

        var tile = grid.TileIndicesFor(transform.Coordinates);
        var atmosphere = _atmosphere.GetTileMixture(transform.GridUid, null, tile, true);

        return atmosphere != null && atmosphere.Temperature < threshold;
    }

    // Смена тайлов на снежочек
    private void ChangeTiles(float freezeThreshold = 122f)
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
                if (atmosTile.Air == null || atmosTile.Air.Temperature >= freezeThreshold)
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
    private void MakeAtmos(float temp)
    {
        if (!_mapManager.MapExists(_activeMapId))
            return;

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int)Gas.Oxygen] = 126f;
        moles[(int)Gas.Frezon] = 141f;
        moles[(int)Gas.NitrousOxide] = 132f;

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
            ? (comp.WhiteoutFinalTemp, comp.WhiteoutStrength * (comp.TimeActive / 1000+1) * comp.WhiteoutFinalModifier)
            : (comp.WhiteoutTemp, comp.WhiteoutStrength);
}
