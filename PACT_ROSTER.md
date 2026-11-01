# Threadbind Accord roster foundation

The consent-led roster is integrated into saves, field simulation, combat coordination, portable Camp UI, and six wild Echosworn encounters. Player-facing language uses **Threadbind Accord**, **rescue**, and voluntary work preferences. `CapturedPactActor` is an internal compatibility name; it is never shown to the player.

## Included files

- `Assets/Scripts/TetherfallPactRoster.cs`
  - Species, combat, resonance, movement, art, work-preference, stamina, and morale data.
  - Per-instance and roster save records.
  - Stable normalization, old-save story-link seeding, slot rules, and Concord.
  - Partial `TetherfallGame` roster APIs, reserve simulation, field formation, actor activation, save hooks, and settlement UI helper.
- `Assets/Scripts/TetherfallThreadbindAccords.cs`
  - Exact-ID defeated-wild eligibility.
  - Time-limited rescue interaction implementing `IWorldInteractable`.
  - Lightweight `CapturedPactActor` using `ExpeditionFollowerMotor`, a reusable bounded target buffer, and staggered assists.
- `Assets/Scripts/TetherfallCapturedPactArt.cs`
  - Original collider-free articulated fallback art and its allocation-free pose driver.
- `Assets/Editor/TetherfallPactRosterVerification.cs`
  - Edit-mode verification for identity, eligibility, normalization, migration, limits, diversity, budgets, and copy.

## Authored species

Six wild species have dedicated gameplay encounters:

- Alderwake Basin
  - `species.wild.alderwake.mossbell_strider`: Grove Warden, grounded longstrider.
  - `species.wild.alderwake.copperquill_nester`: Gale Skirmisher, canopy glider.
- Morrowfen
  - `species.wild.morrowfen.tideveil_skimmer`: Tide Mender, amphibious skimmer.
  - `species.wild.morrowfen.gloamreed_lanternback`: Gloam Controller, low hoverer.
- Cinderwold Verge
  - `species.wild.cinderwold.emberhorn_burrower`: Ember Vanguard, grounded burrower.
  - `species.wild.cinderwold.prismtail_kitelizard`: Prism Resonator, thermal glider.

Each has a unique dedicated enemy ID beginning with `enemy.wild.`. Eligibility is an exact reverse lookup. Prefix alone is not sufficient, so an unknown `enemy.wild.*` value, a generic or human enemy, an elite, a boss, a dungeon summon, and a legendary story Pact are rejected.

Story adapters link Bramblet, Mistling, and Cinderwing to the same ordered field roster as rescued wild Pacts. Their existing `PactCreature` is the live actor, so no `CapturedPactActor` duplicate is created. Slateback remains a separate mount and is explicitly ineligible. Story records point to authoritative legendary Pact IDs and never copy, replace, or mutate `PactProgressSaveData`.

## Roster and field rules

- A record keeps:
  - Stable per-instance ID.
  - Species ID.
  - Optional nickname and epithet.
  - Level, experience, and roster bond.
  - Stamina and morale.
  - Reserve, active-field, or settlement-preference state and field slot.
  - Settlement, station, and bed IDs.
  - Origin ID and Accord timestamp.
  - Optional authoritative story Pact link.
- Active field IDs are ordered, deduplicated, record-backed, and hard-capped at five.
- Clear-rank slots are explicit:
  - Rank 1: 2.
  - Rank 2: 3.
  - Rank 4: 4.
  - Rank 6: 5.
- `PactRosterRules.GetConcordMultiplier` adds `0.04` for each additional unique active resonance affinity, from `1.00` to a capped `1.16`.
- Command focus must name an active instance. Invalid focus normalizes to the first active instance.
- Outside Threadlink, `T` cycles command focus through every active roster member. Command focus owns traversal and greater-form passives; all active Pacts may auto-assist and answer bounded combat coordination.
- `ActiveEchosworn` remains only the compatibility alias for a focused story `PactCreature`. It is not an extra field slot.
- Settlement-assigned Echosworn are removed from the field list and never receive a field actor or catch-up warp.
- Reserve and settlement records remain abstract. Their stamina and morale recover from elapsed UTC time, capped to twelve hours per update.
- At most five field actors exist. Actors outside the near-simulation radius deactivate. Outside safe settlements, a stranded active actor can use the bounded follower-motor recall. Safe-settlement actors do not perform a long warp.

