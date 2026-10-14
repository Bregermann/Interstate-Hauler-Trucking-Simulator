# Billiards presentation completed — 2026-10-14

Saved balls have collider-free smooth UV-numbered/striped rolling spheres and soft contact shadows. Displacement/radius drives visual rotation only; existing frozen physical rotation/planar constraints/collider sizes/materials/rules/power/AI are preserved. Pocket scoring retires physical contact immediately, with a.38s downward visual drop and no extra turn delay. Rack/cue placement kills stale drops and avoids teleport rotation jumps.

Full safe-fit table and compact rail HUD retain names/colors, groups/remaining balls, turn/armed/power and rack/series progress. Existing placement/group/pause/reconnect gates are preserved; ordinary held movement continues.53 visual/physical/mouse/oracle checks,54 physical input/AI/held-turn regressions,7 group ownership/mouse checks and five-aspect layouts pass. Fresh local-physics A/B produces identical authoritative outcomes across nine trials; do not use contact-cache reuse as a deterministic oracle. See TruckSports/VALIDATION.md/CHECKPOINT.md for final build 14 and pushed codex/truck-sports-polish feature d637e67042a2acdf74d9b3c62c981f4369237ae7. Human hardware/rumble/audio checks remain. Preserve this physical model in later visual work.

Earlier Billiards checkpoints below are historical.

> Superseded by the 2026-10-12 saved 3v3 collection update. See ../TruckSports/UPDATE_3V3.md and ../TruckSports/VALIDATION.md for current tests, build and Git delivery; the historical checkpoint below is not the current handoff.

# Truck Billiards checkpoint — 2026-10-12

All four requested additions implemented and wired. Updated Windows x64 development build COMPLETE.
Executable: F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckBilliards\TruckBilliards.exe
Startup/only scene: Assets/TruckBilliards/Scenes/TruckBilliards.unity

- [x] Single Rack, Best of 3 and Best of 5 in every competitive control mode; running names/score/rack/target wins.
- [x] Exactly-once rack awards from existing rules authority; next-rack breaker alternation, reset/preservation, final Series Won, rematch opener alternation.
- [x] Confirmed Restart Rack preserves completed score/current breaker; Restart Series clears score/restores original opener; hot-seat readiness and result reconnect preserved.
- [x] Persisted Easy / Normal / Hard (Normal default), serialized decision/error tuning, bounded physical approach recovery and legal fallback.
- [x] Reproducible three layouts x three difficulties x twelve seeds. Observed mean aim error Easy 2.8450 degrees, Normal .8794, Hard .2845; no win-rate claim. All levels physically bumper-struck the opening cue.
- [x] Main-menu Practice, one truck/no AI/group/turn/winner; continuous driving with one strike until settling; 8 remains down, normal scratch placement.
- [x] Practice pause Reset Rack mid-motion and settled Reposition Cue; competitive reposition refused; inactive pad slot/join ownership corrected.
- [x] Eight original sound effects, actual-strength scaling, single ball-pair clack, resting threshold, six voices and Master/SFX routing/headroom.
- [x] Persisted Vibration On/Off, assigned-pad requests and unscaled expiry/lifecycle stop; winner-colored DOTween rack/series confetti/stings, immediately usable buttons.
- [x] Actual Unity compilation/saved-scene wiring, 44 existing rules assertions and 72 feature assertions; build succeeded with zero errors, 25 existing warnings.
- [x] Final feature player: 92 passing checks at 1280x720 and 1920x1080 each; final multiplayer regression: 58 checks. No captured runtime errors.
- [x] Standalone menu/Practice/Result/Settings captures inspected at both sizes.
- [x] Normal executable launched. Native capture failed twice after fresh selection; no native-input PASS claimed.
- [x] README, VALIDATION, CHECKPOINT and BUILD-INFO updated beside executable; all required companions verified.
- [ ] Sustained native keyboard/mouse play and unassisted complete series / long-play AI balance.
- [ ] Real one-pad/two-pad ownership, unplug/replug/replacement, hot-seat handoffs and actual rumble. Zero physical pads observed before injected checks.
- [ ] Listen to full-break/soft-contact mix, distinct cues and mute behavior on real speakers/headphones.
- [ ] Display switching/relaunch and steering feel on actual hardware.

Evidence: Logs/TruckBilliards/features-build-release.log, features-release-720.log, features-release-1080.log, features-release-regression.log; Features-Release-720, Features-Release-1080 and Features-Release-Regression results/screenshots; authored-audio-metrics.csv; features-native-player.log. Read VALIDATION for exact fixture boundaries.
Series outcomes use authoritative rule-fact fixtures; Practice pots/scratches and AI strikes use real physics. Controller/haptic checks use injected devices/software requests, not physical hardware proof. Existing full-match fixtures reposition balls between physical shots, not unassisted matches.

Source changes are confined to Assets/TruckBilliards. Host/Taxi scene configurations, vendor source and global input assets were not changed. EditorBuildSettings SHA256 retained 9554C220A85A14A6769EBB339F6EA3E56A83F6C78B02DFAF23CBE08938E7BFA6.
The OS product/window title inherits Interstate Hauler to preserve host PlayerSettings. Requested artifact is a development build; later distribution branding/production packaging is not part of this pass.

VENDOR ASSETS AUDITED: NWH/GR3D, Heat APIs/audio, UTS, vehicle/environment audio, DOTween APIs, Input System/DirectInput.
VENDOR ASSETS USED: NWH/GR3D truck geometry, Heat UI/Click.wav, DOTween, installed Input System/URP/uGUI/TMP.
RELEVANT ASSETS NOT USED: Full road-vehicle/traffic/wheel stacks; unsuitable vehicle/environment audio. No installed pool-series/Practice semantics provider.
CUSTOM SYSTEMS CREATED: Series bookkeeping, Practice extension, AI tuning/evaluation, pool feedback/haptic adapter, celebration orchestration and focused fixtures.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Truck Sports integration checkpoint — 2026-10-12

Collection hooks added only to BilliardsSession.Start/MainMenu and BilliardsMenus.Setup Back. The saved Billiards scene, standalone builder, series/Practice/difficulty/rules/physics/feedback are retained. Final Truck Sports build-11 executable passed the existing 92-check feature suite and 58-check local regression (Logs/TruckSports/Billiards-Features-11 and Billiards-Regression-11). Repeated collection sport switches test one active EventSystem/AudioListener/authority and retained match/Practice setup.

Current collection artifact and native/hardware boundaries: ../TruckSports/CHECKPOINT.md and VALIDATION.md. Earlier source-scope/window-title statements above describe the prior Billiards-only delivery.
