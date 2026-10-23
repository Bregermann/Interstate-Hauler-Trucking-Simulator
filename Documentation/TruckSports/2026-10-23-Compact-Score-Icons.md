# Compact score icons and result copy — October 23, 2026

The eight/twelve-card single-row HUD no longer draws the locked-state icon across its score/time suffix. The lock occupies the upper icon lane; while locked, the card hides its device glyph so those icons do not stack. Unlocked device prompts are unchanged. Other grid positions, inputs, cameras, trucks, rules and scores are unchanged. No scene, prefab, vendor source or image asset changes were required.

The root cause was geometric: the old 18x18 lock at y=-38 overlapped the numeric area beginning at y=-28. Its compact position is now y=-8, above that area. Nonsingle-row coordinates remain exactly their original values. Actual pre-fix Avalanche images and exact source are retained.

Result summaries also use shorter player-facing copy, remove implementation wording and correctly say 1 FALL. Existing obstacle counts, fall totals, five-second penalties, finish times, ranks and identities are preserved.

## Actual validation

Both unchanged old-renderer assertions failed: 8/12 cards each had the lock covering the actual 181.0s value (two native failures, 4.9016 s). The final targeted suite passed 7/7 in 34.5880 s. Two clearly labelled synthetic saved-HUD fixtures use a software gamepad and explicit rule-finish state to check value/lock separation, one visible icon, text fit and controlled 720p/1080p geometry. Another synthetic six-athlete saved-results fixture checks the shorter copy and existing layout. Two tests exercise real mole/court contacts; two exercise synthetic receipt order. These UI fixtures are not physical-match claims. Four controlled HUD and two controlled Ninja-copy frames were inspected.

Both original full Windows probes passed on the exact delivered payload:

| Sport | Checks / errors | Duration |
|---|---|---|
| Avalanche | 59 / 0 | 261.47 s |
| HotPotato | 39 / 0 | 57.14 s |

Avalanche runs the full twelve-truck race with all three physical routes, real snow/burial, exact finish-result accounting, inputs, replay and rematch. Hot Potato runs its complete eight-AI bomb match, followed by the original controlled two-human keyboard/contact fixture and replay/rematch checks. That contact fixture deliberately prepares truck positions before driving real NWH contact; it is not confused with the autonomous full match. All original scoring checks remain. Four fresh actual 720p/1080p point/time award frames were inspected, plus the refreshed Avalanche result copy. Hot Potato retains its existing nonsingle-row grid; it is a preservation regression, not a claim that every eight-player sport was redesigned into one row.

The new Ninja wording was checked in controlled native rendering; no new full Windows Ninja run was needed for that text-only refinement. The previous full physical Ninja acceptance remains separately recorded. No physical-device/human/listening, extended-soak or isolated performance acceptance is claimed.

## Delivery

`Builds/TruckSports/TruckSports.exe`, 549 verified build files and 60 scenes. Full payload SHA-256 `395fa335a22d2c7f87a5f3e4d1d5a0eaf1bee4c0d614d8a875927149df9adb44`; managed SHA-256 `cb69491ea1f9ad4947657c84d628f326ee7e1fee98e6d42c4da0244e8842ae66`. Exact sources and the previous accepted build are retained. Evidence: `Documentation/TruckSports/Evidence/2026-10-23-Compact-Score-Icons/`. Broader sky, scoring/results, Carnival 0.6.0 and queued Impossible 0.7.0 work remain open.

## VENDOR ASSETS AUDITED

Existing Heat/uGUI card/theme and DOTween presentation bindings.

## VENDOR ASSETS USED

Existing Heat pages/buttons, shared truck portrait, typography and DOTween feedback. Original NWH/PhysX driving is retained.

## RELEVANT ASSETS NOT USED

No replacement UI/input/vehicle framework was needed for a layout and copy correction.

## CUSTOM SYSTEMS CREATED

No new runtime subsystem. A targeted controlled layout regression fixture was added.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
