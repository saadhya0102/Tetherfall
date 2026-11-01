# Tetherfall transition biomes

Transition biomes are soft, authored seam corridors. They are **not progression regions** and never add a fourteenth region identity. Quests, discovery XP, Pact sources, saves, Threadways, survey boons, and `CurrentRegion` continue to use one of the thirteen parent region names. A transition name is ambient locale copy only.

`Assets/Scripts/TetherfallTransitionBiomes.cs` owns the immutable 12 v1 definitions, allocation-free segment projection, locale hysteresis, presentation blends, soft map-band data, semantic seam content, and bounded streaming. `Assets/Editor/TetherfallTransitionBiomesVerification.cs` owns the focused edit-mode checks.

## 12 v1 seams

The completed v1 catalog contains exactly 12 seams. The first two follow the legacy `x=±80` borders. The other ten use the current region-pack connector endpoints without moving those connectors.

| Stable ID | Ambient locale | Parent A | Parent B | Path |
|---|---|---|---|---|
| `transition.alderwake.morrowfen` | Alderwake–Morrowfen Verge | Alderwake Basin | Morrowfen | `x=80`, `z=-100..100` |
| `transition.alderwake.cinderwold` | Alderwake–Cinderwold Verge | Alderwake Basin | Cinderwold Verge | `x=-80`, `z=-100..100` |
| `transition.alderwake.glassmere` | Alderwake–Glassmere Verge | Alderwake Basin | Glassmere Steppe | `(0,128)→(0,152)` |
| `transition.morrowfen.rimecoil` | Morrowfen–Rimecoil Verge | Morrowfen | Rimecoil Range | `(170,128)→(170,152)` |
| `transition.cinderwold.sunken_weald` | Cinderwold–Sunken Weald Verge | Cinderwold Verge | Sunken Weald | `(-170,128)→(-170,152)` |
| `transition.cinderwold.prismscar` | Cinderwold–Prismscar Verge | Cinderwold Verge | Prismscar Downs | `(-90,-128)→(-90,-152)` |
| `transition.morrowfen.galecradle` | Morrowfen–Galecradle Verge | Morrowfen | Galecradle Mesa | `(100,-128)→(100,-152)` |
| `transition.cinderwold.lumenwaste` | Cinderwold–Lumenwaste Verge | Cinderwold Verge | Lumenwaste Reach | `(-238,120)→(-308,120)` |
| `transition.morrowfen.brassroot` | Morrowfen–Brassroot Verge | Morrowfen | Brassroot Deeps | `(238,110)→(292,110)` |
| `transition.sunken_weald.cloudbarrow` | Sunken Weald–Cloudbarrow Verge | Sunken Weald | Cloudbarrow Expanse | `(-230,288)→(-230,322)` |
| `transition.rimecoil.nightloom` | Rimecoil–Nightloom Verge | Rimecoil Range | Nightloom Hollow | `(225,288)→(225,322)` |
| `transition.prismscar.tideglass` | Prismscar–Tideglass Verge | Prismscar Downs | Tideglass Strand | `(-21,-288)→(-21,-342)` |

The legacy definitions blend across their north-south border segments. Connector definitions blend along the connector from parent A to parent B. `TryProject` is deterministic and allocation-free: it uses horizontal dot products, clamped segment projection, and a cubic smoothstep. It performs no physics query or LINQ operation.

## Locale identity

`RegionLocaleQuery` separates progression identity from sensory locale:

- `ProgressionRegion` is always parent A or parent B.
- `AmbientLocaleName` may be the authored Verge name while hysteresis is active.
- `TransitionId` is the stable `transition.*` identity.
- `BlendT` is finite and monotonic from parent A (`0`) to parent B (`1`).
- `InSeamCore` identifies the narrow center used to enter ambient-locale hysteresis.

Call:

```csharp
var state = default(TransitionLocaleHysteresisState);
if (TetherfallTransitionBiomes.TryGetLocale(
        playerPosition,
        alreadyResolvedParentRegion,
        ref state,
        out var locale))
{
    // Use locale.ProgressionRegion for CurrentRegion, saves, quests, and discovery.
    // Use locale.AmbientLocaleName for presentation-only copy.
}
```

The state enters in a seam core and leaves only beyond the soft corridor plus an eight-unit pad. It is designed for `0.25s` polling. A transition ambient name must never be inserted into `discoveredRegions` or returned as a progression ID. The canonical parent name `Cinderwold Verge` remains valid; only the exact authored transition locale names are excluded.

## Presentation and map hooks

`TryGetBlendedPresentation(locale, out presentation, out fogDensity)` interpolates parent ambient, fog, sun, sky, ground, and fog density. It returns a normal `RegionPresentation.Definition`, so presentation integration does not need a parallel renderer.

`TryGetMapSeamBand(index, mapRect, out band)` exposes three feather layers per definition. Each band is a projected line segment with thickness and alpha. Draw the wide, low-alpha layer first and the narrow layer last. This produces a soft capsule-like seam and does not create hard rectangular transition tiles.

