# Truck Sports master repair — October 17, 2026

All ten requested repair packages are implemented, saved and validated. The combined Windows x64 Development build contains all 31 implemented sports in 33 explicit Sports-only scenes. TruckSports starts first; the normal catalogue has 30 entries and Wallball remains manually reachable in Experimental Scrap Heap.

Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the complete accompanying folder together. Build `2026-10-17T10:53:04.7155517Z`, zero errors / 47 incremental warnings, 1,191,046,536 bytes, 00:01:39.5986327. Managed assembly SHA256 `1ded15c3f1629166224a67d36d754d2d821b22a8cdf587fbfdecdf42efa9bf4d`. All 25 protected ProjectSettings files match their pre-build hashes, including the host build configuration; the deliberate failure-restoration fixture also passes.

The sole delivery folder includes TruckSports.exe, TruckSports_Data, UnityPlayer.dll, MonoBleedingEdge, UnityCrashHandler64.exe and the remaining generated companions. AMD64 PE machine 0x8664 verified. Unity 6000.4.10f1, URP 17.4, Mono, Development.

Build uses LWS.TruckSports.Editor.SportsBuilder.Build with explicit Sports-only scenes and TruckSports startup. No host build-list or Taxi settings are changed. All 25 pre-build ProjectSettings hashes match after success; deliberate failure restoration also passed. Generated build files are excluded from Git.

All 33 Windows acceptance suites have explicit version/asset/compiled-method provenance; see [full results](../../Documentation/TruckSports/MasterRepairValidation.md). Original licensed packages must be installed for a fresh source build. Verified Git delivery is recorded below.

## Verified Git delivery

Implementation `bfd14cadbc72ceab0b865c912a6e8661f67c198e` on `codex/truck-sports-seven-events` was pushed successfully to that branch and GitHub `main`, verified at 2026-10-18T00:59:19.606915+00:00. **Both previous relevant Truck Sports work and this repair update are committed and pushed to main.** The public Sports tree matches the reviewed local Sports implementation. Unrelated local Taxi/host/vendor work and private Taxi ancestry are preserved and excluded; generated builds, logs, caches and original licensed package prerequisites are excluded.

This receipt is committed as a subsequent clean public descendant. The delivered folder's GIT-DELIVERY.txt records the final verified public tip after the receipt push. All build, test and remaining-manual limits above still apply.
