using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;

namespace LWS.TruckYoga.Editor
{
    public static class YogaAuthoring
    {
        public const string Folder = "TruckYoga", Scene = "Assets/TruckYoga/Scenes/TruckYoga.unity";
        [MenuItem("Truck Sports/Impossible Games/Create NEW Truck Yoga")]
        public static void Create()
        {
            ImpossibleBalanceArt.GuardNew(Scene);
            var m = SportsEventAuthoring.Begin<YogaMatch>(Folder); m.setup.count = 4; m.option = 0; m.NormalizeSetup();
            var mat = ImpossibleBalanceArt.Material(Folder, "Woven meadow yoga mats", new Color(.13f, .37f, .34f), .18f);
            var ivory = ImpossibleBalanceArt.Material(Folder, "Retreat limestone", new Color(.72f, .66f, .51f), .24f);
            var purple = ImpossibleBalanceArt.Material(Folder, "Instructor lavender mat", new Color(.32f, .16f, .42f), .2f);
            var mesh = ImpossibleBalanceArt.Mesh(Folder, "Bound stitched retreat mat", ImpossibleBalanceArt.RoundedBlock);
            Meadow(m);
            m.rigs = new YogaTruckRig[4];
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 10;
                var truck = ImpossibleBalanceArt.Tractor(m, i, new Vector3(x, 0, 0));
                float lowest = truck.wheels.Min(w => truck.transform.InverseTransformPoint(w.transform.position).y - w.wheel.radius - w.spring.maxLength);
                var rig = truck.gameObject.AddComponent<YogaTruckRig>(); rig.truck = truck; rig.slot = i; rig.home = new Vector3(x, .18f - lowest, 0); truck.transform.position = rig.home;
                rig.attachedBodies = truck.GetComponentsInChildren<Rigidbody>(true).Where(b => b != truck.body).ToArray();
                rig.cabArticulation = new GameObject("Owned render-only yoga twist control").transform; rig.cabArticulation.SetParent(truck.transform, false);
                // Source Cab is a real 1500kg rigidbody: no reparent or Transform
                // animation. Selected render-only body branch is posed at runtime.
                ImpossibleBalanceArt.Part(null, "Bound woven yoga mat " + (i + 1), mesh, mat, new Vector3(x, .08f, 0), new Vector3(8.3f, .16f, 12));
                m.rigs[i] = rig;
            }
            ImpossibleBalanceArt.Part(null, "Instructor lavender teaching mat", mesh, purple, new Vector3(0, .09f, 14), new Vector3(7, .18f, 5));
            Instructor(m); Wildlife(m);
            var rocks = new GameObject("Retreat stone lanterns and waterfall garden").transform;
            for (int i = 0; i < 8; i++)
            {
                float x = (i - 3.5f) * 5.8f;
                ImpossibleBalanceArt.Part(rocks, "Limestone mat edge marker", mesh, ivory, new Vector3(x, .2f, -8.2f), new Vector3(.7f, .35f, .7f));
            }
            Waterfall(rocks);
            m.broadcast = SportsEventAuthoring.Camera("Mountain meadow yoga broadcast"); m.broadcast.Pose(new Vector3(0, 2, 2), new Vector3(0, 24, -31), 57, true); m.cameras = new[] { m.broadcast.view };
            var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional); sun.transform.rotation = Quaternion.Euler(32, -24, 0); sun.color = new Color(1, .88f, .73f); sun.intensity = 1.8f;
            m.ambience = m.gameObject.AddComponent<AudioSource>(); m.ambience.playOnAwake = false; m.ambience.loop = true; m.ambience.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_MorningBirds.ogg"); m.ambience.spatialBlend = 0;
            m.feedback.clips = (AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();
            ImpossibleBalanceArt.UI(m); Hud(m);
            ImpossibleBalanceArt.Finish(m, Folder, m.rigs.Select(r => r.truck).ToArray());
        }
        static void Meadow(YogaMatch m)
        {
            var data = new TerrainData { name = "Yoga mountain meadow", heightmapResolution = 129, size = new Vector3(340, 55, 300) };
            var heights = new float[129, 129];
            for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++)
            {
                float px = x / 128f * 340 - 170, pz = z / 128f * 300 - 130;
                float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(42, 135, new Vector2(px, pz).magnitude));
                heights[z, x] = edge * (.18f + Mathf.PerlinNoise(x * .075f, z * .071f) * .65f);
            }
            data.SetHeights(0, 0, heights);
            data.terrainLayers = new[] { AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer") };
            if (data.terrainLayers[0] == null) throw new InvalidOperationException("Installed Vista meadow grass is required.");
            string path = "Assets/" + Folder + "/Materials/Mountain meadow.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(path) != null) throw new InvalidOperationException("Preserve existing retreat terrain; use a deliberate new asset path.");
            AssetDatabase.CreateAsset(data, path);
            var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>(); terrain.name = "Vista grass mountain meadow"; terrain.transform.position = new Vector3(-170, 0, -130); terrain.drawInstanced = true; terrain.heightmapPixelError = 5;
            var material = ImpossibleBalanceArt.Material(Folder, "Meadow terrain surface", Color.white); material.shader = Shader.Find("Universal Render Pipeline/Terrain/Lit"); terrain.materialTemplate = material;
            var scenery = new GameObject("Installed retreat trees and scenic rocks").transform;
            for (int i = 0; i < 38; i++)
            {
                float angle = i * 2.39996f, radius = 42 + i % 5 * 10; var position = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius + 10); position.y = terrain.SampleHeight(position);
                if (i % 3 == 0) YogaDecorativeAuthoring.AddMeadowRock(scenery, terrain, i);
                else LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(scenery, position, 7 + i % 5, true, Folder);
            }
        }
        static void Instructor(YogaMatch m)
        {
            var root = new GameObject("Animated yoga instructor · original UTS biped rig").transform;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab");
            var actor = SportsAudienceAuthoring.Spawn(prefab, root, new Vector3(0, .18f, 14), new Vector3(0, .18f, 0), null, 2);
            if (actor.avatar == null || !actor.avatar.isValid) throw new InvalidOperationException("Yoga instructor requires the actual installed valid biped avatar.");
            // UTS2.0 Girl_44 and its compatible clips are Generic (importer
            // animationType2), not Humanoid. Bind the actual named Biped bones;
            // do not call Animator.GetBoneTransform or reimport the vendor rig.
            var roles = new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg };
            var names = new[] { "Bip01 Pelvis", "Bip01 Spine", "Bip01 Spine2", "Bip01 Head", "Bip01 L UpperArm", "Bip01 R UpperArm", "Bip01 L Forearm", "Bip01 R Forearm", "Bip01 L Thigh", "Bip01 R Thigh", "Bip01 L Calf", "Bip01 R Calf" };
            var all = actor.GetComponentsInChildren<Transform>(true); var bones = names.Select(name => all.Single(b => b.name == name)).ToArray();
            var instructor = root.gameObject.AddComponent<YogaInstructor>(); instructor.match = m; instructor.animator = actor; instructor.roles = roles; instructor.bones = bones; m.instructor = instructor;
            actor.runtimeAnimatorController = null; actor.applyRootMotion = false; actor.enabled = false;
        }
        static void Wildlife(YogaMatch m)
        {
            const string path = "Assets/TruckFishing/CarnivalPresentation/Genuine animated ambient wolf.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Existing genuine animated WolfLite presentation required; no static substitute.");
            for (int i = 0; i < 2; i++)
            {
                var parent = new GameObject("Animated ambient wolf trail " + i); parent.transform.position = new Vector3(i == 0 ? -26 : 26, 0, 20);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform); var animator = visual.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null) throw new InvalidOperationException("Real wolf clips/rig are required.");
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var wildlife = parent.AddComponent<YogaWildlife>(); wildlife.match = m; wildlife.animator = animator;
                wildlife.route = new[] { parent.transform.position, parent.transform.position + Vector3.forward * 8, parent.transform.position + new Vector3(i == 0 ? 5 : -5, 0, 8), parent.transform.position + new Vector3(i == 0 ? 5 : -5, 0, 0) };
            }
        }
        static void Waterfall(Transform parent)
        {
            var material = ImpossibleBalanceArt.Material(Folder, "River Modeler flowing mountain cascade", new Color(.26f, .65f, .72f), .92f);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/Splash_Normals.png");
            if (normal == null) throw new InvalidOperationException("Installed River Modeler water normals required.");
            material.SetTexture("_BumpMap", normal); material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale", .5f);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/WaterFoam.png"));
            var mesh = ImpossibleBalanceArt.Mesh(Folder, "Curving waterfall sheet", () =>
            {
                var result = new Mesh(); var vertices = new Vector3[42]; var uv = new Vector2[42]; var indices = new int[120];
                for (int row = 0; row <= 20; row++) for (int side = 0; side < 2; side++) { float t = row / 20f; int k = row * 2 + side; vertices[k] = new Vector3((side == 0 ? -1 : 1) * (2 + t), (1 - t) * 18, t * t * 3); uv[k] = new Vector2(side, t * 4); }
                for (int row = 0; row < 20; row++) { int k = row * 2, q = row * 6; indices[q] = k; indices[q + 1] = k + 1; indices[q + 2] = k + 2; indices[q + 3] = k + 1; indices[q + 4] = k + 3; indices[q + 5] = k + 2; }
                result.vertices = vertices; result.uv = uv; result.triangles = indices; return result;
            });
            var cascade = ImpossibleBalanceArt.Part(parent, "Flowing River Modeler waterfall", mesh, material, new Vector3(-38, 0, 31), Vector3.one, Quaternion.Euler(0, 35, 0));
            cascade.AddComponent<LWS.TruckFishing.FishingWaterFlow>();
            YogaDecorativeAuthoring.CompleteWaterfall(parent, cascade.transform);
        }
        static void Hud(YogaMatch m)
        {
            m.menus.standings.gameObject.SetActive(false);
            var hud = m.menus.hud.AddComponent<YogaPoseHud>(); hud.match = m;
            hud.target = SportsUIAuthoring.Text("Achievable pose targets", m.menus.hud.transform, "", new Vector2(1620, 40), new Vector2(0, 447), 23);
            hud.cards = new TMPro.TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var card = SportsUIAuthoring.Rect("Owned yoga pose feedback " + i, m.menus.hud.transform, new Vector2(438, 116), new Vector2((i - 1.5f) * 456, -360)); card.AddComponent<UnityEngine.UI.Image>().color = new Color(.008f, .03f, .035f, .9f);
                hud.cards[i] = SportsUIAuthoring.Text("Participant achieved pose", card.transform, "", new Vector2(410, 100), Vector2.zero, 21); hud.cards[i].fontSizeMax = 22; hud.cards[i].fontSizeMin = 18;
            }
            m.freeNext = SportsUIAuthoring.Button(m.menus.hud.transform, "NEXT POSE", "yoga-next", new Vector2(-220, -445), new Vector2(370, 42)).gameObject;
            m.freeFinish = SportsUIAuthoring.Button(m.menus.hud.transform, "FINISH CLASS", "yoga-finish", new Vector2(220, -445), new Vector2(370, 42)).gameObject;
            m.pauseFreeFinish = SportsUIAuthoring.Button(m.menus.pages[(int)SportsEventScreen.Pause].transform, "FINISH FREE CLASS", "yoga-finish", new Vector2(0, -280), new Vector2(560, 48)).gameObject;
        }
    }
}
