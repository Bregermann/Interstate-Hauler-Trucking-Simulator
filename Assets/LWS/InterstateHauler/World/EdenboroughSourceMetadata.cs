using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class EdenboroughSourceMetadata : MonoBehaviour
    {
        public string districtId;
        public string regionId;
        public string coordinateSystem;
        public string sourceUrl;
        public string sourceSha256;
        public string sourceDownloadedUtc;
        public string sourceBoundary;
        public string license;
        public string attribution;
        public string attributionUrl;
        public int pipelineVersion;
        public bool leftHandTraffic;
        public int roadCount;
        public int laneCount;
        public int turnCount;
        public string roadGeometryAuthority = "EasyRoads3D Pro";
        public int easyRoadsObjectCount;
        public int excludedGradeSeparatedRoadCount;
        public EdenboroughTrafficData trafficData;
    }
}
