# Truck Taxi focused correction

## Ownership

NWH remains the vehicle, transmission, fuel, damage and connected-trailer physics authority. UTS retains pedestrian movement/animation/ragdolls and snowplow driving. Weatherade owns visible snow and trace rendering; Weather Maker owns precipitation/fog. Compass displays the existing LWS route. Heat and the existing Taxi input/focus system host services, preferences and recovery. No new save, navigation, vehicle controller, or vendor fork was introduced.

## Services, needs and recovery

- `TruckTaxiServicePoint` augments existing authored bathroom/store/gas locations with independent flags. `Truck Taxi/Configure Service Capabilities` saves these additions without rebuilding the city. Route 66 gas station is the placeholder repair/recovery bay. Change its flags, radius, recovery anchor and assistance price in the Inspector.
- Normal HUD **SERVICES / TOW**, keyboard **End**, or Pause > Driver Needs > Services provides store, food/drink, gas, restroom and repair routing, Ride Requests ON/OFF, and confirmed towing. Existing focus navigation supports no-mouse use. No gamepad hardware validation is claimed.
- Tow quotes the nearest compatible recovery bay and cost (default $50 plus missing fuel at the station's authored rate). Confirmation pauses/fades, cancels temporary jug/stop actions, moves the same tractor and attached body together, calls `LwsTruckUprightRecoveryController`, repairs through NWH, refuels where supported, charges once, and restores control. No replacement truck. The NWH general repair API repairs deformation and the complete powertrain/wheels; engine/transmission/tire flags also support narrower service points.
- Passenger consequences reuse session personality: very impatient/time-obsessed cancels, patient waits, chaos-friendly enjoys it, others lose satisfaction. No new economy.
- `TruckTaxiRoadsideAssistance.Diagnostics` reports actual fuel, engine running/enabled, damage, transmission/gear, brakes, local snow multipliers and tow eligibility. Zero speed alone is not a failure diagnosis. Normal out-of-fuel behavior now waits for confirmed assistance; the older cartoon rescue is opt-in `automaticOutOfFuelRescue` only.
- New Free Play session inventory grants one of every existing store category once. Consuming an item does not re-grant it. Story behavior is unchanged.
- Throw filled bottle/jug anywhere normal driving is allowed: **Home**, **gamepad View/Select + Right Shoulder**, or the existing HUD button shown when a filled container exists. Uses the existing physical projectile, inherited truck velocity, reaction and Chaos event. That chord suppresses conflicting indicator/ejection actions.
- Restroom, disposal and cleaning are separate capabilities at stores/gas/repair points as well as dedicated bathrooms. No service proximity is required to throw.

## Route and preference ownership

`TruckTaxiSession.RideRequestsEnabled` defaults ON and persists via `TruckTaxi.RideRequestsEnabled.v1`. OFF suppresses future dispatch without cancelling an active ride. Explicit service routes survive offer preview/decline/expiry and ride update notifications; they clear on arrival, intentional cancel/replacement, or explicit ride acceptance. The food/drink search accepts either capability. No second route renderer.

## Drivetrain evidence

Git comparison against the pre-expansion Taxi implementation found no added needs torque/brake/input lock and no engine/mass/brake tuning regression in the Taxi handling override. The old snow region did retain accumulated depth indefinitely after CLEAR, leaving its grip/resistance penalties active. It now melts normally; zero-depth restores the captured NWH baseline, and `ClearSnow()` is an explicit debug/test reset. No arbitrary engine power increase was made.

A fresh clear-road keyboard launch with Hunger=100% and Thirst=100% reached **25.9 MPH in 8 seconds** in Editor Play Mode. Trace: throttle 1, brake 0, parking brake 0, engine damage 0, rolling resistance 55 Nm; approximately 3.8 MPH at 2s, 11 MPH at 4s, 21.5 MPH at 7s. This proves the needs meters do not immobilize this tested truck. It does not establish the cause of the designer's historical fresh-dry slowdown or immobility. NWH engine/transmission damage can disable operation; the new display makes those authoritative reasons inspectable. The historical incident remains unconfirmed, not attributed to hunger.

## Pedestrians

The walking capsule ignores only canonical tractor solids, with a lightweight hit trigger preserving one real impact/reaction/award. Activated UTS ragdoll bones ignore the tractor for their lifetime while retaining world/AI collision. No global collision-layer shutdown.

The bounded startup walk graph excludes full road widths, road surface footprints, buildings and dynamic bodies, samples supported static ground, and validates edges against terrain and obstacles. Only authored legal crosswalks connect across roads. UTS receives dynamic reachable paths instead of tiny repeated loops. Current city generated 667 nodes and ramps to the existing 144-person target. World Builder anchors/road metadata extend this graph without a new locomotion engine.

## Snow

See `../InterstateHauler/Weather/TruckTaxiSnowWeatheradeCorrection.md`. Visible lane-cell meshes and raised snow colliders are gone; hidden depth remains. Explicit SNOW / HEAVY SNOW / BLIZZARD buttons use existing semantic presets. `TruckTaxi_EnvironmentSettings` defaults: maximum .6m, light 90s, heavy 35s, blizzard 25s, clear melt .01m/s, plow residual .04m, tire compression .002m per travelled meter. Inspect the asset to tune rates.

## Validation and limitations

- Focused EditMode: CorrectionTests 8/8, SnowTests 7/7, ServiceNavigationOwnershipTests 1/1, SessionTests 41/41.
- PlayMode: three focused tests passed: canonical tractor/root/bone collision filtering; two actual 4/12 m/s impacts with bounded ragdoll motion, single scoring, cleanup and population replenishment (27.76s); full-road-width/static-ground/building exclusion. The earlier population count assertion was corrected to wait for the configured dense target rather than comparing against the initial ramp count.
- Explicit `TruckTaxiCorrectionProbe`: 29/29 Editor runtime checks, zero unexpected errors. Covers full-needs dry launch, starter inventory, requests/route ownership, physical throw, engine damage diagnosis, tow repair/restart/charge, gas restroom and service lookups, snow depth, absence of slabs/colliders, vendor textures/depth cameras, grounded wheel traces, GPU trace/indentation pixels and active walk graph.
- Initial full-snow renders exposed missing vendor textures, runtime material updates and a nearby bare region. The existing SRS depth renderer was present at index 1, but the generated coverage camera used the normal renderer at index 0. Scene setup now resolves/serializes the actual installed depth-renderer index. Runtime binding configures coverage plus both trace cameras and excludes trace-only geometry from the coverage camera. No vendor source was changed.
- After saving that configuration and restarting Play Mode, the actual gameplay render showed continuous nearby snow and two tire tracks behind the moving truck. Runtime diagnostics: 59 updated snow materials/depth sources, all three cameras bound to SRS, 12,249 active GPU trace pixels and 7,610 indentation pixels. Services panel readability and ON/OFF labeling were also inspected from the actual render. A plow-cleared lane's subjective appearance still needs designer review; mask operation alone is not visual acceptance.
- Attached-trailer towing reuses the existing connected-combination reset, but a real connected-trailer tow was not exercised in this focused Taxi fixture. Designer review remains required for articulated combinations and subjective handling/UI/gamepad feel.
- One final Windows Development build succeeded: 466,663,319 bytes, zero build errors. `Builds/TruckTaxiDemo/TruckTaxi.exe -truck-taxi-correction-smoke` completed 29/29 checks, zero unexpected runtime errors and process exit 0. Standalone dry launch reached 27.7 MPH in eight seconds with both needs at 100%. Its Weatherade readback contained 10,648 trace pixels and 6,174 indentation pixels. The opt-in fixture is never created in ordinary gameplay.
- No Chatterbox generation or voice-reference assignment was performed.
- Lose Vehicle stays DISABLED: no verified connected legal UTS lane/turn route contract or safe live path handoff exists. See `TruckTaxi_Pursuit_Limit.md`. No fake pursuit.

## Source control

Prior Taxi expansion checkpoint: `b59d9473` (`truck-taxi: checkpoint expansion systems and authored content`). This includes intentionally authored Taxi systems/content and attributable LWS integration, not unrelated dirty Interstate/DeadAir/vendor work. Temporary voice-smoke manifest and pipeline-validation audio remain excluded. Build/log/screenshots are local validation artifacts, not source commits. No push is authorized.

Recovery/services checkpoint: `1805ef4d` (`truck-taxi: fix vehicle state and service recovery`). Includes normal recovery UI, authoritative condition reporting, independent service capabilities, starter inventory, throw input, requests preference and service-route ownership with focused tests.

The snow/pedestrian integration commit also includes the saved sandbox service components, weather dependencies/settings, focused runtime fixture and this handoff. Prior handoff items about subjective signal/crosswalk, decoration and audio review remain manual checks, not evidence of missing implementations; this pass did not rebuild those systems.
