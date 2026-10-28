using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFieldGoal;

namespace LWS.TruckMechanicalBull.Editor
{
    public static class MechanicalBullAuthoring
    {
        public const string Folder = "TruckMechanicalBull", Scene = "Assets/TruckMechanicalBull/Scenes/TruckMechanicalBull.unity";
        [MenuItem("Truck Sports/Impossible Games/Create NEW Mechanical Bull")]
        public static void Create()
        {
            ImpossibleBalanceArt.GuardNew(Scene);
            var m = SportsEventAuthoring.Begin<MechanicalBullMatch>(Folder); m.setup.count = 4; m.option = 0; m.NormalizeSetup();
            var steel = ImpossibleBalanceArt.Material(Folder, "Powder coated midnight iron", new Color(.035f, .055f, .07f), .5f, .65f);
            var copper = ImpossibleBalanceArt.Material(Folder, "Brushed rodeo copper", new Color(.47f, .19f, .055f), .5f, .75f);
            var leather = ImpossibleBalanceArt.Material(Folder, "Hand stitched saddle oxblood", new Color(.21f, .038f, .028f), .18f);
            var ivory = ImpossibleBalanceArt.Material(Folder, "Polished ivory horn", new Color(.92f, .82f, .55f), .45f);
            var cedar = ImpossibleBalanceArt.Material(Folder, "Western cedar fencing", new Color(.32f, .13f, .065f), .2f);
            var ground = ImpossibleBalanceArt.Material(Folder, "Warm rodeo dirt", new Color(.36f, .23f, .11f), .08f);
            var block = ImpossibleBalanceArt.Mesh(Folder, "Crafted bevelled metal panel", ImpossibleBalanceArt.RoundedBlock);
            var oval = ImpossibleBalanceArt.Mesh(Folder, "Sculpted bull silhouette", ImpossibleBalanceArt.Ovoid);
            var drum = ImpossibleBalanceArt.Mesh(Folder, "Hydraulic lathed drum", () => ImpossibleBalanceArt.Lathe(new[] { new Vector2(0, 0), new Vector2(.5f, 0), new Vector2(.54f, .08f), new Vector2(.49f, .92f), new Vector2(.42f, 1), new Vector2(0, 1) }));
            var floor = ImpossibleBalanceArt.Part(null, "Dirt rodeo arena", block, ground, new Vector3(0, -.6f, 0), new Vector3(125, 1, 115)); floor.AddComponent<BoxCollider>();
            var baseRoot = new GameObject("IRON LONGHORN · machined rodeo pedestal").transform;
            ImpossibleBalanceArt.Part(baseRoot, "Weighted octagonal safety pedestal", drum, steel, Vector3.zero, new Vector3(13, 1.4f, 13));
            var piston = ImpossibleBalanceArt.Part(baseRoot, "Polished double acting hydraulic ram", drum, copper, Vector3.up * 1.2f, new Vector3(3.5f, 3, 3.5f)).transform;
            var machineRoot = new GameObject("Physical bucking saddle"); machineRoot.transform.position = Vector3.up * 4.5f;
            var machine = machineRoot.AddComponent<MechanicalBullMachine>(); m.machine = machine;
            machine.body = machineRoot.AddComponent<Rigidbody>(); machine.body.isKinematic = true; machine.body.interpolation = RigidbodyInterpolation.Interpolate; machine.body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            machine.piston = piston;
            var tractor0 = ImpossibleBalanceArt.Tractor(m, 0, Vector3.zero);
            var shape = tractor0.shape; float width = Mathf.Max(4, shape.size.x * 1.7f), length = Mathf.Max(9, shape.size.z * 1.2f);
            var saddle = ImpossibleBalanceArt.Part(machineRoot.transform, "Stitched leather riding saddle", block, leather, Vector3.zero, new Vector3(width, .65f, length));
            machine.deck = machineRoot.AddComponent<BoxCollider>(); machine.deck.size = new Vector3(width, .65f, length);
            machine.deck.sharedMaterial = SportsEventAuthoring.Contact(Folder, "Leather NWH contact", .85f, 0);
            ImpossibleBalanceArt.Part(machineRoot.transform, "Broad sculpted longhorn chest", oval, copper, new Vector3(0, -.6f, length * .2f), new Vector3(width * 1.2f, 2.7f, length * .75f));
            ImpossibleBalanceArt.Part(machineRoot.transform, "Forged bull muzzle", oval, steel, new Vector3(0, -.25f, length * .52f), new Vector3(3.3f, 1.9f, 2.8f));
            for (int side = -1; side <= 1; side += 2)
            {
                var horn = ImpossibleBalanceArt.Mesh(Folder, side < 0 ? "Swept left longhorn" : "Swept right longhorn", () => ImpossibleBalanceArt.Horn(side));
                ImpossibleBalanceArt.Part(machineRoot.transform, "Hand carved longhorn " + side, horn, ivory, new Vector3(side * 1.25f, .1f, length * .43f), Vector3.one);
                ImpossibleBalanceArt.Part(machineRoot.transform, "Inlaid bronze eye " + side, oval, steel, new Vector3(side * 1.25f, .1f, length * .49f), new Vector3(.28f, .35f, .38f));
                for (int i = 0; i < 22; i++) ImpossibleBalanceArt.Part(machineRoot.transform, "Saddle brass stitch", oval, ivory, new Vector3(side * (width * .5f - .15f), .34f, Mathf.Lerp(-length * .46f, length * .46f, i / 21f)), new Vector3(.08f, .045f, .16f));
            }
            m.riders = new MechanicalBullRider[4];
            for (int i = 0; i < 4; i++)
            {
                var truck = i == 0 ? tractor0 : ImpossibleBalanceArt.Tractor(m, i, Vector3.zero);
                float lowest = truck.wheels.Min(w => truck.transform.InverseTransformPoint(w.transform.position).y - w.wheel.radius - w.spring.maxLength);
                var rider = truck.gameObject.AddComponent<MechanicalBullRider>(); rider.truck = truck; rider.match = m; rider.slot = i;
                rider.attachedBodies = truck.GetComponentsInChildren<Rigidbody>(true).Where(b => b != truck.body).ToArray();
                rider.spawn = new Vector3(-shape.center.x, machineRoot.transform.position.y + .325f - lowest + .08f, -shape.center.z);
                rider.waiting = new Vector3(-22 + i * 12, .5f - lowest, -28); truck.transform.position = i == 0 ? rider.spawn : rider.waiting;
                m.riders[i] = rider;
            }
            Stands(m, block, cedar, steel);
            m.broadcast = SportsEventAuthoring.Camera("Iron Longhorn arena broadcast"); m.broadcast.Pose(new Vector3(0, 6, 0), new Vector3(20, 15, -27), 54, true); m.cameras = new[] { m.broadcast.view };
            m.machinery = machineRoot.AddComponent<AudioSource>(); m.machinery.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NWH/Vehicle Physics 2/Sound/Engine/Running/EngineRunning - Truck4.wav"); m.machinery.playOnAwake = false; m.machinery.loop = true; m.machinery.spatialBlend = .75f; m.machinery.maxDistance = 90;
            m.feedback.clips = (AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();
            ImpossibleBalanceArt.UI(m); ImpossibleBalanceArt.Finish(m, Folder, m.riders.Select(r => r.truck).ToArray());
        }
        static void Stands(MechanicalBullMatch m, Mesh block, Material cedar, Material steel)
        {
            var root = new GameObject("Western stands fencing banners and living audience").transform;
            var controller = CrowdController(); var actors = new System.Collections.Generic.List<Animator>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab");
            if (prefab == null) throw new InvalidOperationException("Installed UTS character rig missing.");
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 4; row++)
                {
                    float x = side * (28 + row * 2.4f), y = row * 1.4f + .5f;
                    ImpossibleBalanceArt.Part(root, "Cedar stepped bleacher", block, cedar, new Vector3(x, y, 4), new Vector3(2.4f, .65f, 46));
                    for (int i = 0; i < 8; i++) actors.Add(SportsAudienceAuthoring.Spawn(prefab, root, new Vector3(x, y + .35f, -16 + i * 5.4f), new Vector3(0, y, 0), controller, 1.1f));
                }
                for (int z = -22; z <= 28; z += 4)
                {
                    ImpossibleBalanceArt.Part(root, "Carved cedar fence upright", block, cedar, new Vector3(side * 22, 1.2f, z), new Vector3(.3f, 2.8f, .3f));
                    for (int rail = 0; rail < 2; rail++) ImpossibleBalanceArt.Part(root, "Cedar arena rail", block, cedar, new Vector3(side * 22, .8f + rail, z + 2), new Vector3(.22f, .25f, 4.3f));
                }
                var pole = ImpossibleBalanceArt.Part(root, "Copper floodlight mast", block, steel, new Vector3(side * 23, 8, 24), new Vector3(.6f, 16, .6f));
                for (int j = 0; j < 3; j++)
                {
                    var lamp = new GameObject("Warm rodeo floodlight").AddComponent<Light>(); lamp.transform.SetParent(pole.transform, true); lamp.transform.position = new Vector3(side * 23, 16, 22 + j * 2); lamp.transform.LookAt(new Vector3(0, 4, 0)); lamp.type = LightType.Spot; lamp.range = 75; lamp.spotAngle = 58; lamp.intensity = 7; lamp.color = new Color(1, .79f, .53f); lamp.shadows = LightShadows.None;
                }
            }
            var title = SportsEventAuthoring.Label("Rodeo enamel sign", "IRON LONGHORN\nRIDE THE IMPOSSIBLE", new Vector3(0, 10, 31), .23f, new Color(1, .82f, .43f)); title.transform.rotation = Quaternion.identity;
            var crowd = m.gameObject.AddComponent<FieldGoalCrowd>(); crowd.eventSession = m; crowd.batches = Array.Empty<FieldGoalCrowd.Batch>(); crowd.nearFans = actors.ToArray();
            crowd.response = root.gameObject.AddComponent<AudioSource>(); crowd.response.playOnAwake = false; crowd.cheer = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav"); crowd.boo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDockDiving/Audio/Playful crowd boo.wav"); m.crowd = crowd;
        }
        static AnimatorController CrowdController()
        {
            string path = "Assets/" + Folder + "/Materials/Rodeo audience.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (controller != null) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var pair in new[] { ("Idle", "idle1_f"), ("Cheer", "cheer_f"), ("Disappointed", "idle2_f") })
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/UTS_FullPack/Models/People/Animations/Girl no Heel/" + pair.Item2 + ".FBX").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
                var state = controller.AddMotion(clip); state.name = pair.Item1; if (pair.Item1 == "Idle") controller.layers[0].stateMachine.defaultState = state;
            }
            EditorUtility.SetDirty(controller); return controller;
        }
    }
}
