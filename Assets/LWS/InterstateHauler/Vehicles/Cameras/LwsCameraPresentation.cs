using System;
using NWH.Common.Cameras;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsVehicleCameraMode
    {
        Unknown,
        Cockpit,
        Exterior
    }

    public enum LwsGpsPresentationPolicy
    {
        Auto,
        ForceHudMinimapOn,
        ForceHudMinimapOff,
        ForceCabGpsOn
    }

    public interface ILwsCameraPresentationService : ILwsService
    {
        LwsVehicleCameraMode CurrentMode { get; }
        string CurrentCameraName { get; }
        LwsGpsPresentationPolicy GpsPresentationPolicy { get; }
        bool CabGpsActive { get; }
        bool ShouldShowHudMinimap { get; }
        event Action<LwsVehicleCameraMode> CameraModeChanged;
        event Action<LwsGpsPresentationPolicy> GpsPresentationPolicyChanged;
        void SetCameraMode(LwsVehicleCameraMode mode, string cameraName);
        void SetGpsPresentationPolicy(LwsGpsPresentationPolicy policy);
        void SetCabGpsActive(bool active);
    }

    public sealed class LwsCameraPresentationService : ILwsCameraPresentationService
    {
        private LwsVehicleCameraMode _currentMode = LwsVehicleCameraMode.Unknown;
        private string _currentCameraName = "Unknown";
        private LwsGpsPresentationPolicy _gpsPresentationPolicy = LwsGpsPresentationPolicy.Auto;
        private bool _cabGpsActive;

        public string ServiceId => "lws.camera.presentation";
        public LwsVehicleCameraMode CurrentMode => _currentMode;
        public string CurrentCameraName => _currentCameraName;
        public LwsGpsPresentationPolicy GpsPresentationPolicy => _gpsPresentationPolicy;
        public bool CabGpsActive => _cabGpsActive;
        public bool ShouldShowHudMinimap
        {
            get
            {
                switch (_gpsPresentationPolicy)
                {
                    case LwsGpsPresentationPolicy.ForceHudMinimapOn:
                        return true;
                    case LwsGpsPresentationPolicy.ForceHudMinimapOff:
                        return false;
                    case LwsGpsPresentationPolicy.ForceCabGpsOn:
                    case LwsGpsPresentationPolicy.Auto:
                    default:
                        return _currentMode != LwsVehicleCameraMode.Cockpit;
                }
            }
        }

        public event Action<LwsVehicleCameraMode> CameraModeChanged;
        public event Action<LwsGpsPresentationPolicy> GpsPresentationPolicyChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _currentMode = LwsVehicleCameraMode.Unknown;
            _currentCameraName = "Unknown";
            _gpsPresentationPolicy = LwsGpsPresentationPolicy.Auto;
            _cabGpsActive = false;
            return LwsServiceResult.Success("LWS camera presentation service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            CameraModeChanged = null;
            GpsPresentationPolicyChanged = null;
            return LwsServiceResult.Success("LWS camera presentation service shut down.");
        }

        public void SetCameraMode(LwsVehicleCameraMode mode, string cameraName)
        {
            cameraName = string.IsNullOrWhiteSpace(cameraName) ? "Unknown" : cameraName;
            if (_currentMode == mode && string.Equals(_currentCameraName, cameraName, StringComparison.Ordinal))
            {
                return;
            }

            _currentMode = mode;
            _currentCameraName = cameraName;
            CameraModeChanged?.Invoke(_currentMode);
        }

        public void SetGpsPresentationPolicy(LwsGpsPresentationPolicy policy)
        {
            if (_gpsPresentationPolicy == policy)
            {
                return;
            }

            _gpsPresentationPolicy = policy;
            GpsPresentationPolicyChanged?.Invoke(_gpsPresentationPolicy);
        }

        public void SetCabGpsActive(bool active)
        {
            _cabGpsActive = active;
        }
    }

    [DefaultExecutionOrder(190)]
    [DisallowMultipleComponent]
    public sealed class LwsNwhCameraPresentationMonitor : MonoBehaviour
    {
        [SerializeField] private CameraChanger cameraChanger;
        [SerializeField] private bool publishOnStart = true;

        private ILwsCameraPresentationService _presentationService;
        private int _lastCameraIndex = int.MinValue;
        private GameObject _lastCameraObject;
        private LwsVehicleCameraMode _lastMode = LwsVehicleCameraMode.Unknown;

        public LwsVehicleCameraMode CurrentMode { get; private set; } = LwsVehicleCameraMode.Unknown;
        public string CurrentCameraName => _lastCameraObject != null ? _lastCameraObject.name : "Unknown";
        public bool BoundToCameraChanger => cameraChanger != null;

        private void Reset()
        {
            ResolveCameraChanger();
        }

        private void Awake()
        {
            ResolveCameraChanger();
            ResolveService();
        }

        private void Start()
        {
            ResolveService();
            if (publishOnStart)
            {
                PublishCurrentCamera(true);
            }
        }

        private void LateUpdate()
        {
            PublishCurrentCamera(false);
        }

        public void ResolveCameraChanger()
        {
            if (cameraChanger == null)
            {
                cameraChanger = GetComponentInChildren<CameraChanger>(true);
            }
        }

        public void PublishCurrentCamera(bool force)
        {
            if (cameraChanger == null)
            {
                ResolveCameraChanger();
            }

            ResolveService();
            GameObject activeCamera = ResolveActiveCamera();
            LwsVehicleCameraMode mode = ResolveMode(activeCamera);
            int index = cameraChanger != null ? cameraChanger.currentCameraIndex : -1;

            if (!force && index == _lastCameraIndex && activeCamera == _lastCameraObject && mode == _lastMode)
            {
                return;
            }

            _lastCameraIndex = index;
            _lastCameraObject = activeCamera;
            _lastMode = mode;
            CurrentMode = mode;
            _presentationService?.SetCameraMode(mode, activeCamera != null ? activeCamera.name : "Unknown");
        }

        private GameObject ResolveActiveCamera()
        {
            if (cameraChanger == null || cameraChanger.cameras == null || cameraChanger.cameras.Count == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(cameraChanger.currentCameraIndex, 0, cameraChanger.cameras.Count - 1);
            GameObject candidate = cameraChanger.cameras[index];
            if (candidate != null)
            {
                return candidate;
            }

            for (int i = 0; i < cameraChanger.cameras.Count; i++)
            {
                candidate = cameraChanger.cameras[i];
                if (candidate != null && candidate.activeInHierarchy)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static LwsVehicleCameraMode ResolveMode(GameObject cameraObject)
        {
            if (cameraObject == null)
            {
                return LwsVehicleCameraMode.Unknown;
            }

            CameraInsideVehicle inside = cameraObject.GetComponent<CameraInsideVehicle>();
            return inside != null && inside.isInsideVehicle
                ? LwsVehicleCameraMode.Cockpit
                : LwsVehicleCameraMode.Exterior;
        }

        private void ResolveService()
        {
            if (_presentationService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _presentationService);
        }
    }
}
