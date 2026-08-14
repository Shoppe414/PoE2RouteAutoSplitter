# BossRush v0.2.0 Boss Lists

BossRush v0.2.0 separates **visual identity support** (`../bosses.txt`) from **split targets** (the mode lists in this folder).

## Mode counts

| Mode | Split targets |
|---|---:|
| Campaign 100% | 66 |
| Campaign Any% v0.5 core-progression baseline | 42 |
| Endgame Pinnacle v0.5 | 10 |

Each mode has both a **Predefined** LiveSplit profile and a **Dynamic** profile. Both use the same target whitelist; only the presentation differs.

- **Predefined:** rows are named up front in a canonical clear order. Use this when following that list deliberately.
- **Dynamic:** rows begin as `Boss 01`, `Boss 02`, etc. Each accepted `GONE` event renames the current row to the boss actually completed. This is recommended for nonlinear routing, dual-boss kill-order variance, and practice runs.

## Deliberate exclusions

`excluded-unstable-minibosses.txt` contains the named rare miniboss roster. These are intentionally omitted from `bosses.txt` so BossWatcher does not treat their intermittent/nonstandard health-bar behavior as a reliable completion signal. Beira of the Rotten Pack is now treated as a proper optional boss and is included in Campaign 100%.

`support-only.txt` contains The Plagueling. BossWatcher must recognize it to reconcile the Scourge of the Skies dual UI, but killing/dismissing it is not a BossRush split target.

## Campaign 100% scope

For v0.2.0, **Campaign 100%** means every supported unique boss encounter in Acts 1-4 plus all boss encounters in the three temporary interludes, subject to the UI-stability exclusions above. Ascension-trial/endgame pinnacle bosses are not part of this campaign list; current pinnacle encounters have their own mode.

## Campaign Any% scope

The Any% list is explicitly a **BossRush v0.5 core-progression baseline**, not an official community leaderboard ruleset. It is intended to represent bosses that gate the current campaign progression. Act 4 has league-rotating required islands, so that section is version-scoped and should be revisited when the rotation changes.

## Optional trial boss scope

`../bosses.txt` also contains the complete supported Trial of the Sekhemas / Trial of Chaos boss identities. These identities are intentionally **not** added to Campaign 100%, Campaign Any%, or other existing route target lists simply because the OCR catalog recognizes them. Custom-route users opt in through the Setup UI's trial-boss checklist, and campaign/Act premades can opt into trial progression through the Premade tab. Pinnacle retains Zarokh and Trialmaster as established category targets.

Sekhemas uses a fixed floor-boss sequence. The second-floor Hadi/Rafiq encounter is emitted as two BossWatcher identities but is represented by one composite custom-route milestone. Chaos uses a random three-boss pool, so its boss identities are selected individually instead of assuming a fixed order.
