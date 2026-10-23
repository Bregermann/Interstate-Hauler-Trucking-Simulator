# Physical-attempt results — October 23, 2026

Q22-SKY-RESULT-ATTEMPT-01 adds saved animated result boards to six older sports. This is another scoped delivery toward the existing global sky/time/scoring/results directive. Parent Q22-SKY, Carnival 0.6.0 and the queued Impossible Games 0.7.0 are not declared complete.

| Sport | Actual result semantics and retained statistics |
|---|---|
| Dock Diving | Existing extra-jump winner takes precedence over equal best distances. Main value is the actual best distance in feet; all three recorded jumps retain feet/inches. Summary includes the actual deciding extra-jump distance when a tie required it. |
| Field Goal | Existing competitive/practice winner authority; actual successful kicks and actual attempts. No invented practice winner. |
| Target Launch | Original final-rest points and resolved launch count, including actual extra rounds. No invented last-ring statistic from a field reset during extra rounds. |
| Skee Ball | Original totals, actual launches, last retained launch award and extra rounds. Existing simultaneous and eight-participant hot-seat configurations retained. |
| Giant Swing | Original series/tiebreak winner or shared top total; actual points, throws and extra rounds. |
| Trailer Plug | Every original shared winner remains highlighted. Actual totals and resolved drops; no invented aggregate pedestrian-hit or coverage statistics that the original match does not retain. |

The original Dock, Field Goal, Target Launch, Skee and Swing solo personal-best lines were initially omitted by the new typed adapter. Review caught this while the candidate was held from promotion: stored records remained intact. Five read-only owner queries now restore those exact records to solo summaries with their original units. The actual Skee solo Windows match also asserts that its completed board shows the stored record. No production preference writes or scoring rules were added. Summary stat labels avoid incorrect singular/plural extra-round wording.

These older modes previously displayed an unsorted text list and winner. A small read-only placing helper now orders the existing declared winner first, then shares placings among equal final totals. It never changes points, chooses a replacement winner, or changes physics. Each Match exposes this display placing; original standings and tiebreak rules remain intact. Immutable result snapshots copy real participant IDs, names, colors and AI/human flags independently of duplicate display names.

The existing Heat navigation, buttons, rematch/menu actions, portraits and DOTween card/value animation are reused. Poker, Bowling and other specialized results remain unchanged. Six scenes receive only an additional results hierarchy and the existing ResultsPage child references. Every pre-existing document is retained, including all original physics and world poses. Empty fields serialized incidentally by Unity were restored from the original scene documents.

The first controlled captures passed layout assertions but visually exposed Field Goal's bright stadium scoreboard through the results headings. A saved rounded, noninteractive readability backing now separates the six result panels from busy venue backgrounds. Initial passing XML and all twelve initial frames remain recoverable. The first static fixture captured the opening menu fade rather than its settled state; final controlled capture now waits for that existing unscaled transition. Final checks also validate the saved backing and its opacity/interaction settings.

## Executed validation

Native EditMode: 30 passed, zero failed, 12.1169 seconds. Includes result placing/immutable snapshot contracts, Trailer Plug rules and saved Dock references.

Final native PlayMode: 7 passed, zero failed, 91.2656 seconds. The six-scene maximum-roster fixture and five solo-record scenarios use explicit synthetic rule values with actual saved Heat/DOTween UI. Solo fixtures seed isolated existing records, assert the original feet/inches, goals or point units, and check those records survive rendering and rematches. These fixtures prove display/preservation, not earned personal-best achievements in physical gameplay. Field Goal uses its existing practice configuration for this synthetic forced-result fixture; a natural practice-match finish is not claimed. Actual competitive Field Goal weather matches are separate. The maximum-roster fixture uses the existing competitive Field Goal and hot-seat Skee configurations; it checks winner/tie authority, actual metric/unit/value binding, duplicate-name-safe identities, bounds/TMP overflow at controlled 720p/1080p, unchanged rule arrays and rematch cleanup. The remaining tests cover the prior contest boards and existing arcade/team contact regressions. Synthetic UI fixtures are not physical-match or human acceptance.

