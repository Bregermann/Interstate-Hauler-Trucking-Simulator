using System.IO;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiAnalyticsConfiguration
    {
        [MenuItem("Tools/Truck Taxi/Analytics/Create External Local Configuration")]
        public static void CreateLocalConfiguration()
        {
            string path = Path.Combine(Application.persistentDataPath, "analytics.local.json");
            Directory.CreateDirectory(Application.persistentDataPath);
            if (!File.Exists(path))
                TruckTaxiAnalyticsStorage.AtomicWrite(path, JsonUtility.ToJson(new TruckTaxiAnalyticsLocalConfig(), true));
            EditorUtility.RevealInFinder(path);
        }
        [MenuItem("Tools/Truck Taxi/Analytics/Open Local Analytics Folder")]
        public static void OpenLocalFolder()
        {
            string path = Path.Combine(Application.persistentDataPath, "Analytics");
            if (Directory.Exists(path)) EditorUtility.RevealInFinder(path);
        }
        [MenuItem("Tools/Truck Taxi/Analytics/Export Active Session CSV")]
        public static void ExportCsv()
        {
            var analytics = TruckTaxiBootstrap.Instance != null ? TruckTaxiBootstrap.Instance.GetComponent<TruckTaxiAnalytics>() : null;
            if (analytics != null && analytics.IsInitialized) analytics.ExportCsv();
        }
    }
}
