using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;
namespace LWS.TruckNestBuilding.Editor
{
    public static class NestAuthoring
    {
        public const string Folder = "TruckNestBuilding", Scene = "Assets/TruckNestBuilding/Scenes/TruckNestBuilding.unity";
        [MenuItem("Truck Sports/Impossible Games/Create NEW Nest Building")]
        public static void Create()
        {
            ImpossibleBalanceArt.GuardNew(Scene);
            var m = SportsEventAuthoring.Begin<NestMatch>(Folder); m.setup.count = 4; m.option = 0; m.NormalizeSetup();
            var bark = ImpossibleBalanceArt.Material(Folder, "Forest branch bark", new Color(.25f, .13f, .065f), .13f);
            var stick = ImpossibleBalanceArt.Material(Folder, "Fresh pale sticks", new Color(.52f, .34f, .17f), .15f);
            var leaf = ImpossibleBalanceArt.Material(Folder, "Woven broadleaf lining", new Color(.23f, .43f, .08f), .15f);
            var moss = ImpossibleBalanceArt.Material(Folder, "Protective soft moss", new Color(.12f, .28f, .075f), .1f);
            var gold = ImpossibleBalanceArt.Material(Folder, "Wildflower decoration", new Color(1, .65f, .08f), .24f);
            var metal = ImpossibleBalanceArt.Material(Folder, "Forest builder telescopic beak", new Color(.32f, .39f, .43f), .65f, .7f);
            var timber = ImpossibleBalanceArt.Material(Folder, "Carved tree tower timber", new Color(.34f, .22f, .11f), .2f);
            var block = ImpossibleBalanceArt.Mesh(Folder, "Carved bevelled forest timber", ImpossibleBalanceArt.RoundedBlock);
            var oval = ImpossibleBalanceArt.Mesh(Folder, "Smooth original nest eggs and buds", ImpossibleBalanceArt.Ovoid);
            var branch = ImpossibleBalanceArt.Mesh(Folder, "Six metre natural interlocking branch", () => Wood(6, .28f));
            var twig = ImpossibleBalanceArt.Mesh(Folder, "Five metre nest weaving stick", () => Wood(5.4f, .2f));
            var foliage = ImpossibleBalanceArt.Mesh(Folder, "Layered organic foliage bundle", Foliage);
            Forest(m);
            var physics = new GameObject("Explicit physical nest materials and building hands").transform;
            var contact = SportsEventAuthoring.Contact(Folder, "Dry nest wood contact", .65f, .015f);
            m.sites = new NestBuildSite[4];
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject("Elevated tree nest platform " + i); root.transform.position = new Vector3((i - 1.5f) * 32, 6, 30);
                var site = root.AddComponent<NestBuildSite>(); site.index = i; site.competitiveHome = root.transform.position; site.body = root.AddComponent<Rigidbody>(); site.body.isKinematic = true;
                var deck = ImpossibleBalanceArt.Part(root.transform, "Supported timber tree platform", block, timber, Vector3.down * .25f, new Vector3(12, .5f, 12)); site.deck = deck.AddComponent<BoxCollider>(); site.deck.sharedMaterial = contact;
                for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) ImpossibleBalanceArt.Part(root.transform, "Tree tower structural leg", block, timber, new Vector3(x * 4.7f, -3.1f, z * 4.7f), new Vector3(.5f, 6.3f, .5f));
                LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(root.transform, root.transform.position + new Vector3(9, -6, 6), 32, true, Folder);
                for (int socket = 0; socket < 12; socket++)
                {
                    var mark = SportsEventAuthoring.Label("Nest construction guide " + socket, (socket + 1) + " " + NestBuildSite.Kind(socket).ToString().ToUpperInvariant(), site.Target(socket) + Vector3.up * .25f, .075f, new Color(.8f, 1, .66f)); mark.transform.SetParent(root.transform, true); mark.gameObject.AddComponent<SportsPerCameraBillboard>();
                }
                Family(m, site, physics, oval); m.sites[i] = site;
            }
            var all = new System.Collections.Generic.List<NestMaterial>();
            for (int depot = 0; depot < 4; depot++) for (int n = 0; n < 12; n++)
            {
                var kind = NestBuildSite.Kind(n); var p = new GameObject("Physical " + kind + " · depot " + depot + " part " + n); p.transform.SetParent(physics, false);
                var piece = p.AddComponent<NestMaterial>(); piece.pieceId = "depot" + depot + ".piece" + n; piece.kind = kind; piece.body = p.AddComponent<Rigidbody>(); piece.body.isKinematic = true; piece.body.mass = kind == NestMaterialKind.Branch ? 6 : kind == NestMaterialKind.Stick ? 3 : 1; piece.body.linearDamping = .12f; piece.body.angularDamping = .3f; piece.body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; piece.body.interpolation = RigidbodyInterpolation.Interpolate; piece.body.solverIterations = 12;
                if (kind == NestMaterialKind.Branch || kind == NestMaterialKind.Stick)
                {
                    ImpossibleBalanceArt.Part(p.transform, "Actual wood body", kind == NestMaterialKind.Branch ? branch : twig, kind == NestMaterialKind.Branch ? bark : stick, Vector3.zero, Vector3.one, Quaternion.Euler(90, 0, 0));
                    var capsule = p.AddComponent<CapsuleCollider>(); capsule.direction = 2; capsule.radius = kind == NestMaterialKind.Branch ? .28f : .2f; capsule.height = kind == NestMaterialKind.Branch ? 6 : 5.4f; capsule.sharedMaterial = contact; piece.shape = capsule;
                }
                else
                {
                    var size = kind == NestMaterialKind.Leaves ? new Vector3(4.2f, .1f, 4.2f) : kind == NestMaterialKind.Moss ? new Vector3(3.8f, .2f, 3.8f) : new Vector3(.8f, .3f, .8f);
                    ImpossibleBalanceArt.Part(p.transform, "Actual cohesive " + kind + " bundle", foliage, kind == NestMaterialKind.Leaves ? leaf : kind == NestMaterialKind.Moss ? moss : gold, Vector3.zero, size);
                    var box = p.AddComponent<BoxCollider>(); box.size = size; box.sharedMaterial = contact; piece.shape = box;
                }
                float ground = kind == NestMaterialKind.Branch ? .30f : kind == NestMaterialKind.Stick ? .22f : kind == NestMaterialKind.Moss ? .12f : kind == NestMaterialKind.Leaves ? .07f : .17f;
                piece.home = new Vector3((depot - 1.5f) * 32 + 8 + n % 3 * 7, ground, -46 + n / 3 * 6); piece.pickupStand = new Vector3((depot - 1.5f) * 32, 0, piece.home.z - 14); p.transform.position = piece.home; all.Add(piece);
            }
            m.materials = all.ToArray(); m.builders = new NestBuilder[4];
            for (int i = 0; i < 4; i++)
            {
                var truck = ImpossibleBalanceArt.Tractor(m, i, new Vector3((i - 1.5f) * 32, 0, -75));
                float lift = .08f - truck.wheels.Min(w => truck.transform.InverseTransformPoint(w.transform.position).y - w.wheel.radius - w.spring.maxLength); truck.transform.position += Vector3.up * lift;
                var builder = truck.gameObject.AddComponent<NestBuilder>(); builder.match = m; builder.truck = truck; builder.slot = i; builder.spawn = truck.transform.position;
                builder.originalAttachedBodies = truck.GetComponentsInChildren<Rigidbody>(true).Where(b => b != truck.body).ToArray();
                builder.carryHome = new Vector3(0, 3, truck.shape.center.z + truck.shape.size.z * .5f + 4.5f);
                var hand = new GameObject("Actual finite-force construction gripper " + i); hand.transform.SetParent(physics, false); hand.transform.position = truck.transform.TransformPoint(builder.carryHome); builder.hand = hand.AddComponent<Rigidbody>(); builder.hand.isKinematic = true; builder.hand.interpolation = RigidbodyInterpolation.Interpolate; builder.hand.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                ImpossibleBalanceArt.Part(hand.transform, "Machined curved construction beak", oval, metal, Vector3.zero, new Vector3(.4f, .3f, .7f));
                builder.boom = ImpossibleBalanceArt.Part(physics, "Telescopic forest construction boom " + i, block, metal, Vector3.zero, Vector3.one).transform;
                m.builders[i] = builder;
            }
            Birds(m);
            m.broadcast = SportsEventAuthoring.Camera("Forest nest construction broadcast"); m.broadcast.Pose(new Vector3(0, 6, 20), new Vector3(27, 25, -42), 60, true); m.cameras = new[] { m.broadcast.view };
            m.feedback.clips = (AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone(); ImpossibleBalanceArt.UI(m);
            m.drivingViews = SportsDrivingViewsAuthoring.Install(m, m.builders.Select(b => b.truck).ToArray(), m.broadcast, Folder); m.drivingViews.defaultMode = 1; m.drivingViews.independentViewModes = true; m.drivingViews.rally.groundOffset = new Vector3(0, 14, -27); m.drivingViews.rally.groundLookAhead = 14;
            SportsUIAuthoring.Button(m.menus.pages[(int)SportsEventScreen.Pause].transform, "SAVE WORKSHOP", "nest-save", new Vector2(-350, -340), new Vector2(520, 48));
            SportsUIAuthoring.Button(m.menus.pages[(int)SportsEventScreen.Pause].transform, "NEW WORKSHOP", "nest-new", new Vector2(350, -340), new Vector2(520, 48));
            m.finishWorkshop = SportsUIAuthoring.Button(m.menus.pages[(int)SportsEventScreen.Pause].transform, "FINISH WORKSHOP", "nest-finish", new Vector2(0, -410), new Vector2(560, 42)).gameObject;
            SportsEventAuthoring.Save(Folder); SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(m, m.builders.Select(b => b.truck).ToArray(), m.builders.SelectMany(b => new[] { b.hand.transform, b.boom }).ToArray());
            EditorSceneManager.MarkSceneDirty(m.gameObject.scene); EditorSceneManager.SaveScene(m.gameObject.scene); AssetDatabase.SaveAssets();
        }
        public static Mesh Wood(float length, float radius)
        {
            var profile = new Vector2[13]; for (int i = 0; i < profile.Length; i++) { float u = i / 12f; profile[i] = new Vector2(i == 0 || i == 12 ? 0 : radius * (.92f + Mathf.Sin(i * 2.1f) * .06f), (u - .5f) * length); } return ImpossibleBalanceArt.Lathe(profile, 16);
        }
        public static Mesh Foliage()
        {
            // Extruded, lobed woven leaf/moss bundle. Source shape is original.
            const int count = 32; var v = new Vector3[count * 2 + 2]; var uv = new Vector2[v.Length]; var t = new System.Collections.Generic.List<int>();
            for (int side = 0; side < 2; side++) for (int i = 0; i < count; i++) { float a = i * Mathf.PI * 2 / count, r = .46f + .035f * Mathf.Sin(a * 7); v[side * count + i] = new Vector3(Mathf.Sin(a) * r, side == 0 ? -.5f : .5f, Mathf.Cos(a) * r); uv[side * count + i] = new Vector2(v[side * count + i].x + .5f, v[side * count + i].z + .5f); }
            v[count * 2] = Vector3.down * .5f; v[count * 2 + 1] = Vector3.up * .5f;
            for (int i = 0; i < count; i++) { int next = (i + 1) % count; t.AddRange(new[] { count * 2, i, next, count * 2 + 1, count + next, count + i, i, count + i, next, next, count + i, count + next }); }
            for (int i = 0; i < t.Count; i += 3) { int swap = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = swap; }
            var mesh = new Mesh(); mesh.vertices = v; mesh.uv = uv; mesh.SetTriangles(t, 0); return mesh;
        }
        static void Family(NestMatch match, NestBuildSite site, Transform physics, Mesh oval)
        {
            var family = site.gameObject.AddComponent<NestFamily>(); site.family = family; family.match = match; family.site = site;
            var white = ImpossibleBalanceArt.Material(Folder, "Warm nesting eggshell", new Color(.97f, .89f, .72f), .28f);
            var egg = new GameObject("Actual supported family egg " + site.index); egg.transform.SetParent(physics, false); egg.transform.position = site.transform.position + Vector3.up * 3;
            family.eggBody = egg.AddComponent<Rigidbody>(); family.eggBody.mass = 2; family.eggBody.isKinematic = true; family.eggBody.interpolation = RigidbodyInterpolation.Interpolate; family.eggBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; family.eggBody.angularDamping = .4f;
            var shell = egg.AddComponent<CapsuleCollider>(); shell.radius = .5f; shell.height = 1.35f; shell.enabled = false; family.eggCollider = shell;
            family.eggRenderer = ImpossibleBalanceArt.Part(egg.transform, "Actual family egg shell", oval, white, Vector3.zero, new Vector3(1, 1.35f, 1)).GetComponent<Renderer>(); family.eggRenderer.enabled = false;
            egg.AddComponent<NestEggContact>().family = family;
            family.babies = new Transform[2]; for (int i = 0; i < 2; i++) { family.babies[i] = new GameObject("Animated miniature baby hauler " + i).transform; family.babies[i].SetParent(physics, false); family.babies[i].gameObject.SetActive(false); }
            family.tinyHorn = site.gameObject.AddComponent<AudioSource>(); family.tinyHorn.playOnAwake = false; family.tinyHorn.spatialBlend = 1; family.tinyHorn.maxDistance = 80; family.tinyHorn.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NWH/Vehicle Physics 2/Resources/NWH Vehicle Physics 2/Defaults/Sound/Horn.wav");
            var fx = new GameObject("Pooled hatching shell fragments").AddComponent<ParticleSystem>(); fx.transform.SetParent(physics, false); fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var main = fx.main; main.playOnAwake = false; main.loop = false; main.maxParticles = 24; main.startSize = .12f; main.startSpeed = 1; main.startLifetime = 1.5f; main.gravityModifier = .4f; var emission = fx.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)16) }); var renderer = fx.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = oval; renderer.sharedMaterial = white; family.shellBurst = fx;
        }
        static void Forest(NestMatch m)
        {
            var data = new TerrainData { name = "Forest nesting clearing", heightmapResolution = 129, size = new Vector3(300, 32, 220) }; var heights = new float[129, 129];
            for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++) { float px = x / 128f * 300 - 150, pz = z / 128f * 220 - 100; float r = Mathf.Max(Mathf.Abs(px) / 110, Mathf.Abs(pz) / 100); heights[z, x] = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.8f, 1.7f, r)) * (.15f + Mathf.PerlinNoise(x * .07f, z * .08f) * .7f); }
            data.SetHeights(0, 0, heights); data.terrainLayers = new[] { AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer") };
            string path = "Assets/" + Folder + "/Materials/Forest clearing.asset"; if (AssetDatabase.LoadAssetAtPath<TerrainData>(path) != null) throw new InvalidOperationException("Existing forest terrain is preserved."); AssetDatabase.CreateAsset(data, path);
            var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>(); terrain.transform.position = new Vector3(-150, -.02f, -100); terrain.drawInstanced = true; var mat = ImpossibleBalanceArt.Material(Folder, "Vista woodland floor", Color.white); mat.shader = Shader.Find("Universal Render Pipeline/Terrain/Lit"); terrain.materialTemplate = mat;
            var trees = new GameObject("Installed giant forest canopy and rocks").transform;
            for (int i = 0; i < 52; i++) { float a = i * 2.39996f; var p = new Vector3(Mathf.Sin(a) * (95 + i % 4 * 8), 0, 20 + Mathf.Cos(a) * (63 + i % 4 * 5)); p.y = terrain.SampleHeight(p) + terrain.transform.position.y; LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(trees, p, i % 5 == 0 ? 5 : 18 + i % 5 * 3, i % 5 != 0, Folder); }
        }
        static void Birds(NestMatch match)
        {
            const string root = "Assets/TruckBaseJumping/ThirdParty/QuaterniusBirds/";
            for (int i = 0; i < 6; i++)
            {
                bool eagle = i % 2 == 0; var source = AssetDatabase.LoadAssetAtPath<GameObject>(root + (eagle ? "Eagle.fbx" : "bird.fbx")); var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(root + (eagle ? "Eagle flight.controller" : "Small bird flight.controller"));
                if (source == null || controller == null || controller.animationClips.Length == 0) throw new InvalidOperationException("Existing CC0 Quaternius animated bird rig/clips required.");
                var actor = new GameObject("Genuine animated forest bird " + i); var model = (GameObject)PrefabUtility.InstantiatePrefab(source, actor.transform); var animator = model.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.avatar == null || !animator.avatar.isValid) throw new InvalidOperationException("Actual Generic bird avatar is required.");
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var flight = actor.AddComponent<NestBirdFlight>(); flight.match = match; flight.animator = animator; flight.center = new Vector3((i - 2.5f) * 15, 18 + i % 2 * 5, 32); flight.phase = i; flight.radius = 12 + i;
            }
        }
    }
}
