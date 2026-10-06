# Truck Taxi world life and vehicle condition

## Authorities

- `TruckTaxiWorldLifeController` owns driving/on-foot/temporary interaction mode and the one first-person rig. NWH keeps the physical tractor, trailer and truck input; LWS input suppression is acquired only while outside the cab.
- `TruckTaxiWorldInteractable` is the scene-side interaction contract. Stable IDs are authored or derived from stable service-point IDs. E/gamepad South interacts; G/gamepad right-stick press exits or re-enters, Tab opens inventory, I opens truck inspection.
- `TruckTaxiDriverNeedsState` remains the inventory, hunger, thirst, bladder, bowel, toilet discovery, motel access and ambient-choice authority. The existing needs snapshot carries these optional fields; older saves restore with empty values. Pixel Crushers remains the save authority.
- `TruckTaxiFuelController` uses NWH's FuelModule. It no longer auto-refills at a bay: the on-foot nozzle/filler hold action dispenses and charges fuel. `TruckTaxiComponentDamage` stores semantic per-part condition and adapts public NWH wheel, powertrain, brake and steering properties; NWH still simulates the vehicle.
- `TruckTaxiFacilityAmbience` binds optional interactions to *existing pooled UTS pedestrians* near facilities. It creates no pedestrian population. The existing Road Pep effect handles the abstract encounter and comedown.

## Authoring facilities

Keep a stable `TruckTaxiServicePoint.stableId` and set Store, Food, Sleep, Restroom and/or Refuel capabilities. `TruckTaxiPhysicalFacilities` builds simple enterable validation rooms, pump, shelf items, register, toilet, urinal and portable toilet around loaded points. These cubes are validation art, not a final building kit. Set `generateValidationFacility = false` on a point with authored geometry to prevent duplicates, then place the same project-owned interactable components and stable IDs on authored props. Keep doorways clear, floors on collision ground and service parking bays within the point radius. Runtime refresh is idempotent per loaded stable point.

`TruckTaxiProductShelf` references an existing `TruckTaxiStorePoint` and `TruckTaxiNeedsItem`; `TruckTaxiCheckoutRegister` commits the basket through the existing wallet and inventory. Selecting a shelf only adds to the temporary basket. Quantity, return-one, insufficient funds and 99-item stack limits are enforced at checkout. The existing product catalog remains the only item catalog. The inventory modal provides item buttons, stack counts, current needs and a use action.

`TruckTaxiDieselPump` references `TruckTaxiGasStationPoint`. Take the nozzle at the pump, walk to `TruckTaxiFuelFiller` on the tractor and hold Interact. Fueling requires an on-foot driver, parked truck, parking brake, compatible bay, wallet funds and a hose within 12 m of the station. Release to pause, return the nozzle at the pump; NWH capacity prevents overfill. The HUD reports gallons and dollars. Rescue remains the existing roadside system.

`TruckTaxiDinerSeat` opens a seated interaction panel; order invokes existing `EatHere` billing/needs and Close returns the walker to the saved standing point. `TruckTaxiMotelDesk` charges the existing wallet for 12 game-hours of access; `TruckTaxiMotelBed` requires that stable room ID before invoking existing clock/needs sleep. Access and expiry survive the needs snapshot. `TruckTaxiCabSleeper` offers 2/4/8-hour rests while parked with the brake set; cab recovery is 0.8x and prepaid motel recovery 1.1x of existing sleep recovery. The world and clock remain live.

`TruckTaxiRestroomFixture` can be a urinal, toilet or portable toilet. Toilets and porta-potties permit bowel relief; bladder relief uses the existing needs authority. Three timing taps complete the default non-graphic challenge. Tab/gamepad Select toggles simplified hold-to-complete timing; Cancel never grants relief. Successful use records each stable toilet ID once in the existing needs snapshot. A future achievement service can consume the unique ID set; no Steam dependency is present.

The legacy in-cab service panel remains a route/menu surface. Its former instant buy/eat/bathroom/sleep callbacks now direct the player to physical interactions; the physical register, fixture, seat and bed own those actions. `ToiletCollectionChanged` fires once per newly used stable ID for future achievement evaluation.

`TruckTaxiFacilityAmbience` checks nearby loaded pedestrians every 0.8 s, caps interactive roles at two, and disables them off-foot/away from the facility. A panhandler can receive $2 or be declined; a fictional Road Pep interaction uses the existing timed effect and later comedown. Choice IDs are semantic and persisted. No NPC voice, nudity, detailed drug instructions or new effect framework is added.

## Units, damage and props

`TruckTaxiUsUnits` formats MPH, US gallons and USD/gallon, feet below 0.1 mile, or miles rounded to a tenth from 0.1 mile onward. Keep physics and vendor APIs in SI units.

`TruckTaxiComponentDamage` generates stable tractor part IDs (body regions, engine, radiator, transmission, steering, front/rear brakes, suspension corners, fuel tank, headlights and every actual NWH tire/rim) from the physical wheel layout. A collision maps contact to region and nearby wheel. The adapter applies NWH wheel damage/grip/rolling resistance, spring/damper reduction, engine/transmission damage, brake and steering reduction, cooling heat and fuel leakage. The on-foot truck-condition panel shows per-part condition and repair commands; only a service with the matching repair capability can charge and restore a part. Roadside assistance quotes and repairs by capability. The existing Pixel Crushers snapshot contains an optional component-damage member; old saves start healthy.

Add `TruckTaxiBreakawayProp` to lightweight/breakaway/destructible scenery. Structural category keeps its original hard collider. Small authored property targets auto-receive a low-mass breakaway adapter unless named as concrete/building/wall/guardrail/large tree. The adapter starts kinematic/non-blocking, activates on tractor contact, limits debris to four pieces and expires it. Adjust category/mass/durability per prop; do not put it on road, building or terrain colliders. NWH still determines the resulting tractor dynamics. Prop reset uses the existing `TruckTaxiImpactTarget` reset path.

## Performance and limits

No per-frame scene-wide object search, custom serializer, second NPC population or vendor source edits were added. Facility construction is once per loaded point, wheel adaptation runs through the existing traction tick, and ambient actor binding is capped. The temporary cab model and validation geometry need art/placement review when the commercial truck/interiors are replaced. CharacterController collisions, building access, steering feel, NPC placement and all interface readability require human Windows acceptance; automated tests do not establish those qualities.
