# Truck Taxi Post-Mega-Pass Addendum

Implementation continues the existing Truck Taxi runtime. This is not a new vehicle,
navigation, persistence, traffic, or weather framework. Automated evidence belongs in
`TruckTaxi_Addendum_Validation.md`; implementation below is not a claim of human acceptance.

## Runtime Ownership

- NWH remains player/trailer physics and recovery authority. UTS drives NPCs and race opponents.
- Compass remains the map presentation; LWS graph queries supply routes and safe travel candidates.
- Existing regional streaming prepares destination scenes before fast travel moves the rig.
- Weather Maker/Weatherade presentation and the Hurricane implementation are retained.
- Heat factories and the existing HUD canvas host menus. Pixel Crushers persistence is unchanged.

## Traffic, Horns, Rivals

`TruckTaxiTrafficTuning` defaults ordinary personalities to 15/65/14/5/1 percent.
Following distances include speed, weather, personality and vehicle length. Voluntary
lane changes use 6-12 second base cooldowns and require a reason and clear gap.
`TruckTaxiTrafficHorn` plays complete nonlooping clips with explicit short/double/long/angry
patterns, randomized cooldowns, 3D attenuation and a five-horn ordinary concurrency limit.
Pooling resets audio state. High priority is reserved for nearby contextual incidents.

Rivals retain 12 logical / 3 nearby live actors and use installed UTS Truck_1 cab vehicles,
not full NWH rigs. Existing rider presentation remains independent of player fares.
Town01/Town02 depot metadata and stadium/Neon staging metadata live on the traffic host.
`TruckTaxiRivalDepotAuthoring.Apply()` configures these references idempotently.

## Travel and Collisions

The existing full map offers selected-POI travel and map-point-to-road travel. Candidates
come from current map places/markers. Travel requires an Available session: active fares
are blocked rather than cancelled for a payout. Private visits, races and recovery block
travel with a reason. Tractor/trailer poses are transactional; destination loading,
ground support and clearance precede movement through existing recovery. Cancellation
restores source poses and releases only the travel-owned offer suppression.

Town perimeter walls had disabled renderers but enabled physical colliders. The narrow
`RemovePrototypeTownPerimeterBlockers` migration disables those known prototype blockers.
`TruckTaxiWorldColliderValidator.ReviewWorld()` is report-only for other solids. It does
not delete arbitrary collision or claim every geometry defect can be detected automatically.

## Thunder Bowl

`TruckTaxiRaceRuntime` hosts practice, 1/3/5-lap time trials, truck, elimination,
chaos and scheduled event races. UTS truck opponents use the authored track paths.
Countdown owns a temporary input/physics hold; cleanup restores it. Start-grid clearance
is checked, entries refund on failed/cancelled starts, checkpoint progression owns laps,
and results use the existing session wallet/statistics. Registration/results own modal
focus and Escape. Race offer suppression is a lease, not a global request toggle.
Defaults: five normal opponents, seven event opponents, $5 entry; normal podium prizes
$50/$25/$10, event prizes $100/$50/$25. Time-trial medals target 64/72/82 seconds and
pay $30/$18/$8. These live in `TruckTaxiRaceSettings`, not in the wallet implementation.

## Extreme Events and Kaiju

Existing authored hazard zones are preferred; eligible events can also create bounded,
fixed-world footprints near the loaded player. Tornado defaults to 55% per eligible day,
earthquake 29%, slides 40%, escalating to 3/5/4/4-day dry-streak bounds respectively.
Debug diagnostics expose eligible days, current probability, placement and rejection.
Terrain-aware/fallback slide presentation does not require a mountain-only trigger.

The original procedural kaiju is prototype presentation, not a licensed character.
One near/far-LOD actor has sighting/attack schedules, bounded prop interaction, fire,
and player capture using existing roadside recovery. Imported-model Town02 set pieces
are explicitly destructible; arbitrary production buildings are not destroyed.
Kaiju and weather publish through the shared alerts, map and radio. A threatened venue
occurrence can evacuate without cancelling the same event on a future week.
`TruckTaxiKaijuConfig` defaults to 68 m height, 1-3 game-day sightings, 3-7 game-day
attacks and a three-day maximum sighting drought. One actor, at most four props and
three debris bodies bound event cost. Its roar/stomp/fire clips remain unassigned.

