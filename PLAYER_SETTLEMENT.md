# Player settlement module

This module is an isolated, persistent player-built settlement. It does not
replace Alderwake restoration or the portable camp, and none of those systems
are read or modified.

## New files

- `Assets/Scripts/TetherfallPlayerSettlement.cs`
- `Assets/Scripts/TetherfallSettlementCatalog.cs`
- `Assets/Scripts/TetherfallSettlementPlacement.cs`
- `Assets/Scripts/TetherfallSettlementPersistence.cs`
- `Assets/Scripts/TetherfallSettlementPactWorkers.cs`
- `Assets/Editor/TetherfallPlayerSettlementVerification.cs`
- `PLAYER_SETTLEMENT.md`

## Integration status

The runtime is source-integrated after the Pact roster at parent save schema
24. The requested Unity verification pass has not been run yet.

- [x] Initialize `InitializePactRosterSystem()` and then
  `InitializePlayerSettlementSystem()` before save loading.
- [x] Update the Pact roster and then the player settlement before ordinary
  interaction, attack, camp, and mount input.
- [x] Include `IsPlayerSettlementInputCaptured` in gameplay pause and central
  modal predicates, with one-frame close/cancel capture for `Esc` and `RMB`.
- [x] Save `WritePactRosterSave(data)` and then
  `WritePlayerSettlementSave(data)` before disk persistence.
- [x] Restore `RestorePactRosterState(data)` and then
  `RestorePlayerSettlementState(data)` before `isRestoringSave` becomes false.
- [x] Render the panel or placement status from central `OnGUI` with responsive
  scaling, overlay exclusivity, and GUI-matrix restoration.
- [x] Add the remappable `OpenSettlementPlanning` action with default `N`,
  controls/settings/tutorial copy, useful blocked reasons, and toggle/`Esc`
  close behavior.
- [x] Normalize `playerSettlement` with safe UTC ticks, preserve old-save
  migration, and confirm JSON sees both `pactRoster` and `playerSettlement`.
- [x] Reject the one Threadheart core from bounded world edges, both cities,
  Threadway arrivals, dungeon and encounter footprints, and story restoration
  footprints through catalog-based checks.
- [x] Keep Pact no-warp safety aware of the claimed Threadheart.
- [x] Chain progression, Pact roster, settlement art, and player-settlement
  verification into the non-Play-Mode full-game verifier.

No scene wiring or per-piece update component is required.

## Public integration hooks

Required lifecycle and panel surface:

- `InitializePlayerSettlementSystem()`
- `UpdatePlayerSettlementSystem()`
- `WritePlayerSettlementSave(TetherfallSaveData data)`
- `RestorePlayerSettlementState(TetherfallSaveData data)`
- `OpenPlayerSettlementPanel()`
- `ClosePlayerSettlementPanel()`
- `DrawPlayerSettlementPanel(Rect rect)`
- `CanOpenPlayerSettlement`
- `GetPlayerSettlementPlanningBlockReason()`
- `GetPlayerSettlementHudHint()`
- `IsPlayerSettlementInputCaptured`
- `IsPlayerSettlementPanelOpen`
- `IsPlayerSettlementPlacementActive`

Build and maintenance surface:

- `BeginPlayerSettlementPlacement(string definitionId, out string failure)`
- `CancelPlayerSettlementPlacement()`
- `TryUpgradePlayerSettlementCore(out string failure)`
- `TryRepairPlayerSettlementPiece(string instanceId, out string failure)`
- `DamagePlayerSettlementPiece(string instanceId, int amount)`
- `TryRestAtPlayerSettlementBed(string instanceId, out string failure)`
- `TryDemolishPlayerSettlementPiece(string instanceId, out string failure)`
- `GetPlayerSettlementCorePosition()`

Storage and production surface:

- `GetPlayerSettlementStorage(ResourceKind kind)`
- `GetPlayerSettlementStorageCapacity()`
- `TryDepositPlayerSettlementResource(...)`
- `TryWithdrawPlayerSettlementResource(...)`
- `TryQueuePlayerSettlementRecipe(...)`
- `CancelPlayerSettlementRecipe(string stationPieceId)`

