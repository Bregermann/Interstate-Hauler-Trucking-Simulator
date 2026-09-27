# Truck Taxi Regional Polish

Baseline: `d1089e28`. This pass extends the existing regional implementation;
it does not replace NWH, UTS, Compass, Scene Streamer, Heat, or the ride session.
Validation status is recorded separately in `TruckTaxi_Regional_Polish_Validation.md`.

## Dispatch and Ride Behavior

The old offer path computed routed distances for every nearby pickup against
every destination before showing an offer. A cold regional graph magnified this
work. `TruckTaxiSession.Offers.cs` now performs cheap spatial filtering, limits
pickup route checks to six and destination checks to twelve per attempt, and
yields between route queries. It allows one job, a 3ms frame budget, and a 3s
wall timeout. A single existing route-planner query is indivisible; development
timings explicitly record maximum step duration as well as route counts.
The compatibility `OfferRide` drain exists for deterministic fixtures; normal
dispatch and debug controls use `RequestRideOffer`. No offer code loads scenes.

Default intercity chance is 10%, with three local offers after an intercity fare.
An unsuccessful local attempt does not silently become an intercity offer.
Ride Requests retains its existing default and saved ON/OFF preference.

Pickup patience remains an approach countdown, with an owner-scoped suspension
for the waiting passenger's injury/recovery presentation. Once boarded, the
separate 0-1 onboard patience meter samples routed progress every four seconds.
Good progress stays stable or recovers. Sustained wrong-way travel has a 20s
grace; unexplained stops have a 60s grace. Current accepted diversions count as
correct destinations. Authored service bays, nearby signal stops, and detected
traffic/pedestrian obstructions exempt waiting from punishment. These are
bounded semantic/proximity checks, not a claim of comprehensive traffic intent.

Zero patience requests a safe drop at an existing navigable authored dropoff bay.
It does not eject the passenger on the highway. Arrival pays a cancellation fee
(default 300 cents), actual distance/time, completed goals/diversions and earned
Chaos, subject to penalties that cannot erase completed goal/diversion rewards.
Cancellation does not increment completed-fare statistics. History distinguishes
passenger cancellation from pickup expiration and deliberate ejection.

## Optional Diversions

`PendingDiversion` is not an active request. The existing Heat dispatch panel
shows the request, location, and rewards, and offers accept/decline. Declining
does not reduce agreed fare or change the original destination. Accepting moves
the existing request into the active list and grants temporary GPS ownership.
The final destination remains semantic authority and restores afterward.

Authored reward fields support cash, two consumable grants, score, satisfaction,
tip multiplier, stop Chaos and gated adult thirst relief. Sketchy stops grant at
least $20; scenic stops grant two Mystery Mushrooms. The diner request grants an
Energy Drink and Snack. Existing inventory/fare systems receive rewards once.
No new wallet, voice generator, job system or save framework is introduced.

## Authoring and Reuse

- `Truck Taxi/Regional/Apply Ride Polish Content`: migrate the old 20% default,
  author diversion rewards and diner metadata using ordinary Unity assets.
- `Truck Taxi/Regional/Author Thunder Bowl Speedway`: project-owned EasyRoads
  content, persistent routing/POI metadata and a separate Scene Streamer chunk.
- `Truck Taxi/Regional/Add City Shuttle And Regional Buses`: append routes;
  preserve the existing Town 02 circle route.
- `Tools/Truck Taxi/Dialogue/Apply Curated Regional Polish`: additive stable-ID
  authored dialogue; never changes generated audio or voice references.

The companion remains a separate consensual adult encounter, not a paid fare.
Its offer-suppression lease is independent of the user's Ride Requests setting.
Normal private completion and ejection have distinct outcomes: only successful
private completion grants thirst relief. The normal passenger ejection command
and HUD are shared with companions rather than adding another input system.

## Occupants and Companion Stops

Companion pickup is the existing NWH horn (`H`, air horn `B`, or the current
wheel/gamepad binding), not an automatic proximity pickup. The HUD shows the
binding. The companion approaches the cab and claims typed private-stop GPS
ownership. Entering the target does not auto-clear it. A pink world ring and
label use that stop's actual `radius` (9 m for the tested nearby stop), loaded
region availability, approach speed and parking-brake state. Inside the circle,
the label names the current brake binding. `ENTER` begins the existing event.

The normal passenger HUD hold button and `F`/gamepad Select action share
`Passengers.RequestEjection()`. Companions reuse `TruckTaxiPassengerActor.Eject`,
its 22 m/s velocity cap, truck collision ignoring and 12-second body cleanup.
Ejecting cancels the private encounter, clears its route/marker/ring and releases
the offer lease without satisfying thirst or changing Ride Requests preference.
An existing service target prevents a new companion pickup rather than being
silently overwritten; private-stop ownership blocks competing service routing.
There is therefore no prior service route to restore in the current legal flow.

