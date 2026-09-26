using LWS.TruckTaxi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiCorrectionSetup
    {
        [MenuItem("Truck Taxi/Configure Service Capabilities")]
        public static void ConfigureServices()
        {
            if (EditorApplication.isPlaying) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != TruckTaxiMainMenu.FreePlaySceneName) throw new System.InvalidOperationException("Open the Truck Taxi demo scene first.");
            TruckTaxiServicePoint.BindExistingPoints(scene);
            TruckTaxiGasStationPoint first = null;
            foreach (var gas in Object.FindObjectsByType<TruckTaxiGasStationPoint>(FindObjectsSortMode.None))
                if (gas.gameObject.scene == scene && (first == null || string.CompareOrdinal(gas.stableId, first.stableId) < 0)) first = gas;
            if (first == null) throw new System.InvalidOperationException("The existing sandbox gas/recovery bay is required.");
            var service = first.GetComponent<TruckTaxiServicePoint>();
            service.displayName = first.displayName + " / REPAIR & RECOVERY";
            service.capabilities |= TruckTaxiServiceCapability.RepairEngine | TruckTaxiServiceCapability.RepairTransmission |
                TruckTaxiServiceCapability.RepairGeneralDamage | TruckTaxiServiceCapability.RepairTires | TruckTaxiServiceCapability.RecoverVehicle;
            EditorUtility.SetDirty(service);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
