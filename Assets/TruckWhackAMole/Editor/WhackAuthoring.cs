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
using LWS.TruckFieldGoal;
namespace LWS.TruckWhackAMole.Editor
{
    public static class WhackAuthoring
    {
        public const string Folder = "TruckWhackAMole", Scene = "Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity";
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Preserve the authored fairground. Apply targeted repairs to this scene.");
            MolePrefabAuthoring.Create();
            var m = SportsEventAuthoring.Begin<WhackMatch>(Folder); m.setup.count = 8; m.NormalizeSetup();
            m.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            var dirt = SportsEventAuthoring.Mat(Folder, "Fairground textured earth", new Color(.41f, .23f, .10f), .08f);
            var mud = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKeepaway/Materials/Rocky mud terrain.mat"); dirt.mainTexture = mud.mainTexture; dirt.mainTextureScale = Vector2.one * 26;
            var grass = SportsEventAuthoring.Mat(Folder, "Vista meadow surround", new Color(.42f, .64f, .24f), .05f); grass.mainTexture = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer").diffuseTexture; grass.mainTextureScale = Vector2.one * 40;
            var ground = SportsEventAuthoring.Contact(Folder, "Firm traversable fairground earth", .8f, .02f);
            SportsEventAuthoring.Box("Continuous meadow surround", new Vector3(0, -1.1f, 0), new Vector3(550, 1, 550), grass, ground);
            SportsEventAuthoring.Box("Continuous navigable dirt arena", new Vector3(0, -.5f, 0), new Vector3(172, 1, 172), dirt, ground);
            var wood = SportsEventAuthoring.Mat(Folder, "Warm wooden carnival boundary", new Color(.49f, .27f, .11f), .1f);
            Fence(wood, ground); Stadium(m); Dress(wood, ground);
            var holeMaterial = SportsEventAuthoring.Mat(Folder, "Deep shadowed mole holes", new Color(.025f, .016f, .018f), .01f);
            var mound = Mound(); m.moleField = new GameObject("Recorded animated fairground mole field").transform; m.moles = new WhackMole[36]; m.dirt = new ParticleSystem[36];
            for (int i = 0; i < 36; i++)
            {
                var position = new Vector3(-45 + (i % 6) * 18, .02f, -45 + (i / 6) * 18);
                var ring = new GameObject("Sculpted gentle dirt rim " + i); ring.transform.position = position; ring.AddComponent<MeshFilter>().sharedMesh = mound; ring.AddComponent<MeshRenderer>().sharedMaterial = dirt; ring.AddComponent<MeshCollider>().sharedMesh = mound;
                var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); hole.name = "Inactive holes never award points " + i; hole.transform.position = position; hole.transform.localScale = new Vector3(3.5f, .008f, 3.5f); hole.GetComponent<Renderer>().sharedMaterial = holeMaterial; UnityEngine.Object.DestroyImmediate(hole.GetComponent<Collider>());
                var actor = new GameObject("Pooled physical mole exposure " + i); actor.transform.SetParent(m.moleField, false); actor.transform.position = position; actor.transform.rotation = Quaternion.LookRotation(-position + Vector3.up * position.y);
                var body = actor.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                var target = actor.AddComponent<CapsuleCollider>(); target.isTrigger = true; target.radius = 1.55f; target.height = 4.3f; target.center = Vector3.up * 2; target.enabled = false;
                var mole = actor.AddComponent<WhackMole>(); mole.match = m; mole.hole = i; mole.target = target; mole.models = new Transform[3]; mole.animators = new Animator[3]; m.moles[i] = mole;
                for (int kind = 0; kind < 3; kind++)
                {
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MolePrefabAuthoring.PathFor((MoleKind)kind)), actor.transform);
                    model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.zero; mole.models[kind] = model.transform; mole.animators[kind] = model.GetComponent<Animator>(); mole.animators[kind].enabled = false;
                }
                m.dirt[i] = Dust("Pooled dirt clods " + i, dirt);
            }
            m.trucks = new SportsEventTruck[8]; m.fragments = new SportsFragmentRig[8]; m.decoyBlasts = new ParticleSystem[8];
            for (int i = 0; i < 8; i++)
            {
                var truck = FareFightVehicleAuthoring.Spawn(m, i, m.Grid(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(m.Grid(i), Vector3.up))); m.trucks[i] = truck;
                m.fragments[i] = SportsFragmentAuthoring.InstallRigid(truck.body, truck.visuals.GetComponentsInChildren<MeshFilter>(true), Folder);
                var bumper = new GameObject("Actual front bumper contact zone"); bumper.layer = truck.gameObject.layer; bumper.transform.SetParent(truck.transform, false);
                var component = bumper.AddComponent<WhackBumper>(); component.match = m; component.slot = i;
                var shape = bumper.AddComponent<BoxCollider>(); shape.isTrigger = true; shape.center = truck.shape.center + Vector3.forward * (truck.shape.size.z * .5f + .2f); shape.size = new Vector3(truck.shape.size.x * .95f, truck.shape.size.y * .75f, .8f);
                var blast = new GameObject("Pooled decoy cartoon blast " + i).AddComponent<ParticleSystem>(); SportsChaosAuthoring.FixBlast(blast, SportsChaosAuthoring.Flare(Folder)); m.decoyBlasts[i] = blast;
            }
            m.broadcast = SportsEventAuthoring.Camera("Fairground whole-arena broadcast"); m.broadcast.view.orthographic = true;
            m.framing = m.broadcast.gameObject.AddComponent<SportsCameraFit>(); m.framing.cameraTarget = m.broadcast.view; m.framing.arenaFit = true; m.framing.topInset = .1f; m.framing.bottomInset = .25f; m.cameras = new[] { m.broadcast.view };
            SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m); m.drivingViews = SportsDrivingViewsAuthoring.Install(m, m.trucks, m.broadcast, Folder); m.drivingViews.independentViewModes = true;
            m.atmosphere = SportsChaosAuthoring.Weather(m, Folder); m.atmosphere.presets = SportsWeatherPresetsAuthoring.EnsureCatalog(); m.drivingViews.rally.weather = m.atmosphere; SportsWeatherPickerAuthoring.Install(m, m.atmosphere.presets, new Vector2(350, -180));
            SportsUIAuthoring.Button(m.menus.pages[6].transform, "WATCH THE WHACKS", "watchwhack", new Vector2(0, -245), new Vector2(520, 48)); var hud = m.gameObject.AddComponent<SportsExpansionViewHud>(); hud.match = m; hud.views = m.drivingViews;
            Artwork(m); SportsBroadcastAuthoring.ApplyScene(); SportsEventAuthoring.Save(Folder);
        }
        static Mesh Mound()
        {
            string path = "Assets/TruckWhackAMole/Materials/Original sculpted mole-hole rim.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            const int slices = 32; var vertices = new Vector3[slices * 4]; var uv = new Vector2[vertices.Length]; var triangles = new int[slices * 3 * 6]; float[] radii = { 1.72f, 2.1f, 2.9f, 3.8f }, heights = { -.06f, .32f, .24f, 0 };
            for (int ring = 0; ring < 4; ring++) for (int k = 0; k < slices; k++) { float a = k * Mathf.PI * 2 / slices, r = radii[ring] * (1 + Mathf.Sin(k * 2.1f) * .025f); int at = ring * slices + k; vertices[at] = new Vector3(Mathf.Sin(a) * r, heights[ring], Mathf.Cos(a) * r); uv[at] = new Vector2(vertices[at].x, vertices[at].z) * .3f; }
            int index = 0; for (int ring = 0; ring < 3; ring++) for (int k = 0; k < slices; k++) { int a = ring * slices + k, b = ring * slices + (k + 1) % slices, c = a + slices, d = b + slices; triangles[index++] = a; triangles[index++] = c; triangles[index++] = b; triangles[index++] = b; triangles[index++] = c; triangles[index++] = d; }
            mesh = new Mesh { name = "Original gently rounded mole-hole dirt rim", vertices = vertices, uv = uv, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static ParticleSystem Dust(string name, Material material)
        {
            var effect = new GameObject(name).AddComponent<ParticleSystem>(); SportsChaosAuthoring.FixBlast(effect, material); var main = effect.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .9f); main.startSpeed = new ParticleSystem.MinMaxCurve(3, 7); main.startSize = new ParticleSystem.MinMaxCurve(.1f, .35f); main.gravityModifier = 1; main.startColor = new Color(.6f, .35f, .14f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var over = effect.colorOverLifetime; over.enabled = false; var template = GameObject.CreatePrimitive(PrimitiveType.Sphere); var renderer = effect.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = template.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.DestroyImmediate(template); return effect;
        }
        static void Fence(Material material, PhysicsMaterial contact)
        {
            var root = new GameObject("Owned Mega City wood-tone fairground fencing").transform;
            for (int side = 0; side < 4; side++) for (int i = 0; i < 21; i++)
            {
                var p = Quaternion.Euler(0, side * 90, 0) * new Vector3(-80 + i * 8, 0, 85);
                var collider = SportsEventAuthoring.Box("Continuous fairground fence contact", p + Vector3.up * 1.3f, new Vector3(8.1f, 2.6f, .5f), material, contact, root); collider.transform.rotation = Quaternion.Euler(0, side * 90, 0); collider.GetComponent<Renderer>().enabled = false;
                var prop = new GameObject("Reused authored fence mesh").transform; prop.SetParent(root, false); prop.position = p; prop.rotation = Quaternion.Euler(0, side * 90, 0); LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(prop, "Assets/JC_LP_MegaCity/Prefabs/FloorProps/SM_FloorProps_Fence_01.prefab", new Vector3(8, 2.6f, .5f), 0);
                foreach (var renderer in prop.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            }
        }
        static void Stadium(WhackMatch m)
        {
            var scene = SceneManager.GetActiveScene(); var source = EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); var hot = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LWS.TruckHotPotato.HotPotatoMatch>(true)).First();
                var stand = source.GetRootGameObjects().First(g => g.name == "Reused Hot Potato Existing Union Field grandstands and press boxes"); var venue = UnityEngine.Object.Instantiate(stand); venue.name = "Reused owned fairground spectator stands"; SceneManager.MoveGameObjectToScene(venue, scene); venue.transform.localScale *= 1.2f;
                var fans = UnityEngine.Object.Instantiate(hot.crowd.batches[0].fans[0].transform.parent.gameObject); fans.name = "Shared-mesh fairground spectators"; SceneManager.MoveGameObjectToScene(fans, scene); fans.transform.localScale *= 1.2f;
                var filters = fans.GetComponentsInChildren<MeshFilter>(); m.crowd = m.gameObject.AddComponent<FieldGoalCrowd>(); m.crowd.eventSession = m; m.crowd.nearFans = Array.Empty<Animator>(); m.crowd.batches = new FieldGoalCrowd.Batch[hot.crowd.batches.Length];
                for (int i = 0; i < m.crowd.batches.Length; i++) { var original = hot.crowd.batches[i]; var posed = filters.Where(f => f.sharedMesh == original.idle).ToArray(); m.crowd.batches[i] = new FieldGoalCrowd.Batch { idle = original.idle, cheer = original.cheer, materials = original.materials, fans = posed, seats = posed.Select(f => f.transform.localToWorldMatrix).ToArray() }; }
                m.crowd.response = m.gameObject.AddComponent<AudioSource>(); m.crowd.response.playOnAwake = false; m.crowd.cheer = hot.crowd.cheer; m.crowd.boo = hot.crowd.boo;
            }
            finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(scene); }
        }
        static void Dress(Material wood, PhysicsMaterial contact)
        {
            var root = new GameObject("Fairground stalls, bunting and countryside").transform;
            var teal = SportsEventAuthoring.Mat(Folder, "Carnival turquoise", new Color(.03f, .56f, .50f)); var gold = SportsEventAuthoring.Mat(Folder, "Carnival ochre", new Color(1, .65f, .05f)); var coral = SportsEventAuthoring.Mat(Folder, "Carnival coral", new Color(.94f, .16f, .1f));
            for (int i = 0; i < 60; i++) { float a = i * 2.39996f, radius = 185 + i % 4 * 18; LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(root, new Vector3(Mathf.Sin(a) * radius, -.6f, Mathf.Cos(a) * radius), i % 4 == 0 ? 10 : 16, i % 4 != 0, Folder); }
            var canopy = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckPedestrianPlowing/Models/Festival canopy.asset"); var pennant = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckLimbo/Materials/Original cloth pennant.asset");
            for (int i = 0; i < 8; i++)
            {
                var stall = new GameObject("Reused authored carnival stall").transform; stall.SetParent(root, false); stall.position = new Vector3(-68 + i * 19, -.6f, -105); stall.localScale = Vector3.one * 2.2f;
                var roof = new GameObject("Existing festival canopy mesh"); roof.transform.SetParent(stall, false); roof.AddComponent<MeshFilter>().sharedMesh = canopy; roof.AddComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? teal : coral;
                foreach (float x in new[] { -1.8f, 1.8f }) foreach (float z in new[] { -1.3f, 1.3f }) SportsEventAuthoring.Box("Wooden stall post", new Vector3(x, 1.35f, z), new Vector3(.12f, 2.7f, .12f), wood, contact, stall);
                SportsEventAuthoring.Box("Carnival counter", new Vector3(0, .9f, .4f), new Vector3(3.5f, 1.8f, 1.4f), i % 2 == 0 ? teal : gold, contact, stall);
            }
            for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 40; i++) { var flag = new GameObject("Reused original fairground pennant"); flag.transform.SetParent(root, false); flag.transform.position = new Vector3(-78 + i * 4, 9 + Mathf.Sin(i * .2f), side * 90); flag.transform.localScale = Vector3.one * 1.6f; flag.AddComponent<MeshFilter>().sharedMesh = pennant; flag.AddComponent<MeshRenderer>().sharedMaterial = i % 3 == 0 ? gold : i % 3 == 1 ? teal : coral; }
        }
        static void Artwork(WhackMatch m)
        {
            var logo = SportsRetroMenuAuthoring.LoadLogo(Folder);
            foreach (int page in new[] { 0, 4 }) { var image = SportsUIAuthoring.Rect("Original sport title artwork", m.menus.pages[page].transform, new Vector2(260, 160), new Vector2(-560, 365)).AddComponent<UnityEngine.UI.Image>(); image.sprite = logo; image.preserveAspect = true; image.raycastTarget = false; var title = m.menus.pages[page].transform.Find("Title").GetComponent<TMP_Text>(); title.rectTransform.anchoredPosition += Vector2.right * 140; title.rectTransform.sizeDelta = new Vector2(1000, title.rectTransform.sizeDelta.y); }
            var material = SportsEventAuthoring.Mat(Folder, "Original Whack-a-Mole venue artwork", Color.white); material.shader = Shader.Find("Universal Render Pipeline/Unlit"); material.mainTexture = logo.texture; material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10); material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            var sign = GameObject.CreatePrimitive(PrimitiveType.Quad); sign.name = "Original fairground title billboard"; UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>()); sign.transform.position = new Vector3(0, 23, 100); sign.transform.localScale = new Vector3(35, 23, 1); sign.GetComponent<Renderer>().sharedMaterial = material;
            var plate = SportsEventAuthoring.Mat(Folder, "Scoreboard chocolate backing", new Color(.055f, .025f, .04f)); SportsEventAuthoring.Box("Physical carnival score board", new Vector3(0, 10, 101), new Vector3(58, 19, 1), plate);
            var label = new GameObject("Actual authoritative fairground scores").AddComponent<TextMeshPro>(); label.transform.position = new Vector3(0, 10, 100.4f); label.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; label.rectTransform.sizeDelta = new Vector2(54, 17); label.fontSize = 20; label.enableAutoSizing = true; label.fontSizeMin = 8; label.fontSizeMax = 22; label.color = new Color(1, .9f, .5f); label.outlineColor = Color.black; label.outlineWidth = .15f; label.alignment = TextAlignmentOptions.Center; m.stadiumScores = label;
        }
    }
}
