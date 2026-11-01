# Tetherfall content authoring

The runtime is split so new content does not require changes to combat AI or the main game loop.

## Add an enemy archetype

Open `Assets\Scripts\TetherfallCatalogs.cs` and add one entry to `EnemyCatalog.Definitions`.

An `EnemyDefinition` controls:

- stable save/content ID
- display name and color
- melee, ranged, or heavy attack style
- health, damage, XP, speed, range, detection, cooldown, and telegraph timing
- projectile speed and visual/body scale
- special-move parameters, including `groundBurstRadius` for snapshot ground zones

`Enemy` in `TetherfallActors.cs` reads those values. Movement, collision, attack coordination, projectiles, damage, death, loot, and camp reporting are reusable and should not be copied into a new enemy class for a stat-only variant. `GroundBurst` snapshots the player’s ground position at windup, leaves an exact-radius non-solid marker, validates the caster-to-marker path at detonation, and uses the existing line-of-sight-aware shockwave for damage. Keep future ground attacks snapshot-based and cancel their marker on stagger or defeat.

### Threat presentation

`TetherfallThreatPresentation` reads the shared enemy attack style, elite flag, live scaled threat band, vitality, poise, and typed combat status. Do not add an enemy-owned HUD or `OnGUI` method. New attack styles need a distinct central archetype mark, while new statuses need an explicit remaining-duration API and a documented priority in the director.

The director owns four pooled outline hulls and at most four world plates. Keep hostile visuals collider-free and light-free, use the shared outline material with property blocks, and resolve hostile color through `TetherfallAccessibilityRuntime`. Threat bands must continue to come from the scaling values that combat actually applied.

Run `Tetherfall > Verify Threat Presentation` after changing hostile status APIs, scaling bands, plate layout, or outline construction.

### Author a wild Threadbind encounter

Wild Echosworn eligibility is opt-in and exact. Add a `PactSpeciesDefinition` to `TetherfallAccordSpeciesCatalog`, then add an `EnemyDefinition` whose ID exactly matches its dedicated `enemy.wild.*` `eligibleEnemyId`. Never alias a human, generic hostile, elite, boss, dungeon summon, or legendary world Pact into this namespace.

Use `TetherfallCapturedPactArt` for the creature silhouette and keep the normal `Enemy` controller, health, collider, telegraphs, camp death report, and respawn flow. Eligible defeat creates a 24-second rescue interaction keyed by enemy ID and rounded spawn position; defeating an Echosworn never adds it directly to the roster. Place dedicated `encounter.wild.*` groups away from cities, Threadways, main roads, and hostile-camp footprints. Keep each group to one or two creatures and use a respawn longer than the rescue window.

Rescued-wild reserve records have no actor. Active rescued Pacts use `CapturedPactActor`; linked non-mount story Pacts use their existing `PactCreature` as an adapter, deactivated and frozen while reserved. Slateback and human companions are separate from the maximum five-Pact combat roster.

## Extend expedition combat

`TetherfallExpeditionCombat.cs` owns the shared player-and-ally combat layer. `Z` cycles three saved directives: Trail keeps allies on nearby threats, Focus prioritizes the Threadglass target and increases allied damage, and Bulwark keeps the expedition defensive and reduces incoming damage while allies are present.

The player creates a short `Threadlink` window by using `G` near a target. Weapon discipline determines the response: Pathblade creates **Sever** for a stagger burst, Riftbow creates **Latch** for a spreading exposed-target burst, and Threadstaff creates **Aegis** for recovery and temporary protection. `V` during a live link begins the two-beat Threadlink Rally rather than the normal all-ally command, so the seal has a dependable player-led resolution. Before the Rally launches, `T` steers Beat 1 and `Q` steers Beat 2 through all live active-roster Pacts and field companions; outside Threadlink, `T` cycles Pact command focus. `GetThreadlinkWeaveBeatText()` supplies the expedition card's matching Beat Rail. Pact and human-companion strikes report through `OnExpeditionAllyStrike`, while dedicated Rally responses use `OnThreadlinkRallyResponse`; both keep the ally `Transform` for synchronized origin-side combat feedback.

