using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckPedestrianPlowing;
namespace LWS.TruckTrailerPlug.Editor
{
    public static class TrailerPlugAuthoring
    {
        public const string Folder = "TruckTrailerPlug", Scene = "Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity";
        static Material steel, quarry, luminous;
        static PhysicsMaterial contact;
        [MenuItem("Truck Sports/Trailer Plug/Create quarry arena")]
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Preserve the authored Trailer Plug scene; apply targeted upgrades.");
            var m = SportsEventAuthoring.Begin<TrailerPlugMatch>(Folder);
            foreach (string folder in new[] { "Models", "Prefabs", "Configuration", "Art" }) Directory.CreateDirectory("Assets/" + Folder + "/" + folder);
            m.setup = new LWS.TruckBowling.BowlingSetup(); m.setup.count = 4; m.option = 0; m.NormalizeSetup();
            for (int i = 1; i < 4; i++) m.setup.players[i].source = LWS.TruckBowling.BowlingSource.AI;
            steel = SportsEventAuthoring.Mat(Folder, "Quarry crane safety enamel", new Color(.95f, .55f, .065f), .4f);
            quarry = SportsEventAuthoring.Mat(Folder, "Rocky quarry floor", Color.white, .1f);
            var terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdMudRocky.terrainlayer");
            if (terrainLayer == null) throw new InvalidOperationException("Installed rocky quarry terrain layer missing.");
            quarry.mainTexture = terrainLayer.diffuseTexture; quarry.mainTextureScale = Vector2.one * 32;
            if (terrainLayer.normalMapTexture != null) { quarry.SetTexture("_BumpMap", terrainLayer.normalMapTexture); quarry.EnableKeyword("_NORMALMAP"); }
            luminous = SportsEventAuthoring.Mat(Folder, "Bubbling mint glow", new Color(.1f, .67f, .35f), .7f);
            luminous.EnableKeyword("_EMISSION"); luminous.SetColor("_EmissionColor", new Color(.06f, .65f, .25f) * 2);
            contact = SportsEventAuthoring.Contact(Folder, "Quarry landing contact", .68f, .06f);
            m.hole = new GameObject("Functional open sinkhole").transform;
            Ground(m, terrainLayer); QuarryScenery(m);
            var truck = SportsEventAuthoring.Truck(m, Folder, "Suspended competition tractor", new Vector3(-7, 61, -22), 0);
            SportsRallyAuthoring.FitTruck(truck); truck.Power(false); truck.body.mass = 12; truck.body.solverIterations = 20;
            var r = truck.gameObject.AddComponent<TrailerPlugRig>(); m.rig = r; r.match = m; r.truck = truck;
            r.anchor = new GameObject("Bungee crane anchor").transform; r.anchor.position = new Vector3(0, 65, -12);
            r.spring = truck.gameObject.AddComponent<SpringJoint>(); r.spring.autoConfigureConnectedAnchor = false;
            r.spring.connectedAnchor = r.anchor.position; r.spring.anchor = new Vector3(0, .5f, -.5f);
            r.spring.maxDistance = 48; r.spring.spring = 180; r.spring.damper = 12; r.spring.tolerance = .05f;
            r.trailer = Trailer(r.truckStart + r.trailerOffset);
            r.trailer.gameObject.AddComponent<TrailerPlugContacts>().rig = r;
            r.cable = new GameObject("Actual bungee cable").AddComponent<LineRenderer>(); r.cable.widthMultiplier = .15f;
            r.cable.sharedMaterial = steel; r.cable.useWorldSpace = true;
            m.broadcast = SportsEventAuthoring.Camera("Quarry broadcast and landing camera"); SportsRallyAuthoring.TuneCamera(m.broadcast);
            m.broadcast.view.farClipPlane = 2500; m.cameras = new[] { m.broadcast.view };
            m.broadcast.Pose(new Vector3(0, 23, -4), new Vector3(42, 25, -62), 65, true);
            m.replay = SportsRallyAuthoring.Replay(m, new[] { truck.visuals, r.trailer.transform });
            m.replay.capacity = 1800; m.replay.rolling = true; m.replay.interval = .04f; m.replay.maximumReplayAge = 18;
            m.crowdRoot = new GameObject("Pooled shared Taxi pedestrian ecosystem").transform;
            var source = AssetDatabase.LoadAssetAtPath<PlowSettings>(LWS.TruckPedestrianPlowing.Editor.PlowAuthoring.SettingsPath);
            if (source == null || source.characters.Length == 0) throw new InvalidOperationException("Existing Plowing crowd configuration missing.");
            var settings = UnityEngine.Object.Instantiate(source); settings.name = "Trailer Plug crowd configuration";
            settings.keepRagdolls = true; settings.ragdollSeconds = 50; settings.crowdThinkRate = 6;
            AssetDatabase.CreateAsset(settings, "Assets/" + Folder + "/Configuration/Sinkhole Crowd.asset"); m.crowdSettings = settings;
            m.steam = Effect("Seal steam", new Vector3(0, .4f, 0), false, 14);
            m.bubbles = Effect("Bubbling sinkhole smoke", new Vector3(0, -1.5f, 0), true, 9);
            m.voice = m.gameObject.AddComponent<AudioSource>(); m.voice.playOnAwake = false;
            m.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            m.atmosphere = SportsChaosAuthoring.Weather(m, Folder); m.presets = SportsWeatherPresetsAuthoring.EnsureCatalog();
            SportsWeatherRenderingAuthoring.Install(m, Folder); SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m);
            SportsWeatherPickerAuthoring.Install(m, m.presets, new Vector2(0, 305));
            SportsScenicGradeAuthoring.Install(m.gameObject, m.cameras, Folder, -.4f);
            BakeNavigation(); SportsEventAuthoring.Save(Folder);
            Validate(); Debug.Log("TRAILER_PLUG_AUTHORED physical hitch, real open hole and shared Taxi crowd.");
        }
        static Rigidbody Trailer(Vector3 position)
        {
            var root = new GameObject("Detachable full cargo trailer"); root.layer = 2; root.transform.position = position;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(RenderOnlyTrailer(), root.transform);
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            var body = root.AddComponent<Rigidbody>(); body.mass = 9; body.isKinematic = true; body.solverIterations = 20;
            body.solverVelocityIterations = 8; body.maxDepenetrationVelocity = 3; body.maxAngularVelocity = 6;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = .015f; body.angularDamping = .18f;
            var hull = root.AddComponent<BoxCollider>(); hull.size = new Vector3(6, 4.6f, 16); hull.sharedMaterial = contact;
            return body;
        }
        static GameObject RenderOnlyTrailer()
        {
            string path = "Assets/" + Folder + "/Prefabs/Cargo Trailer Render Model.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab");
            var reference = UnityEngine.Object.Instantiate(source); reference.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            try
            {
                var filters = reference.GetComponentsInChildren<MeshFilter>(true); Bounds bounds = default; bool first = true;
                foreach (var filter in filters) { var renderer = filter.GetComponent<Renderer>(); if (renderer == null) continue; if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
                if (first || bounds.size.x < .1f) throw new InvalidOperationException("Owned trailer model has no usable geometry.");
                var scale = new Vector3(6 / bounds.size.x, 4.6f / bounds.size.y, 16 / bounds.size.z);
                var root = new GameObject("Cargo Trailer Render Model");
                foreach (var filter in filters)
                {
                    var renderer = filter.GetComponent<Renderer>(); if (renderer == null) continue;
                    var part = new GameObject(filter.name); part.transform.SetParent(root.transform, false);
                    part.transform.localPosition = Vector3.Scale(filter.transform.position - bounds.center, scale);
                    part.transform.localRotation = filter.transform.rotation; part.transform.localScale = Vector3.Scale(filter.transform.lossyScale, scale);
                    part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var copy = part.AddComponent<MeshRenderer>(); var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) if (materials[i] != null)
                    {
                        string materialPath = "Assets/" + Folder + "/Materials/Imported " + materials[i].name + ".mat";
                        var owned = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (owned == null) { owned = new Material(Shader.Find("Universal Render Pipeline/Lit")); owned.mainTexture = materials[i].mainTexture; if (materials[i].HasProperty("_Color")) owned.color = materials[i].color; AssetDatabase.CreateAsset(owned, materialPath); }
                        materials[i] = owned;
                    }
                    copy.sharedMaterials = materials; copy.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(reference); }
        }
        static void Ground(TrailerPlugMatch m, TerrainLayer layer)
        {
            const int n = 128; var vertices = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var triangles = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float angle = i * Mathf.PI * 2 / n; var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices[i * 2] = new Vector3(dir.x * m.openingRadii.x, 0, dir.y * m.openingRadii.y);
                vertices[i * 2 + 1] = new Vector3(dir.x * 90, 0, dir.y * 90);
                uv[i * 2] = new Vector2(vertices[i * 2].x, vertices[i * 2].z) / 180;
                uv[i * 2 + 1] = new Vector2(vertices[i * 2 + 1].x, vertices[i * 2 + 1].z) / 180;
                int a = i * 2, b = (i + 1) % n * 2, t = i * 6;
                triangles[t] = a; triangles[t + 1] = b; triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1; triangles[t + 4] = b; triangles[t + 5] = b + 1;
            }
            var mesh = new Mesh { name = "Quarry floor with real elliptical opening", vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, "Assets/" + Folder + "/Models/Open Quarry Floor.asset");
            var floor = new GameObject("Continuous physical floor around open hole"); floor.AddComponent<MeshFilter>().sharedMesh = mesh;
            floor.AddComponent<MeshRenderer>().sharedMaterial = quarry; var collision = floor.AddComponent<MeshCollider>(); collision.sharedMesh = mesh; collision.sharedMaterial = contact;
            var data = new TerrainData { heightmapResolution = 257, size = new Vector3(600, 100, 600), terrainLayers = new[] { layer } };
            var heights = new float[257, 257];
            for (int z = 0; z < 257; z++) for (int x = 0; x < 257; x++)
            { float px = x / 256f * 600 - 300, pz = z / 256f * 600 - 300, d = new Vector2(px, pz).magnitude;
              float h = d < 90 ? 0 : Mathf.SmoothStep(0, 50, Mathf.Clamp01((d - 90) / 85)) + Mathf.PerlinNoise(px * .018f + 17, pz * .018f + 31) * Mathf.Clamp(d - 90, 0, 15); heights[z, x] = (h + 30) / 100; }
            data.SetHeights(0, 0, heights); int res = data.holesResolution; var holes = new bool[res, res];
            for (int z = 0; z < res; z++) for (int x = 0; x < res; x++) holes[z, x] = new Vector2((x + .5f) / res * 600 - 300, (z + .5f) / res * 600 - 300).magnitude > 85;
            data.SetHoles(0, 0, holes); AssetDatabase.CreateAsset(data, "Assets/" + Folder + "/Models/Industrial Quarry Terrain.asset");
            var terrain = Terrain.CreateTerrainGameObject(data); terrain.name = "Complete rocky quarry basin and cliffs"; terrain.transform.position = new Vector3(-300, -30, -300);
            SportsEventAuthoring.Box("Deep pit catch floor", new Vector3(0, -28, 0), new Vector3(25, 1, 25), quarry, contact);
            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder); liquid.name = "Emissive bubbling sinkhole liquid"; UnityEngine.Object.DestroyImmediate(liquid.GetComponent<Collider>());
            liquid.transform.position = new Vector3(0, -5, 0); liquid.transform.localScale = new Vector3(m.openingRadii.x * 2, .08f, m.openingRadii.y * 2); liquid.GetComponent<Renderer>().sharedMaterial = luminous;
            var glow = new GameObject("Sinkhole underground glow").AddComponent<Light>(); glow.type = LightType.Point; glow.color = new Color(.1f, 1, .4f); glow.intensity = 12; glow.range = 20; glow.transform.position = new Vector3(0, -1, 0);
        }
        static void QuarryScenery(TrailerPlugMatch m)
        {
            var root = new GameObject("Quarry event environment").transform;
            var craneMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckBungeeDemolition/Models/Lattice Crane.asset");
            if (craneMesh == null) throw new InvalidOperationException("Existing Bungee Destruction lattice crane mesh missing.");
            var crane = new GameObject("Existing modeled bungee support crane"); crane.transform.SetParent(root); crane.transform.position = new Vector3(0, 0, -12);
            crane.AddComponent<MeshFilter>().sharedMesh = craneMesh; crane.AddComponent<MeshRenderer>().sharedMaterial = steel;
            for (int i = 0; i < 6; i++)
            {
                float a = (i + 1) * Mathf.PI * 2 / 7; Vector3 p = new Vector3(Mathf.Sin(a) * 64, 0, Mathf.Cos(a) * 64);
                SportsRallyAuthoring.Prop(root, "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab", p, new Vector3(20, 15, 22));
                SportsRallyAuthoring.Prop(root, "Assets/ShipContainerPack/URP/Prefabs/ShipContainerRed_URP.prefab", p * .8f, new Vector3(11, 3, 3));
            }
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2 / 28; Vector3 p = new Vector3(Mathf.Sin(a) * (78 + i % 3 * 4), 0, Mathf.Cos(a) * (78 + i % 3 * 4));
                SportsRallyAuthoring.Prop(root, "Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_DesertRock.prefab", p, new Vector3(8, 7 + i % 4 * 3, 12));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                SportsEventAuthoring.Label("Quarry warning sign", "DANGER\nTRAILER DROP ZONE", new Vector3(side * 22, 5, 6), .12f, Color.yellow);
                for (int row = 0; row < 4; row++) SportsEventAuthoring.Box("Stepped spectator terrace", new Vector3(side * (34 + row * 2), row * .65f, 4), new Vector3(2, .8f, 32), steel, null, root);
                for (int pole = 0; pole < 3; pole++)
                {
                    var light = new GameObject("Quarry event floodlight").AddComponent<Light>(); light.transform.SetParent(root); light.transform.position = new Vector3(side * 30, 17, -20 + pole * 22);
                    light.transform.LookAt(new Vector3(0, 0, 0)); light.type = LightType.Spot; light.spotAngle = 90; light.range = 80; light.intensity = 8;
                }
            }
            SportsEventAuthoring.Label("Party quarry banner", "TRUCK SPORTS: PARTY!\nTRAILER PLUG", new Vector3(0, 12, 38), .22f, new Color(.95f, .7f, .16f));
        }
        static ParticleSystem Effect(string name, Vector3 position, bool loop, float rate)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Effects/Prefabs/SkidSmoke.prefab");
            var root = UnityEngine.Object.Instantiate(prefab); root.name = name; root.transform.position = position;
            var ps = root.GetComponentInChildren<ParticleSystem>(); root.transform.localScale = Vector3.one; ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = loop; main.playOnAwake = false; main.startLifetime = 2.4f; main.startSize = 2; main.startSpeed = 2; main.maxParticles = 100;
            var emission = ps.emission; emission.rateOverTime = rate; var shape = ps.shape; shape.radius = 2;
            FixEffect(ps); return ps;
        }
        public static void UpgradePresentation()
        {
            EditorSceneManager.OpenScene(Scene); var m = UnityEngine.Object.FindAnyObjectByType<TrailerPlugMatch>();
            FixEffect(m.bubbles); FixEffect(m.steam);
            var trailer = m.rig.trailer.transform;
            if (trailer.Find("Cargo Trailer Render Model") == null)
            {
                var old = trailer.Find("Installed SemiTrailer Variant"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(RenderOnlyTrailer(), trailer); model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
                foreach (var ghost in m.replay.ghosts) UnityEngine.Object.DestroyImmediate(ghost.gameObject);
                UnityEngine.Object.DestroyImmediate(m.replay); m.replay = SportsRallyAuthoring.Replay(m, new[] { m.rig.truck.visuals, trailer });
                m.replay.capacity = 1800; m.replay.rolling = true; m.replay.interval = .04f; m.replay.maximumReplayAge = 18;
            }
            if (trailer.GetComponentsInChildren<Rigidbody>(true).Length != 1 || trailer.GetComponentsInChildren<NWH.VehiclePhysics2.VehicleController>(true).Length != 0) throw new InvalidOperationException("Trailer art must not own a second vehicle simulation.");
            EditorSceneManager.SaveScene(m.gameObject.scene); AssetDatabase.SaveAssets();
        }
        static void FixEffect(ParticleSystem system)
        {
            string path = "Assets/" + Folder + "/Materials/URP quarry steam.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/NWH/Vehicle Physics 2/Effects/Materials/DustCloud.mat");
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                material.SetTexture("_BaseMap", source.mainTexture); material.SetColor("_BaseColor", new Color(.55f,.75f,.58f,.4f));
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0); material.SetInt("_Cull", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
                AssetDatabase.CreateAsset(material, path);
            }
            foreach (var renderer in system.transform.root.GetComponentsInChildren<ParticleSystemRenderer>(true)) renderer.sharedMaterial = material;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.startSize = new ParticleSystem.MinMaxCurve(.4f,1.4f); main.maxParticles = 64; main.startColor = new Color(.6f,.85f,.68f,.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.7f,1.5f); main.startLifetime = 1.8f;
            var shape = system.shape; shape.radius = 1.6f; system.transform.root.localScale = Vector3.one;
            EditorUtility.SetDirty(system);
        }
        static void BakeNavigation()
        {
            var surface = new GameObject("Shared pedestrian quarry navigation").AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.layerMask = 1;
            var sources = new List<NavMeshBuildSource>(); NavMeshBuilder.CollectSources(new Bounds(Vector3.zero, new Vector3(180, 30, 180)), 1, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            var settings = NavMesh.GetSettingsByID(0); settings.agentRadius = .4f; settings.agentHeight = 1.8f; settings.overrideVoxelSize = true; settings.voxelSize = .18f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(180, 30, 180)), Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("Quarry pedestrian navigation failed.");
            AssetDatabase.CreateAsset(data, "Assets/" + Folder + "/Models/Quarry Pedestrian Navigation.asset"); surface.navMeshData = data; surface.AddData();
        }
        public static void Validate()
        {
            var m = UnityEngine.Object.FindAnyObjectByType<TrailerPlugMatch>();
            if (m == null || m.rig == null || m.rig.trailer == null || m.rig.spring == null || m.replay == null || m.crowdSettings == null || m.crowdSettings.characters.Length == 0)
                throw new InvalidOperationException("Trailer Plug scene is missing a physical rig, replay or shared crowd roster.");
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length != 1) throw new InvalidOperationException("Trailer Plug needs one listener.");
        }
    }
}