Pact assignment surface:

- `TryAssignPlayerSettlementWorker(pactInstanceId, role, workPieceId, bedPieceId, out failure)`
- `TrySetPlayerSettlementWorkerBed(...)`
- `UnassignPlayerSettlementWorker(string pactInstanceId)`
- `GetPlayerSettlementWorkerAssignments()`
- `GetPlayerSettlementWorkerEfficiency(string pactInstanceId)`
- `RegisterPlayerSettlementWorkerProxy(ITetherfallSettlementWorkerProxy proxy)`
- `UnregisterPlayerSettlementWorkerProxy(string pactInstanceId)`

Assignments are keyed by the stable `CapturedPactSaveData.instanceId` exposed
through `TetherfallGame.PactRoster`, not by a species-level `PactId`. The
settlement itself is always identified as
`settlement.player.threadheart`; display text is never used as authority.

The Workers tab is a dedicated Accord roster view. Its bounded scroll shows
the Pact display and species names, authored work affinities, stamina, morale,
and Field, Reserve, or Settlement state. It explains Concord and the
Rank 2/4/6 total field capacities of 3/4/5. Selecting an Accord exposes only
operational placed work pieces that match an authored affinity and only free,
operational Pact beds. Prev and Next cycle through those bounded choices.

Assignment is transactional. The module validates both local slots, calls
`TryAssignPactSettlementPreference(instanceId, settlementId, stationId, bedId,
matchedWorkKind, out reason)`, and only then adds or changes the settlement
record. A failed second-side commit restores the prior Field, Reserve, or
Settlement preference. Unassignment and demolition call
`ClearPactSettlementPreference`. Bed changes use the same transaction, so
`CapturedPactSaveData.bedId` cannot drift from the settlement worker record.

### Work preference mapping

Settlement roles accept only the following authored `PactWorkKind`
preferences. The highest-rated matching affinity is passed to the Pact roster:

- Builder: Crafting or Salvage
- Smith: Heatkeeping, Crafting, or Salvage
- Cook: Heatkeeping, Foraging, or Care
- Weaver: Crafting
- Healer: Care or Foraging
- Farmer: Cultivation or Foraging
- Water tender: Waterkeeping or Care
- Rancher: Care, Foraging, or Cultivation
- Watchkeeper: Watch or Survey
- Hauler: Courier or Salvage

An Accord with no positive authored affinity in the role's set is rejected.
Catalog definitions remain read-only.

## Controls

The remappable `OpenSettlementPlanning` action defaults to `N`. It opens or
closes the panel when no other field panel is active; after the Threadheart is
claimed, the Wayfinder must be inside its management radius. `Esc` also closes
the panel. The HUD gives a compact availability/nearby line or a compass
direction and distance to an existing core.

During ghost placement:

- `LMB`: commit a valid placement
- settlement-planning action, `RMB`, or `Esc`: cancel
- Mouse wheel or `R`: rotate
- Structural pieces: 90-degree steps and the core-relative 2 m grid/sockets
- Furnishings, stations, agriculture, and decorations: 15-degree steps

Legacy `Input` calls are isolated in
`TetherfallSettlementLegacyPlacementInput`. The panel itself uses the existing
remappable cancel action.

The ghost uses the main camera ray, five ground samples, per-piece slope limits,
claim bounds, and `Physics.OverlapBoxNonAlloc`. Its status text gives the
specific failure: missing ground, excessive slope or height variation, blocked
space, progression gate, resource shortage, piece cap, or claim-radius breach.

## Threadheart and hard bounds

There is exactly one stable core:

- Definition ID: `settlement.threadheart.core`
- Placed instance ID: `settlement.instance.threadheart.core`
- Tier 1: 48 m radius, 96 pieces, 8 workers, 200 base storage
- Tier 2: 64 m radius, 160 pieces, 16 workers, 320 base storage
- Tier 3: 80 m radius, 256 pieces, 24 workers, 500 base storage

