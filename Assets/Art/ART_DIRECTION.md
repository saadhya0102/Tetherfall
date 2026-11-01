# Project Tetherfall art direction

## Chosen direction

The player delegated the visual choice. This pass uses the existing Threadglass field-instrument identity and the generated regional studies as its north star:

- **Alderwake:** a path-first, repaired frontier. Moss, warm stone, weathered wood, and restrained teal routework make the first region feel inhabited and recoverable.
- **Morrowfen:** cool drowned distance. Wet peat, lantern reeds, reflective water, and a clear luminous destination create tension without making navigation unreadable. Teal Siltward Cairns mark hazardous siltcaller territory as small, navigable ritual landmarks rather than arbitrary combat pockets.
- **Fenlight Causeway:** a canal-edge restoration landmark that carries Morrowfen's route story through four concise states: drowned broken timber, a rehung teal lantern rhythm, a scarred defended signal, and a raised copper-railed deck facing back toward Veyrwater.
- **Glasswater Sluice:** a buried keeper-work beneath Morrowfen, readable as dark wet retaining stone, narrow reflective side channels, sparse copper depth ribs, and restrained tideglass teal. Each room announces one job through silhouette before text: wheel triad, broad cistern, Threadglass refuge, raised spillway face, three height columns, circular mirror basin, then the bright Outflow Loom. Interaction colors always carry a shape or LOW/MID/HIGH label, and amber is reserved for reset/retreat safety.
- **Cinderwold:** ash, broken copper, and exposed routes. Rust, basalt, ember detail, and vertical wind columns communicate danger and gliding opportunity.
- **Cinderwold Skyfire Relay:** a vertical reclaimed-route landmark with an adjacent witness dais. Layered basalt, visibly tensioned copper cables, molten fissures, and a framed witness mural give the late-game region a readable human story and a horizon-scale destination. The Ash-Copper Spine turns the route into three composed beats from mural to breach to tower; its lens stays ember-lit until Routebound restores a teal signal, while Ashbound reveals warm drifting shards.
- **Alderwake Hearthmarket:** an upward-looking river city. A repaired copper-and-stone gateway, pitched moss roofs, warm lantern pools, terraced garden growth, compact stalls, and an animated expedition forge plaza turn repaired routework into a place people choose to live. The flywheel, trip hammer, supply response, and route mast give the market a working civic heartbeat. Copper is reserved for civic frames and wayfinding, while moss and timber carry the built mass.
- **Veyrwater Mooring:** a wet lantern harbor. A dark-timber channel frame, teal markers, reed roofs, sparse canal-edge growth, bobbing skiffs, a raised bridge, and a working signal winch keep the city legible without losing marsh tension. Reactive lantern chains make completed civic work visible. Teal identifies crossings and current-safe paths rather than covering every building.

The target is an original, stylized fantasy action-adventure look. It does not copy the characters, locations, UI, or art direction of any existing game.

## Runtime asset inventory

| Ingredient | Medium | Use |
| --- | --- | --- |
| Three regional ground materials | Generated raster | Tiled terrain in the built-in render pipeline |
| Three regional overview studies | Generated raster | Region discovery presentation and field log art |
| Threadglass icon atlas | Generated raster | Field-kit UI accents |
| Trees, rocks, bushes, tents, bridges, fences, campfires, mushrooms | CC0 Kenney Nature Kit geometry | Collision-safe world dressing and set dressing |
| Route markers, pylon light, water motion, foliage motion, combat arcs | Unity geometry and runtime code | Reactive, readable gameplay elements; route signals shift from stable regional color to slow amber patrol pulses before a defense |
| Skyfire Relay tower, cable pylons, Ash-Copper Spine road/gantries, ember fissures, obsidian skyline, ash drift, and relay mural | Original runtime geometry and procedural raster | Cinderwold landmark composition, ambient depth, and Ember Compact storytelling |
| Siltward Cairns, resonance pools, teal glyphs, and drifting motes | Original runtime geometry and effects | Morrowfen combat landmarks and siltcaller encounter readability |
| Fenlight Causeway state roots, lantern chain, warning shards, breach wake, and raised-deck dressing | Original runtime geometry and shared materials | Morrowfen restoration payoff without new collision or light sources |
| Glasswater intake, modular retaining rooms, side channels, puzzle controls, checkpoint, spillway route, Mirror Reservoir, and Outflow Loom | Original runtime geometry and shared materials | Stable-ID regional dungeon readability under a 190-renderer, 96-collider, zero-added-light ceiling |
| City art palettes, procedural wayfinding signs, Hearthmarket gateway and forge plaza, Veyrwater channel frame, skiffs, raised bridge, signal winch, and reactive lantern chains | Original runtime geometry and procedural raster | Shared city hierarchy, approach silhouettes, civic motion, and navigation without extra lights |
| Alderwake threadfire, Morrowfen veil glimmers, and Cinderwold ember-ash atmosphere | Original bounded particle systems and procedural soft-particle raster | Region-specific motion and depth with fixed particle caps, distance culling, shared materials, and no particle lights or collisions |
| Faceted Wayfinder fieldwear, four Echosworn bodies, and thirteen hostile silhouettes | Original procedural meshes and primitive compositions | Readable character identity, equipment sockets, regional anatomy, and attack-role recognition with collider-free art roots |
| Articulated city residents with layered clothing, faces, hair, and service-role props | Original procedural primitive compositions | Fifteen-socket human animation, stronger social silhouettes, shared materials, and collider/light-safe crowd presentation |
| Six route-side regional compositions | Original static procedural geometry | Additional foreground, midground, and route framing under a fixed 84-renderer, 12-material, zero-light budget |
| Modular facades, stalls, carts, workshop/storage props, trees, rocks, and foliage | CC0 Quaternius geometry with preserved source materials/textures | Collider-free city, service, camp-perimeter, and original-region overlays under one distance-managed runtime module |
| Alderwake Threadheart settlement kit | Existing CC0 Quaternius geometry plus original procedural overlays | 28 stable art IDs for modular construction, beds, storage, production, growing, and perimeter utility pieces |