Keep short-lived combat state out of saves. Only the player-selected directive is persistent. New reactions should remain clear, brief, and driven by an explicit player setup followed by a readable ally response.

The Expedition card lists up to five active Pacts with role and command-focus marker. Story Pact sigils follow that compatibility focus, while `HumanCompanion.SetReserve()` updates companion field state during pair rotation. New Pact or companion variants should expose a concise `TacticalAbilityShortLabel` so the expedition panel explains what `V` will ask them to do.

Rally responders must be role-safe. Offensive variants may deal a small hit or control effect, while healers and support Pacts can contribute a zero-damage weave plus a compact recovery effect. Do not modify passive-assist timers from a Rally response, and keep each response fast enough for the two beats to remain about 0.4 seconds apart.

## Author Pact Unfolding

`TetherfallPactUnfolding.cs` owns stable Pact IDs, bond-rank thresholds, greater-form branch data, source-ledger normalization, camp consent UI, save migration, and effect lookups. Every world-created Echosworn name must resolve through `TetherfallPactCatalog`; save data uses the Pact ID and form ID rather than the display name.

A greater form needs:

- a globally unique stable form ID under its Pact ID;
- a required bond rank that exists in the owning Pact's strictly increasing threshold list;
- readable tactical, passive, and traversal descriptions;
- bounded, finite effect values;
- an idempotent collider-free visual profile in `TetherfallPactFormArt`;
- a concise tactical label for the expedition card.

Bond XP is derived from the persisted one-time source ledger, not trusted as an independent reward balance. Supported sources are Pact formation, unique first-clear encounters, first discovery of each region, each camp service level, the first successful tactical/Crossweave/Threadlink response for that Pact, and Skyvault restoration. Repeated camps, repeated combat commands, loading, and reapplying a form must never pay again. Add a new source only through `PactBondSourceIds`, give it a stable ID and fixed award, and call it from an event that is itself first-completion-safe.

Rank readiness never applies a form. The player must open **Pact Unfolding** beside the portable camp, consider a branch, and confirm the persistent choice. Keep this two-step consent path intact. Traversal benefits must use existing collision-aware movement: Slateback forms may tune grounded ride values, while aerial forms may provide capped impulses or gliding support. Do not turn a bounded lift into unrestricted free flight.

`Tetherfall > Verify Pact Unfolding` covers IDs, thresholds, source-derived save normalization, invalid records, and idempotent form art. Add verifier expectations whenever the catalog or source set changes.

`TetherfallTactics.cs` owns the non-mutating command plan used by both the HUD and `TryTacticalCommand()`. New roles must implement `CanAnswerTacticalCommand()` to match their real command rules, then provide a short ability label. The plan checks cooldown, 22-stamina cost, field membership, Threadlink reachability, and valid targets before anything spends stamina or starts a cooldown.

## Extend the player settlement

The player settlement is one bounded Threadheart claim, not a second city or portable-camp variant. `TetherfallSettlementCatalog.cs` owns stable piece, station, recipe, upgrade, and player-settlement IDs plus the grid/build-radius rules. Add catalog data before runtime branches, keep every released ID stable, and preserve `settlement.player.threadheart` as the single core identity. New build pieces must use an exact `artId` implemented by `TetherfallSettlementArt`; visuals must remain finite, collider-free, light-free, and free of per-piece `Update` loops because placement collision comes from the settlement runtime.

Core placement must continue through `TetherfallSettlementForbiddenZones.CanClaimCoreAt`. When adding a city, Threadway arrival, dungeon, encounter, or story-restoration footprint, extend that bounded helper from catalog/current-position data and add a verifier case. Do not scan the scene or use display names as spatial authority. Keep the claim inside `TetherfallSettlementRules` world bounds and keep ordinary pieces inside the one core's current build radius.

