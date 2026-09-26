# Truck Taxi GPS Display

## Player Controls

- **GPS ON / OFF** in the lower-right HUD toggles the on-screen Compass map in
  every driving camera. It does not disable the physical dashboard GPS.
- **GPS SETTINGS**, above DEBUG, opens the existing HUD's GPS display panel.
  The game pauses while adjusting settings; Close/Escape restores the prior
  pause state.
- Heat switches control on-screen visibility, north-up versus heading-up, and
  place markers. Heat sliders control on-screen size, independent on-screen/cab
  map ranges, and route line width. Color swatches select the route color.
- DEFAULTS restores visible HUD, heading-up, markers enabled, 30% HUD size,
  700 m capture ranges, 12 px route width, and cyan routes.
- Preferences survive restart through namespaced local PlayerPrefs. They are
  display settings, not career saves or a second persistence framework.

## Existing Systems Preserved

The two existing `LwsCompassNavigatorProAdapter` instances remain the only map
renderers: `IH Compass Navigator Pro HUD` and `IH Cab GPS Compass Navigator Pro`.
LWS still calculates routes and hands them to Compass's public `SetRoute` API.
No new navigator, map camera, route renderer, render texture, or canvas was added.

The offer preview temporarily borrows the existing HUD map camera. The duplicate
HUD canvas is hidden only during that preview; its camera remains available even
when the user's HUD preference is off. Closing the offer restores the player's
range, follow offset, orientation, markers and visibility. Full-map control still
belongs to the existing Compass/LWS presentation path.

All changes are Truck Taxi scoped. Normal Interstate GPS configuration, routing,
cab anchors, input, truck physics, save system and vendor source are unchanged.

## Fixes

The old taxi adapter forced `Canvas.enabled=false` every LateUpdate, hiding the
on-screen GPS in every camera. That override is removed. Visibility now uses
the existing camera-presentation policy instead of treating cockpit mode as a
reason to permanently disable the player's HUD.

The cab route had geometry, but the 260 m capture range with the vendor's zoom
cropped most of the route; its 6 px line was very thin on the small physical
screen. The default capture range is now 700 m and line width 12 px. Both are
adjustable in game. The whole display sits 4 mm along its existing screen normal
in front of the physical face: the map shader ignores depth, while the vendor
route shader is depth-tested. No shader or vendor source changes were needed.
The screen remains 640x340 at actual lossy scale 0.00023, approximately
0.1472x0.0782 m. User settings never modify the physical placement or scale.

## Implementation

- `TruckTaxiGPSAdapter`: existing Compass configuration, HUD visibility, offer
  framing restoration and application of preferences.
- `TruckTaxiGPSDisplaySettings`: small validated local display preferences.
- `TruckTaxiGPSSettingsPanel`: Heat controls on the existing TruckTaxiHud canvas.
- `TruckTaxiHud`: toggle/settings buttons and modal pause/input coordination.
- `TruckTaxiDemoBuilder`: assigns Heat slider/switch references to new scenes;
  **Truck Taxi/Update GPS Settings References** updates only those references in
  the existing city, without regenerating it. The current scene is already wired.

## Validation

The automated PlayMode presentation test renders the actual scene and compares
cyan pixels inside the cab screen with Compass route rendering enabled/disabled.
The focused run passed: route-on 11 matching pixels, route-off 0. Moving back to
the original face depth with the improved range/width yielded 6 pixels, confirming
that the small depth clearance improves visibility without changing screen size.
Tests also exercise all available camera selections, HUD on/off without disabling
the cab, hidden-HUD offer preview, restored camera framing, settings sliders,
preference reload and pause restoration. Captures at 1080p/1440p were reviewed.

This is automated Unity PlayMode and rendered-image evidence, not a claim of
manual driving, human gamepad usability or every GPU/display combination.
Final regression: EditMode 53 passed, 0 failed, 2 unrelated opt-in voice-generation
setup tests skipped; PlayMode 3/3 passed. Windows development build succeeded with
0 errors (328921329 bytes). The rebuilt executable's opt-in smoke test exited 0:
existing keyboard/NWH input drove 22.9 m, passenger audio/pickup/ride/ejection flow
completed, and the cockpit render shows both Compass GPS displays with routes.
The executable is `Builds/TruckTaxiDemo/TruckTaxi.exe`.
Reports and captures: `Builds/TruckTaxiDemo/Validation/GpsEditMode.xml`,
`GpsPlayMode.xml`, `GpsWindowsBuild.log`, `GpsWindowsSmoke.log`,
`GpsCabRouteOn.png`, `GpsCabRouteOff.png`, `GpsSettings1080.png`,
`GpsSettings1440.png`, and `Windows_Cockpit.png`.

## Vendor Audit

- Audited/used: Compass Navigator Pro minimap/route APIs and shaders; Heat button,
  slider and switch controls; Unity UI/TMP; existing LWS navigation/presentation.
- Not used: Heat's minimap (would duplicate Compass), new GPS/map packages,
  Pixel Crushers career storage for cosmetic display preferences.
- Custom work: taxi display preferences and their HUD presenter only.
- Vendor source modified: NO. Duplicate vendor functionality created: NO.
