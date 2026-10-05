# Truck Taxi Analytics Workstream

Section 31 of the consolidated late-September prompt. Analytics observes gameplay;
it never saves, restores, rewards, advances, or otherwise owns career state.

## Parent Integration

No bootstrap, HUD, session, needs, vendor, scene, prefab, assembly definition or
serialized asset was changed by this workstream. The existing mega debug panel
has one additional ANALYTICS page and tab spacing to accommodate it.

After creating Session, Player and the other services, and before StartShift:

```csharp
var analytics = GetComponent<TruckTaxiAnalytics>()
    ?? gameObject.AddComponent<TruckTaxiAnalytics>();
analytics.Initialize(this);
```

Initialization is idempotent and development-only (Editor or development build).
Initialize once per bootstrap lifetime. Disable, destroy and quit end the session
and unsubscribe. A disabled/ended instance does not restart itself. When returning
to a menu without destroying the bootstrap, explicitly call EndSession; a later
new telemetry session requires a new component/bootstrap. Ordinary shifts share
the component's random telemetry session ID. Late attachment does not replay old
ride history or pretend already-active offers were observed.

## Collection And Gaps

Collected from existing authorities:

| Metric | Source and meaning |
| --- | --- |
| Offer generated / accepted | Session Changed, offer identity and accepted ride ID; reentrant Changed calls deduplicated |
| Offer closed | OfferClosedUnknown: DeclineRide handles player decline, expiry and recovery, then resets StateAge before Changed. These cannot honestly be separated by this observer |
| Pickup time | Session PickupElapsed at boarding completion; simulation seconds |
| Ride outcomes | Newly appended RideHistory records; Completed, Failed, PickupCancelled, Ejected, PassengerCancelled; no FailureReason text |
| Fare / tip / stars / patience | Final ride result, existing OnboardPatience, event and weather bonuses, demand multiplier, intercity flag; money in cents, patience 0..1 |
| Objectives / Scenic / Sketchy | Existing RequestCreated, RequestResolved, DiversionOffered with request enum, outcome and elapsed seconds; no descriptions, dialogue or target names |
| Driving / crashes | DrivingEvent enum; debug crashes counts Collision and TrafficRam, not application crashes |
| Jug / needs | DriverNeedsEvent enum from the existing coordinator |
| Radio | Changed records only on/off and mute bits; no station title, track name, bulletin, file or audio metadata |
| Regions | Authored region ID whitelist via RegionalWorld, sampled every 10..30 seconds; brief visits can be missed |
| Weather | CalendarWeather condition enum transitions at sample time; not warning text |
| Errors | Exception notification and SceneOperationFailed become fixed error enum signatures; 30-second per-category throttle, no log text or stack hashes |
| Fairness | Public RecordFairness seam only; no automatic sampler wiring yet |
| Generic custom content / item | CustomContent.PhaseChanged plus session CustomItemInteraction and PassengerCustomInteractionStarted/Completed; phase enums and opaque session-local content/item/event aliases only |

Roster coverage is unique offered IDs divided by the configured distinct authored
roster, capped at 512 IDs. It is NOT dynamic eligible-roster coverage. Never-seen
roster entries appear in Passengers.csv with zero offers. Archetypes map only a
small known category vocabulary; unrecognized free-form archetypes become Other,
never copied verbatim. Per-passenger export is sorted most-offered first and can
be grouped by the category enum. Counts summarize retained telemetry, not lifetime
career history. Last seen includes session ID plus relative real and game time.

Generic CustomContent/CustomItem signals are automatically subscribed when the
parent initializes analytics after CustomContent. Their authored identifiers are
validated then replaced by random per-session opaque IDs, with a 256-alias cap.
The mapping is memory-only, never exported. Custom content names, prompts,
descriptions, reaction text, presentation/audio assets and item labels are not
collected. WorldEvents.csv includes the neutral aliases. This intentionally
supports within-session correlation, not cross-session content identification.
Do not call RecordCustomContent again for an already-subscribed runner event.