## Radio and HUD

The top HUD has a permanent needs/time/commands band, one critical lane and one secondary
lane, with a bounded priority queue beneath. It replaces overlapping notifications.
Radio provides six fictional stations, authored time bands, shuffle, saved station/power/
volume, camera-dependent attenuation and track continuity across interruptions.
Defaults: F4 power, F9 previous, F10 next, F11 mute, numpad +/- volume; HUD Radio opens
the Heat-based controls. The Controls panel lists these bindings.
The radio caption sits above lower-left telemetry; race timing uses the right-side
free-drive area. Neither new panel occupies the default Compass minimap area. Custom
player GPS placements still need the same presentation acceptance as other HUD settings.

The catalog is `Assets/LWS/TruckTaxi/Resources/TruckTaxi/TruckTaxiRadioCatalog.asset`.
Six original synthesized instrumental prototype beds are supplied. These are not finished
genre shows. Live game-state bulletins currently use an alert tone and readable text;
authored spoken programs can be assigned as licensed/project-owned playlist clips.
No passenger voices are regenerated or external stations streamed.

**Radio mixer routing remains pending at the user's request.** It currently uses Master
plus its own source volume. The existing mixer has not been rewritten. Once Radio and
RadioVolume exist, `Truck Taxi/Radio/Bind Authored Radio Mixer Group` binds them.

## Mushroom and Road Pep

The project-owned URP Mushroom pass distorts only registered gameplay cameras before
transparent/UI rendering. Default amplitude is .005 UV at .4 Hz. Existing color effects
remain; Compass cameras and HUD are excluded.

Road Pep is exceptional stock at River Bridge Services, not every shop. Its abstract
steady/timing challenge keeps driving and world simulation running and consumes actual
vehicle motion/collision telemetry. Quality scales duration at 100/90/70/45 percent;
stacking caps duration and increases side effects rather than multiplying engine power.
The effect/comedown use the existing driver-needs/effect authority. No real-world drug
preparation instructions or voice-generation workflow are introduced.

## Sleeper Preparation

`TruckTaxiCabOccupantRig` adds explicit optional seat/sleeper anchors, allowed occupant
types, reversible placement and private-event transition coordination. Capability is off
by default and the daycab model is unchanged. A future sleeper prefab supplies two
distinct rear anchors and an existing exterior camera. See
`Assets/LWS/TruckTaxi/Documentation/SleeperOccupantIntegration.md`.

## Edenborough

`Tools/GeoImport/Edenborough` holds the OSM normalization pipeline, provenance and tests.
Raw extract/cache stay outside Assets and are ignored. Stable way/lane/turn identities,
metric coordinates, LHT semantics, restrictions and ODbL attribution are retained.
`EdenboroughAuthoring.BuildRegion("R_0_0")` authors one flat district using actual EasyRoads
meshes/colliders and existing LWS graph/traffic/manifest contracts. It is not a final city.
The prototype excludes unsupported grade-separated roads; junction refinement, signals,
truck clearance and production streaming activation remain future world-authoring work.
An actual NWH input-driven PlayMode smoke test passed on this district; it is still
excluded from the Taxi Windows build. See the validation report for measured limits.

## Vendor Report

- VENDOR ASSETS AUDITED: NWH, UTS, Compass Navigator Pro, EasyRoads, Scene Streamer,
  Weather Maker, Weatherade, Heat, existing Pixel Crushers integration, URP.
- VENDOR ASSETS USED: existing vendor authorities/adapters above; imported UTS trucks
  and car meshes, installed building mesh and Heat notification tone.
- RELEVANT ASSETS NOT USED: no suitable original giant creature asset; no new Vista
  terrain or DOTween dependency needed for the bounded prototype effects.
- CUSTOM SYSTEMS CREATED: Taxi policies, race/kaiju semantics, radio catalog/player,
  travel coordination, HUD alert queue, occupant coordination, import adapters and tests.
- VENDOR SOURCE MODIFIED: NO by this addendum. Existing unrelated dirty files are preserved.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
