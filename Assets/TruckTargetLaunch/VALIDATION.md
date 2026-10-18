# Target Launch validation — October 18

Current repair: native acceptance and all13 exact-build Windows suites pass. This is an accepted Target milestone, not completion of the full queued release. See [Target Launch repair](../../Documentation/TruckSports/TargetLaunchRepair.md) for the cause, implementation, evidence distinctions and remaining work.

- 14 deterministic rule assertions pass.
- Saved-scene reload audit passes: owned TerrainData shared by renderer/collider,1089 unchanged terrain heights,28 installed Vista outcrops, nine replay groups and48 pooled physical fragments.
- Final saved-scene Play Mode acceptance:71 checks, zero errors. Real supported rest, inward/outward slides, upside-down rest, renewed-motion timer reset, simultaneous finalization, real late fall/lava/debris, visible fragment projection and destroyed-body retirement, persistent-motion zero, limited wing torque, pause, reset and menu cleanup.
- Existing TargetFocused regression:52 checks, zero errors. Real keyboard runway launch; finite aftertouch/dive; four physical10/25/50/100 landings; locked results; actual-motion replay/angles/speeds/rewind/skip; four independent software input slots; duplicate/shared-stick source validation; reconnect neutral gate; long-name camera-only renders across five aspect ratios; repeated menu returns.
- The artificial1500m/s downward fixture visibly hits the target and rebounds at about224m/s. It awards nothing while unsupported and does not freeze. This replaces the obsolete immediate-contact score assertion; it is not a normal gameplay speed or proof of a final resting bullseye.
- First six-speed real-input sample produced five ramp flights and one genuine spinout off the approach, correctly resolved as a zero-point lava miss. The range probe now distinguishes an allowed physical miss from an unresolved delivery; unchanged handling is not retuned to guarantee each trial's success.

Controlled captures include the UI only when explicitly identified as controlled composites. Existing focused/range captures are camera-only native renders. The Target menu image is now a real natural-flight capture from the saved volcanic scene, rather than the old staged quarry preview.

Windows full-match/performance results are recorded in the linked repair report and curated evidence. No human playtest, physical device, audible mix, real haptics or release-ready claim is made. Earlier October16 first-contact-score evidence is historical and superseded by final-rest scoring.
