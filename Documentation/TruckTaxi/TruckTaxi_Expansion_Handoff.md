# Truck Taxi expansion handoff

## Scope and status

Integrated sandbox expansion, September 2026. This is a substantial partial completion,
not completion of every requested feature: true Lose Vehicle pursuit is still absent.
The existing NWH tractor, UTS population,
Compass maps, Weather Maker/Weatherade, Heat controls, Pixel Crushers bark adapter,
and LWS route/clock/accessory authorities remain in place. This is not a new career
save system. No passenger voice generation or source-reference assignment ran.

**Known incomplete requirement:** Lose Vehicle remains disabled. The installed UTS
setup follows authored loops; increasing cruise speed on a loop is not genuine
player-following pursuit. The attempted speed-only adapter was rejected and removed.
The other six target-vehicle objective evaluators are implemented.

## Play and author

- Start `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_MainMenu.unity`, then New Game > Free Play.
  Story/Arcade remain disabled placeholders. Load reports No Save Available. Options
  reuse keyboard/gamepad audio and GPS controls. Stats are actual last-session values.
- Main tuning: `Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_DemoConfiguration.asset`.
  Ten recent offers get a 0.2 weighting, not exclusion. Recent repeats use their last
  dropoff for 60 game minutes; destination-only stops can use the nearest legal bay
  within 40 m. Location/district preference lasts up to 360 minutes.
  Pickup patience uses accepted-route distance, 25 seconds grace and 45-300 second
  bounds. Five patience types plus authored multipliers remain configurable.
- Morning/evening/late-night/pre-dawn affect cadence, fare and authored passenger tags.
  Weather adds configurable demand/fare multipliers. Appreciation is 35% base only
  after explicit 21+ eligibility and excellent ride gates; acceptance is still required.
- Captain Applause was not duplicated or given special weight in the inspected data.
  Independent weighted rolls lacked recent history; deduplication and a soft recent
  penalty address that mechanism. The historical random sequence was not reproduced.
- NEEDS opens Driver Needs, STORE and ITEMS using the existing Heat canvas. Purchases
  require stopping at a registered store and use the session wallet. Drinks leave empty
  bottles. A usable bottle/jug is required for the Insert-key QTE. Its World-bus liquid
  loop stops on success/spill/cancel. Filled containers can be physically thrown.
- Decorations use existing Left/Right dashboard and Mirror anchors. They are simple
  placeholder meshes; other suggested slots are not added. No GPS anchor is moved.
- Fuel uses NWH FuelModule. Default new Taxi module: 80 L, consumption multiplier 12.
  Stop in a gas bay to refuel; default stations use 8 L/s and 150 cents/L. Empty fuel
  triggers a cartoon burst/launch, placeholder cackle, fade, nearest-gas recovery via
  the existing upright controller, full refill and double normal refill charge.
- Snow/Heavy Snow/Blizzard use existing semantic weather and Weather Maker. Bounded
  lane-region depth reaches 0.6 m with real mesh/collider height, NWH grip/rolling
  resistance effects, and moving UTS plows that clear swept cells to 0.04 m.
- GPS uses a north-only cardinals sprite in both existing Compass instances. Routes,
  map cameras, player tracking and typed POIs are retained.

## Content and tools

- Roster: 114 profiles, including 24 explicitly adult glamorous IDs and five original
  racing homage IDs. Wobble presentation is a new stylized fallback: exact earlier
  desired assets were not recoverable in the inspected history. UTS visuals remain an
  alternate, including the current UTS rig on ragdoll activation.
- All passenger dialogue sets receive JugStarted/JugSucceeded/JugSpilled/
  JugThrownFromWindow text coverage, plus optional repeat/cancellation lines.
- Dialogue: `Assets/LWS/TruckTaxi/Passengers/Dialogue/<PassengerID>.asset`.
  Truck Taxi > Passenger Factory offers asset/folder access and selected/all CSV
  export/import at `Tools/TruckTaxiPassengerFactory/DialogueCSV/`. Keep stable IDs;
  changed text marks audio stale without deleting clips. See
  `Assets/LWS/TruckTaxi/Documentation/TruckTaxi_DialogueAuthoring.md`.
- Generated audio stays under `Assets/LWS/TruckTaxi/Passengers/GeneratedAudio/`.
  Future samples belong in `F:/Codexprojects/MyVoiceForRecording/WorkHere/voice_refs/`
  as `<PassengerStableID>__<tone>.wav`. Scan/preview/exact-ID assignment remains ready;
  generation is explicitly deferred until the designer supplies samples.
- Truck Taxi > World Builder configures named TT_ empty anchors non-destructively.
  The saved sandbox has each prefix example plus a built EasyRoads ServiceRoad.
  Road updates preserve unrelated graph edges and UUIDs. Parking/building anchors
  are metadata only; new road traffic lanes/intersections require their existing
  vendor authoring workflow rather than guessed automatic lane generation.
- Vehicle Objectives menu creates authored request assets and adds functional ones
  to a selected passenger. The first racing validation profile has those definitions.
  Exact-target rams, bounded follow/block/race, mission-only durability and physical
  collectible triggers use the existing session/collision/Compass services.
- Fare Complete lists goal result, progress, expiry and earned bonus/points, with
  keyboard/gamepad pagination and the existing fare/star breakdown.

## Population and audio

