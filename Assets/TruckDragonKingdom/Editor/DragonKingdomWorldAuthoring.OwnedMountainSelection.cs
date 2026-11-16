#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomWorldAuthoring
    {
        static DragonKingdomMountainRoadLayout explicitOwnedMountain;
        [Serializable]public sealed class OwnedMountainGradeInput
        {
            public string roadId,authoritativePointsSha256,actualSdkInputPointsSha256;public int markers,actualNativePieces,savedPhysicalPieces;public bool exact,nativeMeshMatchesSaved,nativePhysicalMeshMatchesSaved,nativeWorldPosesMatchSaved;public string meshComparisonScope="SAVED_RENDER_MESH_ALL_UVS_UNCHANGED_DISPOSABLE_SDK_GRADING_PHYSICAL_EQUIVALENCE_ONLY_UV4_TERRAIN_DETAIL_CONTEXT_NORMALIZED_FOR_COMPARISON";public List<OwnedMountainNativeMeshComparison> meshComparisons=new List<OwnedMountainNativeMeshComparison>();public DragonKingdomMountainRoadLayoutAuthoring.BuildBudget budget;
        }
        [Serializable]public sealed class OwnedMountainNativeMeshComparison
        {public int index,actualVertices,savedVertices;public string actualName,savedName,reason;public bool meshExact,physicalEquivalentAfterOnlyUv4Normalization,inputMeshesUnchanged,worldPoseExact,positionsExact,normalsExact,tangentsExact,colorsExact,boundsExact;public string actualMeshPayloadBefore,actualMeshPayloadAfter,savedMeshPayloadBefore,savedMeshPayloadAfter,normalizedComparisonFailure,actualUv4Sha256,savedUv4Sha256;public int actualUv4Count,savedUv4Count,actualUv4Dimension,savedUv4Dimension;public Vector4 actualUv4First,savedUv4First;public float maximumVertexDelta,maximumNormalDelta;public Bounds actualBounds,savedBounds;public Vector3 actualPosition,savedPosition,actualScale,savedScale;public Quaternion actualRotation,savedRotation;}
        public static OwnedMountainGradeInput LastOwnedMountainGradeInput{get;private set;}
        public static IDisposable SelectQualifiedOwnedMountainCandidate(DragonKingdomMountainRoadLayout layout)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||explicitOwnedMountain!=null)throw new InvalidOperationException("Only one stopped explicit candidate road selection may be active.");
            DragonKingdomMountainRoadLayoutAuthoring.ReplaceOwnedSpecification(Roads(),layout);explicitOwnedMountain=layout;LastOwnedMountainGradeInput=null;return new OwnedMountainSelection(layout);
        }
        sealed class OwnedMountainSelection:IDisposable
        {
            readonly DragonKingdomMountainRoadLayout selected;bool disposed;public OwnedMountainSelection(DragonKingdomMountainRoadLayout layout){selected=layout;}
            public void Dispose(){if(disposed)return;if(explicitOwnedMountain!=selected)throw new InvalidOperationException("Owned authoring selection lost its sole authority.");explicitOwnedMountain=null;disposed=true;}
        }
        static RoadSpec[] SelectExplicitOwnedMountainSpecifications(RoadSpec[] original)=>explicitOwnedMountain==null?original:DragonKingdomMountainRoadLayoutAuthoring.ReplaceOwnedSpecification(original,explicitOwnedMountain);
        static bool TryExactOwnedMountainGradeMarkers(RoadSpec road,BridgeSpan[] spans,out Vector3[] points)
        {
            points=null;if(explicitOwnedMountain==null||road.id!=explicitOwnedMountain.roadId)return false;points=road.points.ToArray();var stations=new List<float>{0};float distance=0;for(int i=1;i<points.Length;i++){distance+=Vector3.Distance(points[i-1],points[i]);stations.Add(distance);}
            // The qualified layout already contains the exact cave boundary.
            // A new crossing must be explicitly rebaked into the one saved
            // authority, never silently add a second grading-only alignment.
            foreach(var span in spans)foreach(float boundary in new[]{span.start,span.end})if(!stations.Any(station=>Mathf.Abs(station-boundary)<=.001f))throw new InvalidOperationException("Qualified owned road lacks an actual physical crossing/exclusion boundary station; explicit layout rebake required: "+span.lower+" at "+boundary);
            if(DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(points)!=explicitOwnedMountain.authoritativePointsSha256)throw new InvalidOperationException("Selected road markers differ from the qualified saved physical authority.");return true;
        }
        static bool TryCreateExplicitOwnedMountainGrade(ERRoadNetwork network,ERModularBase owner,ERRoadType type,string name,Vector3[] points,ERMarkerControlType[] controls,out ERRoad road)
        {
            road=null;if(explicitOwnedMountain==null||name!=explicitOwnedMountain.roadId)return false;
            if(points.Length!=646||controls.Length!=points.Length||controls.Any(control=>control!=ERMarkerControlType.StraightXZY)||DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(points)!=explicitOwnedMountain.authoritativePointsSha256)throw new InvalidOperationException("Exact646 qualified physical stations and original straightXYZ controls must supply native grading.");
            road=DragonKingdomMountainRoadLayoutAuthoring.CreateQualifiedGradeRoad(network,owner,type,name,points,controls,out var budget);LastOwnedMountainGradeInput=new OwnedMountainGradeInput{roadId=name,markers=points.Length,authoritativePointsSha256=explicitOwnedMountain.authoritativePointsSha256,actualSdkInputPointsSha256=DragonKingdomMountainRoadLayoutAuthoring.PointsDigest(road.GetMarkerPositions()),budget=budget};LastOwnedMountainGradeInput.exact=LastOwnedMountainGradeInput.actualSdkInputPointsSha256==LastOwnedMountainGradeInput.authoritativePointsSha256;var actualRoad=road;var native=actualRoad.roadScript.GetComponentsInChildren<MeshCollider>(true).Where(collider=>collider.enabled&&!collider.isTrigger&&collider.sharedMesh!=null&&collider.sharedMesh.vertexCount>2&&(actualRoad.roadScript.surfaceMesh==null||!collider.transform.IsChildOf(actualRoad.roadScript.surfaceMesh.transform))).ToArray();LastOwnedMountainGradeInput.actualNativePieces=native.Length;LastOwnedMountainGradeInput.savedPhysicalPieces=explicitOwnedMountain.physicalPieces.Length;
            for(int i=0;i<Math.Max(native.Length,explicitOwnedMountain.physicalPieces.Length);i++)
            {
                var comparison=new OwnedMountainNativeMeshComparison{index=i};LastOwnedMountainGradeInput.meshComparisons.Add(comparison);
                if(i>=native.Length||i>=explicitOwnedMountain.physicalPieces.Length){comparison.reason="Actual/saved native piece membership differs";continue;}
                var current=native[i];var saved=explicitOwnedMountain.physicalPieces[i];comparison.actualName=current.name;comparison.savedName=saved.nativeSource;comparison.actualVertices=current.sharedMesh.vertexCount;comparison.savedVertices=saved.mesh.vertexCount;comparison.actualPosition=current.transform.position;comparison.savedPosition=saved.position;comparison.actualRotation=current.transform.rotation;comparison.savedRotation=saved.rotation;comparison.actualScale=current.transform.lossyScale;comparison.savedScale=saved.scale;
                CompareOwnedMountainMeshPayload(current.sharedMesh,saved.mesh,comparison);
            }
            LastOwnedMountainGradeInput.nativeMeshMatchesSaved=native.Length==explicitOwnedMountain.physicalPieces.Length&&LastOwnedMountainGradeInput.meshComparisons.All(c=>c.meshExact);LastOwnedMountainGradeInput.nativePhysicalMeshMatchesSaved=native.Length==explicitOwnedMountain.physicalPieces.Length&&LastOwnedMountainGradeInput.meshComparisons.All(c=>c.physicalEquivalentAfterOnlyUv4Normalization);LastOwnedMountainGradeInput.nativeWorldPosesMatchSaved=native.Length==explicitOwnedMountain.physicalPieces.Length&&LastOwnedMountainGradeInput.meshComparisons.All(c=>c.worldPoseExact);
            if(!LastOwnedMountainGradeInput.exact||!LastOwnedMountainGradeInput.nativePhysicalMeshMatchesSaved||!LastOwnedMountainGradeInput.nativeWorldPosesMatchSaved)throw new InvalidOperationException("Actual public SDK grading markers/native pavement differ from the qualified graph/saved mesh authority: "+JsonUtility.ToJson(LastOwnedMountainGradeInput));return true;
        }
        public static OwnedMountainGradeInput InspectSelectedOwnedMountainActualSdkInput(Scene scene)
        {
            if(explicitOwnedMountain==null||!scene.IsValid()||!scene.isLoaded||UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None).Length!=0)throw new InvalidOperationException("One explicit stopped owned road scope and isolated vendor network required.");var terrains=Terrain.activeTerrains.ToArray();if(terrains.Any(terrain=>terrain.gameObject.scene!=scene))throw new InvalidOperationException("Do not touch unrelated active terrains during SDK input proof.");GameObject instance=null;
            try
            {
                // This bounded input/mesh proof never builds or deforms the
                // original terrain. Full candidate integration does that in
                // its existing cloned-field SDK stage and original gates.
                foreach(var terrain in terrains)terrain.enabled=false;
                var plan=InspectNativeMainRoadGradingPlan(explicitOwnedMountain.roadId,explicitOwnedMountain.originalUnderground,true);instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab"),scene);var owner=instance.GetComponent<ERModularBase>();owner.RoadNetworkInit();var network=new ERRoadNetwork(owner);var type=new ERRoadType{roadTypeName=explicitOwnedMountain.roadId,roadWidth=explicitOwnedMountain.width,hasMeshCollider=true,roadMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/EasyRoads3D/Resources/Materials/roads/road material.mat")};RegisterOwnedNativeGradingType(type,owner,28,110);
                if(!TryCreateExplicitOwnedMountainGrade(network,owner,type,explicitOwnedMountain.roadId,plan.actualGradingMarkers,Enumerable.Repeat(ERMarkerControlType.StraightXZY,plan.actualGradingMarkers.Length).ToArray(),out _))throw new InvalidOperationException("Explicit saved road did not supply the native SDK input proof.");return LastOwnedMountainGradeInput;
            }
            finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);foreach(var terrain in terrains)if(terrain!=null)terrain.enabled=true;}
        }
    }
}
#endif
