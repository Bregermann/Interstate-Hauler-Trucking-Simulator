using UnityEditor;
using UnityEngine;

namespace LWS.InterstateHauler.Editor
{
    public static class LwsUtsTrafficProfileBuilder
    {
        public const string ProfilePath = "Assets/LWS/InterstateHauler/Traffic/Data/IH_TrafficProfile_InterstateValidation.asset";

        private static readonly string[] TrafficPrefabPaths =
        {
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_3.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_5.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Jeep.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Taxi.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_2.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/City_bus.prefab"
        };

        [MenuItem("Interstate Hauler/Traffic/Repair Interstate Validation Traffic Profile")]
        public static void RepairInterstateValidationTrafficProfile()
        {
            LwsUtsTrafficProfile profile = AssetDatabase.LoadAssetAtPath<LwsUtsTrafficProfile>(ProfilePath);
            bool created = false;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<LwsUtsTrafficProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
                created = true;
            }

            var serialized = new SerializedObject(profile);
            serialized.FindProperty("trafficEnabled").boolValue = true;

            if (created)
            {
                SerializedProperty policy = serialized.FindProperty("spawnPolicy");
                policy.FindPropertyRelative("densityTier").enumValueIndex = (int)LwsTrafficDensityTier.Dense;
                policy.FindPropertyRelative("maxActiveVehicles").intValue = 32;
                policy.FindPropertyRelative("spawnIntervalSeconds").floatValue = 1.25f;
                policy.FindPropertyRelative("minimumPlayerSpawnDistanceMeters").floatValue = 120f;
                policy.FindPropertyRelative("maximumPlayerSpawnDistanceMeters").floatValue = 1800f;
                policy.FindPropertyRelative("despawnDistanceMeters").floatValue = 2600f;
                policy.FindPropertyRelative("despawnNearLaneEndMeters").floatValue = 90f;
                policy.FindPropertyRelative("targetCruiseSpeedScale").floatValue = 0.72f;
                policy.FindPropertyRelative("maximumTrafficSpeedMetersPerSecond").floatValue = 22f;
                policy.FindPropertyRelative("includeRampTraffic").boolValue = true;
                policy.FindPropertyRelative("includeTurnaroundTraffic").boolValue = false;
                policy.FindPropertyRelative("showDebugPanel").boolValue = true;
            }

            serialized.FindProperty("spawnPolicy").FindPropertyRelative("autoResolveEditorPrefabs").boolValue = false;

            SerializedProperty prefabs = serialized.FindProperty("trafficPrefabs");
            prefabs.arraySize = TrafficPrefabPaths.Length;
            for (int i = 0; i < TrafficPrefabPaths.Length; i++)
            {
                prefabs.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(TrafficPrefabPaths[i]);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
