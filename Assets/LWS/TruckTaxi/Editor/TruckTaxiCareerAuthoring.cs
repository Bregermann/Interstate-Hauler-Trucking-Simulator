using System;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiCareerAuthoring
    {
        private const string Root="Assets/LWS/TruckTaxi/Resources/TruckTaxi/Career";
        private const string StarterDefinitionPath="Assets/LWS/InterstateHauler/Vehicles/Data/IH_TruckDefinition_StarterNwhSemi.asset";
        [MenuItem("Truck Taxi/Career/Migrate Starter Catalog To Metadata")]
        public static void MigrateStarterCatalogToMetadata()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before Career migration.");
            var catalog=AssetDatabase.LoadAssetAtPath<TruckTaxiCareerCatalog>(Root+"/Catalog.asset");
            if(catalog==null || EditorUtility.IsDirty(catalog))
                throw new InvalidOperationException("An existing, unmodified-in-memory Career catalog is required.");
            var truck=AssetDatabase.LoadAssetAtPath<LwsTruckDefinition>(StarterDefinitionPath);
            if(MigrateStarterEntry(catalog,truck))
            { EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog); }
            Debug.Log("Starter Career entry is metadata-only. No prefab or other asset was saved.");
        }

        // In-memory seam for focused tests; only the command above saves the catalog asset.
        public static bool MigrateStarterEntry(TruckTaxiCareerCatalog catalog,LwsTruckDefinition truck)
        {
            if(catalog==null || truck==null || string.IsNullOrWhiteSpace(truck.StableId) || truck.StableId.Length>256 ||
                string.IsNullOrWhiteSpace(truck.DisplayName)) throw new InvalidOperationException("Valid starter metadata is required.");
            if(!catalog.Validate(out string reason)) throw new InvalidOperationException(reason);
            int index=Array.FindIndex(catalog.trucks,item=>item.StableId==truck.StableId);
            if(index<0) throw new InvalidOperationException("Existing starter catalog entry was not found.");
            var entry=catalog.trucks[index];
            if(entry.truck!=null && entry.truck!=truck)
                throw new InvalidOperationException("Starter entry references a different authored definition.");
            if(entry.truck==null && entry.stableId==truck.StableId && entry.displayName==truck.DisplayName && !entry.availableForPurchase)
                return false;
            string before=EditorJsonUtility.ToJson(catalog);
            var expected=ScriptableObject.CreateInstance<TruckTaxiCareerCatalog>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(before,expected);
                var target=expected.trucks[index];
                target.stableId=truck.StableId; target.displayName=truck.DisplayName;
                target.truck=null; target.availableForPurchase=false;
                var serialized=new SerializedObject(catalog);
                var item=serialized.FindProperty("trucks").GetArrayElementAtIndex(index);
                item.FindPropertyRelative("stableId").stringValue=truck.StableId;
                item.FindPropertyRelative("displayName").stringValue=truck.DisplayName;
                item.FindPropertyRelative("truck").objectReferenceValue=null;
                item.FindPropertyRelative("availableForPurchase").boolValue=false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if(!catalog.Validate(out reason) || EditorJsonUtility.ToJson(catalog)!=EditorJsonUtility.ToJson(expected))
                    throw new InvalidOperationException("Catalog migration changed unexpected data or failed validation: "+reason);
                return true;
            }
            catch { EditorJsonUtility.FromJsonOverwrite(before,catalog); throw; }
            finally { UnityEngine.Object.DestroyImmediate(expected); }
        }
        [MenuItem("Truck Taxi/Career/Create Missing Starter Catalog")]
        public static void CreateMissingStarterCatalog()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring Career content.");
            var truck=AssetDatabase.LoadAssetAtPath<LwsTruckDefinition>(StarterDefinitionPath);
            if(truck==null || string.IsNullOrWhiteSpace(truck.StableId) || truck.StableId.Length>256 || string.IsNullOrWhiteSpace(truck.DisplayName))
                throw new InvalidOperationException("Existing starter truck definition metadata is required.");
            string parent="Assets/LWS/TruckTaxi";
            foreach(string segment in new[]{"Resources","TruckTaxi","Career"})
            { string path=parent+"/"+segment; if(!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent,segment); parent=path; }
            if(AssetDatabase.LoadAssetAtPath<TruckTaxiCareerCatalog>(Root+"/Catalog.asset")!=null)
            { Debug.Log("Existing Career catalog retained; no authored values overwritten."); return; }
            var catalog=ScriptableObject.CreateInstance<TruckTaxiCareerCatalog>();
            catalog.trucks=new[]{new TruckTaxiCareerTruckDefinition{stableId=truck.StableId,displayName=truck.DisplayName,
                truck=null,description="Current starter tractor",availableForPurchase=false}};
            catalog.upgrades=new[]{
                Upgrade(truck,"engine-calibration","Engine Calibration",TruckTaxiUpgradeCategory.Engine,1.1f,150000),
                Upgrade(truck,"brake-hardware","Brake Hardware",TruckTaxiUpgradeCategory.Brakes,1.15f,100000),
                Upgrade(truck,"auxiliary-tank","Auxiliary Fuel Tank",TruckTaxiUpgradeCategory.Fuel,1.25f,75000)};
            if(!catalog.Validate(out string reason)) throw new InvalidOperationException(reason);
            AssetDatabase.CreateAsset(catalog,Root+"/Catalog.asset"); AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Starter Career catalog created using existing NWH truck definition. No scene or vendor asset changed.");
        }
        private static TruckTaxiCareerUpgradeDefinition Upgrade(LwsTruckDefinition truck,string id,string name,TruckTaxiUpgradeCategory category,float multiplier,long price)
        {
            string path=Root+"/"+id+".asset";
            var tuning=AssetDatabase.LoadAssetAtPath<TruckTaxiCareerUpgradeTuning>(path);
            if(tuning==null)
            {
                tuning=ScriptableObject.CreateInstance<TruckTaxiCareerUpgradeTuning>(); tuning.multiplier=multiplier;
                AssetDatabase.CreateAsset(tuning,path);
            }
            return new TruckTaxiCareerUpgradeDefinition{stableId="taxi.upgrade."+id,displayName=name,
                description="NWH " + category + " multiplier " + multiplier.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),
                category=category,compatibleTruckIds=new[]{truck.StableId},priceCents=price,
                adapterId=TruckTaxiCareerUpgradeTuning.AdapterId,adapterConfiguration=tuning,availableForPurchase=true};
        }
    }
}
