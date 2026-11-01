# City institutions and explorable districts

Project Tetherfall's city layer is split between the established city runtime in
`Assets/Scripts/TetherfallCities.cs` and two additive modules:

- `TetherfallCityInstitutions.cs` owns staffed institutions, rotating auction
  lots, guild services, civic housing, and institution presentation.
- `TetherfallCityDistricts.cs` owns named districts, cooperative merchant
  stalls, service alleys, and ordered rooftop traversal routes.

The modules use the existing `CityDefinition`, resource, vendor, Favor, XP, and
save systems. No scene wiring is required.

## Alderwake Hearthmarket

- **Growers' Arcade** is a public market with the Rootbasket and Coppercord
  staffed counters. Both use Hearthmarket's cooperative stock while retaining
  distinct merchant names, roles, greetings, and stable stall IDs.
- **Guild Terrace** contains the Wayfarers Hall and Threadwright Loomhouse.
  Their repeatable contract and tuning services consume field materials and use
  real-time cooldowns.
- **Manor Row** contains the civic manor and three resident quest houses.
- **Guildroof Courier Circuit** begins in a dressed service alley, climbs a
  continuous copper-and-timber route over the guild roofs, and requires its
  markers in order.
- The Emberleaf auction rotates fixed public consignments instead of rolling
  random rewards. The Rivergate ring escalates from one to three controlled
  waves across repeat completions.

## Veyrwater Mooring

- **Lantern Commons** is a sail-covered market with Dryreed Ferry and Fogpantry
  staffed counters sharing the regional ferry-store ledger.
- **Lowtide Walk** combines the auction loft, courier housing, service freight,
  and the elevated **Lowtide Loft Run**.
- **Flood Council Rise** links the manor, bridge court, spindlehouse, and
  channel works.
- The loft run uses raised timber boardwalks, visible stilts, dry-route markers,
  and an ordered descent beside the swimmable canal.
- The Floodwatch ring uses a distinct fen roster while the Lowtide auction
  rotates mooring-specific consignments.

## Activity and reward rules

Every institution and traversal activity has a stable ID and a positive
real-time cooldown. Completion count and next-available UTC ticks are stored in
the existing city activity save list. Loading never restores an in-progress
arena wave or traversal attempt.

Deliveries and auction consignments spend their listed materials before
settlement. Threadwright services spend tuning materials before restoring
vitality and stamina. Rooftop routes enforce ordered checkpoints and a time
limit before awarding their small Favor/XP payment. A cooling activity cannot
advance its completion count or grant its reward a second time.

## Runtime budget

- City geometry reuses each city's cached `CityArtPalette` materials.
- Institution and district builders are idempotent.
- Decorative props and market merchants are non-solid.
- Traversal landings, ramps, and boardwalks are the only new district
  colliders because they are playable surfaces.
- No institution or district module adds a realtime light.
- Labels use the established distance-gated `BillboardLabel`.
- Named institution stewards use the shared collider-free fifteen-socket,
  eight-renderer crowd rig; animation sleeps beyond 55 metres and the visual
  hierarchy culls beyond 95 metres while interaction state remains active.
- Only active arena drills and active traversal attempts perform gameplay work;
  ambient motions and scheduled citizens are distance-gated.

## Verification

Run the focused edit-mode verifier:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\Users\hasto\rpg\Tetherfall" `
  -executeMethod Tetherfall.Editor.TetherfallCityInstitutionsVerification.VerifyCityInstitutions `
  -logFile "C:\Users\hasto\rpg\Tetherfall\Logs\city-institutions-verification.log"
```

The verifier covers stable IDs, required institution kinds, fair auction bands,
merchant stock references, route reachability, cooldown persistence,
double-reward guards, idempotent construction, renderer/light/collider budgets,
safe traversal reset, and arena escalation.