`TruckTaxiSeatProfile.PlaceActor` divides by the actual parent lossy scale and
uses 88% primary / 84% secondary apparent scale. The secondary perch has its own
offset and rotation. A persistent posed Wobble driver belongs to the player
truck, not the passenger pool; its head layer is excluded from NWH cab cameras.
Cab fit still needs human visual review across the authored body variants.

The current waiting passenger has a one-shot tractor-hit sensor. Light tumble,
recovery, character bark and the shared NPC jug launcher keep the same passenger
identity while temporarily suspending pickup patience. Sketchy-stop presentation
uses a voluntary exit, brief Wobble exchange/sway and reboarding. Completion is
gated on presentation readiness; leaving or failing the stop cleans up and
reseats the actor. Neither path replaces UTS/NWH physics.

## Map, Services and World Content

- Full-map zoom follows Compass's positive-is-closer convention. A collapsible
  typed legend uses the existing icon registry. Stop Navigation hides guidance;
  Restore Route retains ride/service identity and does not abandon a passenger.
- Nine editor-baked regional map tiles display only for the Compass full-map
  camera beneath live scenery. Opening the map never loads distant chunks.
  The tile catalog references an authored URP material so player shader stripping
  cannot remove the map's only runtime shader dependency.
  Rebuild with `Truck Taxi/Regional/Rebuild Map Tiles` after editing geography.
- Store, fuel, restroom, restaurant and repair eligibility share authored bay
  semantics and a maximum 1 MPH interaction speed. Purchase errors are explicit.
  Restaurant entry exposes EAT HERE; damage-bar and Services/Tow expose repair.
- Five prototype perimeter renderers per town are hidden; collision is retained.
- Snow coverage periodically refreshes Weatherade's existing presentation
  registration for streamed/new/material-changed road renderers. Incompatible
  materials are diagnosed, not silently claimed to be snow-capable.
- Normal mushroom / boost / invulnerability durations are 360 / 300 / 420 seconds.
  Development short-duration mode is separate; repeated use refreshes bounded
  time without multiplying strength.
- Live traffic/bus audio is spatial and routed through the existing World mixer.
  Extreme rage is restricted to nearby full-physics vehicles, one global event,
  and a 90-second global cooldown. `angryBark` is optional and remains unassigned;
  this pass does not generate audio. The NPC launcher does not award player throws.
- Existing Town 02 circle-route coordinates remain unchanged. Three appended
  city/shuttle/regional routes retain logical distance/passengers while pooled;
  at most two buses materialize. Wobble stop exchange is presentation only.
- Thunder Bowl Speedway is an original fictional, steeply banked EasyRoads oval
  with access road, infield, parking and bowl stands in its own streamed scene.
  Six persistent taxi stops, graph edges, map POI and an ordered-checkpoint lap
  diversion are authored. Racing passengers get configurable weighting, not a
  bypass of intercity eligibility. Physical lap/curb behavior needs driving review.
- 43 identity-specific subtitle lines were added across 25 authored profiles;
  they are not a claim of full category coverage for every passenger. No WAVs or
  voice-reference assignments were generated or changed.
- Pedestrian pooling adds visibility/relevance pinning and an eight-second
  offscreen grace; it retains the 720 logical-pedestrian and 150-car targets.

## Validation Entry Point

Focused new tests use category `TaxiPolish`. The Windows development player can
run `-truck-taxi-polish-smoke` for bounded dispatch timing, map controls/geography,
stationary population performance, and the companion acceptance checks. Its
companion positioning uses an explicit kinematic/teleport fixture; ejection uses
the existing input and live actor physics. It must not be described as a full
manual drive or a complete test of all emergent traffic situations.

## Vendor-First Record

VENDOR ASSETS AUDITED: existing NWH, UTS, Compass Navigator Pro, EasyRoads,
Scene Streamer, Weather Maker/Weatherade, Heat and Pixel Crushers adapters.

VENDOR ASSETS USED: those authorities plus the existing Wobble People assets,
Input System, TextMeshPro and project-owned exploding-container presentation.

RELEVANT ASSETS NOT USED: no new voice generation, spreadsheet system, external
navigation framework, native plugin or persistence backend.

CUSTOM SYSTEMS CREATED: bounded offer scheduling and ride-behavior extensions,
small presentation/authoring helpers, static map geography, and focused tests.
These implement Taxi semantics and presentation without replacing vendor systems.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
