using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiNeedsPresentationAuthoring
    {
        // Batch entry point: existing canonical asset only, no scene generation or global SaveAssets.
        public static void ConfigureExistingEnvironmentPools()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TruckTaxiEnvironmentSettings>(TruckTaxiEnvironmentSetup.SettingsPath);
            if (settings == null) throw new System.InvalidOperationException("Existing environment settings not found: " + TruckTaxiEnvironmentSetup.SettingsPath);
            if ((!HasImage(settings.hungerCravingPool) || !HasImage(settings.thirstCravingPool)) && !ConfigureEmptyPools(settings))
                throw new System.InvalidOperationException("Import needs images before configuring empty pools.");
            MigrateLegacyThirstRate(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            Debug.Log("NEEDS AUTHORING: baseline thirst " + (settings.thirstGainPerGameHour * 100).ToString("0.###") +
                "% per game hour; existing custom rates and nonempty pools preserved.", settings);
        }

        public static bool MigrateLegacyThirstRate(TruckTaxiEnvironmentSettings settings)
        {
            // Exact known prior default only. Nearby custom values are not migration candidates.
            if (settings == null || settings.thirstGainPerGameHour != .2f) return false;
            Undo.RecordObject(settings, "Migrate legacy thirst rate");
            settings.thirstGainPerGameHour = .05f;
            EditorUtility.SetDirty(settings);
            Debug.Log("NEEDS AUTHORING: migrated legacy thirst 20% -> 5% per game hour.", settings);
            return true;
        }

        [MenuItem("Tools/Truck Taxi/Needs/Configure Selected Settings Image Pools")]
        private static void ConfigureSelected()
        {
            var settings = Selection.activeObject as TruckTaxiEnvironmentSettings;
            if (settings == null) { Debug.LogError("Select a TruckTaxiEnvironmentSettings asset first."); return; }
            ConfigureEmptyPools(settings);
        }

        // Deliberately does not save, regenerate a scene, or overwrite authored nonempty pools.
        public static bool ConfigureEmptyPools(TruckTaxiEnvironmentSettings settings)
        {
            if (settings == null) return false;
            var burger = Load("Hunger"); var pizza = Load("Pizza");
            var water = Load("Thirst"); var soda = Load("Soda");
            if (burger == null || pizza == null || water == null || soda == null)
            { Debug.LogError("Needs pools require all four imported food/drink images."); return false; }
            Undo.RecordObject(settings, "Configure needs image pools");
            bool changed = false;
            if (!HasImage(settings.hungerCravingPool)) { settings.hungerCravingPool = new[] { burger, pizza }; changed = true; }
            if (!HasImage(settings.thirstCravingPool)) { settings.thirstCravingPool = new[] { water, soda }; changed = true; }
            if (changed) EditorUtility.SetDirty(settings);
            return true;
        }
        private static bool HasImage(Texture2D[] pool)
        { if (pool != null) foreach (var image in pool) if (image != null) return true; return false; }
        private static Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/LWS/TruckTaxi/Resources/TruckTaxi/Needs/" + name + ".png");
    }
}