`TetherfallSettlementPersistence` owns defaults, corruption repair, UTC-safe offline-production timestamps, and normalization of `TetherfallSaveData.playerSettlement`. Durable mutations belong behind `WritePlayerSettlementSave` / `RestorePlayerSettlementState`; do not create a parallel save file or store live `GameObject`, `Transform`, or actor references. Core claims, destructive removals, and upgrades use the module's existing critical-save path; routine storage, crafting, assignment, and production changes use its coalesced save path.

Pact workers remain owned by `PactRosterSaveData`. Assign and clear only stable Pact instance IDs through `TryAssignPactSettlementPreference` and `ClearPactSettlementPreference`, retain explicit consent copy, and let centralized production resolve current eligibility and capacity. Never transfer a Pact into settlement ownership, duplicate it as an actor record, or treat a missing field actor as a missing worker.

Settlement planning is the remappable `OpenSettlementPlanning` action (default `N`). Keep it independent of portable camp and crafting actions, modal while its panel or placement preview is active, and routed through the central lifecycle after Pact-roster initialization/update/save/restore. After changing settlement content or integration, run `Tetherfall > Verify Settlement Art Pack`, `Tetherfall > Verify Player Settlement`, and `Tetherfall > Verify Full Game`. These checks are wired for the later Unity verification pass; source edits alone are not a passed Unity run.

## Extend Crossweave Relays

`TetherfallCrossweave.cs` turns a real `1`/`2`/`3` combat weapon swap on a focused target into a short, transient relay. Its definitions live in `CrossweaveCatalog`: each weapon maps to one `CrossweaveAffinity` and a visual cue. The next basic attack, not `G`, can resolve the relay; Riftbow bolts retain a relay token so an old projectile cannot consume a later swap. Keep relay state transient: active targets, timers, and tokens should not enter save data.

For a new story responder, map its Pact name through `CrossweaveCatalog`; captured roster actors use their catalogued resonance affinity directly. Implement both `TriggerCrossweaveResponse` overloads and keep the response fast, role-specific, and safe across enemies and bosses. Crossweave prefers a matching command-focus Pact, then checks the remaining active roster and field companions. It validates responder distance and line of sight, reports through `OnExpeditionAllyStrike` for normal Threadlink participation, then explicitly adds `OvertonePitch.Bind` on the focused target. Do not make a Crossweave response issue a tactical cooldown or replace the two-beat Rally.

`EnemyDefinition.ashguardDuringShockwave` is an opt-in elite encounter hook. It is currently used only by Cindermaw: routine hits deal configured chip damage and cannot cancel the shockwave telegraph, while a valid responder completed through `TryResolveCrossweave` calls `TryBreakAshguardFromCrossweave` after its strike succeeds. The fracture is transient, cancels the telegraph, and grants configured exposed/stagger windows. A perfect guard can use `TryBreakAshguardFromPerfectGuard` as a generous fallback. Keep Ashguard scoped to authored elite encounters rather than changing ordinary-enemy stagger rules.

Focused combat status is intentionally semantic: `CombatTargetIndicator` reads `Enemy.AshguardRemaining`, `Enemy.ExposeRemaining`, `Enemy.StaggerRemaining`, and `DungeonBoss.StaggerRemaining` to show named state and duration without adding notification spam. Keep new target states typed with clear remaining-duration accessors; do not infer gameplay state from effect colors or source strings. Player-side expedition protection is surfaced through `GetExpeditionWardStatus()`.

## Extend the Overtone Lattice

`TetherfallOvertoneLattice.cs` owns focused-target Overtone state. The system is intentionally transient: it clears when focus changes, expires after a short weave window, and is not written to saves.

