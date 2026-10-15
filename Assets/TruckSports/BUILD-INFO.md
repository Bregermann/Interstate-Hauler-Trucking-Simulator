# Truck Sports Windows development build — 2026-10-15

Executable: `C:/Users/Jeremy/.codex/worktrees/truck-bowling-delivery/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`

Unity6000.4.10f1 · Windows x64 (`PE8664`) · Mono Development · normal DefaultCompany preferences · Truck Sports branding. Startup TruckSports. Explicit twelve-scene list includes all eleven sports. Native `LWS.TruckSports.Editor.SportsBuilder.Build` succeeded0errors30warnings,430344831bytes,51.3696071seconds. Required UnityPlayer/crash-handler/data/Managed/Mono files are present.

Actual injected pointer/keyboard normal-build smoke passed10 assertions; four-open-canopy720/1080 visual checks passed13; normal-build physical Dock/replay/record/eight-AI checks passed68. The normal build also passed119 assertions across22 sport transitions with maximum new rosters, pause/rematches and explicit weather cleanup. Full match, fixture and performance evidence: Documentation/TruckSports/ExpansionValidation.md and ExpansionEvidence. No human or real-controller acceptance claimed.

PlayerSettings and EditorBuildSettings SHA256 match exactly before/after scoped success/failure validation. Main F: Taxi checkout and its previous build were preserved; this updated build lives in the isolated managed Sports checkout.

Code implementation: a53cc7ea8cbf47aa846d6934537fa9deab9258a9 plus existing-sport e7e07e652d8263ca13884d33e5cc4d4ca9b224e1, both pushed. Final documentation/push tip is recorded in the adjacent generated GIT-DELIVERY.txt. Required copied cross-task dependencies remain local/unstaged pending explicit approval; see Documentation/TruckSports/ExpansionDependencyRepair.md. Clean-clone reproducibility is blocked, while this self-contained player runs locally.

Hashes:
- TruckSports.exe: `8b85cc34becb9a5e63dc14325c0266cf01cdaf798f5f35638fc01c0be0ce7878`
- UnityPlayer.dll: `cb471a5ffda8a2083bb5d13193b660c94e82273582e048e4c2921b413c94cd8d`
- TruckSports_Data/Managed/Assembly-CSharp.dll: `69fc11a0cf6623851c728e77dcbf0edb80c18ce5205043f622d3527c1641ced8`
- TruckSports_Data/globalgamemanagers: `5bb0d3ab0b65fa5d2e40ef437ac330b1a9aa870370e470057584c252663fbfe7`
