# Current combined Windows delivery — October16,2026

Skee-Ball is included in the final31-sport Windows build. All five full physical cases passed, including three difficulties, eight-player hot seat and solo, plus focused swept high-speed entry/input/replay checks. A previous native hot-seat run exceeded its old test deadline; the retained failure is not a pass, and the complete longer final Windows case is the acceptance evidence. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Skee-Ball native acceptance

2026-10-16, Unity6000.4.10f1. Final Windows production pass remains pending.

- 47 authoritative rule fixtures: allowed points, one delivery result, nine-launch limit, independent lane completion, equal leader extras, solo and eight-player hot seat, rematch/reset.
- Three actual four-AI matches: Easy totals10/20/30/0,36launches,5entries; Normal10/30/40/30,42launches (equal extras),7entries; Hard20/30/0/20,36launches,3entries. All completed with real upward physical flights and recorded replays. These small samples do not establish balance or guarantee a higher Hard score.
- Eight-player hot-seat full match:60/30/0/80/50/30/50/40,78launches,13entries,65misses; tied leaders each received three extras. Solo:30,9launches,1entry. Both completed; pause retained exact live momentum/IDs; rematch reset passed. Evidence: SkeeRemainingEvidence.
- The first 600-second hot-seat probe deadline expired after47 deliveries while the match was progressing. Retained SkeeShortHotseatDeadline; extended only that fixture deadline to1800real seconds, with no gameplay timeout relaxation.
- Physical calibration used nine ordinary injected keyboard throttle/brake approaches. Measured ramp exit angles59–67degrees contradicted the original ideal56degree planner. AI now estimates ordinary entry controls from measured height/speed samples, with difficulty error and bounded aftertouch. A second concrete AI bug capped approach lookahead at the lip, leaving the target behind the truck and causing late steering lock; lookahead now continues forward. No hidden forces, homing, scoring or collision changes.
- Focused injected four-source ownership, actual Taxi cab/steering wheel, right-stick/mouse look ownership, shared-stick fixed views, natural held-acceleration ramp flight, fresh range gate, limited aftertouch, exact paused flight, software source replacement, actual PhysX high-speed centered/angled hole passage and solid outside impact, repeated-award rejection, and separately identified rear-clearance pose fixtures passed. Shared-keyboard Poker privacy regression passed.
- Controlled Camera.Render world+UI captures at1280x720/1920x1080/1440x900/1280x960/2560x1080 passed per-view HUD bounds. Actual1280x720 composite inspected: real interior, stable horizon, independent views, readable compact text. This is not normal-window rendering or a sustained GPU benchmark.
- Initial physical/AI samples and corrected fixtures remain in Logs; do not describe them as all passing. Final SkeeEvidence cases0–2 and SkeeRemainingEvidence cases3–4 supply the complete accepted match coverage.

Remaining: final combined Windows build smoke/regression and rendered performance; human feel, ring readability in motion, high-value shots, audio and real hardware controllers. No human playtesting or physical-controller claims.
