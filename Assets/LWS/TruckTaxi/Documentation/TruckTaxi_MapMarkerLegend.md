# Truck Taxi Map Marker Legend

## Authority And Setup

`TruckTaxiMapMarkerType` is shared by dashboard Compass, HUD Compass and the existing ride-offer texture projection. `TruckTaxiMapIconRegistry` maps that type to a sprite, tint and scale. This is presentation metadata, not another navigation system.

Run `LWS.TruckTaxi.Editor.TruckTaxiMapIconSetup.EnsureAssets()` once after scripts compile (or **Truck Taxi > Map > Ensure Typed Map Icons**). It creates `Assets/LWS/TruckTaxi/Resources/TruckTaxi/TruckTaxiMapIcons.asset` through Unity APIs. Re-running preserves populated designer mappings. Generated raster glyphs live in `Assets/LWS/TruckTaxi/Art/MapIcons`; Heat sprites are referenced, not edited.

Runtime hookup is inside the existing `TruckTaxiGPSAdapter.ConfigureDemoPresentation()`. It initializes one `TruckTaxiMapMarkers` on the existing Taxi host. No Bootstrap, HUD, Session, scene builder, road, vendor or navigation-authority edits are required.

## Legend

| Type | Silhouette | Meaning / current source |
| --- | --- | --- |
| Player | Heat map arrow | Existing Compass player, same arrow in offer preview |
| PassengerPickup | Person + plus | Current ride's truck-stop/pickup-zone position, not the walking actor |
| Destination | Checkered flag | Current passenger destination; remains known during stop detours |
| ActiveRoute | Heat double arrow | Existing Compass route cue; route line stays the existing route line |
| ActiveObjective | Reticle | Property targets while a property-damage request is active; available for authored objective areas |
| ScenicStop | Mountains + sun | Scenic stop definitions |
| IllicitStop | Handled bag with shifty eyes | Illicit/sketchy pickup definitions |
| PrivateEventStop | Heat heart | Authored private meeting definitions; no new private-event gameplay |
| Shortcut | Bent arrow | Unknown shortcut entry, shown as optional by default |
| DiscoveredShortcut | Bent arrow + check | Entered a valid existing shortcut; distinct across future rides/restarts |
| TargetVehicle | Car + reticle | Eligible current traffic-ram targets; explicit target ID honored |
| Collectible | Heat box | Authored collection stop or explicitly registered collectible; no collectible gameplay invented |
| Dropoff | Down arrow to bar | Available explicit dropoff source; passenger final destination uses the checkered flag |
| SpecialEvent | Heat extras burst | Authored special-event stop |
| Danger | Warning triangle | Explicitly authored hazard; not generated for every collision |
| Debug | Heat information | Existing LWS road POIs, hidden by default |
| FoodStop | Cutlery | Authored food stop |
| PhotoStop | Camera | Authored photo stop |
| Bathroom | Toilet silhouette | Active service destination requested by the driver-needs coordinator |

No specific pursuit vehicle or collectible gameplay currently exists. The mapping and `TruckTaxiMapMarker` authoring component support those sources when their owning systems exist. Ambient pedestrians are not all turned into targets or POIs.

## State And Readability

- Active: 1.3x scale, white radius outline through Compass, `ACTIVE:` title prefix. Shape is retained.
- Known/optional: static, 75% brightness; nearest ten points within 350 metres by default. Active points and the final destination are not discarded by this cap.
- Completed stop/target: 80% scale, 45% brightness, desaturated, `DONE:` title. Optional stops reset completed presentation for a new ride.
- Unknown shortcut: plain bent arrow, optionally hidden by registry setting. Discovered shortcut: checked bent arrow. Discovery persists as namespaced `TruckTaxi.Map.Shortcut.v1.<stable ID>` presentation prefs, not career save data.
- Debug road POIs: explicitly typed, hidden unless `showDebugPoints` is enabled.
- Offer preview: 34px main icons, 28px context icons, same registry; existing label collision placement and existing route polylines retained. Context symbols do not add extra labels.

