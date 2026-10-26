using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckFishing.Editor
{
    public static class FishingCarnivalPresentationAuthoring
    {
        const string Scene = "Assets/TruckFishing/Scenes/TruckFishing.unity";
        const string Folder = "Assets/TruckFishing/CarnivalPresentation";
        [MenuItem("Truck Sports/Fishing/Install Missing Carnival Water And Wildlife")]
        public static void InstallMissingPresentation()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Scene); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save the existing Fishing scene edits first.");
            if (opened) scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive);
            try
            {
                var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FishingMatch>(true)).Single();
                if (match.streams.Any(stream => stream.GetComponent<FishingCarnivalWater>() != null))
                {
                    if (match.streams.Any(stream => stream.GetComponent<FishingCarnivalWater>() == null)) throw new InvalidOperationException("Preserve partial custom water bindings for explicit review.");
                    var addedWolf = TryWolfPrefab(); bool changed = false;
                    if (addedWolf != null) foreach (var stream in match.streams)
                        if (stream.GetComponentInChildren<FishingCarnivalWildlife>(true) == null) { Wildlife(stream, match, stream.GetComponent<FishingCarnivalWater>(), addedWolf); changed = true; }
                    if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); }
                    Debug.Log("FISHING_CARNIVAL_PRESENTATION_PRESERVED existing water/wildlife bindings retained; missing genuine wildlife added=" + changed + "; bear remains missing."); return;
                }
                foreach (var stream in match.streams)
                {
                    var surface = stream.transform.Find("River Modeler clear flowing surface");
                    if (surface == null || surface.GetComponent<MeshFilter>()?.sharedMesh == null || surface.GetComponentsInChildren<Collider>(true).Length > 0)
                        throw new InvalidOperationException("An existing render-only winding water mesh is required.");
                }
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                var ring = MissingMesh("Expanding water ripple ring", Ring);
                var quad = MissingMesh("Horizontal River Modeler response quad", Quad);
                var ripple = MissingMaterial("Soft water rings", null, false);
                var foam = MissingMaterial("River Modeler contact foam", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/WaterFoam.png"), false);
                var splash = MissingMaterial("River Modeler small splashes", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/Splash_AlbedoAlpha.png"), false);
                var refraction = MissingMaterial("River Modeler surface separation", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/Splash_AlbedoAlpha.png"), true);
                GameObject wolf = TryWolfPrefab();
                foreach (var stream in match.streams)
                {
                    var water = stream.gameObject.AddComponent<FishingCarnivalWater>(); water.stream = stream; water.match = match; water.view = match.streamCameras[stream.slot].view;
                    water.surface = stream.transform.Find("River Modeler clear flowing surface"); water.localWaterHeight = .27f;
                    var position = water.surface.localPosition; position.y = water.localWaterHeight - water.surface.GetComponent<MeshFilter>().sharedMesh.bounds.center.y; water.surface.localPosition = position;
                    var root = new GameObject("Carnival responsive water").transform; root.SetParent(stream.transform, false);
                    water.ripples = Particles(root, "Depth-aware expanding ripples", ripple, ring, 128, 1.15f, true);
                    water.foam = Particles(root, "Speed-dependent foam wake", foam, quad, 128, .7f, false);
                    water.separation = Particles(root, "URP water separation refraction", refraction, quad, 64, .45f, false);
                    water.droplets = Particles(root, "Small fish and wildlife splashes", splash, null, 64, .45f, false);
                    if (wolf != null) Wildlife(stream, match, water, wolf);
                    EditorUtility.SetDirty(water); EditorUtility.SetDirty(water.surface);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("FISHING_CARNIVAL_WATER_BOUND raised render-only water from .02 to .27 around unchanged .13 fish roots; vendor-normal flow retained; speed/depth/contact ripples, foam, droplets and refraction added. Genuine wolf=" + (wolf != null) + ". BEAR_MISSING: no owned bear model/clips found; no substitute or placeholder created. Gameplay/QTE/speed/trajectory sources unchanged; native visual acceptance pending.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        static GameObject TryWolfPrefab()
        {
            string path = Folder + "/Genuine animated ambient wolf.prefab"; var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string privateFolder = FishingCarnivalWildlifeIntake.Folder;
            if (!File.Exists(privateFolder + "WL_Walk.anim") || !File.Exists(privateFolder + "WL_Idles.FBX"))
            { Debug.LogWarning("FISHING_WOLF_CLIPS_UNAVAILABLE water remains independently usable; invoke the explicit private clip intake before creating the first wildlife binding."); return null; }
            AnimationClip Clip(string file, string name = null)
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(privateFolder + file).OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
                var result = name == null ? clips.Single() : clips.Single(clip => clip.name == name);
                if (result.empty || result.length < .2f || result.humanMotion) throw new InvalidOperationException("A genuine generic owned wolf clip is required: " + file); return result;
            }
            var clipsByState = new Dictionary<string, AnimationClip> { { "Idle", Clip("WL_Idles.FBX", "WL_Idle01") }, { "Walk", Clip("WL_Walk.anim") }, { "Left", Clip("WL_Walk Left.anim") },
                { "Right", Clip("WL_Walk Right.anim") }, { "Swim", Clip("WL_Swim.anim") }, { "SwimIdle", Clip("WL_Swim_Idle.anim") } };
            string controllerPath = Folder + "/Ambient wolf states.controller"; var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            bool createdController = controller == null;
            if (createdController) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (createdController || controller.layers.Length == 0)
            {
                // A prior interrupted explicit intake may have saved the empty
                // initial controller before its layers/states were committed.
                // Repair that exact invalid case; retain nonempty owner edits.
                if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
                var machine = controller.layers[0].stateMachine;
                foreach (var clip in clipsByState) { var state = machine.AddState(clip.Key); state.motion = clip.Value; if (clip.Key == "Idle") machine.defaultState = state; EditorUtility.SetDirty(state); }
                EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
                Debug.Log("FISHING_WOLF_STATES_COMMITTED six genuine clip states; empty interrupted controller repaired, existing nonempty customization preserved.");
            }
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(FishingCarnivalWildlifeIntake.Model); if (source == null) throw new InvalidOperationException("Existing owned WolfLite rig required.");
            var root = new GameObject("Genuine Malbers WolfLite ambient wildlife");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source); model.transform.SetParent(root.transform, false);
                if (model.GetComponentsInChildren<Component>(true).Any(component => !(component is Transform) && !(component is Animator) && !(component is Renderer) && !(component is MeshFilter)))
                    throw new InvalidOperationException("Wildlife scenery requires model/animation only; no actor input, colliders or physics are copied.");
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true); if (skins.Length == 0) throw new InvalidOperationException("Real skinned wolf mesh required.");
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckCheeseRolling/Materials/Scenery Wolf.mat"); if (material == null) throw new InvalidOperationException("Existing owned URP wolf material required.");
                foreach (var skin in skins) { skin.sharedMaterials = Enumerable.Repeat(material, skin.sharedMaterials.Length).ToArray(); skin.updateWhenOffscreen = false; }
                var bounds = skins[0].bounds; foreach (var skin in skins.Skip(1)) bounds.Encapsulate(skin.bounds);
                float scale = 1.6f / Mathf.Max(bounds.size.x, bounds.size.z); model.transform.localScale *= scale; model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
                var animator = model.GetComponent<Animator>();
                // Editor GetComponent can return Unity's fake-null object;
                // null coalescing would retain that unusable component.
                if (animator == null) animator = model.AddComponent<Animator>();
                string avatarPath = privateFolder + "Fishing wolf generic avatar.asset"; var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
                if (avatar == null) { avatar = AvatarBuilder.BuildGenericAvatar(model, ""); if (!avatar.isValid) throw new InvalidOperationException("Installed wolf generic avatar could not be built."); AssetDatabase.CreateAsset(avatar, avatarPath); }
                animator.avatar = avatar; animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.fireEvents = false; animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void Wildlife(FishingStream stream, FishingMatch match, FishingCarnivalWater water, GameObject prefab)
        {
            var root = new GameObject("Occasional genuine wolf — additional wildlife"); root.transform.SetParent(stream.transform, false);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab); visual.transform.SetParent(root.transform, false);
            var actor = root.AddComponent<FishingCarnivalWildlife>(); actor.match = match; actor.stream = stream; actor.water = water; actor.visual = visual;
            actor.animator = visual.GetComponentInChildren<Animator>(true); actor.species = "Wolf"; actor.initialSeed = 176 + stream.slot * 41; actor.initialDelay = 7 + stream.slot * 5;
            actor.terrain = stream.GetComponentInChildren<Terrain>();
            Vector3 Point(float side, float z)
            {
                var local = new Vector3(FishingStream.ChannelCenter(z) + side, 0, z); var world = stream.transform.TransformPoint(local);
                local.y = actor.terrain != null ? stream.transform.InverseTransformPoint(new Vector3(world.x, actor.terrain.SampleHeight(world) + actor.terrain.transform.position.y, world.z)).y : .6f; return local;
            }
            actor.route = new[] { Point(-5.9f, 29), Point(-5.3f, 18), Point(-4.7f, 12), Point(4.7f, 14), Point(5.5f, 21), Point(6, 34) };
            actor.swimSegments = new[] { false, false, false, true, false, false };
            actor.feet = visual.GetComponentsInChildren<Transform>(true).Where(bone => bone.name.IndexOf("paw", StringComparison.OrdinalIgnoreCase) >= 0 || bone.name.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0).Take(4).ToArray();
            EditorUtility.SetDirty(actor);
        }
        static Material MissingMaterial(string name, Texture texture, bool distortion)
        {
            string path = Folder + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            material.SetFloat("_Surface", 1); material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); material.SetInt("_ZWrite", 0); material.SetInt("_Cull", 0);
            material.SetColor("_BaseColor", Color.white); if (texture != null) material.SetTexture("_BaseMap", texture);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3010;
            if (distortion)
            {
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/Splash_Normals.png"); if (normal == null) throw new InvalidOperationException("Installed River Modeler splash normal required.");
                material.SetTexture("_BumpMap", normal); material.SetFloat("_DistortionEnabled", 1); material.SetFloat("_DistortionStrength", .08f); material.SetFloat("_DistortionStrengthScaled", .008f); material.SetFloat("_DistortionBlend", .8f); material.EnableKeyword("_DISTORTION_ON"); material.EnableKeyword("_NORMALMAP");
            }
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static ParticleSystem Particles(Transform parent, string name, Material material, Mesh mesh, int budget, float lifetime, bool expanding)
        {
            var system = new GameObject(name).AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); system.transform.SetParent(parent, false);
            var main = system.main; main.playOnAwake = false; main.loop = true; main.useUnscaledTime = false; main.startRotation3D = true; main.maxParticles = budget; main.startLifetime = lifetime; main.startSpeed = 0; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = mesh == null ? .3f : 0;
            var emission = system.emission; emission.rateOverTime = 0; emission.SetBursts(Array.Empty<ParticleSystem.Burst>()); var shape = system.shape; shape.enabled = false;
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            if (mesh != null) { renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = mesh; }
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Tangent, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            var size = system.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, expanding ? .25f : .8f, 1, expanding ? 2.2f : 1.3f));
            var color = system.colorOverLifetime; color.enabled = true; var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(0, 1) }); color.color = fade; return system;
        }
        static Mesh MissingMesh(string name, Func<Mesh> create) { string path = Folder + "/" + name + ".asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh; mesh = create(); mesh.name = name; AssetDatabase.CreateAsset(mesh, path); return mesh; }
        static Mesh Ring()
        {
            const int count = 40; var vertices = new Vector3[count * 2]; var uv = new Vector2[vertices.Length]; var indices = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                for (int side = 0; side < 2; side++) { var point = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (side == 0 ? .42f : .5f); vertices[i * 2 + side] = point; uv[i * 2 + side] = new Vector2(point.x + .5f, point.z + .5f); }
                int a = i * 2, b = (i + 1) % count * 2, t = i * 6; indices[t] = a; indices[t + 1] = b; indices[t + 2] = a + 1; indices[t + 3] = a + 1; indices[t + 4] = b; indices[t + 5] = b + 1;
            }
            var mesh = new Mesh { vertices = vertices, uv = uv, triangles = indices }; mesh.RecalculateNormals(); mesh.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, -1), vertices.Length).ToArray(); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh Quad() { var mesh = new Mesh { vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) }, uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, triangles = new[] { 0, 1, 2, 0, 2, 3 } }; mesh.RecalculateNormals(); mesh.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, -1), 4).ToArray(); mesh.RecalculateBounds(); return mesh; }
    }
}