Player actions contribute **Cut** (Pathblade), **Line** (Riftbow), **Ring** (Threadstaff), and **Pulse** (a successful `G` resonance). Perfect guards and successful `V` mandates contribute **Bind**. A pair resolves after a brief readable delay; a third distinct pitch resolves immediately into a stronger outcome.

To add an outcome, add its enum value, define its required pitches in `GetOvertoneOutcome()`, and implement its compact effect in `ApplyOvertoneOutcome()`. Keep effects target-local, use existing `TakeDamage`, `ApplyStagger`, `ApplyExpose`, `HealPlayer`, or `RestoreStamina` hooks, and create one `TimedPulse` plus a short notification. New ally command code should report through `OnExpeditionAllyStrike` or explicitly call `RegisterOvertone` only when it meaningfully binds or marks the Threadglass focus.

## Add a camp or patrol

In `EncounterCatalog`, add an `EncounterDefinition` with a stable ID and a list of enemy IDs. The catalog creates a ring of spawn offsets automatically. Use `CreatePhased(...)` when an encounter should summon a second or later wave after the prior wave is defeated.

Place it with:

```csharp
EnemyCamp.Create(game, "encounter.region.my_encounter", worldPosition);
```

For extended wilderness routes, add an `EncounterPlacement` to `EncounterCatalog.ExtendedPlacements`. First-clear state is persisted by encounter ID and every encounter repopulates on its configured timer. `encounter.wild.*` habitats reuse wave/death bookkeeping but do not increment hostile-camp progression.

## Add a route signal patrol

Add a `RouteEventDefinition` to `RouteEventCatalog`. Route signals reuse the generic multi-wave defense interaction in `TetherfallRouteEvents.cs`; give every site a stable ID, readable landmark-adjacent position, regional signal color, two compact enemy waves, and first-clear Favor/resource rewards.

For a first-clear story signal, set `contributesToSignalbreak = true`. For an optional patrol unlocked after that story beat, set `requiresSignalbreakCompletion = true` instead. Repeatable signals need `repeatable = true`, a real-time `cooldownSeconds`, and reduced `repeatFavorReward`, `repeatResourceAmount`, and `repeatExperienceReward`. The game saves `completionCount` and `nextAvailableUtcTicks` in `RouteEventSaveData`; active waves intentionally remain transient and restart safely after a reload.

First-clear-only effects belong in `OnRouteEventCompleted`: quest advancement, Cinderwold state, and restoration-tier changes must never run on repeat patrols. Route signals are routed by the Threadglass only after higher-priority active bounties and city drills, and the post-`signalbreak` objective selects the nearest available patrol after field-survey guidance is complete.

## Add an exploration cache

Add a `FieldCacheDefinition` to `FieldCacheCatalog`. Each cache has a stable ID, position, color, short lore inscription, resource reward, and Favor reward. `FieldCacheBuilder` creates the interactable and the save system persists recovered IDs automatically.

After the original three signal repairs, the Threadglass objective directs the player to the nearest unrecovered cache before resuming available route patrol guidance. Three caches in each region grant its traversal boon: Alderwake reduces climbing stamina cost, Morrowfen increases swim speed, and Cinderwold reduces gliding stamina cost.

## Extend farming

`FarmPlot` growth is stored as a real-world UTC maturity timestamp, so crops continue growing while the player is away. New plots need a deterministic `PersistenceId`; use the same ID whenever the world is constructed so the matching `FarmPlotSaveData` is restored correctly.

Released resource nodes follow the original pattern: their stable ID is derived from resource kind and world position, and depleted-node respawn time is saved as UTC ticks. Keep those positions and IDs unchanged. New append-only regional content must call the five-argument `ResourceNode.Create` path with an explicit `PersistenceId` under a semantic namespace such as `resource.annex.<parent>.<name>`; never derive a new persistent ID from coordinates.

## Add provisions, contracts, or restoration sites