## Consent and work preferences

Every species defines two to four authored voluntary preferences rated from 1 to 5. `TryAssignPactSettlementPreference` accepts an assignment only when:

- The species expressed that preference.
- Morale is at least 25.
- A settlement and station are provided.

The assignment records the chosen settlement, station, and optional bed, removes the instance from the field, and prevents a field actor from being created. Clearing the preference returns the instance to abstract reserve; it does not automatically force it into the field.

## Threadbind opportunity

`OnEligibleWildEchoswornDefeated(enemyId, position, originId)` performs an exact catalog lookup. A valid defeat creates a 24-second `IWorldInteractable` rescue opportunity before the enemy object is destroyed. Interacting records the Echosworn's offered Threadbind Accord and adds the new stable instance to the roster. It fills an open field slot when possible; otherwise the new Accord begins in reserve.

Gameplay defeats use a stable `enemyId@rounded-spawn-x:rounded-spawn-y:rounded-spawn-z` origin while placing the rescue at the actual defeat point. Repeated calls for that origin return the existing opportunity while it remains active.

## Runtime budgets

- Maximum active and instantiated field actors: 5.
- Actor activation distance: 46 metres.
- One reusable `Collider[32]` per actor.
- At most one non-alloc assist query per assist tick.
- Assists receive a stable per-instance startup offset to avoid synchronized queries.
- No actor collider, rigidbody, light, animator controller, or per-frame collection creation.
- Original fallback art uses at most 24 renderers and 4 owned materials per actor.
- Rescued-wild reserve records have no GameObject or update loop; an existing story adapter is deactivated and frozen while its linked record is in reserve.
- Threadbind opportunity lifetime: 24 seconds, with at most 8 renderers.

## Integrated runtime

- Main save schema 24 serializes `PactRosterSaveData pactRoster` before the player-settlement snapshot; normalization uses the saved Wayfinder rank.
- The game initializes, updates, writes, and restores roster state in the main lifecycle. Legacy Pact names and progression fields remain migration inputs.
- Active story and rescued wild Pacts share one five-actor budget, one formation, command-focus selection, tactical `V`, Crossweave selection, Threadlink responders, Overtone affinities, and active-ally accounting.
- Concord scales Pact auto-assist and tactical potency by active affinity diversity and remains capped at `1.16`.
- Only command focus supplies traversal and greater-form passives. Tactical area responses are limited to the focus; additional Pact responders use compact target-local actions.
- The Expedition HUD lists all five possible active Pacts with role and focus marker.
- The portable Camp exposes **Pact roster** beside Services and Pact Unfolding. The integrated settlement Workers tab consumes the same roster authority and stores only stable assignment preferences.
- Threadway and player teleport explicitly recall active field Pacts. Reserve records never own actors, and both city safe zones and the claimed Threadheart suppress distant visible pop-in.

## Verification

Run `Tetherfall > Verify Pact Roster Foundation` in edit mode. It checks:

- Stable and unique catalog, art, species, and gameplay enemy IDs.
- Rejection of current, elite, boss, runtime, story, and unmapped identities.
- Roster dedupe, worker exclusion, field clamp, and focus repair.
- Rank slot milestones.
- Concord affinity diversity.
- Deterministic schema-24 save normalization, serialized-field presence, and Pact-before-settlement lifecycle hook ordering.
- Idempotent story-link migration, active existing-actor adapters, Slateback exclusion, and no legendary progression mutation.
- Six exact wild enemy definitions and placements, defeat-hook ordering, 24-second origins, and camp/wave bookkeeping.
- Five-actor, nonalloc query, formation, recall, opportunity, renderer, material, collider, rigidbody, and light budgets.
- Consent-led player copy.

The roster verifier is edit-mode only. Runtime validation still requires a later Unity smoke pass.
