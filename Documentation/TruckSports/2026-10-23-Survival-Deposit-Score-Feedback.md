# Survival, search and cargo scoring feedback — October 23, 2026

Q22-SKY-SCORE-01 remains **PARTIAL**. Floor Is Lava, Hide and Seek and Hungry Haulers now publish actual awards into the existing Heat/DOTween presentation. A reproduced shared queue-order defect is also corrected. Remaining publishers, results and environmental requirements retain their existing statuses.

## Implemented

| Sport | Authoritative commit and behavior |
|---|---|
| Floor Is Lava | Existing +1 per fifteen-second survival milestone, +1 credited knockoff and +5 sole-survivor awards emit independent receipts. Knockoffs retain their original contact/time threshold; simultaneous lava contacts still resolve before the sole survivor. |
| Hide and Seek | Existing accepted physical tags emit +1, an actual sweep +3, and untagged hiders their existing +2 or last-survivor +3. IDs include the actual round/target/participant. Hiding and seeking timers, role changes, perception and privacy behavior remain intact. |
| Hungry Haulers | Only an accepted settled-bank deposit emits its actual delta and total. Normal/golden values remain 1/5. Scooping, carrying, dropping, bombs and rams do not create points. Existing HaulerRules is unchanged. |

Lava/Hide rules emit typed callbacks after their actual score commits; scene adapters subscribe once and unsubscribe on destruction. Cargo uses its existing accepted Deposit transition. Stable journal IDs prevent repeated presentation from compound contact, settled overlap, replay or reset. Presentation never writes the authoritative score.

Explicit additive authoring installs one existing Heat notification per saved scene, with rounded opaque backing, participant-colored portrait/accent and DOTween emphasis. Original scene documents retained: Lava 5,614; Hide 72,399; Hauler 8,284. Each adds 46 UI documents. Original changes are only the Canvas child list and existing shared null/default serialization, individually inspected. All original world poses and physics documents remain preserved, and 925 unrelated pending files were rehashed unchanged.

## Reproduced queue defect and correction

Winner priority previously inserted a winner before earlier queued awards for that same recipient. Ordinary awards could also merge across that winner into an older queued item. A deliberately synthetic, presentation-only native fixture reproduced the defect: expected three displayed receipt totals 1/4/5, but observed two entries with 4 first. The failed XML and exact initial fixture source are retained.

The scheduler now prioritizes across recipients while preserving each recipient's commit order. Aggregation considers only the latest pending event for that recipient and cannot cross an intervening winner/reason/tier. This preserves legitimate negative changes too; values are never clamped or fabricated. Refined real-Heat fixtures verify 1→4→5 both when all receipts are queued together and when the final ordinary receipt arrives after the first two are shown. These fixtures inject receipts only and explicitly assert that actual game points remain unchanged; they are not physical gameplay acceptance.

## Lava rematch defect found during Windows acceptance

The initial 76659278... candidate completed its actual eight-AI Lava match with 17 correctly attributed receipts, but recorded 44 checks/1 failure: the separate physical knockoff fixture reached lava with no qualifying truck contact. Its failed result/process receipt is retained. The initial failure did not record engine damage, so no prior field values are invented.

Investigation exposed a real reset gap: Lava restored positions/visuals but did not repair NWH powertrain/wheel damage. An isolated native fixture set actual engine/transmission/tyre damage to 1.0/0.9/0.8, started a fresh match and observed the same values; the failed XML is retained. Fresh attempts now call the installed NWH DamageHandler.Repair API (powertrain/wheel fallback when absent) and start the engine during the held countdown, following the existing working Hauler setup. No vendor code, platform geometry, countdown holding, collapse duration or knockoff threshold is changed.

A second Windows candidate 39ef7359... verified the repaired damage but still failed the separate physical knockoff (45 checks/1 failure; zero contacts). This established that damage repair alone did not resolve the intermittent failure. A full Editor diagnostic reproduced correct software input/throttle with a cycling engine and no acceleration. Two further full Editor sequences passed the original contact test; failed and successful transcripts are retained.

The installed NWH limiter coroutine sets public `revLimiterActive`, then yields before clearing it. Vehicle disable stops all its coroutines; its engine-disable path does not clear this flag. A controlled native fixture lets NWH itself enter the limiter, interrupts it through ordinary SetupScreen/StartMatch and reproduces the stale restriction: expected false, observed true. This is a confirmed reset defect, consistent with the recorded intermittent low-power symptom; the original failed run did not record the limiter flag, so its exact value is not invented.

Lava and Haulers' fresh-attempt adapters now clear this transient public flag after disabling the prior attempt and before starting the engine. They use existing vendor APIs/fields, with no vendor source edits. The new native regression verifies the interrupted limiter is cleared. The final Windows ram fixture also lets both actual NWH engines enter their limiter before ordinary rematch, verifies the cleared flags, then uses the unchanged 84/112 spawn geometry and real controller acceleration/contact. No qualifying contact, knockoff or score is fabricated. Damage-reset checks remain present too.


