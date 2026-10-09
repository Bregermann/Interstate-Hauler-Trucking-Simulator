using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using sc.splines.spawner.runtime;

namespace LWS.TruckTaxi.Editor
{
    public static partial class TruckTaxiWorldGeometryAuthoring
    {
        public const string DressingProfileFolder = "Assets/LWS/TruckTaxi/Authoring/Dressing";
        [MenuItem("Truck Taxi/World Dressing/Create Missing Context Profiles")]
        public static void CreateMissingDressingProfiles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
            string parent = "Assets";
            foreach (var part in DressingProfileFolder.Split('/').Skip(1))
            {
                string path = parent + "/" + part;
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, part);
                parent = path;
            }
            foreach (TruckTaxiDressingContext context in Enum.GetValues(typeof(TruckTaxiDressingContext)))
            {
                string path = DressingProfileFolder + "/" + context + ".asset";
                var existing = AssetDatabase.LoadMainAssetAtPath(path);
                if (existing != null)
                {
                    if (!(existing is TruckTaxiDressingProfile)) throw new InvalidOperationException("Asset path occupied: " + path);
                    continue;
                }
                var profile = ScriptableObject.CreateInstance<TruckTaxiDressingProfile>(); profile.context = context;
                AssetDatabase.CreateAsset(profile, path);
                Undo.RegisterCreatedObjectUndo(profile, "Create dressing profile");
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            Debug.Log("Eight context profile slots prepared; existing assets preserved. Pools intentionally empty until approved real scenery prefabs are assigned. No scene generation.");
        }

        [MenuItem("Truck Taxi/World Dressing/Bind Selected Existing Vendor Spawner")]
        public static void BindSelectedDressingSpawner()
        {
            var target = Selection.activeGameObject; RequireEditableSceneObject(target);
            var spawner = target.GetComponent<SplineSpawner>();
            if (spawner == null) throw new InvalidOperationException("Select an existing vendor SplineSpawner; no new generator is created.");
            var binding = target.GetComponent<TruckTaxiDressingBinding>();
            if (binding == null) binding = Undo.AddComponent<TruckTaxiDressingBinding>(target);
            Undo.RecordObject(binding, "Bind vendor dressing"); binding.spawner = spawner;
            Undo.RecordObject(spawner, "Disable automatic dressing generation");
            spawner.startupBehaviour = SplineSpawner.StartupBehaviour.None;
            spawner.respawnTriggers = SplineSpawner.RespawnTriggers.None; spawner.spawnObjects = false;
            EditorUtility.SetDirty(spawner); PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);
            if (string.IsNullOrWhiteSpace(binding.stableId)) binding.stableId = "dressing." + Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(binding); PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
            EditorSceneManager.MarkSceneDirty(target.scene); Selection.activeObject = binding;
            Debug.Log("Binding added only. Assign profile, road graph and complete exclusion references, then validate. No objects spawned or scene saved.");
        }

        private static TruckTaxiDressingBinding SelectedDressing()
        {
            var target = Selection.activeGameObject; RequireEditableSceneObject(target);
            var binding = target.GetComponent<TruckTaxiDressingBinding>();
            if (binding == null) throw new InvalidOperationException("Select a dressing binding.");
            foreach (var other in Resources.FindObjectsOfTypeAll<TruckTaxiDressingBinding>())
                if (other != binding && !EditorUtility.IsPersistent(other) && other.gameObject.scene.IsValid() &&
                    other.stableId == binding.stableId) throw new InvalidOperationException("Duplicate loaded dressing binding ID: " + binding.stableId);
            if (!binding.Validate(out string reason)) throw new InvalidOperationException(reason);
            return binding;
        }
        [MenuItem("Truck Taxi/World Dressing/Validate Selected Binding")]
        public static void ValidateSelectedDressing()
        {
            var binding = SelectedDressing();
            Debug.Log("Dressing references valid: " + binding.stableId + ". Review exclusion completeness and content art; no generation performed.");
        }
        [MenuItem("Truck Taxi/World Dressing/Generate Selected Binding Explicitly")]
        public static void GenerateSelectedDressing()
        {
            var binding = SelectedDressing();
            if (!EditorUtility.DisplayDialog("Generate selected dressing", "Vendor Respawn will replace only this dedicated spawner's generated output. Source scene remains unsaved. Continue?", "Generate", "Cancel")) return;
            Undo.RegisterFullObjectHierarchyUndo(binding.gameObject, "Generate vendor dressing");
            Undo.RecordObject(binding.spawner, "Configure vendor dressing");
            // Vendor retains all distribution, instantiation and object-container ownership.
            bool generated;
            string reason;
            try { generated = binding.GenerateExplicitly(out reason); }
            finally
            {
                EditorUtility.SetDirty(binding.spawner); PrefabUtility.RecordPrefabInstancePropertyModifications(binding.spawner);
                EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);
            }
            if (!generated) throw new InvalidOperationException(reason);
            if (!string.IsNullOrEmpty(binding.LastFailure)) throw new InvalidOperationException(binding.LastFailure);
            Debug.Log("Vendor dressing generated for " + binding.stableId + "; rejected=" + binding.LastRejected +
                "; instance count=" + binding.spawner.InstanceCount + ". Review before explicitly saving the scene.");
        }
    }
}
