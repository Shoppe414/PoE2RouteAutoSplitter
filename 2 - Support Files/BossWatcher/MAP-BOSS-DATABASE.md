# Map Boss Database — development schema

`map-bosses.json` is the ordinary-map completion-boss catalog used by BossWatcher v0.3.3.

## Goal

Maps can contain unrelated Unique bosses from endgame mechanics. A structural boss bar by itself is therefore not sufficient evidence that the ordinary map-completion boss has been defeated.

For a deterministic map entry, BossWatcher now uses this sequence:

1. ASL supplies the current `Map<name>` area ID through `poe2_boss_context.txt`.
2. BossWatcher resolves that area against this database.
3. A structurally valid boss UI triggers narrow OCR.
4. OCR is matched only against the boss or bosses listed for that map.
5. Non-matching bosses are ignored.
6. Once the expected map boss is armed, normal disappearance tracking applies.
7. `MAP_GONE` is emitted only after the configured disappearance confirmation window, backdated to the first missing signal.

Unknown and special/random-completion maps currently use structural fallback so this database can be expanded without making unlisted maps impossible to test.

## Map entry fields

- `MapName` — readable Atlas/map name.
- `AreaIds` — optional exact internal area IDs observed in `Client.txt`. Matching also normalizes `Map<name>` IDs against `MapName`.
- `CompletionType`
  - `boss` — deterministic completion boss; database OCR gating is active.
  - `event` — special event completion; currently structural fallback.
  - `random-bosses` — random/special boss sequence; currently structural fallback.
  - `none` — no fixed boss listed; currently structural fallback.
  - `unknown` — source does not currently identify a deterministic boss.
- `BossRule`
  - `any` — any listed boss is accepted as the map's expected boss.
  - `all` — all listed identities are expected in a multi/dual encounter.
- `Bosses` — map-completion OCR identities.
- `SourceStatus` / `Notes` — curation and field-test notes.

## EventBosses

`EventBosses` does not qualify a map. It exists to identify known unrelated event bosses in diagnostics.

Initial development entries:

- Delirium — Omniphobia, Fear Manifest
- Delirium — Kosis, the Revelation

If one of these appears in a deterministic map where it is not the expected completion boss, the debug log should contain:

```text
MAP_EVENT_BOSS_IGNORED | area=... | map=... | mechanic=Delirium | boss=...
```

An unrecognized non-matching Unique boss is still ignored and logged as:

```text
MAP_UNEXPECTED_BOSS_IGNORED
```

so the exclusion policy does not depend on having every event boss catalogued in advance.

## Source / validation status

The initial map-to-boss seed was transcribed from the current Path of Exile 2 Wiki `List of maps` page on 2026-08-17. It is a community-maintained source and should be treated as a development seed, not final authoritative game data.

The following entries especially need direct field confirmation because the source lists multiple bosses or unusual completion behavior:

- Alpine Ridge
- Arid Plains
- Razed Fields
- Sulphuric Caverns
- Woodland
- The Ezomyte Megaliths
- The Silent Cave
- Barren Atoll
- Sloughed Gully
- Hive Fortress

When a `Client.txt` area ID differs from the normalized `MapName`, add the exact ID to `AreaIds`.

## Audit

SetupUI copies the current support database to `LiveSplit Target\poe2_map_bosses.json`, records that snapshot's database version and SHA-256 in `poe2_run_settings.json`, and launches BossWatcher with the generated snapshot. Both files are included in the setup-validation manifest, so submitted run files identify and validate the exact map-boss database used at runtime.

## Fail-closed behavior
Unknown map IDs and special completion types do not use structural boss fallback. A missing database entry therefore causes a missed automatic qualification rather than a false map completion.
