# Truck Taxi pedestrian impacts

## Authority and vendor audit

The existing `TruckTaxiPedestrianPopulation` still calls UTS Full Pack 2.0
`PeopleWalkPath.SpawnPeople` / `SpawnOnePeople`. UTS `Passersby` owns walking,
avoidance and animation. These NPCs do not use a NavMeshAgent.

Previously the city used non-ragdoll UTS walking prefabs. LWS hid all renderers
and colliders on a hit, then restored the NPC at its spawn. The new city
references project-owned variants of the supplied UTS `Girl_11` / `Girl_22`
ragdoll prefabs (adult pedestrian models), with 11 jointed bone rigidbodies.
The vendor `NPCStats.EnablePhysics()` remains the ragdoll switch. No vendor
source was edited and no second population or skeleton builder was created.

`TruckTaxiUtsRagdollAdapter` suppresses the vendor's unconditional Car-tag
trigger activation. Actual player contact must pass the taxi speed/attribution
check. The adapter retains the vendor's public physics activation method.
LWS explicitly stops Passersby/MovePath because UTS adds them after NPCStats.Awake.

## Collision and event path

NWH tractor OnCollisionEnter -> TruckTaxiCollisionObserver ->
TruckTaxiPedestrian.TryStrike -> vendor NPCStats.EnablePhysics ->
TruckTaxiSession.RecordPedestrianHit -> existing requests, Chaos,
passenger preferences, dialogue categories and passenger mechanics.

The pedestrian latches its hit before activation. All later bone contacts return
without generic collision scoring. Session additionally deduplicates by runtime
pedestrian ID. Impact context includes ID, relative speed, applied impulse,
world contact, passenger ID and accepted-ride ID. Context/statistics also record
hits without a passenger; ride score/objectives remain gated to an active ride.
No NPC code hardcodes character reactions.

## Tuning

Select `ScriptableObjects/TruckTaxi_DemoConfiguration.asset`, section
`Pedestrian impact / UTS ragdoll`:

| Setting | Default |
| --- | --- |
| Minimum Ragdoll Impact Speed | 2 m/s |
| Impact Force Multiplier | 40 N s per m/s |
| Maximum Ragdoll Impulse | 900 N s total |
| Upward Impulse | 70 N s at >=8 m/s, proportional below |
| Angular Impulse | 2 N m s at nearest bone |
| Ragdoll Lifetime | 15 seconds |

Horizontal launch follows incoming player velocity and uses collision-relative
speed measured before the solver. The summed impulse is clamped, then divided
over the bones. Total ragdoll mass is 70 kg, overriding UTS's per-bone 40 kg.
Dynamic bones use 8/3 solver iterations, continuous collision detection,
3 m/s maximum depenetration, 12 rad/s angular and 30 m/s linear limits.
The walking root alone collides before impact; bone colliders activate after.
NPCs without a vendor rig receive a physical root-body fallback and one
`PEDESTRIAN RAGDOLL NOT CONFIGURED` warning, not a disappearing model.

## Cleanup

UTS destroys the walking Rigidbody/capsule when enabling physics. This population
does not pool those irreversibly changed objects. After the lifetime, LWS removes
the ragdoll and requests a fresh pedestrian from its original UTS path. The fresh
instance gets original bones, Animator, movement, colliders and a new hit ID.
Reset Pedestrians uses the same UTS population paths. No persistent save format
or pedestrian save database is introduced.

## Requests and wording

The authored HitPedestrian asset and content builder say `Hit a pedestrian`.
Runtime objectives, request text and results use the actual scaled target:
`Hit a pedestrian` / `Hit 2 pedestrians` / etc.

`PassengerRequestDefinition` exposes additional required/forbidden behavior
flags. Built-in pedestrian/traffic/property-hit requests require Impact;
NoCollisions and SmoothRide forbid Impact. Eligibility checks both directions
against every active request. MaximumChaos does not inherently require a hit
and is not unnecessarily excluded. Future authored rules can extend the flags.

## Debug and content maintenance

Existing F8 / DEBUG panel, fourth tool page:
Ragdoll Nearest, Ragdoll All Visible, Reset Pedestrians, Show Pedestrian
Colliders (Scene gizmos), Show Last Pedestrian Impact. Diagnostics show speed,
impulse, ID, ragdoll state and whether a hit event was sent. Debug-only ragdoll
buttons do not award points. Actual player collisions do.

