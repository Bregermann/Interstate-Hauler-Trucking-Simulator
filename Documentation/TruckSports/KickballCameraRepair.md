# Kickball sky-only rendering repair

The reported Windows screenshot was reproduced in the canonical Windows player and again from an untouched saved-scene startup in the isolated Editor. The stadium and twelve-truck match continued to simulate, but the rendered field was covered by blue sky. The previous Windows match completed 12 functional checks with 39 pitches, 38 kicks, 184 catches, 149 throws, 110 base crossings and a 10–12 result. Those checks did not establish visual acceptance; its sky-only captures are retained as failing evidence.

## Cause and correction

`KickballViews.Tick` changed the already-rendered broadcast camera from perspective to orthographic. Installed Weather Maker's `WeatherMakerFullScreenCloudsScript.DisallowCamera` explicitly excludes orthographic cameras. Its previously registered perspective sky work survived that transition and overpainted the stadium. In the same paused reproduction, disabling the cloud component removed the sky overlay and exposed the intact field. This was a rendering failure, not missing stadium assets or a match-scoring failure.

The sport-owned adapter now keeps a perspective projection through setup, broadcast, replay and rematch. Its stable elevated view fits the actual circular fence, height and dugouts to the display aspect with safe space for the HUD. Existing vendor cloud, precipitation and weather selection remain enabled. Chase and Taxi Cab ownership and controls are preserved. No vendor source, truck physics, scoring, field dimensions or collision geometry changed.

`KickballProbe` now inspects the actual Windows framebuffer at startup, after a recorded replay and after rematching at 720p, 1080p, 16:10, 4:3 and ultrawide sizes. A clear-weather broadcast must contain visible green turf; the old fullscreen sky fails that condition. It saves the real frame without replacing the rendering path. The native camera regression also checks all 56 legal fence vertices at four display shapes, human/AI viewport ownership, rematches and that vendor clouds remain enabled.

Validation receipts and final player results are recorded below when executed. Hardware controls, human gameplay, other weather presets and the remainder of the collection backlog require their separate acceptance checks.

- VENDOR ASSETS AUDITED: installed Weather Maker cloud/sky and URP render feature, Cinemachine, existing NWH trucks and sport camera adapters.
- VENDOR ASSETS USED: unchanged Weather Maker perspective sky and clouds, unchanged Cinemachine recorded/chase views, existing NWH match simulation.
- RELEVANT ASSETS NOT USED: orthographic volumetric cloud rendering, explicitly excluded by the installed vendor component; a custom sky renderer was unnecessary.
- CUSTOM SYSTEMS CREATED: scoped perspective framing in the existing Kickball adapter and development-only framebuffer regression checks.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


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
