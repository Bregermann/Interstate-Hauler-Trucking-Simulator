using System;
using System.Collections;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEngine;

namespace DeadAir
{
    [DefaultExecutionOrder(195)]
    [DisallowMultipleComponent]
    public sealed class DeadAirCockpitCameraLock : MonoBehaviour
    {
        [SerializeField] private DeadAirVehicleAdapter vehicleAdapter;
        [SerializeField] private bool enforceCockpitInLateUpdate = true;
        [SerializeField] private bool disableOnFootMode = true;
        [SerializeField] private bool allowInteriorLook = true;

        private ILwsCameraPresentationService _cameraPresentationService;
        private Component _cameraChanger;
        private GameObject _cockpitCamera;
        private int _cockpitCameraIndex = -1;

        public bool CockpitLocked { get; private set; }
        public bool CameraCycleBlocked => true;
        public bool OnFootModeDisabled => disableOnFootMode;
        public bool InteriorLookAllowed => allowInteriorLook;
        public bool CockpitCameraFound => _cockpitCamera != null;
        public string CockpitCameraName => _cockpitCamera != null ? _cockpitCamera.name : "Unresolved";
        public LwsVehicleCameraMode CurrentMode => _cameraPresentationService != null ? _cameraPresentationService.CurrentMode : LwsVehicleCameraMode.Unknown;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ApplyCockpitLock(vehicleAdapter);
        }

        private void LateUpdate()
        {
            if (!enforceCockpitInLateUpdate || vehicleAdapter == null)
            {
                return;
            }

            if (_cameraPresentationService == null ||
                _cameraPresentationService.CurrentMode != LwsVehicleCameraMode.Cockpit ||
                !CockpitLocked)
            {
                ApplyCockpitLock(vehicleAdapter);
            }
        }

        public bool ApplyCockpitLock(DeadAirVehicleAdapter adapter)
        {
            if (adapter != null)
            {
                vehicleAdapter = adapter;
            }

            ResolveReferences();
            vehicleAdapter?.SetDeadAirCameraCycleSuppressed(true);

            bool appliedNwhCamera = ApplyNwhCockpitCamera();
            PublishCockpitMode(appliedNwhCamera ? CockpitCameraName : "Dead Air Cockpit");
            CockpitLocked = appliedNwhCamera || _cameraPresentationService != null;
            return CockpitLocked;
        }

        public static LwsVehicleCommandFrame FilterDeadAirCommands(LwsVehicleCommandFrame commands)
        {
            return LwsKeyboardGamepadTruckInputSource.SuppressCameraCycle(commands);
        }

        private bool ApplyNwhCockpitCamera()
        {
            ResolveCameraChanger();
            if (_cameraChanger == null)
            {
                return false;
            }

            IList cameras = ResolveCameraList(_cameraChanger);
            if (cameras == null || cameras.Count == 0)
            {
                return false;
            }

            if (_cockpitCameraIndex < 0 || _cockpitCameraIndex >= cameras.Count || _cockpitCamera == null)
            {
                _cockpitCameraIndex = ResolveCockpitCameraIndex(cameras);
                _cockpitCamera = ResolveCameraGameObject(cameras, _cockpitCameraIndex);
            }

            if (_cockpitCameraIndex < 0 || _cockpitCamera == null)
            {
                return false;
            }

            SetCameraChangerIndex(_cameraChanger, _cockpitCameraIndex);
            InvokeCameraChangerRefresh(_cameraChanger);
            return true;
        }

        private void PublishCockpitMode(string cameraName)
        {
            ResolveCameraPresentationService();
            if (_cameraPresentationService == null)
            {
                return;
            }

            _cameraPresentationService.SetCameraMode(LwsVehicleCameraMode.Cockpit, cameraName);
            _cameraPresentationService.SetCabGpsActive(true);
        }

        private void ResolveReferences()
        {
            if (vehicleAdapter == null)
            {
                vehicleAdapter = FindFirstObjectByType<DeadAirVehicleAdapter>();
            }

            ResolveCameraPresentationService();
            ResolveCameraChanger();
        }

        private void ResolveCameraPresentationService()
        {
            if (_cameraPresentationService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _cameraPresentationService);
        }

        private void ResolveCameraChanger()
        {
            if (_cameraChanger != null || vehicleAdapter == null)
            {
                return;
            }

            foreach (Component component in vehicleAdapter.GetComponentsInChildren<Component>(true))
            {
                if (component != null && string.Equals(component.GetType().Name, "CameraChanger", StringComparison.Ordinal))
                {
                    _cameraChanger = component;
                    return;
                }
            }
        }

        private static IList ResolveCameraList(Component cameraChanger)
        {
            if (cameraChanger == null)
            {
                return null;
            }

            Type type = cameraChanger.GetType();
            FieldInfo field = type.GetField("cameras", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(cameraChanger) as IList : null;
        }

        private static int ResolveCockpitCameraIndex(IList cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                GameObject camera = ResolveCameraGameObject(cameras, i);
                if (camera != null && HasCameraInsideVehicleFlag(camera))
                {
                    return i;
                }
            }

            for (int i = 0; i < cameras.Count; i++)
            {
                GameObject camera = ResolveCameraGameObject(cameras, i);
                if (camera != null && NameSuggestsCockpit(camera.name))
                {
                    return i;
                }
            }

            return -1;
        }

        private static GameObject ResolveCameraGameObject(IList cameras, int index)
        {
            if (cameras == null || index < 0 || index >= cameras.Count)
            {
                return null;
            }

            object entry = cameras[index];
            if (entry is GameObject gameObject)
            {
                return gameObject;
            }

            return entry is Component component ? component.gameObject : null;
        }

        private static bool HasCameraInsideVehicleFlag(GameObject cameraObject)
        {
            foreach (Component component in cameraObject.GetComponents<Component>())
            {
                if (component == null || !string.Equals(component.GetType().Name, "CameraInsideVehicle", StringComparison.Ordinal))
                {
                    continue;
                }

                Type type = component.GetType();
                FieldInfo field = type.GetField("isInsideVehicle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(bool))
                {
                    return (bool)field.GetValue(component);
                }

                PropertyInfo property = type.GetProperty("isInsideVehicle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.PropertyType == typeof(bool) && property.CanRead)
                {
                    return (bool)property.GetValue(component);
                }
            }

            return false;
        }

        private static bool NameSuggestsCockpit(string cameraName)
        {
            if (string.IsNullOrWhiteSpace(cameraName))
            {
                return false;
            }

            return cameraName.IndexOf("cockpit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cameraName.IndexOf("cab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cameraName.IndexOf("driver", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cameraName.IndexOf("interior", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SetCameraChangerIndex(Component cameraChanger, int index)
        {
            Type type = cameraChanger.GetType();
            FieldInfo field = type.GetField("currentCameraIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(int))
            {
                field.SetValue(cameraChanger, index);
                return;
            }

            PropertyInfo property = type.GetProperty("currentCameraIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(int) && property.CanWrite)
            {
                property.SetValue(cameraChanger, index);
            }
        }

        private static void InvokeCameraChangerRefresh(Component cameraChanger)
        {
            Type type = cameraChanger.GetType();
            type.GetMethod("EnableCurrentDisableOthers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(cameraChanger, null);
            type.GetMethod("CheckIfInside", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(cameraChanger, null);
        }
    }
}