`Truck Taxi/Update Pedestrian Ragdolls` updates only the existing city's UTS
prefab references and request/configuration assets. It does not rebuild roads,
GPS, truck, passenger definitions or the city. Normal city generation references
these same prefab variants. Vendor source remains untouched.

## Validation

EditMode: 61 passed, 2 unrelated opt-in voice-generation tests skipped.
Coverage includes deduplication, context, objective progress, personality response,
thresholds, impulse caps, natural content wording and extensible compatibility.

PlayMode: 4/4 passed, including three consecutive rides, ordinary keyboard/NWH
driving, passenger presentation, GPS controls/rendering, and the new real-contact
ragdoll probe. Recorded contacts were 4.00 m/s / 163.78 N s and
12.00 m/s / 485.08 N s, each with 11 dynamic bones. Objective progress was
1 then 2; Chaos was 250 then 500. Passenger bark/subtitles responded, cleanup
completed and fresh UTS pedestrians resumed. Test PNGs were visually inspected.

Windows: build succeeded with zero errors (327,349,805 bytes). The actual
`Builds/TruckTaxiDemo/TruckTaxi.exe` ran both collisions and exited with code 0.
The standalone repeated the same 4/12 m/s measurements, 250/500 Chaos, 1/2 then
completed objective, passenger bark/subtitles, finite moving bones, no repeated
score, cleanup and fresh spawns. The standalone capture was visually inspected.
See `Validation/PedestrianStandalone.log` and `Windows_Pedestrian_*.png`.

The opt-in
`-truck-taxi-pedestrian-smoke` development build flag runs the same runtime
probe as the PlayMode test. It stages existing UTS pedestrians and drives the
actual NWH Rigidbody into them at 4 and 12 m/s; it does not invoke a fake
collision or scoring event. Normal launches do not create the probe.

The probe checks dynamic bone movement, stopped AI/Animator, finite velocities,
one event/score/objective update, existing passenger response, stronger fast
impulse, 15-second cleanup and fresh UTS respawns. Screenshots and XML/logs are
written under `Builds/TruckTaxiDemo/Validation`. This is automated runtime
validation, not a claim of a human-controlled manual street-driving session.

## Files in this patch

Created (plus Unity metadata):
- Scripts/TruckTaxiPedestrianImpact.cs: tuning, impact context and vendor bridge contract.
- Scripts/TruckTaxiPedestrianRuntimeProbe.cs: opt-in Editor/development EXE test fixture.
- VendorBridges/TruckTaxiUtsRagdollAdapter.cs: measured-hit gate over vendor activation.
- Prefabs/Pedestrians/Taxi_Girl_11.prefab and Taxi_Girl_22.prefab: UTS prefab variants.
- Tests/PlayMode/TruckTaxiPedestrianPlayModeTests.cs: real tractor/ragdoll test.
- Documentation/TruckTaxi_Pedestrian_Ragdolls.md: this report.

Modified:
- Scripts/TruckTaxiPedestrian.cs: physical transition, impulse and lifetime.
- Scripts/TruckTaxiPedestrianPopulation.cs: same-path replacement and debug tools.
- Scripts/TruckTaxiCollisionObserver.cs: measured hit and residual-contact guard.
- Scripts/TruckTaxiImpactTarget.cs: removes context-free pedestrian scoring.
- Scripts/TruckTaxiSession.cs: impact context/deduplication, request compatibility and wording.
- Scripts/PassengerRequestDefinition.cs: required/forbidden behavior vocabulary.
- Scripts/TruckTaxiPassengerRuntime.cs and TruckTaxiHud.cs: natural scaled objectives.
- Scripts/TruckTaxiConfiguration.cs and TruckTaxiBootstrap.cs: shared tuning binding.
- Scripts/TruckTaxiDebugPanel.cs: fourth tools page and impact diagnostics.
- Scripts/TruckTaxiPlayerSmokeTest.cs: dedicated standalone ragdoll-test flag.
- Editor/TruckTaxiDemoBuilder.cs: project-owned variants, targeted update and natural wording.
- Scenes/TruckTaxi_DemoCity.unity: existing UTS paths reference the ragdoll variants.
- ScriptableObjects/Requests/HitPedestrian.asset: objective/dialogue wording.
- ScriptableObjects/TruckTaxi_DemoConfiguration.asset: explicit impact defaults.
- Tests/EditMode/TruckTaxiSessionTests.cs: semantic regression coverage.

Existing unrelated work and the preceding GPS changes are preserved. No commit
or push was requested for this patch.
