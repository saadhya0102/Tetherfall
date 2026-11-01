# Quaternius CC0 environment subset

Curated for Tetherfall from Quaternius' free Standard archives. Retrieval date: 2026-08-15.

## License and source

All imported files are released under **CC0 1.0 Universal, Public Domain Dedication**:
https://creativecommons.org/publicdomain/zero/1.0/

The creator's original `License_Standard.txt` is preserved unchanged in each pack folder. Quaternius is the creator; this project does not claim authorship. The stable official creator pages and the official Quaternius itch.io download pages are recorded below. No mirrors were used.

### Medieval Village MegaKit

- Official creator page: https://quaternius.com/packs/medievalvillagemegakit.html
- Official creator download page: https://quaternius.itch.io/medieval-village-megakit
- Version: free Standard release, January 2025
- Archive: `Medieval Village MegaKit[Standard].zip`
- Official archive upload: 2025-01-21 22:44 UTC
- Downloaded archive size: 161,003,471 bytes
- SHA-256: `E60DEA67C10F30DCCCCFBFF92A7933F5EA5CFE99BE0E2A0FA5118CCEABEEC5C4`
- Preserved license: `MedievalVillageMegaKit_Standard/License_Standard.txt`

### Fantasy Props MegaKit

- Official creator page: https://quaternius.com/packs/fantasypropsmegakit.html
- Official creator download page: https://quaternius.itch.io/fantasy-props-megakit
- Version: free Standard release, June 2025
- Archive: `Fantasy Props MegaKit[Standard].zip`
- Official archive upload: 2025-06-02 19:31 UTC
- Downloaded archive size: 150,213,360 bytes
- SHA-256: `8B6F7E806D222E585478F0E1BDC6B271BBC7BC6F84DD6AF8CA703A7C64F0CB1E`
- Preserved license: `FantasyPropsMegaKit_Standard/License_Standard.txt`

### Stylized Nature MegaKit

- Official creator page: https://quaternius.com/packs/stylizednaturemegakit.html
- Official creator download page: https://quaternius.itch.io/stylized-nature-megakit
- Version: free Standard release, July 2024
- Archive: `Stylized Nature MegaKit[Standard].zip`
- Official archive upload: 2024-07-30 20:36 UTC
- Downloaded archive size: 104,088,529 bytes
- SHA-256: `298F6732B872E4CF7B30E6E7ABF9641C7F6DC6B326DF37AC089533ED7E3D58C9`
- Preserved license: `StylizedNatureMegaKit_Standard/License_Standard.txt`

## Curated inventory

The subset contains 46 FBX models (22 village modules, 15 fantasy props, and 9 nature assets) plus 35 referenced PNG textures.

### Buildings, stairs, and tower pieces

Intended for modular city houses, cave-built structures, elevated routes, and tower silhouettes.

- Walls: `Corner_Exterior_Brick`, `Wall_Arch`, `Wall_Plaster_Door_Round`, `Wall_Plaster_Straight`, `Wall_Plaster_Window_Wide_Round`, `Wall_Plaster_WoodGrid`, `Wall_UnevenBrick_Door_Round`, `Wall_UnevenBrick_Straight`, `Wall_UnevenBrick_Window_Wide_Round`
- Floors and roofs: `Floor_UnevenBrick`, `Floor_WoodDark`, `Roof_Front_Brick4`, `Roof_Modular_RoundTiles`, `Roof_RoundTiles_4x4`, `Roof_Tower_RoundTiles`
- Stairs and platforms: `Stair_Interior_Simple`, `Stairs_Exterior_Platform`, `Stairs_Exterior_Straight`
- Openings and logistics: `Door_1_Round`, `Window_Wide_Round1`, `Prop_Crate`, `Prop_Wagon`

### Markets, camps, and utility props

Intended for city markets, camp services, cave stores, workshops, and route dressing.

- Market and work surfaces: `Stall_Cart_Empty`, `Stall_Empty`, `Table_Large`, `Bench`, `Workbench`
- Storage and transport: `Bag`, `Barrel`, `Barrel_Apples`, `Barrel_Holder`, `Bucket_Wooden_1`, `Crate_Wooden`, `FarmCrate_Empty`, `Rope_1`
- Camp and lighting props: `Cauldron`, `Torch_Metal`

### Rocks and foliage

Intended for region dressing, cave mouths, tower bases, camp edges, and route silhouettes.

- Trees and shrubs: `Bush_Common`, `CommonTree_3`, `DeadTree_1`
- Ground foliage: `Grass_Common_Short`, `Grass_Wispy_Tall`, `Mushroom_Common`
- Rocks: `Rock_Medium_1`, `Rock_Medium_2`, `Rock_Medium_3`

## Unity Built-in compatibility

- Imported format is raw binary FBX 7.4 plus only referenced PNG textures. No Unity packages, URP materials or shaders, Blend files, OBJ, glTF/GLB, source projects, scripts, examples, or preview scenes are included.
- Source units are centimeters (`0.01 m` metadata) with meter-correct transforms under Unity's file-unit conversion. Axes are Y-up. Village walls use an approximately 2 m wide by 3.12 m high grid.
- Every FBX has one mesh renderer. Material-slot count is 1-4 per model. Across the subset there are 33,739 vertices and 51,258 triangles; the largest single mesh is `DeadTree_1` at 6,169 triangles.
- All meshes contain imported unit-length normals and 1-2 UV sets. No source collision meshes were found.
- Model importers have automatic mesh colliders disabled, animation/camera/light imports disabled, file-unit conversion enabled, and authored normals preserved.
- Normal textures use Unity/Godot-oriented normals where the archive provides them and are imported as normal maps. Roughness maps are imported as linear data. Retained textures are at most 2048 x 2048; unused 4096 x 4096 wear/noise maps were excluded.
- Most pivots are ground- or grid-aligned. Door and window pivots intentionally preserve hinge/grid placement; the wall torch preserves its wall-mount offset; stairs, wagon, and market cart preserve their assembly anchors.
- Nature textures preserve alpha. When extracting shared Built-in materials, use Standard Cutout rendering for leaf and grass cards.
- Gameplay collision is intentionally not supplied. Add simple authored colliders only on placed prefabs that require navigation or interaction.
