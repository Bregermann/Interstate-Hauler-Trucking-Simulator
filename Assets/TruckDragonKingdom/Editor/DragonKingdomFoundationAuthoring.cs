#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static class DragonKingdomFoundationAuthoring
    {
        [Serializable] sealed class Receipt
        {
            public string scope = "EXPLICIT_EDITOR_ASSET_AUTHORING_NOT_RUNTIME_ACCEPTANCE";
            public string catalog;
            public SpeciesReceipt[] species;
        }
        [Serializable] sealed class SpeciesReceipt
        {
            public string id, visual, configuredPrefab;
            public string[] actions;
            public int renderers, bones, animatorStates;
        }
        [MenuItem("Truck Sports/Dragon Kingdom/Create NEW Configured Dragon Assets")]
        public static void CreateDragonAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            var catalog = AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath)
                ?? DragonSpeciesAuthoring.CreateCatalog();
            var issues = new List<string>(); catalog.CollectValidationIssues(issues);
            if (issues.Count != 0) throw new InvalidOperationException(string.Join("\n", issues));
            var facts = new List<SpeciesReceipt>();
            foreach (var species in catalog.species)
            {
                string folder = "Assets/TruckDragonKingdom/Prefabs/Dragons/" + species.id;
                var prefab = DragonAnimalMotorAuthoring.BuildConfiguredPrefab(species, folder);
                prefab = DragonAnimalMotorAuthoring.UpgradeConfiguredPrefab(species, prefab, folder);
                species.breathEffect = DragonAnimalMotorAuthoring.BuildBreathEffectVariant(species, folder);
                var actor = DragonAnimalMotorAuthoring.BuildActorPrefab(species, prefab, folder);
                actor = DragonAnimalMotorAuthoring.UpgradeActorMouthPrefab(species, actor, folder);
                var motor = prefab.GetComponentInChildren<DragonAnimalMotor>(true);
                if (motor == null || motor.animator.avatar == null || !motor.animator.avatar.isValid)
                    throw new InvalidOperationException("Actual configured motor/Avatar missing: " + species.id);
                facts.Add(new SpeciesReceipt {
                    id = species.id, visual = AssetDatabase.GetAssetPath(species.visualPrefab), configuredPrefab = AssetDatabase.GetAssetPath(actor),
                    actions = species.animations.Select(a => a.action + " | " + AssetDatabase.GetAssetPath(a.clip) + " | " + a.sourceNote).ToArray(),
                    renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,
                    bones = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(r => r.bones.Length),
                    animatorStates = motor.animator.runtimeAnimatorController.animationClips.Length
                });
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsDragonReceipt");
            if (at >= 0 && at + 1 < args.Length)
            {
                string output = Path.GetFullPath(args[at + 1]); Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(new Receipt { catalog = DragonSpeciesAuthoring.CatalogPath, species = facts.ToArray() }, true));
            }
            Debug.Log("DRAGON_KINGDOM_ASSETS_AUTHORED " + facts.Count + " actual configured species. Rendered flight/action acceptance remains required.");
        }
    }
}
#endif
