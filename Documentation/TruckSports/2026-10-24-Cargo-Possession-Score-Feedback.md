# Cargo, Dodgeball and possession feedback

Existing authoritative gameplay now publishes its committed events into the shared Heat/TMP/DOTween notification:

- Buzkashi publishes the original whole-cargo delivery once, with its actual configured team name, stable team ID, +1delivery and original team total. Sudden-death winner tier follows the original winner decision. Rope, physical cargo, five-minute clock and AI rules remain authoritative.
- Trampoline Dodgeball publishes +1win only after the original contact-batched round resolution chooses a surviving team. Draws publish no win. First-to-three, replay, trampoline, ball colors and elimination rules remain intact.
- Keepaway arena and Cab Chaos celebrate completed ten-second cumulative possession milestones and the original winning threshold. They show seconds, not invented point awards. Original pickup/theft/damage and possession accumulation remain unchanged.

Actual visual review exposed a pre-existing Dodgeball card defect: name-based text parsing reused the first player's team/status when display names duplicated, and did not show team wins. A mode-owned TryReadScoreCard override now reads the actual slot, team, wins and fighter state. No global parser or rules were modified. Additional actual native-round assertions verify both the typed source and each rendered TMP card. The initial passing4-case feedback suite and its two visibly defective card captures are retained; the final1-case typed-card test and two final images supersede those card views.

Four saved scenes each add46existing Heat documents and one Canvas child. All5454Buzkashi,6759Dodgeball,6903Keepaway and6853KeepawayFirstPerson original documents are retained. Twelve original components with proven added null/default reserialization were restored exactly. Original world, geometry, truck poses, environment, cameras, physics and results remain intact. Normal Play Mode/builds do not regenerate the venues.

Native validation: initial4actual autonomous lifecycle tests passed, additional1actual Dodgeball typed-card test passed, and5existing EditMode receipt contracts passed. Tests use real delivery, physical ball eliminations and cumulative ownership; no points/damage/final outcomes were injected. All10controlled720p/1080p images were individually inspected with pre-fix/final distinctions. These are early lifecycle checks, not full-match Windows or physical-human claims. Both native Keepaway cases use AI-only broadcast.

Original development-only complete-match workloads gained receipt tallies and actual award backbuffer capture. Buzkashi tally accepts its real configurable team names; existing default team-label verification is preserved for other callers. Keepaway tallies completed ten-second milestones against the original float possession totals. The original probes and assertions remain, including actual physical obstacles and resets in both Keepaway variants.

Exact final Windows acceptance:
- BuzCargoAwardsWindows: 24 checks, zero errors; actual process 35536, 92.27 seconds; 4 actual backbuffers and 0 controlled Windows RenderTextures, all individually inspected.
- DodgeRoundAwardsWindows: 68 checks, zero errors; actual process 27120, 511.27 seconds; 12 actual backbuffers and 0 controlled Windows RenderTextures, all individually inspected.
- KeepawayPossessionAwardsWindows: 37 checks, zero errors; actual process 11744, 56.85 seconds; 4 actual backbuffers and 4 controlled Windows RenderTextures, all individually inspected.

Total: 129 checks, zero errors. All20fresh actual Windows backbuffers and4controlled Windows RenderTextures were individually inspected. Buzkashi completes the original six-AI five-minute cargo match. Dodgeball completes original Easy1v1, Normal4v4 and Hard2v2 first-to-three series with original labelled rule fixtures kept separate. Keepaway completes four-AI first-to-thirty matches in both saved variants, with actual pickup, steals, explosions, breakable obstacles, rematch restoration and menu cleanup. Its original venue images use SportsControlledCapture, while new award images use actual backbuffers; results-page visual acceptance is not inferred from those venue captures. Native/software and AI acceptance is not human playtesting. No physical controllers, listening, subjective art acceptance, isolated CPU/GPU benchmarks or extended soak are claimed.

Observed existing pacing limitation: Normal4v4 took21rounds, including16draws, to finish3:2. This presentation pass preserves the original AI,105second explicit draw rule and physical contacts. It does not establish satisfactory human match pacing. Exact original workload telemetry:

```text
MATCH difficulty=0 winner=1 rounds=4 wins=1,3 draws=0 bounces=131 strikes=11 returns=2 eliminations=4 neutralizations=5 friendly=0 peakY=5.433258 contactPeak=3
MATCH difficulty=1 winner=0 rounds=21 wins=3,2 draws=16 bounces=6638 strikes=278 returns=22 eliminations=78 neutralizations=167 friendly=57 peakY=6.636464 contactPeak=3
MATCH difficulty=2 winner=1 rounds=12 wins=2,3 draws=7 bounces=1890 strikes=79 returns=16 eliminations=21 neutralizations=42 friendly=10 peakY=6.031229 contactPeak=3
```

Observed UI limits retained explicitly: an Orange-team strike cooldown can ellipsize in the participant card, although team and win count remain visible. A previous result-copy pass still leaves the singular label "1 TEAM WINS" on the Easy loser card. The compact cooldown caption needs follow-up; the result-copy source is in the frozen142-file candidate and must be handled after that freeze resolves. Neither is claimed fixed by these confirmed-score adapters.

Build: Windows x64,60scenes548files401scoped project-source inputs, zero errors and90warnings. The pipeline retired one4096-byte sharedassets11.assets.resS stream; no remaining serialized asset, level or manager file references it. Build summary and exact source archive are retained separately from existing vendor/Unity inputs. Canonical executable: Builds/TruckSports/TruckSports.exe. Full payload SHA-256: a9a5a8654d804d8364e8c45868135054e17e54bdeca214d569d28315364b7197. Managed SHA-256: f466079bae7807e4644a2d1eb32c5f2d1cea8d739bba68d97ddd64e90c993b0a. All548files byte-verified at promotion. Previous accepted full build: F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\.sports-validation\Recovery\AcceptedBuildBeforeCamera-20261024-065906. The completed Snake first hitch and prior score milestones remain included.

Publication remains pending. Automatic approval review rejected the separate142-file Parking public push after the reviewed publication-helper/scope change; the exact frozen candidate awaits the already-requested human approval. These new files and evidence remain preserved locally; permanent-ID deltas must be merged after that ledger freeze resolves. No raw logs are prepared for publication, and private Dead Air ancestry must not be pushed. Q22, Carnival0.6 and Impossible0.7 remain incomplete.

- VENDOR ASSETS AUDITED: NWH contacts and original cargo/possession/Dodgeball rule owners; Heat NotificationManager/animation APIs, TMP and DOTween.
- VENDOR ASSETS USED: NWH/PhysX original gameplay; Heat notification/animation, TMP, existing portraits and DOTween through the established score adapter.
- RELEVANT ASSETS NOT USED: no replacement vehicle, UI, weather, replay or scoring framework was needed; established owners remain authoritative.
- CUSTOM SYSTEMS CREATED: small committed-award adapters, Dodgeball typed card binding and focused native/development acceptance extensions.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.

Evidence: [native XML, source/scene reviews and inspected images](Evidence/2026-10-24-Cargo-Possession-Score-Feedback/).