Inspect `Truck Taxi typed Compass POIs > Map: <stable ID>` for marker type/state/source, assigned sprite and vendor component. For additional runtime-authored sources, use `GPS.MapMarkers.Register(marker)`; duplicate IDs are rejected and repeat registration is idempotent. No arbitrary name guessing.

### Relative-Rect Sizing

`TruckTaxiGPSAdapter` sizes the existing Compass route and icons against each resolved `MiniMapMask` rect. The installed vendor's `Pixels` route-width mode builds geometry in local UI units; one raw width does not produce comparable results on a small HUD rect and the 640x340 cab rect. Source inspection and the pre-fix screenshots showed an overly broad HUD ribbon but a hairline cab route. This is separate from weather lighting: the map and route shaders are unlit.

- `MapElementScale(size) = min(width, height) / 240`, applied only to positive resolved rects larger than one unit.
- Route width = the existing player-selected `DisplaySettings.routeWidth` times that scale. Default 12 therefore spans 5% of the map's shorter side. For a 640x340 cab rect it becomes 17 local units; for a 60x60 HUD rect it becomes 3. Neither the screen's physical transform nor route geometry source changes.
- Base POI scale = `0.8 * scale` for the vendor's 24-unit icon rect; player-arrow scale = `1.6 * scale` for its 12-unit rect. Both occupy 8% of the shorter side before per-type/active/completed multipliers. Registry silhouette, tint and state distinctions remain intact. Offer-preview icons retain their independent 34px/28px UI sizes.
- Vendor `routeEdgeFeather` is 0.5 instead of the inherited 1.5, retaining antialiasing without excessively softening a narrow physical-screen route.
- Vendor `miniMapShowViewCone` is disabled on the Taxi cab/HUD instances. The broad white wedge visible in the original captures was this view cone, not a marker or a new weather effect.
- Cached map rect sizes are compared in the existing adapter's late update. Vendor sizing properties are reapplied only when dimensions change or display settings are applied; there is no per-frame hierarchy search or layout rebuild.

This is a Taxi presentation configuration fix through existing public Compass APIs. No vendor source, routing authority, camera, cab mount or navigation framework is replaced. Final cockpit readability after this change still requires runtime confirmation.

## Integration Details

Compass Navigator Pro 4 at `Assets/Plugins/Kronnect/CompassNavigatorPro` supplies the existing cameras, roads, route, player following and per-compass POI rendering. The installed `CompassProPOI` public fields `iconNonVisited`, `iconVisited`, `miniMapVisibility`, `miniMapIconScale`, `miniMapShowCircle`, and `StableId` are accessed through a cached public API adapter because Compass is in Assembly-CSharp. No vendor reflection internals or source edits.

The existing LWS active destination POI is restyled and reused while present. A same-ID logical marker is not rendered a second time. At zero-length arrivals where the LWS route legitimately clears, the typed POI remains available without requiring another route.

Driver-needs integration: `GPS.SetServiceDestination(stableId, displayName, position)` submits the ordinary LWS truck-legal route and sets a bathroom marker. `GPS.IsServiceDestination` and `GPS.RouteReady` include it. The current ride pickup/destination become secondary during the service detour. Calling the existing pickup/destination/stop routing methods or `ClearDestination` clears the service presentation. This does not alter the session's passenger destination or award any driver-need progress.

Existing shortcut colliders get an additive `TruckTaxiShortcutMapDiscovery` observer. It only records presentation discovery after canonical-truck, speed and direction checks; the original `ShortcutTrigger` remains the sole score/event owner. No collider, reward or trigger position changes.

Sources are inventoried once at initialization. Moving markers follow cached transforms. Bounded 0.4s presentation refresh performs no full-scene search; dynamic traffic is inspected under its existing owner at most every 3s only during a relevant request. No cameras or minimaps are instantiated by this feature.

## Validation Handoff

Added `TruckTaxiMapMarkerTests`: stop mappings; all-type asset uniqueness; nonempty/distinct low-resolution glyph silhouettes; state scaling; idempotent identity; persistent discovery; moving-source tracking.

The sizing regression adds four `CabAndHudUseMatchingRouteAndIconProportions` cases covering cab and several HUD rect sizes. These assert the 5% default route and 8% base icon proportions, but do not prove rendered visibility.