The 99581483... candidate passed Lava 48/0 and Hide 54/0, but Haulers 52/1 failed its separate actual opposing-truck cargo-spill fixture after the main match passed with 12 receipts. Its transcript is retained. The same controlled NWH limiter-entry/interruption regression also failed in Haulers before its fix (expected false, actual true). Haulers now clears that transient flag on a fresh attempt too; its original collision threshold, cargo-spill and deposit rules remain unchanged. The final full probe repeats the original physical fixture and retains positions/state/limiter diagnostics.

## Actual validation

70 native EditMode tests passed in 0.7583 s: existing Lava/Hide/Hauler rules, immutable receipt invariants and exact award accounting through knockoffs, milestones, sweeps, survival, end locks and rematches.

Ten targeted PlayMode tests passed in 46.6575 s; the exact duration is recorded in the retained XML. Five exercise genuine physical scoring/lifecycles: prior Sumo, mole contact, grounded-Hill, Volleyball court contact, and the new actual AI cargo scoop/drive/bank sequence. Cargo collection is observed before banking with zero points/no receipt; actual banking reaches Heat and rematch clears it. Three additional controlled real NWH fixtures verify damage repair and interrupted-limiter reset in both Lava and Haulers. The limiter fixture initializes RPM above cutoff and lets NWH enter its own limiter before normal rematch; it is a controlled state reproduction, not a naturally occurring match observation. The other two are explicitly synthetic queue-order presentation fixtures described above. No cargo score, clock or placement is injected.

All three original complete Windows probes passed on the exact delivered payload:

| Sport | Checks / errors | Process duration |
|---|---|---|
| Lava | 48 / 0 | 52.08 s |
| Hide | 54 / 0 | 97.29 s |
| Hauler | 52 / 0 | 79.32 s |

Each full match reconstructs all authoritative point totals from the actual receipt stream. Original physical/platform/contact, AI, pause, software-input ownership, camera, replay and rematch fixtures remain in the transcripts. Six actual 720p/1080p award frames were inspected for readable opaque, screen-safe, nonblocking panels with correct participants/deltas. Capture holds the genuine scored frame after the Heat entrance and restores simulation speed; it never creates an award or substitutes a camera.

Native physical tests are identified above; separate native physical tests for every Lava/Hide interaction are not claimed. Their full Windows gameplay probes provide the recorded actual runtime coverage. No human playtest, physical-controller/wheel, subjective listening, prolonged soak or isolated CPU/GPU/memory acceptance is claimed. Existing package-cache startup warnings remain unsuppressed.

## Delivery and remaining work

Windows x64: `Builds/TruckSports/TruckSports.exe` with all companion files. The source manifest also records two deferred sport-match files with CRLF/LF normalization only; archived accepted source comparison confirms identical code text. Their pending publishers are excluded from this delivered payload. Full payload SHA-256 `f07151945287538bfd6b6b262739f2568677ea3763f1bfe60267874ff851b493`; managed SHA-256 `9d57970e3c8f16a6e5ae1f86e1add568deb44c3d7c1fca9efc9849a5fdfaa70a`. Exact source inputs and the previous accepted build are preserved locally. Evidence: `Documentation/TruckSports/Evidence/2026-10-23-Survival-Deposit-Scores/`.

The initial failed run did not log its limiter flag; the independently reproduced stale-limiter and damage reset defects are corrected and exercised as described above. Other score publishers, race/finish presentation semantics, wider tier/split-screen/performance acceptance, new global results and remaining sky/lighting/snow/reflections are unfinished. Carnival 0.6.0 follows the complete current pass; Q23-IMPOSSIBLE remains queued after complete 0.6.0. Specialized Poker/Bowling/Kickball presentation is preserved.

## VENDOR ASSETS AUDITED

Installed Heat notification/API, DOTween, NWH DamageHandler.Repair/Powertrain.Repair and existing Hauler reset setup, original NWH/PhysX sport contacts, CNP/perception integration, existing cargo/scoop/bank/replay and score authorities.

## VENDOR ASSETS USED

Heat transitions, DOTween portrait/cargo animation, shared broadcast plates/portrait, original NWH/PhysX contacts and vendor damage-repair APIs, original perception/navigation and replay systems.

## RELEVANT ASSETS NOT USED

No replacement physics, vehicle, AI, navigation, UI-animation or replay framework was needed. LWS retains score semantics and coordination.

## CUSTOM SYSTEMS CREATED

Extended existing LWS award callbacks/adapters, explicit additive UI authoring and receipt queue scheduling; added targeted accounting, actual banking and explicitly synthetic UI-order tests.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