Global hard caps are 256 pieces, 24 workers, 4,000 stored resource units, 100
queued cycles per station, and 2,048 production cycles per simulation advance.
The first claim must be within 12 m of the player. Vertical construction is
bounded to 24 m from the core.

`TetherfallSettlementForbiddenZones` rejects a core outside the finite world
bound or within static catalog-driven clearances around Alderwake, Veyrwater,
Threadway arrivals, modular/Skyvault dungeons, legacy and regional encounters,
route events, bounties, and story/restoration footprints. It performs no
scene-wide search.

The stable catalog covers all 28 first-pack roles: Threadheart core; timber and
stone foundations; straight, window, doorway, and gate walls; floor, stair,
ramp, and pitched roof; Pact and Wayfinder beds; chest and pallet storage;
workbench, forge, cookpot, loom, and medicine table; planter, farm plot, water
collector, and feeding trough; watch post, lantern post, fence, and banner.
Every entry has `ResourceCost` construction recipes and explicit Wayfinder/core
progression gates.

## Runtime and production policy

Every placed piece has a bounded, deliberate gameplay-collider set separate
from its art. Most use one root `BoxCollider`. Doorways and gates use three
simple colliders around a clear opening; stairs/ramps and pitched roofs use
small rotated collider children so navigation is not blocked by a solid
footprint box. Art remains collider-free. Pieces implement `IWorldInteractable`
but have no `Update`; the game partial owns all placement, distance,
assignment, and production ticks.

- At or below 110 m: one simulation tick per second. Assigned captured wild
  Pacts may receive visible worker proxies.
- From 110 m through 180 m: one simulation tick per 10 seconds. Built-in
  worker proxy GameObjects are destroyed.
- Beyond 180 m: fully abstract 10-second simulation; piece renderers and
  gameplay colliders are disabled, with no worker proxy GameObjects.
- UTC catch-up is capped at 8 hours. Each worker and each total advance also
  has a hard cycle bound.

`TetherfallSettlementPactWorkerProxy` implements
`ITetherfallSettlementWorkerProxy` and registers through
`RegisterPlayerSettlementWorkerProxy`. The central settlement update owns the
bounded list and calls `TickCentralized`; the component has no `Update`.
`TetherfallCapturedPactArt.Build` supplies the articulated rig, and
`CapturedPactArtRig.Tick` supplies motion. Proxies move and turn smoothly
toward deterministic station work sockets. They do not combat, follow, warp,
or use physics. Collider, light, and rigidbody components are removed at
creation. Missing records, unassignment, demolition, and leaving the near band
destroy the proxy. The count cannot exceed the 24-worker settlement hard cap.

Workbench, forge, cookpot, loom, and medicine-table recipes consume settlement
storage only and carry explicit core-tier/Wayfinder-rank gates. Farms and water
collectors run automatically when a matching worker is assigned. Food, water,
Pact beds, feeding troughs, watch coverage, morale, stamina, station damage,
and core progression affect efficiency. The Wayfinder bed provides a local
rest action. Maintenance output can pay full repair recipes before the
Wayfinder bag is checked.

Settlement worker stamina and morale are production caches, not a second
authority. Assignment initializes both from the mutable Pact roster record.
Each production simulation, offline catch-up, and save clamps them to 0-100
and writes them back through `TetherfallSettlementPactBridge`. The settlement
save hook also patches the Pact roster snapshot created earlier in the same
save transaction.

Settlement storage is separate from the player bag. Deposit and withdrawal are
explicit. Placement and bag-funded repair use the existing
`CanAfford`/`SpendCost` path. Demolition returns an exact rounded-down 60% of
construction `ResourceCost` values to the Wayfinder bag and clears affected
worker assignments on both sides, plus jobs and proxies. A work piece or Pact
bed that reaches zero durability also clears its affected assignments.

## Save fields

