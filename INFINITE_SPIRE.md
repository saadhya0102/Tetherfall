# Infinite Spire generation foundation

This document covers the pure catalog, deterministic generation, stable run-local IDs, scaling, and runtime budget contracts. It does not define world placement, UI, art construction, save migration, economy mutation, or GameObject lifecycle.

## Persistence identity

- Dungeon ID: `dungeon.infinite_spire`
- Immutable content namespace: `spire.template.*`
- Rest template: `spire.template.rest.loomrest_meridian`
- Rest display name: Loomrest Meridian
- Room categories: Easy, Combat, Swarm, Traversal, Puzzle, Miniboss, Boss, Treasure, Merchant, Rest

Published constants in `InfiniteSpireIds` are persistence contracts. Existing values must not be renamed, removed, or reused for different content.

The catalog contains:

- 4 Easy templates
- 6 Combat templates
- 4 Swarm templates
- 3 Traversal templates
- 4 Puzzle templates
- 4 Miniboss room templates and 4 miniboss profiles
- 4 Boss room templates and 8 boss profiles
- 3 Treasure templates
- 4 Merchant room templates and 4 merchant kits
- Exactly 1 Rest template, Loomrest Meridian

All hostile references resolve to current `EnemyCatalog` or `BossCatalog` IDs. Spire content never references `enemy.wild.*`, so it cannot reuse a wild Threadbind identity.

## Deterministic hash contract

`InfiniteSpireHash.Compute` applies FNV-1a to four explicit little-endian 32-bit values:

1. Signed `runSeed`, preserved as its two's-complement bit pattern
2. `floorIndex`
3. `roomIndex`
4. Immutable template salt

Template salts are stable FNV values of template IDs. Layout, rotation, wave set, puzzle solution, treasure value, merchant stock, miniboss profile, and boss profile are all derived from the room inputs and stable salts. No global random state, time, locale, or Unity object state participates.

Generation uses caller-owned `InfiniteSpireGenerationState` and `InfiniteSpireFloorPlan` buffers. Construct them once, replay floors in ascending order, and persist or restore the bounded recent-history windows. Equal seed, floor order, and history produce equal output.

## Floor beat chain

Every floor contains 3 to 5 logical rooms:

1. Approach
2. Zero to two pressure rooms
3. One or more cadence rooms
4. Seal

The base target is `3 + (floorIndex % 3)`. Overlapping cadence events can raise a shorter target only as far as needed to retain every forced event, never beyond 5 rooms.

Overlapping cadence rules share the same 5-room ceiling by reducing pressure-room count:

- Miniboss every 5 floors
- Boss every 10 floors, replacing the miniboss
- Merchant on eligible floors divisible by 7
- Loomrest Meridian every 12 floors beginning at floor 6: 6, 18, 30, 42, and so on
- Floor 1 uses only Easy and Combat templates and never contains a boss

When a floor has no scheduled cadence event, it receives a Treasure cadence room. Boss floors never insert a Treasure room. If boss, merchant, and Meridian cadence coincide, all three fit between Approach and Seal with no pressure room.

## Anti-repetition

`InfiniteSpireGenerationState` retains the newest 8 room templates and newest 3 boss IDs.

Normal generation enforces:

- No template repeats inside the newest 8 rooms
- No category appears 3 times in sequence
- No adjacent Puzzle rooms
- No adjacent Swarm rooms
- No merchant on consecutive floors or outside its cadence
- No Loomrest Meridian within 4 floors of another
- No underlying boss ID repeats inside the newest 3 boss encounters
- No Treasure room on a boss floor

A candidate is rehashed at most 12 times. The bounded fallback chooses the least-recent legal category and template in deterministic order. A final relaxed path exists only to recover from impossible or corrupt caller history; normalized persisted history should never require it.

## Stable run-local IDs

The seed component is always 8 lowercase hexadecimal digits:

```text
spire.room.{seedHex}.{floor}.{room}
spire.room.{seedHex}.{floor}.{room}.wave.{index}
spire.room.{seedHex}.{floor}.{room}.mechanism.{slot}
spire.room.{seedHex}.{floor}.{room}.puzzle
spire.room.{seedHex}.{floor}.{room}.chest
spire.room.{seedHex}.{floor}.{room}.boss
spire.room.{seedHex}.{floor}.{room}.stock.{slot}
```

