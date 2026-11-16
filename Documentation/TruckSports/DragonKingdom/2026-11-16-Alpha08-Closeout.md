# Alpha 0.8 closeout

The November16 priority override freezes scenery, terrain polish and expanded validation. Existing failed results and unfinished requirements remain recorded. Alpha0.9's controller-first multiplayer foundation follows this bounded closeout.

## Actual results

- Dragon Riders: V609 completed both ordinary four-AI races and rematch; retained passing evidence.
- Dragon Escape: V624 completed Fortress/Boat matches, rematch and scene unload; retained passing evidence.
- Egg Delivery: V632 completed both journeys with two physical deliveries each, results and rematch. Active screenshot writes failed at263-character paths. The filename-only repair uses a stable short method hash; gameplay assertions are unchanged. No unrelated world rerun is required.
- Hoard Heist: V638 focused saved-rig regression passes1/1 (14.479279s). V639 normal match fails before rematch because no delivery points were earned. Captured bots repeatedly reject their four escape courses as static/terrain-obstructed. This remains an AI banking limitation, not a verified gameplay completion. Source/DATA preservation passes for both attempts.

## Candidate and limits

The candidate is being built separately at `Builds/TruckSportsCamera/TruckSports.exe`, using the saved current world and explicit gameplay repairs. It carries `0.8.0`, `Wyrms & Wheels`, `Alpha`. The installed `Builds/TruckSports/TruckSports.exe` remains the working0.7 version until the candidate launches and passes basic Windows smoke testing.

Windows build and smoke results are pending. Physical-controller, listening and human-playtest acceptance have not been performed. Current terrain/cliff/lava presentation is accepted for this alpha with polish deferred. The pending V637 seating, V631 palette and expanded combined-source/cavern/epoch validation work are frozen uninvoked.

Private source/preparation backups: `Logs/TruckSports/DragonKingdom/Alpha08CloseoutBuildPrep01`. Source-copy inventory records13selected C# files,12of which change Root; source metadata is preserved. Uncompiled current-repair Policy/Closure drafts are excluded.

## Vendor accounting

- VENDOR ASSETS AUDITED: NWH, Malbers dragons, existing shared sports driver/navigation, URP and existing Windows build pipeline.
- VENDOR ASSETS USED: Existing NWH vehicles, Malbers animation, saved kingdom/environment assets and shared UI/cameras/build pipeline.
- RELEVANT ASSETS NOT USED: New terrain/dressing authoring is deferred by the scope freeze.
- CUSTOM SYSTEMS CREATED: None for closeout; bounded changes extend existing gameplay adapters and screenshot naming.
- VENDOR SOURCE MODIFIED: None.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: None.