Parent schema 24 exposes both `TetherfallSaveData.pactRoster` and
`TetherfallSaveData.playerSettlement` to `JsonUtility`.
`playerSettlement` contains one `SettlementSaveData` object:

- `version`, `coreClaimed`, `coreTier`, `lastSimulatedUtcTicks`
- `nextPieceSequence`
- `foodUnits`, `waterUnits`, `maintenanceUnits`
- `storage`: `ResourceKind` and amount
- `pieces`: stable instance/definition IDs, position, Y rotation, durability,
  and deterministic visual seed
- `workers`: Pact instance ID, work role, work-piece ID, bed ID, morale,
  stamina, production/need progress, and last-meal state
- `stationJobs`: station-piece ID, recipe ID, queued cycles, and progress

Restore normalization drops unknown and duplicate pieces, duplicate cores,
non-finite transforms, out-of-claim construction, invalid assignments and
recipes, and over-cap records. It clamps all scalar stock, durability,
rotation, UTC, queue, progress, worker, piece, and storage values.

## Art adapter

The concurrent art module became available during implementation. Its confirmed
signature is `TetherfallSettlementArt.Build(artId, parent)`, not the anticipated
`TetherfallSettlementArt.Build(parent, artId, seed)` overload. The single
`TetherfallSettlementArtAdapter.Build` seam maps all 28 gameplay entries
one-to-one to confirmed `alderwake.*` art IDs and invokes:

`TetherfallSettlementArt.Build(artId, parent);`

That art API is deterministic without a seed. The adapter retains the clean
collider-free, light-free bounded fallback for an unknown future art ID; no
other runtime code calls settlement art.

## Verification

Do not enter Play Mode. In the later verification pass, run the menu item
`Tetherfall > Verify Player Settlement`, or use:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\Users\hasto\rpg\Tetherfall" `
  -executeMethod Tetherfall.Editor.TetherfallPlayerSettlementVerification.VerifyFromCommandLine `
  -logFile "C:\Users\hasto\rpg\Tetherfall\Logs\player-settlement-verification.log"
```

The verifier has not been run for this integration yet. It checks schema-24
JSON visibility and real lifecycle/save/restore/input/modal/UI ordering,
forbidden-zone coverage, catalog identity and coverage, role-to-affinity mapping and
mismatch rejection, bidirectional assignment rollback, bed and demolition
cleanup, Pact vital clamping, Workers-tab reachability and bounded scrolling,
the 24-proxy budget, no per-proxy `Update`, allocation-free central proxy
ticks, forbidden-component removal, near-only proxy lifecycle, recipes,
progression gates, 48-80 m bounds, 15/90-degree rotation, the 2 m grid, 60%
refunds, save normalization, worker/job/storage caps, the 8-hour UTC cap, cycle
bounds, centralized simulation, required public hooks, camera and NonAlloc
placement queries, input isolation, socket names, confirmed art/fallback
routing, and this documentation.

## Integrated compile dependencies

- `TetherfallSaveData` is partial; its parent schema and normalization now
  initialize the partial settlement record alongside the Pact roster record.
- `TetherfallGame` is partial and supplies `IWorldInteractable`,
  `RequestSave`, `RequestCriticalSave`, `CanAfford`, `SpendCost`, resource
  access, GUI styles, notifications, player/camera access, and interactable
  registration.
- The Pact roster bridge depends on the current public `PactRoster`,
  `ActivePactFieldIds`, `TryAssignPactSettlementPreference`,
  `ClearPactSettlementPreference`, and mutable public stamina, morale,
  settlement, station, and bed fields on `CapturedPactSaveData`.
- Runtime files require UnityEngine and the existing Tetherfall runtime types.
- The verifier requires UnityEditor and therefore stays under `Assets/Editor`.
- The now-present `TetherfallSettlementArt` and
  `TetherfallSettlementArtCatalog` are compile dependencies. The adapter uses
  only their confirmed stable-string overload and keeps all art calls in one
  method.