`ProvisioningCatalog` owns meal recipes, vendor profiles, supplies, and camp contracts. New meals need a stable ID, recipe cost, required camp level, restorative values, and optional temporary field effect. Vendor profiles use stable vendor IDs defined when the NPC is created.

`RestorationCatalog` contains modular restoration sites. Each tier declares whether it is manual, any material cost, route-event prerequisite, rewards, and optional equipment reward. Route events can use `requiredRestorationSiteId` and `completionRestorationSiteId` to create a staged restoration sequence without adding bespoke enemy AI.

For a restoration landmark with a richer visual payoff, keep art state separate from its progression state. `FenlightCausewayPresentation` builds all four non-solid visual roots once, polls `FenlightTier`, and activates exactly one root without modifying collision, water volumes, or the restoration save schema. Follow that pattern for new landmark art: use a small shared palette, prebuild state roots, allocate no materials in `Update`, and let the gameplay site remain the only authority on progression.

## Add equipment

Add an `EquipmentDefinition` in `EquipmentCatalog.CreateAll()`. Choose a slot, optional weapon discipline, bonuses, Favor price, or a list of `ResourceCost` crafting ingredients. The field pack, crafting screen, vendor screen, save system, and equipment bonuses consume the catalog automatically. To give a new item an authored visual, extend `CombatVisualBuilder.DressWayfinderWeapon` for a weapon discipline or `DressWayfinderFieldGear` for a stable coat, charm, or tool ID; `PlayerAvatar` rebuilds those small loadout roots whenever the selection changes or a save is restored.

## Add a quest

Add a record in `QuestCatalog.CreateAll()`, then call `AdvanceQuest(stableId, amount)` from the gameplay event that should count. Existing examples are camp clears, Pact formation, farm harvests, restoration tiers, crafting, and dungeon pylons.

## Add a modular dungeon

`DungeonCatalog` in `Assets\Scripts\TetherfallDungeonCatalog.cs` owns authored dungeon graphs. A `ModularDungeonDefinition` declares the stable dungeon ID, regional gate/return/interior positions, unlock and progression quests, boss and reward references, local bounds, hard renderer/collider/light budgets, and these stable-ID records:

- `DungeonRoomDefinition`: entrance, puzzle, combat, checkpoint, traversal, boss, or exit node, plus local bounds and a safe re-entry point;
- `DungeonConnectionDefinition`: a directed room edge and its physical seal;
- `DungeonPuzzleDefinition`: ordered-sequence or free-order state-match logic with stable mechanism IDs, clue copy, and an explicit solution;
- `DungeonCheckpointDefinition`: a room-linked recovery point; and
- `DungeonWaveDefinition`: a compact catalog enemy roster for a combat room.

`ModularDungeonDirector` in `Assets\Scripts\TetherfallDungeons.cs` builds the graph, collision shell, objective targets, puzzle mechanisms, wave encounters, checkpoint recovery, traversal latch, `DungeonBoss`, retreat/outflow portals, and reward-safe replay. Add content to the catalog rather than branching the director for stat, graph, or layout changes. A genuinely new puzzle grammar can extend `ModularDungeonPuzzleKind` and the shared mechanism evaluation, but should not introduce a dungeon-specific save class.

Every dungeon persists one `DungeonSaveData` record keyed by dungeon ID. Keep room, connection, puzzle, mechanism, wave, and checkpoint IDs stable after release. Save only durable progression: completed rooms, solved puzzles, normalized mechanism values, checkpoint, active combat wave/remaining roster, boss completion, and the one-shot reward guard. Boss health, live enemy objects, transient effects, and replay attempts intentionally restart. `DungeonProgressRules.Normalize` must leave a valid checkpoint/entrance route, reconstruct missing puzzle records, reject stale references, and fail open after a completed boss so reload, teleport, or death cannot strand the player.

