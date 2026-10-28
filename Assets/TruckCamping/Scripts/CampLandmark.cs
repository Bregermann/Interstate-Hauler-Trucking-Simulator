using UnityEngine;
namespace LWS.TruckCamping
{
    public sealed class CampLandmark : MonoBehaviour
    {
        public string id, label;
        public int index;
        public CampPlaceKind kind;
        public Transform parking, activity, view;
        public float discoveryRadius = 35;
    }
}
