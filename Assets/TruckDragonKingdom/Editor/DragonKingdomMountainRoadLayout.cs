#if UNITY_EDITOR
using System;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Editor-only source bake. Saved runtime worlds consume its graph and meshes,
    // never regenerate a route when entering Play Mode.
    public sealed class DragonKingdomMountainRoadLayout : ScriptableObject
    {
        public string profile = "CircularMountainPass01", roadId = "caverns-volcano", from = "caverns", to = "volcano";
        public int schema = 1;
        public float width = 18, maximumGrade, measuredLength, mainCircularRadius, connectorMinimumRadius;
        public float conservativeMainFootprint = 238.25f, mainTerrainRightMargin;
        public string originalRoadSpecificationSha256, originalGradingPlanSha256, sdkSha256, authoritativePointsSha256;
        public string physicalInputPointsSha256;
        public int physicalQueries, physicalMisses;
        public float maximumPhysicalHeightError;
        public Vector3[] preservedPrefix, connectorGuides, circularGuides, authoritativePoints;
        public Vector3[] actualVendorConnector, actualVendorCircular;
        public OwnedRoadVendorQuarter[] circularQuarters;
        public Vector3[] actualConnectorMarkers, actualCircularMarkers;
        public float maximumSemanticEndpointSampleDistance;
        public string[] actualConnectorControls, actualCircularControls;
        public float connectorStartFlat = 16, connectorEndFlat = 34, circularStartFlat = 34, circularEndFlat = 34;
        public Vector3 sourceEndpoint = new Vector3(1040, 240, 840);
        public Bounds originalUnderground;
        public OwnedRoadPhysicalPiece[] physicalPieces;
        public string scope = "OWNED_AUTHORING_PATH_AND_VENDOR_MESH_BAKE_REQUIRES_NATIVE_TERRAIN_PHYSICAL_WORLD_AND_SAVED_ACCEPTANCE";
    }

    [Serializable]
    public sealed class OwnedRoadVendorQuarter
    {
        public Vector3[] guides, actualMarkers, actualSpline, selectedSamples;
        public string[] actualControls;
        public float actualRadius, maximumSemanticEndpointSampleDistance;
    }

    [Serializable]
    public sealed class OwnedRoadPhysicalPiece
    {
        public Mesh mesh;
        public Vector3 position, scale;
        public Quaternion rotation;
        public string nativeSource;
    }
}
#endif
