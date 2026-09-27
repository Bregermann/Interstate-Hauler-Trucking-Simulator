using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiRosterVisualApply
    {
        private const string MaterialFolder = TruckTaxiPassengerFactoryBuilder.Root + "/Materials/RosterVisuals";
        private const string DesignVersion = "RosterVisual-v1-";
        private static readonly HashSet<string> FactoryParts = new HashSet<string>(StringComparer.Ordinal) {
            "Body", "Pants", "Left sleeve", "Right sleeve", "Left hand", "Right hand", "Head",
            "Left foot", "Right foot", "Left eye", "Right eye", "Left clothed chest", "Right clothed chest",
            "Tail", "Left wing", "Right wing", "Racing cap", "Cap brim", "Stylized hair",
            "Wide hat", "Service badge", "Visor"
        };
        public sealed class Result
        {
            public int applied;
            public readonly List<string> skipped = new List<string>();
            public readonly List<string> failed = new List<string>();
        }

        [MenuItem("Truck Taxi/Passenger Factory Tools/Apply Themed Wobble Roster Visuals")]
        public static void ApplyMenu()
        {
            var result = ApplyAll();
            Debug.Log($"TRUCK TAXI ROSTER VISUALS: {result.applied} applied; {result.skipped.Count} preserved/skipped; {result.failed.Count} failed.\n" +
                string.Join("\n", result.skipped.Concat(result.failed)));
        }

        // Call after EnsureRoster/Configure. This only replaces fallback Wobble children,
        // never a supplied model or a runtime prefab whose actor is not marked fallback.
        public static Result ApplyAll()
        {
            var db = AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(TruckTaxiPassengerFactoryBuilder.DatabasePath);
            if (db == null) throw new InvalidOperationException("Register the passenger database before applying roster visuals.");
            var profiles = db.passengers ?? Array.Empty<PassengerProfile>();
            var missing = profiles.Where(p => p != null && !TruckTaxiRosterVisualCatalog.TryGet(p.passengerId, out _))
                .Select(p => p.passengerId).ToArray();
            if (missing.Length != 0) throw new InvalidOperationException("Missing intentional Wobble designs: " + string.Join(", ", missing));
            var duplicate = profiles.Where(p => p != null).GroupBy(p => p.passengerId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null) throw new InvalidOperationException("Duplicate registered passenger: " + duplicate.Key);

            TruckTaxiPassengerDialogueAuthoring.EnsureFolder(MaterialFolder);
            var result = new Result();
            foreach (var p in profiles)
            {
                if (p == null) { result.skipped.Add("<null>: missing profile"); continue; }
                if (p.modelPrefab != null) { result.skipped.Add(p.passengerId + ": authored model"); continue; }
                if (p.appearance == null || p.appearance.visualStyle != TruckTaxiVisualStyle.WobblePeople)
                { result.skipped.Add(p.passengerId + ": non-Wobble appearance"); continue; }
                if (p.runtimePrefab == null)
                { result.failed.Add(p.passengerId + ": missing runtime prefab; run Configure Requested Cast first"); continue; }

                string path = AssetDatabase.GetAssetPath(p.runtimePrefab);
                if (string.IsNullOrEmpty(path) || !path.StartsWith(TruckTaxiPassengerFactoryBuilder.Root + "/Prefabs/", StringComparison.Ordinal))
                { result.skipped.Add(p.passengerId + ": external or non-factory prefab"); continue; }
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    var actor = root.GetComponent<TruckTaxiPassengerActor>();
                    if (actor == null || !actor.fallbackModel)
                    { result.skipped.Add(p.passengerId + ": authored runtime presentation"); continue; }
                    var old = root.transform.Find(TruckTaxiWobbleVisual.VisualName);
                    if (old == null)
                    { result.skipped.Add(p.passengerId + ": no replaceable Wobble child"); continue; }
                    if (old.Find(DesignVersion + p.passengerId) != null)
                    { result.skipped.Add(p.passengerId + ": themed visual already applied"); continue; }
                    if (HasAuthoredParts(p.appearance) || old.Cast<Transform>().Any(t => !FactoryParts.Contains(t.name)))
                    { result.skipped.Add(p.passengerId + ": authored Wobble additions"); continue; }
                    TruckTaxiRosterVisualCatalog.TryGet(p.passengerId, out var entry);
                    var design = MakeDesign(p, entry);
                    Object.DestroyImmediate(old.gameObject);
                    var themed = TruckTaxiWobbleVisual.CreateThemed(root.transform, design);
                    new GameObject(DesignVersion + p.passengerId).transform.SetParent(themed.transform, false);
                    if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                        throw new InvalidOperationException("Prefab save failed.");
                    result.applied++;
                }
                catch (Exception ex) { result.failed.Add(p.passengerId + ": " + ex.Message); }
                finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static bool HasAuthoredParts(TruckTaxiAppearanceProfile a) =>
            a.head != null || a.hair != null || a.facialHair != null || a.eyes != null || a.glasses != null ||
            a.hat != null || a.upperClothing != null || a.lowerClothing != null || a.shoes != null ||
            (a.accessories != null && a.accessories.Any(x => x != null));

        public static TruckTaxiWobbleVisual.Design MakeDesign(PassengerProfile p, TruckTaxiRosterVisualCatalog.Entry entry)
        {
            // This API also gives the parent a straightforward template for red adult roadside NPCs:
            // supply persistent red/body, dark/clothes, accent and skin materials to CreateThemed.
            bool human = p.casting == null || p.casting.human;
            return new TruckTaxiWobbleVisual.Design {
                silhouette = entry.silhouette, headwear = entry.headwear, accessory = entry.accessory,
                height = p.appearance.heightMeters, width = entry.width, headScale = entry.headScale,
                upperBodyScale = p.appearance.stylizedUpperBodyScale,
                clothedAdult = p.explicitlyAdult && p.minimumAdultAge >= 21 && p.adultFemalePresentation && human,
                body = PaletteMaterial(entry.bodyHex), clothes = PaletteMaterial(entry.clothesHex), accent = PaletteMaterial(entry.accentHex),
                skin = human ? PaletteMaterial(SkinColor(p.passengerId)) : PaletteMaterial(entry.bodyHex)
            };
        }

        private static string SkinColor(string id)
        {
            // Stable visual variety independent of race/ethnicity labels.
            string[] tones = { "E8C5A0", "C99468", "AA704F", "80563E", "D4A779" };
            uint hash = 2166136261;
            foreach (char c in id ?? string.Empty) { hash ^= c; hash = unchecked(hash * 16777619); }
            return tones[hash % (uint)tones.Length];
        }

        private static Material PaletteMaterial(string hex)
        {
            string path = MaterialFolder + "/" + hex + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No compatible material shader for Wobble visuals.");
            mat = new Material(shader) { color = TruckTaxiRosterVisualCatalog.Color(hex) };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
