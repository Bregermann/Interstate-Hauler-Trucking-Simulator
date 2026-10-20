using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using LWS.TruckSports.Editor;
namespace LWS.TruckWhackAMole.Editor
{
    // Original character art is needed: no mole/rodent prefab was present in
    // the installed-model audit. Normal Unity Animator owns the authored clips.
    public static class MolePrefabAuthoring
    {
        public const string Folder = "TruckWhackAMole";
        public static string PathFor(MoleKind kind) => "Assets/TruckWhackAMole/Prefabs/Fairground " + kind + " Mole.prefab";
        public static void Create()
        {
            foreach (var directory in new[] { "Prefabs", "Materials", "Animations" }) Directory.CreateDirectory("Assets/TruckWhackAMole/" + directory);
            AssetDatabase.Refresh(); var controller = Controller();
            foreach (MoleKind kind in Enum.GetValues(typeof(MoleKind)))
            {
                if (File.Exists(PathFor(kind))) continue; // Never regenerate designer edits.
                var root = new GameObject("Original animated fairground " + kind + " mole"); var rig = new GameObject("Rig").transform; rig.SetParent(root.transform, false);
                var fur = SportsEventAuthoring.Mat(Folder, kind + " mole coat", kind == MoleKind.Golden ? new Color(.98f, .56f, .05f) : kind == MoleKind.Decoy ? new Color(.12f, .14f, .18f) : new Color(.30f, .15f, .07f), .18f);
                var belly = SportsEventAuthoring.Mat(Folder, kind + " mole muzzle and belly", kind == MoleKind.Golden ? new Color(1, .86f, .32f) : new Color(.71f, .47f, .28f), .15f);
                var black = SportsEventAuthoring.Mat(Folder, "Mole eyes and brows", new Color(.015f, .008f, .025f), .35f);
                var white = SportsEventAuthoring.Mat(Folder, "Mole ivory claws and eye whites", new Color(1, .94f, .76f), .2f);
                var rose = SportsEventAuthoring.Mat(Folder, "Mole rosy snout", new Color(.75f, .16f, .20f), .55f);
                Part(rig, "Pear-shaped chest", new Vector3(0, 1.5f, 0), new Vector3(2.6f, 3.1f, 2.2f), fur);
                Part(rig, "Soft belly", new Vector3(0, 1.5f, .9f), new Vector3(1.6f, 2.1f, .45f), belly);
                var head = new GameObject("Head").transform; head.SetParent(rig, false); head.localPosition = new Vector3(0, 3, .05f);
                Part(head, "Round tufted head", Vector3.zero, new Vector3(2.8f, 2.2f, 2.3f), fur);
                Part(head, "Long expressive muzzle", new Vector3(0, -.28f, 1), new Vector3(1.8f, 1.05f, 1.35f), belly);
                Part(head, "Rosy shiny nose", new Vector3(0, -.08f, 1.69f), new Vector3(.9f, .65f, .55f), rose);
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(head, "Little round ear", new Vector3(side * 1.21f, .1f, -.05f), new Vector3(.65f, .75f, .4f), fur);
                    Part(head, "Ear inset", new Vector3(side * 1.24f, .1f, .14f), new Vector3(.38f, .42f, .12f), belly);
                    Part(head, "Oversized eye", new Vector3(side * .59f, .38f, .96f), new Vector3(.72f, .9f, .4f), white);
                    Part(head, "Curious pupil", new Vector3(side * .53f, .38f, 1.18f), new Vector3(.29f, .49f, .17f), black);
                    Part(head, "Eye glint", new Vector3(side * .53f - .06f, .52f, 1.26f), Vector3.one * .12f, white);
                    var brow = Part(head, "Raised eyebrow", new Vector3(side * .6f, .92f, 1.05f), new Vector3(.81f, .17f, .18f), black); brow.localRotation = Quaternion.Euler(0, 0, side * -12);
                    Part(head, "Comical buck tooth", new Vector3(side * .21f, -.7f, 1.53f), new Vector3(.36f, .54f, .26f), white);
                    var paw = new GameObject(side < 0 ? "PawLeft" : "PawRight").transform; paw.SetParent(rig, false); paw.localPosition = new Vector3(side * 1.47f, 1.6f, .4f);
                    Part(paw, "Broad digging paw", Vector3.zero, new Vector3(.85f, 1.12f, .9f), fur);
                    for (int claw = 0; claw < 4; claw++) { var tip = Part(paw, "Ivory digging claw", new Vector3((claw - 1.5f) * .19f, -.36f, .39f), new Vector3(.14f, .48f, .21f), white); tip.localRotation = Quaternion.Euler(40, 0, 0); }
                }
                for (int i = 0; i < 3; i++) { var tuft = Part(head, "Swept forehead tuft", new Vector3((i - 1) * .35f, 1.01f, -.15f), new Vector3(.36f, .75f, .4f), fur); tuft.localRotation = Quaternion.Euler(-25, 0, -25 + i * 20); }
                if (kind == MoleKind.Golden)
                {
                    var badge = new GameObject("Golden star belly badge"); badge.transform.SetParent(rig, false); badge.transform.localPosition = new Vector3(0, 1.5f, 1.18f); badge.AddComponent<MeshFilter>().sharedMesh = Star(); badge.AddComponent<MeshRenderer>().sharedMaterial = white;
                }
                if (kind == MoleKind.Decoy)
                {
                    var red = SportsEventAuthoring.Mat(Folder, "Decoy warning scarlet", new Color(1, .055f, .025f), .3f);
                    Part(head, "Obvious black decoy helmet", new Vector3(0, .75f, -.1f), new Vector3(3.05f, 1.05f, 2.5f), black);
                    for (int side = -1; side <= 1; side += 2) { var cross = Part(rig, "Decoy red warning cross", new Vector3(0, 1.6f, 1.2f), new Vector3(1.1f, .22f, .15f), red); cross.localRotation = Quaternion.Euler(0, 0, side * 45); }
                    Part(head, "Decoy fuse cap", new Vector3(0, 1.4f, 0), new Vector3(.35f, .7f, .35f), rose);
                }
                var animator = root.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(root, PathFor(kind)); UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets(); Debug.Log("WHACK_ORIGINAL_ANIMATED_PREFABS three authored mole identities; existing prefabs preserved");
        }
        static Transform Part(Transform parent, string name, Vector3 point, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere); part.name = name; part.transform.SetParent(parent, false); part.transform.localPosition = point; part.transform.localScale = scale; UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>()); part.GetComponent<Renderer>().sharedMaterial = material; return part.transform;
        }
        static AnimatorController Controller()
        {
            string path = "Assets/TruckWhackAMole/Animations/Fairground mole.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (existing != null) return existing;
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path); var machine = controller.layers[0].stateMachine;
            foreach (var stateName in new[] { "Emerge", "Look", "Dive", "Whack" })
            {
                var clip = new AnimationClip { name = "Original mole " + stateName, frameRate = 30 };
                float duration = stateName == "Look" ? 2 : stateName == "Whack" ? .55f : .5f;
                var elevation = stateName == "Emerge" ? AnimationCurve.EaseInOut(0, -5, duration, 0) : stateName == "Dive" ? AnimationCurve.EaseInOut(0, 0, duration, -5) : stateName == "Whack" ? new AnimationCurve(new Keyframe(0, 0), new Keyframe(.15f, 1.1f), new Keyframe(duration, -5)) : AnimationCurve.Constant(0, duration, 0);
                clip.SetCurve("Rig", typeof(Transform), "localPosition.y", elevation);
                clip.SetCurve("Rig/Head", typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(new Keyframe(0, -22), new Keyframe(duration * .5f, 25), new Keyframe(duration, -22)));
                clip.SetCurve("Rig/Head", typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(new Keyframe(0, 5), new Keyframe(duration * .5f, -7), new Keyframe(duration, 5)));
                foreach (var paw in new[] { "PawLeft", "PawRight" }) clip.SetCurve("Rig/" + paw, typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(new Keyframe(0, -15), new Keyframe(duration * .5f, 18), new Keyframe(duration, -15)));
                if (stateName == "Whack") clip.SetCurve("Rig", typeof(Transform), "localEulerAnglesRaw.z", AnimationCurve.EaseInOut(0, 0, duration, 90));
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = stateName == "Look"; AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, "Assets/TruckWhackAMole/Animations/" + stateName + ".anim"); var state = machine.AddState(stateName); state.motion = clip; if (stateName == "Look") machine.defaultState = state;
            }
            return controller;
        }
        static Mesh Star()
        {
            string path = "Assets/TruckWhackAMole/Materials/Original golden mole star.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            mesh = new Mesh { name = "Original five-point golden mole badge" }; var vertices = new Vector3[11]; var triangles = new int[30];
            for (int i = 0; i < 10; i++) { float a = i * Mathf.PI / 5; float r = i % 2 == 0 ? .6f : .28f; vertices[i + 1] = new Vector3(Mathf.Sin(a) * r, Mathf.Cos(a) * r, 0); triangles[i * 3] = 0; triangles[i * 3 + 1] = (i + 1) % 10 + 1; triangles[i * 3 + 2] = i + 1; }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
    }
}
