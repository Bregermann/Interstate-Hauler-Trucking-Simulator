# Combined Windows delivery — 2026-10-15

Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`.

Unity6000.4.10f1, Windows x64 (PE8664), Mono Development build. Native final24 exited0; SportsBuilder.Build returned Succeeded, zero errors,13 inherited shader/host warnings,314,604,880 bytes,49.6710765 seconds. Product title is Truck Sports; the normal build uses the original company/preferences. Isolated validation uses LWS Truck Sports Validation.

Startup TruckSports; explicit scenes: TruckSports, TruckBilliards, TruckHockey, TruckBowling, TruckAirHockey, TruckWallball, TruckCornhole. The host build list is untouched. Exact title/company/settings restoration passed both normal success and deliberate-failure fixtures. All1,536 ProjectSettings/Packages baseline hashes remained unchanged. Legacy Wallball/front regeneration was explicitly exercised and retained all six entries; the new hook belongs at the end of UpgradeFront, not in Wallball scene UI creation.

All176 managed DLL hashes and all seven level0–6 hashes match isolated validation23. Only normal/validation company metadata differs. The full accompanying runtime payload includes TruckSports_Data, MonoBleedingEdge, UnityPlayer.dll, UnityCrashHandler64.exe, D3D12/runtime libraries and development symbols. Keep these with the executable. BuildReport summary and runtime/source SHA256 manifests are supplied beside it. Generated builds, logs and player preferences are excluded from Git.

Runtime, hardware limitations and actual publication status are in VALIDATION.md, CHECKPOINT.md and the generated Builds/TruckSports/GIT-DELIVERY.txt. The source branch for publication is codex/truck-cornhole; the root branch contains unrelated unpublished host/Taxi history and must not be pushed.
