# Infinite Spire reusable art kit

## Scope and identity

This isolated art pass adds exactly 40 immutable `spire.*` visual IDs without
choosing the Infinite Spire gameplay architecture. Gameplay owns collision,
room generation, encounter state, interaction logic, saving, and final
placement. The kit owns visual contracts, source-safe construction, named
sockets, shared materials, presentation metadata, and verification.

The original hero silhouette is a stepped civic drum stack in pale ash stone.
Four external copper lift ribs make civic infrastructure readable at skyline
distance. One active teal seam identifies the current Accord route. Teal does
not fill walls, amber is reserved for retreat/reset safety, and copper never
signals danger. An unmodified Quaternius tower must never become the hero.

All roots use one Unity unit per metre, a ground-contact pivot, +Y up, +Z
forward, local identity on first build, and collider-free visual geometry.

## Immutable catalog

The numeric enum value and stable string on every line are serialization
contracts. Do not renumber or reuse them.

### Exterior tiers

- `100 BasePlinth` — `spire.exterior.base_plinth` — 28 × 6 × 28 m — local
  exterior — optional existing `Rock_Medium_2` Resources buttress — six snap
  sockets plus `Socket_Lift`.
- `101 CityFlange` — `spire.exterior.city_flange` — 32 × 4 × 32 m — local
  exterior — original timber, moss, and copper composition — six snaps plus
  `Socket_Lift`.
- `102 TierDrumA` — `spire.exterior.tier_drum_a` — 22 × 12 × 22 m — local
  exterior — original lower civic drum — six snaps, `Socket_Lift`, and
  `Socket_VFX`.
- `103 TierDrumB` — `spire.exterior.tier_drum_b` — 18 × 12 × 18 m — local
  exterior — original upper civic drum — six snaps, `Socket_Lift`, and
  `Socket_VFX`.
- `104 CoilRibSet` — `spire.exterior.coil_rib_set` — 24 × 24 × 24 m — local
  exterior — four original copper ribs — six snaps plus `Socket_Lift`.
- `105 CrownRing` — `spire.exterior.crown_ring` — 12 × 4 × 12 m — local
  exterior — original open copper crown — six snaps, `Socket_Lift`, and
  `Socket_VFX`.
- `106 ActiveSeam` — `spire.exterior.active_seam` — 20 × 0.3 × 20 m — local
  exterior — the one active teal seam — six snaps plus `Socket_VFX`.

### Distant proxy

- `120 DistantNeedle` — `spire.proxy.distant_needle` — 18 × 88 × 18 m —
  original shadow/copper mass proxy — no gameplay sockets.
- `121 DistantCrown` — `spire.proxy.distant_crown` — 12 × 5 × 12 m —
  original copper/teal crown proxy — no gameplay sockets.

### Facades

- `200 FacadeStraight` — `spire.facade.straight` — 4 × 3 × 0.5 m — preferred
  `Wall_Plaster_Straight` Resources geometry with an original copper repair
  seam — complete procedural panel fallback — six snaps.
- `201 FacadeWindow` — `spire.facade.window` — 4 × 3 × 0.5 m — preferred
  `Wall_Plaster_Window_Wide_Round` with an original copper sill — procedural
  open-window fallback — six snaps.
- `202 FacadeDoorBay` — `spire.facade.door_bay` — 4 × 3 × 0.6 m — preferred
  `Wall_UnevenBrick_Door_Round` with an original copper lintel — procedural
  clear-opening fallback — six snaps plus `Socket_Door`.
- `203 FacadeArch` — `spire.facade.arch` — 4 × 3.2 × 0.7 m — preferred
  `Wall_Arch` with an original copper crown — procedural civic arch fallback —
  six snaps plus `Socket_Door`.

### Room shells

- `300 ShellStandard` — `spire.shell.standard` — 16 × 6 × 16 m — procedural
  retaining shell — six snaps, `Socket_Door`, `Socket_Stair_Up`,
  `Socket_Stair_Down`, and `Socket_Lift`.
- `301 ShellCombat` — `spire.shell.combat` — 20 × 6 × 20 m — open-center
  combat shell — six snaps, `Socket_Door`, and `Socket_VFX`.
- `302 ShellPuzzle` — `spire.shell.puzzle` — 16 × 6 × 16 m — shape-coded
  puzzle shell — six snaps, `Socket_Door`, `Socket_Puzzle`, and `Socket_VFX`.
- `303 ShellRest` — `spire.shell.rest` — 18 × 6 × 18 m — warm Coilhaven room
  shell — six snaps, `Socket_Door`, `Socket_Rest`, `Socket_Work`, and
  `Socket_VFX`.

### Doors

- `400 DoorRound` — `spire.door.round` — 4 × 3.2 × 0.6 m — preferred
  `Wall_Plaster_Door_Round` with a copper threshold — procedural clear-opening
  fallback — six snaps plus `Socket_Door`.
- `401 DoorSeal` — `spire.door.seal` — 4 × 3.2 × 0.6 m — original boss/miniboss
  locking seal — six snaps, `Socket_Door`, and `Socket_BossGate`.
