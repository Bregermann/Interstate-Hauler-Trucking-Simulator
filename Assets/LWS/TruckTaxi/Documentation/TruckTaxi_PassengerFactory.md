# Truck Taxi Passenger Factory

## Ownership

Open **Truck Taxi > Passenger Factory**. This extends `PassengerProfile`, not a
second passenger/job system. Runtime state remains in `TruckTaxiSession`.
NWH owns tractor physics, LWS/Compass own routing/presentation, UTS owns ambient
traffic/pedestrians, Heat/TMP supplies runtime UI, and Pixel Crushers supplies
the bark lifecycle. The factory window uses Unity UI Toolkit.

The authored database contains 85 stable profiles: the ten original demo
passengers plus 75 roster entries. Runtime display names are original; inspiration
names are development metadata. Explicit casting metadata is retained separately
from personality, requests and gameplay preferences.

## Authoring Workflow

1. Select or create a passenger. Search by name, ID, species, race, traits,
   district or voice ID; filter type, rarity and seat.
2. Edit Passenger Data, Casting, Appearance/Model/Rig, Animation, Seat, Voice
   and Dialogue. Unique Mechanics and pair/companion references are on Passenger Data.
3. Build Missing Components preserves existing authored data and stable IDs.
4. Generate Model Manifest produces a reviewable JSON/reference-sheet brief in
   `Tools/TruckTaxiPassengerFactory/Manifests/Passengers`.
5. Choose Manual Drop Folder, Placeholder, or a configured external generator.
   No external generator is installed or assumed. Its executable and arguments
   are configured in the factory's Installed Model Tools foldout; `{manifest}`
   receives a quoted absolute path. No credentials are embedded.
6. Set Appearance > Source Model Path, then Process Model. Blender processes a
   separate file under `Passengers/Models/Processed/<ID>`. Sources are immutable.
7. Setup Humanoid, Build Ragdoll, Build Prefab and Validate Passenger are also
   available individually. Invalid humanoid replacements retain the prior prefab.
8. Assign external reference WAVs, edit dialogue, and Generate Missing Audio.
9. Register Passenger refreshes the single runtime-safe database.

Build All performs Data, Manifest, Model, Rig/Animation, Prefab, Validation,
Registration and Audio stages. It skips valid cached outputs, reports missing
inputs, and continues after individual failures. Pause/Resume pauses between
operations. Cancel After Operation does not terminate a file write midway.
Rebuild Selected/Invalid are explicit repair paths. Re-running preserves GUIDs.
Runtime players do not use AssetDatabase, Blender or Python.

## Model / Animation Pipeline

Installed Blender 3.5 CLI is the default. `Models/process_model.py` imports FBX,
GLB/GLTF or OBJ, normalizes height/ground origin, recalculates normals, removes
loose vertices/cameras/lights, safely merges unrigged meshes, supplies missing
materials, checks UVs/material count, applies optional polygon reduction and
exports a separate FBX plus static LOD1. The prefab builder installs an LODGroup
when that file exists. Existing rig hierarchies/bone names are preserved.

Source format conventions are handled by Blender's import/export axes. Arbitrary
bad forward orientation, texture atlasing, unusual skeletons and damaged source
UVs still need artist review; warnings do not pretend to repair missing content.
No procedural humanoid skinning is claimed. A supplied armature must map cleanly.

Project-owned copies of two installed UTS models and eight animation files are
configured as Unity Humanoid in `Passengers/Animations/Humanoid`. The original
vendor models/importers remain unchanged. Their avatars and shared 22-state
controller animate the human fallback prefabs. Some semantic states deliberately
reuse an appropriate installed clip; these are not 22 new custom animations.
Human ejection uses a generated bone-based ragdoll. Nonhumans use explicit
stylized geometry, simple movement animation and a single-body physics fallback.

Reusable head/hair/clothing/accessory prefab slots and skin materials are supported.
Parts retain their authored local transforms. Height, body scale, head scale and
shoulder width are configurable. This is not a replacement spreadsheet or a
full automatic modular-character skin-weighting system. Final casting-matched
models are still required and are listed in `TruckTaxi_MissingModels.md`.

## Voice / Dialogue

Reuse the existing installation at
`F:/Codexprojects/MyVoiceForRecording/WorkHere`, its `.venv`, Engine, caches,
reference validation and model lock. No Chatterbox clone/install exists in Unity.
The queue sends line jobs, imports validated WAV paths, and assigns AudioClips.
Reference audio remains external and is never modified/copied into the game.

Only original demo passenger **Al Lee** is intentionally cast to the provided
neutral sample for this validation pass. Nine generated clips cover greeting,
chatter, arrival, collision, shortcut, speed, boarding, ejection and pair reaction.
This is a neutral audition, not impersonation/casting of the reference characters.
Other profiles have voice placeholders and work with subtitles. See
`TruckTaxi_MissingVoiceSamples.md` for exact missing references/audio.

