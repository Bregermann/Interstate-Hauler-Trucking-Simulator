using LWS.InterstateHauler;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadAir
{
    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    public sealed class DeadAirSceneBootstrapper : MonoBehaviour
    {
        private const string DefaultPlayerTruckPrefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab";
        private const string DefaultDeliveryTrailerPrefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab";
        private const string DefaultTransmissionDefinitionPath = "Assets/LWS/InterstateHauler/Vehicles/Transmission/Data/IH_18SpeedTransmission_G29_EatonDevelopment.asset";

        [SerializeField] private bool createRuntimeSystems = true;
        [SerializeField] private bool createDefaultUi = true;
        [SerializeField] private bool createUnplacedBeatLayout = true;
        [SerializeField] private bool spawnPlayerTruck = true;
        [SerializeField] private GameObject playerTruckPrefab;
        [SerializeField] private GameObject deliveryTrailerPrefab;
        [SerializeField] private Lws18SpeedTransmissionDefinition transmissionDefinition;
        [SerializeField] private Vector3 truckSpawnPosition;
        [SerializeField] private Vector3 truckSpawnEulerAngles;

        public void ConfigurePrefabs(GameObject truckPrefab, GameObject trailerPrefab)
        {
            if (truckPrefab != null)
            {
                playerTruckPrefab = truckPrefab;
            }

            if (trailerPrefab != null)
            {
                deliveryTrailerPrefab = trailerPrefab;
            }
        }

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
            DeadAirStartMarker ensuredStartMarker = EnsureStartMarker(start);
            DeadAirStartMarker startMarker = DeadAirBeatLayoutUtility.FindPreferredRuntimeStartMarker() ?? ensuredStartMarker;
            ResolveEditorDefaultAssets();

            if (!createRuntimeSystems)
            {
                return;
            }

            EnsureComponent<DeadAirGameManager>(systems, "Dead Air Game Manager");
            EnsureComponent<DeadAirStoryDirector>(systems, "Dead Air Story Director");
            EnsureComponent<DeadAirAudioDirector>(systems, "Dead Air Audio Director");
            EnsureComponent<DeadAirGPSDirector>(systems, "Dead Air GPS Director");
            DeadAirGPSController gpsController = EnsureComponent<DeadAirGPSController>(systems, "Dead Air GPS Controller");
            EnsureComponent<DeadAirEndingDirector>(systems, "Dead Air Ending Director");
            EnsureComponent<DeadAirAnomalyDirector>(systems, "Dead Air Anomaly Director");
            EnsureComponent<DeadAirDashboardMisinformationDirector>(systems, "Dead Air Dashboard Director");
            EnsureComponent<DeadAirTrafficHorrorDirector>(systems, "Dead Air Traffic Horror Director");
            DeadAirStartRigController startRig = EnsureComponent<DeadAirStartRigController>(start, "Dead Air Start Rig Controller");
            startRig.Configure(playerTruckPrefab, deliveryTrailerPrefab, startMarker, transmissionDefinition);
            EnsureComponent<DeadAirCockpitCameraLock>(systems, "Dead Air Cockpit Camera Lock");

            if (createDefaultUi)
            {
                EnsureComponent<DeadAirHud>(ui, "Dead Air HUD");
            }

            if (spawnPlayerTruck)
            {
                EnsurePlayerTruck(startRig);
                gpsController.AttachToCurrentTruck();
            }

            if (createUnplacedBeatLayout)
            {
                DeadAirBeatLayoutUtility.EnsureUnplacedBeatLayout();
                DeadAirBeatLayoutUtility.EnsureConstructionKit();
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

        private static DeadAirStartMarker EnsureStartMarker(Transform start)
        {
            Transform marker = start.Find("DeadAirStartMarker");
            if (marker == null)
            {
                marker = new GameObject("DeadAirStartMarker").transform;
                marker.SetParent(start, false);
            }

            DeadAirStartMarker startMarker = marker.GetComponent<DeadAirStartMarker>();
            if (startMarker == null)
            {
                startMarker = marker.gameObject.AddComponent<DeadAirStartMarker>();
            }

            Transform truck = EnsureChild(marker, "TRUCK_START_REFERENCE");
            Transform trailer = EnsureChild(marker, "TRAILER_START_REFERENCE");
            trailer.localPosition = trailer.localPosition == Vector3.zero ? new Vector3(0f, 0f, -13.5f) : trailer.localPosition;
            EnsureChild(marker, "FORWARD_DIRECTION").localPosition = new Vector3(0f, 0f, 8f);
            startMarker.ConfigureRigReferences(truck, trailer);
            return startMarker;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private void EnsurePlayerTruck(DeadAirStartRigController startRig)
        {
            if (startRig != null)
            {
                startRig.InitializeRig();
                return;
            }

            DeadAirVehicleAdapter existing = FindFirstObjectByType<DeadAirVehicleAdapter>();
            if (existing != null)
            {
                existing.ConfigureDeadAirTruckRig(transmissionDefinition, out _);
                return;
            }

            if (playerTruckPrefab == null)
            {
                return;
            }

            Transform start = EnsureRoot("START");
            GameObject truck = Instantiate(playerTruckPrefab, truckSpawnPosition, Quaternion.Euler(truckSpawnEulerAngles), start);
            truck.name = "Dead Air Player Truck";
            DeadAirVehicleAdapter adapter = truck.GetComponent<DeadAirVehicleAdapter>();
            if (adapter == null)
            {
                adapter = truck.AddComponent<DeadAirVehicleAdapter>();
            }

            adapter.ConfigureDeadAirTruckRig(transmissionDefinition, out _);
        }

        private void ResolveEditorDefaultAssets()
        {
#if UNITY_EDITOR
            if (playerTruckPrefab == null)
            {
                playerTruckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPlayerTruckPrefabPath);
            }

            if (deliveryTrailerPrefab == null)
            {
                deliveryTrailerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultDeliveryTrailerPrefabPath);
            }

            if (transmissionDefinition == null)
            {
                transmissionDefinition = AssetDatabase.LoadAssetAtPath<Lws18SpeedTransmissionDefinition>(DefaultTransmissionDefinitionPath);
            }
#endif
        }
    }
}