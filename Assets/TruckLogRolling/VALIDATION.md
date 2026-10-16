# Log Rolling native acceptance

2026-10-16 · Unity6000.4.10f1. Source stage only; final combined Windows production build remains pending.

## Reproduced causes and fixes

- Initial vendor equilibrium FAILED. Closed80triangle6m×22m hull611.8539m³,300000kg: measured22.613MN upward force at waterline, incorrect unloaded height2.14–2.81m. Native IL inspection proved TriangleSurface calls Sin(Vector3.Angle) without degree conversion. LWS corrects exposed active wet/dry triangle-area metadata using cross-product area, then calls unchanged vendor ComputeAllForces. No replacement buoyancy solver or patched DLL. Original/imported DLL SHA256A9F70FB389DCCDB922DACF9A4E265BFC6BEEDFE7805443FC76C95C4C85B2E6BD.
- Isolated native preview-scene force AFTER:2.99934MN at waterline, zero spurious angular velocity. Actual20second equilibrium y.04256487..04268962, peak.5415137m/s,1000solver steps. Failure retained LogInitialAreaBug; force/IL diagnostics under Logs/TruckSports/SevenSports.
- Off-center starting grid slid off before fighting. Runtime+authoring moved starts to the centerline; actual3second unforced support fixture passes. No hidden balance force.
- Physical bump fixture then FAILED: wheel angular speed climbed toward450rad/s with almost no forward chassis travel. NWH tire rating was5400N despite12000kg truck. Public MaxLoad now58860N, following vendor twice-static-load guidance. Genuine NWH driving now creates a real truck-to-truck contact on the buoyant log. No injected impulse/score. Earlier failures retained LogSideStartsFocused /LogCenterlineBeforeTireRating.
- Original branchy model did not visibly match the hull; a rotated import then exposed world/local fit mismatch. Native LogPresentationAuthoring now bakes owned Old Sea Port model transforms into a6x6x22metre mesh, with actual bark/endgrain. Physical hull unchanged.
- River depth graph vanished under orthographic view. Existing Fishing URPLit adapter reuses River Modeler normal map and DOTween flow; pond now visibly reads as water. No custom fluid simulation or vendor shader edit.

## Final physical games

LogEvidence/result.txt,0errors, saved final scene, actual NWH wheels/PhysX and owned buoyancy. No injected outcomes.

| Difficulty | Participants | Heats | Wins | Draws | Water falls | Truck contact callbacks | Max roll |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Easy |2AI|7|5/2|0|7|28|71.66°|
| Normal |4AI|9|0/2/0/5|2|29|86|73.34°|
| Hard |4AI|17|5/3/3/4|2|53|172|74.29°|

Every match reached a legitimate first-to-five winner. Exact paused log pose/velocity/heat, real falls, rotating log, decisive recorded replays and clean rematch passed. Peak truck speeds14.76–15.01m/s include physical falls; powered limits remain7m/s forward/4reverse through NWH motor authority. Contact counts are callbacks from both trucks, not unique impacts. Earlier zero-error games before tire/grid correction had no contacts and are superseded, not accepted as final gameplay coverage.

LogFocusedEvidence/result.txt final0errors after grid/tire corrections: actual support, genuine ordinary-control bump, four mixed keyboard/whole-controller/opposite-stick source ownership, fresh brace lowering only owning COM with finite cooldown, exact pause, software device removal/replacement, same-round resume, rematch and collection return/one listener. No physical hardware was used.

Five controlled world+UI Camera.Render captures at1280x720/1920x1080/1440x900/1280x960/2560x1080. Final four-player captures in LogEvidence; actual1280x720 inspected after final model/water fixes. These are controlled composites, not ordinary player-window capture or a sustained GPU benchmark. Final native scene/menu/31explicit scene-list integration validated.

Remaining: final31sport Windows build smoke/regressions and sustained rendered performance; human balance/brace/AI feel, sound/vibration and real controller testing. Do not claim a completed Windows build from this native stage.
