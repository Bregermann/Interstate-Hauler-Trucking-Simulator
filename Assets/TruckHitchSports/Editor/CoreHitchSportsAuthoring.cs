using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckBowling;

namespace LWS.TruckHitchSports.Editor
{
    public static class CoreHitchSportsAuthoring
    {
        const string Folder = "TruckHitchSports";
        public static readonly string[] SceneNames = { "Trailerblazers", "JackknifeHighwire", "HitchAndRun", "TrailerTrebuchet", "HitchHandoff", "Unhitched" };
        public static string Scene(string name) => "Assets/TruckHitchSports/Scenes/" + name + ".unity";
        const float BoosterSpawnClearance = 1.4f;
        [MenuItem("Truck Sports/Alpha 0.10/Repair active Rocket booster clearance")]
        public static void RepairActiveRocketBoosterClearance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Scene("Trailerblazers")) throw new InvalidOperationException("Open the existing Alpha 0.10 Trailerblazers scene explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TrailerblazersMatch>(true)).Single();
            var apron = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true)).Single(c => c.name == "Industrial competition apron");
            int changed = 0;
            foreach (var trailer in match.trailers)
                foreach (var casing in trailer.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Mounted physical booster casing ", StringComparison.Ordinal)))
                { FitBoosterClearance(casing, apron.bounds.max.y); changed++; }
            if (changed != 16) throw new InvalidOperationException("Expected the existing sixteen owned booster casings; repaired " + changed + ".");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HH10_ROCKET_CLEARANCE_REPAIRED " + changed + " casings; compound minimum clearance=" + BoosterSpawnClearance + "m; native vehicle and hitch poses preserved.");
        }
        static void FitBoosterClearance(Transform casing, float floorY)
        {
            // Unity's cylinder is two units tall: 1.75 scale yields the intended
            // 3.5m casing, rather than the old 7m ground-penetrating collider.
            var scale = casing.localScale; scale.y = 1.75f; casing.localScale = scale;
            var nozzle = casing.Find("Booster thrust nozzle");
            if (nozzle != null) nozzle.localPosition = new Vector3(0, -1.04f, 0);
            Physics.SyncTransforms();
            float lowest = casing.GetComponentsInChildren<Collider>(true).Min(c => c.bounds.min.y);
            casing.position += Vector3.up * Mathf.Max(0, floorY + BoosterSpawnClearance - lowest);
            Physics.SyncTransforms();
            EditorUtility.SetDirty(casing); if (nozzle != null) EditorUtility.SetDirty(nozzle);
        }
        [MenuItem("Truck Sports/Alpha 0.10/Create missing core hitch venues")]
        public static void CreateAll()
        {
            Create<TrailerblazersMatch>(SceneNames[0]); Create<HighwireMatch>(SceneNames[1]); Create<HitchRunMatch>(SceneNames[2]);
            Create<TrebuchetMatch>(SceneNames[3]); Create<HandoffMatch>(SceneNames[4]); Create<UnhitchedMatch>(SceneNames[5]);
        }
        static void Create<T>(string key) where T : HitchCompetitionSession
        {
            if (File.Exists(Scene(key))) { Debug.Log("Preserving authored hitch venue " + key); return; }
            var match = SportsEventAuthoring.Begin<T>(Folder); match.setup = new BowlingSetup(); match.setup.count = 4; match.option = 0; match.NormalizeSetup();
            for (int i = 1; i < 4; i++) match.setup.players[i].source = BowlingSource.AI;
            var asphalt = SportsEventAuthoring.Mat(Folder, "Textured haul asphalt", new Color(.24f, .28f, .29f), .12f);
            var steel = SportsEventAuthoring.Mat(Folder, "Safety orange painted steel", new Color(.86f, .29f, .035f), .42f);
            var wood = SportsEventAuthoring.Mat(Folder, "Siege timber", new Color(.29f, .16f, .075f), .08f);
            var physical = SportsEventAuthoring.Contact(Folder, "Hauling venue contact", .65f, .05f);
            var terrain = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdMudRocky.terrainlayer");
            if (terrain != null) { asphalt.mainTexture = terrain.diffuseTexture; asphalt.mainTextureScale = Vector2.one * 30; }
            match.trucks = new SportsEventTruck[4]; match.couplers = new HitchCoupler[4];
            bool highwire = match is HighwireMatch, relay = match is HandoffMatch, surf = match is UnhitchedMatch;
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = new Vector3(highwire ? i * 24 : (i - 1.5f) * 55, highwire ? 61 : 1.2f, -25);
                if (relay) position = new Vector3((i % 2) * 55 - 27.5f, 1.2f, i < 2 ? -25 : 135);
                match.trucks[i] = HitchAuthoring.CreateTractor(match, i, position); match.couplers[i] = match.trucks[i].GetComponent<HitchCoupler>();
            }
            int count = match is HitchRunMatch ? 8 : relay ? 2 : 4;
            match.trailers = new TrailerBody[count];
            for (int i = 0; i < count; i++)
            {
                var position = i < 4 ? match.trucks[i].body.position : new Vector3((i - 5.5f) * 30, 1.2f, 35);
                match.trailers[i] = surf ? HitchAuthoring.CreateFlatbed(position, Quaternion.identity) : HitchAuthoring.CreateTrailer(position, Quaternion.identity);
                if (!(match is HitchRunMatch) && !surf) HitchAuthoring.PlaceCoupledAtStart(match.couplers[i], match.trailers[i]);
                else if (match is HitchRunMatch) match.trailers[i].transform.position = new Vector3((i % 4 - 1.5f) * 38, 1.2f, i < 4 ? 5 : 50);
            }
            if (highwire) Highwire((HighwireMatch)(object)match, asphalt, physical, steel);
            else
            {
                SportsEventAuthoring.Box("Industrial competition apron", new Vector3(0, -1, 120), new Vector3(350, 2, 700), asphalt, physical);
                if (match is TrailerblazersMatch rocket) Rocket(rocket, steel, physical);
                if (match is HitchRunMatch theft) Depots(theft, steel, physical);
                if (match is TrebuchetMatch siege) Trebuchets(siege, wood, steel, physical);
                if (match is HandoffMatch handoff) Relay(handoff, steel, physical);
                if (match is UnhitchedMatch unhitched) Surf(unhitched, steel, physical);
            }
            Scenery(match, highwire ? -1 : 0);
            match.views = SportsRallyAuthoring.Views(match, match.trucks); match.broadcast = match.views.broadcast;
            match.views.groundOffset = new Vector3(15, 12, -24); match.views.groundLookAhead = 5;
            match.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            match.presentationWeather = SportsChaosAuthoring.Weather(match, Folder); match.views.weather = match.presentationWeather;
            SportsWeatherRenderingAuthoring.Install(match, Folder);
            SportsEventAuthoring.UI(match);
            SportsScenicGradeAuthoring.Install(match.gameObject, match.cameras, Folder, -.2f);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Scene(key));
            if (match is TrebuchetMatch) CoreHitchSetupRepairs.RepairActiveTrebuchetApparatus();
            SportsProductionAuthoring.InstallScene(); SportsFocusPolicy.InstallScene();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Scene(key)); AssetDatabase.SaveAssets();
            Debug.Log("ALPHA10_HITCH_AUTHORED " + key + " native fifth wheels, axles, trailer rigidbodies and assigned controls; gameplay acceptance pending.");
        }
        static void Highwire(HighwireMatch match, Material road, PhysicsMaterial contact, Material steel)
        {
            match.route = new[] { new Vector3(0, 60, 30), new Vector3(13, 60, 90), new Vector3(-6, 60, 160), new Vector3(12, 60, 220), new Vector3(0, 60, 285) };
            match.fallHeight = 20;
            for (int lane = 0; lane < 4; lane++)
            {
                Vector3 last = new Vector3(lane * 24, 60, -45);
                foreach (Vector3 point in match.route)
                {
                    Vector3 next = point + Vector3.right * lane * 24, direction = next - last;
                    var deck = SportsEventAuthoring.Box("Suspended narrow physical roadway", (last + next) * .5f - Vector3.up * .6f, new Vector3(5.5f, 1.2f, direction.magnitude + 1), road, contact);
                    deck.transform.rotation = Quaternion.LookRotation(direction);
                    last = next;
                }
                SportsEventAuthoring.Box("Finish receiving platform", last + Vector3.forward * 10 - Vector3.up * .5f, new Vector3(12, 1, 50), steel, contact);
            }
            SportsEventAuthoring.Box("Distant ravine terrain", new Vector3(40, -15, 100), new Vector3(450, 10, 600), road, contact);
            SportsRallyAuthoring.Prop(null, "Assets/WeatherMaker/Demo/DemoResources/Models/Terrain/MountainPeaks/Mountains.prefab", new Vector3(45, -80, 150), new Vector3(1000, 270, 1100));
        }
        static void Rocket(TrailerblazersMatch match, Material steel, PhysicsMaterial contact)
        {
            match.landingPads = new Transform[4]; match.exhaust = new ParticleSystem[4];
            for (int i = 0; i < 4; i++)
            {
                float x = match.trucks[i].body.position.x;
                match.landingPads[i] = SportsEventAuthoring.Box("Precision recovery pad " + (i + 1), new Vector3(x, .12f, 130), new Vector3(24, .25f, 24), steel, contact).transform;
                var boosterBounds = HitchAuthoring.Bounds(match.trailers[i].gameObject);
                for (int booster = 0; booster < 4; booster++)
                {
                    var rocket = GameObject.CreatePrimitive(PrimitiveType.Cylinder); rocket.name = "Mounted physical booster casing " + (booster + 1);
                    rocket.transform.SetParent(match.trailers[i].transform, false);
                    rocket.transform.localPosition = new Vector3((booster % 2 == 0 ? -1 : 1) * (boosterBounds.extents.x + .7f), boosterBounds.center.y, boosterBounds.center.z + (booster < 2 ? -3 : 3));
                    rocket.transform.localScale = new Vector3(1.1f, 3.5f, 1.1f); rocket.GetComponent<Renderer>().sharedMaterial = steel; rocket.layer = 2;
                    var nozzle = SportsEventAuthoring.Box("Booster thrust nozzle", new Vector3(0, -.54f, 0), new Vector3(1.05f, .12f, 1.05f), steel, contact, rocket.transform); nozzle.layer = 2;
                    FitBoosterClearance(rocket.transform, 0);
                }
                var exhaust = new GameObject("Rocket booster exhaust " + (i + 1)); exhaust.transform.SetParent(match.trailers[i].transform, false); exhaust.transform.localPosition = new Vector3(0, -.8f, 0); exhaust.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var particles = exhaust.AddComponent<ParticleSystem>(); var main = particles.main; main.playOnAwake = false; main.startLifetime = .8f; main.startSpeed = 20; main.startSize = 1.1f; main.startColor = new Color(1, .3f, .025f); var emission = particles.emission; emission.rateOverTime = 80; match.exhaust[i] = particles;
                SportsRallyAuthoring.Prop(null, "Assets/ScaffoldsPack/URP/Prefabs/SM_StairTower.prefab", new Vector3(x + 13, 0, -20), new Vector3(7, 35, 7));
            }
        }
        static void Depots(HitchRunMatch match, Material steel, PhysicsMaterial contact)
        {
            match.depots = new Transform[4];
            for (int i = 0; i < 4; i++) match.depots[i] = SportsEventAuthoring.Box("Delivery depot " + (i + 1), new Vector3((i - 1.5f) * 55, .05f, 145), new Vector3(25, .1f, 35), steel).transform;
        }
        static void Relay(HandoffMatch match, Material steel, PhysicsMaterial contact)
        {
            match.transferZones = new Transform[2]; match.finishes = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float x = i * 55 - 27.5f;
                match.transferZones[i] = SportsEventAuthoring.Box("Physical relay transfer zone " + i, new Vector3(x, .1f, 105), new Vector3(24, .2f, 36), steel).transform;
                match.finishes[i] = SportsEventAuthoring.Box("Relay finish depot " + i, new Vector3(x, .1f, 280), new Vector3(28, .2f, 35), steel).transform;
                var barrier = SportsEventAuthoring.Box("Obstacle relay offset chicane", new Vector3(x, 1.5f, 50), new Vector3(10, 3, 4), steel, contact, match.transform);
                barrier.AddComponent<HitchModeObstacle>().ModeMask = 1 << 1;
            }
        }
        static void Trebuchets(TrebuchetMatch match, Material wood, Material steel, PhysicsMaterial contact)
        {
            match.engines = new TrebuchetArm[4]; match.targets = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float x = match.trucks[i].body.position.x;
                var support = SportsEventAuthoring.Box("Trebuchet pivot tower", new Vector3(x + 7, 4, 15), new Vector3(2, 8, 4), wood, contact);
                var armObject = SportsEventAuthoring.Box("Physical siege arm", new Vector3(x, 9, 15), new Vector3(1.5f, 1.3f, 26), wood, contact);
                armObject.transform.rotation = Quaternion.Euler(35, 0, 0);
                var body = armObject.AddComponent<Rigidbody>(); body.mass = 8000; body.solverIterations = 24;
                var pivot = armObject.AddComponent<HingeJoint>(); pivot.axis = Vector3.right; pivot.autoConfigureConnectedAnchor = false; pivot.connectedAnchor = armObject.transform.position;
                var spring = pivot.spring; spring.spring = 10000000; spring.damper = 1000000; spring.targetPosition = 0; pivot.spring = spring; pivot.useSpring = true;
                var counterweight = SportsEventAuthoring.Box("Physical trebuchet counterweight", new Vector3(0, 0, -9), new Vector3(4, 4, 4), steel, contact, armObject.transform);
                // Compound counterweight contributes actual inertia and center of mass.
                body.mass = 45000; body.centerOfMass = new Vector3(0, 0, -7.5f);
                var cradle = SportsEventAuthoring.Box("Trailer loading cradle", new Vector3(0, 1.1f, 9), new Vector3(7, .8f, 16), wood, contact, armObject.transform);
                var loading = new GameObject("Physical loading volume"); loading.transform.SetParent(cradle.transform, false); loading.transform.localScale = new Vector3(1 / 7f, 1 / .8f, 1 / 16f);
                var volume = loading.AddComponent<BoxCollider>(); volume.isTrigger = true; volume.center = new Vector3(0, 3, 0); volume.size = new Vector3(8, 8, 18);
                var engine = armObject.AddComponent<TrebuchetArm>(); engine.Arm = body; engine.Pivot = pivot; engine.LoadingCradle = volume; match.engines[i] = engine;
                var approach = SportsEventAuthoring.Box("Physical siege loading ramp", new Vector3(x, 1.4f, 3), new Vector3(8, .6f, 20), wood, contact);
                approach.transform.rotation = Quaternion.Euler(-14, 0, 0);
                match.targets[i] = SportsEventAuthoring.Box("Siege target zone", new Vector3(x, .15f, 210), new Vector3(25, .3f, 25), steel).transform;
                for (int row = 0; row < 3; row++) for (int column = 0; column < 4; column++)
                {
                    var block = SportsEventAuthoring.Box("Physical breakable siege target", new Vector3(x + column * 4 - 6, 2 + row * 4, 215), new Vector3(3.8f, 3.8f, 3.8f), wood, contact);
                    var brick = block.AddComponent<Rigidbody>(); brick.mass = 900; brick.solverIterations = 12;
                    block.transform.SetParent(match.transform, true); var target = block.AddComponent<SiegeTarget>(); target.Match = match; target.Lane = i;
                }
            }
        }
        static void Surf(UnhitchedMatch match, Material steel, PhysicsMaterial contact)
        {
            match.deckSurfaces = new Collider[4];
            for (int i = 0; i < 4; i++)
            {
                var trailer = match.trailers[i]; var bounds = HitchAuthoring.Bounds(trailer.gameObject);
                foreach (var collider in trailer.GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = 0;
                var deck = SportsEventAuthoring.Box("Load-bearing surf deck", new Vector3(0, bounds.max.y + .15f, bounds.center.z), new Vector3(5.5f, .3f, bounds.size.z), steel, contact, trailer.transform);
                deck.layer = 0; match.deckSurfaces[i] = deck.GetComponent<Collider>();
                var tractorBounds = HitchAuthoring.Bounds(match.trucks[i].gameObject);
                match.trucks[i].transform.position = trailer.transform.TransformPoint(new Vector3(0, bounds.max.y + .55f - tractorBounds.min.y, bounds.center.z));
                for (int ramp = 0; ramp < 3; ramp++)
                {
                    var jump = SportsEventAuthoring.Box("Trailer surf launch ramp", new Vector3(trailer.transform.position.x, 1.5f, 65 + ramp * 85), new Vector3(18, 1, 24), steel, contact);
                    jump.transform.rotation = Quaternion.Euler(-8, 0, 0);
                }
            }
        }
        static void Scenery(HitchCompetitionSession match, int highwire)
        {
            for (int i = 0; i < 12; i++)
            {
                float x = i % 2 == 0 ? -150 : 150, z = i / 2 * 70 - 40;
                SportsRallyAuthoring.Prop(null, "Assets/ShipContainerPack/URP/Prefabs/ShipContainer" + (i % 3 == 0 ? "Red" : i % 3 == 1 ? "Blue" : "Gray") + "_URP.prefab", new Vector3(x, highwire < 0 ? -10 : 0, z), new Vector3(10, 5, 22));
                if (highwire >= 0) SportsRallyAuthoring.Prop(null, "Assets/RoadPropsPack/URP/Prefabs/TrafficCone_URP.prefab", new Vector3(x * .8f, 0, z), new Vector3(1, 2, 1));
            }
        }
    }
}
