# Cornhole clean-hole scoring repair

## Observed failure

The original truck observer required a physics-frame sample with the entire projected truck inside the circular opening while its bounds overlapped the board thickness. Entry and exit were checked only at sampled poses. A clean passage could skip that state in one step. The course could then resolve a low or grounded truck as a miss before observing its completed passage.

A real PhysX baseline used the saved board, unchanged 1.65m opening and authoritative truck collider. Centered downward drops at 8, 30, 70, 110 and 160m/s were repeated three times. All three 160m/s stress cases failed to score despite no board contact; the slower cases scored. This proves the discrete sampling defect. It does not claim that the exact speed or orientation in the user's unrecorded throw was reproduced.

## Change

CornholeTruck now checks swept leading-bound entry and full rear-bound exit, interpolating the authoritative body position/rotation at each crossing and checking the whole collider projection against the existing opening. Entry and exit may occur in one step. The same observer handles a previously landed truck pushed through later. It does not require board contact.

CornholeCourse observes completed motion before miss cleanup. Exclude performs a final passage observation before sealing a failure. Once collected, the existing Holed flag and points precedence retain three points once; later updates cannot revoke or duplicate the award. Already excluded failures still cannot acquire late points. The board, hole, colliders, launch response and aftertouch tuning are unchanged.

## Evidence

Editor physical regression: **67 checks passed**, fifteen repeated centered passages across the five speeds, no board contact, one award retained, outside-opening rejection, upward-passage rejection and hanging/overlap rejection. Zero captured runtime errors. These are injected launch/drop fixtures using actual PhysX and saved geometry, not human throws or a claim of natural AI accuracy.

Windows repetition, existing exclusion/stale-body guards, a complete physical AI game and final artifact acceptance passed in the milestone below. The repaired baseline and failed baseline receipts are retained under project-local Logs/TruckSports/Party/Cornhole.

## Files

Assets/TruckCornhole/Scripts/CornholeTruck.cs; CornholeCourse.cs; CornholeHoleProbe.cs. No scene dimensions or vendor assets are modified by this scoring repair.

## Vendor accounting

VENDOR ASSETS AUDITED: existing PhysX/NWH collider authority, Cornhole scoring observer and Skee-Ball's established swept whole-body crossing pattern.

VENDOR ASSETS USED: original NWH/PhysX truck and saved target geometry; existing course, score and effects authority. The crossing policy follows the existing Skee-Ball pattern.

RELEVANT ASSETS NOT USED: trigger callbacks alone do not supply reliable ordered full-body passage; no vendor supplies this sport-specific scoring rule.

CUSTOM SYSTEMS CREATED: no replacement physics framework; narrowly scoped swept scoring observation and an explicit physical regression probe.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Accepted Windows milestone

All 21 strict Windows suites passed for managed assembly `ef8a78590f6119af62340482d3b2c16fcc49be010f1e87e0cfa435fb19ea7b28`. See [version-bound acceptance and limitations](PartyCheeseImpactValidation.md). The 20-mode camera suite passed 2,467 checks plus separate preference-write/read fixtures. Applicable physical, rematch and full-match regressions passed; human/hardware validation remains manual. Earlier pending paragraphs record pre-acceptance history.
