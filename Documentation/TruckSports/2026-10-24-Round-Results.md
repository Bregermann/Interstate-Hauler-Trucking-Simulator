# Truck Sports — round and continuous-score results

Implemented the eight existing-mode results boards and the unranked-label repair under Q22-SKY-RESULT-ROUND-01 and Q22-SKY-RESULT-UNRANKED-01. This narrow delivery does not complete the parent Q22 sky/scoring pass, Carnival 0.6.0 or Impossible Games 0.7.0.

Beer Pong displays original team hits/shots/cups and practice statistics. Bungee preserves banked totals, its declared tiebreak winner and the solo record. Curling displays shared team totals and completed ends. Dance separates cumulative show points from final-song judgments. Derby displays actual heat wins and final-heat outcomes. Fare Fight displays deliveries and steals. Parking preserves the original survivor and explicit eliminations without inventing loser placings. Snake displays actual attached trailers, the original multiplier, distance, survival, jumps and the existing saved record.

Implementation: `Assets/TruckSports/Scripts/SportsRoundLegacyResults.cs`, `SportsFinishResultsSource.cs`, `SportsResultsPresentation.cs`, `Assets/TruckSports/Editor/SportsResultsPresentationAuthoring.cs` and `Assets/TruckSports/Tests/GoddessPlayMode/LegacyRoundResultsLifecycleTests.cs`. The unchanged scene identities are `Assets/{TruckBeerPong,TruckBungeeDemolition,TruckCurling,TruckDanceOff,TruckDemolitionDerby,TruckFareFight,TruckMusicalParking,TruckSnake}/Scenes/<same-name>.unity`. Evidence contains the exact source/tool and scene path lists. No new sport, vendor asset or placeholder artwork is introduced.

All eight scene changes are additive: 426 UI documents per scene and only two new child references in one original ResultsPage document. Original objects, child order, transforms and physics are retained. Twelve complete original Match/Rules source files match the preceding public delivery. Original physical probe simulation/assertions are unchanged except for a result-inspection hook after genuine completion.

Visual review found that the shared renderer called every unranked entry OUT, including solo practice. A neutral dash now represents an absent placing; authoritative ELIMINATED/DNF values remain explicit. The original passing geometry test and its visibly incorrect practice images are retained. The final fixture asserts the label as well as real data, glyph bounds, full stored names, minimum type size and reset behavior.

98 native EditMode tests passed before the isolated presentation-label/assertion correction. Final native PlayMode tests passed 3/0 on the corrected source: twelve new configurations plus previous possession/team and six-athlete result-board regressions. All 24 main final controlled 720p/1080p images were individually inspected. Controlled captures use labeled synthetic final values and are separate from actual physical-match acceptance.

| Original Windows workload | Checks / errors | Seconds |
| --- | --- | --- |
| BeerPong | 30 / 0 | 197.99 |
| Bungee | 23 / 0 | 77.5 |
| Curling | 36 / 0 | 620.61 |
| Dance | 32 / 0 | 75.64 |
| Derby | 62 / 0 | 650.22 |
| FareFight | 26 / 0 | 387.98 |
| Parking | 48 / 0 | 123.1 |
| Snake | 37 / 0 | 58.39 |

Total: 294 checks, zero errors. All 28 fresh actual completed-match 720p/1080p Windows backbuffers were individually inspected. Original configured rules, physical interactions, replay and rematch assertions were exercised. Workload transcripts record the actual configurations; these runs do not establish all hardware combinations or 50-trailer performance.

Canonical Windows delivery: `Builds/TruckSports/TruckSports.exe`, including its required companion files. Full payload SHA-256: `995599b80ef8083bf1c6bfd8251af479350a71b91ec5f8be86827b1723fa8c9d`. Managed SHA-256: `f953db024db7b20c4f0ad95d5231245801fe31916b54be29d6a3eff299403312`. Exact393 compiled source inputs are retained in the evidence; previous accepted build retained by the promotion receipt. The build summary records actual errors, warnings and scene list, including the existing Unity.Collections.Tests Unsafe.dll import diagnostic.

A specifically coordinated frozen peer player overlapped some native rendering/build work. Elapsed times are not isolated CPU/GPU benchmarks. Human driving/playtest, physical controllers/reconnect, listening, extended soak and isolated profiling have not been claimed. The original unlimited Beer Pong practice loop is preserved; its synthetic final UI fixture does not invent a natural practice completion. Other Q22 environment/lighting/score requirements remain separately tracked, including the outdoor Parking weather/time binding.

Remaining copy issue Q22-SKY-RESULT-COPY-01: the new Dance summary currently says 1 SONGS for a one-song show; Derby also shows 1 WINS and 1 DRAWS. Singular/plural copy corrections and two additional rendered regression cases are prepared for the next Q22 presentation build. Only API compilation has passed; they have not been substituted into this frozen payload or claimed runtime-tested. Original seeded rule/pose fixtures inside the physical probes remain labeled separately from the following autonomous full matches.

New human feedback SNK-HITCH-01: the first Snake trailer visibly separates from the tractor during driving. This results milestone preserves the original convoy simulation and does not claim the physical hitch defect is repaired. The requested lead-trailer connection repair is the next active priority; later trailers retain their existing following architecture. The reported failure and explicit hitch-specific acceptance are recorded in the permanent ledger.

- VENDOR ASSETS AUDITED: existing Heat UI authoring, shared TMP theme/portraits and DOTween results presentation; original sport-owned rule, replay, vehicle and environment adapters.
- VENDOR ASSETS USED: existing Heat navigation and panels, TextMeshPro, DOTween, unchanged NWH/PhysX vehicles, original Core-Collapse and other sport integrations.
- RELEVANT ASSETS NOT USED: no new road, traffic, GPS, weather or terrain authoring is needed for read-only results; each existing vendor-backed authority remains in place.
- CUSTOM SYSTEMS CREATED: one game-specific read-only results adapter, saved UI integration, focused regression/physical-result inspection hooks and scoped delivery tooling.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none; presentation animation and navigation reuse existing assets.

Evidence: [original native XML archive, scene/source proofs, build receipt, controlled images, original defect and actual Windows transcripts/images](Evidence/2026-10-24-Round-Results/).