The previous `TruckTaxiPresentationPlayModeTests.CockpitPresentation` run failed its actual cab-image assertion: route-on contained 4 qualifying cyan pixels versus 0 with route-off, below the required difference of more than 5. That assertion is unchanged. Captures now allow two frames after each route visibility toggle for vendor late update and Canvas rebuilding. A `[TearDown]` restores `Time.timeScale = 1` even if an earlier assertion fails, so a failed test cannot leave the following suite paused.

**Relative-rect/edge-feather/view-cone targeted validation:** main reported `CockpitPresentation` PASS 1/1 in 13.33s, saved in `SavedSystemsGPSPlayMode.json`, and inspected fresh captures confirming a visible cab route and proportionate HUD route. This does not imply all Windows marker scenarios passed. Confirm changes at multiple GPS sizes and in both driving camera modes. Do not substitute source-level calculations for visual validation or lower the pixel assertion to claim success.

### Windows Map Probe Fixture Correction

The first Windows systems run passed actual pickup, destination, scenic/illicit active and completed stops, shortcut discovery, traffic target, and offer-context sprite checks. It failed 19 cab/HUD assertions in the forced all-type showcase only. The fixed-metre fixture placed rows at 35, 70, 105 and 140 metres ahead with range 350. Vendor zoom 0.5 and the cab's wide aspect ratio put later rows outside the actual map: only row one appeared on cab; rows one and two appeared on HUD.

Installed Compass creates the per-group Image before viewport clipping, then assigns its sprite only when visible. Therefore an offscreen new POI can have a 24x24 Image with no assigned sprite despite its `iconNonVisited` field being correct. The probe was reading the correct Image, but its forced placement precondition was wrong. Runtime marker assets and production clipping behavior were not changed to make this pass.

The development-only `TruckTaxiMapRuntimeProbe` now places the showcase with public `GetMiniMapWorldPositionFromUV` on the narrower cab map, after camera settings settle. It compensates the public icon-position shift and temporarily uses equal map ranges/north-up, restored with the original display settings in `finally`. Every original actual-Image sprite, visibility and dimension assertion remains. An additional case moves an optional fixture offscreen, asserts vendor clipping with its configured sprite retained, then returns it and requires successful cab/HUD rendering. Failure messages distinguish configured sprite, rendered sprite and visibility.

**Corrected Windows fixture validation: PASS.** The rebuilt `-truck-taxi-systems-smoke` run passed 226 total system checks with zero failures, including all map assertions and clipping/re-entry. `Windows_Map_Cab_ForcedIconFamilies.png`, HUD and offer counterparts were captured from the actual player. See `TruckTaxi_Systems_Validation.md` and `Builds/TruckTaxiDemo/Validation/WindowsSystemsFinal.log`.

`EnsureAssets`, compilation, EditMode and PlayMode validation have been executed. The final Taxi EditMode suite passed 188 tests with two external voice tests intentionally skipped; all seven PlayMode integration tests passed.

**Dashboard/HUD/offer rendering: verified in the Windows player.** Captures and actual Image checks cover pickup, destination flag, both shortcut states, scenic and illicit stops, traffic targets and forced remaining categories. Detours preserve the final destination and only one active endpoint. HUD/offer silhouettes are distinct. The all-type showcase deliberately crowds the small physical cab screen; it is not the normal marker budget and does not prove every simultaneous icon is legible at cockpit distance.

The current physical screen is small; the 24px glyph sampling test is only a static readability guard, not proof of cockpit readability. The selected marker cap/range, tints and scales remain designer-tunable in the registry.

## Vendor Report

- VENDOR ASSETS AUDITED: installed Compass Pro POI/minimap/route public APIs and sprite collection; Heat HUD/navigation/misc icon collection.
- VENDOR ASSETS USED: Compass POI renderer and route-cue APIs; Heat arrow/double-arrow, heart, box, extras, information sprites.
- RELEVANT ASSETS NOT USED: Compass fantasy building icons do not describe passenger pickups or sketchy stops. No new UI or navigation framework was needed.
- CUSTOM SYSTEMS CREATED: Taxi semantic marker registry, source/state binder, presentation-only shortcut discovery observer, editor-only bold missing-category glyph authoring, tests.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
