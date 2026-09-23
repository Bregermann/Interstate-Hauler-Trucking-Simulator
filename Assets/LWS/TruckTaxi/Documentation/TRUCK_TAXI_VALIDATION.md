# Truck Taxi Validation

## Executed In Unity 6000.4.10f1

- EditMode: 22 passed, 0 failed. Includes all 11 request types, guarded ride
  transitions, one-time payment, personality-specific reactions, target deduplication,
  career-service isolation, generated content completeness and all 380 directed
  stop-to-stop routes over the 45-node / 160-edge demo road graph. The final run
  also verifies authored shortcut/scenic dialogue overrides and ride-state gating.
- PlayMode: final end-to-end test passed, 0 failed (30.92 seconds).
- Existing keyboard E/W input drove the actual NWH tractor approximately 25 metres.
  Batch input testing uses temporary cloned Input System settings to prevent an
  unfocused Editor window from clearing virtual keyboard state. Settings are restored.
- Actual UTS car and pedestrian movement was asserted, not just object existence.
- Three rides completed consecutively with no scene restart. Pickup/dropoff used
  controlled teleports and stationary dwell, NOT manual street driving.
- Each ride generated authored passenger data and requests, switched LWS/Compass
  routes, calculated a fare, and returned to the next-ride flow.
- Traffic, pedestrian and property typed fixtures received real PhysX contacts from
  the NWH tractor. OnCollisionEnter produced the corresponding chaos rewards.
  These are collision fixtures, not claims of manually ramming ambient UTS actors.
- Reset City preserved one canonical truck, no trailer, and no career save service.
- Visible taxi HUD RectTransforms were checked for positive final heights.
- No unexpected exception/error logs were tolerated by the Unity Test Runner.

## Render Checks

Editor offscreen captures exercised 1920x1080 and 2560x1440 layouts. Passenger,
request, offer, fare and action controls were inspected. The final Passenger2.png
also visibly shows the actual Compass road map and route in the lower-left corner,
clear of the reset/pause/debug controls. This is Editor offscreen evidence, not
proof of a normal visible standalone window.

Final Windows build: succeeded with zero build errors, 317,647,555 bytes. Includes
the authored shortcut-dialogue fix. Built and revalidated on September 23, 2026.

Standalone gameplay smoke: passed one additional teleport-assisted ride in the
final Windows executable, with no error/exception logs and process exit code 0. Pickup, request creation,
LWS route success, destination completion and single fare payment were checked.

Standalone visual validation: REQUIRED. Captures from the hidden Windows player
were black, even though the gameplay smoke completed. They are not visual proof
of the map, roads, HUD or a normally visible game window. Do not treat those PNGs
as successful screenshots. Normal visible-window map/HUD inspection remains open.

## Evidence

Builds/TruckTaxiDemo/Validation contains screenshots, test XML and build/player logs.
The opt-in development-player command `-truck-taxi-smoke` captures normal rendered
frames and exercises one additional teleport-assisted ride in the standalone player.
Without that flag, it creates no test object and performs no automation.

## Not Claimed

- No human-operated three-ride road-driving session.
- No successful visible-window standalone screenshot verification.
- No physical gamepad or Logitech wheel test.
- No performance certification across low-end hardware.
- No full-city traffic signals/intersection AI, final art, passenger boarding
  animation, dynamic weather, career saves or freight progression.
- Teleport-assisted rides do not prove driving difficulty or every shortcut's balance.

The full-game truck controller, transmission, physics, GPS framework, trailer system,
freight lifecycle and Pixel Crushers persistence implementation were not replaced.
