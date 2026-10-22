using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckCheeseRolling.Editor
{
    public static class CheeseExplosionPresentationAuthoring
    {
        const string Private = "Assets/TruckCheeseRolling/PrivateLicensedVfx/";
        const string Source = Private + "Source/Particle-Example/Explosion3.prefab";
        const string Prefab = Private + "Resources/CheeseOwnedExplosion.prefab";
        static Material ParticleMaterial(Material source, Shader shader, Dictionary<Material, Material> materials)
        {
            if (source == null) throw new InvalidOperationException("Owned GAPH particle material dependency missing.");
            if (materials.TryGetValue(source, out var existing)) return existing;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)); string path = Private + "Materials/URP " + source.name.Replace('/', '_') + " " + guid.Substring(0, 8) + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader); var texture = source.mainTexture; Color tint = source.HasProperty("_TintColor") ? source.GetColor("_TintColor") : Color.white;
                material.SetTexture("_BaseMap", texture); material.SetTexture("_MainTex", texture); material.SetColor("_BaseColor", tint); material.SetColor("_Color", tint);
                bool alpha = source.name.IndexOf("Smoke", StringComparison.OrdinalIgnoreCase) >= 0 || source.name.IndexOf("Alpha", StringComparison.OrdinalIgnoreCase) >= 0;
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", alpha ? 0 : 2); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)(alpha ? BlendMode.OneMinusSrcAlpha : BlendMode.One)); material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0); material.SetOverrideTag("RenderType", "Transparent"); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(material, path);
            }
            materials.Add(source, material); return material;
        }
        [MenuItem("Truck Sports/Cheese Rolling/Upgrade pooled owned explosion visuals")]
        public static void Upgrade()
        {
            if (!File.Exists(Private + "intake.json")) throw new InvalidOperationException("Run Tools/TruckSports/ImportCheeseVfxAssets.py using the owned Particle Ingredient Pack first.");
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); if (shader == null) throw new InvalidOperationException("Installed URP particle shader required.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source); if (source == null) throw new InvalidOperationException("Owned GAPH explosion example could not be imported.");
            var scene = EditorSceneManager.OpenScene(CheeseAuthoring.Scene); var match = UnityEngine.Object.FindAnyObjectByType<CheeseMatch>();
            if (match.explosions.Length != 8) throw new InvalidOperationException("Preserve the existing eight-entry blast pool.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (prefab == null)
            {
                Directory.CreateDirectory(Private + "Resources"); Directory.CreateDirectory(Private + "Materials"); AssetDatabase.Refresh();
                var root = (GameObject)PrefabUtility.InstantiatePrefab(source); PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); root.name = "Owned GAPH layered Cheese explosion"; root.SetActive(false);
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(component);
                foreach (var component in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(component);
                foreach (var component in root.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(component);
                foreach (var component in root.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(component);
                foreach (var component in root.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(component);
                var systems = root.GetComponentsInChildren<ParticleSystem>(true); if (systems.Length < 3) throw new InvalidOperationException("The selected owned explosion must include multiple real effect layers.");
                var materials = new Dictionary<Material, Material>();
                foreach (var system in systems)
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); var main = system.main; main.loop = false; main.playOnAwake = false; main.stopAction = ParticleSystemStopAction.None; main.maxParticles = Mathf.Clamp(800 / systems.Length, 16, 160);
                    var collision = system.collision; collision.enabled = false; var triggers = system.trigger; triggers.enabled = false;
                    var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterials = renderer.sharedMaterials.Select(m => ParticleMaterial(m, shader, materials)).ToArray();
                }
                root.transform.localScale *= 2.5f; prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefab); UnityEngine.Object.DestroyImmediate(root);
                Debug.Log("CHEESE_OWNED_EXPLOSION_LAYERS " + string.Join(" | ", prefab.GetComponentsInChildren<ParticleSystem>(true).Select(p => p.name)));
            }
            bool changed = false;
            foreach (var system in match.explosions) if (system.GetComponent<CheeseExplosionPresentation>() == null) { system.gameObject.AddComponent<CheeseExplosionPresentation>(); changed = true; }
            if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); }
            Debug.Log("CHEESE_OWNED_EXPLOSION_PRESENTATION " + (changed ? "integrated" : "preserved") + " original eight pool roots, lifecycle, recovery safety and all blast mechanics; licensed effect source/private derivatives excluded from public Git.");
        }
    }
}
