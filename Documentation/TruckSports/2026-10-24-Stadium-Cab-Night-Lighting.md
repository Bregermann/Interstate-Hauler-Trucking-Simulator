# Kickball and Keepaway cab night lighting — October24,2026

Both saved venues now have clock-owned night illumination. Kickball uses six purchased Streetlight_1C fixtures in the clear band between its radius78fence and first seating tier. Keepaway First Person reuses the companion main arena's exact four authored fixtures and existing asset references. The main Keepaway scene remains byte-identical. Weather Maker's existing match clock switches lights on at Night/Evening and off at Day/Morning.

The original saved lamp lists were empty. Actual pre-fix Windows Day/Night gameplay showed dim fields with isolated truck-headlight patches. The existing environment probe passed41/0with exit0, establishing its clock behavior while the visual deficiency remained visible. Eight pre-fix gameplay frames were individually reviewed; its four setup captures are not credited as reviewed.

Implementation: explicit SportsEnvironmentAuthoring audit/install commands; no build/play/load-time regeneration. Original17058Kickball and6899Cab scene records retained, plus68/46render/light records. Each scene changes only an existing parent child list and its clock's lamp references; inert default serialization was restored. No new physics, gameplay scripts, shadow maps, camera changes or rule changes. One sport-owned URP material adaptation was created for Kickball; the Cab variant shares existing owned materials.

Actual validation:

* Native PlayMode2PASS,14.7149s: six/four saved fixtures, actual meshes, zero added physics/scripts, Night/Evening on, Day/Morning off, original clock latching, pause and fresh rematch light identities. In the same frozen original camera, lighting brightened52045/57600Kickball pixels and32898/57600Cab pixels. Four controlled720p/1080p images inspected. These are AI-only starts, not complete matches or human cab acceptance.
* Existing EditMode time and authoritative score contracts8PASS. The later explicit development-only single-human-cab probe mode does not change normal setup behavior and was compiled before the final build.
* Exact final Windows payload:41/0Day/Night scene checks plus25/0single-software-keyboard-human Cab checks, both actualexit0. Original match starts, clock/forecast state, frozen time, menu return and actual backbuffers exercised. One human receives the full-screen saved cab with other competitors remaining AI. Eight final AI gameplay and four software-human gameplay720p/1080p images inspected. This does not claim new complete Kickball/Keepaway matches, physical controls, free look, human driving or subjective audio acceptance.
* Windows x64 development build:60saved scenes, zero errors; exact summary retained.425scoped owned C# inputs frozen with the payload. Full content d03c33c6157f6b7c0993c7a2079f02271bff72a61e900648766693baef2564e8; managed bcbe8474574d8d411fc0d1722e134c80b290ced6a395317e5c48aff5aba59020.

Candidate: Builds/TruckSportsCamera/TruckSports.exe. Not promoted: installed Builds/TruckSports/TruckSports.exe remains60d4716f. Prior5749922f candidate is fully retained at .sports-validation/Recovery/SwingVenueCandidateBeforeStadiumCab-20261024. The separate native Dock shutdown blocker remains recorded under Q22-SKY-NATIVE-QUIT-01 on the user-retained Editor.925unrelated pending files remain byte-identical. No public push for this increment. Parent Q22-SKY, completeCarnival0.6 andImpossible0.7 remain unfinished.

## VENDOR ASSETS AUDITED

Weather Maker match clock; installed Toon Gas Station fixture; original purchased stadium geometry, NWH truck beams, and existing Keepaway fixture/material assembly.

## VENDOR ASSETS USED

Toon Gas Station Streetlight_1C mesh/texture, existing sport-owned URP material conversion, exact saved companion Keepaway fixtures, Weather Maker clock and ordinary URP non-shadow spotlights.

## RELEVANT ASSETS NOT USED

No additional lighting/physics/camera package is needed. Existing purchased stadium/arena geometry is preserved.

## CUSTOM SYSTEMS CREATED

Explicit additive authoring and focused native/development acceptance. No new runtime lighting, weather, input, camera or replay manager.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
