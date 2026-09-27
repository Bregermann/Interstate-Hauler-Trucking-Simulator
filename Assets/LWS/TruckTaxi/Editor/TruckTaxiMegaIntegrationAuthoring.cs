using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiMegaIntegrationAuthoring
    {
        [MenuItem("Truck Taxi/Regional/Finish Mega World Integration")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene edits first.");
            var scene=EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host=UnityEngine.Object.FindAnyObjectByType<TruckTaxiBootstrap>();
            if(host==null) throw new InvalidOperationException("Taxi bootstrap missing.");
            var root=host.transform.Find("Authored regional hazard zones");
            if(root==null) {root=new GameObject("Authored regional hazard zones").transform; root.SetParent(host.transform,false);}
            Zone(root,"taxi.hazard.open-highway","Highway tornado watch","taxi.highway",TruckTaxiExtremeKind.Tornado,TruckTaxiHazardExposure.OpenTerrain,120,
                new[]{new Vector3(1700,0,130),new Vector3(1900,0,0),new Vector3(2120,0,-100)});
            Zone(root,"taxi.hazard.regional-storm","Regional hurricane warning","taxi.town02",TruckTaxiExtremeKind.Hurricane,TruckTaxiHazardExposure.Coastal,1200,
                new[]{new Vector3(4000,0,0)});
            Zone(root,"taxi.hazard.town-quake","Town earthquake","taxi.town01",TruckTaxiExtremeKind.Earthquake,TruckTaxiHazardExposure.BuiltArea,250,
                new[]{Vector3.zero});
            Zone(root,"taxi.hazard.pass-avalanche","Pinecrest avalanche warning",TruckTaxiVenueLayout.MountainId,TruckTaxiExtremeKind.Avalanche,TruckTaxiHazardExposure.MountainSnow,60,
                new[]{new Vector3(1090,28,-1540),TruckTaxiVenueLayout.MountainSlopeCrossing,new Vector3(1250,0,-1540)});
            Zone(root,"taxi.hazard.pass-mudslide","Pinecrest saturated slope",TruckTaxiVenueLayout.MountainId,TruckTaxiExtremeKind.Mudslide,TruckTaxiHazardExposure.RainSoakedSlope,60,
                new[]{new Vector3(1270,28,-1540),TruckTaxiVenueLayout.MountainSlopeCrossing,new Vector3(1110,0,-1540)});
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            TruckTaxiMapIconSetup.EnsureAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("Taxi mega integration metadata ready; existing gameplay content preserved.");
        }
        private static void Zone(Transform root,string id,string label,string region,TruckTaxiExtremeKind kind,TruckTaxiHazardExposure exposure,float radius,Vector3[] path)
        {
            foreach(var existing in root.GetComponentsInChildren<TruckTaxiHazardZone>(true)) if(existing.zoneId==id) return;
            var go=new GameObject(label); go.transform.SetParent(root,false); go.transform.position=path[0];
            var zone=go.AddComponent<TruckTaxiHazardZone>(); zone.zoneId=id; zone.displayName=label; zone.regionId=region;
            zone.kind=kind; zone.exposure=exposure; zone.radiusMeters=radius; zone.path=path;
            if(!zone.IsValid(out var reason)) throw new InvalidOperationException(reason);
        }
    }
}
