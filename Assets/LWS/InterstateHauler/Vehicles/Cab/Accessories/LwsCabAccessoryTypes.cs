using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsCabAccessoryAnchorType
    {
        DashboardAccessory,
        HangingAccessory,
        PassengerSeat,
        Sleeper,
        PersonalMemento
    }

    [Serializable]
    public struct LwsCabAccessoryAnchorDefinition
    {
        public string anchorId;
        public LwsCabAccessoryAnchorType anchorType;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
        public string expectedContent;
        public string notes;

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                message = "Cab accessory anchor ID is empty.";
                return false;
            }

            if (!anchorId.StartsWith("IH_CabAnchor_", StringComparison.Ordinal))
            {
                message = $"{anchorId} is not a stable Interstate Hauler cab anchor ID.";
                return false;
            }

            if (localScale.x <= 0f || localScale.y <= 0f || localScale.z <= 0f)
            {
                message = $"{anchorId} has invalid local scale.";
                return false;
            }

            message = $"{anchorId} is valid.";
            return true;
        }
    }

    public readonly struct LwsCabAccessoryAttachmentResult
    {
        public LwsCabAccessoryAttachmentResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static LwsCabAccessoryAttachmentResult Success(string message)
        {
            return new LwsCabAccessoryAttachmentResult(true, message);
        }

        public static LwsCabAccessoryAttachmentResult Failure(string message)
        {
            return new LwsCabAccessoryAttachmentResult(false, message);
        }
    }
}
