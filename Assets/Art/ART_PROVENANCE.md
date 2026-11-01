# Art provenance

## Original Tetherfall-generated assets

The following images were generated specifically for Project Tetherfall during this project session. They are original-to-this-project visual material for the prototype and must not be represented as assets from any reference game. Generation prompts are embedded in the PNG metadata.

- `Resources/Art/Original/tetherfall_alderwake_ground.png`: Alderwake Basin terrain material
- `Resources/Art/Original/tetherfall_morrowfen_ground.png`: Morrowfen terrain material
- `Resources/Art/Original/tetherfall_cinderwold_ground.png`: Cinderwold Verge terrain material
- `Resources/Art/Original/tetherfall_threadglass_icons.png`: Threadglass field-kit icon sheet
- `Resources/Art/Original/tetherfall_alderwake_overview.png`: Alderwake regional discovery art
- `Resources/Art/Original/tetherfall_morrowfen_overview.png`: Morrowfen regional discovery art
- `Resources/Art/Original/tetherfall_cinderwold_overview.png`: Cinderwold regional discovery art

## Original runtime-generated art

- `CityVisualBuilder.CreateCrestMaterial` in `Assets/Scripts/TetherfallCities.cs`: an original procedural Hearthmarket lantern-and-reed crest, generated at runtime for city banners. It has no third-party source material.
- City buildings, stalls, lanterns, banners, road surfaces, and resident accessories in `TetherfallCities.cs`: original runtime geometry and material compositions using Unity primitives.
- `CityVisualBuilder.CityArtPalette` and `CreateWayfindingMaterial` in `Assets/Scripts/TetherfallCities.cs`: original cached city material sets and abstract 128×128 wayfinding textures for Alderwake and Veyrwater. They use no source imagery.
- `CityDirector.CreateHearthmarketGateway` and `CreateVeyrwaterChannelFrame` in `Assets/Scripts/TetherfallCities.cs`: original runtime-built landmark geometry using Unity primitives and the cached city palettes.
- `CityDirector.DressHearthmarketTerraces` and `DressVeyrwaterMooringEdges` in `Assets/Scripts/TetherfallCities.cs`: original scene composition using the documented CC0 Kenney Nature Kit foliage, fence, and rock stand-ins. No external assets were added.
- `CityDirector.CreateHearthmarketForgePlaza`, `CreateVeyrwaterPortLife`, and `CityLifeLandmark` in `Assets/Scripts/TetherfallCities.cs`: original runtime-built forge machinery, route mast, skiffs, raised bridge, signal winch, reactive lantern chains, and civic interaction motion. These additions use Unity primitives and the existing cached city palettes, with no third-party source imagery or meshes.
- `WorldPresentationBuilder.CreateRegionalAtmosphere` and `RuntimeEffectMaterialCache.GetSoftParticleTexture` in `Assets/Scripts/TetherfallWorldEffects.cs`: original runtime-configured regional particle fields and a procedural 32×32 soft Threadglass particle texture. They use no source imagery, external shaders, or third-party particle assets.
- `CinderwoldArtBuilder` in `Assets/Scripts/TetherfallCinderwoldArt.cs`: original runtime-created Skyfire Relay architecture, cable pylons, molten fissures, obsidian skyline, ash drift, and a procedural relay-witness mural texture. It uses no third-party images or meshes.
- `CinderwoldArtBuilder.CreateRelayApproach` in `Assets/Scripts/TetherfallCinderwoldArt.cs`: original runtime-created Ash-Copper Spine road, cable gantries, branch pins, shelf composition, and state-reactive route signals. It uses only Unity primitives and optional CC0 Kenney rock stand-ins already documented below.
- `WorldBuilder.CreateSiltcallerShrine` in `Assets/Scripts/TetherfallWorldBuilder.cs`: original runtime-created Morrowfen Siltward Cairns with basin geometry, resonance pools, glyphs, and drifting motes. It uses no third-party images or meshes.
- `FenlightCausewayPresentation` in `Assets/Scripts/TetherfallFenlightPresentation.cs`: original runtime-created four-state Causeway dressing, including drowned route timber, emissive lantern-chain rhythm, breach shards, wake ring, and a copper-railed deck overlay. It uses only Unity primitives and project runtime materials.
- `ModularDungeonDirector` and `ModularDungeonGate` in `Assets/Scripts/TetherfallDungeons.cs`: original runtime-created Glasswater Sluice architecture and interaction art, including the drowned intake, eight-room retaining shell, copper depth ribs, glasswater courses, shape-coded weir wheels, Tideglass level columns, Keeper's Threadglass, pressure lift, climbable spillway, Mirror Reservoir, Outflow Loom, and Sluiceglass reward colorway. They use Unity primitives, shared project runtime materials, and the existing Skyvault-derived procedural boss silhouette; no third-party images, meshes, shaders, or audio were added.
- `RouteEvent.CreateVisuals` and its patrol state in `Assets/Scripts/TetherfallRouteEvents.cs`: original Unity-primitive signal plinths and shards, with regional stable light, active red defense light, and slow amber destabilization pulses. No external art is used.
- `Enemy.CreateAshguardVisual` in `Assets/Scripts/TetherfallActors.cs`: original runtime-created Cindermaw ember shield shards and fracture pulse, using Unity primitives and project runtime materials only.
- `CrossweaveRelayMark` in `Assets/Scripts/TetherfallCrossweave.cs`: original runtime-generated combat relay rings, shards, and countdown labels. It uses Unity primitives and project runtime materials only.
- `TetherfallWayfinderArtOverhaul`, `TetherfallEchoswornArtOverhaul`, and `TetherfallHostileArtOverhaul`: original collider-free procedural character silhouettes, field clothing, creature anatomy, enemy equipment, and regional combat tells. They use project-authored meshes, Unity primitives, and runtime materials without third-party character assets.
- `TetherfallPactFormArt` in `Assets/Scripts/TetherfallPactFormArt.cs`: original runtime-authored greater-form accents for Grovewarden, Ridgebearer, Rainwarden, and Suncrest. The boughs, shell ridges, current halos, extended wing feathers, and resonance marks use collider-free Unity primitives and project runtime materials; no external creature art is used.
- `TetherfallCityResidentArt` in `Assets/Scripts/TetherfallCityResidentArt.cs`: original lightweight articulated city-resident silhouettes with layered clothing, faces, hair variants, role props, and fifteen named animation pivots. It also supplies an eight-renderer crowd tier for institution stewards and cooperative-stall merchants. The module uses collider-free Unity primitives, shared runtime materials, and no per-character lights or third-party character meshes.
- `TetherfallRegionalArtOverhaul`: original bounded route-side compositions for all three regions. Its 84 static renderers use 12 cached materials, no gameplay colliders, no lights, and no runtime update loop.
- `TetherfallSettlementArt` in `Assets/Scripts/TetherfallSettlementArt.cs`: the Alderwake Threadheart construction family and its original copper, teal, timber, warm-stone, canvas, moss, soil, and water procedural overlays. The catalog exposes 28 stable art IDs with bounded fallback recipes, named snap/work sockets, shared runtime materials, collider-free visual roots, and one optional central culling controller. It introduces no new external source.