UTS still drives cars and pedestrians. Twelve bounded local walking areas replace
the rectangular-loop presentation, with lane-setback validation. A vendor Standard
Semaphore example supplies signal/crosswalk gates. Two concurrent four-second
road-rage responses are capped and restore normal settings. Genuine AI pedestrian
contacts ragdoll and emit world events/SFX without awarding player objective credit.
The arcade ragdoll now totals 12 kg, with zero-friction contact material and limited
impulses, so its bones do not act as heavy speed bumps against the multi-ton tractor.
This is deliberately lightweight cartoon tuning, not a realistic human mass.

The designer's `Assets/LWS/TruckTaxi/Audio/TruckTaxi.mixer` was verified and wired:
Voices, Vehicle, World, UI under Master. A project-owned NWH mixer copy preserves
its internal DSP/subgroups. No vendor mixer source was edited. Liquid, impact and
cackle fallback sounds are non-speech placeholders; subjective sound approval is
still the designer's task.

## Evidence

- Unity 6000.4.10f1; official CLI 1.0.0-beta.5 at
  `C:/Users/Jeremy/AppData/Local/Unity/bin/unity.exe`, reused without reinstall.
  Connected Editor commands drove asset setup, tests, Play Mode and BuildPipeline.
- Compile: clean. Initial TruckTaxi EditMode checkpoint: 251 pass, one obsolete
  capability assumption failed, two opt-in voice/preparation tests skipped. That
  failure exposed a missing description fallback, which was fixed. Focused final
  objective suite: 11/11 pass including the new expiry/reward snapshot test.
  Final continuity suite: 11/11 pass, including two new destination-only pickup
  regression tests. Combined latest outcomes cover 255 passing tests and two
  intentional skips; this is the union of the checkpoint and focused reruns, not
  a claim that the entire suite was rerun after each fix.
- Focused PlayMode: AI pedestrian real-collision test 1/1; liquid AudioSource
  play/pause/resume/stop/dispose test 1/1.
- Integrated Editor Play Mode: 48 checks, zero failures. Real Input System events
  navigated New Game/Free Play and completed the QTE. A real tractor collision
  completed exact-target Ram; actual trigger contact collected the physical drop.
  Repeat-location, cancellation, stores, thrown Rigidbody, live UTS plow movement/
  swept clearing, bounded rage, fuel rescue/charge/upright, and menu stats passed.
- Main menu, fare goals, driver needs, city, blizzard and rescue were rendered for
  inspection. A leaked global development HUD and clipped needs text were repaired.
  These are automated, teleport-assisted checks, not a human driving session.
- Windows build: `Builds/TruckTaxiDemo/TruckTaxi.exe`, StandaloneWindows64
  Development, succeeded with zero build errors, 443227611 bytes in the final build
  report. The actual executable exited 0 after **50 checks / zero failures / zero
  unexpected runtime errors**. This remains teleport-assisted validation, not manual
  driving or subjective listening. Viewed the rendered menu, goal summary, needs UI,
  blizzard and raised snow geometry.
- Corrective builds were needed after actual player validation exposed stripped
  Weatherade runtime shaders, deep-snow plow spawn interference, empty collider mesh
  submission when all snow cleared, and destination-only repeat-pickup rejection.
  These were fixed at their project-owned sources. Failed logs were preserved, not
  overwritten as evidence. The last run passed all affected checks.
- Snow's focused Editor observation measured 9.9 m of real UTS motion and 208 newly
  cleared cells over three seconds. Final player observation measured 1.05 m and 32
  swept cells during its bounded check. No test fabricated plow motion.
- Evidence: `Builds/TruckTaxiDemo/Validation/ExpansionEditor.log`,
  `Expansion_EditMode_Initial.json`, `Expansion_Objectives_EditMode.json`,
  `Expansion_AiImpact_PlayMode.json`, `Expansion_JugAudio_PlayMode.json`,
  `Expansion_Continuity_EditMode.json`, `ExpansionPlayer.log`, failed-run
  `ExpansionPlayer_Initial.log`, `ExpansionPlayer_EmptySnowMesh.log`,
  `ExpansionPlayer_RepeatOfferFailure.log`, and `Expansion_*.png`.

## Remaining limits

- True pursuing AI/Lose Vehicle is not implemented and cannot be selected.
- Manual Follow/Block/Race/Destroy driving, signal stop-line/crosswalk timing,
  prolonged deep-snow handling, trailer fuel rescue, cockpit decoration placement,
  and subjective audio are not claimed as passed.
- Snow cells, inventory, continuity/history and stats are session-local. No new
  Taxi career-save format or pretend working Load Game was introduced.
- Wobble/plow/decorations and cartoon SFX are placeholder presentation, not final art.
- No final truck-art replacement, wiper work, or bulk voices were undertaken.

## Vendor boundary

VENDOR ASSETS AUDITED: NWH Vehicle Physics 2, UTS Full Pack, Compass Navigator Pro,
EasyRoads Pro, Weather Maker, Weatherade, Heat, Pixel Crushers, existing LWS adapters.

VENDOR ASSETS USED: those existing runtime authorities and public adapter APIs;
Unity Input System, uGUI, TextMeshPro and AudioMixer.

RELEVANT ASSETS NOT USED: a replacement navigation/AI/vehicle framework, Scene Streamer,
full career persistence, Chatterbox generation, speculative vendor chase APIs.

CUSTOM SYSTEMS CREATED: Taxi session semantics, scoped objective/needs/fuel/snow
coordinators, editor content tools, UI presentation and explicit test fixtures.

VENDOR SOURCE MODIFIED: NO in this expansion. The pre-existing dirty tree includes
unrelated vendor asset changes; they were neither reverted nor attributed to this pass.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO. NWH/UTS/Compass/weather/Pixel Crushers
authorities were not replaced.
