PoE2 Route AutoSplitter - User Settings
========================================

PoE2AS-Settings.json contains supported user-facing tuning values shared by SetupUI,
BossWatcher, and GameTimeWatcher. The SetupUI Settings button edits the same file.

SetupUI
-------
DefaultLanguage: language used when SetupUI opens. The Settings window applies a saved language immediately and keeps it as the startup default.
Supported codes: en, fr, de, es-ES, ja, ko, pt-BR, ru, th.
SetupUI Language and PoE2 Game Language intentionally use this same supported-language set.
The Windows installer asks for this application language during installation; English is selected by default.
WindowWidthPercent: initial SetupUI width as a percentage of the current monitor work area (25-100).
WindowHeightPercent: initial SetupUI height as a percentage of the current monitor work area (50-100).
DeveloperConsoleDefault: enables developer diagnostic launch behavior for BossWatcher and GameTimeWatcher.
This toggle is exposed only in the SetupUI Settings window. Leave it off for normal runs; enable it when
troubleshooting or collecting diagnostic logs.

BossWatcher
-----------
GoneConfirmMs: identity/single-boss disappearance confirmation (500-30000 ms). Default: 5500 ms.
This is a confirmation grace only. Confirmed boss completion remains backdated to the first
valid missing signal, so increasing the grace does not add the grace period to the recorded split time.

MapGoneConfirmMs: Maps-mode boss disappearance confirmation (100-30000 ms). Default: 5500 ms.
For deterministic maps, BossWatcher first OCR-confirms the expected map-boss identity from map-bosses.json.
The value remains separate because Maps policy requires boss qualification before map exit. Increasing it can
require the runner to remain inside the map until qualification, otherwise the exit remains provisional/unresolved.

Identity-based dual-boss lane removal keeps its dedicated short resolver windows so two separate boss deaths
cannot be incorrectly backdated to the same first-missing signal.

GameTimeWatcher
---------------
ProvisionalTimeoutMs: maximum ESC/Start provisional window while GameTimeWatcher waits for visual confirmation (200-3000 ms).
PauseStackThreshold: required visual match for the centered pause-menu button stack. Default: 62%.
ResumeGameThreshold: required visual match for the Resume Game button/text fallback. Default: 58%.
PauseBannerThreshold: required visual match for the upper pause-menu banner/header fallback. Default: 40%.
ExitPathOfExileThreshold: required visual match for the Exit Path of Exile button/text fallback. Default: 50%.
MtxShopThreshold: required visual match for the Microtransaction Shop screen marker. Default: 70%.

GameTimeWatcher uses image-template matching rather than text OCR for these controls. SetupUI displays the
thresholds as percentages even though the JSON stores them internally as 0.00-1.00 values. Higher percentages
are stricter; values set too low can increase false positives while values set too high can miss the intended UI.
Only change them when diagnostics show a specific detector is too strict or permissive. Restore Defaults returns
all supported values to the tested defaults.

Run snapshots and validation
----------------------------
Generate / Deploy writes the effective settings to LiveSplit Target\poe2_run_settings.json.
BossWatcher and GameTimeWatcher launched from SetupUI read that snapshot, not the mutable master settings file.
The run snapshot is included in poe2_setup_validation.sha256, so submitted audit files identify the exact
user-facing detector settings used by the generated setup. The snapshot also records the generated poe2_map_bosses.json database snapshot, its version, and SHA-256 because that exact database affects Maps-mode boss qualification.

If PoE2AS-Settings.json is malformed or outside supported ranges, SetupUI backs it up as
PoE2AS-Settings.invalid-<timestamp>.json, restores defaults, and reports a warning.

PoE2 game language
------------------
The Settings window has a separate PoE2 game language selection. It controls the language BossWatcher expects on the game screen and is independent from the SetupUI display language. Only authoritative PoE2 game-client languages are offered. GameTimeWatcher also records this selection for language-aware diagnostics while using structure/layout rather than localized text as its primary pause signal.
