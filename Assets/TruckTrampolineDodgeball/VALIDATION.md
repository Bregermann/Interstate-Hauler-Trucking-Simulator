# Trampoline Dodgeball validation

## Native automated evidence

`Logs/TruckSports/TerrierUpdate/DodgeEvidence/result.txt`: final post-regular-floor-state-fix autonomous actual NWH/PhysX matches, ERRORS=0. No injected outcomes.

| Case | Teams | Rounds | Wins | Draws | Bounces | Strikes | Returns | Eliminations | Neutralizations |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Easy | 1v1 | 6 | 3–2 | 1 | 405 | 18 | 3 | 5 | 10 |
| Normal | 4v4 | 10 | 3–1 | 6 | 3034 | 122 | 13 | 39 | 68 |
| Hard | 2v2 | 14 | 3–0 | 11 | 2223 | 81 | 15 | 20 | 46 |

Each completed first-to-three, pause preserved actual ball state/clock, actual ground neutralization occurred, replay played, and rematch reset every ball/fighter/contact/score. Contact peak3 of64; no dropped contacts. Team sizes differ, so these are completion samples rather than a controlled difficulty/balance comparison. Hard AI remains draw-heavy in this sample; human match pacing needs playtesting. Tests accelerated simulation time; they are not rendered performance measurements.

`DodgeFocusedEvidence/result.txt`: ERRORS=0. Software keyboard/pad/opposite-stick ownership, held-confirm isolation, actual trampoline launch, bounded aftertouch/gravity, isolated regular-floor grounded classification, posed side-rest recovery through genuine grounded upright driving, exact pause, removal/replacement, four human chase-camera owners, five controlled UI aspect captures with own-viewport checks, actual physical friendly/enemy/return/ground-neutral/crowd contacts, equal-step two-ball wipe and locked result, historical replay colors, rematch/menu cleanup/one listener. Contact fixtures explicitly pose/freeze bodies; they are distinguished from the autonomous matches.

Controlled Camera.Render plus UI composites:1280×720,1920×1080,1440×900,1280×960,2560×1080. Shared-eight1280×720 inspected: readable cards, all trucks visible behind transparent upper walls, both court signs readable. These are not normal player-window screenshots.

`DodgeFixtures.Run`:34 rule/arbitration checks. `DodgeStrikeEvidence`: targeted real physics/direct strike API reproduction of simultaneous opposing impulses. Before fix two pending impulses cancelled horizontal motion and doubled vertical motion. Now only one strike is accepted per ball/physics step; all participant intent is gathered first, earliest input wins, then actual distance, then an explicit rotating tie rank. Final strike measurement is recorded separately using the configured lift.

## Concrete failures fixed

- MaterialPropertyBlock creation in a MonoBehaviour field initializer threw during replay; moved to Awake and probe error capture starts before scene loading.
- Opposing same-step AddForce requests cancelled horizontal velocity and stacked lift; single accepted impulse and fair intent aggregation.
- AI applied reverse wheel-steering inversion in the air, avoided wall balls too early, aimed using a navigation waypoint angle, and counted every non-trampoline floor as airborne. Corrected these distinct policies; actual regular-floor fixture verifies NWH grounding. Original physical recovery torque passed its targeted fixture and was retained.
- The opaque near wall hid trucks and the far sign read backward. Native authoring/saved scene now use transparent upper containment, low padded rails and readable floor signs.
- Interrupted development-probe cleanup could access already-destroyed menu pages; cleanup now checks page lifetime. This was diagnostic teardown, not a normal gameplay result.

Retained superseded evidence folders are labelled DodgeInitialReplayInitializer, DodgeBeforeStrikeAggregation, DodgeSingleStrikeBeforeTactics, DodgeTacticalBeforeAirSteeringFix, DodgeBeforeSideRecoveryAndRange, DodgeBeforeFlightRangeCalibration, DodgeBeforeStrongerPhysicalRecovery and DodgeBeforeRegularFloorGroundedFix. Interrupted runs are not passing full-match evidence.

## Remaining acceptance

Final combined Windows player, normal-window layout/lifecycle checks, sustained rendered performance, human pacing/handling/audio judgment and physical-controller validation remain required. No human or real-hardware validation is claimed by software-device fixtures. Main-branch/final-build delivery is tracked in the collection checkpoint, not implied by this native milestone.
