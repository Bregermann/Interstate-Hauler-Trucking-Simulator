# Customize Pedestrian Plowing

Use the saved scene and Inspector tools below to customize this sport. Validation/build status is recorded separately in PedestrianPlowingCheckpoint.md. Normal builds preserve hand-edited saved scenes.

## Quick start: crowd size and characters

1. Open the project in Unity. Choose **Truck Sports → Pedestrian Plowing → Select Crowd Settings**.
2. In **Parade Crowd**, set **Crowd Size** (default 240, supported 16–384), **Bonus Fraction** and **Penalty Fraction**. The remaining targets are standard +10 targets. Gold star runners award +100; red STOP targets deduct100. Roles are shuffled independently of character appearance.
3. Expand **Characters**. Each entry has a prefab, display name, spawn weight and reaction text. Weight0 disables an entry. A larger weight makes it more common. Stop Play Mode before changing roster prefabs/weights, then re-enter the scene to create its finite crowd. Rematches reuse the allocated character pool; no target respawns during a round.
4. To add an existing funny Taxi character, select one or more **PassengerProfile** assets under `Assets/LWS/TruckTaxi/Passengers/Profiles`, then choose **Truck Sports → Pedestrian Plowing → Add Selected Taxi Profiles to Crowd**. This creates sport-owned render/rig prefabs under `Assets/TruckPedestrianPlowing/Prefabs` and appends weighted entries. It preserves the original meshes/materials and never changes the Taxi source prefab.
5. Choose **Truck Sports → Pedestrian Plowing → Refresh Crowd Rendering** after adding profiles or changing character meshes/materials. This updates owned role badges and distance representations without rebuilding the city. Close-up and struck characters use their complete original rig. The current flat-color funny models have merged distant geometry; textured or multi-material custom parts retain full rendering. **Use Distance Rendering** on the prefab can be unticked explicitly.
6. For a custom look, duplicate a sport-owned `… Parade.prefab`, open the duplicate and edit the character render meshes/materials attached to its humanoid bones. Keep the root PlowPedestrian/Taxi ragdoll/Animator/NavMeshAgent, walking capsule, bone Rigidbodies/joints, vest and badge references. Drag that duplicate into a Characters entry. Do not replace the root with an unrigged model.
7. Save the asset, stop/re-enter Play Mode and start a new match. Keep at least one positive-weight valid character. Model identity, skin color and body shape do not change points.

Standard targets also wear an oversized blue cartoon costume nose. It has no collider and never changes character skin, ragdoll bodies or hit qualification. In an owned character prefab, select **Standard cartoon costume nose** under **Bip01 Head** to adjust its placement/scale, or edit **Standard costume blue.mat** for its costume color. Refresh Crowd Rendering after renderer changes; role visibility is controlled automatically.

## Spawn areas and parade density

1. Open `Assets/TruckPedestrianPlowing/Scenes/TruckPedestrianPlowing.unity` (arena/chase), or `TruckPedestrianPlowingFirstPerson.unity` (Cab Chaos).
2. Select the hierarchy group **Crowd Spawn Zones**. Each child is a `PlowSpawnZone`, shown as a green wire rectangle when Gizmos are on.
3. Move or rotate a zone using the normal scene tools. Change its **Size X/Y** for width/depth. **Weight** controls relative population. Untick **Enabled For Crowd** or set weight0 to stop using that rectangle. Duplicate a zone to add another parade gathering area.
4. Spread zones across the connected streets, crossings and plaza. Leave truck-width routes between solid objects. Spawns are sampled on the connected truck-clear navigation mesh, separated from truck starts and other targets; excessive density inside tiny zones produces a clear spawn error instead of placing targets in walls.
5. After adding/removing zones or moving solid buildings, parked cars, stalls or barriers, choose **Truck Sports → Pedestrian Plowing → Rebake Current Scene Navigation**. This refreshes the match's zone references and saves the current layout. It does not regenerate the city.
6. Modify **Truck start** transforms for starting positions and headings; keep the complete truck clear of solid geometry and inside connected navigation. Test all eight starts.
7. The two scene layouts are separate. Edit/rebake each if you want a layout change in both. The crowd configuration asset is shared.

## Movement, reactions and performance

The same settings asset exposes walking/panic/bonus speed, danger radius, staggered crowd think rate, animation detail distance, ragdoll lifetime and active-ragdoll budget. Bonus runners use bounded acceleration and normal navigation; they never teleport. Increase density gradually and test four views in the Windows build. More characters means more meshes, animation and physics, even when effects are pooled. Unity LOD groups select the distant merged model, and hidden detailed skeletons stop updating. A genuine hit restores the full articulated model before ragdoll activation.

Reaction text is authored per roster entry. Original Taxi character appearances and available animations are retained. Recorded voice coverage is limited by the actual installed Taxi recordings; most funny profiles have text without recorded speech. Do not assign another character's recording as a claimed matching voice.

## Buildings, fire and props

The city hierarchy separates **Buildings**, **Festival dressing**, **Bounded cartoon fire**, **Crowd Spawn Zones**, and truck starts. Move or duplicate existing owned buildings and overturned-car models, adjusting their solid colliders to match. Fire is decorative and never awards points; each ParticleSystem has bounded emission and particle counts. Keep fire/props off essential routes and rebake navigation after changing solid obstacles. Lightweight crate bodies reset between rematches.

## Cab Chaos

The dedicated first-person scene uses its own setup preference type and forces driver-seat views. Each local human owns their cab, animated wheel, free look, Compass GPS and Taxi jug state. GPS follows that participant and routes toward the nearest remaining reachable positive target. Cab needs timing is configured in `Assets/TruckPedestrianPlowing/Configuration/Cab Driver Needs.asset`. The jug is an optional driving distraction and never modifies target points.

Jug bindings are displayed in the owning view. Whole pad: holdLB / tapRB; shared left: holdLT / clickleftstick; shared right: holdRT / clickrightstick. Keyboard jug bindings follow the selected keyboard cluster and are serialized on the scene's PlowCabExtras component. Do not assign keys already used by any active keyboard movement cluster. Pause/reconnect preserves challenge state and requires fresh input after release.

## Rebuild versus preserve hand edits

**Create or Rebuild Owned Scene** reauthors the arena from defaults and can overwrite hand edits in that owned scene. **Update First Person Variant** derives a fresh cab scene from the saved arena and can overwrite cab-scene edits. Use these deliberately, not as a routine save command. Normal build delivery uses **LWS.TruckSports.Editor.SportsBuilder.Build**, which validates saved scenes and uses the explicit Sports-only scene list. Keep the complete final `Builds/TruckSports` directory together.

## Impact amount and persistent ragdolls

In either Plowing mode, open **Pause → Settings → Blood Amount** and drag the percentage slider.0 disables the short stylized splashes;100 is the bounded maximum. The preference is shared by the arena/chase and Cab Chaos versions, survives rematches and is saved with settings or scene exit. It changes presentation only and never changes points or impact qualification.

For authoring defaults, choose **Truck Sports → Pedestrian Plowing → Select Crowd Settings**. Under **Impact Presentation**, drag **Blood Amount** (0–1) and keep **Keep Ragdolls** enabled. A player's existing in-game preference takes precedence over the default. Hit characters remain visible until rematch; once settled, their real poses leave active physics. Original flat-color characters combine into one posed mesh each. Custom textured/non-readable models keep their original renderers, so test their crowd performance. Settled presentation bodies do not obstruct driving. Rematch restores the original rig, animation, movement and colliders. No source Taxi prefab is modified.
