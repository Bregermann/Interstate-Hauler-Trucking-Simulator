# Alpha 0.10 core trailer sports — implementation handoff

Status: PARTIAL. Source implementation and explicit scene-authoring commands exist. No Unity execution, physical-controller acceptance, Windows smoke test, gameplay screenshot or human-playtest success is claimed by this implementation agent. Root integrator owns those checks and the authoritative acceptance ledger.

## Owned source and integration

All source resides under `Assets/TruckHitchSports`. `CoreHitchSportsAuthoring.CreateAll()` creates only missing scenes; it deliberately preserves already-authored scenes. It does not register menu entries or alter build settings. Root owns catalog, typed-results, metadata and final build integration.

Scenes produced:

| Sport | Saved scene | Runtime owner | Implemented physical behavior |
|---|---|---|---|
| 07 Trailerblazers | Scenes/Trailerblazers.unity | TrailerblazersMatch | Native hitched booster trailer, fuel/thrust, physical stage release, aerodynamic torque, descent braking, actual altitude/distance/resting-landed scoring |
| 08 Jackknife Highwire | Scenes/JackknifeHighwire.unity | HighwireMatch | Native articulated trailer on narrow suspended decks, exposed load forces, independent trailer braking, actual checkpoint traversal and falls |
| 09 Hitch & Run | Scenes/HitchAndRun.unity | HitchRunMatch | Physical kingpin approach, low-speed latch contest, explicit detachment/reconnection, cargo mass/condition, stopped depot delivery, team delivery totals |
| 10 Trailer Trebuchet | Scenes/TrailerTrebuchet.unity | TrebuchetMatch / TrebuchetArm | Physical loading cradle, native uncoupling, sling joint, hinged counterweight arm, timed release, actual projectile/resting score, collision-propagated target damage, three-attempt siege option |
| 11 Hitch Handoff | Scenes/HitchHandoff.unity | HandoffMatch | Two teams of two seats (human or AI), physical trailer baton, stopped uncoupling, teammate approach and recoupling, split-dependent handoff points, four-leg extended relay and reverse-entry transfer rule |
| 12 Unhitched! | Scenes/Unhitched.unity | UnhitchedMatch | Native axle flatbed, independent unjointed truck riding on real deck, weight/reaction impulses, suspension hops, actual rotation/airtime and supported-landing combo scoring |

Each owner derives from existing SportsEventSession through HitchCompetitionSession, keeping existing setup/controllers, ready gating, score journal, participant ownership, pause/reconnect, results and rematches. Existing SportsRallyViews provides owned human split views and AI spectator policy. Existing Heat/uGUI menu authoring and broadcast styling remain in use.

## Shared native trailer API

- `HitchAuthoring.CreateTractor(match, slot, position)` returns a SportsEventTruck backed by the original NWH SemiTruck powertrain, suspension and fifth-wheel module.
- `CreateTrailer(position, rotation, name)` instantiates the matched NWH SemiTrailer Variant. `CreateFlatbed` uses the NWH RocketCarrierTrailer axle/chassis rig.
- `PlaceCoupledAtStart(coupler, trailer)` is authoring-only initial pose setup. Runtime coupling never uses it.
- `TrailerBody` exposes Body, Vehicle, Module, Kingpin, CargoCondition, InitialMass and Owner, with cargo mass/COM controls and full child-body rematch reset.
- `HitchCoupler.TryCouple` requires ready native vehicles, kingpin separation <=0.025m, heading error <=8 degrees and point-relative velocity <=0.8m/s before calling the vendor API. NWH's API performs only the final tiny anchor correction and owns the real joint.
- `GuideCoupling` applies physical capture forces inside 0.65m at low speed; it does not move transforms or teleport a trailer through the approach.
- `Release` uses the vendor module, retains velocity/spin and waits before a new attachment so the old Unity joint can retire.

Initially coupled competitions hold both assemblies during countdown while NWH initializes; successful native attachment releases the bodies for suspension settlement. Results freeze physical assemblies. The fixture exercises rejected distant attachment, native anchor retention, real release momentum and repeated starts.

## Owned assets audited and reused

NWH Vehicle Physics 2 TrailerHitchModule, TrailerModule, matched Euro Truck/SemiTrailer and RocketCarrierTrailer; existing LwsNwhTrailerCouplingAdapter and TrailerPlugRig patterns; SportsNwhTractorMotor; installed ShipContainerPack/RoadPropsPack/ScaffoldsPack; Weather Maker terrain mountain model and weather adapter; existing terrain textures; Heat/uGUI/TextMeshPro/Cinemachine/DOTween presentation installed by shared authoring.

The normal Interstate coupling adapter remains unchanged. Its broad-overlap normal-trucking attachment policy is unsuitable for the sport's strict no-approach-teleport requirement; the sport adapter adds alignment/game rules and delegates actual coupling to NWH. No replacement vehicle, wheel or hitch solver was created. Specialized trebuchet hinges/slings are genuinely new gameplay equipment, outside normal road-trailer behavior.

UTS traffic, EasyRoads road-authoring and full open-world streaming are not replaced. These bounded equipment venues do not currently include ambient traffic or a streamed road network; the suspended highwire spans are collision-bearing sport equipment. No vendor source was modified.

## Verification and unresolved acceptance

`HitchCorePhysicsPlayModeTests` uses the existing reflective test-assembly pattern. Tests are implemented but NOT run by this agent. There is no fabricated test report.

Required integration checks remain: native prefab import/compilation; all six generated scenes; actual human control and AI completion; physical loading/capture tolerances; complete outcomes/rematches; Game Night typed winners/statistics; controller-only UI; Windows rendering and performance. Root must capture the actual result of each.

Presentation still needs each sport's original logo and actual gameplay image. Generic feedback clips are shared working audio, not bespoke final sound design. Some mode variants still share physical course layouts, especially Highwire precision/technical and Handoff obstacle settings; these need distinct authored equipment/rules before all four advertised variants can be accepted. AI control is real force/input based but unverified; no arbitrary AI scores are assigned. No sport is marked VERIFIED on the strength of source alone.

## Lead integration checkpoint — November 17, 2026

All six scenes are saved. Native fifth-wheel latch/release momentum and rematch tests passed. Saved rocket clearance passed. FocusedObjectives08 measured a 179.82 m rocket peak, one physical stage release and controlled landing. Five other natural sport objectives are queued. Later variant rules remain unverified. Original six PNG logos exist; menu/Windows integration is in progress. See the permanent acceptance ledger and Logs/TruckSports/Alpha10 for the exact scoped receipts. This supplements the implementation agent handoff; it does not certify the entire sport.
