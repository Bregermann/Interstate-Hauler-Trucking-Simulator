# Truck Air Hockey

Offline 1v1 truck mallets on a 24 × 12 table with 2.5-unit rounded corners, matching the original Hockey footprint. Start from Truck Sports; choose Human/AI per slot, editable names/colors, independent Easy/Normal/Hard difficulty, and shared keyboard, separate controllers, keyboard/controller or two sticks of one controller. The truck itself hits the puck, with no stick or separate mallet attachment. Its colored cab identifies its participant. Your truck stays in its own half. The puck crosses center freely.

## Rules and match flow

First to 7. A goal requires the whole puck to cross inside the opening. Exactly one award per crossing; an own goal credits the opponent. Blue/left opens the initial match, with a two-second countdown. The conceding side serves after each goal; trucks reset fairly inside their halves. Rematches preserve configuration and alternate opening serve. Results stay until the user chooses rematch, setup or selection. Escaped pucks and genuinely sealed/trapped pucks receive an announced fair recovery, retaining scores. Accessible stationary pucks are left alone. No timer, possession, shoot button, aim assist, online play, powerups or penalties.

## Controls and ownership

Blue: W/A/S/D. Orange: arrows. Whole pad: left stick steers, RT drives forward, LT brakes/reverses. Shared pad: each stick X steers/Y drives relative to its truck. Opposite throttle first brakes local velocity and then reverses. North/Y/Triangle joins empty sources. Start/Escape pauses. Mouse, arrows/D-pad/stick, Enter/South navigate menus.

Each whole controller has one owner, or its left/right sticks have separate owners. Duplicate whole/stick/keyboard sources are rejected. Shared disconnect identifies both owners and pauses score, serving state, physical state and phase clocks. Reconnect the same device or explicitly reassign. Release driving and confirmation controls before confirming/resuming. Inactive unassigned devices do not confirm human modal actions. Real keyboard rollover/controller ergonomics still need physical-device testing.

## Handling and physics

AirHockeyTruck exposes acceleration 24, drive cap 9, reverse cap 6, steering 205 degrees/sec, lateral grip 6, coasting drag .85 and opposite-input brake drag 9. These are separate from slippery Ice Hockey. SportsPlanarDrive shares the existing planar drive/brake convention. PhysX owns impacts; no second collision impulse is applied. Partial throttle respects the drive/reverse limits while physical impacts retain separately bounded headroom. Whole-body projection accounts for an oblong truck's steering pose, and all four corners against the rounded outline, with physical truck-only center/goal barriers as backup; the puck ignores those barriers.

AirHockeyPuck: radius .43, mass .7, drag .012, speed cap 22, continuous dynamic collision detection, locked height/rotation. Rails restitution .98, puck .96, zero surface friction. Truck mallets use mass 3/restitution .55. Long glides and banks come from physical contact. AI uses the same controls, limits, grip and containment; difficulty changes reaction, collider-based rebound prediction, positioning/aiming error. Rail approaches first line up at a reachable interior waypoint, make a brief shallow stroke for an inward-facing truck-side contact, and back off after physical contact to leave rebound space. Fair serve resets clear stale AI plans. Poor positioning uses reverse/turn recovery, never live puck/truck teleportation or scoring shortcuts. Whole-controller steering uses the processed stick plus a .16 axis deadzone; opposing trigger throttle has a .1 deadzone.

## Integration, tooling and validation

Assets/TruckAirHockey/Scenes/TruckAirHockey.unity is saved with two trucks, two AI agents, three truck-only barriers, one EventSystem/AudioListener and shared Heat/settings/feedback. AirHockeyRules owns scoring; AirHockeyMatch owns phase/device/serve/recovery. AirHockeyMenus caches source UI and refreshes on revisions. Audio and restrained assigned-pad haptics reuse SportsFeedback and respect shared Master/SFX/vibration; requests stop on pause, disconnect, focus loss and unload.

AirHockeyAuthoring.Create authors this sport through Unity Editor APIs. SportsArenaAuthoring upgrades only Ice Hockey geometry/tuning and the four-card collection. Existing legacy authoring calls the updated integrations. SportsBuilder.Build validates saved scenes and builds TruckSports, TruckBilliards, TruckHockey, TruckBowling, TruckAirHockey explicitly, restoring host settings on success/failure. CreateValidateBuild reauthors scenes and is not a validation-only command.

Development-only flags: -tsArenaUpdate targets geometry/contact/input/recovery/collection cases; -tsAirAI runs complete physical AI matches/rematches without successful-goal injection. -tsAirCorner isolates and records the accessible-corner reproduction. Existing -tsSixAI and retained Billiards/Bowling probes remain available. Explicit development probes mute native input while injected devices remain active, so desktop typing cannot navigate the test. Normal launches do not run probes or mute physical devices. VALIDATION.md records exact final runs, versions and remaining checks; generated logs/captures/builds are excluded from Git.

## Vendor report

VENDOR ASSETS AUDITED: NWH Vehicle Physics 2 ArcadeModule and WheelController APIs/demo/friction; GR3D SemiTruck prefab; Heat UI/InputField/Slider; DOTween; existing Truck Sports flat PhysX contacts, input/settings/navigation/feedback. Installed content inventory found no air-hockey/table-rules implementation.

VENDOR ASSETS USED: Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab render geometry; existing owned Billiards Ball/Cushion/Strike audio and Hockey Faceoff/GoalHorn/FinalWhistle; Assets/Heat - Complete Modern UI/Audio/Click.wav and Michsky.UI.Heat ButtonManager/InputFieldManager/SliderManager; shared SportsDeviceInput/SportsSettings/SportsFeedback/SportsMenuView/SportsCameraFit; DOTween DOScale; Unity PhysX/Input System/uGUI/TMP/URP.

RELEVANT ASSETS NOT USED: NWH full road powertrain/Arcade assists assume grounded wheel groups and do not provide flat-table mallet confinement, rebound rules or seven-point scoring. The update extends the existing Hockey planar PhysX adapter instead of replacing the road-driving authority. UTS road routes, wheel hardware, world/streaming/weather/save/dialogue and online services are outside this offline sport. Bowling continues using its NWH wheel authority unchanged.

CUSTOM SYSTEMS CREATED: Air-hockey scoring/serve/recovery, participant coordination, constrained mallet adapter/AI, table presentation, saved-scene tooling and focused validation. The shared planar drive extracts the already-existing Hockey control behavior with independent sport tuning.

VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
