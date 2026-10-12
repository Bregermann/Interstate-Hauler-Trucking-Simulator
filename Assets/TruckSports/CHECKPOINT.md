# Truck Sports checkpoint — 2026-10-12

Continue from the saved Truck Sports front end, Truck Billiards and 3v3 Truck Hockey scenes. The six-participant source/AI/team upgrade and Billiards forfeit/held-input fixes extend the existing implementation. Preserve formats, Practice, settings, scene transitions and scoped build configuration.

Final build entry: LWS.TruckSports.Editor.SportsBuilder.Build. Do not use CreateValidateBuild as a validation-only operation: it reauthors the front end and Hockey. SportsUpdateAuthoring explicitly migrates owned scenes; default authoring also applies the new configuration.

Executable: F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckSports\TruckSports.exe. Keep the entire player payload together. VALIDATION.md records actual executable test results, exact host settings restoration and manual boundaries. UPDATE_3V3.md records the reported Billiards reproductions, six-player ownership/contact policy and sustained match evidence. README.md documents controls and future participant expectations.

Native UI Automation/screen-reader attachment can trigger a known Unity 6000.4.10f1 accessibility cleanup crash on shutdown. Releasing the computer-use client restores normal exit in the software checks; this is not a production engine fix. Upgrade the engine in a controlled follow-up to resolve that scenario. No vendor source, host scene list, package or physics/input setting was changed for this update.

Validated six-AI evidence: three full two-minute matches and two rematches, 18 physical goals in 453.0 seconds, zero emergency faceoffs/overflow/exceptions, actual Windows exit 0. Autonomous Billiards completed a rack with 12 real bumper strikes in 199.9 seconds; its final outcome was an illegal/early-eight loss. Final Hockey executable checks passed 118 assertions at both 720p/1080p, including held reconnect rejection and actual fresh South UI resume for both shared owners. Final Billiards-16 passed 54 checks, actual Windows exit 0, no captured runtime errors. Final build 16 succeeded with zero errors/25 existing warnings using SportsBuilder.Build. Exact build metadata and manual/native-engine limitations are in VALIDATION.md.

Branch: codex/truck-sports-3v3. Previous completed sports work is committed/pushed as 3a8801772d305e05cd64da8ba046e18d8ff59f69. Current delivery commit/push hashes are in the final handoff. Preserve unrelated worktree changes; generated builds/logs remain excluded from Git.

VENDOR ASSETS AUDITED / USED / RELEVANT ASSETS NOT USED / CUSTOM SYSTEMS CREATED / VENDOR SOURCE MODIFIED / DUPLICATE VENDOR FUNCTIONALITY CREATED: see README.md and VALIDATION.md for the full report. Vendor source modified: NO. Duplicate vendor functionality created: NO.