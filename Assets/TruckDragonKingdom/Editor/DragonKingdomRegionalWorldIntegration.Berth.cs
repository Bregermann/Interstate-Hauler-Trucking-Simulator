#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class MooringBerth
        {
            public string scope="CANDIDATE_ONLY_LOCAL_MERCHANT_BERTH_OVER_EXISTING_BAY_NOT_ORIGINAL_SEABED_OR_VISUAL_APPROVAL";
            public string policy="LEGACY_INPUT_DELTA_AND_EXACT_BOUNDED_POCKET";public BerthHullClearance actualHull;
            public Bounds core,outerEnvelope;public float targetBed=-12,collar=20,maximumLowering,maximumOutsideDelta;
            public int changedSamples,outsideSamples,outsideChangedSamples,seamSamples,shoreSamples,protectedFerrySamples;
            public bool passed,outsideMaskExact,neverRaised,seamsExact,shoreExact,ferryBedExact;public BerthTile[] tiles;
        }
        [Serializable]public sealed class BerthTile
        {public string terrain,beforeHeightSha256,afterHeightSha256,beforeOutsideSha256,afterOutsideSha256;public int changedSamples,outsideSamples;}
        [Serializable]public sealed class BerthDepthSample{public Vector3 position;public float bed,keel,clearance;}
        [Serializable]public sealed class BerthSweepSample{public Vector3 center;public Quaternion rotation;public Bounds conservativeWorldBox;public bool separatedFromActualCompleteHull;}
        [Serializable]public sealed class BerthHullClearance
        {
            public string source,sourceGuid,sourceSha256,sourceMetaSha256,actualGeometrySha256,ferryPose;
            public Bounds actualCompleteHullBounds;public float requiredKeelClearance=3,minimumKeelClearance=float.PositiveInfinity;
            public int depthSamples,departureSweepSamples;public bool sourceBytesExact,departureClear,passed;
            public BerthDepthSample[] depths;public BerthSweepSample[] sweep;
        }
        public static MooringBerth LastMeasuredMerchantBerth{get;private set;}
        static MooringBerth CreateMeasuredMerchantBerth(DragonKingdomWorld world,TileState[] tiles,bool nativeHullDepthPolicy=false)
        {
            var core=new Bounds(new Vector3(805,0,-728),new Vector3(34,0,78));var envelope=core;envelope.Expand(new Vector3(40,0,40));
            var proof=new MooringBerth{core=core,outerEnvelope=envelope,neverRaised=true,seamsExact=true,shoreExact=true,ferryBedExact=true};var rows=new List<BerthTile>();LastMeasuredMerchantBerth=proof;if(nativeHullDepthPolicy)proof.policy="NATIVE_ACTUAL_SOURCE_HULL_25_KEEL_CLEARANCES_AND_53_CONSERVATIVE_FERRY_SWEEPS";
            if(envelope.min.x<768||envelope.min.z< -787||envelope.max.z> -669)throw new InvalidOperationException("Exact authored side berth envelope differs from reviewed road/shore/seam separation.");
            foreach(var tile in tiles)
            {
                var data=tile.candidate;int n=data.heightmapResolution;var before=data.GetHeights(0,0,n,n);var desired=(float[,])before.Clone();var position=tile.terrain.transform.position;var size=data.size;
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    var p=position+new Vector3(x*size.x/(n-1),0,z*size.z/(n-1));float distance=DistanceTo(core,p);if(distance>=proof.collar)continue;
                    float old=before[z,x]*size.y+position.y;float weight=1-Mathf.SmoothStep(0,1,distance/proof.collar);float lowered=Mathf.Min(old,Mathf.Lerp(old,proof.targetBed,weight));desired[z,x]=(lowered-position.y)/size.y;
                }
                data.SetHeights(0,0,desired);tile.terrain.Flush();var after=data.GetHeights(0,0,n,n);var beforeOutside=new List<float>();var afterOutside=new List<float>();var row=new BerthTile{terrain=UnityEditor.AssetDatabase.GetAssetPath(data),beforeHeightSha256=HeightArraySha(before),afterHeightSha256=HeightArraySha(after)};
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    var p=position+new Vector3(x*size.x/(n-1),0,z*size.z/(n-1));bool outside=DistanceTo(core,p)>=proof.collar;float delta=(after[z,x]-before[z,x])*size.y;bool same=after[z,x]==before[z,x];
                    if(!same){row.changedSamples++;proof.changedSamples++;proof.maximumLowering=Mathf.Max(proof.maximumLowering,-delta);if(delta>0)proof.neverRaised=false;tile.fieldTags[z,x]=11;tile.fieldOwners[z,x]="Explicit side-moored merchant berth";}
                    if(outside){beforeOutside.Add(before[z,x]);afterOutside.Add(after[z,x]);row.outsideSamples++;proof.outsideSamples++;if(!same)proof.outsideChangedSamples++;proof.maximumOutsideDelta=Mathf.Max(proof.maximumOutsideDelta,Mathf.Abs(delta));}
                    if(x==0||z==0||x==n-1||z==n-1){proof.seamSamples++;proof.seamsExact&=same;}
                    if(p.x>=500&&p.x<=900&&p.z>= -650&&p.z<= -620){proof.shoreSamples++;proof.shoreExact&=same;}
                    if(p.x>=706&&p.x<=734&&p.z>= -1110&&p.z<= -620){proof.protectedFerrySamples++;proof.ferryBedExact&=same;}
                }
                row.beforeOutsideSha256=FloatValuesSha(beforeOutside.ToArray());row.afterOutsideSha256=FloatValuesSha(afterOutside.ToArray());if(row.beforeOutsideSha256!=row.afterOutsideSha256)throw new InvalidOperationException("Native berth changed a height sample outside the bounded authored pocket.");rows.Add(row);
            }
            proof.tiles=rows.ToArray();proof.outsideMaskExact=proof.outsideChangedSamples==0&&proof.maximumOutsideDelta==0;if(nativeHullDepthPolicy)proof.actualHull=MeasureActualMerchantBerthClearance(world);bool depthAccepted=nativeHullDepthPolicy?proof.actualHull.passed:proof.maximumLowering<=4.04f;proof.passed=proof.changedSamples>0&&proof.changedSamples<1500&&depthAccepted&&proof.neverRaised&&proof.outsideMaskExact&&proof.seamsExact&&proof.shoreExact&&proof.ferryBedExact&&proof.seamSamples>4000&&proof.shoreSamples>500&&proof.protectedFerrySamples>500;
            if(!proof.passed)throw new InvalidOperationException("Actual localized side berth failed unchanged outside-mask/seam/shore/ferry or lowering-only contract: "+JsonUtility.ToJson(proof));return proof;
        }
        public static void ValidateSavedMerchantBerth(DragonKingdomWorld world,MooringBerth proof)
        {
            bool native=proof!=null&&proof.policy=="NATIVE_ACTUAL_SOURCE_HULL_25_KEEL_CLEARANCES_AND_53_CONSERVATIVE_FERRY_SWEEPS";
            if(proof==null||!proof.passed||!proof.outsideMaskExact||!proof.neverRaised||!proof.seamsExact||!proof.shoreExact||!proof.ferryBedExact||proof.outsideChangedSamples!=0||proof.maximumOutsideDelta!=0||proof.changedSamples<1||proof.changedSamples>=1500||(!native&&proof.maximumLowering>4.04f)||proof.targetBed!= -12||proof.collar!=20||proof.core.center!=new Vector3(805,0,-728)||proof.core.size!=new Vector3(34,0,78)||proof.tiles==null||proof.tiles.Length!=4||proof.tiles.Select(t=>t.terrain).Distinct().Count()!=4||proof.seamSamples<=4000||proof.shoreSamples<=500||proof.protectedFerrySamples<=500)throw new InvalidOperationException("Exact localized authored berth/outside/ferry/shore/seam evidence required.");
            if(native)
            {
                var actual=MeasureActualMerchantBerthClearance(world);var expected=proof.actualHull;
                if(expected==null||!expected.passed||!actual.passed||actual.sourceSha256!=expected.sourceSha256||actual.sourceMetaSha256!=expected.sourceMetaSha256||actual.actualGeometrySha256!=expected.actualGeometrySha256||actual.ferryPose!=expected.ferryPose||Mathf.Abs(actual.minimumKeelClearance-expected.minimumKeelClearance)>.01f||actual.actualCompleteHullBounds!=expected.actualCompleteHullBounds)throw new InvalidOperationException("Saved actual merchant source-hull,25keel contacts or53ferry sweep clearance differs.");
            }
            foreach(var row in proof.tiles)
            {
                var data=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>(row.terrain);if(data==null||!world.GetComponentsInChildren<Terrain>(true).Any(t=>t.terrainData==data)||row.beforeOutsideSha256!=row.afterOutsideSha256||HeightArraySha(data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution))!=row.afterHeightSha256)throw new InvalidOperationException("Saved actual berth terrain differs from its native measured full-array hash.");
            }
        }
        static BerthHullClearance MeasureActualMerchantBerthClearance(DragonKingdomWorld world)
        {
            var proof=new BerthHullClearance{source=MooredTraderSource,sourceGuid=AssetDatabase.AssetPathToGUID(MooredTraderSource),sourceSha256=AssetSha(MooredTraderSource),sourceMetaSha256=AssetSha(MooredTraderSource+".meta"),departureClear=true};
            PreviewRenderUtility preview=null;GameObject root=null;
            try
            {
                preview=new PreviewRenderUtility();root=new GameObject("Inactive disposable merchant source-hull clearance");root.hideFlags=HideFlags.HideAndDontSave;root.SetActive(false);preview.AddSingleGO(root);
                var ship=PrepareMooredTraderHull(root.transform,out _,out _);var bounds=BoundsOf(ship);proof.actualCompleteHullBounds=bounds;
                proof.actualGeometrySha256=FinishDigest(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new RidgeGeometryContainer{bindings=RidgeGeometryBindings(ship,new Dictionary<string,string>())})));
                var terrains=world.GetComponentsInChildren<Terrain>(true);var depths=new List<BerthDepthSample>();
                for(int z=0;z<5;z++)for(int x=0;x<5;x++)
                {
                    var p=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,x/4f),0,Mathf.Lerp(bounds.min.z,bounds.max.z,z/4f));float bed=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p),clearance=bounds.min.y-bed;
                    depths.Add(new BerthDepthSample{position=p,bed=bed,keel=bounds.min.y,clearance=clearance});proof.minimumKeelClearance=Mathf.Min(proof.minimumKeelClearance,clearance);
                }
                proof.depths=depths.ToArray();proof.depthSamples=depths.Count;
                var ferry=world.extractionSites.Single(s=>s.vessel!=null).vessel;proof.ferryPose=Pose(ferry.transform);var sweep=new List<BerthSweepSample>();
                for(int i=0;i<=52;i++)
                {
                    var center=Vector3.Lerp(ferry.transform.position,ferry.departureWaypoints.Last().position,i/52f)+Vector3.up*8;var box=new Bounds(center,Vector3.zero);
                    foreach(float x in new[]{-12f,12f})foreach(float y in new[]{-12f,12f})foreach(float z in new[]{-26f,26f})box.Encapsulate(center+ferry.transform.rotation*new Vector3(x,y,z));
                    // The preview has an isolated physics scene. Prove no actual
                    // hull geometry can intersect the same53ferry boxes using
                    // conservative complete-source bounds; never query the wrong
                    // PhysicsScene and misreport an empty result as clearance.
                    bool separate=!box.Intersects(bounds);proof.departureClear&=separate;sweep.Add(new BerthSweepSample{center=center,rotation=ferry.transform.rotation,conservativeWorldBox=box,separatedFromActualCompleteHull=separate});
                }
                proof.sweep=sweep.ToArray();proof.departureSweepSamples=sweep.Count;proof.sourceBytesExact=AssetSha(proof.source)==proof.sourceSha256&&AssetSha(proof.source+".meta")==proof.sourceMetaSha256;
                proof.passed=proof.sourceBytesExact&&proof.depthSamples==25&&proof.minimumKeelClearance>=3&&proof.departureSweepSamples==53&&proof.departureClear;return proof;
            }
            finally{if(preview!=null)preview.Cleanup();else if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
        }
        static string HeightArraySha(float[,] heights)
        {var bytes=new byte[heights.Length*sizeof(float)];Buffer.BlockCopy(heights,0,bytes,0,bytes.Length);return Hash(bytes).Replace("-","").ToLowerInvariant();}
        static string FloatValuesSha(float[] values)
        {var bytes=new byte[values.Length*sizeof(float)];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);return Hash(bytes).Replace("-","").ToLowerInvariant();}
    }
}
#endif
