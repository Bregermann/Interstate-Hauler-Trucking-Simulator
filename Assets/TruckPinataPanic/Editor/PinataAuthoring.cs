using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckFareFight.Editor;
using LWS.TruckFieldGoal;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckPinataPanic.Editor
{
    public static class PinataAuthoring
    {
        public const string Folder = "TruckPinataPanic", ScenePath = "Assets/TruckPinataPanic/Scenes/TruckPinataPanic.unity";
        static readonly Color[] Palette = { new Color(.95f,.15f,.35f), new Color(1,.62f,.055f), new Color(.04f,.66f,.67f), new Color(.55f,.2f,.75f), new Color(.24f,.69f,.29f), new Color(.98f,.32f,.10f), new Color(.08f,.38f,.80f) };
        static T Require<T>(string path) where T : UnityEngine.Object { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset == null) throw new InvalidOperationException("Installed foundation required: " + path); return asset; }
        static Material Mat(string name, Color color, float smoothness = .25f, bool paper = false)
        {
            var material = SportsEventAuthoring.Mat(Folder, name, color, smoothness);
            if (paper) { material.mainTexture = PinataArt.PaperTexture(); material.mainTextureScale = Vector2.one * 4; }
            return material;
        }
        [MenuItem("Truck Sports/Impossible Games/Create NEW Pinata Panic Venue")]
        public static void Create()
        {
            if (EditorApplication.isPlaying || File.Exists(ScenePath)) throw new InvalidOperationException("Create is explicit and new-scene only. Preserve the existing Pinata Panic venue.");
            var originalTractor = Require<GameObject>(FareFightVehicleAuthoring.PrefabPath);
            Require<GameObject>("Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Notification/Notification.prefab");
            Require<RuntimeAnimatorController>("Assets/TruckFieldGoal/Crowd/Stadium reactions.controller");
            Require<GameObject>("Assets/TruckSports/FinishPresentation/Existing Balloon paper confetti.prefab");
            Require<GameObject>("Assets/CoreCollapse/Demo/Prefabs/Particles/Explosion.prefab");
            foreach (string dir in new[] { "Art", "Art/Meshes", "Prefabs", "Tests", "Rendering" }) Directory.CreateDirectory("Assets/" + Folder + "/" + dir);
            var match = SportsEventAuthoring.Begin<PinataMatch>(Folder); match.setup.count = 4; match.option = 0; match.NormalizeSetup();
            match.feedback.clips = new[] {
                Require<AudioClip>("Assets/Heat - Complete Modern UI/Audio/Click.wav"),
                Require<AudioClip>("Assets/TruckBilliards/Audio/Ball.wav"), Require<AudioClip>("Assets/TruckBilliards/Audio/Cushion.wav"),
                Require<AudioClip>("Assets/TruckBilliards/Audio/Strike.wav"), Require<AudioClip>("Assets/TruckHockey/Audio/Faceoff.wav"),
                Require<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav"), Require<AudioClip>("Assets/TruckHockey/Audio/FinalWhistle.wav") };
            match.feedback.voices = 8;
            match.festival = match.gameObject.AddComponent<PinataFestivalPresentation>(); match.festival.match = match;
            var paper = new Material[7]; for (int i = 0; i < paper.Length; i++) paper[i] = Mat("Hand laid festival tissue " + i, Palette[i], .08f, true);
            var physics = SportsEventAuthoring.Contact(Folder, "Carnival paved driving grip", .85f, .02f);
            Venue(match, paper, physics); HangingStar(match, paper); CandyPool(match, paper);
            match.trucks = new SportsEventTruck[4];
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * .25f + i * Mathf.PI * .5f; var p = new Vector3(Mathf.Cos(a) * 35, .65f, Mathf.Sin(a) * 35);
                // Preserve the installed canonical prefab byte-for-byte. Its
                // EnsurePrefab helper also dirties legacy cameras on the asset;
                // this new scene only needs the already-authored full tractor.
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(originalTractor);
                actor.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Vector3.ProjectOnPlane(-p, Vector3.up)));
                var facade = actor.GetComponent<SportsEventTruck>();
                if (facade == null || facade.tractorMotor == null || facade.tractorMotor.vehicle == null) throw new InvalidOperationException("Canonical full NWH sport tractor is required.");
                facade.session = match; facade.slot = i; facade.tractorMotor.settings = match.settings; match.trucks[i] = facade;
                EditorUtility.SetDirty(facade); EditorUtility.SetDirty(facade.tractorMotor);
            }
            match.broadcast = SportsEventAuthoring.Camera("Pinata festival shared Cinemachine camera"); match.broadcast.view.farClipPlane = 1600;
            match.broadcast.Pose(new Vector3(0, 5, 0), new Vector3(0, 65, -72), 60, true);
            match.framing = match.broadcast.gameObject.AddComponent<SportsCameraFit>(); match.framing.cameraTarget = match.broadcast.view; match.framing.arenaFit = true; match.framing.arenaBounds = new Bounds(Vector3.up * 5, new Vector3(82, 16, 82));
            match.framing.topInset = .12f; match.framing.bottomInset = .18f; match.cameras = new[] { match.broadcast.view };
            SportsEventAuthoring.UI(match); SportsRallyAuthoring.CompactUI(match);
            match.drivingViews = SportsDrivingViewsAuthoring.Install(match, match.trucks, match.broadcast, Folder); match.drivingViews.independentViewModes = true;
            match.drivingViews.rally.groundOffset = new Vector3(0, 9, -19);
            var hud = match.gameObject.AddComponent<SportsExpansionViewHud>(); hud.match = match; hud.views = match.drivingViews;
            PinataHudAuthoring.Install(match);
            var cameras = new[] { match.broadcast.view }.Concat(match.drivingViews.rally.views.Select(view => view.view)).ToArray();
            SportsScenicGradeAuthoring.Install(match.gameObject, cameras, Folder, -.25f);
            SportsEventAuthoring.Save(Folder);
            // The lead's shared binder requires the actual saved scene identity.
            SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match, match.trucks);
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(match.gameObject.scene); EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("PINATA_AUTHORED: saved new finished festival, full NWH4, physical suspended CoreCollapse shell, seven pooled prefragments, physical candy48, Heat UI and original crowd. Common registry/weather/logo integration remains explicit.");
        }
        [MenuItem("Truck Sports/Impossible Games/Repair owned Pinata top suspension")]
        public static void RepairSavedSuspension()
        {
            if (EditorApplication.isPlaying || !File.Exists(ScenePath)) throw new InvalidOperationException("Repair requires the saved owned Pinata scene outside play mode.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                RepairOwnedOpenSuspension();
            }
            finally
            {
                // A headless batch may begin without a loaded active scene.
                if (previous.Any(scene => scene.isLoaded && scene.isActive)) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }
        public static void RepairOwnedOpenSuspension()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Suspension repair is authoring only.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PinataMatch>(true)).ToArray();
            if (scene.path != ScenePath || matches.Length != 1) throw new InvalidOperationException("Preserve unrelated scene or custom Pinata match topology.");
            var shell = matches[0].pinata;
            if (shell == null || shell.match != matches[0] || shell.body == null || shell.body.gameObject != shell.gameObject || shell.suspension == null || shell.suspension.gameObject != shell.gameObject || shell.shellColliders == null || shell.shellColliders.Length != 7 || shell.shellColliders.Any(c => c == null || c.attachedRigidbody != shell.body || !(c is MeshCollider mesh) || mesh.sharedMesh == null || !mesh.convex)) throw new InvalidOperationException("Preserve custom shell/compound contact bindings.");
            for (int i = 0; i < shell.shellColliders.Length; i++)
                if (((MeshCollider)shell.shellColliders[i]).sharedMesh.name != "Seven point shell panel " + i) throw new InvalidOperationException("Preserve custom paper mesh identity/order.");
            if (Mathf.Abs(((MeshCollider)shell.shellColliders[0]).sharedMesh.bounds.max.y - 5.1f) > .0001f) throw new InvalidOperationException("Modeled top tip no longer matches the reviewed attachment point.");
            var joint = shell.suspension;
            bool near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-8f;
            if (!near(shell.restPosition, new Vector3(0, 5.2f, 0)) || !near(shell.transform.position, shell.restPosition) || Quaternion.Angle(shell.transform.rotation, Quaternion.identity) > .001f || !near(shell.transform.lossyScale, Vector3.one) || joint.connectedBody != null || joint.autoConfigureConnectedAnchor || !near(joint.connectedAnchor, new Vector3(0, 15.2f, 0)) || joint.xMotion != ConfigurableJointMotion.Limited || joint.yMotion != ConfigurableJointMotion.Limited || joint.zMotion != ConfigurableJointMotion.Limited || joint.angularXMotion != ConfigurableJointMotion.Free || joint.angularYMotion != ConfigurableJointMotion.Free || joint.angularZMotion != ConfigurableJointMotion.Free) throw new InvalidOperationException("Preserve custom rest/support/free-swing configuration.");
            bool legacy = near(joint.anchor, Vector3.zero) && Mathf.Abs(joint.linearLimit.limit - 10) < .0001f;
            bool repaired = near(joint.anchor, new Vector3(0, 5.1f, 0)) && Mathf.Abs(joint.linearLimit.limit - 4.9f) < .0001f;
            if (!legacy && !repaired) throw new InvalidOperationException("Preserve custom suspension anchor or rope length; explicit original center attachment required.");
            if (repaired) { Debug.Log("PINATA_TOP_SUSPENSION_ALREADY_AUTHORED"); return; }
            // The modeled top tip bears the rope. The real COM hangs below it,
            // so PhysX gravity restores the paper face after genuine impacts.
            // Health, mass, free angles, colliders and all gameplay remain intact.
            joint.anchor = new Vector3(0, 5.1f, 0); var limit = joint.linearLimit; limit.limit = 4.9f; joint.linearLimit = limit;
            if (shell.rope != null) { shell.rope.SetPosition(0, joint.connectedAnchor); shell.rope.SetPosition(1, shell.transform.TransformPoint(joint.anchor)); EditorUtility.SetDirty(shell.rope); }
            EditorUtility.SetDirty(joint); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Owned suspension repair could not save scene.");
            Debug.Log("PINATA_TOP_SUSPENSION_REPAIRED localTop=(0,5.1,0) support=(0,15.2,0) rope=4.9; original health/mass/contacts/scoring unchanged");
        }
        static void HangingStar(PinataMatch match, Material[] paper)
        {
            var root = new GameObject("Seven point hand-fringed physical paper pinata"); root.transform.position = new Vector3(0, 5.2f, 0);
            var body = root.AddComponent<Rigidbody>(); body.mass = 1100; body.linearDamping = .04f; body.angularDamping = .9f; body.maxAngularVelocity = 6; body.maxDepenetrationVelocity = 3;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.solverIterations = 24; body.solverVelocityIterations = 12; body.isKinematic = true;
            root.AddComponent<PinataSuspensionSupport>(); var shell = root.AddComponent<PinataShell>(); match.pinata = shell; shell.match = match; shell.body = body;
            shell.restPosition = root.transform.position; shell.maximumHealth = 2400;
            var intact = new GameObject("Original finished folded paper and tissue fringe").transform; intact.SetParent(root.transform, false); shell.intact = intact;
            shell.fragments = new Rigidbody[7]; var colliders = new List<Collider>();
            var contact = SportsEventAuthoring.Contact(Folder, "Paper shell real impact contact", .35f, .08f);
            var paperContact = SportsEventAuthoring.Contact(Folder, "Light paper prefragment contact", .5f, .05f);
            for (int i = 0; i < 7; i++)
            {
                var mesh = PinataArt.PaperWedge(i); PinataArt.Model("Folded colored paper star point " + i, intact, mesh, paper[i], Vector3.zero);
                var colliderObject = new GameObject("Exact convex shell paper panel contact " + i); colliderObject.transform.SetParent(root.transform, false);
                var collider = colliderObject.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = true; collider.sharedMaterial = contact; colliders.Add(collider);
                var frag = PinataArt.Model("Pooled actual torn star panel " + i, root.transform, mesh, paper[i], Vector3.zero);
                var fragmentCollider = frag.AddComponent<MeshCollider>(); fragmentCollider.sharedMesh = mesh; fragmentCollider.convex = true; fragmentCollider.sharedMaterial = paperContact;
                var fragmentBody = frag.AddComponent<Rigidbody>(); fragmentBody.mass = 8; fragmentBody.linearDamping = .8f; fragmentBody.angularDamping = .8f; fragmentBody.maxDepenetrationVelocity = 2; fragmentBody.isKinematic = true;
                shell.fragments[i] = fragmentBody; frag.SetActive(false);
            }
            shell.shellColliders = colliders.ToArray();
            for (int i = 0; i < 3; i++) PinataArt.Model("Overlapping individually cut tissue fringe " + i, intact, PinataArt.Fringe(i), paper[(i + 2) % 7], Vector3.zero);
            match.festival.damageLayers = new Renderer[3]; var kraft = Mat("Torn inner kraft paper", new Color(.17f, .055f, .035f), .02f, true);
            for (int i = 0; i < 3; i++) { var tear = PinataArt.Model("Actual damage exposed inner paper " + i, intact, PinataArt.Tear(i), kraft, Vector3.zero); match.festival.damageLayers[i] = tear.GetComponent<Renderer>(); match.festival.damageLayers[i].enabled = false; }
            var joint = root.AddComponent<ConfigurableJoint>(); shell.suspension = joint;
            joint.autoConfigureConnectedAnchor = false; joint.connectedAnchor = new Vector3(0, 15.2f, 0); joint.anchor = new Vector3(0, 5.1f, 0);
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited; joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
            joint.linearLimit = new SoftJointLimit { limit = 4.9f, bounciness = 0, contactDistance = .05f }; joint.projectionMode = JointProjectionMode.PositionAndRotation; joint.projectionDistance = .15f;
            var rope = new GameObject("Actual taut braided suspension cable").AddComponent<LineRenderer>(); rope.transform.SetParent(root.transform, false); shell.rope = rope; rope.useWorldSpace = true; rope.positionCount = 2;
            rope.SetPosition(0, joint.connectedAnchor); rope.SetPosition(1, root.transform.position); rope.widthMultiplier = .16f; rope.numCapVertices = 4; rope.sharedMaterial = Require<Material>("Assets/TruckBungeeDemolition/Materials/Bungee braided safety cable.mat");
        }
        static void CandyPool(PinataMatch match, Material[] paper)
        {
            var pool = new GameObject("Finite physical candy pool").transform; match.candies = new PinataCandy[PinataRules.CandyCapacity];
            var gold = Mat("Golden embossed confection foil", new Color(1, .68f, .075f), .72f); gold.SetFloat("_Metallic", .72f);
            var liquorice = Mat("Danger sweet black and red foil", new Color(.045f, .018f, .022f), .55f, true);
            var fuse = Mat("Unlit woven explosive sweet fuse", new Color(1, .32f, .035f), .2f);
            var physics = SportsEventAuthoring.Contact(Folder, "Candy real rolling and bounce", .4f, .3f);
            for (int i = 0; i < match.candies.Length; i++)
            {
                var kind = i % 12 == 0 || i % 12 == 1 ? PinataCandyKind.Explosive : i % 12 == 2 || i % 12 == 3 ? PinataCandyKind.Golden : PinataCandyKind.Normal;
                var mesh = kind == PinataCandyKind.Golden ? PinataArt.GoldenSweet() : PinataArt.WrappedSweet(kind == PinataCandyKind.Explosive);
                var model = PinataArt.Model(kind + " physical confection " + i, pool, mesh, kind == PinataCandyKind.Golden ? gold : kind == PinataCandyKind.Explosive ? liquorice : paper[i % 7], Vector3.zero);
                var shape = model.AddComponent<SphereCollider>(); shape.radius = kind == PinataCandyKind.Golden ? .72f : .65f; shape.sharedMaterial = physics;
                var body = model.AddComponent<Rigidbody>(); body.mass = 6; body.linearDamping = .16f; body.angularDamping = .2f; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.maxDepenetrationVelocity = 3; body.isKinematic = true;
                var candy = model.AddComponent<PinataCandy>(); candy.match = match; candy.body = body; candy.id = i; candy.kind = kind; match.candies[i] = candy;
                if (kind == PinataCandyKind.Explosive)
                {
                    var wick = new GameObject("Modeled curved readable warning fuse").AddComponent<LineRenderer>(); wick.transform.SetParent(model.transform, false); wick.useWorldSpace = false; wick.positionCount = 9; wick.widthMultiplier = .095f; wick.sharedMaterial = fuse;
                    for (int k = 0; k < 9; k++) { float t = k / 8f; wick.SetPosition(k, new Vector3(.1f + .6f * t, .4f + Mathf.Sin(t * 2.2f) * .55f, 0)); }
                    var warning = new GameObject("Explosive warning mark").AddComponent<TextMeshPro>(); warning.transform.SetParent(model.transform, false); warning.transform.localPosition = new Vector3(0, 0, -.51f); warning.text = "!"; warning.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; warning.fontSize = 5; warning.color = new Color(1, .15f, .08f); warning.alignment = TextAlignmentOptions.Center; warning.rectTransform.sizeDelta = new Vector2(.6f, .8f);
                }
                model.SetActive(false);
            }
        }
        static void Venue(PinataMatch match, Material[] paper, PhysicsMaterial contact)
        {
            var root = new GameObject("Finished Fiesta del Motor festival venue").transform;
            var paving = Mat("Weathered terracotta carnival pavers", new Color(.48f, .22f, .13f), .12f);
            var source = Require<Material>("Assets/TruckHotPotato/Materials/Festival scorched clay.mat"); paving.mainTexture = source.mainTexture; paving.mainTextureScale = Vector2.one * 35;
            var floor = SportsEventAuthoring.Box("Continuous original NWH festival driving floor", new Vector3(0, -.5f, 0), new Vector3(164, 1, 164), paving, contact, root);
            SportsDrivingSurfaceAuthoring.MarkHierarchy(floor.transform);
            var cream = Mat("Warm hand painted carnival ivory", new Color(.94f, .85f, .62f), .14f, true); var dark = Mat("Dark carved festival timber", new Color(.075f, .032f, .02f), .2f);
            var surround = Mat("Vista grass beyond festival", new Color(.26f, .48f, .17f), .1f);
            var terrain = Require<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer"); surround.mainTexture = terrain.diffuseTexture; surround.mainTextureScale = Vector2.one * 70;
            SportsEventAuthoring.Box("Finished meadow surround", new Vector3(0, -1.2f, 0), new Vector3(700, 1, 700), surround, contact, root);
            PinataArt.Model("Painted multiband competition apron", root, PinataArt.Ring("Ivory inset competition apron", 64, 67, .025f), cream, Vector3.zero);
            var wall = PinataArt.Model("Rounded continuous festival perimeter", root, PinataArt.Ring("Rounded festival safety perimeter", 76, 78, 1.5f), paper[2], Vector3.zero);
            var wallCollider = wall.AddComponent<MeshCollider>(); wallCollider.sharedMesh = wall.GetComponent<MeshFilter>().sharedMesh; wallCollider.sharedMaterial = contact;
            for (int i = 0; i < 28; i++)
            {
                float angle = i * Mathf.PI * 2 / 28; var p = new Vector3(Mathf.Cos(angle) * 77, 1.55f, Mathf.Sin(angle) * 77);
                var stripe = SportsEventAuthoring.Box("Hand painted perimeter accent " + i, p, new Vector3(3.8f, .12f, 1.5f), paper[i % 7], null, root); stripe.transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            }
            // The crossbeam and rope are physical suspension geometry, placed
            // outside the central run-up. No moving invisible anchor is authored.
            for (int side = -1; side <= 1; side += 2)
            {
                var pole = PinataArt.Model("Carved suspension tower " + side, root, PinataArt.Tube("Carved suspension tower column", .65f, 16), dark, new Vector3(side * 13, 8, 0)); var collider = pole.AddComponent<CapsuleCollider>(); collider.radius = .65f; collider.height = 16; collider.sharedMaterial = contact;
                for (int band = 0; band < 5; band++) PinataArt.Model("Wrapped decorative tower band", pole.transform, PinataArt.Ring("Tower decorative band", .65f, .74f, .28f, 32), paper[band], new Vector3(0, band * 2.5f - 5, 0));
            }
            SportsEventAuthoring.Box("Laminated actual pinata suspension beam", new Vector3(0, 15.8f, 0), new Vector3(28, 1.2f, 1.5f), dark, contact, root);
            var lights = new List<Light>();
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12; var pos = new Vector3(Mathf.Cos(a) * 89, 0, Mathf.Sin(a) * 89);
                var booth = new GameObject("Detailed festive confection kiosk " + i).transform; booth.SetParent(root, false); booth.localPosition = pos; booth.rotation = Quaternion.LookRotation(-pos);
                SportsEventAuthoring.Box("Painted wood service counter", new Vector3(0, 1.1f, 0), new Vector3(7, 2.2f, 4.8f), paper[i % 7], null, booth);
                SportsEventAuthoring.Box("Worn counter top", new Vector3(0, 2.3f, -.1f), new Vector3(7.5f, .25f, 5.1f), cream, null, booth);
                for (int side = -1; side <= 1; side += 2) SportsEventAuthoring.Box("Carved canopy upright", new Vector3(side * 3.4f, 3, 1.9f), new Vector3(.22f, 6, .22f), dark, null, booth);
                PinataArt.Model("Scalloped sewn tissue canopy", booth, PinataArt.Canopy(), paper[(i + 3) % 7], new Vector3(0, 5.5f, 0));
                for (int shelf = 0; shelf < 5; shelf++) { var sweet = PinataArt.Model("Decorative embossed confection display", booth, PinataArt.GoldenSweet(), cream, new Vector3(-2 + shelf, 2.9f, -.8f)); sweet.transform.localScale = Vector3.one * .5f; }
                var bunting = PinataArt.Model("Sewn festival bunting " + i, root, PinataArt.Pennants(), paper[(i + 1) % 7], new Vector3(Mathf.Cos(a) * 64, 11, Mathf.Sin(a) * 64)); bunting.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90, 0); bunting.transform.localScale = new Vector3(2.5f, 1.2f, 1);
                var post = PinataArt.Model("Tall festoon support " + i, root, PinataArt.Tube("Festoon timber pole", .2f, 12), dark, new Vector3(Mathf.Cos(a) * 69, 6, Mathf.Sin(a) * 69));
                var bulb = new GameObject("Warm bounded festive lamp " + i).AddComponent<Light>(); bulb.transform.SetParent(post.transform, false); bulb.transform.localPosition = Vector3.up * 5.5f; bulb.type = LightType.Point; bulb.color = Palette[i % 7]; bulb.intensity = 1.3f; bulb.range = 19; bulb.shadows = LightShadows.None; lights.Add(bulb);
                PinataArt.Model("Modeled warm lantern shade", post.transform, PinataArt.Tube("Folded paper lantern shade", .55f, .85f, 12), cream, Vector3.up * 5.3f);
            }
            match.festival.festoonLights = lights.ToArray(); Crowd(match, root, dark);
            var board = new GameObject("Festival actual typed score and shell board").AddComponent<TextMeshPro>(); board.transform.SetParent(root, false); board.transform.localPosition = new Vector3(0, 13, 77); board.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; board.rectTransform.sizeDelta = new Vector2(50, 7); board.fontSize = 24; board.enableAutoSizing = true; board.fontSizeMin = 10; board.fontSizeMax = 24; board.color = cream.color; board.alignment = TextAlignmentOptions.Center; match.festival.arenaBoard = board;
            SportsEventAuthoring.Box("Carved scoreboard enclosure", new Vector3(0, 13, 77.6f), new Vector3(53, 9, 1), dark, null, root);
            var music = match.gameObject.AddComponent<AudioSource>(); music.clip = Require<AudioClip>("Assets/TruckMusicalParking/Audio/Original Parking Polka.wav"); music.loop = true; music.playOnAwake = false; music.spatialBlend = 0; match.festival.music = music;
            match.festival.confetti = ParticleCopy("Assets/TruckSports/FinishPresentation/Existing Balloon paper confetti.prefab", "Pooled existing cut-paper celebration");
            match.festival.blast = ParticleCopy("Assets/CoreCollapse/Demo/Prefabs/Particles/Explosion.prefab", "Pooled CoreCollapse explosive sweet effect");
            match.festival.paperDust = ParticleCopy("Assets/CoreCollapse/Demo/Prefabs/Particles/Dust.prefab", "Pooled CoreCollapse paper impact dust");
            match.festival.sweetSparkle = ParticleCopy("Assets/TruckSports/FinishPresentation/Existing Balloon paper confetti.prefab", "Pooled golden sweet glints");
            foreach (var system in new[] { match.festival.confetti, match.festival.blast, match.festival.paperDust, match.festival.sweetSparkle })
            {
                foreach (var child in system.GetComponentsInChildren<ParticleSystem>(true)) { child.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var main = child.main; main.loop = false; main.playOnAwake = false; main.maxParticles = Mathf.Min(main.maxParticles, 160); }
                system.transform.position = Vector3.up * 5;
            }
        }
        static ParticleSystem ParticleCopy(string path, string name)
        {
            var asset = Require<GameObject>(path); if (asset.GetComponentsInChildren<Rigidbody>(true).Length != 0 || asset.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Particle-only foundation required: " + path);
            var copy = UnityEngine.Object.Instantiate(asset); copy.name = name;
            foreach (var script in copy.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
            var root = copy.GetComponent<ParticleSystem>();
            if (root == null)
            {
                // Container coordinates the existing child particle systems; do
                // not add a default white-particle substitute to the vendor art.
                root = copy.AddComponent<ParticleSystem>(); var emission = root.emission; emission.enabled = false; root.GetComponent<ParticleSystemRenderer>().enabled = false;
            }
            root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var main = root.main; main.loop = false; main.playOnAwake = false;
            return root;
        }
        static void Crowd(PinataMatch match, Transform root, Material wood)
        {
            var controller = Require<RuntimeAnimatorController>("Assets/TruckFieldGoal/Crowd/Stadium reactions.controller");
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/TruckPedestrianPlowing/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).Take(6).ToArray();
            if (paths.Length == 0) throw new InvalidOperationException("Existing rigged Taxi audience models required.");
            var crowd = match.gameObject.AddComponent<FieldGoalCrowd>(); crowd.eventSession = match; crowd.batches = Array.Empty<FieldGoalCrowd.Batch>(); crowd.nearFans = new Animator[24]; crowd.runnerChance = 0;
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24; var p = new Vector3(Mathf.Cos(a) * 82, 1.9f, Mathf.Sin(a) * 82);
                var terrace = SportsEventAuthoring.Box("Finished small timber spectator terrace " + i, p - Vector3.up, new Vector3(7, 1.8f, 3), wood, null, root); terrace.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                crowd.nearFans[i] = SportsAudienceAuthoring.Spawn(Require<GameObject>(paths[i % paths.Length]), root, p, new Vector3(0, p.y, 0), controller, 1.2f);
            }
            crowd.response = match.gameObject.AddComponent<AudioSource>(); crowd.response.playOnAwake = false; crowd.cheer = Require<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav"); crowd.boo = Require<AudioClip>("Assets/TruckDockDiving/Audio/Playful crowd boo.wav"); match.festival.crowd = crowd;
        }
    }
}
