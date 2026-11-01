# Tetherfall

A Unity RPG prototype. Open this repository in Unity Hub using the version in `ProjectSettings/ProjectVersion.txt`. Source assets, packages, and project settings are included. Local builds, editor caches, logs, and user settings are excluded.

## Opening tutorial

New expeditions begin with short, non-blocking field guidance. Press `Enter` to begin it, `K` to skip it, or `F1` to hide/show it. It teaches movement, camera control, attacking, guarding, dodging, interaction, equipment, settlement planning, and the first voluntary Pact.

## Wayfinder progression

XP requirements still begin at 100 and grow by the existing rounded `1.28x` curve, with overflow carried into the next rank. The HUD now gives XP its own Threadglass bar, combines nearby gains into one `+XP` readout, and shows unspent discipline points beside the `U` skill-lattice hint.

Every new rank grants `+12` max vitality, fully restores vitality, and adds `+1` discipline point. Ranks 2, 4, and 6 also report a `+1` expedition field-slot milestone through a roster-safe progression helper. The discipline budget breakdown names all four sources: rank points above rank 1, up to 2 portable camp points, up to 3 Alderwake restoration points, and 2 Skyvault points.

## Controls

- `WASD`: move
- Mouse: camera
- `Shift`: sprint
- `Space`: jump; release, then press and hold again while airborne to glide; hold against a marked rootface to climb
- `Left Ctrl`: dodge on land, descend while swimming, drop from a climb
- Right mouse: guard; guarding at the moment of impact performs a perfect guard
- `F` or left click: attack
- `G`: weapon-specific resonance ability
- `L` or middle mouse: bind/release Threadglass combat focus on a nearby hostile
- `V`: issue a bounded tactical command to active field Pacts and companions, or launch a Threadlink Rally while a seal is active
- `Z`: cycle expedition orders: Trail, Focus, and Bulwark
- Focus a hostile with `L`, then combine weapon hits, `V` commands or perfect guards, and `G` abilities to braid Overtone Lattice outcomes
- `X`: consume the selected prepared provision
- `1`, `2`, `3`: switch between crafted Pathblade, Riftbow, and Threadstaff builds; a real focused combat swap can prime a Crossweave Relay
- `E`: interact, gather, Pact, recruit, rest, harvest
- `I`: open equipment and field stores
- `J`: open the quest ledger
- `U`: open the three-branch skill lattice
- `M`: open the regional map and Threadway travel view
- `N`: open or close settlement planning when field panels are closed; return within the Threadheart management radius after claiming it
- `B`: open crafting, or camp services while near the portable camp
- `Q`: rotate the active human companion pair after recruiting three people; during a Threadlink seal, cycle Rally Beat 2
- `T`: cycle Pact command focus across the active roster; during a Threadlink seal, cycle Rally Beat 1
- `C`: build or rest at the portable camp
- `R`: mount or dismount with Slateback; while gliding with Suncrest active, request its one collision-bounded thermal lift for that airtime
- `Tab`: open the field log
- `F1`: hide or show first-expedition guidance
- `K`: skip first-expedition guidance
- `H`: hide/show controls
- `F10`: open accessibility and control-remapping settings
- `Escape`: close a panel, or pause field simulation and release the mouse cursor

## Included playable systems

