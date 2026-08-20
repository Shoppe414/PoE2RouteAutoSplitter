PoE2 Route AutoSplitter v3.0.0 Release Candidate
Verification Files

This directory centralizes SHA-256 and run-verification material.

SOURCE PACKAGE
--------------
SOURCE-PACKAGE-SHA256SUMS.txt
    SHA-256 manifest for the source Release Candidate package.

BossWatcher-CHECKSUMS.sha256
    Component-level SHA-256 manifest for the BossWatcher source/support tree.

Verify-SHA256.ps1
    Verifies RUNTIME-SHA256SUMS.txt in an installed/portable build when present,
    otherwise verifies SOURCE-PACKAGE-SHA256SUMS.txt in the source package.
    A specific manifest can also be supplied with -Manifest.

INSTALLED / PORTABLE RUNTIME
----------------------------
Build-Release.ps1 creates RUNTIME-SHA256SUMS.txt here. It covers the immutable
installed runtime while intentionally excluding mutable user settings,
LiveSplit Target output, and diagnostic output.

SETUP / RUN VERIFICATION
------------------------
After Generate / Deploy, SetupUI creates:
    poe2_setup_validation.sha256
    RUN_VALIDATION_README.txt
    Verify-RunValidation.ps1

During a timed run, the generated ASL creates:
    poe2_run_<RunId>.log
    poe2_run_<RunId>_summary.txt
    poe2_run_<RunId>_setup.sha256
    poe2_run_<RunId>.sha256
    poe2_run_current.txt

The per-run _setup.sha256 file is a run-specific copy of the setup manifest so
a later Generate / Deploy does not overwrite the manifest that was associated
with the completed run. The generated setup files themselves remain in
1 - User Setup\LiveSplit Target and may change when a new setup is deployed.

RELEASE ASSET CHECKSUMS
-----------------------
The final installer/portable-ZIP SHA256SUMS.txt remains beside the release
artifacts under the artifacts directory (and is published beside them on
GitHub). An archive cannot meaningfully contain the checksum of itself.
