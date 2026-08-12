using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsWheelProfileControlDescriptor
    {
        public LwsWheelLogicalControl logicalControl;
        public string displayName;
        public LwsWheelControlKind expectedControlKind;
        public bool requiredForDriving;
    }

    [CreateAssetMenu(menuName = "Interstate Hauler/Input/Wheel Device Profile", fileName = "IH_WheelDeviceProfile")]
    public sealed class LwsWheelDeviceProfile : ScriptableObject
    {
        [SerializeField] private string stableId = "logitech.g29.driving-force";
        [SerializeField] private string displayName = "Logitech G29 Driving Force";
        [SerializeField] private string manufacturerHint = "Logitech";
        [SerializeField] private string productHint = "G29";
        [SerializeField] private bool requiresSeparateShifter = true;
        [SerializeField] private bool supportsForceFeedbackThroughDirectInput = true;
        [SerializeField] private LwsWheelCompatibilityStatus compatibilityStatus = LwsWheelCompatibilityStatus.SupportedNotPhysicallyVerified;
        [SerializeField] private List<LwsWheelProfileControlDescriptor> controls = new List<LwsWheelProfileControlDescriptor>();

        public string StableId => stableId;
        public string DisplayName => displayName;
        public string ManufacturerHint => manufacturerHint;
        public string ProductHint => productHint;
        public bool RequiresSeparateShifter => requiresSeparateShifter;
        public bool SupportsForceFeedbackThroughDirectInput => supportsForceFeedbackThroughDirectInput;
        public LwsWheelCompatibilityStatus CompatibilityStatus => compatibilityStatus;
        public IReadOnlyList<LwsWheelProfileControlDescriptor> Controls => controls;

        public bool Matches(LwsWheelDeviceDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return false;
            }

            string combined = $"{descriptor.displayName} {descriptor.manufacturer} {descriptor.product} {descriptor.layout}".ToLowerInvariant();
            bool hasManufacturer = string.IsNullOrWhiteSpace(manufacturerHint) ||
                                   combined.Contains(manufacturerHint.ToLowerInvariant());
            bool hasProduct = string.IsNullOrWhiteSpace(productHint) ||
                              combined.Contains(productHint.ToLowerInvariant()) ||
                              descriptor.LooksLikeLogitechG29();
            return hasManufacturer && hasProduct;
        }

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                message = $"{name} has no stable wheel profile ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                message = $"{stableId} has no display name.";
                return false;
            }

            if (controls == null || controls.Count == 0)
            {
                message = $"{stableId} has no logical controls.";
                return false;
            }

            bool hasSteering = HasControl(LwsWheelLogicalControl.Steering);
            bool hasThrottle = HasControl(LwsWheelLogicalControl.Throttle);
            bool hasBrake = HasControl(LwsWheelLogicalControl.Brake);
            bool hasClutch = HasControl(LwsWheelLogicalControl.Clutch);
            bool hasGate6 = HasControl(LwsWheelLogicalControl.ShifterGate6);
            bool hasReverse = HasControl(LwsWheelLogicalControl.ShifterReverse);
            bool hasRange = HasControl(LwsWheelLogicalControl.RangeToggle);
            bool hasSplitter = HasControl(LwsWheelLogicalControl.SplitterToggle);

            if (!hasSteering || !hasThrottle || !hasBrake || !hasClutch || !hasGate6 || !hasReverse || !hasRange || !hasSplitter)
            {
                message = $"{stableId} is missing one or more required DirectInput/G29 logical controls.";
                return false;
            }

            message = $"{stableId} DirectInput wheel device profile is valid.";
            return true;
        }

        private bool HasControl(LwsWheelLogicalControl logicalControl)
        {
            if (controls == null)
            {
                return false;
            }

            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i] != null && controls[i].logicalControl == logicalControl)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
