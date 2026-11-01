# Tetherfall regional expansion packs

`Assets\Scripts\TetherfallRegionPacks.cs` is the registry-first seam for large-world expansion. All ten stable additional-region entries now build as bounded playable slices.

## Shipped expansion set

Ten packs are playable, bringing the world to thirteen playable regions including the established three:

- **Glassmere Steppe**: saltglass terraces, lateral glass ramps, the Choirglass Arch, a Refraction Niche secret, a mirror-palisade encounter, six Chimesalt/Stone/Fiber nodes, and the Glassmere Tuning Fork Threadway.
- **Rimecoil Range**: four blue-ice switchbacks, climbable high shelves, the Rimecoil Needle, a Blue Cleft secret, a coil-redoubt encounter, six Ore/Fiber/Chimesalt nodes, and the Rimecoil Turn Threadway.
- **Sunken Weald**: raised root causeways across swimmable rootwater, the Hollow Crown, a Hollow-Heart secret, a root-snare encounter, six Herbs/Resin/Timber nodes, and the Weald Root-Loom Threadway.
- **Prismscar Downs**: sawtooth ridges, trench ramps, the Split-Sun Array, a Shadowline secret, a prism-trench encounter, six Stone/Ore/Chimesalt nodes, and the Prismscar Meridian Threadway.
- **Galecradle Mesa**: paired mesas, a grounded ramp route plus one bounded updraft, the Kitebone Crown, a High-Perch secret, a kite-ring encounter, six Fiber/Ore/Resin nodes, and the Galecradle Yoke Threadway.
- **Lumenwaste Reach**: phosphor dunes, two lens ramps, the Walking Beacon, a Lens-Shadow secret, a lens-walk encounter, six Chimesalt/Stone/Fiber nodes, and the Lumenwaste Walking Line Threadway.
- **Brassroot Deeps**: stepped foundry terraces, climbable brass roots, the Brassroot Crucible, a Quenched Ledger secret, a foundry-terrace encounter, six Ore/Stone/Resin nodes, and the Brassroot Intake Threadway.
- **Cloudbarrow Expanse**: high-grass burial shelves, two processional rises, the Cloudbarrow Cairn, a Lee-Side secret, a barrow-gate encounter, six Herbs/Fiber/Stone nodes, and the Cloudbarrow Processional Threadway.
- **Tideglass Strand**: a dry approach over static shallow glasswater, island ramps, the Tideglass Needle Reef, a Stranded Survey secret, a tidebreak-spines encounter, six Chimesalt/Stone/Ore nodes, and the Tideglass Drymark Threadway.
- **Nightloom Hollow**: paired ravine shelves, two canopy bridges, the Nightloom Lattice, a Glow-Knot secret, a loom-ambush encounter, six Herbs/Resin/Fiber nodes, and the Nightloom Low Weave Threadway.

Each playable pack has a unique biome, map palette, silhouette, landmark style, encounter footprint, terrain layout, resource mix, discovery presentation, map footprint, and Threadway metadata. All ten pack secrets persist through the existing recovered-cache ID channel but do not advance the original nine-cache Field Archives quest.

## Append-only regional frontier annexes

`Assets\Scripts\TetherfallRegionalAnnexes.cs` adds exactly one connected frontier annex to each of the thirteen current regions without changing any released transform, RegionPack root, encounter, city, settlement, dungeon, farm plot, Threadway, resource node, or coordinate-derived persistence ID. The 13 frontier annexes are:

- **Alderwake Basin — Rainroot Canopy:** rain-fed canopy shelves, the Canopy Compass, and the Greenwake Survey Cache.
- **Morrowfen — Siltglass Reach:** stillwater ledges, the Reedwake Orrery, and the Stillwater Ledger.
- **Cinderwold Verge — Emberstep Shelf:** cooling basalt steps, the Ashbell Gantry, and the Cooling Vault.
- **Glassmere Steppe — Rainprism March:** rain-dark prism shelves, the Rain Dial, and the Vanishing Reflection Cache.
- **Rimecoil Range — Hoarfold Shelf:** blue-ice folds, the Split-Ice Sextant, and the Warm Sextant Case.
- **Sunken Weald — Rootwake Fen:** dry root shelves, the Drowned Lantern Crown, and the Amber Seedcase.
- **Prismscar Downs — Afterlight Scar:** dark flint terraces, the Facet Harp, and the Last-Ray Case.
- **Galecradle Mesa — Kitefall Table:** grounded wind-cut switchbacks, the Wind Yoke, and the Lee Courier Box.
- **Lumenwaste Reach — Dusklens Margin:** muted phosphor shelves, the Dawn Lens, and the Unlit Aperture Cache.
- **Brassroot Deeps — Quenchroot Gallery:** cooled casting steps, the Pouring Root, and the Cold Tally Cache.
- **Cloudbarrow Expanse — Highwake Barrows:** wind shelves, the Sky Cairn, and the Weatherward Bundle.
- **Tideglass Strand — Ebbmirror Flats:** dry black shelves, the Tidal Comb, and the Ebb-Reader Case.
- **Nightloom Hollow — Starroot Cleft:** violet root shelves, the Moon Lattice, and the Darkstrand Knot.