## Approved external-source candidates

Only import a candidate after preserving its license text and source URL beside the imported files.

- Kenney Nature Kit, CC0 1.0: https://kenney.nl/assets/nature-kit
- Quaternius Texture Fantasy Nature Pack, CC0 1.0: https://quaternius.com/packs/texturedfantasynature.html
- Poly Haven textures and HDRIs, CC0: https://polyhaven.com/license

Attribution is not legally required for these CC0 candidates, but retain source and version information in this file when a specific asset is imported.

## Imported third-party assets

### Kenney Nature Kit 2.1

- License: CC0 1.0 Universal
- Official source: https://kenney.nl/assets/nature-kit
- Imported model source mirror: https://github.com/ETdoFresh/kenney.nl/tree/master/kenney_natureKit_2.1
- Preserved license: `Resources/Art/KenneyNature/LICENSE-KENNEY-NATURE-KIT.txt`
- Imported files: tree, rock, bush, campfire, tent, bridge, fence, and mushroom FBX stand-ins in `Resources/Art/KenneyNature`

These are generic environmental stand-ins. Tetherfall-specific placement, palettes, lore, construction motifs, and gameplay behavior are original.

### Quaternius environment MegaKits, free Standard releases

- Creator: Quaternius (`@Quaternius`)
- License: CC0 1.0 Universal, Public Domain Dedication
- License text/link: https://creativecommons.org/publicdomain/zero/1.0/
- Retrieval date: 2026-08-15
- Official sources:
  - Medieval Village MegaKit: https://quaternius.com/packs/medievalvillagemegakit.html
  - Fantasy Props MegaKit: https://quaternius.com/packs/fantasypropsmegakit.html
  - Stylized Nature MegaKit: https://quaternius.com/packs/stylizednaturemegakit.html
- Official creator download pages:
  - https://quaternius.itch.io/medieval-village-megakit
  - https://quaternius.itch.io/fantasy-props-megakit
  - https://quaternius.itch.io/stylized-nature-megakit
