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
        [SerializeField] private DeadAirStoryDirector storyDirector;
        [SerializeField] private DeadAirVehicleAdapter vehicleAdapter;
        [SerializeField] private DeadAirAudioDirector audioDirector;
        [SerializeField] private DeadAirGPSDirector gpsDirector;
        [SerializeField] private DeadAirEndingDirector endingDirector;
        [SerializeField] private DeadAirAnomalyDirector anomalyDirector;
        [SerializeField] private DeadAirHud hud;

        public static DeadAirGameManager Instance { get; private set; }
        public DeadAirGameState State { get; private set; } = DeadAirGameState.Boot;
        public DeadAirControlMode DefaultControlMode => defaultControlMode;
        public DeadAirStoryDirector StoryDirector => storyDirector;
        public DeadAirVehicleAdapter VehicleAdapter => vehicleAdapter;
        public DeadAirAudioDirector AudioDirector => audioDirector;
        public DeadAirGPSDirector GpsDirector => gpsDirector;
        public DeadAirEndingDirector EndingDirector => endingDirector;
        public DeadAirAnomalyDirector AnomalyDirector => anomalyDirector;

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
            storyDirector?.ResetRun();
            endingDirector?.ResetEnding();
            anomalyDirector?.ClearAllAnomalies();
            gpsDirector?.ResetGps();
            audioDirector?.StopAll();
            if (forceBasicAutomaticOnStart && defaultControlMode == DeadAirControlMode.BasicAutomatic)
            {
                vehicleAdapter?.EnableBasicAutomatic();
            }

            State = DeadAirGameState.Playing;
        }

        public void RequestEnding(DeadAirEndingId endingId)
        {
            if (State == DeadAirGameState.Ending)
            {
                return;
            }

            State = DeadAirGameState.Ending;
            endingDirector?.PlayEnding(endingId);
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
            if (vehicleAdapter == null) vehicleAdapter = FindFirstObjectByType<DeadAirVehicleAdapter>();
            if (audioDirector == null) audioDirector = FindFirstObjectByType<DeadAirAudioDirector>();
            if (gpsDirector == null) gpsDirector = FindFirstObjectByType<DeadAirGPSDirector>();
            if (endingDirector == null) endingDirector = FindFirstObjectByType<DeadAirEndingDirector>();
            if (anomalyDirector == null) anomalyDirector = FindFirstObjectByType<DeadAirAnomalyDirector>();
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
