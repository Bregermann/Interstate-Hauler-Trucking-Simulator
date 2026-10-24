# Truck Sports scoring presentation continuation

The combined Windows build now includes confirmed score feedback across the ten increments below, the corrected Snake first-trailer hitch, and participant-owned Fishing and Skee-Ball notifications. The existing sports retain their own physics, point arithmetic and match authority. This is a tested scoring/presentation delivery; Q22-SKY, Carnival 0.6 and Impossible 0.7 are not complete.

| Increment | Concrete change | Recorded Windows checks |
|---|---|---:|
| [Pickups and deliveries](2026-10-24-Pickup-Delivery-Score-Feedback.md) | Actual Beer Pong cups, Fare Fight deliveries and Snake pickups; full Heat entrance followed by readable hold time. | 124 / 0 errors |
| [Structural and end scoring](2026-10-24-Structural-End-Score-Feedback.md) | Original Bungee module awards, Curling end points and Derby heat wins. | 199 / 0 errors |
| [Cargo and possession](2026-10-24-Cargo-Possession-Score-Feedback.md) | Buzkashi deliveries, Dodgeball team results and Keepaway control milestones; duplicate-name Dodgeball cards. | 129 / 0 errors |
| [Water contests](2026-10-24-Water-Contest-Score-Feedback.md) | Bog placement points and Log Rolling wins; accurate typed cards and shorter Dodgeball result caption. | 149 / 0 errors |
| [Metric wording and forecast](2026-10-24-Result-Metrics-Forecast-Caption.md) | Known count-unit singular forms and Bog's actual authoritative forecast caption. | 105 / 0 errors |
| [Stunt scoring](2026-10-24-Stunt-Score-Feedback.md) | Limbo attempt points, Sky Tow ring points and banked Trailer Plug results. | 133 / 0 errors |
| [Air attempts](2026-10-24-Air-Attempt-Score-Feedback.md) | Dock Diving distance as a non-score feet measurement; Giant Swing original zone points; typed cards. | 108 / 0 errors |
| [Fishing](2026-10-24-Fishing-Owned-Catch-Feedback.md) | Actual catch weights and four notifications following real viewport ownership. | 131 / 0 errors |
| [Skee-Ball and Field Goal](2026-10-24-Ring-Kick-Score-Feedback.md) | Original ring and legal-kick awards; correct hot-seat owner; popup clear of stadium-board statistics. | 234 / 0 errors |
| [Race outcomes](2026-10-24-Race-End-Score-Feedback.md) | Minefield heat points, original Mattress placement deltas and Terrier legal heat wins; duplicate-name Mattress cards. | 119 / 0 errors |

These are 1,431 recorded Windows checks across the original full-match workloads and their explicitly labeled controlled fixtures. They are not 1,431 human playtests. Each linked report records its exact build, tests, inspected captures, rejected intermediate evidence and limitations. Native EditMode/PlayMode evidence is separate from the Windows counts.

The latest candidate completed an eight-truck Minefield match over fourteen heats, an eight-truck Mattress three-race series, and a twelve-truck Hard AI Terrier match over twenty-seven heats. All 119 checks passed. Its twelve actual Windows backbuffers and two separate controlled camera renders were individually reviewed at 720p and 1080p. The ten native controlled images include four explicitly rejected and superseded Mattress frames; they are retained for traceability.

## Installed build

`Builds/TruckSports/TruckSports.exe`, with all companion files, starts at the existing Sports menu and contains sixty scenes. All 548 installed files were byte-verified after promotion. The immediately preceding complete build is retained in `.sports-validation/Recovery/AcceptedBuildBeforeCamera-20261024-114841`.

- Full payload SHA-256: `3912133d47a0db89131f10f2101125baf82d06b0c8d731c866716194a79e04a7`.
- Managed SHA-256: `9fe109d0ea8c945cde18ee88ea13f62fc6f198284e21f15bd3cd1515dcdf0502`.
- BuildReport: zero errors, ninety warnings; 2,158,078,358 bytes.
- Exact 412 scoped owned C# inputs retained with the payload identity. This is not a claim that these are every Unity/vendor compiler input.

## Requirements and delivery boundaries

The nineteen-entry reconciliation updates only the active scoring work, the completed approved Git delivery and directly observed follow-ups. Original permanent IDs and the prior ledger are retained. The historical backlog is not imported. The authoritative [acceptance ledger](../../TRUCK_SPORTS_ACCEPTANCE_LEDGER.md) separates implementation, automated evidence and human verification.

The human-approved 142-file candidate `6b71510087273b8604d7349d1afd6ffcc01dd3af` was atomically pushed to both public branches and preserved separately in private checkpoint `c9fb0b644ac2ccb4c1a106ad47173bd006039584`. Private ancestry was not pushed. The old empty Git index lock was preserved by an exact-path move after exclusive-open and no-writer checks. This later score continuation receives its own scoped publication receipt; it does not reuse the earlier exact approval as evidence for a different payload.

Eight readable public diagnostic/ledger files received trailing-whitespace-only formatting corrections. Their exact originals remain in the local recovery archive; non-whitespace content and XML test/result attributes are unchanged. Tested sources, scene YAML, images, source-input archives and gameplay values were not reformatted. [Formatting and ledger preservation evidence](Evidence/2026-10-24-Score-Continuation-Publication/) records the original/public hashes.

## Still open

- Minefield compact distance labels visibly truncate their units; the current replay can also be obscured by an existing tree.
- Mattress result statistics still contain the hardcoded phrase “1 RACE WINS”; the previously corrected general count-unit formatter remains intact.
- The race bridge's driven-mile and exactly tied physical landing branches were not independently exercised by the retained ordinary matches. The race requirement therefore remains implemented/unverified for that broader acceptance.
- Remaining Q22 scoring coverage and atmosphere/presentation work, the complete Carnival 0.6 pass and Impossible 0.7 remain unfinished.
- No human driving, physical-controller/wheel/reconnect, subjective listening, isolated CPU/GPU benchmark or extended-soak acceptance is claimed.

## Vendor accounting

- VENDOR ASSETS AUDITED: existing NWH vehicle/trailer systems, Heat notification prefab/public lifecycle, TMP/DOTween presentation, EasyRoads geometry, Weather Maker/Weatherade bindings, existing physical destruction and sport replays; see the per-increment reports.
- VENDOR ASSETS USED: unchanged vendor vehicle/environment/physics foundations and existing Heat/TMP/DOTween presentation through project-owned adapters.
- RELEVANT ASSETS NOT USED: no additional vehicle, UI, scoring, weather or replay framework was needed for these original-score presentation hooks.
- CUSTOM SYSTEMS CREATED: narrow semantic score adapters, read-only participant/team presentation bindings, additive editor integration and targeted development acceptance fixtures.
- VENDOR SOURCE MODIFIED: none in this delivery.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
