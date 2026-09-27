# Truck Taxi venue events

Run `TruckTaxiMegaWorldAuthoring.AuthorMegaWorld()` in the Unity Editor from the
Truck Taxi Regional menu after the regional world and Thunder Bowl have been
authored. It creates the Union Field Stadium, Neon Yard Concert Venue, and
Pinecrest Mountain Pass as three Scene Streamer chunks; persistent ride stops,
POIs, route nodes, chunk metadata, and the venue catalog remain available while
those scenes are unloaded. It only appends the three new scenes to Build Settings.
Do not rerun it on a partially authored world.

The venue catalog is a separate `TruckTaxiVenueCatalog.cs` MonoScript-backed
ScriptableObject. Authoring also appends UTS access lanes for all three roads,
two venue sidewalk loops to the existing pedestrian population, and two
optional shuttle routes to the existing bus service. The current Town 02
circle bus route is preserved. The runtime registers Stadium and Concert
logical pedestrian regions once inside their persistent world bounds.

The catalog is `Resources/TruckTaxi/TruckTaxiVenueCatalog.asset`. Add a venue with
a unique stable ID, world/map position, region and scene names, influence radius,
and authored parking, crowd, pickup, and dropoff zones. Reference its ID from
one or more event definitions. Thunder Bowl is already registered against its
existing scene and stops; do not rebuild it.

Each event has a stable ID, venue ID, display name, type, date window, start/end
time, arrival/departure windows, attendance and logical traffic/crowd/ride
frequency multipliers, and a fare multiplier. Times and dates come exclusively
from the injected in-game calendar. Recurrence options are Once, Weekly (every
seven days from the first date), Weekends, Weekdays, SpecificWeekday, and
DateRange (daily within the inclusive range). End time earlier than start
means the event continues into the next in-game day. For fare tuning, set
`fareMultiplier` to 1.5 for a 50% event bonus. The runtime selects the highest
applicable event bonus; the session must freeze that quote when the fare is
accepted.

Arrival windows increase destination weighting and venue-bound fares. Departure
windows increase pickup weighting and venue-origin fares. Logical crowd and
traffic multipliers are signals for existing UTS/pedestrian presentation budgets,
not actor counts. Severe weather can delay by one hour, postpone by one day, or
cancel an event. Cancellation emits a one-hour departure pickup pulse. The
runtime queues per-occurrence announcements; the presentation layer should
poll `TryDequeueAlert` at its own cadence. Weather impact applies only to the
currently eligible event occurrence (active or beginning within 24 in-game
hours), keyed by its original start date. Later weekly/daily occurrences stay
on the normal calendar. Repeating the same impact does not restart a
cancellation pulse; `None` clears only the currently eligible occurrence.

The Mountain Pass hazard crossing is
`TruckTaxiVenueLayout.MountainSlopeCrossing` at approximately
`(1180, 8, -1540)`. The affected persistent route edge is
`TruckTaxiVenueLayout.MountainHazardRoadEdgeId`; avalanche/mudslide ownership
belongs to the extreme-event subsystem. The scene includes only a small
authored roadside slope and a collidable EasyRoads pass road.
The new chunk names and region bounds are persistent fallback map tile metadata;
the parent must run the existing `TruckTaxiMapTileBaker.RebuildMapTiles()`
after authoring to produce bitmap tiles. The authoring method does not bake
map textures itself.
