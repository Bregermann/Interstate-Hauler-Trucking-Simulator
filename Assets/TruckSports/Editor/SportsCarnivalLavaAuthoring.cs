using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckFloorIsLava;
using LWS.TruckTargetLaunch;
using LWS.TruckTerrierRacing;
using LWS.TruckTugOfWar;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static class SportsCarnivalLavaAuthoring
    {
        const string Folder = "Assets/TruckSports/Materials/CarnivalLava";
        const string ShaderPath = "Assets/TruckSports/Scripts/SportsCarnivalMoltenSurface.shader";
        const string DustPrefab = "Assets/NWH/Vehicle Physics 2/Effects/Prefabs/SurfaceDust.prefab";
        [MenuItem("Truck Sports/Carnival/Install Missing Molten Presentations")]
        public static void InstallMissingPresentations()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            foreach (string name in new[] { "TruckTargetLaunch", "TruckTerrierRacing", "TruckFloorIsLava", "TruckTugOfWar" }) Install(name);
            AssetDatabase.SaveAssets();
        }
        static void Install(string name)
        {
            string path = "Assets/" + name + "/Scenes/" + name + ".unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing scene edits first: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                if (roots.SelectMany(root => root.GetComponentsInChildren<SportsCarnivalLavaSurface>(true)).Any())
                { Debug.Log("CARNIVAL_LAVA_PRESENTATION_PRESERVED " + name); return; }
                Renderer renderer; Collider[] contacts; ParticleSystem[] impacts;
                var components = roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)).Where(component => component != null).ToArray();
                if (name == "TruckTargetLaunch")
                {
                    var match = components.OfType<TargetMatch>().Single(); renderer = match.volcano.lava.GetComponent<Renderer>();
                    contacts = match.volcano.lava.GetComponents<Collider>(); impacts = match.volcano.splashes;
                }
                else if (name == "TruckTerrierRacing")
                {
                    var match = components.OfType<TerrierMatch>().Single(); renderer = match.volcano.lava.GetComponent<Renderer>();
                    contacts = match.volcano.lava.GetComponents<Collider>(); impacts = match.volcano.splashes;
                }
                else if (name == "TruckFloorIsLava")
                {
                    var match = components.OfType<LavaMatch>().Single(); renderer = match.lava.GetComponent<Renderer>();
                    contacts = new[] { match.lavaVolume }; impacts = match.splashes;
                }
                else
                {
                    var match = components.OfType<TugMatch>().Single(); renderer = components.OfType<Renderer>().Single(part => part.name == "Lava far below the mountain");
                    contacts = renderer.GetComponents<Collider>(); impacts = match.fire;
                }
                if (renderer == null || renderer.sharedMaterial == null || contacts.Length == 0 || contacts.Any(contact => contact == null))
                    throw new InvalidOperationException("Existing authored molten surface and actual contact volumes are required: " + name);
                if (contacts.Any(contact => contact.GetComponent<SportsCarnivalLavaContact>() != null)) throw new InvalidOperationException("Existing custom lava observers are preserved; resolve their bindings explicitly.");
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                var dust = AssetDatabase.LoadAssetAtPath<GameObject>(DustPrefab);
                var smoke = AssetDatabase.LoadAssetAtPath<Material>("Assets/NWH/Vehicle Physics 2/Effects/Materials/DustCloud.mat");
                var flare = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTerrierRacing/Materials/Transparent comic flare.mat");
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/WeatherMaker/Prefab/Textures/Water/WeatherMakerWaterBump.png");
                if (shader == null || dust == null || smoke == null || flare == null || normal == null) throw new InvalidOperationException("Installed NWH/Weather Maker rendering content required.");
                if (dust.GetComponentsInChildren<Component>(true).Any(component => !(component is Transform) && !(component is ParticleSystem) && !(component is ParticleSystemRenderer)))
                    throw new InvalidOperationException("The audited SurfaceDust rendering prefab now contains another responsibility.");
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                var molten = MissingMaterial(name + " molten currents", renderer.sharedMaterial, material =>
                { material.shader = shader; material.SetFloat("_FlowSpeed", .45f); material.SetFloat("_ChannelWidth", .16f); material.SetFloat("_PatternScale", .065f); material.SetColor("_HotColor", new Color(7, 2.4f, .08f)); material.enableInstancing = true; });
                var mist = MissingMaterial("NWH soft volcanic steam", smoke, material =>
                {
                    material.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Vehicle Physics 2/Effects/Textures/SmokeTextureSheet.png"));
                    Transparent(material); material.SetFloat("_FlipbookBlending", 0); material.DisableKeyword("_FLIPBOOKBLENDING_ON"); material.DisableKeyword("_DISTORTION_ON"); material.DisableKeyword("_SOFTPARTICLES_ON"); material.DisableKeyword("_FADING_ON");
                });
                var heat = MissingMaterial("URP Weather Maker normal heat refraction", mist, material =>
                {
                    material.SetTexture("_BumpMap", normal); material.SetFloat("_DistortionEnabled", 1); material.SetFloat("_DistortionStrength", .12f); material.SetFloat("_DistortionStrengthScaled", .012f);
                    material.SetFloat("_DistortionBlend", 1); material.EnableKeyword("_DISTORTION_ON"); material.EnableKeyword("_NORMALMAP");
                });
                var glow = MissingMaterial("Owned molten droplet flare", flare, material => { });
                var rootObject = new GameObject("Carnival molten presentation — " + name); SceneManager.MoveGameObjectToScene(rootObject, scene);
                var presentation = rootObject.AddComponent<SportsCarnivalLavaSurface>(); presentation.surface = renderer;
                presentation.originalMaterials = renderer.sharedMaterials; presentation.moltenMaterial = molten;
                presentation.surfaceHeight = renderer.bounds.max.y; presentation.worldEnvelope = renderer.bounds; presentation.existingImpacts = impacts;
                var ambient = new List<ParticleSystem>();
                bool hasHeat = components.OfType<ParticleSystemRenderer>().Any(part => part.sharedMaterial != null && part.sharedMaterial.IsKeywordEnabled("_DISTORTION_ON"));
                bool hasSmoke = components.OfType<ParticleSystem>().Any(part => part.name.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0);
                bool hasEmbers = components.OfType<ParticleSystem>().Any(part => part.name.IndexOf("ember", StringComparison.OrdinalIgnoreCase) >= 0);
                float radius = Mathf.Min(renderer.bounds.extents.x, renderer.bounds.extents.z) * .55f;
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * Mathf.PI * 2 / 3 + .6f;
                    var position = new Vector3(renderer.bounds.center.x + Mathf.Cos(angle) * radius, presentation.surfaceHeight + .4f, renderer.bounds.center.z + Mathf.Sin(angle) * radius);
                    ambient.Add(Mist(rootObject.transform, dust, mist, "Slow volcanic steam " + i, position, false, false));
                    if (!hasHeat) ambient.Add(Mist(rootObject.transform, dust, heat, "Bounded URP heat refraction " + i, position + Vector3.up * 2, true, false));
                    if (!hasSmoke) ambient.Add(Mist(rootObject.transform, dust, mist, "NWH volcanic vent smoke " + i, position, false, true));
                    if (!hasEmbers) ambient.Add(Spray(rootObject.transform, glow, "Drifting molten embers " + i, position, true, false, 32, 4));
                    ambient.Add(Spray(rootObject.transform, glow, "Occasional molten bubble burst " + i, position, true, true, 16, 0));
                }
                presentation.ambientEffects = ambient.ToArray();
                presentation.reactionPool = new ParticleSystem[4];
                for (int i = 0; i < presentation.reactionPool.Length; i++)
                    presentation.reactionPool[i] = Spray(rootObject.transform, glow, "Pooled contact molten droplets " + i, renderer.bounds.center, false, true, 48, 0);
                foreach (var contact in contacts)
                {
                    var observer = contact.GetComponent<SportsCarnivalLavaContact>();
                    if (observer != null) throw new InvalidOperationException("Existing custom lava observer is preserved; resolve its binding explicitly.");
                    observer = contact.gameObject.AddComponent<SportsCarnivalLavaContact>(); observer.presentation = presentation; EditorUtility.SetDirty(observer);
                }
                // Per-camera opaque copy is the installed URP distortion API.
                // No renderer feature, global pipeline asset, light or volume changes.
                foreach (var camera in components.OfType<Camera>()) { var data = camera.GetUniversalAdditionalCameraData(); data.requiresColorTexture = true; EditorUtility.SetDirty(data); }
                renderer.sharedMaterial = molten; EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(presentation);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("CARNIVAL_LAVA_BOUND " + name + " newAmbient=" + ambient.Count + " pooledReactions=4; original triggers, colliders, gameplay, lighting, vendor sources and existing atmosphere retained. Native rendering acceptance pending.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        static Material MissingMaterial(string name, Material source, Action<Material> configure)
        {
            string path = Folder + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(source) { name = name }; configure(material); AssetDatabase.CreateAsset(material, path); return material;
        }
        static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1); material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
        }
        static ParticleSystem Mist(Transform parent, GameObject source, Material material, string name, Vector3 position, bool heat, bool smoke)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(source); PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = name; root.transform.SetParent(parent, false); root.transform.SetPositionAndRotation(position, Quaternion.Euler(-90, 0, 0));
            var system = root.GetComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.loop = true; main.playOnAwake = true; main.useUnscaledTime = false; main.maxParticles = heat ? 16 : 24;
            main.startLifetime = new ParticleSystem.MinMaxCurve(heat ? 4 : 7, heat ? 6 : 12); main.startSpeed = new ParticleSystem.MinMaxCurve(.5f, smoke ? 3 : 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(heat ? 12 : 5, heat ? 24 : 12); main.startColor = heat ? new Color(1, 1, 1, .12f) : smoke ? new Color(.12f, .1f, .08f, .2f) : new Color(.8f, .78f, .7f, .12f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission; emission.rateOverTime = heat ? 1 : 1.5f; emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = heat ? 8 : 4; shape.angle = 12;
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            if (heat) renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Tangent, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            var color = system.colorOverLifetime; color.enabled = true; color.color = Fade(); return system;
        }
        static ParticleSystem Spray(Transform parent, Material material, string name, Vector3 position, bool loop, bool burst, int budget, float rate)
        {
            var system = new GameObject(name).AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.transform.SetParent(parent, false); system.transform.SetPositionAndRotation(position, Quaternion.Euler(-90, 0, 0));
            var main = system.main; main.loop = loop; main.playOnAwake = loop; main.useUnscaledTime = false; main.maxParticles = budget; main.duration = loop ? 7 + parent.childCount % 4 : 3;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, loop && !burst ? 5 : 1.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(loop ? 1 : 4, loop ? 4 : 12);
            main.startSize = new ParticleSystem.MinMaxCurve(loop ? .1f : .2f, loop ? .35f : .7f); main.gravityModifier = loop && !burst ? .05f : 1;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(3, .3f, .01f), new Color(5, 1.5f, .04f)); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission; emission.rateOverTime = rate; emission.SetBursts(burst ? new[] { new ParticleSystem.Burst(loop ? 2 : 0, (short)(loop ? 8 : 36)) } : Array.Empty<ParticleSystem.Burst>());
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = loop ? 5 : .7f; shape.angle = 30;
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var color = system.colorOverLifetime; color.enabled = true; color.color = Fade(); return system;
        }
        static Gradient Fade()
        {
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.8f, .16f), new GradientAlphaKey(0, 1) }); return gradient;
        }
    }
}