Original Windows probes then complete actual matches before inspecting the new board. Dock uses four software-injected humans and four AI with real launches; Field Goal runs two AI through Heavy Rain, Blizzard and Hurricane; Target runs four AI at all three difficulties; the original Skee five-configuration baseline passed 86/0 on the retained pre-records c5b1c55c candidate (1,365.19 seconds), including simultaneous, eight-player hot-seat and solo cases; the final records-only candidate re-executes the affected nine-launch solo path through the same original probe, while native maximum-roster results are rechecked; Swing uses eight AI; Trailer Plug uses one injected keyboard participant and three AI. Existing physical outcomes, contacts, source/replay/pause/rematch checks remain in those probes. The result extension compares displayed identities, winners, ranks and measurements to actual completed-match data, then captures fresh 720p/1080p Windows backbuffers.

| Sport | Checks / errors | Duration |
|---|---|---|
| Dock | 40 / 0 | 183.57 s |
| Field | 47 / 0 | 87.28 s |
| Target | 51 / 0 | 68.83 s |
| Skee | 19 / 0 | 148.57 s |
| Swing | 66 / 0 | 304.41 s |
| Trailer | 31 / 0 | 172.59 s |

Total: 254 exact-payload Windows checks, zero reported errors. Visual inspection is recorded separately in the evidence manifest. Some final Windows acceptance ran alongside the separate ROOT Dead Air batch build under an explicit frozen-player coordination agreement: no second Editor, only one Windows player, separate logs/preferences, and no Sports source/scene/settings mutation. Those durations describe a shared workload and are not isolated performance measurements. No human playtesting, physical controller/wheel, subjective listening, isolated CPU/GPU profiling, every aspect ratio or long-soak acceptance is claimed. Maximum-roster UI coverage is synthetic where the original full physical workload uses fewer participants.

## Build and preservation

Combined executable: `Builds/TruckSports/TruckSports.exe`, with all dependencies and 60 scenes.

Full payload SHA-256: `14da5c0fb40271f7184f0d09a1650896400009a936cf2dced0e009b0d5ab8fad`.

Managed assembly SHA-256: `d856cbd52f68003fe695c104848797c808846e12df8abe5d949875b6787635a8`.

Exact scoped source archive, payload manifest, prior accepted build recovery, strict scene review and process receipts remain retained. All 925 protected unrelated pending files remain byte-identical. The promotion tool retains its default Editor/player guard. An explicit exact-PID option permits only the independently verified ROOT Dead Air batch BuildWindows entry; wrong project, method, interactive Editor and player identities are rejected. Eight pure Python guard checks passed; the actual promotion receipt records any approved concurrent batch build. Dead Air uses independent write ownership and explicit native Editor/player handoffs; it is excluded from this Sports build and Git scope. A separately tracked release-only diagnostic-query repair (Q22-SKY-RELEASE-COMPILE-01) remains outside this frozen development-build payload and source commit; the next Sports build will incorporate it.

## VENDOR ASSETS AUDITED

Heat results/pages/buttons, shared broadcast theme/portraits, DOTween public API and existing match/replay ownership.

## VENDOR ASSETS USED

Heat navigation, buttons and theme; DOTween presentation interpolation; existing portraits, NWH/PhysX gameplay and original replay systems.

## RELEVANT ASSETS NOT USED

Heat supplies the presentation foundation but cannot determine these sport-specific statistics or tiebreak winners. Existing LWS match authority supplies them; no replacement menu, vehicle or replay technology was introduced.

## CUSTOM SYSTEMS CREATED

Read-only six-sport result adapter, display placing helper, six placing queries, five existing-record queries and one deciding-jump query, additive readability backing and targeted fixtures/probe assertions. No new gameplay scoring authority.

## VENDOR SOURCE MODIFIED

No.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

No.
