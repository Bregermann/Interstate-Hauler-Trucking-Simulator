using System.Collections;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiTrafficAdapter : MonoBehaviour
    {
        public GameObject[] trafficPrefabs;
        public LwsTrafficLaneDefinition[] cityLanes;
        public int maximumVehicles = 12;
        private readonly LwsUtsTrafficApi api = new LwsUtsTrafficApi();
        private readonly List<Component> paths = new List<Component>();
        private readonly List<GameObject> vehicles = new List<GameObject>();
        private readonly LwsTrafficSpawnPolicy policy = new LwsTrafficSpawnPolicy {
            targetCruiseSpeedScale = 0.7f, maximumTrafficSpeedMetersPerSecond = 12, autoResolveEditorPrefabs = false };
        private float nextMaintenance;
        private int serial;
        public int ActiveCount => vehicles.Count;
        public bool Ready => paths.Count > 0 && api.IsAvailable;
        public void Initialize()
        {
            if (paths.Count > 0) return;
            if (!api.IsAvailable) { Debug.LogError(api.AvailabilitySummary,this); return; }
            foreach (var lane in cityLanes)
            {
                var root = new GameObject(lane.laneId); root.transform.SetParent(transform,false);
                var path = api.CreatePath(root,lane,trafficPrefabs,policy,out string message);
                if (path == null) Debug.LogError(message,this);
                paths.Add(path);
            }
            for (int i=0;i<maximumVehicles;i++) SpawnTraffic();
        }
        public bool SpawnTraffic()
        {
            vehicles.RemoveAll(v=>v==null);
            if (vehicles.Count >= maximumVehicles || !Ready || trafficPrefabs.Length == 0) return false;
            int index = serial % paths.Count;
            var lane = cityLanes[index];
            int point = 2 + (serial * 23) % Mathf.Max(1,lane.centerline.Length - 4);
            Vector3 position = lane.centerline[point];
            foreach (var car in vehicles) if (Vector3.Distance(car.transform.position,position)<14) { serial++; return false; }
            var player = TruckTaxiBootstrap.Instance?.Player;
            if (player != null && Vector3.Distance(player.transform.position,position)<20) { serial++; return false; }
            var vehicle = api.SpawnVehicle(trafficPrefabs[serial % trafficPrefabs.Length],paths[index],lane,point,transform,policy,out string message);
            serial++;
            if (vehicle == null) { Debug.LogWarning(message,this); return false; }
            // Runtime-added UTS components need their Start lifecycle before AI calls Move.
            foreach (var component in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Name == "CarMove") component.enabled = true;
                if (component.GetType().Name == "CarAIController")
                {
                    component.enabled = false;
                    StartCoroutine(EnableInitializedAi(component));
                }
            }
            var target = vehicle.AddComponent<TruckTaxiImpactTarget>(); target.kind = TaxiImpactKind.Traffic; target.targetId = "taxi.traffic."+serial;
            vehicles.Add(vehicle); return true;
        }
        private IEnumerator EnableInitializedAi(Behaviour ai)
        {
            yield return null;
            if (ai != null) ai.enabled = true;
        }
        private void Update()
        {
            if (!Ready || Time.time < nextMaintenance) return;
            nextMaintenance = Time.time + 3;
            for (int i=vehicles.Count-1;i>=0;i--)
            {
                var car = vehicles[i];
                if (car == null) { vehicles.RemoveAt(i); continue; }
                if (car.transform.position.y < -5 || Mathf.Abs(car.transform.position.x)>420 || Mathf.Abs(car.transform.position.z)>420)
                { car.SetActive(false); Destroy(car); vehicles.RemoveAt(i); }
            }
            SpawnTraffic();
        }
        public void ResetTraffic()
        {
            foreach (var vehicle in vehicles) if (vehicle!=null) { vehicle.SetActive(false); Destroy(vehicle); }
            vehicles.Clear(); serial = 0;
            for (int i=0;i<maximumVehicles;i++) SpawnTraffic();
        }
    }
}
