# Giant Swing night venue — October24,2026

The saved Giant Swing venue now has six purchased-model light fixtures covering its gantry, targets and landing meadow. The original SportsMatchTime/Weather Maker clock switches them on for Night/Evening and off for Day/Morning. No runtime fixture generator or second time/weather owner was added.

Root cause: the existing scene's authored nightLights list was empty, and the shared venue-authoring path had no Swing branch. Its moving truck low beam could not illuminate the wider venue. A native pre-fix test failed with expected6/actual0, matching the previously inspected actual Windows night images.

SportsEnvironmentAuthoring.InstallSwingVenueLighting is an explicit additive menu operation, never called by builds, Play Mode or scene loading. It retains existing designer fixtures, uses the owned Toon Gas Station Streetlight_1C model fitted to its actual height, and places six non-shadow spotlights outside active lanes. SportsNightReplayAcceptance adds two flag-only Windows checks; normal gameplay, physics, cameras, scoring and original recordings are untouched. One sport-owned URP material was created from the installed model's original material.

Scene review retained all3965original YAML documents.68new documents describe six rendered fixtures and six lights, with no added colliders, joints, rigidbodies or scripts. Only two existing documents change: one parent child reference and the existing clock's six lamp bindings. Inert default serialization was restored to original bytes.

Actual validation:

* Final native PlayMode2PASS,65.3089s: original physical AI flight/replay lifecycle and focused saved-fixture rendering/lifecycle. At a frozen original camera, six lamps brightened15807of57600rendered pixels. Night/Evening on, Day/Morning off, pause, rematch and original light identities passed. Existing replay mapping still has offset0. Four controlled720p/1080p images individually inspected; loading/resume captures include existing UI fade and are not complete UI acceptance.
* Existing EditMode time-preference and authoritative score-receipt contracts:8PASS. The pre-fix failed XML remains verbatim in its ZIP; no failure was erased or test disabled.
* Exact Windows candidate: 176checks/0errors, actual PID30404exit0 after300.74s. Original eight-AI/three-attempt physical match, all release/landing accounting, replay checks, final rankings and cleanup retained. All six fresh actual720p/1080p images inspected. This is automated gameplay, not human driving or physical-device/listening acceptance.
* Windows x64 development build:60saved scenes, zero build errors; exact summary retained.424scoped owned C# inputs archived with the payload. Full content 5749922f3346fc7374cddf43193ed11f7cb2db26607407adb21e2c244a91ca11; managed c2d03f76b9d8b8f850e29c76071d4a02ed7b43a64e38f79a5af02f3c91098013.

Candidate is available at Builds/TruckSportsCamera/TruckSports.exe. It is NOT promoted over the installed accepted Builds/TruckSports/TruckSports.exe, which remains60d4716f. The earlier72a420fc candidate is fully retained at .sports-validation/Recovery/NightReplayCandidateBeforeSwingVenue-20261024. Separate Dock native UI Automation shutdown acceptance remains BLOCKED under Q22-SKY-NATIVE-QUIT-01 on the user-retained Editor; this Swing pass does not waive those failed exits.925unrelated pending files are unchanged. No public push performed for this increment.

Scope limits: the exercised Swing lamp behavior is verified; the broader Q22sky/lighting/scoring/results pass remains PARTIAL, followed by completeCarnival0.6 thenImpossible0.7. No new human, controller, listening, isolated performance, memory-leak or extended-soak acceptance is claimed.

## VENDOR ASSETS AUDITED

Weather Maker clock/ambient APIs; NWH existing vehicle light states; installed Toon Gas Station Streetlight_1C and Modern Restaurant Street_Lamp; existing scene physics/recordings and URP renderer.

## VENDOR ASSETS USED

Toon Gas Station Streetlight_1C mesh/texture, sport-owned URP material adaptation, existing Weather Maker match clock and original NWH/PhysX vehicle/pendulum. Existing Unity spotlight rendering; no extra shadow maps.

## RELEVANT ASSETS NOT USED

Modern Restaurant Street_Lamp is unnecessary beside an already-used compatible outdoor model. No camera, replay, physics or lighting framework replacement is needed.

## CUSTOM SYSTEMS CREATED

Explicit additive authoring command, focused native fixture and development-only read-only acceptance checks. No new runtime lighting manager.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
