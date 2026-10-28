using System.Linq;
using LWS.TruckDanceOff;
using UnityEngine;

namespace LWS.TruckOrchestra
{
    // DanceTruckRig remains the pose authority. A full NWH tractor also has a
    // separate suspended cab; keep that original body in the same stage pose
    // instead of pulling it around with an active suspension joint in mid-dance.
    [DefaultExecutionOrder(500)]
    public sealed class OrchestraAssemblyStage : MonoBehaviour
    {
        public OrchestraMatch match;
        public DanceTruckRig dancer;
        Rigidbody[] secondary;
        Vector3[] localPosition;
        Quaternion[] localRotation;
        bool[] wasKinematic;
        bool prepared;
        public int OriginalBodyCount => secondary == null ? 0 : secondary.Length + 1;
        void Awake()
        {
            var root = dancer.truck.body;
            secondary = GetComponentsInChildren<Rigidbody>(true).Where(body => body != root).ToArray();
            localPosition = secondary.Select(body => root.transform.InverseTransformPoint(body.position)).ToArray();
            localRotation = secondary.Select(body => Quaternion.Inverse(root.rotation) * body.rotation).ToArray();
            wasKinematic = secondary.Select(body => body.isKinematic).ToArray();
        }
        public void Begin()
        {
            foreach (var body in secondary) { if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; } body.isKinematic = true; }
            prepared = true; Follow();
        }
        void LateUpdate() { if (prepared && match != null && match.Running && !match.Paused) Follow(); }
        void Follow()
        {
            var root = dancer.truck.body;
            for (int i = 0; i < secondary.Length; i++)
            {
                secondary[i].MovePosition(root.position + root.rotation * localPosition[i]);
                secondary[i].MoveRotation(root.rotation * localRotation[i]);
            }
        }
        void OnDestroy() { if (secondary != null) for (int i = 0; i < secondary.Length; i++) if (secondary[i] != null) secondary[i].isKinematic = wasKinematic[i]; }
    }
}
