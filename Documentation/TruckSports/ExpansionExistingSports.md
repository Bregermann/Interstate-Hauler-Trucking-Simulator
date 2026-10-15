# Existing-sport expansion milestone

2026-10-15. Isolated Sports checkout; main Taxi checkout untouched.

Wallball now supports independent teams of1–4, up to eight participants, team eligibility, primary-interceptor/support AI and cached setup captions. The ball-only oriented contact envelope requires separation, incoming motion and wall clearance. Native truck-ball contacts are excluded so there is one response. Native matches completed1v1(8:11),2v2(2:11),4v4(9:11),1v4(11:6), with respectively19/13/23/20 legal returns and no emergency replays.

Bowling’s bar was authored at lane height and only tweened along Z. It is now a kinematic overhead sweeper: lift survivors, lower front, sweep to pit, return low, raise, replace survivors/fresh rack. Scoring is locked before the physical sweep. Four simultaneous full native games scored71/93/91/124, with19/20/20/20 completed sweeps, every phase exercised and no bar obstruction during live play. Existing scoring/momentum code is preserved.

Ice Hockey adds a capped queued response based on actual cached incoming velocities, replacing the normal solver’s planar result once per fresh pair. Moving targets receive strong displacement; stopped defenders counter the attacker. Measured open-ice11m/s into2m/s produced target20.28m/s and8.57m travel in.55s; stopped defender stayed still while attacker rebounded14.47m/s. Six-AI games completed with goals and no deadlock faceoffs. Articulated puck-only kinematic blades lag/prep/follow through, predict obstacle clearance, tuck/switch safely and rearm only clear of the puck. Native rearm, lag, opposite-board switching and hysteresis checks passed. Broader corners/goal/reverse visual checks remain for final batch validation.

Evidence: Logs/TruckSports/Expansion/existing-runtime-04.log and05.log, Builds/ExpansionStage/ExpansionEvidence/existing-results.txt. These are actual PhysX simulations plus explicitly positioned contact fixtures and injected keyboard readiness, not human or hardware playtesting. Hidden-player images were black and are excluded from visual evidence.

A staged development build succeeded with0errors30warnings. The final expanded menu/build is a later milestone. Clean checkout dependency gaps discovered during baseline remain separately documented in ExpansionCheckpoint.md and ExpansionDependencyAssets.json; dependency publication awaits explicit approval and is excluded from this commit.

Vendor: NWH vehicle models/wheel physics, Heat/TMP UI, DOTween pinsetter motion. No vendor source modified; custom code owns sport semantics/contact coordination.
