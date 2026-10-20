# Kickball stadium — October19 continuation

The actual saved scene had a scoreboard with size4 text on a forty-metre board. The text also shared the housing's front depth. Native images showed a blank board. `KickballAuthoring.InstallStadiumDisplay` now uses appropriately sized, unwrapped lettering in front of the housing, showing the authoritative runs, inning, half and outs. It is called by the existing creation/presentation defaults and by the targeted stadium-display upgrade.

The existing Field Goal audience implementation now accepts an optional common event session. Kickball reuses its actual Taxi posed crowd meshes/materials and response code:2304 baked spectators, no additional live animators, physics bodies or audio listeners. Actual new runs and outs trigger reactions; the audience never awards points. Owned turf and Path Painter earth textures replace the flat field. A sport-owned planar-UV clone of the built-in infield mesh changes only texture coordinates/tangents. The88 existing saved physics component blocks are unchanged.

The final native six-versus-six, three-inning match passed12 checks/0 errors:48 real pitches,47 kicks,244 catches,197 throws,138 ordered base crossings and21–8 final runs. Actual audience reactions, pause, clean rematch and menu cleanup passed. Receipt: `GoddessEvidence/20261019-kickball-stadium-match.txt`. The camera matrix also checks1–4 injected humans, AI camera preference and AI-only viewing at720p/1080p. The menu preview now uses the inspected real stadium capture, retaining its asset GUID.

These are automated Editor/PhysX and injected-input checks. Current Windows, physical-device, human playtest and sustained GPU acceptance remain separate.

- VENDOR ASSETS AUDITED: NWH, existing Field Goal/Taxi crowd, Heat/uGUI, DOTween, owned Field Goal turf and Path Painter II terrain layers.
- VENDOR ASSETS USED: unchanged NWH physics, the existing crowd meshes/materials/backend, existing UI and owned source textures.
- RELEVANT ASSETS NOT USED: another live pedestrian/crowd framework; the existing posed audience already provides the required stadium presentation.
- CUSTOM SYSTEMS CREATED: targeted stadium authoring, a read-only scoreboard/crowd bridge and a visual-only planar-UV mesh variant.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.


## Final exact Windows rendering and match acceptance

PASS saved scene owns twelve separate physical NWH trucks
PASS twelve-AI match starts without a controller or human confirmation
PASS six-a-side and correct starting half
RENDER startup 1280x720 turfFraction=0.517 position=(0.00, 182.71, -38.41)
PASS startup actual player framebuffer contains the stadium, not a fullscreen sky pass
PASS pause preserves actual truck pose, ball state and match clock
PASS recorded replay retains the weather-compatible perspective projection
RENDER first-replay 1280x720 turfFraction=0.206 position=(10.95, 13.41, 26.28)
RENDER broadcast-after-replay 1280x720 turfFraction=0.480 position=(0.00, 182.71, -38.41)
PASS broadcast-after-replay actual player framebuffer contains the stadium, not a fullscreen sky pass
PASS AI physically moves and rolls real pitches
PASS actual bumper contact produces a real kick
PASS physical truck motion produces ordered base-pad crossings
PASS actual front contact secures the visible ball
PASS complete actual three-inning match including necessary extra innings
PASS reused stadium audience responds to actual runs or outs without scoring authority
MATCH difficulty=1 pitches=41 kicks=38 catches=187 throws=150 bases=103 plays=38 innings=3 scores=11,12
PASS rematch clears scores, runners, outs, possession and replay
PASS real match enters a recorded highlight and restores broadcast rendering
RENDER rematch-1280x720 1280x720 turfFraction=0.517 position=(0.00, 182.71, -38.41)
PASS rematch-1280x720 actual player framebuffer contains the stadium, not a fullscreen sky pass
RENDER rematch-1920x1080 1920x1080 turfFraction=0.518 position=(0.00, 182.71, -38.41)
PASS rematch-1920x1080 actual player framebuffer contains the stadium, not a fullscreen sky pass
RENDER rematch-1440x900 1440x900 turfFraction=0.573 position=(0.00, 182.71, -38.41)
PASS rematch-1440x900 actual player framebuffer contains the stadium, not a fullscreen sky pass
RENDER rematch-1280x960 1280x960 turfFraction=0.677 position=(0.00, 182.71, -38.41)
PASS rematch-1280x960 actual player framebuffer contains the stadium, not a fullscreen sky pass
RENDER rematch-2560x1080 2560x1080 turfFraction=0.389 position=(0.00, 182.71, -38.41)
PASS rematch-2560x1080 actual player framebuffer contains the stadium, not a fullscreen sky pass
PASS menu return removes trucks, cameras, replay and extra listeners
PID=28052 CHECKS=21 ERRORS=0. Real NWH/PhysX simulation and software test orchestration, not human or controller hardware validation.

Player exited normally0. Managed SHA256: `414ddce00cd60f2d6cb7782bea626a29236d91a02e9a50d21866bdb4ec1e3eb0`. These are automated NWH/PhysX, actual framebuffer and software orchestration results; no human or physical-controller claim.