- Thirteen distinct playable regions: the established Alderwake Basin, Morrowfen, and Cinderwold Verge plus Glassmere Steppe, Rimecoil Range, Sunken Weald, Prismscar Downs, Galecradle Mesa, Lumenwaste Reach, Brassroot Deeps, Cloudbarrow Expanse, Tideglass Strand, and Nightloom Hollow; all ten expansion packs are playable bounded slices
- Third-person movement, sprinting, jumping, climbing, swimming, gliding, and mounted travel
- Three swappable combat disciplines, combo strikes, ranged bolts, area resonance, guard, perfect guard, and dodge invulnerability
- More than 150 spawn slots across thirty-four respawning encounters, including six small wild Echosworn habitats, ten bounded expansion-pack footprints, selected camps with reinforcements between combat phases, plus four distinct escalating bosses
- Collision-aware enemy and boss movement, group separation, readable cancellable telegraphs, limited simultaneous attackers, ranged volleys, lunge attacks, snapshot ground-burst zones, expanding ground shockwaves, and heavy knockback
- Stamina-governed sprinting, dodging, climbing, gliding, combat focus, perfect-guard counterwaves, exposed-target damage windows, distinct weapon resonance abilities, and combat timers that pause cleanly in field panels
- Player-led Overtone Lattice: focused Cut, Line, Ring, Bind, and Pulse actions braid into short pair or triple outcomes, connecting weapon swaps, perfect guards, expedition commands, Pact creatures, and companion roles
- Regional lighting transitions, distance-culled bounded particle atmosphere (Alderwake threadfire, Morrowfen veil glimmers, and Cinderwold ember-ash), water shimmer, route lanterns, foliage motion, terrain texture materials, and region-discovery art cards including cached generated studies for all ten expansion packs
- Integrated collider-free art overhauls for the faceted Wayfinder, all four legendary Echosworn, six dedicated wild Threadbind species, every other catalogued hostile, and articulated fifteen-socket city residents, plus bounded regional composition clusters with no added lights or update loops
- A runtime-visible Quaternius CC0 environment pass built from 31 Resources-loadable prefabs: 66 deterministic collider-free facade, market/service, camp-perimeter, tree, rock, and foliage placements in the two cities and three original regions, distance-capped at 145 m with zero added lights or per-instance update loops
- Visible Wayfinder loadouts: Pathblade, Riftbow, and Threadstaff swaps; Mosscoat, charm, and field-tool silhouettes refresh immediately when equipment changes
- Weapon trails, impact shards, floating damage numbers, camera impulse, fading telegraphs, physical melee follow-through, and focused-target `EXPOSED` / `STAGGERED` timers
- The Skyvault dungeon with an ordered resonance puzzle, three swarm rooms, a sealed boss chamber, and a return gate
- Glasswater Sluice, the first catalog-driven modular regional dungeon: eight connected stable-ID rooms, two distinct environmental water-control puzzles, a two-wave cistern fight, persistent checkpoint, pressure-lift/climbing route, Glasswater Regent boss and reward-safe replay, Outflow Loom exit, objective guidance, and the Sluiceglass Charm
- The optional post-Skyvault Ashwake Crucible: a Cinderwold forge trial with free-order dial puzzles, vertical Threadwind routes, three ring-defense swarms, relief-latch boss intermissions, a retreat route, and the Thermal Reel reward
- Wayfinder levels and XP from combat, gathering, exploration, recruitment, and Pacts
- A persistent fifteen-node skill lattice across combat, Pact command, and traversal branches, with prerequisites, rank costs, and bonuses wired into moment-to-moment play
- Saved control remapping and accessibility settings for camera shake, hitstop, HUD/tutorial scale, telegraph contrast and palette, plus hold/toggle preferences for sustained actions
- Consent-based Echosworn Pacts, a serialized roster with one total five-Pact field cap, actor-free reserves, capped affinity Concord, persistent story-Pact bond XP/ranks, and camp-only Pact Unfolding choices
- One bounded player Threadheart settlement with grid placement, upgrades, storage, offline production, and explicit consenting Pact-worker assignments; cities, Threadway arrivals, dungeons, encounters, and story-restoration footprints remain protected
- Six exact-ID wild Echosworn encounter types across Alderwake, Morrowfen, and Cinderwold; eligible defeat opens a 24-second voluntary Threadbind rescue instead of recruiting automatically
- Four distinct greater forms with authored procedural silhouettes, stronger tactical identities, passive field benefits, Ridgebearer ride progression, and Suncrest's bounded glide lift
- Three human companions with scout, medic, and ruin-breaker combat roles, plus field-pair rotation
- Player-led Threadlink combat: weapon abilities seal a focused enemy; a visible two-slot Beat Rail lets the player steer either Rally responder with `T` and `Q` before `V` launches the timed weave, reliably triggering distinct Sever, Latch, or Aegis threadbursts
- Crossweave Relays: swap weapons while focusing a hostile, land the next basic attack before the relay fades, and one affinity-matched field ally answers with Anchor control, Sight exposure, or Veil recovery while adding `Bind` to the Overtone Lattice; relay marks name the responding ally
- Cindermaw's Ashguard Counterweave bounty: its shockwave brace resists ordinary flinches and reduces chip damage until a valid Crossweave, or a precise perfect guard, fractures it into a long exposed/staggered Threadlink and Overtone window
- Expedition allies visibly originate their attacks: role- and Pact-colored source cues, bolts or arcs, strike motion, ally-centered tactical pulses, and animated active-field sigils make companion contributions readable in combat
- The expedition card lists up to five active Pacts with roles and command-focus marker, plus the field pair, contextual Threadlink Beat Rail, and truthful `V` readiness
- Seven respawning resource types: Timber, Stone, Resin, Chimesalt, Herbs, Ore, and Fiber
- Glowreed farming with plant, real-time persistent growth, harvest, recipe resources, and quest progress
- Crafting for weapons, a field coat, and climbing tools, plus vendor-sold charms
- Three regional vendors with distinct stock, recipes, and resource supplies
- Portable camp construction, a tiered kitchen, three tactical provisions, resting, two upgrade tiers, and delivery contracts
- Three-stage Alderwake settlement restoration with changing world geometry and service progression
- Two populated city hubs: Alderwake Hearthmarket and Veyrwater Mooring, each with named districts, staffed cooperative market stalls, rotating auctions, Wayfarers and Threadwright guild equivalents, civic manors, resident quest houses, service alleys, ordered rooftop traversal routes, noticeboards, inns, archives, repeatable resource drives, and escalating combat-training rings
- Distinct city life and presentation: Hearthmarket uses an animated expedition forge, copper guild roofs, growers' arcades, and routeworker ambience; Veyrwater uses sail canopies, auction lofts, raised boardwalks, bobbing skiffs, a working signal winch, reactive lantern chains, and a swimmable mooring canal
- Four city-issued elite bounty hunts with distinct target combat styles, half-health reinforcement phases, Threadglass tracking, city-specific rewards, cooldown-based repeat runs, and a dedicated quest
- Nineteen discoverable Threadway anchors at runtime: the original nine plus one bounded anchor in each of the ten playable expansion packs; restore them from the road, spend Chimesalt at remote sites, then fast-travel safely between any restored anchors while your expedition recalls with you
- A staged Fenlight Causeway restoration with patrol clearance, a gated route-event defense, transformed geometry, a four-state canal-edge art pass (drowned route, lantern chain, defended signal, raised copper deck), and a unique charm reward
- A branch-capable Cinderwold Ember Compact arc with relay, cache, camp, and Pact objectives that permanently resolves as a protected route or ash-current shortcut network
- An Ash-Copper Spine approach physically links Cinder Station, the witness mural, Embershelf branch, Ember Relay Breach, and Skyfire Relay with a raised basalt route, cable gantries, shelf masses, and resolution-reactive signals
- Six multi-wave route-signal events: three first-clear story signals plus three repeatable regional patrol sites with saved real-time cooldowns and a late-game wilderness objective
- Nine recoverable Wayfinder field caches with route lore, resources, Favors, objective guidance, and three permanent traversal boons, plus ten persistent expansion-pack secrets that do not advance the original Field Archives quest
- Sixteen catalogued field quests covering settlement repair, city life, elite hunts, Threadway restoration, crafting, camps, farming, Pacts, the Skyvault, Glasswater Sluice, route-signal recovery and patrols, exploration, Fenlight restoration, and the Ember Compact, plus the modular Hollow Choir main-story chapter
- Equipable charm and field-tool loadouts, including defensive, movement, stamina, stamina-regeneration, and Threadwind-launch tradeoffs, each reflected on the Wayfinder model
- Automatic versioned save records for progression, resources, resource-node respawns, quests, equipment, Pact roster/field order/focus, player settlement/core/build/storage/worker state, Pact bond source ledgers/ranks/chosen forms, recruits, saved expedition order, camp upgrades, regional restoration, provisions, contracts, farm timers, field-cache recovery, elite-bounty and route-signal cooldowns, Threadway discovery, mount state, stable room/puzzle/checkpoint dungeon graphs, trial progress, tutorial state, and first encounter clears
- Validated temporary saves plus automatic backup recovery when the primary record is damaged
- Original generated region materials, regional discovery studies, ten cached procedural expansion studies, Threadglass interface art, procedural character/creature/hostile silhouettes, regional composition art, city crestwork, and a Cinderwold Skyfire Relay vista with tower architecture, cable pylons, ash drift, molten fissures, and a procedural witness mural, plus a curated CC0 Kenney environment set with preserved license provenance
- A compact field-kit HUD with equipment, crafting, vendor, quest, camp, settlement, and boss interfaces; settlement planning captures field input and reports Threadheart direction/distance when outside its radius

