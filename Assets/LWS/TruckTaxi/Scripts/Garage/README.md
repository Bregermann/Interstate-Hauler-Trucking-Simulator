# Truck Taxi Drive-In Garage

## Scene Integration

Main authoring owns the scene and bootstrap. No world, main-menu, HUD, spawner,
input bindings, vendor source, physics or GPS files were changed by this feature.

1. Place an LWS scene-owned GameObject near the Edenborough player spawn, with
   a BoxCollider marked isTrigger. Give it a volume tall and wide enough to
   contain the canonical truck Rigidbody position. Rotated volumes work; no
   collision layer, tag, trigger callback or extra Rigidbody is required.
2. Add TruckTaxiGarage to that object (not to the generated modal).
3. After TruckTaxiBootstrap.Ready and its HUD initialization, call:

```csharp
garage.Configure(bootstrap, bootstrap.hud, garageVolume, bodyPaintMaterials);
```

`bodyPaintMaterials` is an explicit Material[] of the existing truck body paint
materials actually present in the spawned truck's sharedMaterials. Do not pass
glass, tires, chrome, dashboard, occupant or whole-truck materials indiscriminately.
Only matching material slots with _BaseColor or _Color are changed, through
MaterialPropertyBlock. No shared material assets are edited. An empty/nonmatching
list deliberately leaves the garage unavailable and emits a warning.

Configure requires the existing HUD Heat slider prefab. It reuses HUD.Panel,
HUD.Text, HUD.Button, Heat sliders and the existing EventSystem/UIInput. Heat's
PlayerPrefs saving is turned off while controls are inactive, before Awake.
The existing WorldInteract action opens the garage; HUD modal routing owns
Submit/Cancel/Pause and slider navigation. There are no new input actions.

The truck must be the registry's canonical ActiveTruck and the existing
spawner's SpawnedTruck, match Career selection, be in the volume, and have both
linear and angular speed at or below 0.1. Active fares, races, on-foot mode,
companion interactions, saving/loading, blocked resume and foreign modals refuse
entry. Existing physical service/companion interactions have priority. Put the
customization volume outside overlapping repair/fuel/service interaction radii.

## Content And Factory Safety

TruckTaxiCareerEquipment resolves the resource catalog's existing null starter
reference on a runtime clone, using only Player.Definition with a real PlayerPrefab.
It does not fabricate catalog entries or modify the resource asset. The catalog
currently has one truck entry. The selector displays only owned, valid real-content
entries, and therefore honestly displays one truck in the present Career.

LwsPlayerTruckSpawner has no hot-swap/rebind API, and bootstrap has cached truck,
NWH, GPS, camera, passenger and save dependencies. GarageDraft.Apply refuses a
different actor definition rather than directly instantiating/replacing that actor.
Future multi-truck runtime switching requires a main-owned canonical factory
swap/rebind and relaunch-resolution implementation first. The Career command
TryApplyGarage persists a valid owned choice and paint atomically; it does not
claim to be a vehicle factory. No alternate truck or unsafe factory was invented.

## Persistence And Lifecycle

Paint is per owned truck: hasCustomPaint plus versioned opaque RGB paint data.
The explicit flag avoids JsonUtility inline-null ambiguity and defaults false
for old saves. The existing Session CapturePersistentState, Vibin participant,
Pixel Crushers provider, slots and autosaves carry it without a new persistence
authority or schema-version replacement. The paint adapter subscribes to
CareerEquipmentChanged and Configure reapplies the restored Career immediately
after startup, including relaunch. Catalog paint and arbitrary paint are mutually
exclusive. Restore Default is a draft edit until Apply. Cancel does not mutate
Career and restores the current saved presentation.

The modal has live HSV spectra/sliders, exact RGB hex readout and an exterior
camera view of the actual canonical truck, not a spawned physics preview clone.
The disabled camera renders on edit only; its texture is released on destruction
or reconfiguration. Garage close releases only its owner-scoped driving and offer
suppression, restores Career paint, and disposes its HUD modal lease. Interruption,
run/selection change, volume exit, disable and destroy use the same cleanup path.

## Validation Handoff

Unity, builds, tests and commits were intentionally not run. Main owns validation:

- Import/compile and run TruckTaxiGarageTests (nine focused cases).
- Drive into the configured volume, stop, interact, and verify HSV drag/gamepad
  navigation plus visible preview without tinting non-body material slots.
- Apply arbitrary paint, save/load and relaunch; confirm identical paint/truck ID.
- Cancel edits and Restore Default separately; verify only Apply persists changes.
- Disable/re-enable or destroy the garage while open; verify input, focus, offer
  suppression and pause ownership release. Try foreign modal/load interruptions.
- Verify the present selector contains only the single actual starter truck.

## Vendor Audit

- Audited/used: Assets/Heat - Complete Modern UI (SliderManager, HUD Heat buttons),
  existing NWH canonical truck/materials, and existing Pixel Crushers-backed
  TruckTaxiVibinSaveParticipant/Career payload. Existing HUD modal lease is reused.
- Relevant but not used: Heat UIManagerColorChanger changes UI theme colors,
  not truck paint. NWH vehicle effects do not provide a Career garage/color picker.
- Custom code: only garage semantics/draft, HSV controls, targeted paint adapter,
  and additive Career paint data/transaction.
- Vendor source modified: NO. Duplicate vendor functionality created: NO.
