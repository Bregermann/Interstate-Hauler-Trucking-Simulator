#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomDiggerCavernAuthoring
    {
        const string RasterClearancePolicy="OneMeasuredHorizontalVoxelDiagonalEachSide01";
        static bool MeasuredRasterClearanceRequested()
        {
            string policy=ProfileArgument("-tsDragonCavernRasterClearance");
            if(policy!=null&&policy!=RasterClearancePolicy)throw new ArgumentException("Unknown explicit native cavern raster-clearance policy: "+policy);
            return policy!=null;
        }
        // Digger owns actual grid/voxelization/mesh/collision generation. A
        // nominal closed input exactly on a reserved boundary can rasterize
        // inward. Reserve one measured horizontal voxel diagonal on each side
        // before voxelization; never shrink the required driving-space box.
        public static float MeasuredHorizontalVoxelGuardBand(Vector3[] actualGrid)
        {
            if(actualGrid==null||actualGrid.Length!=2||actualGrid.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)||v.x<=0||v.y<=0||v.z<=0))throw new ArgumentException("Both actual initialized Digger tile grids are required.");
            double diagonal=actualGrid.Max(v=>Math.Sqrt((double)v.x*v.x+(double)v.z*v.z));
            if(double.IsNaN(diagonal)||double.IsInfinity(diagonal)||diagonal>float.MaxValue)throw new ArgumentException("Finite representable native voxel diagonal required.");
            return (float)diagonal;
        }
        [Serializable]public sealed class NativeDoorwayClearance
        {
            public string scope="ACTUAL_NATIVE_DIGGER_UPPER_ORIGINAL_DOORWAY_VOLUME_AND_WALL_RAY_CONTROLS_NOT_FLOOR_ROAD_FLIGHT_OR_VISUAL_ACCEPTANCE";
            public int nativeMeshColliders;public bool allJoinsClear,nativeWallControlsPassed;public NativeDoorwayJoin[] joins;
        }
        [Serializable]public sealed class NativeDoorwayJoin
        {public Vector3 boundary,queryCenter,queryHalfExtents;public Quaternion queryRotation;public float requiredWidth,floorExcludedMetres=2;public int nativeWallRayHits;public string[] obstructingNativeMeshes;public bool clear;}
        public static NativeDoorwayClearance MeasureNativeDoorwaySpace(DragonKingdomWorld world)
        {
            if(world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||!world.gameObject.scene.isLoaded)throw new InvalidOperationException("Actual loaded owned world clone required for native doorway queries.");
            var systems=world.GetComponentsInChildren<Digger.Modules.Core.Sources.DiggerSystem>(true);if(systems.Length!=2)throw new InvalidOperationException("Both actual initialized/persisted native cavern tiles required.");
            var native=new HashSet<Collider>(systems.SelectMany(system=>system.GetComponentsInChildren<MeshCollider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&c.sharedMesh!=null));if(native.Count==0)throw new InvalidOperationException("A missing or disabled native mesh cannot qualify an empty doorway.");
            Physics.SyncTransforms();var proof=new NativeDoorwayClearance{nativeMeshColliders=native.Count};var joins=new List<NativeDoorwayJoin>();
            foreach(var site in DescribeCurrentCavernJoinSites(world))
            {
                // Signed floor/road contracts remain separately mandatory. This
                // box excludes only the lower2m and tests the unchanged upper
                // driving-space width/height/depth with a5cm sampling inset.
                var row=new NativeDoorwayJoin{boundary=site.boundary,requiredWidth=site.roadWidth+14,queryCenter=site.boundary+Vector3.up*28.5f,queryHalfExtents=new Vector3((site.roadWidth+14)*.5f-.05f,26.5f,15.95f),queryRotation=Quaternion.LookRotation(site.inward,Vector3.up)};
                row.obstructingNativeMeshes=Physics.OverlapBox(row.queryCenter,row.queryHalfExtents,row.queryRotation,~0,QueryTriggerInteraction.Ignore).Where(native.Contains).Cast<MeshCollider>().Select(c=>AssetDatabase.GetAssetPath(c.sharedMesh)).Distinct().OrderBy(x=>x,StringComparer.Ordinal).ToArray();
                var right=Vector3.Cross(Vector3.up,site.inward).normalized;foreach(float side in new[]{-1f,1f})if(Physics.RaycastAll(new Ray(site.boundary+Vector3.up*15,right*side),300,~0,QueryTriggerInteraction.Ignore).Any(hit=>native.Contains(hit.collider)))row.nativeWallRayHits++;
                row.clear=row.obstructingNativeMeshes.Length==0;joins.Add(row);
            }
            proof.joins=joins.ToArray();proof.nativeWallControlsPassed=joins.Count==2&&joins.All(j=>j.nativeWallRayHits==2);proof.allJoinsClear=proof.nativeWallControlsPassed&&joins.All(j=>j.clear);return proof;
        }

    }
}
#endif