Real/play/pause/driving/stopped durations use unscaled frame time while the taxi
session is active. Main-menu inactive time is excluded. Paused frames contribute
only to pause time. Driving threshold is 0.5 m/s. Speed comes from the existing
NWH-backed LwsPlayerTruck.LastTelemetry, never hardware or device information.
Distance integrates speed in simulation time (per-frame dt capped at one second),
excluding recovery/fast-travel movement and transform teleport displacement.
Average speed is time-weighted over unpaused active time, including stops. It is
an estimate, not an odometer replacement. Ride distance remains the session's
authoritative measured ride distance. Long background suspension with no Update
callbacks is not fully represented by frame-based pause time. GameMinutes is the
existing session's clock, fed by the project's game clock.

Performance defaults to a 20-second unscaled cadence, externally clamped to
10..30 seconds: interval FPS / frame time, mean valid main-thread / physics
recorder samples, live cars / pedestrians / rivals, loaded regions. Missing
recorder/counter values are -1 (unknown), not zero. Two consecutive unpaused
intervals below 30 FPS emit one LowPerformance event with sampled population and
region context; recovery resets the incident latch. Paused samples do not trigger
incidents. Main/physics recorder availability needs Editor/player verification.

Explicit remaining wiring gaps:

- Separate decline versus expiry requires the owner to call RecordOfferDisposition
  with the known disposition. It supplements, not replaces, OfferClosedUnknown;
  do not sum them as separate offers. This seam is not currently wired.
- Candidate count, selected weight breakdown, unseen state and modifiers require
  selection-authority calls to RecordFairness. No values are fabricated.
- Item use (Road Pep, Mushroom, food/drink), sleep/fatigue, service purchases,
  map/navigation use, rival actions, traffic recovery/explosions, disasters, Kaiju
  and race outcomes require owner calls to the typed public seams below. Existing
  jug events, objective events and weather condition changes are already observed.
- Route failure and invalid spawn need owner calls to RecordError. Unknown
  passenger IDs at offer time are detected locally. Exception signatures are
  categories only, not deduplicated by exception type/stack or proof of a fatal
  application crash. No raw crash log ingestion.
- No session-end guarantee on force-kill, no lifetime career aggregates, no
  retroactive upload of local-only history and no production-build analytics UI.

Public owner seams (call on Unity main thread, only after the action succeeds):

```csharp
analytics.RecordItemUsed(TruckTaxiNeedsItem.RoadPep);
analytics.RecordServiceUsed(TruckTaxiAnalyticsService.Sleep);
analytics.RecordNavigation(TruckTaxiAnalyticsNavigation.MapOpened);
analytics.RecordWorldEvent(TruckTaxiAnalyticsWorldEvent.TrafficRecovery);
analytics.RecordError(TruckTaxiAnalyticsError.RouteFailure);
analytics.RecordFairness(selectedStableId, candidateCount, baseWeight, finalWeight,
    unseen, recentPenalty, archetypeModifier, eventModifier);
```

RecordDriverNeed is public but already subscribed: do not double-call it from the
needs coordinator. String seams accept only IDs already present in the configured
roster. There is deliberately no arbitrary event payload / dictionary / message API.

## Files, Bounds And Privacy

`Application.persistentDataPath/Analytics/` contains telemetry only:

- `install.id`: random GUID, reused across sessions, unrelated to hardware/user.
- `events-*.jsonl`: schema v1; each event has game version, build ID, schema version,
  random install/session/event IDs and fixed numeric/category fields. Build ID is
  Application.buildGUID (editor/unspecified fallback), never a repository/private path.
- `pending-*.jsonl`: durable optional upload batches, not game saves.
- `writer.lock`: exclusive file lease prevents competing writers to the same root.
- `Exports/`: the six fixed CSV filenames listed below.

