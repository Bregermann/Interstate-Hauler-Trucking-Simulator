using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LWS.InterstateHauler
{
    public static class LwsWheelDeviceDiscovery
    {
        public static List<LwsWheelDeviceDescriptor> GetConnectedWheelCandidates()
        {
            var descriptors = new List<LwsWheelDeviceDescriptor>();
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!IsWheelCandidate(device))
                {
                    continue;
                }

                descriptors.Add(CreateDescriptor(device));
            }

            foreach (LwsWheelDeviceDescriptor directInputDescriptor in LwsDirectInputBackendDiscovery.EnumerateDeviceDescriptors())
            {
                bool alreadyRecorded = descriptors.Exists(descriptor =>
                    descriptor.path == directInputDescriptor.path ||
                    (!string.IsNullOrWhiteSpace(directInputDescriptor.directInputGuid) &&
                     descriptor.capabilities != null &&
                     descriptor.capabilities.Contains(directInputDescriptor.directInputGuid)));
                if (!alreadyRecorded)
                {
                    descriptors.Add(directInputDescriptor);
                }
            }

            return descriptors;
        }

        public static InputDevice FindFirstMatchingDevice(LwsWheelDeviceProfile profile)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!IsWheelCandidate(device))
                {
                    continue;
                }

                if (profile == null || profile.Matches(CreateDescriptor(device)))
                {
                    return device;
                }
            }

            return null;
        }

        public static InputDevice FindDeviceByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            foreach (InputDevice device in InputSystem.devices)
            {
                if (device.path == path)
                {
                    return device;
                }
            }

            return null;
        }

        public static LwsWheelDeviceDescriptor CreateDescriptor(InputDevice device)
        {
            var descriptor = new LwsWheelDeviceDescriptor();
            if (device == null)
            {
                return descriptor;
            }

            descriptor.displayName = device.displayName ?? string.Empty;
            descriptor.manufacturer = device.description.manufacturer ?? string.Empty;
            descriptor.product = device.description.product ?? string.Empty;
            descriptor.deviceClass = device.description.deviceClass ?? string.Empty;
            descriptor.layout = device.layout;
            descriptor.path = device.path;
            descriptor.interfaceName = device.description.interfaceName ?? string.Empty;
            descriptor.capabilities = device.description.capabilities ?? string.Empty;
            descriptor.directInputGuid = device.description.serial ?? string.Empty;
            descriptor.directInputForceFeedbackCapable = descriptor.capabilities.Contains("\"FFBCapable\":true");

            foreach (InputControl control in device.allControls)
            {
                if (control is AxisControl)
                {
                    descriptor.axes.Add($"{control.path} [{control.displayName}]");
                }
                else if (control is ButtonControl)
                {
                    descriptor.buttons.Add($"{control.path} [{control.displayName}]");
                }
                else if (control is DpadControl)
                {
                    descriptor.dpads.Add($"{control.path} [{control.displayName}]");
                }
            }

            return descriptor;
        }

        public static bool TryFindDominantControl(InputDevice device, LwsWheelControlKind kind, out LwsWheelControlBinding binding)
        {
            binding = default;
            if (device == null)
            {
                return false;
            }

            InputControl bestControl = null;
            float bestMagnitude = 0f;
            foreach (InputControl control in device.allControls)
            {
                float magnitude = 0f;
                if (kind == LwsWheelControlKind.Axis && control is AxisControl axis)
                {
                    magnitude = UnityEngine.Mathf.Abs(axis.ReadValue());
                }
                else if (kind == LwsWheelControlKind.Button && control is ButtonControl button)
                {
                    magnitude = button.ReadValue();
                }

                if (magnitude > bestMagnitude)
                {
                    bestMagnitude = magnitude;
                    bestControl = control;
                }
            }

            if (bestControl == null || bestMagnitude < 0.2f)
            {
                return false;
            }

            binding = LwsWheelControlBinding.Create(
                LwsWheelLogicalControl.None,
                kind,
                bestControl.path,
                bestControl.displayName);
            return true;
        }

        public static bool TryReadAxis(InputDevice preferredDevice, LwsWheelControlBinding binding, out float value)
        {
            value = 0f;
            InputControl control = ResolveControl(preferredDevice, binding);
            if (control is AxisControl axis)
            {
                value = axis.ReadValue();
                return true;
            }

            if (control is ButtonControl button)
            {
                value = button.ReadValue();
                return true;
            }

            return false;
        }

        public static bool TryReadButton(InputDevice preferredDevice, LwsWheelControlBinding binding, out bool pressed)
        {
            pressed = false;
            InputControl control = ResolveControl(preferredDevice, binding);
            if (control is ButtonControl button)
            {
                pressed = button.ReadValue() >= UnityEngine.Mathf.Max(0.01f, binding.pressThreshold);
                return true;
            }

            if (control is AxisControl axis)
            {
                pressed = UnityEngine.Mathf.Abs(axis.ReadValue()) >= UnityEngine.Mathf.Max(0.01f, binding.pressThreshold);
                return true;
            }

            return false;
        }

        private static InputControl ResolveControl(InputDevice preferredDevice, LwsWheelControlBinding binding)
        {
            if (!binding.IsBound)
            {
                return null;
            }

            InputControl control = ResolveControlOnDevice(preferredDevice, binding.controlPath);
            if (control != null)
            {
                return control;
            }

            foreach (InputDevice device in InputSystem.devices)
            {
                control = ResolveControlOnDevice(device, binding.controlPath);
                if (control != null)
                {
                    return control;
                }
            }

            return null;
        }

        private static InputControl ResolveControlOnDevice(InputDevice device, string controlPath)
        {
            if (device == null || string.IsNullOrWhiteSpace(controlPath))
            {
                return null;
            }

            foreach (InputControl control in device.allControls)
            {
                if (control.path == controlPath || control.name == controlPath || control.displayName == controlPath)
                {
                    return control;
                }
            }

            return null;
        }

        private static bool IsWheelCandidate(InputDevice device)
        {
            if (device == null)
            {
                return false;
            }

            string combined = $"{device.displayName} {device.description.manufacturer} {device.description.product} {device.description.deviceClass} {device.layout}".ToLowerInvariant();
            return combined.Contains("g29") ||
                   combined.Contains("logitech") ||
                   combined.Contains("driving force") ||
                   combined.Contains("wheel") ||
                   combined.Contains("joystick") ||
                   combined.Contains("gamepad") ||
                   combined.Contains("hid");
        }
    }
}