- `402 DoorRetreat` — `spire.door.retreat` — 4 × 3.2 × 0.6 m — original amber
  retreat frame — six snaps, `Socket_Door`, and `Socket_Rest`.

Every passable door contract reserves at least 1.4 m width and 2.4 m height.
The sealed door is intentionally a state-controlled visual barrier.

### Stairs and lifts

- `500 StairStraight` — `spire.stair.straight` — 4 × 3 × 4 m — original
  three-step broad stair — six snaps, `Socket_Stair_Up`, and
  `Socket_Stair_Down`.
- `501 StairPlatform` — `spire.stair.platform` — 4 × 3 × 4 m — original timber
  landing with copper rails — six snaps and both stair sockets.
- `502 LiftCage` — `spire.lift.cage` — 3 × 3.6 × 3 m — original open copper
  cage — six snaps, `Socket_Lift`, and `Socket_VFX`.
- `503 LiftGuide` — `spire.lift.guide` — 3 × 3 × 0.5 m — original restrained
  teal guides — six snaps, `Socket_Lift`, and `Socket_VFX`.

These are visual clearances only. Gameplay supplies authored stair, ramp, or
lift collision on a separate parent.

### Puzzle props

- `600 CoilDial` — `spire.puzzle.coil_dial` — 2.5 × 2.5 × 1.5 m — circle and
  pointer geometry — `Socket_Puzzle`, `Socket_Work`, and `Socket_VFX`.
- `601 SeamLatch` — `spire.puzzle.seam_latch` — 3 × 2.6 × 1.5 m — LOW/MID/HIGH
  height redundancy plus amber reset lever — puzzle/work/VFX sockets.
- `602 ResonancePlinth` — `spire.puzzle.resonance_plinth` — 2.5 × 2.8 × 2.5 m
  — rotated shard and indexed base — puzzle/work/VFX sockets.
- `603 PressureTile` — `spire.puzzle.pressure_tile` — 2.5 × 0.35 × 2.5 m —
  square, diamond, and circular shape layers — `Socket_Puzzle` and
  `Socket_VFX`.

Puzzle meaning is redundant in shape and height. Color is never the only cue.

### Arenas and thresholds

- `700 MinibossRing` — `spire.arena.miniboss_ring` — 24 × 7 × 24 m —
  open-center ring with side-only pillars — six snaps, `Socket_BossGate`, and
  `Socket_VFX`.
- `701 BossThreshold` — `spire.arena.boss_threshold` — 10 × 8 × 2 m — pale
  piers, copper header, and collider-free Glass Mist veil —
  `Socket_BossGate` and `Socket_VFX`.
- `702 BossArenaFloor` — `spire.arena.boss_floor` — 32 × 8 × 32 m —
  telegraph-safe open center with side masses — six snaps, `Socket_BossGate`,
  and `Socket_VFX`.

### Treasure and service

- `800 CacheChest` — `spire.prop.cache_chest` — 1.8 × 1.2 × 1.3 m — preferred
  `Crate_Wooden` with original copper band and shape latch — full procedural
  chest fallback — treasure/work/VFX sockets.
- `801 RepairBench` — `spire.prop.repair_bench` — 3 × 1.8 × 1.8 m — preferred
  `Workbench` plus original tools and ready seam — complete procedural bench
  fallback — `Socket_Work` and `Socket_VFX`.
- `802 WaymarkBanner` — `spire.prop.waymark_banner` — 1.8 × 3.5 × 0.6 m —
  original canvas/copper waymark — work/VFX sockets.

### Coilhaven rest pieces

- `900 CoilhavenCore` — `spire.rest.coilhaven_core` — 8 × 5 × 8 m — original
  warm timber deck, canvas vault, copper coil hearth, moss apron, and one calm
  teal seam — rest/work/VFX sockets.
- `901 CoilhavenBed` — `spire.rest.coilhaven_bed` — 3 × 1.4 × 3.5 m —
  preferred `Bench` beneath original timber/canvas rest layers — full
  procedural bed fallback — rest/work/VFX sockets.
- `902 CoilhavenHearth` — `spire.rest.coilhaven_hearth` — 3.5 × 2.2 × 3.5 m —
  preferred `Cauldron` beneath an original copper coil and amber retreat-safe
  focus — complete procedural hearth fallback — rest/work/VFX sockets.

### Spirecoil city vendor pieces

- `1000 SpirecoilStall` — `spire.city.spirecoil_stall` — 5 × 4 × 4 m —
  preferred `Stall_Empty` beneath original canvas and copper coil uprights —
  complete procedural stall fallback — vendor/work/VFX sockets.
- `1001 SpirecoilCounter` — `spire.city.spirecoil_counter` — 3.5 × 1.7 × 1.8 m
  — preferred `Table_Large` with original route rail and shape marker —
  complete procedural counter fallback — vendor/work/VFX sockets.
- `1002 SpirecoilBeacon` — `spire.city.spirecoil_beacon` — 1.2 × 4 × 1.2 m —
  original pale stone, copper mast, and teal tip with no `Light` component —
  `Socket_Vendor` and `Socket_VFX`.