The Unity thread only sanitizes small DTOs and TryAdds to a 512-event queue. One
background owner writes JSONL before creating upload batches and flushes local
file data. Batches have at most 64 events / 4096 characters per line. At most 32
local journal files and 64 queue files, each <=256 KiB, are retained (8 MiB local
journal + up to 16 MiB queue). Batches may contain fewer than 64 events; file limits
are limits, not a guaranteed minimum event history. Oldest files are removed at
the caps, including oldest offline queue data; queue losses are counted. Runtime
event storms are capped at 128 records/second; the startup roster is separately
bounded at 512. Debug Dropped includes rate/backpressure/IO/outbox retention loss.
Old local journal rotation is normal bounded retention and does not increment it.

Disk/network waits do not occur on the gameplay thread. Disk failure is reported
as IoUnavailable and events may be lost; there is no false durability promise on
a full/unwritable disk. Startup storage failure closes that writer. On normal
component teardown the background writer drains asynchronously. Process exit,
crash or forced termination can lose the not-yet-durable tail. Periodic snapshots
and durable batches bound the damage; shutdown deliberately does not join/wait on
the game thread. This is development telemetry, not a transactional save system.

No real names, emails, usernames, customer details, device identifiers, hardware
fingerprints, precise coordinates, reaction/dialogue text, private file data,
messages, stack traces, endpoint strings or response bodies are event fields.
Authored passenger/region IDs must be short ASCII identifier tokens. Invalid
strings are rejected wholesale. Numeric data is finite and bounded. Corrupt,
oversized, or unknown-schema queue lines are skipped, and accepted records are
re-serialized from the fixed DTO before upload/export so unknown fields cannot
escape. CSV cells are quoted and formula-leading values are neutralized.

## Optional Remote Delivery

No endpoint was supplied or configured. Real HTTPS/Discord delivery is unverified.
The implementation defaults to local-only and never connects without an explicit
valid external configuration. No network calls were made during this workstream.

External configuration location: `Application.persistentDataPath/analytics.local.json`.
It is outside Assets/Resources/StreamingAssets and is not bundled in builds.
Repository ignore rules cover that filename and its Unity meta if accidentally
placed in the repository. The editor menu
`Tools > Truck Taxi > Analytics > Create External Local Configuration` creates a
disabled template only if missing, preserving an existing file:

```json
{
  "remoteEnabled": false,
  "endpoint": "",
  "performanceSeconds": 20
}
```

Configuration is read once on worker startup, with a 4 KiB size cap; malformed or
missing config falls back to local-only. Restart the telemetry component/application
after editing. A supplied endpoint must use HTTPS, without userinfo or fragment.
No redirects, cookies, default credentials, Discord secret or response-body logging.
Server TLS validation is left intact. Payload is `application/x-ndjson`, not a
Discord webhook message: a private receiver may summarize to Discord server-side.
The operator must configure appropriate server retention/authentication; a receiver
necessarily sees network connection metadata such as source IP.

Uploads are requested periodically, on ride completion/major events, and on session
end (shutdown queues rather than waits). The persistent batch is deleted only on
2xx success. Failures keep it, with per-process exponential retry delays of
5, 10, 20, 40, 80, 160, 300 seconds (capped), and a 10-second request timeout.
Urgent flushes cannot bypass backoff. Retry timer resets on restart; queue data
survives. Delivery is at-least-once: receiver must deduplicate by eventId. Existing
queued data targets the currently configured endpoint after restart. Disabling
remote delivery leaves its retained queue untouched but adds no new upload data.
No older local-only journals are uploaded retroactively.

## CSV And Debug

`DEBUG > ANALYTICS` shows play/pause time, completed rides, unique passengers,
authored-roster coverage, interval/average FPS, estimated distance, qualifying
vehicle crashes, regions, pending uploads, local/upload status and dropped count.
EXPORT CSV queues a background export. Editor provides the same command and an
Open Local Analytics Folder command. No private filesystem paths are logged.