The runtime director also exposes `CurrentLocale`, refreshed at the same `0.25s` cadence as streaming.

## World content

Call `TetherfallTransitionBiomes.Build(game, worldRoot)` after region packs and any annex connectors are registered. Build is idempotent and adds missing local-registry seams on a later call.

Each seam creates:

- A streamed corridor shell with one safe solid route bed and two non-solid feather shoulders.
- One shared, softly blended parent-ground material and shared cached materials for all pieces.
- Six non-solid landmark/dressing renderers, below the cap of eight.
- Original Tetherfall art direction: restrained teal Threadglass, copper routework, and paired parent-ecology forms.
- Exactly four mixed-parent resource interactables.
- One `EnemyCamp` using a valid existing zero-light wild encounter definition and a semantic persistence ID.
- One semantic transition secret with a Threadglass memory crest.
- Zero added lights.

Connector beds remain aligned to the existing connector width and route. Resources and the landmark sit at the bed edges, while the camp and secret are offset back onto parent-owned ground. Existing connector geometry remains available even while a transition shell is asleep.

### Semantic IDs

No transition content depends on coordinates:

- Seams: `transition.*`
- Resources: `resource.transition.*`
- Camps: `encounter.transition.*`
- Secrets: `secret.transition.*`

Resources and secrets reuse the existing recovered-content save channel without counting as Field Archive recoveries. Camps reuse existing first-clear persistence through `EnemyCamp`. Verge locale names are never save IDs.

## Streaming and budgets

One director uses fixed candidate arrays and no per-poll allocations.

- Poll interval: `0.25s`
- Active static shells: at most `3`
- Active detail roots: at most `2`
- Static prewake: `92` units; sleep: `116` units
- Detail wake: `52` units; sleep: `70` units
- Landmark/dressing renderers per seam: `6` of `8`
- Solid route colliders per seam: `1` of `4`
- Resource interactables per seam: exactly `4`
- Added lights: `0`

Static shells prewake before detail ecology. Different enter and sleep distances prevent boundary thrash. Resource, secret, camp, and any encounter updates live below the detail root, so Unity runs no component updates while that root is inactive.

## Annex compatibility

The immutable v1 array never changes. Additional connector seams live in the local transition registry.

`RegisterAnnexConnectorSeam(...)` is the convenience API for parent-to-annex connectors. It assigns the parent name to both progression sides, so an annex and its parent keep the **same progression region** while their presentation palettes can blend. It derives semantic resource, camp, and secret IDs from the supplied `transition.*` ID.

For a more specialized annex-to-neighbor seam, construct an immutable `TransitionBiomeDefinition` and call `RegisterAdditionalSeam`. Registration rejects duplicate v1 IDs, missing encounters, non-semantic IDs, malformed segments, and resource counts other than four.

Register annex seams before the first build when possible:

```csharp
TetherfallTransitionBiomes.RegisterAnnexConnectorSeam(
    "transition.alderwake.north_annex",
    "Alderwake Basin",
    "Alderwake Basin",
    "Alderwake North Annex",
    "Alderwake Northweave",
    "Rainroot terraces lengthen without becoming another region.",
    connectorStart,
    connectorEnd,
    connectorWidth,
    ResourceKind.Resin,
    ResourceKind.Fiber,
    "encounter.wild.alderwake.mossbell_striders",
    mapTint);

TetherfallTransitionBiomes.Build(game, worldRoot);
```

## Parent integration checklist

The self-contained implementation deliberately does not edit shared files. After concurrent regional work lands, add these hooks:

1. World construction: call `TetherfallTransitionBiomes.Build(game, worldRoot)` after packs and annex registration.
2. Region resolution: resolve pack/annex parent identity first, call `TryGetLocale`, and return only `locale.ProgressionRegion` from `ResolveRegionName`.
3. Presentation: retain one `TransitionLocaleHysteresisState` at the `0.25s` poll site and feed the result to `TryGetBlendedPresentation`.
4. HUD/discovery: show `AmbientLocaleName` as presentation-only copy; never award region discovery or XP for it.
5. World map: iterate `MapSeamBandCount` and draw data from `TryGetMapSeamBand` over parent tiles without a transition rectangle or hard outline.
6. Full verification: add `TetherfallTransitionBiomesVerification.VerifyForFullGame` to the full-game verifier.
7. Annex reconciliation: register each new annex connector before Build, then keep its progression parent identical on both sides.

## Verification

Run `Tetherfall > Verify Transition Biomes`.

The verifier checks the exact 12-ID set, unique semantic resources/camps/secrets, current connector alignment, finite monotonic `BlendT`, locale enter/leave hysteresis, parent-only progression identity, renderer/collider/resource/light caps, 3/2 streaming limits, map-projectable endpoints and feather bands, allocation-sensitive source rules, annex registration API, and this documentation.

Command-line and aggregate entry points are available as:

- `TetherfallTransitionBiomesVerification.VerifyFromCommandLine`
- `TetherfallTransitionBiomesVerification.VerifyForFullGame`
