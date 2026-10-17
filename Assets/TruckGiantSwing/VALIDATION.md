# Current combined Windows delivery — October16,2026

Production update: three achievable target layouts selected equally per round, equivalent wind/weather timeline for each attempt, bounded actual flight wind, readable power/release meter and MPH arrow. Full physical scoring, focused equal-condition/replay checks and final Windows layout acceptance passed. No extra scripted launch impulse was added. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Giant Swing validation

Ten native rule fixtures pass: zone centres, closed circle/rectangle boundaries, near/far precision ordering, misses and tied-leader masks.

Actual PhysX eight-AI match: all24 throws physically pumped, released and landed; three attempts each, final totals75/150/150/50/300/150/75/250, winner4. ERRORS0. Measured release speeds approximately18–19m/s; maximum observed swing angle78° and energy3516.729J (small bounded solver-step overshoot of3500J pumping threshold). Earlier runs exercised actual extra-attempt tied-leader rounds. Correlated adjacent planning seeds caused repeated near-identical tied throws; per-round hashed seeds and difficulty-bounded rhythm/aim variation fixed that AI issue without changing physical scores or inventing a winner.

Focused acceptance ERRORS0: opposing versus well-timed physical pumping42.41914J versus2023.498J; exact inherited seat point velocity at release; no added impulse; held range input gating; finite planar aftertouch with vertical velocity following gravity; first physical ground contact; later physical rebound cannot rescore; actual recorded angle/speed/rewind/skip; render-only ghosts; mounted-truck disconnect and explicit virtual-source replacement/neutral gate; sequential controller reuse; rematch and real collection return through the third page. Camera-only1280x720 and1920x1080 pumping images and flight render inspected. Gantry sign orientation and camera framing corrected. Initial half-speed replay skipped normal speed; corrected and focused rerun passed. Null-safe shared replay cleanup and probe teardown fix prevent destroyed-reference errors when interrupting tests.

Evidence: `Logs/TruckSports/TerrierUpdate/SwingEvidence/result.txt` and `SwingFocusedEvidence/result.txt`. Development flags `-tsSwing`, `-tsSwingFocused`. These are physical simulation plus software-injected controls/virtual devices and camera renders; no human playtest, physical-controller, subjective sound, full-window Windows or sustained frame-rate acceptance claimed. Final combined build and built-player checks pending.

The follow-up completes scoped wind and mechanical-loop feedback using installed Weather Maker/NWH clips and applies recorded participant cab paint to replay ghosts. Saved audio references are validated. Updated focused acceptance passed with ERRORS0, including current participant paint and both saved vendor clip references. Shared reset-pose synchronization does not add velocity or alter normal driving.
