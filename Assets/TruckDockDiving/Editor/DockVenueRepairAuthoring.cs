using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckFieldGoal;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDockDiving.Editor
{
    public static class DockVenueRepairAuthoring
    {
        const string Folder = "Assets/TruckDockDiving/Presentation";
        static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        static Material Surface(Material source)
        {
            if (materials.TryGetValue(source, out var prior)) return prior;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)); string path = Folder + "/Audience " + source.name.Replace('/', '_') + " " + guid.Substring(0, 8) + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Dock owned audience · " + source.name, enableInstancing = true };
                mat.SetTexture("_BaseMap", source.mainTexture); mat.SetColor("_BaseColor", source.HasProperty("_Color") ? source.color : Color.white); mat.SetFloat("_Smoothness", .2f);
                if (source.IsKeywordEnabled("_ALPHATEST_ON") || source.HasProperty("_Mode") && source.GetFloat("_Mode") == 1) { mat.SetFloat("_AlphaClip", 1); mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue = (int)RenderQueue.AlphaTest; }
                AssetDatabase.CreateAsset(mat, path);
            }
            materials.Add(source, mat); return mat;
        }
        static AnimationClip Pick(AnimationClip[] clips, string[] words, bool required = true)
        {
            foreach (string word in words) { var found = clips.FirstOrDefault(c => c.name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0); if (found != null) return found; }
            if (required) throw new InvalidOperationException("Owned character controller is missing its idle/applause motion."); return null;
        }
        static AnimatorController Controller(GameObject prefab, int index, out AnimationClip idle, out AnimationClip cheer)
        {
            var original = prefab.GetComponentInChildren<Animator>(true); if (original == null || original.runtimeAnimatorController == null) throw new InvalidOperationException("Use a compatible owned UTS character controller.");
            var clips = original.runtimeAnimatorController.animationClips.Distinct().Where(c => !c.name.StartsWith("__")).ToArray();
            idle = Pick(clips, index % 3 == 1 ? new[] { "idle2", "idle", "wait" } : index % 3 == 2 ? new[] { "dance", "idle", "wait" } : new[] { "idle1", "idle", "wait" });
            cheer = Pick(clips, new[] { "clap", "cheer" }); string path = Folder + "/Audience " + index + " reactions.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path); var machine = controller.layers[0].stateMachine;
                var rest = machine.AddState("Idle"); rest.motion = idle; machine.defaultState = rest; machine.AddState("Cheer").motion = cheer; machine.AddState("Disappointed").motion = Pick(clips, new[] { "idle2", "idle", "wait" });
            }
            return controller;
        }
        static Animator Person(GameObject prefab, Transform parent, Vector3 point, Vector3 focus, AnimatorController controller, float scale)
        {
            var actor = SportsAudienceAuthoring.Spawn(prefab, parent, point, focus, controller, scale); actor.runtimeAnimatorController = controller;
            foreach (var audio in actor.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(audio);
            foreach (var listener in actor.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);
            foreach (var camera in actor.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera);
            foreach (var light in actor.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
            foreach (var skin in actor.GetComponentsInChildren<Renderer>(true)) { skin.sharedMaterials = skin.sharedMaterials.Select(Surface).ToArray(); skin.shadowCastingMode = ShadowCastingMode.Off; }
            actor.cullingMode = AnimatorCullingMode.CullCompletely; return actor;
        }
        static Transform ActorRoot(Animator actor, Transform parent) { var t = actor.transform; while (t.parent != null && t.parent != parent) t = t.parent; return t; }
        static Transform Model(Transform parent, string name, string path, Vector3 point, Vector3 size, float yaw = 0, bool stretch = true)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.SetPositionAndRotation(point, Quaternion.Euler(0, yaw, 0)); LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root, path, size, 0, stretch, "TruckDockDiving"); return root;
        }
        static void Refit(Transform anchor, Vector3 size, float yaw)
        {
            anchor.rotation = Quaternion.Euler(0, yaw, 0); var renderers = anchor.GetComponentsInChildren<Renderer>(true); Bounds bounds = default; bool first = true;
            foreach (var r in renderers) for (int i = 0; i < 8; i++) { var b = r.localBounds; var p = anchor.InverseTransformPoint(r.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)))); if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p); }
            if (first || bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) throw new InvalidOperationException("Expected the saved owned model's actual dimensions.");
            var factor = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            foreach (Transform child in anchor) { child.localPosition = Vector3.Scale(child.localPosition, factor); child.localScale = Vector3.Scale(child.localScale, factor); }
        }
        static GameObject Box(Transform parent, string name, Vector3 point, Vector3 size, Material material, bool solid)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false); g.transform.position = point; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = material; if (!solid) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        static void GroundExhibit(Transform truck)
        {
            var renderers = truck.GetComponentsInChildren<Renderer>(true); var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            var target = truck.position; truck.localScale *= 2.7f / bounds.size.y; bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            truck.position += new Vector3(target.x - bounds.center.x, .1f - bounds.min.y, target.z - bounds.center.z);
        }
        [MenuItem("Truck Sports/Dock Diving/Finish saved pool and audience")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(DockAuthoring.Scene); var m = UnityEngine.Object.FindAnyObjectByType<DockMatch>();
            var existing = m.GetComponent<DockAudiencePresentation>();
            if (existing != null)
            {
                if (existing.authoredVersion < 2)
                {
                    var wall = m.transform.Find("Finished Dock competition venue · owned artwork/Complete solid pool far wall"); var cap = m.transform.Find("Finished Dock competition venue · owned artwork/Finished far-end coping and deck");
                    if (wall != null && wall.GetComponent<MeshFilter>().sharedMesh.name == "Cube") wall.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckDockDiving/Materials/Pool wall without duplicate coping face.asset");
                    var ivory = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Distance ivory.mat"); var blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Stadium navy.mat");
                    if (cap != null && cap.GetComponent<Renderer>().sharedMaterial.name == "Pool ceramic") cap.GetComponent<Renderer>().sharedMaterial = ivory;
                    foreach (var renderer in m.GetComponentsInChildren<Renderer>(true).Where(r => r.name == "Finished rear grandstand tier")) if (renderer.sharedMaterial == m.dock.GetComponent<Renderer>().sharedMaterial) renderer.sharedMaterial = blue;
                }
                if (existing.authoredVersion < 3)
                {
                    var venue = m.transform.Find("Finished Dock competition venue · owned artwork");
                    foreach (Transform anchor in venue)
                    {
                        if (anchor.name == "Owned timber approach decking") Refit(anchor, new Vector3(6.9f, .12f, 2.32f), 0);
                        else if (anchor.name == "Owned timber dock side cladding") Refit(anchor, new Vector3(.12f, 2.15f, 4.35f), 0);
                        else if (anchor.name == "Owned approach safety railing") { Refit(anchor, new Vector3(4.2f, 1, .16f), 90); anchor.GetComponent<BoxCollider>().size = new Vector3(4.2f, 1, .16f); }
                        else if (anchor.name == "Owned venue perimeter fence") Refit(anchor, new Vector3(5.6f, 1.25f, .18f), 90);
                        else if (anchor.name == "Owned far venue enclosure") Refit(anchor, new Vector3(5.8f, 1.4f, .18f), 0);
                    }
                }
                if (existing.authoredVersion < 4)
                {
                    m.splash.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSports/Rendering/River splash alpha.mat");
                    foreach (var t in m.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Novelty truck spectator · render only")) GroundExhibit(t);
                    existing.authoredVersion = 4; EditorUtility.SetDirty(existing); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                Debug.Log("DOCK_VENUE_PRESERVED saved owner audience, water and venue selections."); return;
            }
            var prefabs = DockVenueAssetAudit.Characters.Select(k => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/Prefabs/" + k + ".prefab")).ToArray(); if (prefabs.Any(p => p == null)) throw new InvalidOperationException("All eight inspected owned clothed spectator prefabs are required.");
            var wood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ConstructionPropsBundle/URP/Prefabs/SM_Planks01 Variant.prefab"); var fence = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JC_LP_MegaCity/Prefabs/FloorProps/SM_FloorProps_Fence_01.prefab"); if (wood == null || fence == null) throw new InvalidOperationException("Inspected owned timber and fence assets required.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh(); materials.Clear();
            var root = new GameObject("Finished Dock competition venue · owned artwork").transform; root.SetParent(m.transform, false);
            var oldCrowd = GameObject.Find("UTS spectator crowd · render and animation only"); if (oldCrowd != null) oldCrowd.SetActive(false);
            var presence = m.gameObject.AddComponent<DockAudiencePresentation>(); presence.authoredVersion = 4; presence.match = m; presence.crowd = m.gameObject.AddComponent<FieldGoalCrowd>(); presence.crowd.eventSession = m; presence.crowd.authoredVersion = 6; presence.crowd.runnerChance = 0; presence.crowd.batches = Array.Empty<FieldGoalCrowd.Batch>();
            var audience = new GameObject("Owned audience · eight clothed character variants").transform; audience.SetParent(root, false); presence.variants = new DockAudienceVariant[8]; var controllers = new AnimatorController[8]; var fans = new List<MeshFilter>[8]; var near = new List<Animator>(); var random = new System.Random(482173);
            for (int i = 0; i < 8; i++)
            {
                controllers[i] = Controller(prefabs[i], i, out var idle, out var cheer); var actor = Person(prefabs[i], audience, Vector3.zero, Vector3.forward, controllers[i], 1); var actorRoot = ActorRoot(actor, audience); actorRoot.name = "Pose source · " + prefabs[i].name;
                var skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true); if (skins.Length != 1) throw new InvalidOperationException("Inspected source must contain its single complete skinned character."); actorRoot.gameObject.SetActive(false); presence.variants[i] = new DockAudienceVariant { sourceName = prefabs[i].name, root = actorRoot, animator = actor, skin = skins[0], idle = idle, cheer = cheer }; fans[i] = new List<MeshFilter>();
            }
            int number = 0;
            void Seat(Vector3 point, Vector3 focus, bool animated)
            {
                int variant = number++ % 8; point.x += (float)(random.NextDouble() - .5) * .25f; point.z += (float)(random.NextDouble() - .5) * .55f; float scale = .98f + (float)random.NextDouble() * .16f;
                if (animated) { var actor = Person(prefabs[variant], audience, point, focus, controllers[variant], scale); actor.Play("Idle", 0, (float)random.NextDouble()); near.Add(actor); return; }
                var g = new GameObject("Instanced clothed fan · " + variant + " · " + number); g.transform.SetParent(audience, false); var facing = focus - point; facing.y = 0; g.transform.SetPositionAndRotation(point, Quaternion.LookRotation(facing) * Quaternion.Euler(0, (float)(random.NextDouble() * 24 - 12), 0)); g.transform.localScale = Vector3.one * scale;
                var filter = g.AddComponent<MeshFilter>(); var renderer = g.AddComponent<MeshRenderer>(); renderer.sharedMaterials = presence.variants[variant].skin.sharedMaterials; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; fans[variant].Add(filter);
            }
            foreach (int side in new[] { -1, 1 }) for (int row = 0; row < 3; row++) for (int seat = 0; seat < 56; seat++) Seat(new Vector3(side * (18 + row * 1.8f), .3f + row * .7f, 4 + seat * (92f / 55)), new Vector3(0, .3f + row * .7f, 50), row < 2 && seat % 9 == 4);
            var navy = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Stadium navy.mat");
            for (int row = 0; row < 2; row++) { Box(root, "Finished rear grandstand tier", new Vector3(0, row * .7f, 138 + row * 2), new Vector3(54, .6f, 1.8f), navy, true); for (int seat = 0; seat < 64; seat++) Seat(new Vector3(-25.8f + seat * (51.6f / 63), .3f + row * .7f, 138 + row * 2), new Vector3(0, .3f + row * .7f, 65), false); }
            for (int i = 0; i < 8; i++) presence.variants[i].fans = fans[i].ToArray(); presence.crowd.nearFans = near.ToArray();
            // Existing pool envelope, first-contact trigger and launch collider
            // remain unchanged. Complete its visibly open far end explicitly.
            var ceramic = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Pool ceramic.mat"); var coping = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckDockDiving/Materials/Distance ivory.mat");
            var farWall = Box(root, "Complete solid pool far wall", new Vector3(0, -1.6f, 130.4f), new Vector3(22.8f, 3.2f, .8f), ceramic, true); farWall.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckDockDiving/Materials/Pool wall without duplicate coping face.asset"); Box(root, "Finished far-end coping and deck", new Vector3(0, -.25f, 133), new Vector3(32, .5f, 6), coping, true);
            string woodPath = AssetDatabase.GetAssetPath(wood), fencePath = AssetDatabase.GetAssetPath(fence);
            m.dock.GetComponent<Renderer>().enabled = false;
            for (int panel = 0; panel < 13; panel++) Model(root, "Owned timber approach decking", woodPath, new Vector3(0, 2.075f, -28.8f + panel * 2.3f), new Vector3(6.9f, .12f, 2.32f));
            foreach (int side in new[] { -1, 1 })
            {
                for (int panel = 0; panel < 7; panel++) { Model(root, "Owned timber dock side cladding", woodPath, new Vector3(side * 3.47f, .02f, -28 + panel * 4.3f), new Vector3(.12f, 2.15f, 4.35f)); var rail = Model(root, "Owned approach safety railing", fencePath, new Vector3(side * 4.3f, 2.2f, -27 + panel * 4.25f), new Vector3(4.2f, 1, .16f), 90); var collider = rail.gameObject.AddComponent<BoxCollider>(); collider.center = Vector3.up * .5f; collider.size = new Vector3(4.2f, 1, .16f); }
                for (int panel = 0; panel < 24; panel++) Model(root, "Owned venue perimeter fence", fencePath, new Vector3(side * 25.7f, 0, 1 + panel * 5.8f), new Vector3(5.6f, 1.25f, .18f), 90);
                for (int t = 0; t < 2; t++) { var truck = UnityEngine.Object.Instantiate(m.staged[t].visuals.gameObject, root, false); truck.name = "Novelty truck spectator · render only"; truck.transform.SetPositionAndRotation(new Vector3(side * 30, .1f, 35 + t * 55), Quaternion.Euler(0, side < 0 ? 90 : -90, 0)); truck.transform.localScale *= .7f; foreach (var script in truck.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script); foreach (var col in truck.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col); foreach (var body in truck.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body); }
            }
            for (int panel = 0; panel < 11; panel++) Model(root, "Owned far venue enclosure", fencePath, new Vector3(-30 + panel * 6, 0, 145), new Vector3(5.8f, 1.4f, .18f));
            Model(root, "Finished pool administration building", "Assets/JC_LP_MegaCity/Models/Buildings/SM_Buildings_Commercial_03.fbx", new Vector3(-12, -.15f, 164), new Vector3(30, 15, 20), 180, false);
            foreach (var truck in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Novelty truck spectator · render only")) { foreach (var c in truck.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(c); foreach (var l in truck.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(l); foreach (var a in truck.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(a); foreach (var a in truck.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(a); GroundExhibit(truck); }
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TruckSports/Art/Logos/TruckDockDiving.png"); if (logo == null) throw new InvalidOperationException("Existing original Dock logo required.");
            string logoPath = Folder + "/Dock event branding.mat"; var logoMat = AssetDatabase.LoadAssetAtPath<Material>(logoPath);
            if (logoMat == null) { logoMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); logoMat.SetTexture("_BaseMap", logo); logoMat.SetFloat("_Surface", 1); logoMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); logoMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); logoMat.SetFloat("_ZWrite", 0); logoMat.SetFloat("_Cull", 0); logoMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); logoMat.renderQueue = (int)RenderQueue.Transparent; AssetDatabase.CreateAsset(logoMat, logoPath); }
            var sign = GameObject.CreatePrimitive(PrimitiveType.Quad); sign.name = "Existing original Dock event logo"; sign.transform.SetParent(root, false); sign.transform.position = new Vector3(0, 7, 146); sign.transform.localScale = new Vector3(18, 18f * logo.height / logo.width, 1); sign.GetComponent<Renderer>().sharedMaterial = logoMat; UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());
            var water = GameObject.Find("NWH water plane · URP pool surface"); if (water.GetComponent<LWS.TruckFishing.FishingWaterFlow>() == null) water.AddComponent<LWS.TruckFishing.FishingWaterFlow>();
            m.splash.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSports/Rendering/River splash alpha.mat");
            var reflection = new GameObject("Shared pool environment reflection").AddComponent<ReflectionProbe>(); reflection.transform.SetParent(root, false); reflection.transform.position = new Vector3(0, 4, 65); reflection.mode = ReflectionProbeMode.Realtime; reflection.refreshMode = ReflectionProbeRefreshMode.EveryFrame; reflection.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces; reflection.resolution = 128; reflection.size = new Vector3(80, 35, 180); reflection.center = new Vector3(0, 4, 0); reflection.farClipPlane = 220; reflection.intensity = .7f;
            EditorUtility.SetDirty(m); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("DOCK_VENUE_AUTHORED 464 varied clothed spectators with 24 live rigs and bounded runtime pose baking, owned timber/fence art, far enclosure, existing flowing water and shared ripple/splash systems; original launch/scoring/controls retained.");
        }
    }
}
