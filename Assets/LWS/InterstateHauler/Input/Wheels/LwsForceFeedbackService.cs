using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsForceFeedbackService : ILwsService
    {
        LwsForceFeedbackStatus Status { get; }
        bool DirectInputBackendAvailable { get; }
        void ApplySettings(LwsForceFeedbackSettings settings);
        bool TrySelectDevice(string searchTerm);
        void SetForces(float alignment, float damping, float road, float impact);
        void DisableNow(string reason);
    }

    public sealed class LwsForceFeedbackService : ILwsForceFeedbackService
    {
        private LwsForceFeedbackSettings _settings = LwsForceFeedbackSettings.SafeDefault();
        private readonly LwsDirectInputForceFeedbackBackend _directInputBackend = new LwsDirectInputForceFeedbackBackend();
        private LwsForceFeedbackStatus _status;

        public string ServiceId => "lws.input.force-feedback";
        public LwsForceFeedbackStatus Status => _status;
        public bool DirectInputBackendAvailable => LwsDirectInputBackendDiscovery.IsUnityDirectInputAvailable();

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _status = new LwsForceFeedbackStatus
            {
                available = DirectInputBackendAvailable,
                enabled = false,
                deviceConnected = false,
                backendId = DirectInputBackendAvailable ? "unity-directinput.dmanager" : "lws.ffb.unavailable",
                statusMessage = DirectInputBackendAvailable
                    ? "Unity-DirectInput / DIManager backend is available."
                    : "Unity-DirectInput / DIManager backend is unavailable.",
                masterStrength = _settings.masterStrength
            };
            return LwsServiceResult.Success("LWS force feedback service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            DisableNow("LWS force feedback service shutting down.");
            _directInputBackend.Shutdown();
            return LwsServiceResult.Success("LWS force feedback service shut down.");
        }

        public void ApplySettings(LwsForceFeedbackSettings settings)
        {
            _settings = settings;
            _settings.masterStrength = Mathf.Clamp01(_settings.masterStrength);
            _settings.alignmentStrength = Mathf.Clamp01(_settings.alignmentStrength);
            _settings.dampingStrength = Mathf.Clamp01(_settings.dampingStrength);
            _settings.roadStrength = Mathf.Clamp01(_settings.roadStrength);
            _settings.impactStrength = Mathf.Clamp01(_settings.impactStrength);
            if (_settings.preferredDeviceSearchTerm == null)
            {
                _settings.preferredDeviceSearchTerm = string.Empty;
            }

            _status.available = DirectInputBackendAvailable;
            _status.backendId = _status.available ? "unity-directinput.dmanager" : "lws.ffb.unavailable";
            _status.masterStrength = _settings.masterStrength;
            if (!_status.available)
            {
                _status.enabled = false;
                _status.deviceConnected = false;
                _status.statusMessage = "Force feedback unavailable because Unity-DirectInput / DIManager is not loaded.";
                return;
            }

            if (_settings.enabled)
            {
                TrySelectDevice(_settings.preferredDeviceSearchTerm);
            }
            else
            {
                _status.enabled = false;
                _status.statusMessage = "Unity-DirectInput FFB backend available but disabled by settings.";
            }
        }

        public bool TrySelectDevice(string searchTerm)
        {
            if (!DirectInputBackendAvailable)
            {
                _status.available = false;
                _status.deviceConnected = false;
                _status.statusMessage = "Unity-DirectInput / DIManager backend is unavailable.";
                return false;
            }

            if (!_directInputBackend.TryInitialize(out string initMessage))
            {
                _status.available = false;
                _status.deviceConnected = false;
                _status.statusMessage = initMessage;
                return false;
            }

            if (!_directInputBackend.TryAttachPreferredDevice(searchTerm, out LwsDirectInputDeviceInfo device, out string attachMessage))
            {
                _status.available = true;
                _status.enabled = false;
                _status.deviceConnected = false;
                _status.deviceDisplayName = string.Empty;
                _status.deviceGuid = string.Empty;
                _status.supportedEffects = string.Empty;
                _status.statusMessage = attachMessage;
                return false;
            }

            _status.available = true;
            _status.deviceConnected = true;
            _status.deviceDisplayName = device.DisplayName;
            _status.deviceGuid = device.Guid;
            _status.supportedEffects = string.Join(", ", device.FfbEffects);
            _status.enabled = _settings.enabled;
            _status.statusMessage = _status.enabled
                ? $"Unity-DirectInput FFB active on {device.DisplayName}."
                : $"Unity-DirectInput FFB device selected: {device.DisplayName}.";
            return true;
        }

        public void SetForces(float alignment, float damping, float road, float impact)
        {
            if (!_status.available || !_settings.enabled)
            {
                _status.enabled = false;
                _status.alignmentForce = 0f;
                _status.dampingForce = 0f;
                _status.roadForce = 0f;
                _status.impactForce = 0f;
                return;
            }

            if (!_status.deviceConnected && !TrySelectDevice(_settings.preferredDeviceSearchTerm))
            {
                return;
            }

            _status.enabled = true;
            _status.alignmentForce = LwsWheelCalibrationUtility.ClampForce(alignment * _settings.alignmentStrength * _settings.masterStrength);
            _status.dampingForce = LwsWheelCalibrationUtility.ClampForce(damping * _settings.dampingStrength * _settings.masterStrength);
            _status.roadForce = LwsWheelCalibrationUtility.ClampForce(road * _settings.roadStrength * _settings.masterStrength);
            _status.impactForce = LwsWheelCalibrationUtility.ClampForce(impact * _settings.impactStrength * _settings.masterStrength);

            bool applied = _directInputBackend.TryApplyForces(
                _status.alignmentForce + _status.impactForce,
                _status.dampingForce,
                _status.roadForce,
                out string message);
            if (!applied)
            {
                DisableNow(message);
            }
        }

        public void DisableNow(string reason)
        {
            _directInputBackend.StopAllEffects();
            _status.enabled = false;
            _status.deviceConnected = false;
            _status.alignmentForce = 0f;
            _status.dampingForce = 0f;
            _status.roadForce = 0f;
            _status.impactForce = 0f;
            _status.statusMessage = string.IsNullOrWhiteSpace(reason) ? "Force feedback disabled." : reason;
        }
    }

    public static class LwsDirectInputBackendDiscovery
    {
        public static bool IsUnityDirectInputAvailable()
        {
            return FindType("DirectInputManager.DIManager") != null;
        }

        public static IReadOnlyList<LwsWheelDeviceDescriptor> EnumerateDeviceDescriptors()
        {
            var backend = new LwsDirectInputForceFeedbackBackend();
            if (!backend.TryInitialize(out _))
            {
                return Array.Empty<LwsWheelDeviceDescriptor>();
            }

            return backend.EnumerateDevices()
                .Select(device => new LwsWheelDeviceDescriptor
                {
                    displayName = device.DisplayName,
                    product = device.ProductName,
                    deviceClass = "DirectInput",
                    layout = "Unity-DirectInput",
                    path = device.Guid,
                    directInputGuid = device.Guid,
                    directInputForceFeedbackCapable = device.FfbCapable,
                    directInputSupportedEffects = string.Join(", ", device.FfbEffects)
                })
                .ToArray();
        }

        internal static Type FindType(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = GetTypeSafely(assembly, typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        internal static Type GetTypeSafely(Assembly assembly, string typeName)
        {
            try
            {
                return assembly.GetType(typeName, false);
            }
            catch (ReflectionTypeLoadException)
            {
                return null;
            }
        }
    }

    internal readonly struct LwsDirectInputDeviceInfo
    {
        public LwsDirectInputDeviceInfo(
            string guid,
            string productName,
            string instanceName,
            bool ffbCapable,
            IReadOnlyList<string> ffbEffects)
        {
            Guid = guid ?? string.Empty;
            ProductName = productName ?? string.Empty;
            InstanceName = instanceName ?? string.Empty;
            FfbCapable = ffbCapable;
            FfbEffects = ffbEffects ?? Array.Empty<string>();
        }

        public string Guid { get; }
        public string ProductName { get; }
        public string InstanceName { get; }
        public bool FfbCapable { get; }
        public IReadOnlyList<string> FfbEffects { get; }
        public string DisplayName => string.IsNullOrWhiteSpace(ProductName) ? InstanceName : ProductName;
    }

    internal sealed class LwsDirectInputForceFeedbackBackend
    {
        private const int DirectInputMaxMagnitude = 10000;

        private Type _diManagerType;
        private Type _ffbEffectsType;
        private string _activeGuid;
        private bool _initialized;
        private readonly HashSet<string> _enabledEffects = new HashSet<string>(StringComparer.Ordinal);

        public bool TryInitialize(out string message)
        {
            if (_initialized)
            {
                message = "Unity-DirectInput is already initialized.";
                return true;
            }

            _diManagerType = LwsDirectInputBackendDiscovery.FindType("DirectInputManager.DIManager");
            _ffbEffectsType = LwsDirectInputBackendDiscovery.FindType("DirectInputManager.FFBEffects");
            if (_diManagerType == null || _ffbEffectsType == null)
            {
                message = "Unity-DirectInput DIManager types are not loaded.";
                return false;
            }

            try
            {
                bool initialized = InvokeStatic<bool>("Initialize");
                if (!initialized)
                {
                    message = "DIManager.Initialize() returned false.";
                    return false;
                }

                InvokeStatic("EnumerateDevices");
                _initialized = true;
                message = "Unity-DirectInput initialized.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Unity-DirectInput initialization failed: {ex.Message}";
                return false;
            }
        }

        public IReadOnlyList<LwsDirectInputDeviceInfo> EnumerateDevices()
        {
            if (!TryInitialize(out _))
            {
                return Array.Empty<LwsDirectInputDeviceInfo>();
            }

            var devices = new List<LwsDirectInputDeviceInfo>();
            try
            {
                Array rawDevices = GetStaticProperty<Array>("Devices");
                if (rawDevices == null)
                {
                    return devices;
                }

                foreach (object rawDevice in rawDevices)
                {
                    string guid = ReadField<string>(rawDevice, "guidInstance");
                    string productName = ReadField<string>(rawDevice, "productName");
                    string instanceName = ReadField<string>(rawDevice, "instanceName");
                    bool ffbCapable = ReadField<bool>(rawDevice, "FFBCapable");
                    IReadOnlyList<string> effects = ffbCapable && !string.IsNullOrWhiteSpace(guid)
                        ? GetFfbCapabilities(guid)
                        : Array.Empty<string>();
                    devices.Add(new LwsDirectInputDeviceInfo(guid, productName, instanceName, ffbCapable, effects));
                }
            }
            catch
            {
                return devices;
            }

            return devices;
        }

        public bool TryAttachPreferredDevice(string searchTerm, out LwsDirectInputDeviceInfo selectedDevice, out string message)
        {
            selectedDevice = default;
            IReadOnlyList<LwsDirectInputDeviceInfo> devices = EnumerateDevices();
            if (devices.Count == 0)
            {
                message = "No DirectInput devices were enumerated.";
                return false;
            }

            IEnumerable<LwsDirectInputDeviceInfo> ffbDevices = devices.Where(device => device.FfbCapable);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowered = searchTerm.ToLowerInvariant();
                selectedDevice = ffbDevices.FirstOrDefault(device =>
                    device.DisplayName.ToLowerInvariant().Contains(lowered) ||
                    device.InstanceName.ToLowerInvariant().Contains(lowered));
            }

            if (string.IsNullOrWhiteSpace(selectedDevice.Guid))
            {
                selectedDevice = ffbDevices.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(selectedDevice.Guid))
            {
                message = "No force-feedback-capable DirectInput device was found.";
                return false;
            }

            try
            {
                bool attached = InvokeStatic<bool>("Attach", selectedDevice.Guid);
                if (!attached)
                {
                    message = $"DIManager.Attach() failed for {selectedDevice.DisplayName}.";
                    return false;
                }

                _activeGuid = selectedDevice.Guid;
                EnsureEffectEnabled("ConstantForce");
                EnsureEffectEnabled("Damper");
                EnsureEffectEnabled("Friction");
                message = $"Attached DirectInput FFB device {selectedDevice.DisplayName}.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"DirectInput FFB attach failed: {ex.Message}";
                return false;
            }
        }

        public bool TryApplyForces(float alignment, float damping, float friction, out string message)
        {
            if (string.IsNullOrWhiteSpace(_activeGuid))
            {
                message = "No DirectInput FFB device is attached.";
                return false;
            }

            try
            {
                int constantMagnitude = ToDirectInputMagnitude(alignment);
                int damperMagnitude = ToDirectInputMagnitude(Mathf.Abs(damping));
                int frictionMagnitude = ToDirectInputMagnitude(Mathf.Abs(friction));

                bool constantOk = InvokeStatic<bool>("UpdateConstantForceSimple", _activeGuid, constantMagnitude);
                bool damperOk = InvokeStatic<bool>("UpdateDamperSimple", _activeGuid, damperMagnitude);
                bool frictionOk = InvokeStatic<bool>("UpdateFrictionSimple", _activeGuid, frictionMagnitude);

                message = constantOk && damperOk && frictionOk
                    ? "DirectInput FFB forces applied."
                    : "One or more DirectInput FFB force updates failed.";
                return constantOk && damperOk && frictionOk;
            }
            catch (Exception ex)
            {
                message = $"DirectInput FFB force update failed: {ex.Message}";
                return false;
            }
        }

        public void StopAllEffects()
        {
            if (string.IsNullOrWhiteSpace(_activeGuid) || _diManagerType == null)
            {
                return;
            }

            try
            {
                InvokeStatic<bool>("StopAllFFBEffects", _activeGuid);
            }
            catch
            {
                // FFB shutdown must never make the game unplayable.
            }
        }

        public void Shutdown()
        {
            StopAllEffects();
            if (!string.IsNullOrWhiteSpace(_activeGuid))
            {
                try
                {
                    InvokeStatic<bool>("Destroy", _activeGuid);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
            }

            _activeGuid = string.Empty;
            _enabledEffects.Clear();
        }

        private IReadOnlyList<string> GetFfbCapabilities(string guid)
        {
            try
            {
                return InvokeStatic<string[]>("GetDeviceFFBCapabilities", guid) ?? Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private void EnsureEffectEnabled(string effectName)
        {
            if (_enabledEffects.Contains(effectName))
            {
                return;
            }

            object effect = Enum.Parse(_ffbEffectsType, effectName);
            bool enabled = InvokeStatic<bool>("EnableFFBEffect", _activeGuid, effect);
            if (enabled)
            {
                _enabledEffects.Add(effectName);
            }
        }

        private static int ToDirectInputMagnitude(float normalizedForce)
        {
            return Mathf.RoundToInt(Mathf.Clamp(normalizedForce, -1f, 1f) * DirectInputMaxMagnitude);
        }

        private T InvokeStatic<T>(string methodName, params object[] args)
        {
            object result = InvokeStatic(methodName, args);
            return result is T typed ? typed : default;
        }

        private object InvokeStatic(string methodName, params object[] args)
        {
            MethodInfo method = FindStaticMethod(methodName, args);
            if (method == null)
            {
                throw new MissingMethodException(_diManagerType.FullName, methodName);
            }

            return method.Invoke(null, args);
        }

        private MethodInfo FindStaticMethod(string methodName, object[] args)
        {
            MethodInfo[] methods = _diManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool matches = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    object arg = args[i];
                    if (arg == null)
                    {
                        continue;
                    }

                    Type parameterType = parameters[i].ParameterType;
                    Type argType = arg.GetType();
                    if (!parameterType.IsAssignableFrom(argType))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return method;
                }
            }

            return null;
        }

        private T GetStaticProperty<T>(string propertyName)
        {
            PropertyInfo property = _diManagerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            object value = property != null ? property.GetValue(null) : null;
            return value is T typed ? typed : default;
        }

        private static T ReadField<T>(object target, string fieldName)
        {
            if (target == null)
            {
                return default;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            object value = field != null ? field.GetValue(target) : null;
            return value is T typed ? typed : default;
        }
    }
}
