using LWS.InterstateHauler;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(-320)]
    [DisallowMultipleComponent]
    public sealed class DeadAirGameManager : MonoBehaviour
    {
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool ensureLwsBootstrap = true;
        [SerializeField] private bool forceBasicAutomaticOnStart = true;
        [SerializeField] private DeadAirControlMode defaultControlMode = DeadAirControlMode.BasicAutomatic;
        [SerializeField] private DeadAirStartRigController startRigController;
        [SerializeField] private DeadAirCockpitCameraLock cockpitCameraLock;
        [SerializeField] private DeadAirOffRoadFailureController offRoadFailureController;
        [SerializeField] private DeadAirStoryDirector storyDirector;
        [SerializeField] private DeadAirVehicleAdapter vehicleAdapter;
        [SerializeField] private DeadAirAudioDirector audioDirector;
        [SerializeField] private DeadAirGPSDirector gpsDirector;
        [SerializeField] private DeadAirEndingDirector endingDirector;
        [SerializeField] private DeadAirAnomalyDirector anomalyDirector;
        [SerializeField] private DeadAirDashboardMisinformationDirector dashboardDirector;
        [SerializeField] private DeadAirTrafficHorrorDirector trafficHorrorDirector;
        [SerializeField] private DeadAirHud hud;

        public static DeadAirGameManager Instance { get; private set; }
        public DeadAirGameState State { get; private set; } = DeadAirGameState.Boot;
        public DeadAirControlMode DefaultControlMode => defaultControlMode;
        public DeadAirStartRigController StartRigController => startRigController;
        public DeadAirCockpitCameraLock CockpitCameraLock => cockpitCameraLock;
        public DeadAirOffRoadFailureController OffRoadFailureController => offRoadFailureController;
        public DeadAirStoryDirector StoryDirector => storyDirector;
        public DeadAirVehicleAdapter VehicleAdapter => vehicleAdapter;
        public DeadAirAudioDirector AudioDirector => audioDirector;
        public DeadAirGPSDirector GpsDirector => gpsDirector;
        public DeadAirEndingDirector EndingDirector => endingDirector;
        public DeadAirAnomalyDirector AnomalyDirector => anomalyDirector;
        public DeadAirDashboardMisinformationDirector DashboardDirector => dashboardDirector;
        public DeadAirTrafficHorrorDirector TrafficHorrorDirector => trafficHorrorDirector;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate Dead Air game manager rejected.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveSceneReferences();
            if (ensureLwsBootstrap)
            {
                EnsureLwsBootstrap();
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                BeginRun();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void BeginRun()
        {
            ResolveSceneReferences();
            DeadAirVehicleAdapter rigVehicle = startRigController != null ? startRigController.InitializeRig() : null;
            if (rigVehicle != null)
            {
                vehicleAdapter = rigVehicle;
            }

            storyDirector?.ResetRun();
            endingDirector?.ResetEnding();
            anomalyDirector?.ClearAllAnomalies();
            dashboardDirector?.Clear();
            trafficHorrorDirector?.Cleanup();
            gpsDirector?.ResetGps();
            audioDirector?.StopAll();
            offRoadFailureController?.ResetBoundaryState();
            if (forceBasicAutomaticOnStart && defaultControlMode == DeadAirControlMode.BasicAutomatic)
            {
                vehicleAdapter?.EnableBasicAutomatic();
            }

            cockpitCameraLock?.ApplyCockpitLock(vehicleAdapter);
            State = DeadAirGameState.Playing;
        }

        public void RequestEnding(DeadAirEndingId endingId)
        {
            endingDirector?.ResetEnding();
            vehicleAdapter?.SetDeadAirDrivingInputLocked(false);
            if (State == DeadAirGameState.Ending)
            {
                State = DeadAirGameState.Playing;
            }
        }

        public void RequestVoidFailure()
        {
            endingDirector?.ResetEnding();
            vehicleAdapter?.SetDeadAirDrivingInputLocked(false);
            if (State == DeadAirGameState.Ending)
            {
                State = DeadAirGameState.Playing;
            }
        }

        public void RestartRun()
        {
            State = DeadAirGameState.Restarting;
            BeginRun();
        }

        public void SetPaused(bool paused)
        {
            State = paused ? DeadAirGameState.Paused : DeadAirGameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
        }

        private void ResolveSceneReferences()
        {
            if (storyDirector == null) storyDirector = FindFirstObjectByType<DeadAirStoryDirector>();
            if (startRigController == null) startRigController = FindFirstObjectByType<DeadAirStartRigController>();
            if (cockpitCameraLock == null) cockpitCameraLock = FindFirstObjectByType<DeadAirCockpitCameraLock>();
            if (offRoadFailureController == null) offRoadFailureController = FindFirstObjectByType<DeadAirOffRoadFailureController>();
            if (vehicleAdapter == null) vehicleAdapter = FindFirstObjectByType<DeadAirVehicleAdapter>();
            if (audioDirector == null) audioDirector = FindFirstObjectByType<DeadAirAudioDirector>();
            if (gpsDirector == null) gpsDirector = FindFirstObjectByType<DeadAirGPSDirector>();
            if (endingDirector == null) endingDirector = FindFirstObjectByType<DeadAirEndingDirector>();
            if (anomalyDirector == null) anomalyDirector = FindFirstObjectByType<DeadAirAnomalyDirector>();
            if (dashboardDirector == null) dashboardDirector = FindFirstObjectByType<DeadAirDashboardMisinformationDirector>();
            if (trafficHorrorDirector == null) trafficHorrorDirector = FindFirstObjectByType<DeadAirTrafficHorrorDirector>();
            if (hud == null) hud = FindFirstObjectByType<DeadAirHud>();
        }

        private static void EnsureLwsBootstrap()
        {
            if (LwsApplicationBootstrap.Instance != null)
            {
                return;
            }

            GameObject bootstrap = new GameObject("LWS Application Bootstrap");
            bootstrap.AddComponent<LwsApplicationBootstrap>();
        }
    }
}
