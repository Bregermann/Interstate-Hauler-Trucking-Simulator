# Pedestrian Plowing update — October 18, 2026

Two saved menu entries share one sport: **Pedestrian Plowing** (arena/chase/cab) and **Plowing · Cab Chaos** (full Taxi driver interior, independent live GPS, animated wheel/free look and jug challenge). Up to eight competitors, four local humans, solo and AI-only play use the existing offline lifecycle. The finite crowd occupies nine connected city blocks with burning buildings, flipped burning cars and festival dressing. Character appearances remain independent of points.

Windows x64 Development executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep its entire accompanying folder. Built through `LWS.TruckSports.Editor.SportsBuilder.Build` at2026-10-18T03:52:00.8606738Z: zero errors,47 warnings,1,246,555,118bytes,1m46.245s. Managed assembly SHA256 `e48749992b233aa8f5c061cccb596b3ac9aa768991a87fe3ebfd8e2f581896bf`; build GUID `90f93087e950471c9864a98c5e0f51e5`. All25 protected settings restored; deliberate failure restoration passed. All35 explicit Sports-only scenes and TruckSports startup are retained. Generated builds are excluded from Git.

Eight final Windows suites completed with zero captured runtime errors:384-crowd density, LOD/accessories, actual contact scoring/replay, cab/input/jug/reconnect, smoke/rematch,720p/1080p view layouts and two60-second four-view performance samples. Twelve earlier baseline suites include six complete eight-AI matches, five legacy cab modes and the35-scene/five-aspect tour. These are version-bound automated/injected-input/real-PhysX results, not human or physical-controller testing. Chase/cab mean frame times were29.58/29.28ms; performance is specific to this PC with user applications left running.

[Implementation, evidence and manual limits](../../Documentation/TruckSports/PedestrianPlowingValidation.md) · [Crowd/model/spawn customization](../../Documentation/TruckSports/PedestrianPlowingCustomization.md) · [Checkpoint and ordered outstanding queue](../../Documentation/TruckSports/PedestrianPlowingCheckpoint.md) · [Vendor audit](../../Documentation/TruckSports/PedestrianPlowingVendorAudit.md).

Pedestrian Plowing implementation `cd0a603b` is committed and pushed to the existing Sports branch and GitHub main; see the verified receipt below. Historical receipts apply only to their named milestones. Party/Cheese, Show AI Cameras, Fare Fight and Goddess repairs remain outstanding and are tracked in their ledgers. Human/controller, haptics, audio balance and subjective gameplay acceptance remain manual.

## Verified Pedestrian Plowing Git delivery

Implementation `cd0a603b925c2abc0421a976abb88dad22161a06` on `codex/truck-sports-seven-events` was pushed successfully to that branch and GitHub `main`, verified at2026-10-18T04:17:19.324524+00:00. Local scoped source commit: `960b1cec110d39dfb210478867bab103d0dff998`. **Both previous relevant Truck Sports work and the Pedestrian Plowing update are committed and pushed to main.** Unrelated Taxi/host/vendor work and private Taxi ancestry were preserved and excluded. Builds, caches, logs and original licensed package prerequisites are excluded.

The built assembly and20-case evidence are identified above; later Party/Cheese, Show AI Cameras, Fare Fight and Goddess work is still outstanding. This receipt is a separate documentation commit. The delivered folder's GIT-DELIVERY.txt records the final verified public tip. Post-build source cleanup removed only one blank line at the end of PlowRound.cs; no gameplay behavior changed.

## Historical milestone documentation

# Truck Sports master repair — October 17, 2026

All ten requested repair packages are implemented, saved and validated. The combined Windows x64 Development build contains all 31 implemented sports in 33 explicit Sports-only scenes. TruckSports starts first; the normal catalogue has 30 entries and Wallball remains manually reachable in Experimental Scrap Heap.

Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the complete accompanying folder together. Build `2026-10-17T10:53:04.7155517Z`, zero errors / 47 incremental warnings, 1,191,046,536 bytes, 00:01:39.5986327. Managed assembly SHA256 `1ded15c3f1629166224a67d36d754d2d821b22a8cdf587fbfdecdf42efa9bf4d`. All 25 protected ProjectSettings files match their pre-build hashes, including the host build configuration; the deliberate failure-restoration fixture also passes.

The sole delivery folder includes TruckSports.exe, TruckSports_Data, UnityPlayer.dll, MonoBleedingEdge, UnityCrashHandler64.exe and the remaining generated companions. AMD64 PE machine 0x8664 verified. Unity 6000.4.10f1, URP 17.4, Mono, Development.

Build uses LWS.TruckSports.Editor.SportsBuilder.Build with explicit Sports-only scenes and TruckSports startup. No host build-list or Taxi settings are changed. All 25 pre-build ProjectSettings hashes match after success; deliberate failure restoration also passed. Generated build files are excluded from Git.

All 33 Windows acceptance suites have explicit version/asset/compiled-method provenance; see [full results](../../Documentation/TruckSports/MasterRepairValidation.md). Original licensed packages must be installed for a fresh source build. Verified Git delivery is recorded below.

## Verified Git delivery

Implementation `bfd14cadbc72ceab0b865c912a6e8661f67c198e` on `codex/truck-sports-seven-events` was pushed successfully to that branch and GitHub `main`, verified at 2026-10-18T00:59:19.606915+00:00. **Both previous relevant Truck Sports work and this repair update are committed and pushed to main.** The public Sports tree matches the reviewed local Sports implementation. Unrelated local Taxi/host/vendor work and private Taxi ancestry are preserved and excluded; generated builds, logs, caches and original licensed package prerequisites are excluded.

This receipt is committed as a subsequent clean public descendant. The delivered folder's GIT-DELIVERY.txt records the final verified public tip after the receipt push. All build, test and remaining-manual limits above still apply.
