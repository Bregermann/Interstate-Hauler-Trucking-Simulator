using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckFieldGoal;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckNinjaWarrior.Editor
{
    public static class NinjaCarnivalEnvironmentAuthoring
    {
        const string Folder = "Assets/TruckNinjaWarrior/CarnivalEnvironment";
        [MenuItem("Truck Sports/Ninja/Install Missing Carnival Environment")]
        public static void InstallMissingEnvironment()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var scene = SceneManager.GetSceneByPath(NinjaAuthoring.Scene); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing Ninja scene edits first.");
            if (opened) scene = EditorSceneManager.OpenScene(NinjaAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects(); var match = roots.SelectMany(root => root.GetComponentsInChildren<NinjaMatch>(true)).Single();
                if (roots.SelectMany(root => root.GetComponentsInChildren<NinjaCarnivalEnvironment>(true)).Any()) { Debug.Log("NINJA_CARNIVAL_ENVIRONMENT_PRESERVED existing designer bindings retained."); return; }
                var all = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var water = all.Single(part => part.name == "Continuous genuine water fall pit");
                if (water.GetComponent<LWS.TruckFishing.FishingWaterFlow>() == null || water.GetComponent<Renderer>() == null) throw new InvalidOperationException("Preserve and reuse the existing animated water plane.");
                var crowd = match.crowd;
                if (crowd == null || crowd.batches == null || crowd.batches.Length == 0 || crowd.batches.Any(batch => batch.idle == null || batch.cheer == null || batch.materials.Length == 0)) throw new InvalidOperationException("Existing Field Goal crowd batches required.");
                var deck = all.First(part => part.name == "Finished colored stadium spectator terrace").GetComponent<MeshFilter>();
                var column = all.First(part => part.name == "Grounded branded show gantry column").GetComponent<MeshFilter>();
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/TruckFieldGoal/Crowd/Stadium reactions.controller");
                var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/TruckPedestrianPlowing/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).Take(8).Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
                var fire = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckPedestrianPlowing/Materials/Parade fire.mat");
                var spray = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSports/Rendering/River splash alpha.mat");
                var smoke = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Effects/Prefabs/ExhaustSmoke.prefab");
                if (deck == null || column == null || controller == null || prefabs.Length == 0 || fire == null || fire.GetTexture("_BaseMap") == null || spray == null || smoke == null) throw new InvalidOperationException("Existing owned crowd, masked fire, NWH smoke and Dock/River splash rendering assets required.");
                if (smoke.GetComponentsInChildren<Component>(true).Any(component => !(component is Transform) && !(component is ParticleSystem) && !(component is ParticleSystemRenderer))) throw new InvalidOperationException("The audited smoke source must remain rendering-only.");
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                var rootObject = new GameObject("Carnival Ninja water, crowd and flaming entrance"); SceneManager.MoveGameObjectToScene(rootObject, scene); var root = rootObject.transform;
                var environment = rootObject.AddComponent<NinjaCarnivalEnvironment>(); environment.match = match; environment.waterSurface = water; environment.waterBounds = water.GetComponent<Renderer>().bounds;
                environment.splash = new ParticleSystem[4]; environment.rings = new LineRenderer[4];
                var ringMaterial = Material("Soft expanding water rings", spray, new Color(.7f, .94f, 1, .6f));
                for (int i = 0; i < 4; i++)
                {
                    var system = new GameObject("Pooled real-velocity Ninja water crown " + i).AddComponent<ParticleSystem>(); system.transform.SetParent(root, false); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = system.main; main.loop = true; main.playOnAwake = false; main.useUnscaledTime = false; main.maxParticles = 256; main.startLifetime = 1.5f; main.startSpeed = 0; main.gravityModifier = 1; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = new Color(.8f, .94f, 1, .8f);
                    var emission = system.emission; emission.enabled = false; var shape = system.shape; shape.enabled = false;
                    var fade = system.colorOverLifetime; fade.enabled = true; fade.color = Fade();
                    var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = spray; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    environment.splash[i] = system;
                    var ring = new GameObject("Pooled Ninja expanding surface ring " + i).AddComponent<LineRenderer>(); ring.transform.SetParent(root, false); ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 64; ring.widthMultiplier = .08f; ring.sharedMaterial = ringMaterial;
                    ring.shadowCastingMode = ShadowCastingMode.Off; ring.receiveShadows = false;
                    for (int n = 0; n < 64; n++) { float angle = n * Mathf.PI * 2 / 64; ring.SetPosition(n, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle))); }
                    ring.enabled = false; environment.rings[i] = ring;
                }
                AppendCrowd(environment, crowd, root, deck, prefabs, controller);
                Entrance(environment, root, match, column.sharedMesh, column.GetComponent<Renderer>().sharedMaterial, fire, smoke);
                EditorUtility.SetDirty(environment); EditorUtility.SetDirty(crowd); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("NINJA_CARNIVAL_ENVIRONMENT_BOUND reused existing Y0 animated water and shared countdown; appended810 FieldGoal posed spectators +24 real animated near fans; checkered start +2 flaming render-only pillars; pooled captured-fall visualization. Existing spawn/obstacles/physics/rules/lighting and camera acquisition retained. Native rendered sequence still required.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        static void AppendCrowd(NinjaCarnivalEnvironment environment, FieldGoalCrowd crowd, Transform root, MeshFilter referenceDeck, GameObject[] prefabs, RuntimeAnimatorController controller)
        {
            var newFans = new List<MeshFilter>(); var idle = new List<Mesh>(); var cheer = new List<Mesh>();
            var byBatch = crowd.batches.Select(batch => new List<MeshFilter>(batch.fans ?? Array.Empty<MeshFilter>())).ToArray();
            var seats = crowd.batches.Select(batch => new List<Matrix4x4>(batch.seats)).ToArray();
            for (int side = -1; side <= 1; side += 2) for (int row = 5; row < 8; row++)
            {
                Geometry(root, "Additional render-only stadium terrace", referenceDeck.sharedMesh, referenceDeck.GetComponent<Renderer>().sharedMaterials, new Vector3(side * (72 + row * 4), 4 + row * 2, 310), new Vector3(5, 2, 790));
                for (int seat = 0; seat < 135; seat++)
                {
                    int which = (seat + row) % crowd.batches.Length; var batch = crowd.batches[which];
                    var fan = Geometry(root, "Additional Field Goal posed spectator", batch.idle, batch.materials, new Vector3(side * (72 + row * 4), 5 + row * 2, -70 + seat * 5.7f), Vector3.one);
                    fan.transform.rotation = Quaternion.Euler(0, side < 0 ? 90 : -90, 0); byBatch[which].Add(fan); seats[which].Add(fan.transform.localToWorldMatrix); newFans.Add(fan); idle.Add(batch.idle); cheer.Add(batch.cheer);
                }
            }
            for (int i = 0; i < crowd.batches.Length; i++) { crowd.batches[i].fans = byBatch[i].ToArray(); crowd.batches[i].seats = seats[i].ToArray(); }
            var animated = new List<Animator>();
            for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 12; i++)
            {
                var point = new Vector3(side * 71.4f, 5, -57 + i * 62);
                var actor = SportsAudienceAuthoring.Spawn(prefabs[i % prefabs.Length], root, point, new Vector3(0, point.y, point.z), controller, 1);
                actor.fireEvents = false; actor.cullingMode = AnimatorCullingMode.CullCompletely; foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off; animated.Add(actor);
            }
            crowd.nearFans = (crowd.nearFans ?? Array.Empty<Animator>()).Concat(animated).ToArray();
            environment.newFans = animated.ToArray(); environment.addedPosedFans = newFans.ToArray(); environment.addedIdle = idle.ToArray(); environment.addedCheer = cheer.ToArray();
        }
        static void Entrance(NinjaCarnivalEnvironment environment, Transform root, NinjaMatch match, Mesh structure, Material steel, Material fire, GameObject smoke)
        {
            var flames = new List<ParticleSystem>(); float floor = match.course.start.y - .65f;
            for (int side = -1; side <= 1; side += 2)
            {
                var foot = new Vector3(side * 24, floor, -26);
                float bottom = environment.waterSurface.position.y - 6, top = floor + 22;
                Geometry(root, "Giant flaming entrance pillar", structure, new[] { steel }, new Vector3(foot.x, (bottom + top) * .5f, foot.z), new Vector3(3.2f, top - bottom, 3.2f));
                Geometry(root, "Entrance fire brazier platform", structure, new[] { steel }, foot + Vector3.up * 22, new Vector3(5, .7f, 5));
                var flame = new GameObject("Owned CoreCollapse masked entrance flame").AddComponent<ParticleSystem>(); flame.transform.SetParent(root, false); flame.transform.position = foot + Vector3.up * 22.4f; flame.transform.rotation = Quaternion.Euler(-90, 0, 0); flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = flame.main; main.loop = true; main.playOnAwake = true; main.useUnscaledTime = false; main.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.5f); main.startSpeed = new ParticleSystem.MinMaxCurve(5, 9); main.startSize = new ParticleSystem.MinMaxCurve(3, 5.5f); main.maxParticles = 64; main.startColor = new ParticleSystem.MinMaxGradient(new Color(2, .25f, .01f, .65f), new Color(3, 1, .05f, .8f));
                var emission = flame.emission; emission.rateOverTime = 28; var shape = flame.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = 1.2f; shape.angle = 10;
                var noise = flame.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .7f;
                var color = flame.colorOverLifetime; color.enabled = true; color.color = Fade();
                var renderer = flame.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = fire; renderer.shadowCastingMode = ShadowCastingMode.Off; flames.Add(flame);
                var haze = UnityEngine.Object.Instantiate(smoke, root, false); haze.name = "Existing NWH entrance smoke"; haze.transform.position = foot + Vector3.up * 25; haze.transform.rotation = Quaternion.Euler(-90, 0, 0);
                foreach (var particle in haze.GetComponentsInChildren<ParticleSystem>(true)) { particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var smokeMain = particle.main; smokeMain.loop = true; smokeMain.playOnAwake = true; smokeMain.maxParticles = 32; smokeMain.startLifetime = 3; smokeMain.startSize = 5; var smokeEmission = particle.emission; smokeEmission.rateOverTime = 4; flames.Add(particle); }
            }
            environment.entranceFire = flames.ToArray();
            string path = Folder + "/Actual checkered start strip.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var vertices = new List<Vector3>(); var black = new List<int>(); var white = new List<int>();
                for (int row = 0; row < 2; row++) for (int tile = 0; tile < 20; tile++)
                { int first = vertices.Count; float x = -16 + tile * 1.6f, z = row * 1.6f; vertices.AddRange(new[] { new Vector3(x, 0, z), new Vector3(x, 0, z + 1.6f), new Vector3(x + 1.6f, 0, z + 1.6f), new Vector3(x + 1.6f, 0, z) }); var target = (row + tile) % 2 == 0 ? black : white; target.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 }); }
                mesh = new Mesh { name = "Mode-only checkered starting line", vertices = vertices.ToArray(), subMeshCount = 2 }; mesh.SetTriangles(black, 0); mesh.SetTriangles(white, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path);
            }
            Geometry(root, "Proper checkered Ninja start line", mesh, new[] { Material("Start carbon enamel", steel, new Color(.015f, .02f, .025f)), Material("Start ivory enamel", steel, new Color(.92f, .92f, .85f)) }, new Vector3(0, floor + .025f, -29), Vector3.one);
        }
        static MeshFilter Geometry(Transform parent, string name, Mesh mesh, Material[] materials, Vector3 position, Vector3 scale)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.position = position; root.transform.localScale = scale;
            var filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = mesh; var renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterials = materials; renderer.shadowCastingMode = ShadowCastingMode.Off; return filter;
        }
        static Material Material(string name, Material source, Color color) { string path = Folder + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material; material = new Material(source) { name = name }; if (name == "Soft expanding water rings") { material.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); material.SetTexture("_BaseMap", null); } material.SetColor("_BaseColor", color); material.SetColor("_Color", color); AssetDatabase.CreateAsset(material, path); return material; }
        static Gradient Fade() { var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1) }); return gradient; }
    }
}
