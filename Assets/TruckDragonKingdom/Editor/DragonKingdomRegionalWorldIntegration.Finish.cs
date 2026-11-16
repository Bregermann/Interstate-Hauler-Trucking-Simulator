#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class LandmarkFinish
        {
            public string scope="EXPLICIT_CANDIDATE_ONLY_REAL_OWNED_LANDMARK_COMPOSITION_REQUIRES_NATIVE_PHYSICAL_AND_NINE_REGION_IMAGE_REVIEW";
            public int newStreetfronts,refugeHouses,marketStalls,farmPieces,harborProps,smallMooredBoats,underbrush,mushrooms,cavernCrystals,basalt,alpineRocks,nestRoostTrees,lanterns;
            public RidgeComposition ridgeComposition;public AssemblyMaterialEvidence assemblyMaterials;public AuthoredGroup[] authoredGroups;public HarborForecourt harborForecourt;public HarborCargo harborCargo;public RetainingBankArt retainingBank;public WaterFinish waterways;public ArtistMarket artistMarket;public MooredShip mooredShip;public int traderShips,compositionSchema;
            public List<FinishPlacementFailure> failures=new List<FinishPlacementFailure>();
            public bool complete,singleSourceLightPerLantern;public string[] sourcePrefabs;
            [NonSerialized]public HashSet<string> sources=new HashSet<string>();
        }
        [Serializable]public sealed class FinishPlacementFailure
        {public string region,name,source;public Vector3 hint,lastCandidate;public Bounds lastBounds;public int supportRejects,slopeRejects,roadRejects,overlapRejects,additionalClearanceRejects;public float lastHeightRange,lastSlope;}
        static LandmarkFinish AddOwnedStreetfronts(DragonKingdomWorld world,string folder,List<Placement> records)
        {
            Directory.CreateDirectory(folder+"/Materials/Finish");AssetDatabase.Refresh();var result=new LandmarkFinish{compositionSchema=3};
            int[] styles={4,6,8,9,11,13};var village=world.Region("village");
            Vector3[] streetfronts={new Vector3(-110,0,10),new Vector3(-90,0,-75),new Vector3(-15,0,-100),new Vector3(65,0,-90),new Vector3(105,0,15),new Vector3(70,0,110)};
            for(int i=0;i<streetfronts.Length;i++)
            {
                var t=FinishPlace(world,folder,village,"buildings/P_BLD_house_"+styles[i].ToString("00")+".prefab",village.root.position+streetfronts[i],18+i%3*2,180-i*60,true,0,result,records,"P_BLD_house_"+styles[i].ToString("00")+" finished streetfront "+(i+1),true);result.newStreetfronts++;
            }
            var harbor=world.Region("harbor");Vector3[] harborFronts={new Vector3(45,0,-130),new Vector3(175,0,-125),new Vector3(45,0,-90),new Vector3(180,0,-85)};
            for(int i=0;i<harborFronts.Length;i++)
            {FinishPlace(world,folder,harbor,"buildings/P_BLD_house_"+styles[i+1].ToString("00")+".prefab",harbor.root.position+harborFronts[i],19+i%2*3,i<2?180:0,true,0,result,records,"P_BLD_house_"+styles[i+1].ToString("00")+" harbor merchant "+(i+1),true);result.newStreetfronts++;}
            var refuge=world.Region("refuge");
            for(int i=0;i<4;i++)
            {var p=refuge.root.position+new Vector3(i%2==0?-80:80,0,i<2?15:155);FinishPlace(world,folder,refuge,"buildings/P_BLD_house_"+styles[i+1].ToString("00")+".prefab",p,21,i%2==0?90:270,true,0,result,records,"P_BLD_house_"+styles[i+1].ToString("00")+" mountain refuge lodge "+(i+1),true);result.refugeHouses++;}
            return result;
        }
        static void FinishOwnedLandmarks(DragonKingdomWorld world,string folder,LandmarkFinish result,List<Placement> records)
        {
            var village=world.Region("village");
            for(int i=0;i<6;i++)
            {var p=village.root.position+new Vector3(-85+(i%3)*19,0,-55-(i/3)*20);FinishPlace(world,folder,village,"props/P_PROP_market_v0"+(i%4+1)+"_01.prefab",p,5.8f+(i%2)*.7f,i<3?15:195,true,1,result,records,"Owned market stall "+(i+1));result.marketStalls++;}
            string[] farm={"P_PROP_hay_01","P_PROP_hay_02","P_PROP_fence_v01_01","P_PROP_fence_v01_03","P_PROP_trough_01","P_PROP_cart_02","P_PROP_well","P_PROP_sack_01"};
            // V436's actual graded fields leave the old southern hints below
            // the dry village apron. Keep the farm in the connected northwest
            // yard; source models, counts and every placement guard stay intact.
            for(int i=0;i<24;i++)
            {var p=village.root.position+new Vector3(-170+(i%6)*13,0,55-(i/6)*15);string name=farm[i%farm.Length];float height=name.Contains("fence")?2.2f:name.Contains("well")?5:name.Contains("cart")?3.5f:1.8f;FinishPlace(world,folder,village,"props/"+name+".prefab",p,height,i*35,true,1.2f,result,records,"Owned farmyard "+(i+1));result.farmPieces++;}
            var harbor=world.Region("harbor");string[] quay={"P_PROP_crate_01","P_PROP_barrel_01","P_PROP_cart_02","P_PROP_sack_01"};
            for(int i=0;i<16;i++)
            {var p=new Vector3(i<8?654:790,0,-584-(i%8)*5);FinishPlace(world,folder,harbor,"props/"+quay[i%quay.Length]+".prefab",p,i%4==2?3:1.8f,i*25,true,1,result,records,"Owned quay freight "+(i+1));result.harborProps++;}
            for(int i=0;i<2;i++)
            {FinishPlace(world,folder,harbor,"props/P_PROP_rowboat.prefab",new Vector3(785+i*25,-3.1f,-730-i*24),1.9f,160+i*25,true,-1,result,records,"Actual small moored rowboat "+(i+1));result.smallMooredBoats++;}
            var forest=world.Region("forest");string[] brush={"P_ENV_bush_ancient_01","P_ENV_bush_ancient_02","P_ENV_bush_ancient_03","P_ENV_grass_ancient_01","P_ENV_grass_ancient_02","P_ENV_plant_ancient_03_01","P_ENV_plant_ancient_06","P_ENV_plant_ancient_08","P_ENV_plant_ancient_10"};
            for(int i=0;i<140;i++)
            {float angle=i*2.399963f,radius=48+Mathf.Sqrt(i/139f)*220;var p=forest.root.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius*.78f);string source=DragonKingdomWorldAuthoring.Forest+"Plants/"+brush[i%brush.Length]+".prefab";float height=i%9<3?2.4f+i%3*.5f:i%9<5?.85f:1.4f;FinishPlace(world,folder,forest,source,p,height,i*137,false,.8f,result,records,"Owned forest underbrush "+(i+1));result.underbrush++;}
            for(int i=0;i<36;i++)
            {float angle=i*2.399963f,radius=38+Mathf.Sqrt(i/35f)*190;var p=forest.root.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius*.8f);string source=DragonKingdomWorldAuthoring.Forest+"Mushrooms/P_ENV_mushrooms_ancient_0"+(i%3+1)+"_01.prefab";FinishPlace(world,folder,forest,source,p,1.3f+i%4*.35f,i*130,false,.8f,result,records,"Owned mushroom colony "+(i+1));result.mushrooms++;}
            InstallOwnedCavernCrystals(world,folder,result,records);
            var volcano=world.Region("volcano");
            for(int i=0;i<12;i++)
            {float angle=i*Mathf.PI*2/12,radius=130+(i%3)*24;var p=volcano.root.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);string source=DragonKingdomWorldAuthoring.Art+"Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Cliffs/RockFormation_0"+(i%4+1)+".prefab";FinishPlace(world,folder,volcano,source,p,12+i%4*3,i*47,true,30,result,records,"Owned natural basalt outcrop "+(i+1));result.basalt++;}
            for(int i=0;i<12;i++)
            {float a=i*Mathf.PI*2/12;var p=new Vector3(1370+Mathf.Cos(a)*75,0,1050+Mathf.Sin(a)*70);FinishPlace(world,folder,volcano,DragonKingdomWorldAuthoring.Cliff+"RockFormation_0"+(i%4+1)+".prefab",p,18+i%4*4,i*39,true,45,result,records,"Owned natural basalt source-rim "+(i+1));result.basalt++;}
            foreach(string regionId in new[]{"mountains","refuge","nests"})
            for(int i=0;i<8;i++)
            {var region=world.Region(regionId);float angle=(i+.3f)*Mathf.PI*2/8;var p=region.root.position+new Vector3(Mathf.Cos(angle)*(85+i%3*25),0,Mathf.Sin(angle)*(105+i%2*35));string source=DragonKingdomWorldAuthoring.Cliff+"RockFormation_0"+(i%4+1)+".prefab";FinishPlace(world,folder,region,source,p,8+i%4*3,i*67,true,30,result,records,"Owned alpine shelter geology "+regionId+" "+(i+1));result.alpineRocks++;}
            for(int i=0;i<8;i++)
            {var nest=world.Region("nests");float a=i*Mathf.PI*2/8;FinishPlace(world,folder,nest,DragonKingdomWorldAuthoring.Forest+"Trees/P_ENV_tree_ancient_0"+(i%3+1)+".prefab",nest.root.position+new Vector3(Mathf.Cos(a)*130,0,Mathf.Sin(a)*120),14+i%3*2,i*80,true,8,result,records,"Owned living cliff roost tree "+(i+1));result.nestRoostTrees++;}
            result.artistMarket=InstallArtistMarket(world,folder,result,records);
            result.mooredShip=InstallMooredTraderShip(world,folder,result,records);result.traderShips=1;
            InstallOwnedLanterns(world,folder,result,records);
            result.harborForecourt=CreateSupportedHarborQuayside(world,folder,result,records);RecomposeActualQuayDecorations(world,result.harborForecourt,records);
            result.harborCargo=CreateSupportedTraderCargo(world,folder,result,records);
            result.waterways=FinishActualWaterways(world,folder);
            result.sourcePrefabs=result.sources.OrderBy(p=>p).ToArray();result.complete=result.newStreetfronts==10&&result.refugeHouses==4&&result.marketStalls==6&&result.farmPieces==24&&result.harborProps==16&&result.smallMooredBoats==2&&result.underbrush==140&&result.mushrooms==36&&result.cavernCrystals==12&&result.basalt==24&&result.alpineRocks==24&&result.nestRoostTrees==8&&result.lanterns==28&&result.traderShips==1&&result.artistMarket.passed&&result.mooredShip.passed&&result.waterways.actualBedSamples>100;
            if(!result.complete)throw new InvalidOperationException("Candidate landmark composition must retain every planned actual asset, never silently omit rejected placements.");
        }
        static Transform FinishPlace(DragonKingdomWorld world,string folder,DragonWorldRegion region,string source,Vector3 hint,float height,float yaw,bool solid,float allowedRange,LandmarkFinish evidence,List<Placement> records,string name,bool foundationPlanned=false,bool underground=false,Func<Bounds,bool> additionalClearance=null)
        {
            if(!source.StartsWith("Assets/",StringComparison.Ordinal))source=DragonKingdomWorldAuthoring.Village+source;
            var t=DragonKingdomWorldAuthoring.PlaceOwnedArtForReview(region.root,source,hint,yaw,height,solid);t.name=name;ConfigureFinishMaterials(t,folder);var owned=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));
            var failed=new FinishPlacementFailure{region=region.id,name=name,source=source,hint=hint};bool vegetation=!solid&&region.id=="forest";var terrainSamples=world.GetComponentsInChildren<Terrain>(true);
            var initialBounds=BoundsOf(t);Vector3 rootContact=t.InverseTransformPoint(new Vector3(initialBounds.center.x,initialBounds.min.y,initialBounds.center.z));
            // The original sparse spiral can miss the remaining dry parcels
            // between complete stalls after native terrain grading. Try its
            // exact original prefix first, then sample the SAME bounded area
            // more densely. Every support/water/road/collider guard below still
            // applies; no terrain or source art is changed to force a placement.
            bool denseSettlement=solid&&!foundationPlanned&&!underground&&(region.id=="village"||region.id=="harbor");
            for(int trial=0;trial<(underground?64:denseSettlement?2336:288);trial++)
            {
                var p=hint+FinishPlacementOffset(trial,solid,denseSettlement);var b=BoundsOf(t);
                if(name.StartsWith("Owned quay freight ",StringComparison.Ordinal)&&(p.x<645||p.x>805||p.z< -622||p.z> -570)){failed.supportRejects++;continue;}
                float y=p.y;
                Vector3 normal=Vector3.up;
                if(allowedRange>=0&&!foundationPlanned)
                {
                    float half=Mathf.Clamp(Mathf.Min(b.size.x,b.size.z)*.35f,.25f,solid?12:1.5f),range;
                    if(vegetation)
                    {
                        var terrain=world.GetComponentsInChildren<Terrain>(true).FirstOrDefault(item=>InsideXZ(new Bounds(item.transform.position+item.terrainData.size*.5f,item.terrainData.size),p));
                        if(terrain==null){failed.supportRejects++;continue;}var local=p-terrain.transform.position;normal=terrain.transform.TransformDirection(terrain.terrainData.GetInterpolatedNormal(local.x/terrain.terrainData.size.x,local.z/terrain.terrainData.size.z));
                        failed.lastSlope=Vector3.Angle(Vector3.up,normal);if(failed.lastSlope>50){failed.slopeRejects++;continue;}y=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,p);
                    }
                    else if(underground)
                    {var hits=Physics.RaycastAll(new Vector3(p.x,115,p.z),Vector3.down,16,~0,QueryTriggerInteraction.Ignore).Where(h=>!owned.Contains(h.collider)&&h.normal.y>.4f&&Mathf.Abs(h.point.y-105)<.3f).OrderByDescending(h=>h.point.y).ToArray();if(hits.Length==0){failed.supportRejects++;continue;}y=hits[0].point.y;}
                    else{if(!TryGround(world,p,half,out y,out range)||range>allowedRange){failed.lastHeightRange=range;failed.supportRejects++;continue;}if(name.Contains("basalt")||name.Contains("alpine shelter"))y-=range+height*.15f;}
                }
                else if(foundationPlanned)y=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,p);
                if(vegetation)
                {t.rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,normal,.65f))*Quaternion.Euler(0,yaw,0);t.position=new Vector3(p.x,y-.035f,p.z)-t.TransformVector(rootContact);}
                else AlignBase(t,new Vector3(p.x,y-.04f,p.z));Physics.SyncTransforms();failed.lastCandidate=t.position;failed.lastBounds=BoundsOf(t);
                if(!foundationPlanned&&allowedRange>=0&&(region.id=="village"||region.id=="harbor")&&!SettlementArtHasActualDryFooting(terrainSamples,region.id,t)){failed.supportRejects++;continue;}
                if(solid&&!ClearRoadEnvelope(world,t,false)||!FinishBoundsClearOfRoads(world,BoundsOf(t))){failed.roadRejects++;continue;}
                var actual=BoundsOf(t);if(additionalClearance!=null&&!additionalClearance(actual)){failed.additionalClearanceRejects++;continue;}bool overlap=Physics.OverlapBox(actual.center+Vector3.up*.25f,actual.extents*.85f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>!owned.Contains(c)&&!(c is TerrainCollider)&&!c.name.StartsWith("Chunk",StringComparison.Ordinal)&&c.transform.IsChildOf(world.transform));
                if(overlap){failed.overlapRejects++;continue;}
                evidence.sources.Add(source);records.Add(new Placement{region=region.id,name=name,source=source,operation="Add complete owned art with actual material/collision, measured support and clear road/start buffers",before=hint,after=t.position,bounds=actual,trial=trial});return t;
            }
            evidence.failures.Add(failed);UnityEngine.Object.DestroyImmediate(t.gameObject);throw new InvalidOperationException("No bounded supported clear placement for required actual finish art "+region.id+"/"+name+"; support="+failed.supportRejects+", slope="+failed.slopeRejects+", road/objective="+failed.roadRejects+", other-art="+failed.overlapRejects+". Exact source/last contact in finish.failures receipt.");
        }
        static Vector3 FinishPlacementOffset(int trial,bool solid,bool denseSettlement)
        {
            if(trial<0||trial>=(denseSettlement?2336:288))throw new ArgumentOutOfRangeException(nameof(trial));
            float radius=trial<288?(trial==0?0:(solid?7:3)*Mathf.Sqrt(trial)):7*Mathf.Sqrt(287)*Mathf.Sqrt((trial-288+.5f)/2048);
            float angle=trial*2.399963f;return new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
        }
        static bool FinishBoundsClearOfRoads(DragonKingdomWorld world,Bounds b,Action<AuthoredClearanceWitness> blocked=null)
        {
            foreach(var edge in world.definition.graph.edges)foreach(var sample in edge.samples)
            {if(sample.position.y+8<b.min.y||sample.position.y>b.max.y)continue;float x=Mathf.Max(0,Mathf.Max(b.min.x-sample.position.x,sample.position.x-b.max.x)),z=Mathf.Max(0,Mathf.Max(b.min.z-sample.position.z,sample.position.z-b.max.z));if(Mathf.Sqrt(x*x+z*z)<sample.roadWidthMeters*.5f+5){blocked?.Invoke(new AuthoredClearanceWitness{kind="ROAD_GRAPH_ENVELOPE",path=edge.edgeId,point=sample.position,margin=sample.roadWidthMeters*.5f+5,distance=Mathf.Sqrt(x*x+z*z)});return false;}}
            foreach(var start in world.regions.SelectMany(r=>r.truckStarts))if(DistanceTo(b,start.position)<12&&start.position.y+6>b.min.y&&start.position.y-3<b.max.y){blocked?.Invoke(new AuthoredClearanceWitness{kind="ORIGINAL_TRUCK_START",path=PathOf(start,world.transform),point=start.position,margin=12,distance=DistanceTo(b,start.position)});return false;}
            foreach(var anchor in world.regions.SelectMany(r=>r.nests.Concat(r.treasureCaches).Concat(r.perches)))if(DistanceTo(b,anchor.position)<14&&anchor.position.y+10>b.min.y&&anchor.position.y-3<b.max.y){blocked?.Invoke(new AuthoredClearanceWitness{kind="ORIGINAL_NEST_CACHE_OR_PERCH",path=PathOf(anchor,world.transform),point=anchor.position,margin=14,distance=DistanceTo(b,anchor.position)});return false;}
            foreach(var site in world.extractionSites)if(DistanceTo(b,site.approach.position)<12&&site.approach.position.y+8>b.min.y&&site.approach.position.y-3<b.max.y||b.Intersects(site.secureZone.bounds)){blocked?.Invoke(new AuthoredClearanceWitness{kind=b.Intersects(site.secureZone.bounds)?"ORIGINAL_EXTRACTION_ZONE":"ORIGINAL_EXTRACTION_APPROACH",path=site.id,point=site.approach.position,bounds=site.secureZone.bounds,margin=12,distance=DistanceTo(b,site.approach.position)});return false;}
            return true;
        }
        static void ConfigureFinishMaterials(Transform t,string folder)
        {
            foreach(var r in t.GetComponentsInChildren<Renderer>(true))
            {
                var values=r.sharedMaterials;
                for(int i=0;i<values.Length;i++)
                {
                    var m=values[i];if(m==null)throw new InvalidOperationException("Actual finish art has a missing material.");if(DragonKingdomWorldVisualMigration.HasSupportedUrpShader(m))continue;
                    string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));if(guid.Length!=32)throw new InvalidOperationException("Persistent owned material required.");string path=folder+"/Materials/Finish/"+guid+" URP.mat";
                    values[i]=DragonKingdomWorldVisualMigration.HasAuthoredAlbedo(m)?DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(m,path,false,out _):PreserveLegacySolidColor(m,path);
                }
                r.sharedMaterials=values;
            }
        }
    }
}
#endif
