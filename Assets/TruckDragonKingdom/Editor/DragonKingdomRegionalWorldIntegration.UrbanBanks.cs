#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const float UrbanRetainingBank=12;
        [Serializable]public sealed class UrbanBankProof
        {
            public string scope="CANDIDATE_ONLY_ACTUAL_URBAN_CREEK_RETAINING_BANK_40_TO_12M_NO_ROAD_WATERCORE_OR_OUTSIDE_CHANGE";
            public float originalBank=40,retainingBank=12,creekCore=34,maximumRaise,maximumOutsideDelta;
            public int changedSamples,outsideSamples,outsideChangedSamples,coreSamples,protectedRoadOrPoseSamples,seamSamples;public bool passed,neverLowered,outsideMaskExact,waterCoreExact,roadsAndPosesExact,seamsExact;
            public UrbanBankTile[] tiles;public UrbanBankDistrict[] districts;
        }
        [Serializable]public sealed class UrbanBankTile
        {public string terrain,beforeHeightSha256,afterHeightSha256,beforeOutsideSha256,afterOutsideSha256,afterBankSha256;public int changedSamples,outsideSamples,bankSamples,resolution;public Vector3 origin,size;}
        [Serializable]public sealed class UrbanBankDistrict
        {public string region;public int samples,originalDrySamples,retainingDrySamples,newDrySamples,newDryStillUnderSea;public float minimumNewDryFreeboard;public UrbanBankSample[] points;}
        [Serializable]public sealed class UrbanBankSample
        {public Vector3 position;public float beforeGround,afterGround,creekBankDistance;public bool originalDry,retainingDry,insideChangedMask;}
        static bool IntendedDrySettlementPositionAtBank(string region,Vector3 p,float bank)=>!HydrologyCore(p)&&CreekBankDistance(p)>=bank&&(region!="harbor"||p.z>=-625);
        static bool InsideUrbanBankMask(Vector3 p,SettlementField[] fields)=>!HydrologyCore(p)&&CreekBankDistance(p)>0&&CreekBankDistance(p)<40&&fields.Any(f=>DistanceTo(f.bounds,p)<=120);
        static bool UrbanRoadOrPoseProtected(DragonKingdomWorld world,Vector3 p)
        {
            foreach(var edge in world.definition.graph.edges)for(int i=1;i<edge.samples.Count;i++)if(edge.edgeId!="boarding-deck"&&DistanceXZ(p,edge.samples[i-1].position,edge.samples[i].position)<Mathf.Max(edge.samples[i-1].roadWidthMeters,edge.samples[i].roadWidthMeters)*.5f+4)return true;
            foreach(var start in world.regions.SelectMany(r=>r.truckStarts))if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(start.position.x,start.position.z))<18)return true;
            foreach(var site in world.extractionSites)if(DistanceTo(site.secureZone.bounds,p)<6.25f)return true;return false;
        }
        public static UrbanBankProof LastMeasuredUrbanBank{get;private set;}
        static UrbanBankProof CreateMeasuredUrbanRetainingBanks(DragonKingdomWorld world,TileState[] tiles)
        {
            var proof=new UrbanBankProof{neverLowered=true,waterCoreExact=true,roadsAndPosesExact=true,seamsExact=true};var fields=CoherentSettlementFields(world);LastMeasuredUrbanBank=proof;var districts=new List<UrbanBankDistrict>();var terrainArray=tiles.Select(t=>t.terrain).ToArray();
            foreach(string id in new[]{"village","harbor"})
            {
                var points=new List<UrbanBankSample>();var origin=world.Region(id).root.position;
                for(int z=-140;z<=140;z+=10)for(int x=-180;x<=180;x+=10){var p=origin+new Vector3(x,0,z);points.Add(new UrbanBankSample{position=p,beforeGround=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrainArray,p),creekBankDistance=CreekBankDistance(p),originalDry=IntendedDrySettlementPositionAtBank(id,p,40),retainingDry=IntendedDrySettlementPositionAtBank(id,p,12),insideChangedMask=InsideUrbanBankMask(p,fields)});}
                districts.Add(new UrbanBankDistrict{region=id,points=points.ToArray(),minimumNewDryFreeboard=float.PositiveInfinity});
            }
            var rows=new List<UrbanBankTile>();
            foreach(var tile in tiles)
            {
                int n=tile.candidate.heightmapResolution;var before=tile.candidate.GetHeights(0,0,n,n);var desired=(float[,])before.Clone();var position=tile.terrain.transform.position;var size=tile.candidate.size;
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    var p=position+new Vector3(x*size.x/(n-1),0,z*size.z/(n-1));if(!InsideUrbanBankMask(p,fields)||UrbanRoadOrPoseProtected(world,p))continue;float old=before[z,x]*size.y+position.y;
                    float target=CoherentSettlementFloor(p,old,fields,out _,UrbanRetainingBank);desired[z,x]=(Mathf.Max(old,target)-position.y)/size.y;
                }
                tile.candidate.SetHeights(0,0,desired);tile.terrain.Flush();var after=tile.candidate.GetHeights(0,0,n,n);var beforeOutside=new List<float>();var afterOutside=new List<float>();var bankValues=new List<float>();var row=new UrbanBankTile{terrain=string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(tile.candidate))?UnityEditor.AssetDatabase.GetAssetPath(tile.original):UnityEditor.AssetDatabase.GetAssetPath(tile.candidate),origin=position,size=size,resolution=n,beforeHeightSha256=HeightArraySha(before),afterHeightSha256=HeightArraySha(after)};
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    var p=position+new Vector3(x*size.x/(n-1),0,z*size.z/(n-1));bool mask=InsideUrbanBankMask(p,fields),same=before[z,x]==after[z,x],protectedPose=mask&&UrbanRoadOrPoseProtected(world,p);float delta=(after[z,x]-before[z,x])*size.y;
                    if(!same){row.changedSamples++;proof.changedSamples++;proof.maximumRaise=Mathf.Max(proof.maximumRaise,delta);proof.neverLowered&=delta>=0;tile.fieldTags[z,x]=12;tile.fieldOwners[z,x]="Actual 12m urban retaining creek bank";}
                    if(!mask){beforeOutside.Add(before[z,x]);afterOutside.Add(after[z,x]);row.outsideSamples++;proof.outsideSamples++;if(!same)proof.outsideChangedSamples++;proof.maximumOutsideDelta=Mathf.Max(proof.maximumOutsideDelta,Mathf.Abs(delta));}else{row.bankSamples++;bankValues.Add(after[z,x]);}
                    if(HydrologyCore(p)){proof.coreSamples++;proof.waterCoreExact&=same;}if(protectedPose){proof.protectedRoadOrPoseSamples++;proof.roadsAndPosesExact&=same;}if(x==0||z==0||x==n-1||z==n-1){proof.seamSamples++;proof.seamsExact&=same;}
                }
                row.beforeOutsideSha256=FloatValuesSha(beforeOutside.ToArray());row.afterOutsideSha256=FloatValuesSha(afterOutside.ToArray());row.afterBankSha256=FloatValuesSha(bankValues.ToArray());rows.Add(row);
            }
            foreach(var district in districts)
            {
                foreach(var point in district.points)
                {point.afterGround=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrainArray,point.position);district.samples++;if(point.originalDry)district.originalDrySamples++;if(point.retainingDry)district.retainingDrySamples++;if(!point.originalDry&&point.retainingDry){district.newDrySamples++;district.minimumNewDryFreeboard=Mathf.Min(district.minimumNewDryFreeboard,point.afterGround+2);if(point.afterGround<-.5f)district.newDryStillUnderSea++;}}
                if(district.newDrySamples==0)district.minimumNewDryFreeboard=0;
            }
            proof.tiles=rows.ToArray();proof.districts=districts.ToArray();proof.outsideMaskExact=proof.outsideChangedSamples==0&&proof.maximumOutsideDelta==0&&rows.All(r=>r.beforeOutsideSha256==r.afterOutsideSha256);var village=districts.Single(d=>d.region=="village");var harbor=districts.Single(d=>d.region=="harbor");
            proof.passed=proof.changedSamples>0&&proof.maximumRaise<=20.05f&&proof.neverLowered&&proof.outsideMaskExact&&proof.waterCoreExact&&proof.roadsAndPosesExact&&proof.seamsExact&&village.samples==1073&&village.originalDrySamples==571&&village.retainingDrySamples==743&&village.newDrySamples==172&&village.newDryStillUnderSea==0&&village.minimumNewDryFreeboard>=1.5f&&harbor.samples==1073&&harbor.originalDrySamples==1073&&harbor.retainingDrySamples==1073;
            if(!proof.passed)throw new InvalidOperationException("Actual narrowed urban retaining bank failed unchanged outside/core/road/seam or172 physically dry additional sample checks: "+JsonUtility.ToJson(proof));return proof;
        }
        public static void ValidateSavedUrbanRetainingBanks(DragonKingdomWorld world,UrbanBankProof proof)
        {
            if(proof==null||!proof.passed||!proof.neverLowered||!proof.outsideMaskExact||!proof.waterCoreExact||!proof.roadsAndPosesExact||!proof.seamsExact||proof.outsideChangedSamples!=0||proof.maximumOutsideDelta!=0||proof.originalBank!=40||proof.retainingBank!=12||proof.creekCore!=34||proof.tiles.Length!=4||proof.districts.Length!=2)throw new InvalidOperationException("Exact native urban bank contract required.");var fields=CoherentSettlementFields(world);
            foreach(var row in proof.tiles)
            {
                var data=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>(row.terrain);var terrain=world.GetComponentsInChildren<Terrain>(true).Single(t=>t.terrainData==data);int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);var values=new List<float>();
                for(int z=0;z<n;z++)for(int x=0;x<n;x++){var p=terrain.transform.position+new Vector3(x*data.size.x/(n-1),0,z*data.size.z/(n-1));if(InsideUrbanBankMask(p,fields))values.Add(heights[z,x]);}
                if(values.Count!=row.bankSamples||FloatValuesSha(values.ToArray())!=row.afterBankSha256||row.beforeOutsideSha256!=row.afterOutsideSha256)throw new InvalidOperationException("Saved actual urban-bank region differs from native mask proof; later merchant berth may change only its independently proven disjoint footprint.");
            }
        }
    }
}
#endif
