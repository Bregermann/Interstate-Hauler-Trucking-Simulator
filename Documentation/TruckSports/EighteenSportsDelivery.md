# October20 expansion milestone publication

Tested source commit: `87a97bad7a27c7a50c60094b66e1b1c9a7379901`.

The normal atomic push succeeded to public `main` and `codex/truck-sports-seven-events`. Both remote tips were independently verified at that commit.273explicitly scoped files were committed; private Taxi changes, unrelated imports, host settings and the local private-history preservation branch were excluded. No force-push was used.

The complete current Windows build is at `Builds/TruckSports/TruckSports.exe`. It contains45explicit scenes including the Sports front end and the three new playable modes: Red Light / Green Light, Hot Potato and Balloon Battle. Keep all companion files. Source build information is in `Assets/TruckSports/BUILD-INFO.md`.

Actual Windows gameplay probes passed34,32and34checks respectively; menu/navigation passed292checks. All four processes exited0 on managed SHA256 `7d36b5e6a23ca8a46b7606bee41f16b3037d93671c6c8cf77576a14a514f1ccc`. Native EditMode passed38focused tests. Native Balloon gameplay passed34checks. Exact process receipts, focused XML, actual rendered frames and the runtime file manifest are tracked in `Evidence/2026-10-20-Expansion/`.

This milestone is not the entire18-sport expansion. The remaining15sports and the specific outstanding details/hardware/human checks for these three remain in `TRUCK_SPORTS_ACCEPTANCE_LEDGER.md` and `EighteenSportsProgress.md`. Whack-a-Mole's next implementation and original artwork are local in-progress work, with a recovery snapshot under `.sports-validation/Recovery/Expansion-Next-Slice/`; they are not represented as a playable released scene.

The preservation audit found no changes across887protected source/assets and25host settings. All41existing sport logos remain unchanged. The accepted earlier42-scene build and private-work reconciliation snapshots remain in local recovery storage. The private-history branch remains `codex/local-preservation-2026-10-20` at `f4675ce3d353b42157c942876412bb0731793409` and is not published.

Vendor accounting, exact reused paths/classes and custom semantics are documented in `EighteenSportsVendorAudit.md` and `EighteenSportsProgress.md`. No vendor source was modified and no duplicate vehicle, weather, UI or replay framework was introduced.
