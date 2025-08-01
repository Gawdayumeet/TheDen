// SPDX-FileCopyrightText: 2023 PHCodes <47927305+PHCodes@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 sleepyyapril <flyingkarii@gmail.com>
// SPDX-FileCopyrightText: 2025 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.Psionics.Glimmer;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Map.Components;

namespace Content.Server.StationEvents.Events;

internal sealed class FreeProberRule : StationEventSystem<FreeProberRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private static readonly string ProberPrototype = "GlimmerProber";
    private static readonly int SpawnDirections = 4;

    protected override void Started(EntityUid uid, FreeProberRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        List<EntityUid> PossibleSpawns = new();

        var query = EntityQueryEnumerator<GlimmerSourceComponent>();
        while (query.MoveNext(out var glimmerSource, out var glimmerSourceComponent))
        {
            if (glimmerSourceComponent.GlimmerPerSecond >= 0 && glimmerSourceComponent.Active)
            {
                PossibleSpawns.Add(glimmerSource);
            }
        }

        if (PossibleSpawns.Count == 0 || _robustRandom.Prob(0.25f))
        {
            var queryBattery = EntityQueryEnumerator<PowerNetworkBatteryComponent>();
            while (query.MoveNext(out var battery, out var _))
            {
                PossibleSpawns.Add(battery);
            }
        }

        if (PossibleSpawns.Count > 0)
        {
            _robustRandom.Shuffle(PossibleSpawns);

            foreach (var source in PossibleSpawns)
            {
                var xform = Transform(source);

                if (_stationSystem.GetOwningStation(source, xform) == null)
                    continue;

                var coordinates = xform.Coordinates;
                var gridUid = xform.GridUid;

                if (gridUid == null)
                    continue;

                if (!TryComp<MapGridComponent>(gridUid, out var grid))
                    continue;

                var tileIndices = _sharedMapSystem.TileIndicesFor((EntityUid) gridUid, grid, coordinates);

                for (var i = 0; i < SpawnDirections; i++)
                {
                    var direction = (DirectionFlag) (1 << i);
                    var offsetIndices = tileIndices.Offset(direction.AsDir());

                    // This doesn't check against the prober's mask/layer, because it hasn't spawned yet...
                    if (!_anchorable.TileFree(grid, offsetIndices))
                        continue;

                    Spawn(ProberPrototype, _sharedMapSystem.GridTileToLocal((EntityUid) gridUid, grid, offsetIndices));
                    return;
                }
            }
        }
    }
}
