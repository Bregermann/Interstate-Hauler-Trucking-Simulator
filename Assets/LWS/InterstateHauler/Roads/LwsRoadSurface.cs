using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsRoadSurface : MonoBehaviour
    {
        [SerializeField] private string roadId = "IH_TEST_I000";
        [SerializeField] private string segmentId = "IH_TEST_I000_SURFACE";
        [SerializeField] private LwsRoadSurfaceType surfaceType = LwsRoadSurfaceType.AsphaltInterstate;
        [SerializeField] private string displayName = "Dry interstate asphalt";

        public string RoadId => roadId;
        public string SegmentId => segmentId;
        public LwsRoadSurfaceType SurfaceType => surfaceType;
        public string DisplayName => displayName;

        public void Configure(string newRoadId, string newSegmentId, LwsRoadSurfaceType newSurfaceType, string newDisplayName)
        {
            roadId = newRoadId ?? string.Empty;
            segmentId = newSegmentId ?? string.Empty;
            surfaceType = newSurfaceType;
            displayName = newDisplayName ?? string.Empty;
        }
    }
}
