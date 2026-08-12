using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsCabAccessoryAnchor : MonoBehaviour
    {
        [SerializeField] private string anchorId;
        [SerializeField] private LwsCabAccessoryAnchorType anchorType;
        [SerializeField] private string expectedContent;
        [SerializeField] private bool occupied;
        [SerializeField] private GameObject attachedAccessory;

        public string AnchorId => anchorId;
        public LwsCabAccessoryAnchorType AnchorType => anchorType;
        public string ExpectedContent => expectedContent;
        public bool Occupied => occupied;
        public GameObject AttachedAccessory => attachedAccessory;

        public void Configure(string id, LwsCabAccessoryAnchorType type, string content)
        {
            anchorId = id;
            anchorType = type;
            expectedContent = content;
        }

        public LwsCabAccessoryAttachmentResult Attach(GameObject accessory)
        {
            if (accessory == null)
            {
                return LwsCabAccessoryAttachmentResult.Failure($"{anchorId} cannot attach a null accessory.");
            }

            if (occupied && attachedAccessory != accessory)
            {
                return LwsCabAccessoryAttachmentResult.Failure($"{anchorId} is already occupied by {attachedAccessory.name}.");
            }

            Rigidbody rb = accessory.GetComponentInChildren<Rigidbody>(true);
            if (rb != null)
            {
                return LwsCabAccessoryAttachmentResult.Failure($"{accessory.name} has a Rigidbody and is not presentation-only.");
            }

            Collider[] colliders = accessory.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            accessory.transform.SetParent(transform, false);
            accessory.transform.localPosition = Vector3.zero;
            accessory.transform.localRotation = Quaternion.identity;
            attachedAccessory = accessory;
            occupied = true;
            return LwsCabAccessoryAttachmentResult.Success($"{accessory.name} attached to {anchorId}.");
        }

        public GameObject Detach()
        {
            GameObject detached = attachedAccessory;
            attachedAccessory = null;
            occupied = false;
            if (detached != null)
            {
                detached.transform.SetParent(null, true);
            }

            return detached;
        }

        public bool Validate(out string message)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                message = $"{name} has no cab accessory anchor ID.";
                return false;
            }

            if (!anchorId.StartsWith("IH_CabAnchor_", System.StringComparison.Ordinal))
            {
                message = $"{anchorId} is not a stable semantic cab anchor ID.";
                return false;
            }

            if (GetComponent<Rigidbody>() != null)
            {
                message = $"{anchorId} has a Rigidbody and may affect vehicle physics.";
                return false;
            }

            message = $"{anchorId} is valid.";
            return true;
        }
    }
}