## Original shared material cache

`TetherfallSpireArt` caches ten original runtime materials, below the
twelve-key ceiling:

- Pale Ash Stone
- Deep Stone
- Weathered Timber
- Civic Copper
- Restrained Teal
- Coilhaven Canvas
- Contact Moss
- Amber Retreat
- Glass Mist
- Shadow Proxy

Renderers always receive `sharedMaterial`. Existing Quaternius materials stay
on instantiated source meshes and are never recolored, rebaked, renamed, or
claimed as original Tetherfall work.

## Budget classes

- Structural facade, door, or stair piece: at most 3 renderers and 3 shared
  materials.
- Station or puzzle prop: at most 8 renderers and 6 shared materials.
- Room shell: at most 24 renderers and 6 shared materials.
- Arena or threshold: at most 32 renderers and 6 shared materials.
- Hero rest or vendor piece: at most 16 renderers and 8 shared materials.
- Local exterior definition: at most 48 renderers and 8 shared materials.
- Distant proxy definition: at most 6 renderers and 3 shared materials; the
  combined two-piece proxy is capped at 6 renderers and 3 materials.
- Concurrent verification fixture: at most 220 renderers, at most 12 original
  shared materials, zero visual colliders, and zero lights.

All source-backed definitions reject missing, unsafe, or over-budget Resources
content and select their complete procedural fallback. Every ID also exposes a
forced procedural fallback path for deterministic testing.

## Lifecycle API

Build one visual beneath a gameplay-owned parent:

```csharp
var visual = TetherfallSpireArt.Build(
    TetherfallSpireArtId.ShellStandard,
    gameplayRoot.transform);
```

Use stable strings for data-driven room plans:

```csharp
if (!TetherfallSpireArt.TryBuild(
        "spire.puzzle.coil_dial",
        gameplayRoot.transform,
        out var visual))
{
    throw new InvalidOperationException("Unknown Infinite Spire art ID.");
}
```

Repeated `Build` returns the existing direct child. `Rebuild` refreshes the
deterministic recipe in the same root while preserving caller placement.
`Clear`, string-ID `Clear`, and `ClearAll` are repeat-safe. Explicit fallback is
available through:

```csharp
var fallback = TetherfallSpireArt.BuildProceduralFallback(
    TetherfallSpireArtId.SpirecoilStall,
    gameplayRoot.transform);
```

## Central presentation

Build and place a batch, then create one central controller:

```csharp
var controller = TetherfallSpireArt.EnsurePresentationController(
    presentationRoot,
    Camera.main);
controller.RebuildGroups(Camera.main);
```

The controller owns CullingGroups rather than per-piece distance polling:

- Local exterior and facade metadata uses 70 m / 145 m.
- Coilhaven and Spirecoil vendor metadata uses 70 m / 110 m.
- The distant proxy is mutually exclusive with the local exterior. Near shows
  local art only; beyond the exterior cull band, the proxy alone is enabled.
- Interior pieces are distance-independent and activation-ready. A future room
  director calls `SetInteriorActive(piece, active)` or
  `SetAllInteriorsActive(active)`.

Neither art metadata nor the central controller declares `Update`,
`LateUpdate`, or `FixedUpdate`.

## Source and provenance note

No external download was performed. Optional sources are only existing
Resources prefabs under `Assets/Resources/Art/Quaternius`, built from the
already preserved Quaternius CC0 subset. No unverified resource string points
directly at an imported-only model. Source meshes retain their third-party
materials and authorship; original overlays, fallback geometry, catalog
contracts, sockets, culling policy, and hero composition are Tetherfall work.

`ART_PROVENANCE.md` intentionally remains unchanged in this isolated task.
Parent integration should add the final Infinite Spire runtime-art entry after
the gameplay architecture and integration location are accepted.

## Verification

Use `Tetherfall/Verify Infinite Spire Art Kit` in Edit Mode after changing a
definition, recipe, source name, socket, material, or presentation contract.
Automation entry points are
`TetherfallSpireArtVerification.VerifyFromCommandLine` and
`TetherfallSpireArtVerification.VerifyForFullGame`. This task does not edit the
full-game verifier.

The focused verifier checks exact enum/catalog identity, source allowlisting,
normal and forced fallback builds, finite transforms and bounds, pivot and
forward, sockets, forbidden components, material immutability and sharing,
budget classes, full concurrent load, cleanup, culling bands, proxy exclusion,
interior activation, documentation, and provenance-safe naming.

## Integration and visual-inspection risks

Static verification cannot judge final fog silhouette, night contrast, actual
camera occlusion, imported wall scale at every viewing angle, lift-rib
tangencies after world placement, Glass Mist sorting against combat effects,
or whether the Coilhaven/vendor compositions feel sufficiently distinct in the
final lighting rig. Inspect the assembled hero from gameplay, skyline,
top-down, silhouette-only, fog, and night views before shipping. Also inspect
door clearance, stair readability, arena telegraph space, interaction reach,
and repeated-room cloning once gameplay placement exists.