## Unity workflow

Open the project in Unity 6000.4.7f1. The empty boot scene uses a runtime initializer, so no scene hand-wiring is required.

No Unity verification command was run during the schema-24 integration; the menu and command-line checks below are for the later verification pass.

- `Tetherfall > Create or Refresh Boot Scene` prepares the scene, runtime material, and procedural skybox.
- `Tetherfall > Build Quaternius Runtime Prefabs` idempotently derives 31 Resources prefabs from the curated raw CC0 FBXs while preserving source materials/textures and stripping colliders, lights, cameras, and animation components.
- `Tetherfall > Verify Quaternius Runtime Dressing` rebuilds twice, then checks resource/source references, finite bounds and transforms, forbidden-component safety, 66 planned placements, route/combat/Threadway clearances, runtime idempotency, material/renderer budgets, and license documentation.
- `Tetherfall > Verify Route Signal Patrols` checks saved cooldown restoration, legacy-save migration, cooldown feedback, and Signalbreak target priority.
- `Tetherfall > Verify Cindermaw Ashguard` checks braced chip damage, stagger resistance, one-time Crossweave fracture, and the exposed/staggered payoff.
- `Tetherfall > Verify Fenlight Causeway Presentation` checks tier-reactive state switching plus renderer and zero-added-light budgets.
- `Tetherfall > Verify City Institutions` checks stable institution/district IDs, rotating auction balance, market stock, save-safe cooldowns, traversal routes, arena escalation, and renderer/light/collider budgets.
- `Tetherfall > Verify City Resident Art` checks layered fifteen-socket resident silhouettes, collider/light safety, root-safe articulation, and idempotent animation attachment.
- `Tetherfall > Verify Camera Quality` checks obstruction pull-in and recovery, finite camera poses, non-alloc queries, and single-pass accessibility scaling.
- `Tetherfall > Verify Player Combat Queries` checks allocation-free overlap/line buffers, target de-duplication, obstacle rejection, and narrow-versus-wide attack arcs.
- `Tetherfall > Verify Art Overhaul` checks actor coverage, zero gameplay colliders, duplicate-free integration, and renderer/material/light budgets.
- `Tetherfall > Verify Pact Unfolding` checks stable Pact/form IDs, rank thresholds, bounded source normalization, save migration, idempotent form art, and invalid data.
- `Tetherfall > Verify Pact Roster Foundation` checks schema-24 lifecycle hooks, unified story adapters, five-slot budgeting, exact wild encounters and defeat hooks, nonalloc simulation, and consent-led copy.
- `Tetherfall > Verify Wayfinder Progression` checks XP curve carryover, discipline-budget sources, rank milestones, and required HUD/Field Log hooks.
- `Tetherfall > Verify Settlement Art Pack` checks every stable settlement art ID against normal and forced-fallback renderer, hierarchy, cleanup, and forbidden-component budgets.
- `Tetherfall > Verify Player Settlement` checks schema-24 persistence, Pact-before-settlement lifecycle order, input/modal/UI hooks, bounded forbidden zones, production, storage, and Pact-worker contracts.
- `Tetherfall > Verify Regional Expansion Packs` requires all ten expansion packs playable and checks terrain relief, unique landmarks and encounters, ecology, Threadways, safe arrivals, supported connectors, projection, activation overlap, and hard budgets.
- `Tetherfall > Verify Modular Dungeons` checks graph reachability, stable room/puzzle/checkpoint IDs, catalog references, reload normalization, one-shot rewards, safe bounds, and geometry/effect budgets.
- `Tetherfall > Verify Full Game` runs catalog, reference, position, save-normalization, and documentation checks without entering Play Mode.
- `Tetherfall > Build Windows Vertical Slice` creates `Builds\ProjectTetherfall.exe`.

