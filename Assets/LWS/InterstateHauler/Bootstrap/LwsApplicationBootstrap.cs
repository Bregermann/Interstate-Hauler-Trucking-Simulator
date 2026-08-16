using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class LwsApplicationBootstrap : MonoBehaviour
    {
        public const string BootstrapScenePath = "Assets/LWS/InterstateHauler/Bootstrap/Bootstrap.unity";

        private static LwsApplicationBootstrap _instance;

        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool initializeOnAwake = true;

        private LwsServiceRegistry _registry;
        private bool _shutdownComplete;

        public static LwsApplicationBootstrap Instance => _instance;
        public LwsServiceRegistry Registry => _registry;
        public bool IsReady => _registry != null && _registry.AreAllReady();
        public bool WasDuplicateRejected { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                WasDuplicateRejected = true;
                Debug.LogWarning("Duplicate LWS application bootstrap rejected.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        public LwsServiceResult Initialize()
        {
            if (_registry != null && _registry.AreAllReady())
            {
                return LwsServiceResult.Success("LWS bootstrap is already initialized.");
            }

            _registry = CreateDefaultRegistry();
            LwsServiceResult result = _registry.InitializeAll();
            if (!result.Succeeded)
            {
                Debug.LogError($"LWS bootstrap failed: {result.Message}");
            }

            return result;
        }

        public LwsServiceResult Shutdown()
        {
            if (_shutdownComplete || _registry == null)
            {
                return LwsServiceResult.Success("LWS bootstrap already shut down.");
            }

            _shutdownComplete = true;
            return _registry.ShutdownAll();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Shutdown();
                _instance = null;
            }
        }

        public static LwsServiceRegistry CreateDefaultRegistry()
        {
            var registry = new LwsServiceRegistry();

            registry.Register<ILwsSaveService>(new LwsSaveService());
            registry.Register<ILwsPlayerSettingsService>(new LwsPlayerSettingsService());
            registry.Register<ILwsVehicleInputService>(new LwsVehicleInputService());
            registry.Register<ILwsWheelCalibrationService>(new LwsWheelCalibrationService());
            registry.Register<ILwsForceFeedbackService>(new LwsForceFeedbackService(), typeof(ILwsWheelCalibrationService));
            registry.Register<ILwsWeatherService>(new LwsWeatherCoordinator());
            registry.Register<ILwsRenderingService>(new LwsRenderingService());
            registry.Register<ILwsWorldStreamingService>(new LwsWorldStreamingService());
            registry.Register<ILwsRoadGraphService>(new LwsRoadGraphService(), typeof(ILwsWorldStreamingService));
            registry.Register<ILwsGpsVoiceGuidanceService>(new LwsGpsVoiceGuidanceService(), typeof(ILwsPlayerSettingsService));
            registry.Register<ILwsNavigationService>(new LwsNavigationService(), typeof(ILwsRoadGraphService), typeof(ILwsGpsVoiceGuidanceService));
            registry.Register<ILwsRoadConditionService>(new LwsRoadConditionCoordinator(), typeof(ILwsWeatherService), typeof(ILwsRoadGraphService), typeof(ILwsNavigationService));
            registry.Register<ILwsTrafficService>(new LwsTrafficService(), typeof(ILwsNavigationService));
            registry.Register<ILwsWorldGenerationCoordinator>(new LwsWorldGenerationCoordinator(), typeof(ILwsNavigationService));
            registry.Register<ILwsPlayerVehicleService>(new LwsPlayerVehicleService(), typeof(ILwsVehicleInputService));
            registry.Register<ILwsVehicleRuntimeService>(new LwsVehicleRuntimeService(), typeof(ILwsVehicleInputService));
            registry.Register<ILwsTruckControlService>(new LwsTruckControlService(), typeof(ILwsPlayerVehicleService), typeof(ILwsVehicleRuntimeService));
            registry.Register<ILwsTruckDashboardService>(new LwsTruckDashboardService(), typeof(ILwsTruckControlService), typeof(ILwsVehicleRuntimeService), typeof(ILwsRenderingService));

            return registry;
        }

        public static void ResetForTests()
        {
            _instance = null;
        }
    }
}
