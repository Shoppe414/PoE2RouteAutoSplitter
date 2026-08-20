# PoE2 Route AutoSplitter v3.0.0 — Release Candidate

Version 3.0.0 promotes the accumulated route, localization, diagnostics, and BossWatcher geometry work from the 2.2.1 development line into a new major release candidate.

## Major release highlights

- SetupUI and PoE2 game-language selectors are limited to the nine languages currently supported by Path of Exile 2: English, French, German, Spanish (Spain), Japanese, Korean, Portuguese (Brazil), Russian, and Thai.
- SetupUI UI text plus authoritative boss/area proper nouns are localized where a verified game-data source is available; unresolved proper nouns remain English rather than being guessed.
- Campaign, Custom Route, Trial of the Sekhemas, Trial of Chaos, Vaal Ruins, and Maps setup policies have been consolidated into the current SetupUI.
- Premade and Custom Route start policies include Riverbank start, first-split-zone entry auto-start, and manual start.
- BossWatcher supports localized OCR and now uses height-relative, center-anchored boss-bar geometry so standard 16:9, ultrawide, and super-ultrawide clients do not require manual resolution configuration.
- Dual-boss OCR lanes are independent wide left/right regions rather than subdivisions of the single-boss name ROI.
- GameTimeWatcher remains the optional manual-pause helper while ASL Game Time removes detected loading-screen time.
- Release packaging now separates verification files from documentation and diagnostics.

## Package layout

- `1 - User Setup` — SetupUI launcher, user settings, and generated `LiveSplit Target`.
- `2 - Support Files` — route data, BossWatcher, GameTimeWatcher, build tools, and installer sources/runtime support.
- `3 - verification files` — package/runtime SHA-256 manifests, setup-validation manifests, per-run audit logs/summaries/checksum manifests, and verification helpers.
- `4-README's_and_Diagnostics` — README translations plus centralized diagnostics; diagnostic PNGs are stored in `Diagnostics\images`.

## Release-candidate note

This is a release candidate and should receive another field-validation pass across campaign, trials, maps, manual pause, localized SetupUI display, and common display aspect ratios before the final v3.0.0 release is tagged.