For a command-line build:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "." `
  -executeMethod Tetherfall.Editor.TetherfallBuildTools.BuildWindows `
  -logFile ".\Logs\build.log"
```

For a command-line progression check without entering Play Mode:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "." `
  -executeMethod Tetherfall.Editor.TetherfallProgressionVerification.VerifyFromCommandLine `
  -logFile ".\Logs\progression-verification.log"
```

Save data is written to:

`%USERPROFILE%\AppData\LocalLow\Tetherfall Prototype\Project Tetherfall\tetherfall-expedition.json`

Delete that file and `tetherfall-expedition.bak` to start a completely fresh expedition.

## Adding content

Enemy stats, encounter rosters, placements, equipment, and quests are data catalogs in `Assets\Scripts\TetherfallCatalogs.cs`. Wild and story-linked roster species live in `TetherfallPactRoster.cs`, while `TetherfallThreadbindAccords.cs` owns rescue opportunities and captured field adapters. Player-settlement definitions and placement exclusions live in `TetherfallSettlementCatalog.cs`; persistence, production, art, runtime placement, and Pact-worker adapters remain split across their `TetherfallSettlement*` and `TetherfallPlayerSettlement*` modules. Modular dungeon graphs, rooms, connections, puzzles, checkpoints, and rewards live in `Assets\Scripts\TetherfallDungeonCatalog.cs`, while `Assets\Scripts\TetherfallDungeons.cs` supplies the reusable runtime. Stable story Pact bond-rank, greater-form, and bounded progression-source definitions live in `Assets\Scripts\TetherfallPactUnfolding.cs`. All ten additive expansion regions live in `Assets\Scripts\TetherfallRegionPacks.cs`; `REGION_PACKS.md` records their playable content, runtime policy, bounds, and budgets. `CONTENT_AUTHORING.md` explains the extension points, `PLAYER_SETTLEMENT.md` records the integrated settlement contract, and `CITY_INSTITUTIONS.md` documents the city institution, market, district, and traversal modules.

## Art and scope

`Assets\Art\ART_PROVENANCE.md` records every generated image and imported third-party source. `Assets\Art\ART_DIRECTION.md` records the regional art direction and its runtime use. Imported Kenney Nature Kit and Quaternius MegaKit geometry is CC0; source URLs, archive hashes, and preserved licenses remain beside the imported assets.

This is a growing original vertical slice, not a production-equivalent replacement for *Palworld* or *The Legend of Zelda*. Those games represent years of work by large teams. This build proves the requested gameplay loops on a stable, extensible foundation; production character rigs, bespoke keyframed animation, voiced cinematic story scenes, and a much larger hand-authored world remain future production work.