`InfiniteSpireInstanceIds` provides allocating convenience formatters, caller-buffer formatting, and strict parse and round-trip helpers. A negative signed seed round trips through its exact two's-complement hexadecimal representation.

## Scaling contract

Let `F` be the normalized floor, with invalid floors normalized to 1:

```text
tier  = min(F, 120)
depth = max(0, F - 120)

enemyHP  = baseHP * (1 + 0.065*tier + 0.02*depth) * ConcordDamp
enemyDMG = baseDMG * (1 + 0.045*tier + 0.015*depth)
enemyXP  = baseXP * (1 + 0.03*min(F, 80))

bossHP   = baseHP * (1 + 0.055*tier + 0.018*depth)
bossMove = baseMove * min(1.35, 1 + 0.004*tier)

ConcordDamp = lerp(1.0, 0.92, saturate((ConcordMultiplier - 1) / 0.16))
```

Hard caps:

- Enemy HP multiplier: 8.0
- Enemy damage multiplier: 5.0
- Boss HP multiplier: 7.5
- Boss movement multiplier: 1.35
- Telegraph duration multiplier: never below 0.85 of sanitized catalog baseline

Reward curve:

```text
rewardMultiplier =
    1
    + 0.04 * min(F, 50)
    + 0.015 * max(0, min(F, 100) - 50)
```

Treasure chance gains 0.01 per 10 floors and caps at 0.08. Swarm selection weight doubles beginning at floor 25.

All public scaling methods sanitize nonfinite, negative, and extreme inputs. Scaling returns bounded finite values and never mutates an `EnemyDefinition` or `BossDefinition`.

## Runtime budget contract

The later runtime must stream a bounded previous, current, and next-stub window. It must not retain an ever-growing tower stack.

Loaded-window ceilings:

- 3 loaded room roots
- 1 fully active room
- 110 renderers
- 72 colliders
- 0 added lights
- 8 active enemies
- 1 active boss
- 2 particle systems

Per-template ceilings:

- Standard room: 36 renderers and 28 colliders
- Boss room: 44 renderers and 32 colliders
- Stub: 12 renderers and 8 colliders

Every template carries conservative geometry, mechanism, wave, particle, and active-enemy metadata. A boss room plus two stubs fits inside the loaded-window ceilings.

## Public APIs

- `InfiniteSpireCatalog`: immutable templates, merchant kits, miniboss profiles, boss profiles, and lookup helpers
- `InfiniteSpireHash`: stable FNV hashing
- `InfiniteSpireCadenceRules`: floor cadence predicates
- `InfiniteSpireGenerator.TryGenerateFloor`: pure bounded floor-plan generation
- `InfiniteSpireGenerationState`: reusable anti-repetition and boss-history state
- `InfiniteSpireFloorPlan`: reusable 5-room output buffer
- `InfiniteSpireInstanceIds`: stable instance format and parse helpers
- `InfiniteSpireScaling`: pure capped multipliers and sanitized scale helpers

## Integration dependencies

Later runtime integration must:

1. Persist signed `runSeed`, current floor and room, newest 8 template IDs, newest 3 underlying boss IDs, last merchant floor, and last Meridian floor.
2. Restore history oldest-to-newest before generating the next room or floor.
3. Build only the current room at full detail and treat adjacent rooms as bounded stubs.
4. Spawn catalog enemies and bosses from generated references, applying copied scaled values without mutating global catalog definitions.
5. Persist generated puzzle values, remaining wave enemies, opened chest IDs, and purchased stock IDs so reload cannot reroll a room.
6. Chain `TetherfallInfiniteSpireGenerationVerification.VerifyForFullGame` into the full-game verifier when shared-file workers are clear.

The focused verifier is available at `Tetherfall > Verify Infinite Spire Generation` and through:

```powershell
-executeMethod Tetherfall.Editor.TetherfallInfiniteSpireGenerationVerification.VerifyFromCommandLine
```

Unity was not required to define this foundation. Runtime scene building, save-schema migration, economy mutation, world entry, UI, and art remain separate integration work.
