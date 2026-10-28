using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckFareFight.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckTornadoRodeo.Editor
{
    public static class TornadoAuthoring
    {
        public const string Folder = "TruckTornadoRodeo", ScenePath = "Assets/TruckTornadoRodeo/Scenes/TruckTornadoRodeo.unity";
        static T Required<T>(string path) where T : UnityEngine.Object { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value == null) throw new InvalidOperationException("Installed foundation required: " + path); return value; }
        static Material Mat(string name, Color color, Material source = null)
        {
            string path = "Assets/" + Folder + "/Materials/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit")); material.name = name; AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.enableInstancing = true; return material;
        }
        [MenuItem("Truck Sports/Impossible Games/Create NEW Tornado Rodeo Venue")]
        public static void Create()
        {
            if (EditorApplication.isPlaying || File.Exists(ScenePath)) throw new InvalidOperationException("Preserve the authored Tornado venue. Create only accepts a missing scene.");
            var tractor = Required<GameObject>(FareFightVehicleAuthoring.PrefabPath);
            Required<Mesh>("Assets/TruckSheepHerding/Models/Continuous rolling farm.asset");
            Required<Texture2D>("Assets/WeatherMaker/Prefab/Textures/Precipitation/WeatherMakerSmokeTexture.png");
            Required<RuntimeAnimatorController>("Assets/TruckSports/Animations/Sheep locomotion.controller");
            var match = SportsEventAuthoring.Begin<TornadoMatch>(Folder); match.setup.count = 4; match.option = 0; match.NormalizeSetup();
            InstallStormOrder(match);
            Directory.CreateDirectory("Assets/" + Folder + "/Models"); Directory.CreateDirectory("Assets/" + Folder + "/Rendering");
            match.feedback.clips = new[] { Required<AudioClip>("Assets/Heat - Complete Modern UI/Audio/Click.wav"), Required<AudioClip>("Assets/TruckBilliards/Audio/Ball.wav"), Required<AudioClip>("Assets/TruckBilliards/Audio/Cushion.wav"), Required<AudioClip>("Assets/TruckBilliards/Audio/Strike.wav"), Required<AudioClip>("Assets/TruckHockey/Audio/Faceoff.wav"), Required<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav"), Required<AudioClip>("Assets/TruckHockey/Audio/FinalWhistle.wav") };
            var fieldRoot = new GameObject("Single mode-local tornado spatial field"); match.field = fieldRoot.AddComponent<TornadoField>(); match.field.match = match;
            match.presentation = match.gameObject.AddComponent<TornadoPresentation>(); match.presentation.match = match;
            var ground = Farm(match); match.ground = ground;
            match.trucks = new SportsEventTruck[4]; match.riders = new TornadoRider[4]; match.starts = new Transform[4]; Physics.SyncTransforms();
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * .25f + i * Mathf.PI * .5f; var p = new Vector3(Mathf.Cos(a) * 78, 0, Mathf.Sin(a) * 78);
                if (!ground.Raycast(new Ray(p + Vector3.up * 200, Vector3.down), out var hit, 400)) throw new InvalidOperationException("Actual farm ground required beneath each original tractor spawn."); p.y = hit.point.y + .65f;
                var start = new GameObject("Authored actual ground start " + i).transform; start.position = p; start.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(-p, Vector3.up)); match.starts[i] = start;
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(tractor); actor.transform.SetPositionAndRotation(p, start.rotation); var truck = actor.GetComponent<SportsEventTruck>();
                if (truck == null || truck.tractorMotor == null || truck.tractorMotor.vehicle == null) throw new InvalidOperationException("Existing full NWH tractor required.");
                truck.session = match; truck.slot = i; truck.tractorMotor.settings = match.settings; match.trucks[i] = truck;
                var rider = actor.AddComponent<TornadoRider>(); rider.match = match; rider.truck = truck; rider.slot = i; match.riders[i] = rider; EditorUtility.SetDirty(truck); EditorUtility.SetDirty(truck.tractorMotor);
            }
            Rings(match); Debris(match); Effects(match);
            match.broadcast = SportsEventAuthoring.Camera("Storm rodeo existing Cinemachine action broadcast"); match.broadcast.view.farClipPlane = 2400; match.broadcast.Pose(new Vector3(0, 15, 0), new Vector3(70, 40, -90), 62, true); match.cameras = new[] { match.broadcast.view };
            match.framing = match.broadcast.gameObject.AddComponent<SportsCameraFit>(); match.framing.cameraTarget = match.broadcast.view;
            SportsEventAuthoring.UI(match); SportsRallyAuthoring.CompactUI(match); match.drivingViews = SportsDrivingViewsAuthoring.Install(match, match.trucks, match.broadcast, Folder); match.drivingViews.independentViewModes = true; match.drivingViews.rally.groundOffset = new Vector3(0, 12, -26); match.drivingViews.rally.groundLookAhead = 5;
            var hud = match.gameObject.AddComponent<SportsExpansionViewHud>(); hud.match = match; hud.views = match.drivingViews; TornadoHudAuthoring.Install(match);
            SportsScenicGradeAuthoring.Install(match.gameObject, new[] { match.broadcast.view }.Concat(match.drivingViews.rally.views.Select(v => v.view)).ToArray(), Folder, -.2f);
            SportsEventAuthoring.Save(Folder); SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match, match.trucks);
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(match.gameObject.scene); EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("TORNADO_AUTHORED: real NWH4, bounded spatial hazard, actual farm/debris/animated sheep,6 whole-body rings, Heat UI. Saved mode forecast-order adapter; lead common weather-provider/time/camera/registry bindings remain explicit.");
        }
        public static void InstallOpenStormOrder()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath) throw new InvalidOperationException("Explicit saved Tornado Rodeo scene required; preserve other venues.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TornadoMatch>(true)).Single(); InstallStormOrder(match);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        static void InstallStormOrder(TornadoMatch match)
        {
            var adapters = match.GetComponents<TornadoStormOrder>();
            if (adapters.Length > 1 || adapters.Length == 1 && adapters[0].match != match) throw new InvalidOperationException("Preserve unknown/custom Tornado forecast bindings.");
            var adapter = adapters.Length == 1 ? adapters[0] : match.gameObject.AddComponent<TornadoStormOrder>(); adapter.match = match; EditorUtility.SetDirty(adapter);
        }
        static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = "Assets/" + Folder + "/Models/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { UnityEngine.Object.DestroyImmediate(mesh); return existing; }
            mesh.name = name; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static Mesh Torus()
        {
            const int n = 72, tube = 10; var v = new Vector3[(n + 1) * (tube + 1)]; var uv = new Vector2[v.Length]; var tris = new int[n * tube * 6];
            for (int i = 0; i <= n; i++) for (int j = 0; j <= tube; j++) { float a = i * Mathf.PI * 2 / n, b = j * Mathf.PI * 2 / tube; int k = i * (tube + 1) + j; v[k] = new Vector3(Mathf.Cos(a) * (9.45f + .45f * Mathf.Cos(b)), Mathf.Sin(a) * (9.45f + .45f * Mathf.Cos(b)), .45f * Mathf.Sin(b)); uv[k] = new Vector2(i / (float)n, j / (float)tube); }
            for (int i = 0; i < n; i++) for (int j = 0; j < tube; j++) { int a = i * (tube + 1) + j, k = (i * tube + j) * 6; tris[k] = a; tris[k + 1] = a + 1; tris[k + 2] = a + tube + 1; tris[k + 3] = a + 1; tris[k + 4] = a + tube + 2; tris[k + 5] = a + tube + 1; }
            var mesh = new Mesh(); mesh.vertices = v; mesh.uv = uv; mesh.triangles = tris; return SaveMesh("Modeled storm training hoop", mesh);
        }
        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 position)
        {
            var root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); root.transform.SetParent(parent, false); root.transform.localPosition = position; root.GetComponent<MeshFilter>().sharedMesh = mesh; root.GetComponent<MeshRenderer>().sharedMaterial = material; return root;
        }
        static void Rings(TornadoMatch match)
        {
            var material = Mat("Amber storm beacon ring enamel", new Color(1, .62f, .04f)); material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(1, .45f, .015f) * .8f);
            var mesh = Torus(); match.rings = new TornadoRing[6]; match.presentation.ringLights = new Renderer[6];
            float[] height = { 18, 30, 45, 22, 38, 55 };
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3; var p = new Vector3(Mathf.Cos(a) * 27, height[i], Mathf.Sin(a) * 27);
                var root = MeshObject("Actual open aerial storm hoop " + i, null, mesh, material, p); root.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
                var ring = root.AddComponent<TornadoRing>(); ring.id = i; ring.apertureRadius = 9; match.rings[i] = ring;
                var rim = root.AddComponent<MeshCollider>(); rim.sharedMesh = mesh; match.presentation.ringLights[i] = root.GetComponent<Renderer>();
            }
        }
        static MeshCollider Farm(TornadoMatch match)
        {
            var root = new GameObject("Storm country landscape and authored farm architecture").transform;
            var grass = Mat("Rain darkened Vista grass", new Color(.29f, .4f, .19f), Required<Material>("Assets/TruckSheepHerding/Materials/Living Vista green meadow.mat"));
            var dirt = Mat("Rain darkened original farm tracks", new Color(.31f, .22f, .12f), Required<Material>("Assets/TruckSheepHerding/Materials/Actual Vista worn farm soil paths.mat"));
            var ground = new GameObject("Original continuous collision matched Vista farm"); ground.transform.SetParent(root, false); var mesh = Required<Mesh>("Assets/TruckSheepHerding/Models/Continuous rolling farm.asset"); ground.AddComponent<MeshFilter>().sharedMesh = mesh; ground.AddComponent<MeshRenderer>().sharedMaterials = new[] { grass, dirt }; var collider = ground.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.sharedMaterial = SportsEventAuthoring.Contact(Folder, "Wet NWH farm driving contact", .7f, .03f); SportsDrivingSurfaceAuthoring.MarkHierarchy(ground.transform);
            var destination = SceneManager.GetActiveScene(); var source = EditorSceneManager.OpenScene(LWS.TruckSheepHerding.Editor.HerdAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(destination);
                var scenery = source.GetRootGameObjects().SingleOrDefault(g => g.name == "Owned farm architecture, installed houses, timber paths and trees");
                if (scenery == null) throw new InvalidOperationException("Preserve the known actual farm scenery source; review its binding if renamed.");
                var copy = SportsReplayVisuals.Copy(scenery.transform); copy.name = "Reused actual barn, farmhouses and Vista pines"; SceneManager.MoveGameObjectToScene(copy, destination); copy.transform.SetParent(root, true);
                foreach (Transform building in copy.transform)
                {
                    if (building.name != "Original detailed timber show barn" && building.name != "Installed rural farmhouse" && building.name != "Installed farm storage warehouse") continue;
                    var rendered = building.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
                    if (rendered.Length == 0) throw new InvalidOperationException("Known barn/farmhouse geometry is required.");
                    var bounds = rendered[0].bounds; foreach (var r in rendered) bounds.Encapsulate(r.bounds);
                    var physical = building.gameObject.AddComponent<BoxCollider>(); physical.center = building.InverseTransformPoint(bounds.center); physical.size = bounds.size;
                }
            }
            finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(destination); }
            var metal = Mat("Corrugated storm shelter and silo steel", new Color(.39f, .43f, .4f), Required<Material>("Assets/TruckTetherball/Materials/Galvanised tether mast.mat"));
            var wood = Required<Material>("Assets/TruckSheepHerding/Materials/Weathered real timber farm rails.mat");
            for (int side = -1; side <= 1; side += 2)
            {
                var silo = MeshObject("Modeled corrugated farm silo " + side, root, SiloMesh(), metal, new Vector3(side * 105, 0, 75));
                var siloShape = silo.AddComponent<CapsuleCollider>(); siloShape.radius = 6; siloShape.height = 32; siloShape.center = Vector3.up * 16;
                for (int rung = 0; rung < 24; rung++) SportsEventAuthoring.Box("Actual silo access ladder rung", new Vector3(0, 1 + rung * 1.2f, -6.5f), new Vector3(1.5f, .12f, .15f), metal, null, silo.transform);
                for (int post = 0; post < 17; post++)
                {
                    float z = -95 + post * 11; SportsEventAuthoring.Box("Weathered farm perimeter fence post", new Vector3(side * 100, 1.5f, z), new Vector3(.5f, 3, .5f), wood, null, root);
                    if (post < 16) for (int rail = 0; rail < 3; rail++) SportsEventAuthoring.Box("Finished farm fence rail", new Vector3(side * 100, .7f + rail * .8f, z + 5.5f), new Vector3(.22f, .2f, 11), wood, null, root);
                }
            }
            var board = new GameObject("Actual storm rodeo shelter scoreboard").AddComponent<TextMeshPro>(); board.transform.SetParent(root, false); board.transform.position = new Vector3(0, 10, 112); board.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; board.rectTransform.sizeDelta = new Vector2(45, 7); board.fontSize = 22; board.enableAutoSizing = true; board.fontSizeMin = 12; board.fontSizeMax = 22; board.color = new Color(1, .85f, .5f); board.alignment = TextAlignmentOptions.Center; match.presentation.arenaBoard = board;
            SportsEventAuthoring.Box("Finished storm board timber shelter", new Vector3(0, 10, 112.6f), new Vector3(47, 9, 1), wood, null, root);
            var vane = new GameObject("Wind driven carved farm weather vane").transform; vane.SetParent(root, false); vane.localPosition = new Vector3(103, 32, 75); SportsEventAuthoring.Box("Weather vane directional arrow", Vector3.zero, new Vector3(6, .18f, .3f), metal, null, vane); match.presentation.windVane = vane;
            for (int side = -1; side <= 1; side += 2) { var fin = SportsEventAuthoring.Box("Modeled weather vane arrowhead", new Vector3(2.4f, 0, side * .5f), new Vector3(1.6f, .18f, .18f), metal, null, vane); fin.transform.localRotation = Quaternion.Euler(0, side * 40, 0); }
            return collider;
        }
        static Mesh SiloMesh()
        {
            const int n = 128; var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int k = v.Count; v.AddRange(new[] { a, b, c, d }); uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }); tr.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 }); }
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n, b = (i + 1) * Mathf.PI * 2 / n, ra = 6 + i % 2 * .09f, rb = 6 + (i + 1) % 2 * .09f;
                var p = new Vector3(Mathf.Cos(a) * ra, 0, Mathf.Sin(a) * ra); var q = new Vector3(Mathf.Cos(b) * rb, 0, Mathf.Sin(b) * rb);
                Quad(p, p + Vector3.up * 29, q + Vector3.up * 29, q); Quad(p + Vector3.up * 29, Vector3.up * 34, Vector3.up * 34, q + Vector3.up * 29);
            }
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tr, 0); return SaveMesh("Closed corrugated silo and conical rain cap", mesh);
        }
        public static void RepairSavedSheepAnimationRoots()
        {
            if (EditorApplication.isPlaying || !File.Exists(ScenePath)) throw new InvalidOperationException("Stopped Editor and saved owned Tornado scene required.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); RepairOwnedOpenSheepAnimationRoots(); }
            finally { if (previous.Any(scene => scene.isLoaded && scene.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }
        public static void RepairOwnedOpenSheepAnimationRoots()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Sheep binding repair is authoring only.");
            var scene = SceneManager.GetActiveScene(); var matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TornadoMatch>(true)).ToArray();
            if (scene.path != ScenePath || matches.Length != 1) throw new InvalidOperationException("Preserve unrelated/custom scene topology.");
            var animals = matches[0].debris.Where(d => d != null && d.animal).OrderBy(d => d.id).ToArray();
            if (animals.Length != 4 || !animals.Select(d => d.id).SequenceEqual(new[] { 12, 13, 14, 15 })) throw new InvalidOperationException("Preserve custom animal roster.");
            var controller = Required<RuntimeAnimatorController>("Assets/TruckSports/Animations/Sheep locomotion.controller");
            var plans = new List<(Animator prior, Transform root, SportsSheepMotion motion)>();
            foreach (var animal in animals)
            {
                var root = animal.transform.Find("SheepV5"); if (root == null) throw new InvalidOperationException("Exact original copied SheepV5 root required.");
                var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true); var motions = animal.GetComponentsInChildren<SportsSheepMotion>(true); var animators = animal.GetComponentsInChildren<Animator>(true);
                if (skins.Length != 1 || skins[0].bones.Length == 0 || skins[0].bones.Any(b => b == null || !b.IsChildOf(root)) || motions.Length != 1 || animators.Length != 1 || motions[0].mover != animal.transform || motions[0].animator != animators[0] || animators[0].runtimeAnimatorController != controller || animators[0].applyRootMotion || animators[0].cullingMode != AnimatorCullingMode.AlwaysAnimate) throw new InvalidOperationException("Preserve custom skeleton/controller/motion ownership.");
                bool binds = controller.animationClips.Any(clip => AnimationUtility.GetCurveBindings(clip).Any(binding => binding.type == typeof(Transform) && root.Find(binding.path) != null));
                if (!binds) throw new InvalidOperationException("Installed genuine clip must bind actual descendant skeleton paths.");
                var prior = animators[0]; if (prior.transform == root) continue;
                if (prior.transform != skins[0].transform || prior.name != "SheepLegsBR" || skins[0].bones.Any(b => b.IsChildOf(prior.transform)) || root.GetComponent<Animator>() != null) throw new InvalidOperationException("Only original misplaced skin-node Animator may be repaired; preserve custom bindings.");
                plans.Add((prior, root, motions[0]));
            }
            // Preflight every actor before changing any component. Renderers,
            // skeleton transforms, materials, body/collider and joints stay put.
            foreach (var plan in plans)
            {
                UnityEngine.Object.DestroyImmediate(plan.prior); var animator = plan.root.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                plan.motion.animator = animator; EditorUtility.SetDirty(plan.motion); EditorUtility.SetDirty(animator);
            }
            if (plans.Count > 0) { EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save exact owned animation repair."); }
            Debug.Log("TORNADO_SHEEP_ANIMATION_ROOTS repaired=" + plans.Count + " actual Armature descendants now bound; transforms/physics unchanged");
        }
        static void Debris(TornadoMatch match)
        {
            var root = new GameObject("Finite native farm debris and animated sheep pool").transform; match.debris = new TornadoDebris[16];
            var contact = SportsEventAuthoring.Contact(Folder, "Bounded physical farm debris contact", .4f, .1f);
            for (int i = 0; i < 16; i++)
            {
                bool animal = i >= 12; var prop = new GameObject(animal ? "Genuine rigged flying SheepV5 " + i : "Native flying farm object " + i); prop.transform.SetParent(root, false);
                GameObject visual;
                if (animal) { visual = SportsReplayVisuals.Copy(Required<GameObject>("Assets/TruckCheeseRolling/Art/Sheep/SheepV5.fbx").transform); }
                else visual = SportsReplayVisuals.Copy(Required<GameObject>(i % 2 == 0 ? "Assets/IndieKit/Crates and Barrels/Prefabs/Non-Destructible/crate_01.prefab" : "Assets/IndieKit/Crates and Barrels/Prefabs/Non-Destructible/barrel_01.prefab").transform);
                visual.transform.SetParent(prop.transform, false); visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity;
                var renderers = visual.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray(); if (renderers.Length == 0) throw new InvalidOperationException("Real farm model renderers required."); var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                float scale = (animal ? 3.6f : 3) / Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)); visual.transform.localScale *= scale; visual.transform.localPosition = -bounds.center * scale;
                bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                var box = prop.AddComponent<BoxCollider>(); box.center = prop.transform.InverseTransformPoint(bounds.center); box.size = bounds.size; box.sharedMaterial = contact;
                var body = prop.AddComponent<Rigidbody>(); body.mass = animal ? 65 : 45; body.linearDamping = .05f; body.angularDamping = .3f; body.maxDepenetrationVelocity = 3; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.isKinematic = true;
                var actor = prop.AddComponent<TornadoDebris>(); actor.match = match; actor.body = body; actor.id = i; actor.animal = animal; actor.hazardRadius = Mathf.Max(.5f, bounds.extents.magnitude * .5f);
                float a = i * 2.399963f; actor.startPosition = new Vector3(Mathf.Cos(a) * (18 + i % 4 * 3), bounds.extents.y + .5f, Mathf.Sin(a) * (18 + i % 4 * 3)); prop.transform.position = actor.startPosition; match.debris[i] = actor;
                if (animal) SheepMotionAuthoring.Install(prop, prop.transform);
            }
        }
        static void Effects(TornadoMatch match)
        {
            var smoke = Mat("Owned Weather Maker smoke vortex particles", Color.white, Required<Material>("Assets/NWH/Vehicle Physics 2/Effects/Materials/DustCloud.mat")); smoke.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); smoke.SetTexture("_BaseMap", Required<Texture2D>("Assets/WeatherMaker/Prefab/Textures/Precipitation/WeatherMakerSmokeTexture.png")); smoke.SetFloat("_Surface", 1); smoke.SetFloat("_Blend", 0); smoke.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); smoke.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); smoke.SetInt("_ZWrite", 0); smoke.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); smoke.renderQueue = 3000;
            var root = new GameObject("Bounded actual storm spiral smoke particles").AddComponent<ParticleSystem>(); root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var main = root.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 900; main.startLifetime = 2.5f; main.startSpeed = 0; var emission = root.emission; emission.enabled = false; var shape = root.shape; shape.enabled = false; root.GetComponent<ParticleSystemRenderer>().sharedMaterial = smoke; match.presentation.funnel = root;
            var dust = new GameObject("Continuous low storm skirt dust").AddComponent<ParticleSystem>(); dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); main = dust.main; main.loop = true; main.playOnAwake = false; main.startLifetime = 1.8f; main.startSize = new ParticleSystem.MinMaxCurve(3, 6); main.startSpeed = 2; main.startColor = new Color(.3f, .25f, .17f, .15f); main.maxParticles = 80; emission = dust.emission; emission.rateOverTime = 30; shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 20; dust.transform.rotation = Quaternion.Euler(90, 0, 0); dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = smoke; match.presentation.dust = dust;
            var originalDust = Required<GameObject>("Assets/CoreCollapse/Demo/Prefabs/Particles/Dust.prefab"); var contactRoot = UnityEngine.Object.Instantiate(originalDust); contactRoot.name = "Pooled existing farm debris contact dust"; foreach (var script in contactRoot.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script); var particles = contactRoot.GetComponent<ParticleSystem>(); if (particles == null) { particles = contactRoot.AddComponent<ParticleSystem>(); var e = particles.emission; e.enabled = false; particles.GetComponent<ParticleSystemRenderer>().enabled = false; } foreach (var p in contactRoot.GetComponentsInChildren<ParticleSystem>(true)) { p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var m = p.main; m.loop = false; m.playOnAwake = false; m.maxParticles = Mathf.Min(m.maxParticles, 80); } match.presentation.contact = particles;
            var audio = match.gameObject.AddComponent<AudioSource>(); audio.clip = Required<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Weather/WeatherMakerWind1.ogg"); audio.loop = true; audio.playOnAwake = false; audio.spatialBlend = 1; audio.minDistance = 30; audio.maxDistance = 220; audio.dopplerLevel = 0; match.presentation.roar = audio;
        }
    }
}
