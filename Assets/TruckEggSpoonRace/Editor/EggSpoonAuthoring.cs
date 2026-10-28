using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;

namespace LWS.TruckEggSpoonRace.Editor
{
    public static class EggSpoonAuthoring
    {
        public const string Folder = "TruckEggSpoonRace", Scene = "Assets/TruckEggSpoonRace/Scenes/TruckEggSpoonRace.unity";
        [MenuItem("Truck Sports/Impossible Games/Create NEW Egg And Spoon Race")]
        public static void Create()
        {
            ImpossibleBalanceArt.GuardNew(Scene);
            var m = SportsEventAuthoring.Begin<EggSpoonMatch>(Folder); m.setup.count = 4; m.option = 0; m.NormalizeSetup();
            var metal = ImpossibleBalanceArt.Material(Folder, "Brushed country cutlery", new Color(.65f, .72f, .74f), .78f, .85f);
            var shell = ImpossibleBalanceArt.Material(Folder, "Warm speckled ivory shell", new Color(.96f, .88f, .69f), .26f);
            var wood = ImpossibleBalanceArt.Material(Folder, "Honey bridge timber", new Color(.39f, .22f, .09f), .18f);
            var red = ImpossibleBalanceArt.Material(Folder, "Heritage red barn siding", new Color(.44f, .065f, .035f), .2f);
            var dirt = ImpossibleBalanceArt.Material(Folder, "Country dirt road", new Color(.38f, .29f, .15f), .12f);
            var stone = ImpossibleBalanceArt.Material(Folder, "Country barn slate", new Color(.09f, .13f, .15f), .24f);
            var block = ImpossibleBalanceArt.Mesh(Folder, "Crafted bevelled farm timber", ImpossibleBalanceArt.RoundedBlock);
            var bowl = ImpossibleBalanceArt.Mesh(Folder, "Physical concave cutlery bowl", BowlMesh);
            var eggMesh = ImpossibleBalanceArt.Mesh(Folder, "Original tapered giant egg", () =>
            {
                var profile = new Vector2[25]; for (int i = 0; i < profile.Length; i++) { float a = i * Mathf.PI / 24; profile[i] = new Vector2(Mathf.Sin(a) * (.85f + Mathf.Cos(a) * .13f), -Mathf.Cos(a) * 1.05f); } return ImpossibleBalanceArt.Lathe(profile, 40);
            });
            var contact = SportsEventAuthoring.Contact(Folder, "Egg and cutlery dry contact", .17f, .015f);
            Countryside(m, dirt, wood, red, stone, block);
            m.racers = new EggSpoonRacer[4];
            var gear = new GameObject("Explicit outside-tractor egg and spoon assemblies").transform;
            for (int slot = 0; slot < 4; slot++)
            {
                var truck = ImpossibleBalanceArt.Tractor(m, slot, EggSpoonCourse.Point(slot, 0));
                var racer = truck.gameObject.AddComponent<EggSpoonRacer>(); racer.match = m; racer.truck = truck; racer.slot = slot;
                racer.originalAttachedBodies = truck.GetComponentsInChildren<Rigidbody>(true).Where(b => b != truck.body).ToArray();
                racer.spawnLift = .08f - truck.wheels.Min(w => truck.transform.InverseTransformPoint(w.transform.position).y - w.wheel.radius - w.spring.maxLength);
                truck.transform.position += Vector3.up * racer.spawnLift;
                racer.localAnchor = new Vector3(0, truck.shape.center.y + truck.shape.size.y * .5f + .7f, truck.shape.center.z + truck.shape.size.z * .5f + .6f);
                racer.gearBase = new GameObject("Owned cutlery mast " + slot).transform; racer.gearBase.SetParent(gear, false); racer.gearBase.SetPositionAndRotation(truck.transform.position, truck.transform.rotation);
                float mastHeight = Mathf.Max(2, racer.localAnchor.y - 1);
                ImpossibleBalanceArt.Part(racer.gearBase, "Chrome chassis mounted spoon mast", block, metal, racer.localAnchor - Vector3.up * mastHeight * .5f, new Vector3(.3f, mastHeight, .45f));
                ImpossibleBalanceArt.Part(racer.gearBase, "Cutlery gimbal crosshead", block, metal, racer.localAnchor, new Vector3(1.1f, .25f, .7f));
                racer.spoon = new GameObject("Actual adjustable physical spoon " + slot).transform; racer.spoon.SetParent(gear, false); racer.spoon.position = truck.transform.TransformPoint(racer.localAnchor);
                racer.spoonBody = racer.spoon.gameObject.AddComponent<Rigidbody>(); racer.spoonBody.isKinematic = true; racer.spoonBody.interpolation = RigidbodyInterpolation.Interpolate; racer.spoonBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                var visual = ImpossibleBalanceArt.Part(racer.spoon, "Double skinned stainless concave bowl", bowl, metal, Vector3.zero, Vector3.one);
                racer.bowlCollider = visual.AddComponent<MeshCollider>(); racer.bowlCollider.sharedMesh = bowl; racer.bowlCollider.convex = false; racer.bowlCollider.sharedMaterial = contact;
                ImpossibleBalanceArt.Part(racer.spoon, "Embossed stainless spoon handle", block, metal, new Vector3(0, -.04f, 2), new Vector3(.7f, .2f, 6));
                var eggRoot = new GameObject("Actual free physical egg " + slot); eggRoot.transform.SetParent(gear, false); eggRoot.transform.position = racer.BowlPosition + Vector3.up * 1.17f;
                var egg = eggRoot.AddComponent<FragileRaceEgg>(); egg.racer = racer; egg.body = eggRoot.AddComponent<Rigidbody>(); egg.body.mass = 30; egg.body.linearDamping = .06f; egg.body.angularDamping = .12f; egg.body.isKinematic = true; egg.body.interpolation = RigidbodyInterpolation.Interpolate; egg.body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; egg.body.solverIterations = 12;
                var collider = eggRoot.AddComponent<CapsuleCollider>(); collider.radius = .8f; collider.height = 2.1f; collider.sharedMaterial = contact; egg.shell = collider;
                egg.model = ImpossibleBalanceArt.Part(eggRoot.transform, "Smooth tapered giant eggshell", eggMesh, shell, Vector3.zero, Vector3.one).GetComponent<Renderer>();
                EggEffects(gear, egg, shell); racer.egg = egg; m.racers[slot] = racer;
            }
            m.broadcast = SportsEventAuthoring.Camera("Country egg balancing broadcast"); m.broadcast.Pose(new Vector3(0, 5, 15), new Vector3(22, 22, -34), 60, true); m.cameras = new[] { m.broadcast.view };
            m.feedback.clips = (AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone(); ImpossibleBalanceArt.UI(m);
            m.drivingViews = SportsDrivingViewsAuthoring.Install(m, m.racers.Select(r => r.truck).ToArray(), m.broadcast, Folder); m.drivingViews.defaultMode = 1; m.drivingViews.independentViewModes = true;
            m.drivingViews.rally.groundOffset = new Vector3(0, 14, -25); m.drivingViews.rally.groundLookAhead = 14;
            SportsEventAuthoring.Save(Folder);
            SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(m, m.racers.Select(r => r.truck).ToArray(), m.racers.SelectMany(r => new[] { r.gearBase, r.spoon, r.egg.transform }).ToArray());
            EditorSceneManager.MarkSceneDirty(m.gameObject.scene); EditorSceneManager.SaveScene(m.gameObject.scene); AssetDatabase.SaveAssets();
        }
        public static Mesh BowlMesh()
        {
            const int rings = 10, segments = 40; var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int layer = 0; layer < 2; layer++) for (int row = 0; row <= rings; row++) for (int angle = 0; angle <= segments; angle++)
            {
                float r = row / (float)rings, a = angle * Mathf.PI * 2 / segments;
                vertices.Add(EggSpoonRacer.BowlCenter + new Vector3(Mathf.Sin(a) * r * 2.2f, r * r * .72f - layer * .13f, Mathf.Cos(a) * r * 2.2f)); uv.Add(new Vector2(angle / (float)segments, r));
            }
            int layerSize = (rings + 1) * (segments + 1);
            for (int layer = 0; layer < 2; layer++) for (int row = 0; row < rings; row++) for (int angle = 0; angle < segments; angle++)
            {
                int a = layer * layerSize + row * (segments + 1) + angle, b = a + segments + 1;
                if (layer == 0) triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); else triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
            for (int i = 0; i < segments; i++) { int a = rings * (segments + 1) + i, b = a + layerSize; triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); return mesh;
        }
        static void Countryside(EggSpoonMatch m, Material dirt, Material wood, Material barn, Material roof, Mesh block)
        {
            var floorMaterial = ImpossibleBalanceArt.Material(Folder, "Vista country meadow", new Color(.22f, .36f, .12f), .1f);
            var terrain = new TerrainData { name = "Rolling country meadow", heightmapResolution = 129, size = new Vector3(250, 25, 310) };
            var heights = new float[129, 129]; for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++) { float worldX = x / 128f * 250 - 125; float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(28, 100, Mathf.Abs(worldX))); heights[z, x] = edge * (.12f + Mathf.PerlinNoise(x * .1f, z * .08f) * .45f); }
            terrain.SetHeights(0, 0, heights); terrain.terrainLayers = new[] { AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer") };
            string path = "Assets/" + Folder + "/Materials/Country meadow.asset"; if (AssetDatabase.LoadAssetAtPath<TerrainData>(path) != null) throw new InvalidOperationException("Preserve existing owned country terrain."); AssetDatabase.CreateAsset(terrain, path);
            var ground = Terrain.CreateTerrainGameObject(terrain).GetComponent<Terrain>(); ground.transform.position = new Vector3(-125, -.3f, -40); ground.drawInstanced = true; floorMaterial.shader = Shader.Find("Universal Render Pipeline/Terrain/Lit"); ground.materialTemplate = floorMaterial;
            var contact = SportsEventAuthoring.Contact(Folder, "Country NWH road contact", .75f, .01f);
            var scenery = new GameObject("Country farm scenery bridges service stations and woodland").transform;
            for (int slot = 0; slot < 4; slot++)
            {
                var points = Enumerable.Range(0, 27).Select(i => EggSpoonCourse.Point(slot, -25 + i * 9)).ToArray();
                SportsEasyRoadsAuthoring.Bake(Folder, "Country lane " + slot, points, 8.6f, dirt, contact);
                foreach (float z in new[] { 0f, 45, 90, 135, 180 })
                {
                    var p = EggSpoonCourse.Point(slot, z);
                    for (int side = -1; side <= 1; side += 2) { ImpossibleBalanceArt.Part(scenery, "Carved checkpoint signpost", block, wood, p + new Vector3(side * 4.2f, 1.7f, 0), new Vector3(.25f, 3.5f, .25f)); }
                    var label = SportsEventAuthoring.Label("Egg service station", z == 0 ? "START · FRESH EGGS" : z == 180 ? "FINISH" : "EGG STATION " + (z / 45).ToString("0"), p + new Vector3(0, 4, 0), .12f, new Color(1, .9f, .65f)); label.gameObject.AddComponent<SportsPerCameraBillboard>();
                }
                for (int side = -1; side <= 1; side += 2) for (int row = 0; row < 8; row++)
                {
                    float z = 65 + row * 2; var p = EggSpoonCourse.Point(slot, z) + new Vector3(side * 4.3f, 0, 0);
                    var post = ImpossibleBalanceArt.Part(scenery, "Wooden bridge guard post", block, wood, p + Vector3.up, new Vector3(.23f, 2, .23f)); post.AddComponent<BoxCollider>();
                    ImpossibleBalanceArt.Part(scenery, "Bridge side rail", block, wood, p + new Vector3(0, 1.4f, 1), new Vector3(.2f, .25f, 2.3f));
                    ImpossibleBalanceArt.Part(scenery, "Bridge timber pier", block, wood, p - Vector3.up, new Vector3(.4f, 2.3f, .4f));
                }
                for (int row = 0; row < 16; row++)
                {
                    var p = EggSpoonCourse.Point(slot, 65 + row) + Vector3.up * .035f;
                    var plank = ImpossibleBalanceArt.Part(scenery, "Physical timber bridge deck plank", block, wood, p, new Vector3(8.2f, .06f, .96f)); plank.AddComponent<BoxCollider>().sharedMaterial = contact;
                }
            }
            for (int i = 0; i < 48; i++) { float z = -10 + i / 2 * 9; var p = new Vector3((i % 2 == 0 ? -1 : 1) * (32 + i % 5 * 3), 0, z); p.y = ground.SampleHeight(p) + ground.transform.position.y; LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(scenery, p, i % 3 == 0 ? 3.5f : 7 + i % 4, i % 3 != 0, Folder); }
            // Original crafted farm buildings fill the audited missing barn prop.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 at = new Vector3(side * 42, .3f, 95 + side * 35); ImpossibleBalanceArt.Part(scenery, "Red timber farm barn", block, barn, at + Vector3.up * 3, new Vector3(12, 6, 17));
                for (int slope = -1; slope <= 1; slope += 2) ImpossibleBalanceArt.Part(scenery, "Pitched slate barn roof", block, roof, at + new Vector3(slope * 3.2f, 7, 0), new Vector3(7.6f, .35f, 18), Quaternion.Euler(0, 0, slope * 28));
                ImpossibleBalanceArt.Part(scenery, "Inset double barn doors", block, wood, at + new Vector3(0, 2, -8.55f), new Vector3(4.5f, 4, .18f));
                for (int i = 0; i < 15; i++) ImpossibleBalanceArt.Part(scenery, "Vertical barn siding batten", block, wood, at + new Vector3(-5.6f + i * .8f, 3, -8.6f), new Vector3(.09f, 5.8f, .12f));
            }
            var controller = SportsAudienceAuthoring.DanceController(Folder); var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab");
            for (int i = 0; i < 16; i++) SportsAudienceAuthoring.Spawn(prefab, scenery, new Vector3(i % 2 == 0 ? -26 : 26, .1f, 165 + i / 2 * 3), new Vector3(0, .1f, 175), controller, 1);
        }
        static void EggEffects(Transform parent, FragileRaceEgg egg, Material shell)
        {
            var chipMesh = ImpossibleBalanceArt.Mesh(Folder, "Original curved shell chip", () =>
            {
                var mesh = new Mesh(); mesh.vertices = new[] { new Vector3(-.3f, 0, -.2f), new Vector3(.3f, 0, -.2f), new Vector3(0, .1f, .35f) }; mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up }; mesh.triangles = new[] { 0, 2, 1, 0, 1, 2 }; return mesh;
            });
            egg.chips = Effect(parent, "Pooled real-impact shell chips", shell, chipMesh, 22, .3f, 3);
            var gold = ImpossibleBalanceArt.Material(Folder, "Fresh golden yolk", new Color(1, .56f, .015f), .85f);
            var yolkMesh = ImpossibleBalanceArt.Mesh(Folder, "Original yolk droplets", ImpossibleBalanceArt.Ovoid);
            egg.yolk = Effect(parent, "Pooled real-impact golden yolk", gold, yolkMesh, 10, .22f, 1.6f);
        }
        static ParticleSystem Effect(Transform parent, string name, Material material, Mesh mesh, int count, float size, float speed)
        {
            var fx = new GameObject(name).AddComponent<ParticleSystem>(); fx.transform.SetParent(parent, false); fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = fx.main; main.playOnAwake = false; main.loop = false; main.maxParticles = 32; main.startLifetime = 2.5f; main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .4f, speed); main.startSize = new ParticleSystem.MinMaxCurve(size * .6f, size * 1.4f); main.gravityModifier = .8f; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = fx.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) }); var shape = fx.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = .15f;
            var renderer = fx.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = mesh; renderer.sharedMaterial = material;
            return fx;
        }
    }
}