Each legacy parent remains `160x260` (`41,600 m²`) and receives a `160x260` annex, yielding `83,200 m²` of declared regional footprint. Each RegionPack parent remains `140x140` (`19,600 m²`) and receives a `140x140` annex, yielding `39,200 m²`. Across all thirteen parents, the declared footprint grows from `320,800 m²` to exactly `641,600 m²`. Connectors occupy route corridors outside those parity rectangles and do not redefine either parent or annex bounds.

Every annex has eight authored terrain pieces, solid ramps and climb routes, vertical relief, six explicit semantic-ID resource nodes across three kinds, one distinct procedural-primitives landmark, one stable cache, one stable enemy-camp persistence ID, one parent-named Threadway, teal navigation marks, copper human routework, deterministic shared-material ground variation, and one bounded collider-free/light-free atmosphere field. Annex map tiles reuse the parent palette and `ResolveRegionName` returns the parent display name.

## Runtime and budgets

The traversal shells and inter-pack connectors stay available for safe physical approach and Threadway arrival. Ecology, camp presentation, resource updates, secret interaction, and the pack Threadway hierarchy are grouped under one proximity-activated root. A single director checks districts every 0.25 seconds; inactive detail roots run no per-frame logic. Shared runtime material caches serve terrain, props, resources, secrets, and landmarks.

Conservative hard limits per active pack:

- 160 renderers; current maximum estimate is 144.
- 64 colliders; current maximum estimate is 55.
- 2 lights: one existing campfire light and one existing Threadway beacon.
- 20 active update drivers; current estimate is 16.
- At most 2 regional detail roots can overlap, so the expansion ceiling is 4 active lights and 32 update drivers plus the one shared director.

Static route shells use no lights and no animation loops. Decorative landmark pieces are non-solid unless they visibly support traversal. Encounter cover is limited to four solid pieces per footprint.

Annex connectors and `12x12` arrival pads are always available and have no update loop. Full annex terrain roots prewake 128 metres from their finite bounds and retain a 52-metre hysteresis band; no more than four are active. Detail roots prewake at 96 metres, retain 48 metres of hysteresis, and hard-cap at two so the 78-metre camp population wake radius cannot lead its owning detail root. Spawned camp enemies are adopted beneath that detail root, and a distant streamed camp is soft-reset before deactivation. Threadway shells are attached beneath their static root immediately when `ThreadwayBuilder` creates them, so inactive annexes run no Threadway animation updates while every destination retains an always-on raycast-safe arrival pad.

Conservative per-annex maxima are **177 renderers**, **53 colliders**, **2 lights**, and **15 active update drivers**, under hard limits of 180/64/2/16. The always-on connector and arrival layer totals 135 renderers and 37 colliders across all annexes. At the four-static/two-detail ceiling, annex runtime totals no more than **467 renderers, 147 colliders, 6 lights, and 43 update drivers including the one shared director**. Resource and camp labels retain their static text meshes but drop their per-frame billboard components. No atmosphere or route-dressing object has a light, collider, particle system, or update loop.

Those figures cover the built annex roots, camp presentation, resources, and Threadway shells. Transient enemies, projectiles, and pooled combat feedback remain governed by the existing global actor/effect limits rather than being charged again to the regional world-object budget.

## Compatibility

The original Alderwake Basin, Morrowfen, and Cinderwold Verge builders are unchanged. Their geometry, encounters, services, quests, and nine original Threadways remain in place. Ten pack Threadways are appended before the generic Threadway builder runs, producing nineteen runtime anchors while preserving every existing and scaffold-era stable ID. The existing Threadways quest still requires nine restorations, so expansion anchors are additive and can contribute without invalidating old saves.

Thirteen annex Threadways append after the ten pack definitions and before the generic builder runs, producing thirty-two runtime anchors without changing any prior ID or the nine-restoration quest requirement. New annex resources use `resource.annex.*` IDs through the explicit `PersistenceId` creation path; all pre-existing `ResourceNode.Create` calls retain their coordinate-derived behavior byte-for-byte.

The world-map projection and outer collision boundaries now cover `X -601..581` and `Z -631..731`, an `1182x1362` world envelope with an eleven-unit buffer beyond the outermost annex shells. Annexes never overlap a current parent or one another. Supported copper-railed connectors cross only empty route corridors, and every annex arrival lands on a flat always-on pad clear of authored terrain, camps, and caches.

## Verification

Run `Tetherfall > Verify Regional Expansion Packs` to require all ten expansion packs playable and check unique palettes and silhouettes, terrain relief and ramps, landmarks and secrets, resource diversity, encounters, Threadways, safe arrivals, supported connectors, world-map projection, activation overlap, budget estimates, and documentation.

Run `Tetherfall > Verify Regional Frontier Annexes` to verify exact area parity, all thirteen parent mappings, non-overlap, world bounds, finite transforms, semantic stable IDs, terrain and content minima, connector support and corridor clearance, Threadway arrival safety, static/detail activation ceilings, renderer/collider/light/update budgets, parent-name resolution, world-map projection, and this contract. The full-game verifier chains both regional checks.