`Exports/Sessions.csv`, `Passengers.csv`, `Rides.csv`, `Objectives.csv`,
`Performance.csv`, `WorldEvents.csv` are replaced on export. They summarize only
retained journal files, deduplicated by event ID. Sessions includes start/snapshot/end
rows (take the last row per session for totals; do not sum cumulative snapshots).
Rides contains offer/pickup/outcome rows. Passengers aggregates across retained
sessions, up to 512 authored IDs, including zero-exposure roster rows. Numeric enum
codes are defined in TruckTaxiAnalyticsSchema.cs and the existing gameplay enum
types. Unknown values are -1 where appropriate. CSV is an export, never a restore
or save authority. Exports can coexist with local-only operation.

## Validation

- Added TruckTaxiAnalyticsTests EditMode suite covering private-token rejection,
  finite schema bounds/version checks, pause metrics, HTTPS opt-in validation,
  retry bounds, local persistence/identity/snapshot isolation, fake offline queue
  restart/acknowledgment, retention caps, malformed/unknown JSON fields, CSV
  escaping, real session observation/deduplication/unsubscription, and the shared
  decline/expiry gap. Fake transport tests do not contact a real endpoint.
- Executed pure C# checks for pause/driving/speed integration, five unsafe identifier
  cases, finite bounds and schema version rejection: passed.
- Executed opaque custom-ID checks for repeat correlation, invalid-ID rejection and
  the 256-alias cap: passed. Added generic custom item signal/unsubscription and
  cross-session alias isolation coverage to the unexecuted EditMode suite.
- Roslyn syntax and semantic analysis of all eight analytics/debug C# files against
  existing Unity/project/test reference assemblies: passed with no semantic errors;
  analysis-only, no output assembly or game build emitted.
- Unity EditMode suite was added but not executed. No Unity launch, Play Mode,
  player build, scene authoring, raw YAML edits or commits were performed.
- Remaining integration acceptance: wire Initialize, run the new EditMode suite,
  inspect debug tab at target resolutions, pause/unpause and compare clocks, finish
  a ride, inspect local files and six CSVs, verify profiler availability, test a
  configured receiver with network interruption/restart, and verify the chosen
  development-build shutdown behavior. Do not claim real upload success before it.

## Vendor Asset Report

- VENDOR ASSETS AUDITED: NWH Vehicle Physics 2 DemoTelemetry.cs and the existing
  LwsPlayerTruck/LwsVehicleTelemetry adapter; UTS_FullPack CarMove/CarAIController
  and TruckTaxi traffic/pedestrian adapters; Pixel Crushers Common Save System
  SaveSystem.cs and README; Scene Streamer README (1.26.1 asset, 1.26.3 package
  manifest) and LwsSceneStreamerAdapter; Heat UI HeatUIInternalTools.cs and existing
  TruckTaxiHud/MegaDebugPanel presentation helpers; package analytics inventory.
- VENDOR ASSETS USED: NWH speed through LwsPlayerTruck.LastTelemetry; UTS-backed
  traffic/pedestrian counters through TruckTaxi adapters; Scene Streamer loaded
  region/error facts through TruckTaxiRegionalWorld/LwsSceneStreamerAdapter;
  existing TMP/uGUI diagnostic helpers (no new UI framework).
- RELEVANT ASSETS NOT USED: NWH DemoTelemetry is reflection-based live vehicle
  display, not privacy-filtered durable analytics. Pixel Crushers Save System
  owns gameplay saves, not this telemetry outbox. Heat production widgets are not
  replaced; this extends the existing development diagnostic surface. The legacy
  Unity analytics module is installed, but no configured local-first analytics
  SDK/service was found; no external service dependency or new package was added.
- CUSTOM SYSTEMS CREATED: TruckTaxiAnalytics lifecycle/typed event adapter,
  fixed schema/metrics, bounded local JSONL and optional HTTPS outbox, CSV exports,
  development configuration helper and focused tests.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
