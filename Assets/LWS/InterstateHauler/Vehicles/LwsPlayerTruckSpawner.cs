using NWH.VehiclePhysics2;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsPlayerTruckSpawner : MonoBehaviour
    {
        [SerializeField] private LwsTruckDefinition truckDefinition;
        [SerializeField] private Lws18SpeedTransmissionDefinition transmissionDefinition;
        [SerializeField] private GameObject fallbackPlayerTruckPrefab;
        [SerializeField] private GameObject trailerPrefab;
        [SerializeField] private string spawnedVehicleId = "player.truck.starter";
        [SerializeField] private string spawnedTrailerId = "validation.trailer.dryvan";
        [SerializeField] private Transform truckSpawnPoint;
        [SerializeField] private Transform trailerSpawnPoint;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool addNwhInputProviderIfMissing = true;
        [SerializeField] private bool addDebugPanel = true;
        [SerializeField] private bool buildValidationRoadside = true;

        public LwsPlayerTruck SpawnedTruck { get; private set; }
        public GameObject SpawnedTrailer { get; private set; }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnValidationRig();
            }
        }

        public LwsPlayerTruck SpawnValidationRig()
        {
            if (SpawnedTruck != null)
            {
                return SpawnedTruck;
            }

            GameObject truckPrefab = ResolveTruckPrefab();
            if (truckPrefab == null)
            {
                Debug.LogError("Truck validation spawner has no player truck prefab assigned.", this);
                return null;
            }

            Vector3 truckPosition = truckSpawnPoint != null ? truckSpawnPoint.position : transform.position;
            Quaternion truckRotation = truckSpawnPoint != null ? truckSpawnPoint.rotation : transform.rotation;
            GameObject truckInstance = Instantiate(truckPrefab, truckPosition, truckRotation);
            truckInstance.name = "IH_PlayerTruck_NWH_Runtime";
            ConfigureTruckInstance(truckInstance);

            GameObject resolvedTrailerPrefab = trailerPrefab != null
                ? trailerPrefab
                : truckDefinition != null ? truckDefinition.ValidationTrailerPrefab : null;
            if (resolvedTrailerPrefab != null)
            {
                Vector3 trailerPosition = trailerSpawnPoint != null ? trailerSpawnPoint.position : truckPosition - truckRotation * Vector3.forward * 8f;
                Quaternion trailerRotation = trailerSpawnPoint != null ? trailerSpawnPoint.rotation : truckRotation;
                SpawnedTrailer = Instantiate(resolvedTrailerPrefab, trailerPosition, trailerRotation);
                SpawnedTrailer.name = "IH_TestTrailer_DryVan_Runtime";
                ConfigureTrailerInstance(SpawnedTrailer);
            }

            EnsureNwhInputProvider();
            EnsureValidationRoadside();
            return SpawnedTruck;
        }

        private GameObject ResolveTruckPrefab()
        {
            if (truckDefinition != null && truckDefinition.PlayerPrefab != null)
            {
                return truckDefinition.PlayerPrefab;
            }

            return fallbackPlayerTruckPrefab;
        }

        private void ConfigureTruckInstance(GameObject truckInstance)
        {
            LwsVehicleIdentity identity = truckInstance.GetComponent<LwsVehicleIdentity>();
            if (identity == null)
            {
                identity = truckInstance.AddComponent<LwsVehicleIdentity>();
            }

            identity.Configure(
                spawnedVehicleId,
                truckDefinition != null ? truckDefinition.StableId : "ih.truck.nwh.euro.semi",
                LwsVehicleRole.PlayerTractor,
                truckDefinition != null ? truckDefinition.DisplayName : "NWH Euro Semi Validation Truck",
                true);

            LwsNwhVehicleAdapter adapter = truckInstance.GetComponent<LwsNwhVehicleAdapter>();
            if (adapter == null)
            {
                adapter = truckInstance.AddComponent<LwsNwhVehicleAdapter>();
            }

            LwsNwh18SpeedTransmissionAdapter nwhTransmission = truckInstance.GetComponent<LwsNwh18SpeedTransmissionAdapter>();
            if (nwhTransmission == null)
            {
                nwhTransmission = truckInstance.AddComponent<LwsNwh18SpeedTransmissionAdapter>();
            }

            Lws18SpeedTransmissionController transmission = truckInstance.GetComponent<Lws18SpeedTransmissionController>();
            if (transmission == null)
            {
                transmission = truckInstance.AddComponent<Lws18SpeedTransmissionController>();
            }

            transmission.SetDefinition(transmissionDefinition);
            transmission.SetMode(Lws18SpeedTransmissionController.DevelopmentDefaultMode);

            LwsKeyboardGamepadTruckInputSource keyboardInput = truckInstance.GetComponent<LwsKeyboardGamepadTruckInputSource>();
            if (keyboardInput == null)
            {
                keyboardInput = truckInstance.AddComponent<LwsKeyboardGamepadTruckInputSource>();
            }

            keyboardInput.ConfigureTransmissionController(transmission);
            transmission.SetFallbackInputSource(keyboardInput);

            LwsNwhVehicleInputProvider nwhInputProvider = truckInstance.GetComponent<LwsNwhVehicleInputProvider>();
            if (nwhInputProvider == null)
            {
                nwhInputProvider = truckInstance.AddComponent<LwsNwhVehicleInputProvider>();
            }

            ILwsVehicleInputService inputService = ResolveVehicleInputService();
            if (inputService != null && !inputService.HasActiveSource)
            {
                inputService.SetInputSource(keyboardInput, LwsVehicleInputOwner.KeyboardMouse, true);
            }

            ILwsVehicleInputSource drivingInputSource = inputService != null
                ? (ILwsVehicleInputSource)inputService
                : keyboardInput;
            nwhInputProvider.SetVehicleController(truckInstance.GetComponent<VehicleController>());
            nwhInputProvider.SetInputSource(drivingInputSource);
            nwhInputProvider.SetTransmissionController(transmission);
            nwhInputProvider.SetValidationGearMappingEnabled(false);

            LwsNwhTrailerCouplingAdapter coupling = truckInstance.GetComponent<LwsNwhTrailerCouplingAdapter>();
            if (coupling == null)
            {
                coupling = truckInstance.AddComponent<LwsNwhTrailerCouplingAdapter>();
            }

            LwsNwhTruckControlAdapter truckControlAdapter = truckInstance.GetComponent<LwsNwhTruckControlAdapter>();
            if (truckControlAdapter == null)
            {
                truckControlAdapter = truckInstance.AddComponent<LwsNwhTruckControlAdapter>();
            }

            LwsPlayerGestureController gestureController = truckInstance.GetComponent<LwsPlayerGestureController>();
            if (gestureController == null)
            {
                gestureController = truckInstance.AddComponent<LwsPlayerGestureController>();
            }

            LwsTruckControlController truckControls = truckInstance.GetComponent<LwsTruckControlController>();
            if (truckControls == null)
            {
                truckControls = truckInstance.AddComponent<LwsTruckControlController>();
            }

            nwhInputProvider.SetTruckControlController(truckControls);

            LwsNwhCameraPresentationMonitor cameraMonitor = truckInstance.GetComponent<LwsNwhCameraPresentationMonitor>();
            if (cameraMonitor == null)
            {
                cameraMonitor = truckInstance.AddComponent<LwsNwhCameraPresentationMonitor>();
            }

            LwsTruckDashboardController dashboard = truckInstance.GetComponent<LwsTruckDashboardController>();
            if (dashboard == null)
            {
                dashboard = truckInstance.AddComponent<LwsTruckDashboardController>();
            }

            if (truckDefinition != null)
            {
                dashboard.Configure(truckDefinition.DashboardDefinition);
            }

            LwsTruckMirrorController mirrors = truckInstance.GetComponent<LwsTruckMirrorController>();
            if (mirrors == null)
            {
                mirrors = truckInstance.AddComponent<LwsTruckMirrorController>();
            }

            if (truckDefinition != null)
            {
                mirrors.Configure(truckDefinition.DashboardDefinition);
            }

            LwsCabAccessoryAnchorRegistry anchors = truckInstance.GetComponent<LwsCabAccessoryAnchorRegistry>();
            if (anchors == null)
            {
                anchors = truckInstance.AddComponent<LwsCabAccessoryAnchorRegistry>();
            }

            LwsTruckCabInteriorRecovery cabRecovery = truckInstance.GetComponent<LwsTruckCabInteriorRecovery>();
            if (cabRecovery == null)
            {
                truckInstance.AddComponent<LwsTruckCabInteriorRecovery>();
            }

            LwsCabGpsController cabGps = truckInstance.GetComponent<LwsCabGpsController>();
            if (cabGps == null)
            {
                truckInstance.AddComponent<LwsCabGpsController>();
            }

            LwsCompassNavigatorProAdapter compassAdapter = truckInstance.GetComponent<LwsCompassNavigatorProAdapter>();
            if (compassAdapter == null)
            {
                truckInstance.AddComponent<LwsCompassNavigatorProAdapter>();
            }

            SpawnedTruck = truckInstance.GetComponent<LwsPlayerTruck>();
            if (SpawnedTruck == null)
            {
                SpawnedTruck = truckInstance.AddComponent<LwsPlayerTruck>();
            }

            SpawnedTruck.Configure(truckDefinition, spawnedVehicleId);

            if (addDebugPanel && truckInstance.GetComponent<LwsPlayerTruckDebugPanel>() == null)
            {
                truckInstance.AddComponent<LwsPlayerTruckDebugPanel>();
            }

            if (addDebugPanel && truckInstance.GetComponent<Lws18SpeedTransmissionDebugPanel>() == null)
            {
                truckInstance.AddComponent<Lws18SpeedTransmissionDebugPanel>();
            }

            if (addDebugPanel && truckInstance.GetComponent<LwsTruckControlDebugPanel>() == null)
            {
                truckInstance.AddComponent<LwsTruckControlDebugPanel>();
            }

            if (addDebugPanel && truckInstance.GetComponent<LwsTruckDashboardDebugPanel>() == null)
            {
                truckInstance.AddComponent<LwsTruckDashboardDebugPanel>();
            }

            LwsFloatingOriginRigidbodyParticipant originParticipant =
                truckInstance.GetComponent<LwsFloatingOriginRigidbodyParticipant>() ??
                truckInstance.AddComponent<LwsFloatingOriginRigidbodyParticipant>();
            originParticipant.Configure(spawnedVehicleId, LwsFloatingOriginParticipantKind.PlayerTractor, false);
        }

        private void ConfigureTrailerInstance(GameObject trailerInstance)
        {
            LwsVehicleIdentity identity = trailerInstance.GetComponent<LwsVehicleIdentity>();
            if (identity == null)
            {
                identity = trailerInstance.AddComponent<LwsVehicleIdentity>();
            }

            identity.Configure(
                spawnedTrailerId,
                "ih.trailer.validation.dryvan",
                LwsVehicleRole.Trailer,
                "NWH Dry Van Validation Trailer",
                false);

            LwsFloatingOriginRigidbodyParticipant originParticipant =
                trailerInstance.GetComponent<LwsFloatingOriginRigidbodyParticipant>() ??
                trailerInstance.AddComponent<LwsFloatingOriginRigidbodyParticipant>();
            originParticipant.Configure(spawnedTrailerId, LwsFloatingOriginParticipantKind.PlayerTrailer, false);
        }

        private void EnsureNwhInputProvider()
        {
            if (!addNwhInputProviderIfMissing)
            {
                return;
            }

            if (SpawnedTruck != null && SpawnedTruck.GetComponent<LwsNwhVehicleInputProvider>() != null)
            {
                return;
            }

            Debug.LogWarning("Player truck spawned without an LWS NWH vehicle input provider; stock NWH input was not added to avoid double-feeding the drivetrain.", this);
        }

        private static ILwsVehicleInputService ResolveVehicleInputService()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return null;
            }

            return LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsVehicleInputService service)
                ? service
                : null;
        }

        private void EnsureValidationRoadside()
        {
            if (!buildValidationRoadside)
            {
                return;
            }

            GameObject root = GameObject.Find("IH Truck Validation Roadside Runtime");
            if (root == null)
            {
                root = new GameObject("IH Truck Validation Roadside Runtime");
                root.transform.SetParent(null, false);
                root.transform.position = Vector3.zero;
            }

            LwsInterstateRoadsideBuilder.BuildStraightPairedInterstate(
                root.transform,
                "IH_TRUCK_VALIDATION",
                -120f,
                360f);
        }
    }
}
