# Current combined Windows delivery — October16,2026

Derby is included in the final31-sport Windows build. Full multi-difficulty matches, physical damage/rollover/inactivity/survivor lock, actual rear mirror pixels, source/reconnect/reset checks and a sustained60second four-cab rendered workload passed. The Windows-only repeated stationary reset failure was traced to NWH cancelling its limiter coroutine while preserving revLimiterActive; the fresh Derby reset now clears that public transient latch, with injected-state and real per-heat departure regressions. Timings and before/after evidence are in the collection VALIDATION.md; hardware/audio/human handling remain manual. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Demolition Derby validation

Native acceptance, Unity6000.4.10f1, 2026-10-16. Windows production acceptance is recorded separately in the collection validation report; native results below do not claim physical controllers, human playtesting or player-window performance.

| Physical AI series | Participants | Heats | Hits | Explosions | Draws | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| Easy |2|5|70|5|0|3:2|
| Normal |8|10|781|70|0|slot6 wins3heats|
| Hard |8|7|417|49|0|slot3 wins3heats|

`DerbyEvidence/result.txt`: final0errors. Actual NWH controls, PhysX contacts and qualifying damage, measurable mesh deformation, engine/wheel consequences, rollovers, pooled breakup, recorded replay and rematch. The earlier Hard sustained side-contact stalemate was reproduced and fixed with ordinary bounded AI reverse disengagement; sustained contact itself never repeats damage. Impact normal-speed/intensity caps and per-body contact deduplication prevent pathological impulses. No second truck collision impulse is added.

`DerbyFocusedEvidence/result.txt`: final0errors,775pure rule/damage/movement fixtures plus labelled adapter/collapse checks, exclusive whole/shared source ownership, injected removal/replacement, actual cab steering/free look, five controlled aspect layouts and lifecycle cleanup. All eight private160x96mirror textures contain actual rearward world pixels, with distinct targets; captured images were inspected. Mirrors release resources on pause and exit. Controlled adapter damage is distinct from full-game collision provenance above.

The additional real inactivity reproduction starts a stationary keyboard-owned truck against an ordinary driving AI. The actual chassis explodes once after4.999996measured active seconds with reason `STOPPED FOR FIVE SECONDS`; paused wall time does not advance it. The surviving truck remains alive without further explosions through8.2seconds of the locked aftermath/replay. Source warning text puts the countdown before damage percentages so it remains visible in compact cards.

Final build checks must retain source/scene/default consistency, include this saved scene in the explicit Sports-only list, inspect actual Windows rendering/mirrors and measure a sustained four-view workload. Subjective handling, audio/haptics and real-controller acceptance remain human checks.
