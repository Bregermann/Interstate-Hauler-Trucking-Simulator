using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFareFight.Editor;

namespace LWS.TruckRedLightGreenLight.Editor
{
    public static class RedLightAuthoring
    {
        public const string Folder = "TruckRedLightGreenLight";
        public const string Scene = "Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity";
        // Additive saved-scene upgrade, safe to repeat without rebuilding geometry.
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(Scene);
            var m = UnityEngine.Object.FindAnyObjectByType<RedLightMatch>();
            if (m.GetComponent<SportsExpansionViewHud>() == null) { var hud = m.gameObject.AddComponent<SportsExpansionViewHud>(); hud.match = m; hud.views = m.drivingViews; }
            if (m.GetComponent<SportsExpansionSpectators>() == null) { var spectator = m.gameObject.AddComponent<SportsExpansionSpectators>(); spectator.match = m; spectator.views = m.drivingViews; }
            m.broadcast.view.orthographic = true; m.framing.bottomInset = .29f;
            m.signalCaption.transform.rotation = Quaternion.identity;
            if (m.crowd == null)
            {
                var source = EditorSceneManager.OpenScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene, OpenSceneMode.Additive);
                LWS.TruckFieldGoal.FieldGoalCrowd.Batch[] originals;
                try { originals = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LWS.TruckFieldGoal.FieldGoalCrowd>(true)).First().batches; }
                finally { EditorSceneManager.CloseScene(source, true); UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene); }
                var audience = new GameObject("Existing stadium crowd reactions").AddComponent<LWS.TruckFieldGoal.FieldGoalCrowd>(); m.crowd = audience; audience.eventSession = m; audience.nearFans = Array.Empty<Animator>(); audience.runnerChance = 0;
                audience.response = audience.gameObject.AddComponent<AudioSource>(); audience.response.playOnAwake = false; audience.response.spatialBlend = .5f; audience.cheer = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav"); audience.boo = audience.cheer;
                var fans = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.name == "Reused posed stadium spectator").ToArray();
                audience.batches = originals.Select(original => new LWS.TruckFieldGoal.FieldGoalCrowd.Batch { idle = original.idle, cheer = original.cheer, materials = original.materials, fans = fans.Where(f => f.sharedMesh == original.idle).ToArray(), seats = fans.Where(f => f.sharedMesh == original.idle).Select(f => f.transform.localToWorldMatrix).ToArray() }).ToArray();
            }
            if (!m.menus.pages[6].GetComponentsInChildren<SportsMenuAction>(true).Any(a => a.action == "watchredlight"))
                SportsUIAuthoring.Button(m.menus.pages[6].transform, "WATCH RECORDED FINISH", "watchredlight", new Vector2(0, -245), new Vector2(520, 48));
            var logo = SportsRetroMenuAuthoring.LoadLogo(Folder);
            foreach (int index in new[] { 0, 4 })
            {
                var page = m.menus.pages[index];
                var art = page.transform.Find("Original sport title artwork")?.gameObject ?? SportsUIAuthoring.Rect("Original sport title artwork", page.transform, new Vector2(260, 160), new Vector2(-560, 365));
                var image = art.GetComponent<UnityEngine.UI.Image>() ?? art.AddComponent<UnityEngine.UI.Image>();
                image.sprite = logo; image.preserveAspect = true; image.raycastTarget = false;
                var title = page.transform.Find("Title")?.GetComponent<TMP_Text>();
                if (title != null) { title.rectTransform.anchoredPosition = new Vector2(140, title.rectTransform.anchoredPosition.y); title.rectTransform.sizeDelta = new Vector2(1000, title.rectTransform.sizeDelta.y); }
            }
            SportsBroadcastAuthoring.ApplyScene();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Preserve the existing scene. Use a targeted upgrade instead of rebuilding it.");
            EditorSceneManager.OpenScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene);
            var crowd = UnityEngine.Object.FindAnyObjectByType<LWS.TruckFieldGoal.FieldGoalCrowd>();
            if (crowd == null || crowd.batches.Length == 0) throw new InvalidOperationException("Existing optimized stadium crowd assets required.");
            var crowdMeshes = crowd.batches.Select(b => b.idle).ToArray();
            var crowdMaterials = crowd.batches.Select(b => b.materials).ToArray();
            var m = SportsEventAuthoring.Begin<RedLightMatch>(Folder);
            m.setup.count = 12; m.NormalizeSetup();
            m.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            var ground = SportsEventAuthoring.Mat(Folder, "Festival stadium textured runway", new Color(.16f, .22f, .24f), .12f);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdMudRocky.terrainlayer");
            ground.mainTexture = layer.diffuseTexture; ground.mainTextureScale = new Vector2(12, 40);
            var contact = SportsEventAuthoring.Contact(Folder, "NWH competition tarmac", .8f, .02f);
            SportsEventAuthoring.Box("Continuous competition runway", new Vector3(0, -.25f, 150), new Vector3(100, .5f, 370), ground, contact);
            var grass = SportsEventAuthoring.Mat(Folder, "Stadium park grass", new Color(.22f, .36f, .18f), .08f);
            grass.mainTexture = ground.mainTexture; grass.mainTextureScale = new Vector2(35, 65);
            SportsEventAuthoring.Box("Substantial surrounding park", new Vector3(0, -.6f, 150), new Vector3(450, .6f, 650), grass, contact);
            var cream = SportsEventAuthoring.Mat(Folder, "Track markings ivory", new Color(1, .93f, .72f), .15f);
            var dark = SportsEventAuthoring.Mat(Folder, "Checkered finish charcoal", new Color(.025f, .035f, .05f), .15f);
            for (int x = 0; x < 30; x++) for (int z = 0; z < 2; z++)
                SportsEventAuthoring.Box("Physical finish paint", new Vector3(-43.5f + x * 3, .012f, m.finishZ + z * 3), new Vector3(3, .018f, 3), (x + z) % 2 == 0 ? cream : dark);
            for (int lane = 0; lane < 6; lane++)
                SportsEventAuthoring.Box("Starting grid stripe", new Vector3((lane - 2.5f) * 11, .015f, -4), new Vector3(8, .02f, .24f), cream);
            var dress = new GameObject("Owned modeled stadium perimeter").transform;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 36; i++)
                {
                    var barrier = new GameObject("Installed safety barrier").transform; barrier.SetParent(dress);
                    barrier.SetPositionAndRotation(new Vector3(side * 49, 0, -20 + i * 10), Quaternion.Euler(0, 90, 0));
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(barrier, "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab", new Vector3(10, 1.4f, .7f), 0, false, Folder);
                }
                var wall = new GameObject("Continuous visible barrier collider").AddComponent<BoxCollider>();
                wall.transform.position = new Vector3(side * 49, .6f, 150); wall.size = new Vector3(.8f, 1.2f, 370); wall.sharedMaterial = contact;
                var blue = SportsEventAuthoring.Mat(Folder, "Festival grandstand navy", new Color(.045f, .15f, .26f), .22f);
                for (int row = 0; row < 5; row++)
                {
                    SportsEventAuthoring.Box("Tiered event grandstand", new Vector3(side * (58 + row * 3), row * 1.2f, 150), new Vector3(3, 1.2f, 310), blue, contact, dress);
                    for (int seat = 0; seat < 55; seat++)
                    {
                        int variant = (seat + row * 3) % crowdMeshes.Length;
                        var fan = new GameObject("Reused posed stadium spectator"); fan.transform.SetParent(dress);
                        fan.transform.SetPositionAndRotation(new Vector3(side * (58 + row * 3), row * 1.2f + .65f, seat * 5.5f), Quaternion.Euler(0, -side * 90, 0));
                        fan.transform.localScale = Vector3.one * 1.25f;
                        fan.AddComponent<MeshFilter>().sharedMesh = crowdMeshes[variant];
                        var renderer = fan.AddComponent<MeshRenderer>(); renderer.sharedMaterials = crowdMaterials[variant]; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
                for (int i = 0; i < 8; i++)
                    SportsRallyAuthoring.Prop(dress, "Assets/Toon Gas Station/Prefabs/Streetlight_1C.prefab", new Vector3(side * 52, 0, i * 43), new Vector3(5, 15, 5));
                for (int i = 0; i < 14; i++) LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(dress, new Vector3(side * (100 + i % 3 * 12), 0, -20 + i * 29), 12, true, Folder);
            }
            m.trucks = new SportsEventTruck[12]; m.fragments = new SportsFragmentRig[12]; m.explosions = new ParticleSystem[12];
            for (int i = 0; i < 12; i++)
            {
                var t = FareFightVehicleAuthoring.Spawn(m, i, m.Grid(i), Quaternion.identity); t.name = "Full NWH race tractor " + (i + 1); m.trucks[i] = t;
                var c = t.gameObject.AddComponent<RedLightContact>(); c.match = m; c.slot = i;
                m.fragments[i] = SportsFragmentAuthoring.InstallRigid(t.body, t.visuals.GetComponentsInChildren<MeshFilter>(true), Folder);
                var blast = new GameObject("Pooled enormous violation fireball " + i).AddComponent<ParticleSystem>();
                blast.transform.SetParent(m.transform); SportsChaosAuthoring.FixBlast(blast, SportsChaosAuthoring.Flare(Folder));
                var main = blast.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.5f); main.startSpeed = new ParticleSystem.MinMaxCurve(12, 25); main.startSize = new ParticleSystem.MinMaxCurve(6, 15); main.maxParticles = 160; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = blast.emission; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 120) }); m.explosions[i] = blast;
            }
            Master(m, dress, cream, dark);
            m.broadcast = SportsEventAuthoring.Camera("Full stadium race broadcast"); m.cameras = new[] { m.broadcast.view };
            m.framing = m.broadcast.gameObject.AddComponent<SportsCameraFit>(); m.framing.cameraTarget = m.broadcast.view; m.framing.arenaFit = true; m.framing.topInset = .14f; m.framing.bottomInset = .08f;
            SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m);
            m.drivingViews = SportsDrivingViewsAuthoring.Install(m, m.trucks, m.broadcast, Folder);
            m.drivingViews.independentViewModes = true;
            m.atmosphere = SportsChaosAuthoring.Weather(m, Folder); m.atmosphere.presets = SportsWeatherPresetsAuthoring.EnsureCatalog(); m.drivingViews.rally.weather = m.atmosphere;
            SportsWeatherPickerAuthoring.Install(m, m.atmosphere.presets, new Vector2(350, -180));
            SportsScenicGradeAuthoring.Install(m.gameObject, new[] { m.broadcast.view }.Concat(m.drivingViews.rally.views.Select(v => v.view)).ToArray(), Folder, -.1f);
            SportsEventAuthoring.Save(Folder);
            Directory.CreateDirectory("Logs/TruckSports/EighteenSports"); File.WriteAllText("Logs/TruckSports/EighteenSports/red-light-authoring.txt", "AUTHORED full NWH twelve-truck stadium; runtime acceptance pending.");
        }

        static void Master(RedLightMatch m, Transform root, Material cream, Material dark)
        {
            m.master = new GameObject("Giant ridiculous game master").transform; m.master.SetParent(root); m.master.position = new Vector3(0, 1, m.finishZ + 31); m.master.rotation = Quaternion.Euler(0, 180, 0);
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(m.master, "Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Man_11.prefab", new Vector3(9, 27, 8), 0, false, Folder);
            m.masterAnimator = m.master.GetComponentInChildren<Animator>(true); m.masterAnimator.enabled = true; m.masterAnimator.applyRootMotion = false;
            string path = "Assets/" + Folder + "/Materials/Giant master reactions.controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var pair in new[] { ("Watch", "talk1"), ("Laugh", "cheer") })
            { var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/UTS_FullPack/Models/People/Animations/man/" + pair.Item2 + ".FBX").OfType<AnimationClip>().First(c => !c.name.StartsWith("__")); controller.AddMotion(clip).name = pair.Item1; }
            m.masterAnimator.runtimeAnimatorController = controller;
            m.laugh = m.gameObject.AddComponent<AudioSource>(); m.laugh.playOnAwake = false; m.laugh.spatialBlend = 0; m.laugh.pitch = .7f;
            m.laughs = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckPoker/Audio/Dealer giggle " + i + ".wav")).ToArray();
            var lightMat = SportsEventAuthoring.Mat(Folder, "Emissive traffic signal", Color.green, .4f); lightMat.EnableKeyword("_EMISSION"); lightMat.SetColor("_EmissionColor", Color.green * 4);
            m.signals = new Renderer[2];
            for (int i = 0; i < 2; i++)
            {
                var fixture = new GameObject("Owned giant traffic light fixture").transform; fixture.SetParent(root); fixture.position = new Vector3(i == 0 ? -35 : 35, 0, m.finishZ + 12);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(fixture, "Assets/UTS_FullPack/Models/Traffic light/Prefabs/Traffic Light_1.prefab", new Vector3(7, 18, 3), 0, false, Folder);
                var lens = GameObject.CreatePrimitive(PrimitiveType.Sphere); lens.name = "Authoritative red or green signal lens"; lens.transform.SetParent(root); lens.transform.position = fixture.position + new Vector3(0, 15, -2); lens.transform.localScale = new Vector3(4, 4, .5f);
                UnityEngine.Object.DestroyImmediate(lens.GetComponent<Collider>()); m.signals[i] = lens.GetComponent<Renderer>(); m.signals[i].sharedMaterial = lightMat;
            }
            var board = SportsEventAuthoring.Box("Giant stadium signal display", new Vector3(0, 7, m.finishZ + 10), new Vector3(30, 5, .4f), dark);
            var text = new GameObject("Actual signal caption").AddComponent<TextMeshPro>(); text.transform.position = board.transform.position + new Vector3(0, 0, -.3f); text.transform.rotation = Quaternion.Euler(0, 180, 0); text.rectTransform.sizeDelta = new Vector2(29, 4.5f); text.alignment = TextAlignmentOptions.Center; text.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; text.fontSize = 15; text.color = Color.white; text.text = "GET READY"; m.signalCaption = text;
        }
    }
}
