using UnityEngine;
namespace LWS.TruckCamping
{
    [CreateAssetMenu(menuName="Truck Sports/Camping/Landmark Rock Presentation")]
    public sealed class CampLandmarkRockProfile : ScriptableObject
    {
        public const string ResourceName="TruckCamping/Camp Haul landmark rock presentation";
        [Tooltip("Render-only owned closed rock variant; original FBX mesh, authored basis and textures, no scripts/colliders.")]
        public GameObject boulderPrefab;
        // Retained serialized field identity from the rejected V29 prototype.
        public Mesh originalRockAMesh;
        public Material ownedMaterial;
        public Target[] targets;
        [System.Serializable] public sealed class Pose { public Vector3 position;public Quaternion rotation=Quaternion.identity;[Min(.0001f)]public float uniformScale=1; }
        [System.Serializable] public sealed class Target
        {
            public int landmark;
            public bool overheadCap;
            [HideInInspector] public int[] siblingPath;
            [HideInInspector] public Vector3 sourcePosition,sourceScale,colliderCenter,colliderSize;
            [HideInInspector] public Quaternion sourceRotation;
            [HideInInspector] public Mesh sourceMesh;
            [HideInInspector] public Material[] sourceMaterials;
            [HideInInspector] public Bounds sourceBounds,allowedEnvelope;
            public Pose[] boulders;
        }
    }
}
