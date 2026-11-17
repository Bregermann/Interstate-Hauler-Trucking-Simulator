using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor.Animations;
using DigitalRuby.WeatherMaker;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckBowling;
using LWS.TruckHitchSports;
using LWS.TruckHitchSports.Editor;

namespace LWS.TruckHorrorSports.Editor
{
    public static class HorrorAuthoring
    {
        public static readonly string[] Folders = { "TruckHorrorSports/GraveyardShift", "TruckHorrorSports/ScarecrowSeason", "TruckHorrorSports/DeadEnd", "TruckHorrorSports/CursedCargo" };
        public static string Scene(HorrorSport sport) { string f = Folders[(int)sport]; return "Assets/" + f + "/Scenes/" + f.Substring(f.LastIndexOf('/') + 1) + ".unity"; }
        const string Trees = "Assets/Toon Gas Station/Prefabs/Tree_6A.prefab";
        const string Pump = "Assets/Toon Gas Station/Prefabs/Gas_Pump_1A.prefab";
        const string Warehouse = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab";
        const string House = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_House_04_V1.prefab";
        const string Gas = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Gas_Station_01.prefab";
        const string Fence = "Assets/JC_LP_MegaCity/Prefabs/FloorProps/SM_FloorProps_Fence_01.prefab";
        const string Crate = "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_PK04_WoodenCrate_01.prefab";
        static Material road, grass, amber, straw, dark;
        static PhysicsMaterial ground;
        static string currentFolder;
        static readonly List<HorrorObjective> tasks = new List<HorrorObjective>();
        static readonly List<HorrorAnomaly> events = new List<HorrorAnomaly>();
        static readonly List<Light> lamps = new List<Light>();
        [MenuItem("Truck Sports/Alpha 0.10/Create new horror venues")]
        public static void CreateAll() { foreach (HorrorSport sport in Enum.GetValues(typeof(HorrorSport))) if (!File.Exists(Scene(sport))) Create(sport); }
        public static void Create(HorrorSport sport)
        {
            if (File.Exists(Scene(sport))) throw new InvalidOperationException("Preserve hand-edited horror venue: " + Scene(sport));
            string folder = Folders[(int)sport];
            currentFolder = folder;
            var m = SportsEventAuthoring.Begin<HorrorMatch>(folder); m.sport = sport;
            m.setup.count = 4; m.option = 0; m.NormalizeSetup();
            for (int i = 1; i < 4; i++) m.setup.players[i].source = BowlingSource.AI;
            Directory.CreateDirectory("Assets/" + folder + "/Models");
            road = SportsEventAuthoring.Mat(folder, "Midnight damp asphalt", new Color(.1f, .12f, .14f), .65f);
            grass = SportsEventAuthoring.Mat(folder, "Dark field soil", new Color(.12f, .14f, .08f), .1f);
            amber = SportsEventAuthoring.Mat(folder, "Aged warning enamel", new Color(.7f, .42f, .08f), .2f);
            straw = SportsEventAuthoring.Mat(folder, "Harvest stalks and leaves", new Color(.34f, .29f, .08f), .1f);
            dark = SportsEventAuthoring.Mat(folder, "Scarecrow weathered canvas", new Color(.14f, .1f, .065f), .1f);
            ground = SportsEventAuthoring.Contact(folder, "Wet sealed ground", .7f, .03f);
            tasks.Clear(); events.Clear(); lamps.Clear();
            Ground(folder, sport);
            if (sport == HorrorSport.GraveyardShift) TruckStop(m);
            else if (sport == HorrorSport.ScarecrowSeason) Farm(m, folder);
            else if (sport == HorrorSport.DeadEnd) Highway(m);
            else Freight(m);
            m.objectives = tasks.ToArray(); m.anomalies = events.ToArray(); m.venueLights = lamps.ToArray();
            m.trucks = new SportsEventTruck[4]; m.headlights = new Light[4];
            if (sport == HorrorSport.GraveyardShift) m.footAgents = new HorrorFootAgent[4];
            if (sport == HorrorSport.CursedCargo) m.cargo = new HauntedCargo[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = sport == HorrorSport.DeadEnd ? new Vector3(-15 + i * 10, 1, -269) : new Vector3(-18 + i * 12, 1, -65);
                var truck = sport == HorrorSport.CursedCargo ? HitchAuthoring.CreateTractor(m, i, p + Vector3.forward * 15) : SportsEventAuthoring.Truck(m, folder, "Night driver " + (i + 1), p, i, 5);
                if (sport != HorrorSport.CursedCargo) { SportsRallyAuthoring.FitTruck(truck); truck.maxSpeed = sport == HorrorSport.DeadEnd ? 25 : 17; }
                if (sport == HorrorSport.DeadEnd) truck.transform.rotation = Quaternion.Euler(0, 90, 0);
                m.trucks[i] = truck;
                if (sport == HorrorSport.GraveyardShift) { m.footAgents[i] = FootAgent(i); m.footAgents[i].match = m; m.footAgents[i].participant = i; }
                var light = Lamp("Driver " + (i + 1) + " headlights", truck.transform.position + Vector3.up * 1.4f + Vector3.forward * 2, new Color(.85f, .9f, .72f), 45, 11);
                light.type = LightType.Spot; light.spotAngle = 70; light.transform.SetParent(truck.transform, true); light.transform.rotation = truck.transform.rotation * Quaternion.Euler(8, 0, 0); m.headlights[i] = light;
                if (sport == HorrorSport.CursedCargo)
                {
                    var trailer = HitchAuthoring.CreateTrailer(p, Quaternion.identity, "Haunted load " + (i + 1));
                    var coupler = truck.GetComponent<HitchCoupler>(); HitchAuthoring.PlaceCoupledAtStart(coupler, trailer);
                    var haunted = trailer.gameObject.AddComponent<HauntedCargo>(); haunted.trailer = trailer; haunted.coupler = coupler;
                    foreach (var child in trailer.GetComponentsInChildren<Transform>(true)) { if (child.name == "door left") haunted.doors = child; if (child.name == "door right") haunted.secondDoor = child; }
                    haunted.interior = Lamp("Something breathing inside", trailer.transform.position + Vector3.up * 2, new Color(.75f, .05f, .04f), 8, 0); haunted.interior.transform.SetParent(trailer.transform, true);
                    m.cargo[i] = haunted;
                }
            }
            if (sport != HorrorSport.CursedCargo) m.antagonist = Antagonist(m, folder, sport == HorrorSport.ScarecrowSeason);
            m.views = SportsRallyAuthoring.Views(m, m.trucks); m.views.groundOffset = new Vector3(0, 5, -11); m.views.groundLookAhead = 4; m.views.groundFov = 66;
            m.atmosphere = SportsChaosAuthoring.Weather(m, folder); m.presets = SportsWeatherPresetsAuthoring.EnsureCatalog(); m.views.weather = m.atmosphere;
            var cycle = m.atmosphere.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true);
            string clockPath = "Assets/" + folder + "/Materials/Authored midnight horror clock.asset";
            var night = AssetDatabase.LoadAssetAtPath<WeatherMakerDayNightCycleProfileScript>(clockPath);
            if (night == null) { night = UnityEngine.Object.Instantiate(cycle.DayNightProfile); night.name = "Authored midnight horror clock"; night.TimeOfDay = 0; night.Speed = night.NightSpeed = 0; AssetDatabase.CreateAsset(night, clockPath); } cycle.DayNightProfile = night;
            var weatherCameras = new Camera[5]; weatherCameras[0] = m.views.broadcast.view; for (int i = 0; i < 4; i++) weatherCameras[i + 1] = m.views.views[i].view;
            SportsWeatherRenderingAuthoring.Install(m.gameObject, weatherCameras, folder);
            m.ambience = m.gameObject.AddComponent<AudioSource>(); m.ambience.playOnAwake = false;
            string bed = sport == HorrorSport.ScarecrowSeason ? "Ambient/WeatherMakerAmbientSound_Crickets.ogg" : sport == HorrorSport.CursedCargo ? "Ambient/WeatherMakerAmbientSound_EnergyHum.ogg" : "Weather/WeatherMakerWind1.ogg";
            m.wind = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/" + bed);
            m.warning = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_Owl.ogg");
            m.encounterAudio = m.gameObject.AddComponent<AudioSource>(); m.encounterAudio.playOnAwake = false;
            var click = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Heat - Complete Modern UI/Audio/Click.wav"); m.feedback.clips = new[] { click, click, click, click, click, click, click };
            foreach (var anomaly in m.anomalies)
            { var sound = anomaly.gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 1; sound.minDistance = 5; sound.maxDistance = 90; sound.volume = .55f; sound.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_Crow.ogg"); anomaly.spatialSound = sound; }
            SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m);
            NightLighting(); BakeNavigation(folder, sport == HorrorSport.DeadEnd ? 800 : 520);
            // Save explicitly: SportsEventAuthoring.Save assumes a flat folder/scene name.
            string path = Scene(sport); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            SportsProductionAuthoring.InstallScene(); SportsFocusPolicy.InstallScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            AssetDatabase.SaveAssets();
            Debug.Log("ALPHA10_HORROR_AUTHORED " + sport + " physical objectives, owned environment and participant cameras");
        }
        static void Ground(string folder, HorrorSport sport)
        {
            var data = new TerrainData { heightmapResolution = 129, size = new Vector3(1100, 50, 1100) };
            var heights = new float[129, 129];
            for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++)
            { float edge = Mathf.Max(Mathf.Abs(x - 64), Mathf.Abs(z - 64)); heights[z, x] = edge < 47 ? 0 : Mathf.SmoothStep(0, .65f, (edge - 47) / 17) * (.4f + Mathf.PerlinNoise(x * .11f, z * .11f) * .6f); }
            data.SetHeights(0, 0, heights);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdMudRocky.terrainlayer");
            if (layer != null) data.terrainLayers = new[] { layer };
            string terrainPath = "Assets/" + folder + "/Models/Night landscape.asset"; var savedTerrain = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath);
            if (savedTerrain == null) AssetDatabase.CreateAsset(data, terrainPath); else { UnityEngine.Object.DestroyImmediate(data); data = savedTerrain; }
            var terrain = Terrain.CreateTerrainGameObject(data); terrain.name = "Owned textured countryside and continuous physical terrain"; terrain.transform.position = new Vector3(-550, -.15f, -550);
            for (int i = 0; i < 80; i++)
            { float angle = i * Mathf.PI * 2 / 80; float radius = sport == HorrorSport.DeadEnd ? 390 : 220; Prop(Trees, "Forest silhouette", new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius), new Vector3(12, 25 + i % 4 * 3, 12)); }
        }
        static void TruckStop(HorrorMatch m)
        {
            Surface("Fuel court", new Vector3(0, -.05f, 0), new Vector3(170, .1f, 160));
            Prop(Gas, "The Last Stop convenience store", new Vector3(0, 0, 60), new Vector3(65, 15, 30));
            Prop(House, "Closed roadside diner", new Vector3(-70, 0, 22), new Vector3(35, 12, 30));
            UtilityRoom();
            for (int i = 0; i < 4; i++) { Vector3 p = new Vector3(-35 + i * 22, 0, 5); Prop(Pump, "Pump " + (i + 1), p, new Vector3(3, 6, 2)); lamps.Add(Lamp("Fuel island fluorescent", p + Vector3.up * 8, new Color(.72f, .8f, .57f), 25, 6)); }
            Task("Collect the night delivery", new Vector3(-45, 1, -22)); Prop(Crate, "Undelivered supplies", new Vector3(-45, 0, -17), new Vector3(4, 3, 4));
            Task("Check fuel pump 03", new Vector3(9, 1, -2), 0);
            Task("Reset the breaker", new Vector3(68, 1, 36), 1, true); tasks[tasks.Count - 1].requiresFoot = true; tasks[tasks.Count - 1].truckApproach = new Vector3(70, 1, 15);
            Task("Answer the dead CB", new Vector3(73, 1, 38), 2); tasks[tasks.Count - 1].requiresFoot = true; tasks[tasks.Count - 1].truckApproach = new Vector3(70, 1, 15);
            Task("Check the security feed", new Vector3(73, 1, 32), 3); tasks[tasks.Count - 1].requiresFoot = true; tasks[tasks.Count - 1].truckApproach = new Vector3(70, 1, 15); tasks[tasks.Count - 1].securityMonitor = SecurityMonitor(m);
            Task("Restart the generator", new Vector3(48, 1, 55), 4, true);
            m.exit = Marker("Last stop extraction", new Vector3(-65, 1, -58));
            for (int i = 0; i < 3; i++) { var parked = Prop(HitchAuthoring.TractorSource, "Unoccupied parked semi", new Vector3(-58 + i * 20, 0, -46), new Vector3(6, 5, 12)); StripVehicle(parked); var a = parked.AddComponent<HorrorAnomaly>(); a.kind = HorrorAnomalyKind.DisappearingTruck; a.subject = parked; a.stage = i + 1; events.Add(a); }
            var visitor = new GameObject("Customer in the locked utility room"); visitor.transform.position = new Vector3(74, .3f, 44); visitor.transform.localScale = Vector3.one * .35f;
            AnimatedWatcher(visitor.transform, Folders[(int)HorrorSport.GraveyardShift]);
            var visitorTrigger = new GameObject("Authored impossible customer encounter").AddComponent<HorrorAnomaly>(); visitorTrigger.transform.position = visitor.transform.position; visitorTrigger.kind = HorrorAnomalyKind.Apparition; visitorTrigger.subject = visitor; visitorTrigger.stage = 2; events.Add(visitorTrigger);
        }
        static void Farm(HorrorMatch m, string folder)
        {
            Prop(Warehouse, "Abandoned harvest barn", new Vector3(0, 0, 105), new Vector3(50, 22, 40));
            Prop(House, "Farmhouse with no lights", new Vector3(125, 0, 100), new Vector3(35, 20, 32));
            Mesh stalk = CornMesh(folder);
            // Broad permanent orthogonal corridors form reachable loops. Gates alter secondary clearings only.
            for (int row = 0; row < 5; row++) for (int col = 0; col < 5; col++)
            {
                Vector3 p = new Vector3(-96 + col * 48, 0, -38 + row * 42);
                var patch = new GameObject("Dense rustling corn block " + col + ":" + row); patch.transform.position = p;
                patch.AddComponent<MeshFilter>().sharedMesh = stalk; patch.AddComponent<MeshRenderer>().sharedMaterial = straw;
                var wall = patch.AddComponent<BoxCollider>(); wall.center = new Vector3(0, 2.5f, 0); wall.size = new Vector3(27, 5, 24);
            }
            Task("Recover the first harvest token", new Vector3(-72, 1, -16));
            Task("Unlock the eastern ward", new Vector3(72, 1, 26), 0, true);
            Task("Recover the barn key", new Vector3(-72, 1, 68), 1);
            Task("Restore the final ritual ward", new Vector3(24, 1, 110), 2, true);
            var effigyMesh = ScarecrowMesh(folder);
            foreach (var task in tasks)
            {
                var effigy = new GameObject("Handmade harvest ritual effigy"); effigy.transform.position = task.transform.position + Vector3.right * 2; effigy.transform.localScale = Vector3.one * .3f;
                effigy.AddComponent<MeshFilter>().sharedMesh = effigyMesh; effigy.AddComponent<MeshRenderer>().sharedMaterial = dark;
            }
            m.exit = Marker("Farm escape gate", new Vector3(0, 1, 143));
            m.ritualAltar = Marker("Harvest ritual receiving altar", new Vector3(24, 1, 152));
            Prop(Crate, "Barn ritual altar", new Vector3(28, 0, 154), new Vector3(4, 3, 4));
            for (int i = 0; i < 3; i++)
            {
                var gate = Prop(Fence, "Unoccupied shifting side gate", new Vector3(-120 + i * 48, 0, 152), new Vector3(20, 4, 1));
                foreach (var transform in gate.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 2;
                var obstacle = gate.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box; obstacle.center = Vector3.up * 2; obstacle.size = new Vector3(20, 4, 1); obstacle.carving = true; obstacle.carveOnlyStationary = false;
                var a = gate.AddComponent<HorrorAnomaly>(); a.kind = HorrorAnomalyKind.MazeGate; a.pivot = gate.transform; a.stage = i + 1; events.Add(a);
            }
            var falseLights = new GameObject("False vehicle beyond the corn"); falseLights.transform.position = new Vector3(142, 0, 42);
            var deception = falseLights.AddComponent<HorrorAnomaly>(); deception.kind = HorrorAnomalyKind.FalseHeadlights; deception.stage = 2;
            deception.lights = new[] { Lamp("False driver left", falseLights.transform.position + new Vector3(-1, 2, 0), new Color(.8f, .76f, .5f), 22, 6), Lamp("False driver right", falseLights.transform.position + new Vector3(1, 2, 0), new Color(.8f, .76f, .5f), 22, 6) }; events.Add(deception);
        }
        static void Highway(HorrorMatch m)
        {
            var points = new List<Transform>();
            for (int i = 0; i < 64; i++)
            {
                float t = i * Mathf.PI * 2 / 64;
                Vector3 p = new Vector3(Mathf.Sin(t) * 210, 0, -Mathf.Cos(t) * 270);
                Vector3 tangent = new Vector3(Mathf.Cos(t) * 210, 0, Mathf.Sin(t) * 270).normalized;
                var strip = Surface("Reflective night highway " + i, p, new Vector3(30, .15f, 34)); strip.transform.rotation = Quaternion.LookRotation(tangent);
                var mark = SportsEventAuthoring.Box("Reflective center marking", p + Vector3.up * .1f, new Vector3(.2f, .025f, 10), amber); mark.transform.rotation = strip.transform.rotation;
                points.Add(Marker("Road steering point " + i, p + Vector3.up));
                if (i % 8 == 0) { var sign = Sign("EXIT " + (i / 8 + 1), p + Vector3.Cross(tangent, Vector3.up) * 19 + Vector3.up * 7); var a = sign.gameObject.AddComponent<HorrorAnomaly>(); a.kind = HorrorAnomalyKind.LoopSign; a.sign = sign; a.alternateCaption = "EXIT " + (i / 8) + "\nYOU ARE STILL HERE"; a.stage = 1 + i % 3; events.Add(a); }
            }
            m.roadRoute = points.ToArray();
            Task("Exit clue 01 · keep clockwise", points[10].position); Task("Exit clue 02 · the service road repeats", points[25].position, 0); Task("Exit clue 03 · ignore the false exit", points[41].position, 1); Task("The last headlight", points[57].position, 2);
            m.exit = Marker("The final real exit", new Vector3(-75, 1, -255));
            Surface("Escape service road", new Vector3(-80, 0, -255), new Vector3(50, .12f, 28));
            Prop(Gas, "Repeated abandoned service station", new Vector3(285, 0, 0), new Vector3(45, 14, 35));
            Prop(Warehouse, "Impossible tunnel portal", new Vector3(0, 0, 310), new Vector3(75, 25, 22));
            for (int i = 28; i <= 35; i++)
            {
                var p = points[i].position - Vector3.up;
                var next = points[(i + 1) % points.Count].position; var tangent = Vector3.ProjectOnPlane(next - p, Vector3.up).normalized; var side = Vector3.Cross(tangent, Vector3.up);
                var ceiling = Prop("Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Celling_4x6m_01.prefab", "Enclosed highway tunnel roof", p + Vector3.up * 10, new Vector3(38, .6f, 34), true); ceiling.transform.rotation = Quaternion.LookRotation(tangent);
                foreach (float sign in new[] { -1f, 1f })
                { var wall = Prop("Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Wall_4x3m_01.prefab", "Enclosed highway tunnel wall", p + side * (17 * sign), new Vector3(1, 10, 34), true); wall.transform.rotation = Quaternion.LookRotation(tangent); }
                lamps.Add(Lamp("Tunnel emergency lamp", p + Vector3.up * 8, new Color(.7f, .3f, .08f), 25, 3));
            }
        }
        static void Freight(HorrorMatch m)
        {
            Surface("Containment freight circuit", new Vector3(0, 0, 80), new Vector3(280, .15f, 380));
            for (int i = 0; i < 3; i++)
            { Vector3 p = new Vector3(i % 2 == 0 ? -90 : 90, 0, 15 + i * 90); Prop(Warehouse, "Abandoned containment depot " + i, p + Vector3.right * (p.x < 0 ? -45 : 45), new Vector3(60, 20, 60)); Task("Contain the load at ward " + (i + 1), p + Vector3.up, i - 1, true); }
            m.exit = Marker("Final cursed freight delivery", new Vector3(25, 1, 235));
            Prop(Warehouse, "The final silent destination", new Vector3(25, 0, 295), new Vector3(110, 30, 60));
        }
        static HorrorPursuer Antagonist(HorrorMatch m, string folder, bool scarecrow)
        {
            HorrorPursuer actor;
            if (!scarecrow)
            {
                var t = SportsEventAuthoring.Truck(m, folder, "Driverless night semi", new Vector3(20, 1, m.sport == HorrorSport.DeadEnd ? -285 : 110), 0, 6);
                SportsRallyAuthoring.FitTruck(t); t.grip = 1.4f; actor = t.gameObject.AddComponent<HorrorPursuer>(); actor.semi = t; actor.body = t.body; actor.speed = m.sport == HorrorSport.DeadEnd ? 22 : 10;
            }
            else
            {
                var g = new GameObject("The harvest watcher · physical scarecrow"); g.transform.position = new Vector3(120, 2, 120);
                var body = g.AddComponent<Rigidbody>(); body.mass = 150; body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; body.interpolation = RigidbodyInterpolation.Interpolate;
                var collider = g.AddComponent<CapsuleCollider>(); collider.radius = 1; collider.height = 6; collider.center = Vector3.up * 2;
                actor = g.AddComponent<HorrorPursuer>(); actor.body = body; actor.scarecrow = true; actor.speed = 8;
                actor.animator = AnimatedWatcher(g.transform, folder);
                var model = new GameObject("Authored tattered cloth and straw silhouette"); model.transform.SetParent(g.transform, false); model.AddComponent<MeshFilter>().sharedMesh = ScarecrowMesh(folder); model.AddComponent<MeshRenderer>().sharedMaterial = dark;
                foreach (var child in g.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
            }
            actor.signature = new[] { Lamp("Unnatural left headlight", actor.transform.position + new Vector3(-.7f, 1.6f, 2), new Color(.9f, .7f, .25f), 22, 4), Lamp("Unnatural right headlight", actor.transform.position + new Vector3(.7f, 1.6f, 2), new Color(.9f, .7f, .25f), 22, 4) };
            foreach (var light in actor.signature) light.transform.SetParent(actor.transform, true);
            return actor;
        }
        static Animator AnimatedWatcher(Transform parent, string folder)
        {
            const string sourcePath = "Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Man_11.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath); if (source == null) throw new InvalidOperationException("Owned humanoid antagonist model missing");
            var model = UnityEngine.Object.Instantiate(source); model.name = "Owned humanoid rig · harvest watcher"; model.transform.SetParent(parent, false);
            foreach (var joint in model.GetComponentsInChildren<Joint>(true)) UnityEngine.Object.DestroyImmediate(joint);
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rb);
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
            var animator = model.GetComponentInChildren<Animator>(true); if (animator == null) throw new InvalidOperationException("Owned scarecrow rig has no animator");
            var renderers = model.GetComponentsInChildren<Renderer>(true); Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = 5.5f / Mathf.Max(.1f, bounds.size.y); model.transform.localScale *= scale; model.transform.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i]; if (original == null) continue; string path = "Assets/" + folder + "/Materials/Harvest " + original.name.Replace('/', '_') + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.mainTexture = original.mainTexture; material.color = new Color(.26f, .19f, .09f); AssetDatabase.CreateAsset(material, path); }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            string controllerPath = "Assets/" + folder + "/Models/Harvest watcher locomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller != null) { animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; return animator; }
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath); controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Still watching"); idle.motion = Clip("idle1");
            var run = machine.AddState("Harvest pursuit"); run.motion = Clip("run"); run.speed = .7f;
            var move = idle.AddTransition(run); move.hasExitTime = false; move.duration = .25f; move.AddCondition(AnimatorConditionMode.Greater, .5f, "Speed");
            var stop = run.AddTransition(idle); stop.hasExitTime = false; stop.duration = .3f; stop.AddCondition(AnimatorConditionMode.Less, .5f, "Speed");
            machine.defaultState = idle; animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; return animator;
        }
        static AnimationClip Clip(string name)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/LWS/TruckTaxi/Passengers/Animations/Humanoid/" + name + ".FBX")) if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            throw new InvalidOperationException("Owned humanoid animation missing: " + name);
        }
        static void Task(string caption, Vector3 position, int prerequisite = -1, bool power = false)
        {
            var root = new GameObject(caption); root.transform.position = position;
            var t = root.AddComponent<HorrorObjective>(); t.caption = caption; t.requires = prerequisite; t.restoresPower = power; t.seconds = power ? 3 : 2;
            t.beacon = Lamp("Objective lantern", position + Vector3.up * 2, new Color(.85f, .55f, .14f), 9, 4); t.beacon.transform.SetParent(root.transform, true); tasks.Add(t);
        }
        static Transform Marker(string name, Vector3 p) { var g = new GameObject(name); g.transform.position = p; return g.transform; }
        static TextMesh Sign(string caption, Vector3 p) { var sign = SportsEventAuthoring.Label("Roadside sign", caption, p, .12f, new Color(.8f, .85f, .65f)); sign.transform.rotation = Quaternion.Euler(0, 180, 0); return sign; }
        static Light Lamp(string name, Vector3 p, Color color, float range, float intensity)
        { var l = new GameObject(name).AddComponent<Light>(); l.transform.position = p; l.type = LightType.Point; l.color = color; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None; return l; }
        static GameObject Surface(string name, Vector3 position, Vector3 size) => SportsEventAuthoring.Box(name, position, size, road, ground);
        static GameObject Prop(string source, string name, Vector3 position, Vector3 size, bool stretch = false)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(source) == null) throw new InvalidOperationException("Owned environment source unavailable: " + source);
            var root = new GameObject(name); root.transform.position = position; LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root.transform, source, size, 0, stretch, currentFolder);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh != null) { var c = filter.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = filter.sharedMesh; }
            return root;
        }
        static HorrorFootAgent FootAgent(int slot)
        {
            const string path = "Assets/NOT_Lonely/FirstPersonController/SimpleFPController.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (source == null) throw new InvalidOperationException("Owned first-person interaction rig missing: " + path);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(source); rig.name = "Participant " + (slot + 1) + " interior interaction rig";
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var behaviour in rig.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var camera in rig.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in rig.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);
            foreach (var collider in rig.GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = 2;
            var agent = rig.AddComponent<HorrorFootAgent>(); agent.controller = rig.GetComponentInChildren<CharacterController>(true);
            if (agent.controller == null) throw new InvalidOperationException("Owned first-person rig has no CharacterController");
            agent.flashlight = Lamp("Owner-bound utility flashlight", rig.transform.position + Vector3.up * 1.5f, new Color(.82f, .87f, .72f), 18, 5); agent.flashlight.type = LightType.Spot; agent.flashlight.spotAngle = 65; agent.flashlight.transform.SetParent(rig.transform, true);
            agent.EnterCab(); return agent;
        }
        static void UtilityRoom()
        {
            const string folder = "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/";
            Surface("Utility room sealed floor", new Vector3(70, .04f, 36), new Vector3(22, .12f, 22));
            Prop(folder + "SM_Floor_4x6m_01.prefab", "Utility room owned floor panels", new Vector3(70, .12f, 36), new Vector3(22, .3f, 22), true);
            Wall(new Vector3(59, 0, 36), new Vector3(1, 7, 22)); Wall(new Vector3(81, 0, 36), new Vector3(1, 7, 22)); Wall(new Vector3(70, 0, 47), new Vector3(22, 7, 1));
            Wall(new Vector3(62, 0, 25), new Vector3(6, 7, 1)); Wall(new Vector3(78, 0, 25), new Vector3(6, 7, 1));
            Prop(folder + "SM_ToolCabinet_01.prefab", "Breaker service cabinet", new Vector3(66, 0, 41), new Vector3(4, 4, 2));
            Prop(folder + "SM_MetalTable.prefab", "CB radio desk", new Vector3(75, 0, 42), new Vector3(4, 2, 2));
            Prop(folder + "SM_MetalShelve_01.prefab", "Night supplies shelving", new Vector3(60, 0, 43), new Vector3(2, 5, 4));
            Prop("Assets/ToiletSet/URP/Prefabs/SM_Toilet Variant.prefab", "Night-shift restroom toilet", new Vector3(62, 0, 44), new Vector3(1.2f, 1.2f, 1.4f));
            Prop("Assets/ToiletSet/URP/Prefabs/SM_Sink Variant.prefab", "Night-shift restroom sink", new Vector3(64, 0, 44), new Vector3(1, 1.3f, .8f));
            Prop("Assets/ToiletSet/URP/Prefabs/SM_WetFloorSign Variant.prefab", "Unattended wet floor", new Vector3(65, 0, 40), new Vector3(.7f, 1, .7f));
            Prop("Assets/Modern Resaturant - building and interior/Models/Prefabs/Summer_Terrace_Table.prefab", "Overnight break table", new Vector3(76, 0, 28), new Vector3(3, 2, 3));
            Prop("Assets/Modern Resaturant - building and interior/Models/Prefabs/Summer_Terrace_Chair.prefab", "Overnight break chair", new Vector3(78, 0, 28), new Vector3(1.1f, 1.8f, 1.1f));
            lamps.Add(Lamp("Utility fluorescent fixture", new Vector3(70, 6, 35), new Color(.63f, .76f, .64f), 18, 7));
        }
        static void Wall(Vector3 position, Vector3 size)
        {
            var wall = Prop("Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Wall_4x3m_01.prefab", "Utility architectural wall", position, size, true);
            // Fit preserves material/geometry; a simple solid envelope guarantees reliable wall contact at modular seams.
            var collider = wall.AddComponent<BoxCollider>(); collider.size = size; collider.center = Vector3.up * size.y * .5f;
        }
        static HorrorSecurityMonitor SecurityMonitor(HorrorMatch match)
        {
            var root = new GameObject("Live fuel-court security monitor"); root.transform.position = new Vector3(77, 2.4f, 34); root.transform.rotation = Quaternion.Euler(0, 90, 0);
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad); screen.name = "Live camera glass"; screen.transform.SetParent(root.transform, false); screen.transform.localScale = new Vector3(2.4f, 1.35f, 1); UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());
            string path = "Assets/" + currentFolder + "/Materials/Security monitor glass.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.color = Color.white; AssetDatabase.CreateAsset(material, path); } screen.GetComponent<Renderer>().sharedMaterial = material;
            var frame = SportsEventAuthoring.Box("Security monitor casing", new Vector3(0, 0, .1f), new Vector3(2.6f, 1.55f, .2f), dark, null, root.transform);
            var camera = new GameObject("Fuel-court security camera").AddComponent<Camera>(); camera.transform.position = new Vector3(0, 14, 47); camera.transform.LookAt(new Vector3(0, 1, -10)); camera.fieldOfView = 72; camera.nearClipPlane = .2f; camera.farClipPlane = 220; camera.cullingMask = ~(1 << 5); camera.enabled = false;
            var monitor = root.AddComponent<HorrorSecurityMonitor>(); monitor.match = match; monitor.securityCamera = camera; monitor.display = screen.GetComponent<Renderer>(); return monitor;
        }
        static void StripVehicle(GameObject g)
        { foreach (var b in g.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(b); foreach (var j in g.GetComponentsInChildren<Joint>(true)) UnityEngine.Object.DestroyImmediate(j); foreach (var r in g.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(r); foreach (var c in g.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(c); foreach (var a in g.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(a); }
        static void NightLighting()
        {
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { l.color = new Color(.34f, .48f, .7f); l.intensity = .35f; }
            RenderSettings.ambientLight = new Color(.035f, .05f, .075f); RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .008f; RenderSettings.fogColor = new Color(.018f, .027f, .043f);
            if (RenderSettings.skybox != null) { RenderSettings.skybox.SetColor("_SkyTint", new Color(.015f, .025f, .06f)); RenderSettings.skybox.SetFloat("_Exposure", .25f); }
        }
        static void BakeNavigation(string folder, float size)
        {
            var surface = new GameObject("Baked physical venue navigation").AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.All; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.layerMask = 1;
            var sources = new List<NavMeshBuildSource>(); var bounds = new Bounds(Vector3.up * 15, new Vector3(size, 60, size)); NavMeshBuilder.CollectSources(bounds, 1, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            var settings = NavMesh.GetSettingsByID(0); settings.agentRadius = 2; settings.agentHeight = 3; settings.overrideVoxelSize = true; settings.voxelSize = .5f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity); if (data == null) throw new InvalidOperationException("Horror venue navigation failed");
            string path = "Assets/" + folder + "/Models/Physical venue navigation.asset"; var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing == null) AssetDatabase.CreateAsset(data, path); else { UnityEngine.Object.DestroyImmediate(data); data = existing; } surface.navMeshData = data; surface.AddData();
        }
        static Mesh CornMesh(string folder)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int x = -13; x <= 13; x += 2) for (int z = -11; z <= 11; z += 2)
            {
                float h = 4.5f + Mathf.PerlinNoise(x + 20, z + 20);
                Quad(v, t, new Vector3(x - .05f, 0, z), new Vector3(x + .05f, 0, z), new Vector3(x + .05f, h, z), new Vector3(x - .05f, h, z));
                for (int leaf = 0; leaf < 5; leaf++) { float y = .7f + leaf * .7f; float side = leaf % 2 == 0 ? 1 : -1; Quad(v, t, new Vector3(x, y, z), new Vector3(x + side * 1.4f, y + .25f, z - .25f), new Vector3(x + side * 1.9f, y - .2f, z), new Vector3(x + side * .25f, y + .2f, z + .15f)); }
            }
            return MeshAsset(folder, "Dense authored maize stalks", v, t);
        }
        static Mesh ScarecrowMesh(string folder)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Quad(v, t, new Vector3(-1.8f, .5f, 0), new Vector3(1.8f, .5f, 0), new Vector3(.8f, 4, 0), new Vector3(-.8f, 4, 0));
            Quad(v, t, new Vector3(-3.5f, 3, -.2f), new Vector3(3.5f, 3, -.2f), new Vector3(3.5f, 3.8f, .2f), new Vector3(-3.5f, 3.8f, .2f));
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4, b = (i + 1) * Mathf.PI / 4; int n = v.Count; v.Add(new Vector3(0, 4.7f, -.5f)); v.Add(new Vector3(Mathf.Cos(a) * .75f, 4.7f + Mathf.Sin(a), 0)); v.Add(new Vector3(Mathf.Cos(b) * .75f, 4.7f + Mathf.Sin(b), 0)); t.Add(n); t.Add(n + 1); t.Add(n + 2); }
            return MeshAsset(folder, "Harvest watcher tailored cloth silhouette", v, t);
        }
        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { int n = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3, n + 2, n + 1, n, n + 3, n + 2, n }); }
        static Mesh MeshAsset(string folder, string name, List<Vector3> v, List<int> t)
        { string path = "Assets/" + folder + "/Models/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing; var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh; }
    }
}