## Compositional rules

1. Every region needs a visible route, a landmark, and a traversal promise within a short walk.
2. Teal is a navigation or Accord signal. It should not fill the landscape.
3. Copper means human-made repair, service, or routework.
4. Enemy camps read first through silhouette, firelight, and perimeter dressing, not floating labels.
5. Combat effects must enhance attack timing and target recognition without hiding telegraphs.
6. Settlements use warm and cool light as social landmarks: the market/inn must read from a distance, while activity spaces are recognizable through silhouette before labels.
7. The Cinderwold Relay uses teal only at the restored signal point. Ember orange remains environmental danger, while copper identifies traveler-made routework.
8. The Ash-Copper Spine must leave the Ember Relay Breach visually open, framing combat space through side masses and gantries rather than solid central dressing.
9. Ashguard uses a tight ember-shard silhouette only during Cindermaw's shockwave timing; its fracture must reveal the existing amber exposed state rather than add persistent battlefield clutter.
10. Fenlight's teal is a repaired route rhythm, not general fog fill. Tier two may add amber warning shards only while the breach is defended; the restored deck returns to teal and copper clarity.
11. Glasswater uses teal as active pressure and positive alignment, not ambient fill. Wheel glyphs, tide-height labels, and distinct geometry must remain legible without color; transient pulses stay below combat telegraphs, and all puzzle water remains non-solid.
12. Quaternius dressing stays secondary to authored routes and encounters. Facades sit against existing building faces, service props remain outside city road lanes, camp props remain beyond a 10 m combat-center radius, and regional foliage uses the six previously validated outer pockets.

## Quaternius runtime dressing guardrails

The runtime pass uses 31 Resources prefabs to make 66 deterministic visual placements: 8 city facade modules, 16 market/workshop/service props, 18 camp-perimeter props, and 24 original-region trees, rocks, and foliage pieces. Every generated prefab is collider-free and strips lights, cameras, animators, and legacy animation components while retaining the imported Quaternius materials and textures.

The hard ceiling is 72 placements, 80 renderers, 72 shared source materials, and 12 renderers in any one of 16 clusters. One central `CullingGroup` uses a 70 m near band and a 145 m cull band. There are zero added lights, zero gameplay colliders, and no per-instance update loops. Placement verification keeps Threadways and arrivals at least 8 m clear and combat/training/event centers at least 10 m clear; central city route lanes stay open.

## Alderwake Threadheart settlement kit

The first production construction family presents 28 stable art IDs through `TetherfallSettlementArtId` and matching `alderwake.*` catalog strings. It covers the Threadheart core; timber and stone foundations; straight, window, doorway, and gate bays; floor, stair, ramp, and pitched roof; Pact and Wayfinder beds; chest and pallet storage; workbench, forge, cookpot, loom, and medicine stations; planter, farm plot, water collector, and trough; and watch post, lantern post, fence, and banner.

The family keeps the Alderwake silhouette readable through warm-stone feet, thick weathered timber, canvas and moss shelter surfaces, copper repair brackets, and one restrained teal Threadglass cue at functional points. Existing collider-free Quaternius Resources prefabs supply compatible wall, bench, storage, workshop, vessel, and shelter silhouettes. Original copper/teal/timber procedural overlays make those ingredients specific to Tetherfall. Every source-backed recipe has a complete procedural fallback, so missing or newly over-budget Resources content cannot remove a build role.

Visual roots use a ground-contact pivot, +Y up, +Z forward, a 2 m base grid, 4 × 4 m foundation/floor/roof bays, 4 × 3 m wall bays, six named snap transforms where applicable, and 1.5 m work clearance beyond `Socket_Work`. The art hierarchy contains zero gameplay colliders, zero lights, no physics bodies, and no per-piece update loop; future construction gameplay owns collision on a separate supplied parent.

Hard per-piece ceilings are 3 renderers/3 shared materials for structure, 8/6 for stations and utility compositions, and 16/8 for the hero core. No station needs an exception. Eight original palette materials are cached and shared, while approved source prefabs retain their imported shared materials within each cap. One optional `TetherfallSettlementPresentationController` registers the full settlement after placement and uses a central `CullingGroup` with a 70 m near band and a 110 m cull band. Piece metadata remains LOD/culling-ready without polling distance.
