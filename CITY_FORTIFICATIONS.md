# City fortifications

## Scope

`Assets/Scripts/TetherfallCityFortifications.cs` is an additive, self-contained architecture pass for both existing cities. It does not change either `CityCatalog` center, any route definition, institution placement, Threadway, arrival, market, training ring, or combat center.

`Assets/Editor/TetherfallCityFortificationsVerification.cs` verifies the pass in Edit Mode. The verifier includes `VerifyFromCommandLine` and `VerifyForFullGame`; this task does not modify the shared full-game verifier.

## Architecture

### Alderwake Hearthmarket

- A weathered warm-stone curtain wall encloses the city while leaving the southern road and Hearthmarket Loom corridor open.
- Six varied watch towers, walkable west/east/north wall spans, paired parapets, guard alcoves, moss defensive terraces, timber stairs, copper ramps, and return routes create a vertical perimeter loop.
- The southern gatehouse uses a repaired copper lintel and a high civic focal frame. Moss and timber carry the mass; its single teal bearing is a navigation cue.
- Two authored wall ascent routes each include a deliberate climb face, a tower overlook, railings, and a separate return ramp so neither route traps the player.

### Veyrwater Mooring

- A dark timber-and-stone floodwall follows an offset perimeter that preserves the Veyrwater and Lanternreach Threadway arrivals.
- Raised canal-edge battlements, elevated floodwall walks, three reed-roof guard alcoves, five lantern watch towers, and heavy flood buttresses create a lower, broader marsh silhouette.
- The eastern sluice gatehouse has a wheel, chain runs, counterweights, a raised bridge deck, reed roofs, and simple flood-defense piers.
- Two authored ascent loops reach lantern-tower overlooks and return to ground. Teal appears only at the safe southern gate and sluice bridge crossing markers.

## Runtime API and integration hook

The public lifecycle is:

```csharp
TetherfallCityFortifications.Build(game, parent);
TetherfallCityFortifications.Rebuild(game, parent);
TetherfallCityFortifications.BuildForcedMissingResources(game, parent);
TetherfallCityFortifications.RebuildForcedMissingResources(game, parent);
TetherfallCityFortifications.Cleanup(parent);
```

`Build` is idempotent and returns the existing root. `Rebuild` retains the stable root while replacing its generated children deterministically. `Cleanup` is also idempotent.

The preferred shared-world integration seam is immediately after both existing `BuildCity` calls in `CityBuilder.Build`:

```csharp
BuildCity(game, CityCatalog.AlderwakeHearthmarket);
BuildCity(game, CityCatalog.VeyrwaterMooring);
TetherfallCityFortifications.Build(game, game.transform);
```

If the hook must live inside `CityDirector.BuildCity`, invoke it once after Veyrwater finishes:

```csharp
if (definition.id == CityCatalog.VeyrwaterMooring.id)
{
    TetherfallCityFortifications.Build(game, game.transform);
}
```

The shared `game.transform` parent is intentional: one generated root owns both cities and one central culling driver. Calling once also keeps the combined budget authoritative.

## Collision and traversal contract

Every city root has:

- `Visuals · Collider Free`, containing all procedural meshes and optional source-prefab instances.
- `Gameplay · Deliberate Simple Colliders`, containing only marked `BoxCollider` objects.

Wall bodies, walk decks, railings, ascent surfaces, gate piers, overlook decks, and the sluice crossing receive deliberate simple collision. Only four route-owned faces receive `ClimbableSurface`; decorative wall faces are not climbable.

The perimeter is authored outside protected central road lanes. The verifier checks all deliberate collider bounds against:

- every Threadway anchor and arrival;
- city institution centers and door approaches;
- cooperative market stalls;
- existing city traversal starts and checkpoints;
- training, route-event, and bounty combat centers.

## Performance contract

The combined hard ceilings are 220 renderers, 70 deliberate colliders, 16 shared procedural materials, zero added realtime lights, and one central culling driver. There are no per-piece `Update`, `LateUpdate`, or `FixedUpdate` methods.

One `CullingGroup` owns two city renderer clusters with 85 m near and 170 m cull bands. Gameplay collision is not disabled when visuals cull. Optional Quaternius source pieces are accepted only when compact and free of colliders, lights, cameras, animation, physics bodies, scripts, and per-piece LOD groups.

## Resource fallback

Eight facade accents attempt approved existing Resources prefabs:

- `Wall_UnevenBrick_Straight`
- `Wall_Plaster_WoodGrid`
- `Wall_UnevenBrick_Window_Wide_Round`
- `Wall_Arch`

Each role has a complete primitive-based fallback. The forced missing-Resources path bypasses every optional Quaternius prefab load and builds the same wall, gatehouse, traversal, collision, culling, and budget contract.

## Provenance addendum

The optional source geometry is the existing Quaternius Medieval Village MegaKit subset already preserved in this repository under CC0 1.0. Instantiated source prefabs keep their imported meshes, materials, and textures. This pass does not recolor, rebake, modify, or claim authorship of Quaternius work, and it imports no external files.

The perimeter layouts, tower and gatehouse compositions, flood-defense mechanisms, wall-walk routes, collider plan, metadata, culling system, and procedural fallback geometry are original Tetherfall runtime compositions made from Unity primitives and shared runtime materials.

This file is the provenance update for the task. `Assets/Art/ART_PROVENANCE.md` and `Assets/Art/ART_DIRECTION.md` remain unchanged.

## Verification

Use **Tetherfall > Verify City Fortifications** in Edit Mode, or invoke:

```text
Tetherfall.Editor.TetherfallCityFortificationsVerification.VerifyFromCommandLine
```

The verifier builds normal and forced missing-Resources variants, checks repeat Build/Rebuild/Cleanup behavior, finite bounds and transforms, forbidden components, source/fallback accounting, protected clearances, visual/collider separation, climb-route reachability, distinct silhouettes, centralized culling metadata, material sharing, and all published budgets.
