using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiPolishRideAuthoring
    {
        [MenuItem("Truck Taxi/Regional/Apply Ride Polish Content")]
        public static void Apply()
        {
            var host=UnityEngine.Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            if(host==null || host.configuration==null) throw new InvalidOperationException("Open the persistent Truck Taxi scene first.");
            var config=host.configuration;
            if(Mathf.Approximately(config.intercityRideChance,.2f)) config.intercityRideChance=.1f;
            config.localOffersAfterIntercity=3;
            EditorUtility.SetDirty(config);
            foreach(var request in config.requests ?? Array.Empty<PassengerRequestDefinition>())
            {
                if(request==null) continue;
                if(request.requestType==TaxiRequestType.IllicitStop) request.bonusMoneyCents=Math.Max(2000,request.bonusMoneyCents);
                else if(request.requestType==TaxiRequestType.ScenicRoute)
                { request.rewardItem=TruckTaxiNeedsItem.MysteryMushrooms; request.rewardItemCount=2; }
                else continue;
                EditorUtility.SetDirty(request);
            }
            const string path="Assets/LWS/TruckTaxi/ScriptableObjects/Requests/DinerStop.asset";
            var diner=AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>(path);
            if(diner==null)
            {
                diner=ScriptableObject.CreateInstance<PassengerRequestDefinition>();
                diner.requestType=TaxiRequestType.DinerStop; diner.objectiveId="taxi.objective.diner-stop";
                diner.description="Take a quick diner break"; diner.timer=600;
                diner.bonusMoneyCents=500; diner.rewardItem=TruckTaxiNeedsItem.EnergyDrink; diner.rewardItemCount=1;
                diner.secondRewardItem=TruckTaxiNeedsItem.Snack; diner.secondRewardItemCount=1;
                AssetDatabase.CreateAsset(diner,path);
            }
            if(!config.requests.Contains(diner)) config.requests=config.requests.Concat(new[]{diner}).ToArray();
            var lap=AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>("Assets/LWS/TruckTaxi/ScriptableObjects/Requests/TakeALap.asset");
            if(lap!=null && !config.requests.Contains(lap)) config.requests=config.requests.Concat(new[]{lap}).ToArray();
            foreach(var profile in config.passengerDatabase.passengers)
            {
                if(profile==null || profile.passengerId!="food-obsessed-dad" && profile.passengerId!="lazy-food-lover") continue;
                var requests=profile.possibleRequests ?? Array.Empty<PassengerRequestDefinition>();
                if(requests.Contains(diner)) continue;
                profile.possibleRequests=requests.Concat(new[]{diner}).ToArray(); EditorUtility.SetDirty(profile);
            }
            foreach(var location in UnityEngine.Object.FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None))
            {
                if(location.gameObject.scene!=host.gameObject.scene || location.locationType!=TaxiLocationType.Restaurant) continue;
                var point=location.transform.Find("Diner diversion stop");
                if(point==null) { point=new GameObject("Diner diversion stop").transform; point.SetParent(location.transform,false); }
                point.position=location.StopPosition;
                var stop=point.GetComponent<TruckTaxiStopObjectivePoint>() ?? point.gameObject.AddComponent<TruckTaxiStopObjectivePoint>();
                stop.stableId=location.locationId+".diner"; stop.displayName=location.locationName; stop.district=location.district;
                stop.category=TruckTaxiStopCategory.FoodStop; stop.radius=Mathf.Max(18,location.detectionRadius); stop.durationSeconds=12;
                EditorUtility.SetDirty(stop);
            }
            EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Truck Taxi ride polish: bounded-offer defaults, authored diversion rewards and diner stops applied. Save the persistent scene.");
        }
    }
}
