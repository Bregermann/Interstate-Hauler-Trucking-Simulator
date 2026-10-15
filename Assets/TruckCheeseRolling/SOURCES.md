# Asset provenance and vendor audit

No purchases, downloads or vendor-source edits were made.

## Imported animals

- Sheep: `F:/Unity Projects/Haberdashery/Haberdashery Unity/Assets/The Sheep/SheepV5.fbx` and `SheepBody.png`, copied byte-for-byte to `Art/Sheep/`. Original author is not identified by the accessible source folder. Model SHA256 `6000a711ec723de26642ea49569ebc1f3d8186c0d162103cf82ac77a48367534`; texture `bf0bb424307efdaead1bef74d8f7fd7c96c7488cfe1b035df8b3e06ccc7d3527`. A sport-owned URP wool material and `Prefabs/ExplosiveSheep.prefab` bind those copies. No old Unity-5 prefab scripts or game logic were imported.
- Wolf: Malbers Animations, local `F:/Unity Projects/House-Cat-Simulator/Assets/Malbers Animations/Animal Controller/Wolf Lite/Wolf Lite v2.fbx`, copied to `Art/Animals/WolfLite.fbx`. Its untextured low-poly model uses an owned gray URP material.
- Cat: Malbers Animations, local `.../Animals Packs/04 Pets/Cat/Model/Malbers Cat.fbx` plus `Materials Textures/Realistic/Orange/T_Cat_Body_Orange.png`, copied to `Art/Animals/MalbersCat.fbx` and `CatOrange.png`. An owned URP body material uses that texture. Fur layers and unrelated animation/controller packages are not imported.

The cat and wolf reference poses are baked through Unity's `SkinnedMeshRenderer.BakeMesh` into native owned mesh assets, then pooled as static course props. Original files remain unchanged. Imported files, native metadata, baked meshes, materials and prefabs are committed. Sheep remains a genuine recognizable mesh, not a primitive or billboard.

## VENDOR ASSETS AUDITED

NWH Vehicle Physics 2 / WheelController and GR3D Euro Truck; Heat/uGUI/TMP; Cinemachine 2.10.7; DOTween; Weather Maker and Taxi's weather adapter; Weatherade; Vista terrain layers; Path Painter II terrain layers; JustCreate Low Poly Mega City nature props; Toon Gas Station rocks; Malbers local animal models; UTS and Spline Spawner responsibilities.

## VENDOR ASSETS USED

- NWH: `Assets/NWH/WheelController`, `WheelController.MotorTorque`, `BrakeTorque`, `SteerAngle`, friction/load and suspension public APIs, `DefaultFrictionPreset.asset`; truck geometry from `Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab` through the existing Hockey geometry adapter. Installed engine/skid audio is reused.
- Heat/TMP: existing `SportsUIAuthoring`, Heat `ButtonManager`/`InputFieldManager` and TMP text; shared Bowling participant/source schema and Sports settings/feedback/input adapters.
- Cinemachine 2.10.7: `CinemachineVirtualCamera`, Transposer, Composer, Collider, explicit per-view state updates. DOTween: linked countdown/result-title punch presentation.
- Weather Maker: scene-owned unpacked `Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab`, one precipitation manager, vendor rain/snow rendering for allowed cameras, volume controls and thunder audio. The supplied NoWater container still referenced water shaders, so an owned copy removes unused water shader references and water profiles. This avoids the vendor tessellated-water shader error without changing vendor files. Owned fixed-daylight profile; expensive volumetric clouds/fog disabled.
- Vista `Grass_Light_VistaSample.terrainlayer` and Path Painter II `stdMudRocky.terrainlayer`; JustCreate `SM_Nature_Tree_03` / `SM_Nature_Grass_01`; Toon Gas Station `Rock_Cluster_1A`; the imported Malbers meshes above.
- Taxi presentation: unchanged `TruckTaxiMushroomWaveFeature.SetCameraStrength` and `TruckTaxiMushroomWorldWave.shader`, plus mushroom profile presentation values. Owned renderer/pipeline/Volume/sky assets isolate all changes to Cheese Rolling. These dependencies already exist on the published Sports parent; no unpublished Taxi history is included.

## RELEVANT ASSETS NOT USED

UTS supplies road traffic, not free downhill interception and match rules. Spline Spawner supplies spline/roadside distribution, not this seed-owned race hazard/start logic. Vista's general graph/world generator is unnecessary for one deterministic native Terrain course; its terrain presentation is reused. Weatherade road accumulation is not duplicated: precipitation and grip are used here, with native mud surfaces, and Taxi's snow/road settings stay untouched. Taxi's bootstrap/calendar/needs systems are not imported for weather or cosmetic effects. No installed package supplies this cheese-scoring/round/recovery authority. The installed destructible helpers are Pixel Crushers persistence/saver components, not physical tree fracture. The small tree adapter configures native PhysX rigidbodies and breakable FixedJoints; it does not duplicate a destruction framework.

## CUSTOM SYSTEMS CREATED

LWS race/round/contact scoring, participant/background distinction, downhill interception AI commands, bounded damage/recovery and sheep-chain coordination, seeded course/start semantics, accelerator-edge jump qualification and breakable tree anchors, shared-sport UI adapters, stable camera targets, isolated weather/mushroom configuration, and explicit development validation. NWH/PhysX retain vehicle/contact authority; existing vendor rendering, UI and camera foundations are reused.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO. This adds sport-specific semantics and configuration adapters, not a new tire physics, weather renderer, camera framework, traffic, save or UI framework.

Imported Malbers copy SHA256 values:

- `CatOrange.png`: `7deec669229e3a3b6c9711998aae31218cd99d13789a7dc316c2c8fbf746185c`
- `MalbersCat.fbx`: `f06dcb9703a62c63837140201f3760296900e7a5fd341b1aee1fbeece334a3c8`
- `WolfLite.fbx`: `9c8e7f5b74cc5ee143c00af39f4e2e7e837670564a6171c99523a7cb63691667`

Native Terrain detail-map representation was checked against [Unity TerrainData API/source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/Terrain/Public/TerrainData.bindings.cs). The owned course explicitly uses InstanceCountMode; each populated cell represents six clumps rather than a tiny coverage value. Validation queries actual engine instance transforms as well as inspecting their rendering.