- Source archives: `Medieval Village MegaKit[Standard].zip` (January 2025), `Fantasy Props MegaKit[Standard].zip` (June 2025), and `Stylized Nature MegaKit[Standard].zip` (July 2024)
- Preserved licenses: `ThirdParty/Quaternius/MedievalVillageMegaKit_Standard/License_Standard.txt`, `ThirdParty/Quaternius/FantasyPropsMegaKit_Standard/License_Standard.txt`, and `ThirdParty/Quaternius/StylizedNatureMegaKit_Standard/License_Standard.txt`
- Local inventory and archive hashes: `ThirdParty/Quaternius/README.md`
- Imported subset: 46 raw FBX models and 35 referenced PNG textures for modular walls, floors, roofs, tower pieces, stairs, market stalls, camp/workshop props, carts, crates, barrels, rocks, trees, and ground foliage
- Runtime conversion: `TetherfallQuaterniusPrefabBuilder` derives 31 Resources-loadable visual prefabs in `Resources/Art/Quaternius`. The generated prefabs continue to reference the imported FBX meshes, materials, and PNG textures; the builder does not recolor, rebake, or claim authorship of them. It strips colliders, lights, cameras, animators, and legacy animation components and overwrites the same asset paths on repeat runs so prefab GUIDs remain stable.
- Runtime composition: `TetherfallQuaterniusDressing` places 66 deterministic collider-free overlays: 8 modular city facades, 16 city-service props, 18 props around six selected camp perimeters, and 24 trees/rocks/foliage pieces in six safe original-region pockets. The pass is capped at 72 placements, 80 renderers, 72 shared source materials, and 16 distance-managed clusters. A single `CullingGroup` uses 70 m and 145 m bands; the pass adds zero lights and no per-instance update loops.
- Verification: `TetherfallQuaterniusVerification` checks Resources loading, source dependencies, finite transforms and bounds, forbidden-component removal, prefab and runtime idempotency, route/combat/Threadway clearances, placement and material budgets, and this preserved provenance.

Only the bounded Standard-archive subset is retained. URP projects and shaders, Unity packages, Blend/OBJ/glTF sources, examples, scripts, previews, unused 4K textures, and source collision meshes are excluded. Automatic mesh-collider generation is disabled; placed gameplay prefabs must receive deliberate simple colliders where required. Tetherfall-specific placement, composition, palettes, lore, and behavior remain original project work.

### Kenney Watercraft Kit 2.1

- Asset ID: `kenney.watercraft-kit.2.1`
- Title: Kenney Watercraft Kit 2.1
- Creator: Kenney
- License: CC0 1.0 Universal
- Official URL: https://kenney.nl/assets/watercraft-kit
- Retrieval date: 2026-08-15
- Source archive: `kenney_watercraft-pack.zip`
- Archive SHA-256: `cd1470c1cf441c7f46d0944ae6d0d897242365dc97677c5079b3238965d659f3`
- Preserved license: `LICENSE.txt` at `Assets/ThirdParty/Kenney/WatercraftKit_2.1/LICENSE.txt`
- Imported subset: ten FBX models (`boat-row-small`, two sailboats, buoy flag, cargo pile, wide ramp, cargo ship, large ship, small ship, and spectral small ship) plus their shared `colormap.png`.
- Runtime purpose: canal traffic, boardable ocean routes, dock ramps, cargo dressing, navigation markers, and a spectral deep-water landmark. Runtime visual prefabs and authored gameplay colliders are built separately.
- Exclusions: OBJ and GLB duplicates, previews, documentation, modern speedboats, liners, towboats, houseboats, racing gates, arrows, unused cargo variants, and unrelated props.

### Alderwake Threadheart settlement reuse

- External geometry: existing Quaternius Resources-ready prefabs already documented above; no new external files, archives, textures, materials, or licenses were added for the settlement pack.
- Reused roles: compatible walls and arch, bench, crate and farm crate, workbench and table, cauldron, barrel and holder, stall shelter, and stylized tree.
- Original project work: all 28 stable `alderwake.*` catalog definitions, procedural missing-source fallbacks, copper/teal/timber overlays, modular dimensions, sockets, material cache, lifecycle API, culling metadata/controller, and Editor verification.
- Runtime composition: `TetherfallSettlementArt` retains source-prefab materials when they fit the declared per-piece budget; otherwise it deterministically selects the original procedural fallback. It does not modify or claim authorship of Quaternius geometry.
- Verification: `TetherfallSettlementArtVerification` checks every normal and forced missing-Resources build for provenance-linked source names, finite bounds/transforms, idempotency, cleanup, forbidden components, renderer/material budgets, material sharing, and centralized culling readiness.