Completion rewards must call `DungeonProgressRules.TryClaimReward` before adding Favors, resources, XP, or equipment. Replays may reuse the authored boss but must never clear that guard or advance the first-clear quest. Keep visual-only water, ribs, glyphs, and labels collider-free; use simple primitive colliders only for floors, retaining walls, seals, traversal surfaces, and interactables. Resolve puzzle colors through `TetherfallAccessibilityRuntime`, reinforce them with text/shape cues, and use the existing pooled pulse/effect APIs.

Routine mutations should call `RequestSave()`; they are coalesced and deferred while nearby combat is active to avoid synchronous disk stalls on hit, heal, loot, or XP bursts. Durable transitions that must survive immediately—dungeon checkpoints and rewards, death recovery, Threadway travel, and confirmed Pact Unfolding—must call `RequestCriticalSave()` so they bypass that delay. Application focus loss, pause, and quit also flush synchronously.

Run `Tetherfall > Verify Modular Dungeons` after every graph edit. It checks reachability, node/reference integrity, save and reload normalization, one-shot rewards, safe positions, world bounds, and declared geometry/effect budgets.

## Add an optional trial or boss

`TrialCatalog` declares a stable trial ID, gate, entry and return positions, boss profile, reward equipment ID, and accent color. `BossCatalog` defines the reusable health, phase thresholds, action rotations, movement rates, colors, and optional summoned enemy for a boss.

Implement the trial director in `TetherfallTrials.cs` using `IOptionalTrial` for save registration and `IDungeonBossArena` for phase/latch rules. The existing `AshwakeTrial` is the reference: it stores dial positions, current wave, remaining enemy IDs, completion, and reward state; it intentionally reconstructs an unfinished boss at full health after a reload.

Keep optional trials out of `GetObjective()` and objective-beacon routing unless they should become mandatory story content. Use a dedicated gate rather than `RegisterDungeonGate`, which belongs to Skyvault navigation.

## Add a city, resident, or civic activity

`CityCatalog` owns city definitions, named residents, and repeatable civic activities. A city has a stable ID, center, palette, residents, and activity list. `CityBuilder` constructs registered `CityDirector` instances, while `TetherfallCities.cs` handles the generic noticeboard, resident interaction, inn recovery, archive exchange, combat ring, cooldowns, and persistent activity state.

Use stable IDs for every resident and activity. Delivery activities declare a `ResourceCost`; training activities declare enemy IDs and an arena position. City activity cooldowns are saved as UTC ticks, while a live drill remains transient and is surfaced through `CityDirector.IsTrainingActive` so boards cannot offer the same drill twice. First completion of an activity can advance its city's quest through `CompleteCityActivity`.

Build city geometry in a city-specific `CityDirector` layout method. Keep residents outside closed building wall volumes, use colliders only for roads/buildings/arena floors, and leave ambient residents non-solid so they never block traversal. Waterways pair a non-solid visual surface with a matching `WaterVolume` trigger.

`CityResident.Create` automatically builds the collider-free articulated silhouette in `TetherfallCityResidentArt` and attaches `HumanProceduralAnimator`. Service type selects the idle and interaction gesture; role text selects lightweight props such as civic armor, folios, aprons, or satchels. Keep new role names semantically descriptive so those visual cues continue to resolve without per-resident code.

## Add an elite bounty

`BountyCatalog` owns city-issued elite hunts. A bounty has a stable ID, sponsor city ID, target `EnemyDefinition` ID, world position, difficulty, Favor/XP/material rewards, accent color, phase title, reinforcement roster, and real-time cooldown. `BountyBuilder` creates its target only after the player accepts it from the sponsoring city's noticeboard.

Use a dedicated elite enemy definition rather than changing regular camp archetype values. Existing bounty targets reuse `Enemy` combat AI, collision, telegraphs, loot, and tactical interactions, while `BountyVisualBuilder` adds a readable elite crown, route marker, and half-health phase signal. Reinforcements are transient and are removed when the target resolves or the hunt deactivates. The active bounty is intentionally restored at full health after a reload; its completion count and cooldown remain persistent.