Line selection supports weight, cooldown, one-shot, state/request restrictions
and interrupt priority. Cooldowns cannot be bypassed by fallback text. One bark
plays at a time. Ejection interrupts normal dialogue; optional audio can be absent.
Pixel Crushers `BarkController` calls the taxi `IBarkUI` adapter; the existing HUD
displays its subtitle and the adapter's AudioSource plays the generated clip.
Pausing preserves the bark; arrival dialogue can continue over the fare screen.

Import Dialogue accepts JSON `{ "lines": [...] }`, vendor-parsed CSV, or
tab-separated text. Required columns: passengerId, lineId, text, category.
Optional: subtitle, emotion, tags (semicolon separated), weight. Categories and
emotions use the code enum names. Invalid rows fail before mutation; changed text
invalidates the old generation hash/clip. Stable line IDs are upserted, not duplicated.

## Runtime / Ejection

Offer creation spawns the selected representation at the same pickup used by
the offer, GPS and session. A stationary tractor starts approach/boarding.
Leaving early cancels progress. The seat profile provides approach speed,
boarding distance, cab mount path and local pose/scale. Boarding is intentionally
abstracted rather than a physical cab-door animation.

The eight seat types support normal, oversized, tiny, dashboard, companion,
quadruped, robot and custom representations. Pair passengers spawn separately,
share the ride/objectives, and alternate independent dialogue/voice profiles.
Five authored pairs are configured in the roster. Tiny companions use data-driven
cab mounts. Placeholder clipping is not a final cab art pass.

Hold **F**, gamepad **View/Select**, or the HUD **EJECT PASSENGER** button for
1.2 seconds. All call `TruckTaxiPassengerRuntime.RequestEjection` and the same
session transition. No automatic ejection. The external body ignores the tractor's
colliders, receives capped velocity and self-cleans after 12 seconds.
Objectives fail, ejection bark plays, temporary mass is restored, the fare/rating
penalty is recorded once, and Available returns after three seconds.

Oversized seats apply bounded temporary mass (0-800kg) and a downward off-center
boarding impulse (0-2000Ns), using NWH's existing suspension/lean/cab motion.
The validation yeti uses 350kg and 700Ns. No camera controller or upright lock is
added. Dropoff, ejection, failure, disable and destruction restore mass.

## Unique Mechanics

`ITruckTaxiPassengerModifier` has ride, boarding, tick, driving, request,
destination, ejection, completion and cleanup hooks. A failing modifier is
disabled independently. The twelve authored kinds cover sports commentary,
fees, power level, destruction rewards, style combo, time pressure, design
analysis, never-tip, destination change, backseat driving, oversized and dashboard
companions. Monetary adjustments are bounded integer cents. No freight economy,
career save state or global gameplay manager is added.

Destination changes use the existing session/LWS route request. The accepted
offer remains an immutable historical snapshot. GPS uses the current destination.
Exact pair-specific bespoke scripts and final actor performances remain content
authoring tasks; the shared runtime path is permanent.

## Pickup Presentation / Debug

`TruckTaxiRideLocation.detectionRadius` is the sole radius; default 11m.
`passengerSpawnPoint` and `truckStopPoint` remain separate. The lightweight ring,
translucent inner area, distant beacon and world marker use that same stop/radius.
Surface raycasts happen only on target creation. Yellow approach, green inside,
red too-fast, and accelerated boarding pulse reflect the actual session.
Cancel, expiry, failure, boarding completion and cleanup hide the effect.

The existing debug panel now has Ride, Presentation and Passenger pages. It can
select a roster member, offer that passenger, preview dialogue/animation, eject,
cycle pickup visual states and display radius/spawn/stop guides. Normal gameplay
does not need this panel. No second Canvas or navigation system was introduced.

## Tests And Boundaries

EditMode covers IDs/registration, every prefab, animation bindings, filters,
pickup radius/speed, one-shot ejection/penalties, never-tip, data/prefab idempotency,
voice hash/cache/import and factory-window construction. PlayMode includes three
consecutive human/oversized/robot rides and ejection through the shared runtime.
The development Windows player has an opt-in `-truck-taxi-smoke` harness that
completes a teleport-assisted ride and tests hold-ejection/audio/subtitles.
This is not a claim of human driving or subjective final character art validation.

Validation evidence and remaining limitations are recorded in
`TruckTaxi_Prompt002_Completion.md`. Do not ship test fixture audio or machine-local
Jobs/Logs/UserSettings. Do not commit unrelated Dead Air/freight/vendor changes.
