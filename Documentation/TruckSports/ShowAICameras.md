# Optional AI cameras — implementation and acceptance

The shared **Show AI Cameras: OFF / ON** control is in the top-right of applicable pre-match setup cards. Default is OFF; the saved preference is shared between sports and survives application restart. It is hidden for a selected shared-arena or hot-seat presentation. It affects camera selection only, never participation, source ownership, physics, AI, scores or replay outcomes.

OFF assigns all supported viewports to humans. One human gets the full screen; two receive two horizontal views; three use the existing large-top/two-bottom arrangement; four use quadrants. When no human participates, each sport keeps its existing spectator/broadcast fallback. For separate-lane sports without a broadcast camera, the first AI lane is the full-screen spectator view; Bowling and Cornhole use a stable third-person spectator pose.

ON selects humans first in roster order, then AI in roster order, with a maximum of four views. Paired Wife Carrying participants share their team's existing camera. A human in a later roster slot cannot lose their view to an earlier AI. Poker's AI view does not reveal private cards. Shared Arena View, forced shared highlights/replays and hot-seat privacy retain their existing behavior.

The existing Heat setup button style is reused at runtime by SportsCameraSetupControl; it is installed once before the standard menu action binding. No authored environment is rebuilt and no Inspector scene changes are overwritten. Regenerated scenes receive the same option through SportsMenuView.Start. Shared SportsPrefs owns persistence, including private keys for explicit development probes.

Integrated owners: Bowling, Cornhole, Cheese Rolling, Keepaway cab, Terrier Racing cab, Base Jumping, Bungee Destruction, Fishing, Target Launch, Buzkashi cab, Skee-Ball simultaneous, Minefield, Mattress Racing, Wife Carrying, Bog Snorkelling, Trampoline Dodgeball chase, Demolition Derby cab, Poker simultaneous, Pedestrian Plowing chase/cab, and Cab Chaos. Shared-only modes have no irrelevant control. Musical Parking's requested future cockpit presentation must join this integration when implemented.

## Evidence

Editor deterministic fixtures: **1,969 checks passed**, including default OFF, saved round trip, 1–12 competitors, 0–4 humans, nonadjacent human slots, capacity, paired cameras, uniqueness, unchanged sources and no blank AI-only fallback.

Actual saved-scene Play Mode regression: **1,963 checks passed across the original 18 applicable modes**, ten human/AI-view combinations per mode, viewport rectangles, HUD ownership, repeated starts and menu cleanup; zero captured runtime errors. The expanded twenty-mode Play Mode pass passed **2,239 checks**, including software controllers for both Plowing variants, actual dedicated HUD ownership, real setup actions and shared/hot-seat exclusions. Two fixture wiring mistakes (Derby option index and Plowing's dedicated HUD) were corrected with assertions retained; their failed evidence is preserved. Windows acceptance is recorded below. Software keyboard sources were injected; this is not physical controller or human playtesting.

Windows 720p/1080p, independent-process preference persistence, software-source isolation, actual AI progress and final build acceptance passed in the version-bound milestone below. This does not complete the larger release queue.

## Vendor accounting

VENDOR ASSETS AUDITED: existing Heat ButtonManager/menu authoring, Cinemachine view adapters, native Camera.rect, shared participant/settings and NWH simulation boundaries.

VENDOR ASSETS USED: saved Heat buttons and existing menu navigation; existing Cinemachine/native camera owners and layouts; SportsPrefs shared persistence.

RELEVANT ASSETS NOT USED: no save framework replacement is needed for the existing single boolean preference; no new camera framework or vehicle controller.

CUSTOM SYSTEMS CREATED: one shared camera-selection preference/policy, a setup-control adapter and explicit regression probes/fixtures.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Cab Chaos visual regression found during Windows review

The initial camera-selection assertions passed, but actual images showed the last-slot human view obstructed by its own world truck mesh. Native controlled renders with world truck meshes hidden removed the obstruction; the same cab remained intact. Initial view heading also retained an earlier owner heading (measured46.73° versus the selected truck112° in one reproduction). PlowViews now initializes each selected heading from its actual owner and reuses the existing Bowling pattern of per-camera SRP exterior suppression/restoration. Other cameras and GPS still see the complete truck. It caches renderer references once; no physics, input gates, scoring or serialized layout changes. The camera acceptance probe now verifies actual begin/end-render ownership and restoration for every selected cab camera. The final Windows rerun passed, including actual render callbacks and clear last-slot/quad cab images.

## Accepted Windows milestone

All 21 strict Windows suites passed for managed assembly `ef8a78590f6119af62340482d3b2c16fcc49be010f1e87e0cfa435fb19ea7b28`. See [version-bound acceptance and limitations](PartyCheeseImpactValidation.md). The 20-mode camera suite passed 2,467 checks plus separate preference-write/read fixtures. Applicable physical, rematch and full-match regressions passed; human/hardware validation remains manual. Earlier pending paragraphs record pre-acceptance history.