First resolution should call `AdvanceQuest("bounties", 1)` through `OnBountyTargetDefeated`. Do not add bespoke HUD routing: an accepted bounty automatically becomes the active `GetObjective()` and Threadglass beacon target until it is resolved.

## Add a Threadway anchor

`ThreadwayCatalog` owns all fast-travel anchors. Each definition needs a stable ID, display title, region, destination description, world position, safe arrival position, Chimesalt activation cost, and accent color. `ThreadwayBuilder` creates the same reusable `ThreadwayAnchor` at each listed position.

Threadways do not teleport the player out of an active fight: both restoration and travel reject a nearby hostile. The source anchor opens the generic route panel after it is restored, and the player can travel to any other restored anchor. `TetherfallThreadways.cs` recalls recruited companions and Pacts before closing the panel so a long journey cannot strand the active expedition.

Add an unlock only through `TryRestoreThreadway`; it registers the persistent ID, awards the discovery, advances the Threadways quest, updates the anchor visual, and writes save data. Arrival points must be tested against buildings, water, dungeon walls, and solid props before shipping.

## Add a regional expansion pack

`Assets\Scripts\TetherfallRegionPacks.cs` owns the ten-pack registry for expansion beyond the established three regions. All ten entries are playable bounded slices; additive packs belong there rather than in the legacy region builders. `REGION_PACKS.md` records their stable IDs, authored content, connector policy, runtime bounds, and budgets.

If the catalog is extended later, keep each new region at `AuthoringSlot` until it has all of the following:

- unique biome, palette, silhouette, discovery copy, and generated regional study;
- solid terrain with meaningful elevation change, at least one authored access ramp, and an always-on safe arrival shell;
- a named landmark and persistent secret;
- at least six resource nodes across three resource kinds;
- a stable encounter persistence ID and a footprint not duplicated by another playable pack;
- complete Threadway and world-map metadata with a tested flat, dry arrival point clear of low terrain, encounter footprints, secrets, water, and wind triggers;
- a static connector beginning on the legacy ground or an already reachable neighboring region shell;
- one proximity-activated detail root using shared material caches; and
- a clean `Tetherfall > Verify Regional Expansion Packs` result within renderer, collider, light, and update-driver budgets.

Do not count a catalog slot, distant backdrop, flat ground tile, or decorative-only biome as playable. Static traversal shells may remain active for collision and safe travel, but camps, resources, labels, secrets, animated Threadway pieces, and lights must stay under the pack's bounded detail root.

## Add a regional frontier annex

`Assets\Scripts\TetherfallRegionalAnnexes.cs` is the append-only seam for expanding a released region without moving or resizing its authored content. There must be exactly one annex per current parent. A legacy annex is `160x260` (`41,600 m²`); a RegionPack annex is `140x140` (`19,600 m²`). Annex rectangles must be finite, outside every current parent rectangle, mutually non-overlapping, and connected by supported copper-railed route pieces. The current world AABB is `X -601..581`, `Z -631..731`.

An annex definition is not complete until it provides:

- the stable annex ID and exact parent ID/display name;
- an equal-area finite rectangle and connector polyline that crosses no unrelated region;
- at least eight authored terrain pieces, including a solid ramp, a climb route, and at least three metres of raised relief;
- six resource placements across at least three kinds, each using the explicit `PersistenceId` overload and a semantic `resource.annex.*` ID with no coordinate suffix;
- one unique landmark silhouette assembled from original procedural primitives;
- one semantic stable cache ID and one semantic enemy-camp persistence ID;
- one parent-named Threadway with a distinct finite arrival on the always-on `12x12` support pad;
- teal navigation marks and copper human routework;
- deterministic procedural ground variation using the annex's cached shared material; and
- exactly one bounded atmosphere field with no collider, light, particle system, or `Update`.

