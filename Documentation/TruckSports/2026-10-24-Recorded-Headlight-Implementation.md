# Recorded headlight implementation — October24,2026

The recorded-vehicle headlight repair is implemented; Windows release acceptance remains open. Dock Diving and Giant Swing reuse their existing authored beams during playback through a read-only recorded-view lookup. Original NWH light state controls, replay motion, physics, cameras and scoring remain authoritative. No new lights or recorded hierarchy objects were added.

The original physical Night/Clear Dock test measured86.34269m between its beam and the visible recorded truck. The same unchanged reproduction passed with offset0 after binding its original recorder. Expanded testing revealed that Swing records a scaled visual child; the final mapping preserves that child's scale, offset and rotation. Final original-physics tests for both venues passed with offset0, rotation, pause/rewind, live fallback, setup cleanup, rematch, Day-off and original light identities. The initial failed/incorrect-frame cases remain archived.

Only existing beam coordinator documents changed in the saved scenes: all19877Dock and3965Swing documents retained, no new scene objects/lights, and inspected inert default reserializations restored. Production/editor changes: SportsNightHeadlights.cs, SportsMotionReplay.cs, DockReplay.cs and SportsEnvironmentAuthoring.cs. A focused native fixture and development-only helper extend the original DockSlangProbe/SwingProbe full-match workloads under an explicit validation flag. Validation preferences are isolated/restored. No build/load-time scene regeneration is introduced.

Actual evidence:

* Same unchanged Dock reproduction:1PASS. Final two-venue lifecycle tests:2PASS,46.4812s test runtime. Existing five-venue day/night regression:1PASS,22.1954s test runtime. Six controlled native frames individually inspected, including original/fixed Dock frames; these are not Windows or human acceptance.
* Candidate Swing:174checks/0errors, actual process35864 exit0 after359.93s. Original eight-AI/three-attempt match completed with totals125,200,100,125,150,150,125,250. All24physical releases/landings, original score accounting, recorded headlight checks and results passed. All six fresh Windows frames inspected.
* Candidate Dock: two original complete eight-AI/three-attempt runs each completed134scripted checks/0errors, including actual replay beams, original scoring, rematch and menu cleanup. Processes38072 and15080 nevertheless exited0xC0000005 during native shutdown. Both are FAILED process acceptance; they are not counted as successful Windows runs. Two selected initial Dock night-replay frames inspected; all other failed-run captures remain locally retained without claiming visual review.
* Preserved original60d4716f Dock Night/Clear control:34checks/0errors, actual process36784 exit0 after152.65s. A fresh actual flight frame confirms Night sky/venue; the original isolated DockSlang preference namespace was reused, not ordinary player settings.

Native diagnosis: matching installed Unity DLL/PDB symbolication resolves the current fault at UnityPlayer+0x1d41c9 to PlatformAccessibilityTestUtilities::GetWindow. Both current dumps read address0x138 and match the same DLL/PDB signature in October19 dumps, predating this repair. The preserved baseline's one clean run does not establish absence of this intermittent problem. Unity lists the Windows UI Automation shutdown issue UUM-146676 as fixed in [6000.5.5f1 release notes](https://unity.com/releases/editor/whats-new/6000.5.5f1). The earlier process-local UiaDisconnectAllProviders workaround was already tested, failed and removed; it was not reintroduced. The user selected Keep the current Editor for now, so6000.4.10f1 remains in use. Raw dump memory stays in its original private local folder; only metadata/symbol summaries are recorded here.

Candidate: Builds/TruckSportsCamera/TruckSports.exe,60saved scenes/548files, zero build errors/90warnings,423scoped owned C# inputs archived locally. Full payload72a420fc5af959523bbd454b85ea8b80563fd7ee6028e00de6c0b7f568cf6455; managed9ba99dce2fef2f010c62c846ad087b27ae26cb98ed1f4e3f80a899aa9adac288. It was NOT promoted. Canonical Builds/TruckSports/TruckSports.exe remains accepted60d4716f, with its full prior recovery copies retained.925unrelated pending files remain byte-identical. Scoped local checkpoint only; no new public push beyond fulfilled3a337c13approval.

Remaining: clean Dock Windows process acceptance is blocked by the recorded native UI Automation shutdown issue on the retained runtime. Wider Swing venue lighting is visibly dim and remains open under Q22-SKY-LIGHT-01. No human driving, physical-controller, listening, isolated profiling, leak or extended-soak acceptance is claimed. Native1080p rewound frames immediately after Resume include existing menu fade and are not complete UI acceptance. Parent Q22-SKY remains PARTIAL, followed by completeCarnival0.6 thenImpossible0.7.

## VENDOR ASSETS AUDITED

NWH LightSource/VehicleLight/LightsManager public APIs; original recordings/hierarchies; Weather Maker match clock; existing Heat/TMP/DOTween UI; matching installed Unity symbols and the earlier shutdown investigation.

## VENDOR ASSETS USED

NWH light states, existing authored Unity beams, Weather Maker Night/Clear and original replay geometry/physics. Installed MSVC symbolizer with matching Unity PDB for local diagnosis.

## RELEVANT ASSETS NOT USED

No extra lighting, animation, camera, physics or replay package is needed for a recorded-pose binding. No engine upgrade was made against the user's retained-Editor choice.

## CUSTOM SYSTEMS CREATED

Small read-only recorded-view interface and optional beam binding; focused development acceptance and local metadata-only diagnostic tooling. No replacement light renderer, physics or replay framework.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
