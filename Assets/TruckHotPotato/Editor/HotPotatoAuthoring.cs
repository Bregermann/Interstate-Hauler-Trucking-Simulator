using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFareFight.Editor;
using LWS.TruckPedestrianPlowing;

namespace LWS.TruckHotPotato.Editor
{
    public static class HotPotatoAuthoring
    {
        public const string Folder = "TruckHotPotato", Scene = "Assets/TruckHotPotato/Scenes/TruckHotPotato.unity";
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<HotPotatoMatch>();
            if (match.GetComponent<SportsExpansionViewHud>() == null) { var hud = match.gameObject.AddComponent<SportsExpansionViewHud>(); hud.match = match; hud.views = match.drivingViews; }
            if (match.GetComponent<SportsExpansionSpectators>() == null) { var spectator = match.gameObject.AddComponent<SportsExpansionSpectators>(); spectator.match = match; spectator.views = match.drivingViews; }
            if (match.fuseSparks == null)
            {
                var sparks = new GameObject("Pooled urgent fuse sparks").AddComponent<ParticleSystem>(); sparks.transform.SetParent(match.bomb, false); sparks.transform.localPosition = new Vector3(1.65f, 4.6f, 0);
                SportsChaosAuthoring.FixBlast(sparks, SportsChaosAuthoring.Flare(Folder));
                var main = sparks.main; main.loop = true; main.startLifetime = .35f; main.startSpeed = 2; main.startSize = .3f; main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = sparks.emission; emission.SetBursts(Array.Empty<ParticleSystem.Burst>()); emission.rateOverTime = 18; match.fuseSparks = sparks;
            }
            if (scene.GetRootGameObjects().Any(g => g.name == "Original Hot Potato stadium artwork")) { EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); return; }
            var logo = SportsRetroMenuAuthoring.LoadLogo(Folder);
            var material = SportsEventAuthoring.Mat(Folder, "Original bomb festival stadium artwork", Color.white);
            material.shader = Shader.Find("Universal Render Pipeline/Unlit"); material.mainTexture = logo.texture;
            material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10); material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            var art = GameObject.CreatePrimitive(PrimitiveType.Quad); art.name = "Original Hot Potato stadium artwork"; UnityEngine.Object.DestroyImmediate(art.GetComponent<Collider>()); art.transform.SetPositionAndRotation(new Vector3(0, .025f, 0), Quaternion.Euler(90, 0, 0)); art.transform.localScale = new Vector3(30, 20, 1); art.GetComponent<Renderer>().sharedMaterial = material;
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Preserve the authored scene; use a targeted additive upgrade.");
            var m = SportsEventAuthoring.Begin<HotPotatoMatch>(Folder); m.setup.count = 8; m.NormalizeSetup();
            m.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            var dirt = SportsEventAuthoring.Mat(Folder, "Festival scorched clay", new Color(.27f, .16f, .11f), .07f);
            var mud = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKeepaway/Materials/Rocky mud terrain.mat"); dirt.mainTexture = mud.mainTexture; dirt.mainTextureScale = Vector2.one * 24;
            var contact = SportsEventAuthoring.Contact(Folder, "Competition clay contact", .8f, .03f);
            SportsEventAuthoring.Box("Continuous modeled stadium arena base", new Vector3(0, -.5f, 0), new Vector3(210, 1, 210), dirt, contact);
            var steel = SportsEventAuthoring.Mat(Folder, "Eggplant stadium steel", new Color(.10f, .025f, .14f), .25f);
            var safety = SportsEventAuthoring.Contact(Folder, "Physical rounded containment", .25f, .05f);
            for (int k = 0; k < 80; k++)
            {
                float a = k * Mathf.PI * 2 / 80, b = (k + 1) * Mathf.PI * 2 / 80;
                Vector3 p = new Vector3(Mathf.Sin(a) * 66, 1, Mathf.Cos(a) * 66), q = new Vector3(Mathf.Sin(b) * 66, 1, Mathf.Cos(b) * 66);
                var wall = SportsEventAuthoring.Box("Continuous circular crash barrier", (p + q) * .5f, new Vector3(1, 2, Vector3.Distance(p, q) + .05f), steel, safety); wall.transform.rotation = Quaternion.LookRotation(q - p);
                if (k % 4 == 0) SportsRallyAuthoring.Prop(wall.transform.parent, "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab", p - Vector3.up, new Vector3(4, 1.8f, 1));
            }
            foreach (int side in new[] { -1, 1 })
                SportsEasyRoadsAuthoring.Bake(Folder, "Smooth driveable escape ramp " + side, new[] { new Vector3(side * 24, 0, -18), new Vector3(side * 24, .15f, -12), new Vector3(side * 24, 1.6f, -6), new Vector3(side * 24, 2.1f, 0), new Vector3(side * 24, 1.6f, 6), new Vector3(side * 24, .15f, 12), new Vector3(side * 24, 0, 18) }, 11, dirt, contact);
            Stadium(); Breakables(m);
            m.trucks = new SportsEventTruck[8]; m.fragments = new SportsFragmentRig[8]; m.blasts = new ParticleSystem[8];
            for (int i = 0; i < 8; i++)
            {
                var truck = FareFightVehicleAuthoring.Spawn(m, i, m.Grid(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(m.Grid(i), Vector3.up))); m.trucks[i] = truck;
                var collision = truck.gameObject.AddComponent<HotPotatoContact>(); collision.match = m; collision.slot = i;
                m.fragments[i] = SportsFragmentAuthoring.InstallRigid(truck.body, truck.visuals.GetComponentsInChildren<MeshFilter>(true), Folder);
                var blast = new GameObject("Pooled bomb fireball " + i).AddComponent<ParticleSystem>(); SportsChaosAuthoring.FixBlast(blast, SportsChaosAuthoring.Flare(Folder));
                var main = blast.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.5f); main.startSize = new ParticleSystem.MinMaxCurve(5, 11); main.startSpeed = new ParticleSystem.MinMaxCurve(8, 18); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 120; m.blasts[i] = blast;
            }
            Bomb(m);
            m.broadcast = SportsEventAuthoring.Camera("Hot Potato full stadium broadcast"); m.broadcast.view.orthographic = true;
            m.framing = m.broadcast.gameObject.AddComponent<SportsCameraFit>(); m.framing.cameraTarget = m.broadcast.view; m.framing.arenaFit = true; m.framing.topInset = .1f; m.framing.bottomInset = .25f; m.cameras = new[] { m.broadcast.view };
            SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m);
            m.drivingViews = SportsDrivingViewsAuthoring.Install(m, m.trucks, m.broadcast, Folder); m.drivingViews.independentViewModes = true;
            m.atmosphere = SportsChaosAuthoring.Weather(m, Folder); m.atmosphere.presets = SportsWeatherPresetsAuthoring.EnsureCatalog(); m.drivingViews.rally.weather = m.atmosphere;
            SportsWeatherPickerAuthoring.Install(m, m.atmosphere.presets, new Vector2(350, -180));
            SportsUIAuthoring.Button(m.menus.pages[6].transform, "WATCH LAST BOMB", "watchpotato", new Vector2(0, -245), new Vector2(520, 48));
            var logo = SportsRetroMenuAuthoring.LoadLogo(Folder);
            foreach (int page in new[] { 0, 4 })
            {
                var art = SportsUIAuthoring.Rect("Original sport title artwork", m.menus.pages[page].transform, new Vector2(260, 160), new Vector2(-560, 365)).AddComponent<UnityEngine.UI.Image>(); art.sprite = logo; art.preserveAspect = true; art.raycastTarget = false;
                var title = m.menus.pages[page].transform.Find("Title").GetComponent<TMP_Text>(); title.rectTransform.anchoredPosition += Vector2.right * 140; title.rectTransform.sizeDelta = new Vector2(1000, title.rectTransform.sizeDelta.y);
            }
            SportsBroadcastAuthoring.ApplyScene(); SportsEventAuthoring.Save(Folder);
        }
        static void Stadium()
        {
            var destination = SceneManager.GetActiveScene(); var source = EditorSceneManager.OpenScene(LWS.TruckDerby.Editor.DerbyAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(destination);
                foreach (var root in source.GetRootGameObjects().Where(g => g.name == "Existing Union Field grandstands and press boxes" || g.name == "Installed character crowd — baked low-cost spectators"))
                { var copy = UnityEngine.Object.Instantiate(root); copy.name = "Reused Hot Potato " + root.name; SceneManager.MoveGameObjectToScene(copy, destination); copy.transform.position *= 1.5f; copy.transform.localScale *= 1.5f; }
            }
            finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(destination); }
        }
        static void Breakables(HotPotatoMatch m)
        {
            var destination = SceneManager.GetActiveScene(); var source = EditorSceneManager.OpenScene(LWS.TruckPedestrianPlowing.Editor.PlowAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(destination);
                var existing = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlowBreakable>(true)).Take(4).ToArray();
                if (existing.Length == 0) throw new InvalidOperationException("Existing shared pedestrian-sport breakable props required.");
                m.breakables = new PlowBreakable[4];
                for (int i = 0; i < 4; i++)
                { var copy = UnityEngine.Object.Instantiate(existing[i % existing.Length].gameObject); copy.name = "Reused Core-Collapse festival breakable " + i; SceneManager.MoveGameObjectToScene(copy, destination); copy.transform.SetPositionAndRotation(new Vector3(i < 2 ? -35 : 35, 0, i % 2 == 0 ? -18 : 18), Quaternion.identity); m.breakables[i] = copy.GetComponent<PlowBreakable>(); }
            }
            finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(destination); }
        }
        static void Bomb(HotPotatoMatch m)
        {
            var root = new GameObject("Single authoritative giant cartoon bomb").transform; m.bomb = root;
            var dark = SportsEventAuthoring.Mat(Folder, "Scraped charcoal bomb shell", new Color(.025f, .02f, .035f), .35f);
            var red = SportsEventAuthoring.Mat(Folder, "Urgent bomb warning lamp", Color.red, .4f); red.EnableKeyword("_EMISSION"); red.SetColor("_EmissionColor", Color.red * 3);
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere); shell.name = "Giant bomb shell"; shell.transform.SetParent(root, false); shell.transform.localScale = Vector3.one * 4.5f; shell.GetComponent<Renderer>().sharedMaterial = dark; UnityEngine.Object.DestroyImmediate(shell.GetComponent<Collider>());
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cap.name = "Flashing fuse cap"; cap.transform.SetParent(root, false); cap.transform.localPosition = Vector3.up * 2.3f; cap.transform.localScale = new Vector3(1, .35f, 1); m.bombGlow = cap.GetComponent<Renderer>(); m.bombGlow.sharedMaterial = red; UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
            var rope = root.gameObject.AddComponent<LineRenderer>(); rope.useWorldSpace = false; rope.positionCount = 12; rope.widthMultiplier = .2f; rope.sharedMaterial = SportsEventAuthoring.Mat(Folder, "Original stylized rope fuse", new Color(.6f, .35f, .13f));
            for (int i = 0; i < 12; i++) { float t = i / 11f; rope.SetPosition(i, new Vector3(Mathf.Sin(t * 2) * 1.8f, 2.6f + t * 2, 0)); }
            var label = new GameObject("Readable live bomb fuse").AddComponent<TextMeshPro>(); label.transform.SetParent(root, false); label.transform.localPosition = new Vector3(0, 4.8f, 0); label.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; label.rectTransform.sizeDelta = new Vector2(7, 2.5f); label.fontSize = 16; label.color = new Color(1, .9f, .55f); label.outlineWidth = .25f; label.outlineColor = Color.black; label.alignment = TextAlignmentOptions.Center; m.bombClock = label;
        }
    }
}
