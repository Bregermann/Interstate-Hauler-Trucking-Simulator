using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    public sealed class DeadAirSceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool createRuntimeSystems = true;
        [SerializeField] private bool createDefaultUi = true;
        [SerializeField] private bool createUnplacedBeatLayout = true;
        [SerializeField] private bool spawnPlayerTruck = true;
        [SerializeField] private GameObject playerTruckPrefab;
        [SerializeField] private Vector3 truckSpawnPosition;
        [SerializeField] private Vector3 truckSpawnEulerAngles;

        private void Awake()
        {
            EnsureRoot("DEAD_AIR");
            Transform start = EnsureRoot("START");
            Transform systems = EnsureRoot("SYSTEMS");
            EnsureRoot("STORY_TRIGGERS");
            EnsureRoot("CHOICE_TRIGGERS");
            EnsureRoot("ENVIRONMENT");
            Transform ui = EnsureRoot("UI");
            EnsureRoot("DEBUG");

            if (!createRuntimeSystems)
            {
                return;
            }

            EnsureComponent<DeadAirGameManager>(systems, "Dead Air Game Manager");
            EnsureComponent<DeadAirStoryDirector>(systems, "Dead Air Story Director");
            EnsureComponent<DeadAirAudioDirector>(systems, "Dead Air Audio Director");
            EnsureComponent<DeadAirGPSDirector>(systems, "Dead Air GPS Director");
            EnsureComponent<DeadAirEndingDirector>(systems, "Dead Air Ending Director");
            EnsureComponent<DeadAirAnomalyDirector>(systems, "Dead Air Anomaly Director");

            if (createDefaultUi)
            {
                EnsureComponent<DeadAirHud>(ui, "Dead Air HUD");
            }

            if (spawnPlayerTruck)
            {
                EnsurePlayerTruck(start);
            }

            if (createUnplacedBeatLayout)
            {
                DeadAirBeatLayoutUtility.EnsureUnplacedBeatLayout();
            }
        }

        private static Transform EnsureRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                return existing.transform;
            }

            return new GameObject(name).transform;
        }

        private static T EnsureComponent<T>(Transform parent, string name) where T : Component
        {
            T existing = FindFirstObjectByType<T>();
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private void EnsurePlayerTruck(Transform start)
        {
            DeadAirVehicleAdapter existing = FindFirstObjectByType<DeadAirVehicleAdapter>();
            if (existing != null)
            {
                existing.EnableBasicAutomatic();
                return;
            }

            if (playerTruckPrefab == null)
            {
                return;
            }

            GameObject truck = Instantiate(playerTruckPrefab, truckSpawnPosition, Quaternion.Euler(truckSpawnEulerAngles), start);
            truck.name = "Dead Air Player Truck";
            DeadAirVehicleAdapter adapter = truck.GetComponent<DeadAirVehicleAdapter>();
            if (adapter == null)
            {
                adapter = truck.AddComponent<DeadAirVehicleAdapter>();
            }

            if (truck.GetComponent<DeadAirBasicAutomaticInputSource>() == null)
            {
                truck.AddComponent<DeadAirBasicAutomaticInputSource>();
            }

            adapter.EnableBasicAutomatic();
        }
    }
}
