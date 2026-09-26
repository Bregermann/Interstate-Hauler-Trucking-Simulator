# Driver needs, store, and cab items

## Runtime wiring

The existing `TruckTaxiDriverNeedsCoordinator.Initialize(owner, environment)` and
`TruckTaxiEnvironmentNeedsPanel.Initialize(owner, hud, environment, needs)` calls remain unchanged.
The panel's Store and Items pages use the existing Heat HUD and UI focus/navigation actions.
No Bootstrap, HUD, UIInput, main assembly definition, or city-builder edit is required.

Add `TruckTaxiStorePoint` to an authored stop and set `stableId` (for example
`TT_STORE_corner_market`), `displayName`, and `stopRadius`. An optional
`TruckTaxiRideLocation` supplies its stop position; otherwise the component transform is
used. `CanUse` rejects motion, height mismatch, or positions outside the bay.
The coordinator discovers scene-local stores during initialization.
The Store page can route to the nearest store through the existing GPS service;
purchase still requires the truck to be stationary inside that store's bay.

`TruckTaxiNeedsItemsSetup.ConfigureStorePoint(location)` is an idempotent Editor API
for a parent world builder. `ConfigureExamples(host)` configures existing demo locations
`taxi.stop.07` and `taxi.stop.12` and an existing Weather Maker water-splash clip as
optional impact audio. It does not open, rebuild, or save a scene. The menu command
`Truck Taxi/Configure Needs Store Examples` invokes the same setup on the open scene.
Do not invoke setup while the parent controls the Editor.

The LWS cab accessory registry's open `IH_CabAnchor_Dashboard01`,
`IH_CabAnchor_Dashboard02`, and `IH_CabAnchor_Hanging01` slots carry purchased
decorations via `LwsCabAccessoryAnchor.Attach`. GPS, passenger-seat, sleeper, and
memento slots are left alone. The player chooses Left or Right for dashboard
items, and a purchased copy can occupy only one slot. Decorations are simple
project-owned placeholder meshes.
The filled-container throw uses a project-owned window anchor on the player rig,
briefly holds the object in the cab, then releases a bounded physics projectile.

## Audio and validation

`jugLiquidClip` and `containerImpactClip` on the coordinator are optional. The
existing audio controller routes the jug loop and impact source to World. The liquid
source starts on QTE start and stops on success, spill, cancel, and destruction. The
audio audit found no suitable liquid-stream source clip. `TruckTaxiJugLiquidLoop`
therefore creates a bounded non-speech flowing-water/drop sound in memory; an
authored licensed clip overrides it. Rain/river ambience is not substituted and
source WAVs are untouched. The generated clip is released on disposal.

`TruckTaxiNeedsItemsTests` covers deterministic need rates, bottle creation,
QTE reservation/refund, disposal/throw counts, and stationary store eligibility.
`TruckTaxiNeedsRuntimeProbe` is an opt-in Inspector report. The integration pass
ran the EditMode needs tests, a real AudioSource lifecycle PlayMode test, and the
live store purchase/drink/Insert-key QTE/physical throw path in DemoCity. Cab
decoration mounts still need a designer cockpit-placement review.
