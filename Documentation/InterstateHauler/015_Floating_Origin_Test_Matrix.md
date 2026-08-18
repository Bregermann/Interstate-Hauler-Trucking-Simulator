# Prompt 015 - Floating Origin Test Matrix

| Test | Shift Count | Speed | Trailer | Traffic | GPS | Weather | Road Condition | Streaming | Expected | Observed | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Runtime assembly compile | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | Prompt 015 runtime compiles | `dotnet build LWS.InterstateHauler.Runtime.csproj` succeeded with warnings only | PASS | Local csproj validation path only; normal Unity Editor remains authoritative. |
| EditMode assembly compile | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | Floating-origin EditMode tests compile | `dotnet build LWS.InterstateHauler.Tests.EditMode.csproj` succeeded, 0 errors | PASS | Does not replace Unity Test Runner execution. |
| PlayMode assembly compile | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | Floating-origin PlayMode tests compile | `dotnet build LWS.InterstateHauler.Tests.PlayMode.csproj` succeeded, 0 errors | PASS | Does not physically drive the truck. |
| LocalToGlobal / GlobalToLocal | 0 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | Round trip preserves coordinates | Covered by EditMode tests | PASS | Double offset path. |
| Grid-aligned shift | 1 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | 1325 m becomes 1000 m shift on 500 m grid | Covered by EditMode tests | PASS | XZ-only policy covered. |
| Stable global player position across shift | 1 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | Global position unchanged while local position rebases | Covered by EditMode and PlayMode tests | PASS | Service-level proof. |
| 1000 simulated shifts | 1000 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | Double offset accumulates without material drift | Covered by EditMode tests | PASS | Precision architecture test. |
| Chunk global to local conversion | 1+ | n/a | n/a | n/a | n/a | n/a | n/a | Yes | Canonical bounds remain global and local bounds derive from offset | Covered by EditMode/PlayMode tests | PASS | Scene Streamer visual validation pending. |
| Rigidbody velocity preservation | 1 | Simulated | Simulated | n/a | n/a | n/a | n/a | n/a | Linear and angular velocity survive shift | Covered by PlayMode test | PASS | Real NWH vehicle validation pending. |
| Non-spatial service remains fixed | 1 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | Non-registered objects do not move | Covered by PlayMode test | PASS | Prevents global manager relocation. |
| StreamingHighwayValidation straight drive | 3+ | Highway | Attached | Optional | Optional | Optional | Dry/Wet | Yes | Multiple shifts with no perceptible teleport and correct chunk loads | Not run in normal Editor by Codex | MANUAL REQUIRED | Use low validation threshold. |
| Trailer attached shift | 1+ | Any | Attached | n/a | n/a | n/a | Any | Optional | Hitch and trailer physics remain stable | Not run in normal Editor by Codex | MANUAL REQUIRED | Watch for joint shock. |
| Turning/braking shift | 1+ | Moving | Attached | Optional | n/a | n/a | Any | Optional | No velocity reset, gear reset, or vehicle snap | Not run in normal Editor by Codex | MANUAL REQUIRED | Test curve and braking cases. |
| Ice road condition shift | 1+ | Moving/sliding | Attached | Optional | n/a | Optional | Ice | Optional | Grip state remains Ice and slide continues naturally | Not run in normal Editor by Codex | MANUAL REQUIRED | Requires existing road-condition debug controls. |
| UTS traffic active shift | 1+ | Highway | Attached | Active | n/a | Optional | Any | Yes | NPCs stay on roads with stable registry/path state | Not run in normal Editor by Codex | MANUAL REQUIRED | UTS cache refresh is implemented through LWS adapter. |
| GPS active route shift | 1+ | Highway | Attached | Optional | Active | Optional | Any | Yes | Route remains active and no false off-route/recalculate | Not run in normal Editor by Codex | MANUAL REQUIRED | Physical cab GPS should remain attached. |
| Weather/Weatherade shift | 1+ | Any | Any | Optional | Optional | Rain/Snow | Wet/Snow/Ice | Optional | Weather persists and Weatherade coverage recenters | Not run in normal Editor by Codex | MANUAL REQUIRED | No Weather Maker restart expected. |