Do not move an old object to make room. Do not edit a released coordinate-derived ID. The connector and arrival support stay active, while the full terrain and Threadway shell use 128-metre prewake plus 52-metre hysteresis and a four-root cap. Ecology, camps, resources, caches, landmarks, dressing, and atmosphere use 96-metre prewake plus 48-metre hysteresis and a separate two-root detail cap. Camp enemies must be adopted beneath that detail root and distant populations soft-reset before it deactivates. Parenting a new component beneath either streamed root is the mechanism that guarantees inactive roots run no updates.

Install annex Threadway definitions before `ThreadwayBuilder.Build`; the builder immediately adopts each annex shell under its static root. `TetherfallRegionPacks.ResolveRegionName` and world-map tiles must continue to present the parent display name and palette. Run `Tetherfall > Verify Regional Frontier Annexes` after every annex edit. It verifies parity, thirteen parent mappings, non-overlap, world bounds, transforms, IDs, content minima, connectors, arrivals, streaming ceilings, budgets, map behavior, and documentation, and it is chained from `Tetherfall > Verify Full Game`.

## Add world dressing

- Permanent gameplay geometry should keep a primitive collider.
- Large imported props should call `WorldArt.SpawnKenney(..., solid: true)`, which replaces detailed model collision with a stable box or capsule.
- Bushes, mushrooms, effects, labels, and other visual-only objects should remain non-solid.
- Traversal walls need `ClimbableSurface`.
- Swim spaces need a `WaterVolume`.
- Updrafts need `WindLift`.

`WorldBuilder` owns the three base regions and invokes the region-pack registry. `TetherfallRegionPacks` owns additive regional shells and bounded ecology. `WorldExpansionBuilder` owns services, traversal routes, dungeons, extended encounter placements, resources, and decorative density inside the established world.

## Extend Wayfinder progression

`TetherfallWayfinderProgression` in `Assets\Scripts\TetherfallSkillTrees.cs` owns the existing rounded `1.28x` XP requirement step, carryover, per-rank benefits, and generic expedition-capacity milestones. Keep `level`, `experience`, `experienceToNext`, `health`, and `maxHealth` as the save authorities. Do not add a separate saved discipline-point balance.

Discipline points are derived from one shared breakdown: rank points above rank 1, portable camp upgrades capped at 2, Alderwake restoration capped at 3, and 2 points for Skyvault restoration. UI and normalization must both use `GetPointBudgetBreakdown()` or `GetPointBudget()` so these sources cannot drift.

Ranks 2, 4, and 6 return a `+1` field-slot description from `DescribeRankMilestone()`. The Pact roster consumes those milestones as capacities 3, 4, and 5, while rank 1 starts with 2 slots.

Run `Tetherfall > Verify Wayfinder Progression`, or call `Tetherfall.Editor.TetherfallProgressionVerification.VerifyFromCommandLine` in batch mode, after changing the curve, budget sources, rank benefits, or progression HUD copy.

## Add tutorial or practice questions

The guided opening lives in `Assets\Scripts\TetherfallTutorial.cs`. Add a `TutorialStep`, its instruction, and a corresponding `TutorialEvent` when the answer or action should advance guidance.

For optional practice questions, the clean integration points are:

1. a new world interactable beside a vendor, camp, or dungeon door;
2. a quest objective created in `QuestCatalog`;
3. a puzzle-room mechanism in a `ModularDungeonDefinition`.

Keep question content in a separate catalog with stable IDs, prompt text, answer options, the correct answer, explanation, subject tags, and difficulty. The world object should only request a question and report the result, so educational content can grow without editing dungeon or quest logic.

## Verify changes

Open the project in Unity `6000.4.7f1`, then run:

- `Tetherfall > Create or Refresh Boot Scene` for a compile/configuration check
- `Tetherfall > Build Windows Vertical Slice` for the Windows player

The output executable is `Builds\ProjectTetherfall.exe`.
