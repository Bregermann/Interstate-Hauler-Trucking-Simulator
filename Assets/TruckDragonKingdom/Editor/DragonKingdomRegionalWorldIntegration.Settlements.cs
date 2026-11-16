#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        sealed class SettlementField{public string region;public Bounds bounds;public float footing;}
        [Serializable]public sealed class SettlementDryLand
        {
            public string region,scope="ACTUAL_CONNECTED_SETTLEMENT_FOOTPRINT_CLEAR_OF_COASTAL_WATER_NOT_GENERAL_TERRAIN_ACCEPTANCE";
            public int samples,underSea,declaredWaterOrBankSamples,dryYardSamples,unexpectedUnderSea,buildings,buildingFootSamples,floodedBuildingFootSamples,dryDecorations,decorationFootSamples,floodedDecorationFootSamples,deckSupportedDecorations,deckFootSamples;public float seaLevel,minimumGround,minimumFreeboard,minimumBuildingFreeboard,minimumDecorationFreeboard;public Vector3 lowestPosition;public bool passed;
        }
        static SettlementField[] CoherentSettlementFields(DragonKingdomWorld world)
        {
            var village=world.Region("village");var harbor=world.Region("harbor");
            // These are the authored village streetscape and harbor merchant
            // district, not arbitrary half-world fills. House/road pads alone
            // let their connecting yards fall to the old−20m bed (V192).
            return new[]{
                new SettlementField{region="village",bounds=new Bounds(village.root.position,new Vector3(430,0,330)),footing=village.root.position.y-.08f},
                new SettlementField{region="harbor",bounds=new Bounds(harbor.root.position+Vector3.forward*10,new Vector3(450,0,320)),footing=harbor.root.position.y-.08f}
            };
        }
        static float CreekBankDistance(Vector3 p)
        {float distance=float.PositiveInfinity;for(int i=1;i<FieldCreek.Length;i++)distance=Mathf.Min(distance,Mathf.Max(0,DistanceXZ(p,FieldCreek[i-1],FieldCreek[i])-34));return distance;}
        static bool IntendedDrySettlementPosition(string region,Vector3 p)=>IntendedDrySettlementPositionAtBank(region,p,UrbanRetainingBank);
        static bool BuildingClearOfActualWatercourse(string region,Transform building)
        {
            var b=BoundsOf(building);
            foreach(float z in new[]{b.min.z,b.center.z,b.max.z})foreach(float x in new[]{b.min.x,b.center.x,b.max.x})if(!IntendedDrySettlementPosition(region,new Vector3(x,0,z)))return false;
            return true;
        }
        static bool SettlementArtHasActualDryFooting(Terrain[] terrains,string region,Transform art,float seaLevel=-2)
        {
            var b=BoundsOf(art);
            foreach(float z in new[]{b.min.z,b.center.z,b.max.z})foreach(float x in new[]{b.min.x,b.center.x,b.max.x})
            {var p=new Vector3(x,0,z);if(!IntendedDrySettlementPosition(region,p)||DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p)<seaLevel+1.5f)return false;}
            return true;
        }
        static bool IsDrySettlementDecoration(string name)=>name.StartsWith("Owned market stall ",StringComparison.Ordinal)||name.StartsWith("Owned farmyard ",StringComparison.Ordinal)||name.StartsWith("Owned quay freight ",StringComparison.Ordinal)||name.StartsWith("Owned working lantern village ",StringComparison.Ordinal)||name.StartsWith("Owned working lantern harbor ",StringComparison.Ordinal)||name.StartsWith("P_PROP_cart_01",StringComparison.Ordinal);
        static float CoherentSettlementFloor(Vector3 p,float existingHeight,SettlementField[] fields,out string owner,float creekTransition=40)
        {
            owner=null;
            // Original lake/boat bed, actual creek and volcanic channel keep
            // their hydrology authority. Their geometry is never filled in.
            if(HydrologyCore(p))return float.NegativeInfinity;
            float floor=float.NegativeInfinity;
            foreach(var field in fields)
            {
                float distance=DistanceTo(field.bounds,p);if(distance>120)continue;
                float candidate=field.footing-distance*.2f;
                // The merchant land tapers to the original harbor shoreline;
                // the boarding deck/hull beyond−650m remains genuinely water.
                if(field.region=="harbor")candidate=Mathf.Min(candidate,field.footing+Mathf.Min(0,p.z+625)*.8f);
                // Retain the real creek passing through the village and its
                // continuous40m native bank transition. Raising every old
                // water sample to a dry plane would destroy that watercourse.
                float creek=CreekBankDistance(p);if(creek<creekTransition)candidate=Mathf.Lerp(existingHeight,candidate,Mathf.SmoothStep(0,1,creek/creekTransition));
                if(candidate>floor){floor=candidate;owner=field.region+" connected dry streetscape apron";}
            }
            return floor;
        }
        static SettlementDryLand[] MeasureCoherentSettlementDryLand(DragonKingdomWorld world,float seaLevel)
        {
            var records=new List<SettlementDryLand>();var terrains=world.GetComponentsInChildren<Terrain>(true);
            foreach(string id in new[]{"village","harbor"})
            {
                var region=world.Region(id);var record=new SettlementDryLand{region=id,seaLevel=seaLevel,minimumGround=float.PositiveInfinity,minimumFreeboard=float.PositiveInfinity,minimumBuildingFreeboard=float.PositiveInfinity,minimumDecorationFreeboard=float.PositiveInfinity};
                // Same actual1073ground samples per district as the failed
                // V192 water diagnosis; no redefined smaller acceptance area.
                for(int z=-140;z<=140;z+=10)for(int x=-180;x<=180;x+=10)
                {
                    var p=region.root.position+new Vector3(x,0,z);float ground=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p);record.samples++;
                    if(ground<seaLevel-.05f)record.underSea++;
                    bool waterOrBank=!IntendedDrySettlementPosition(id,p);
                    if(waterOrBank)record.declaredWaterOrBankSamples++;
                    else{record.dryYardSamples++;if(ground<seaLevel-.05f)record.unexpectedUnderSea++;record.minimumFreeboard=Mathf.Min(record.minimumFreeboard,ground-seaLevel);}
                    if(ground<record.minimumGround){record.minimumGround=ground;record.lowestPosition=p;}
                }
                foreach(Transform building in region.root)
                {
                    if(!building.name.StartsWith("P_BLD_house",StringComparison.Ordinal)&&building.name!="Complete owned windmill house14")continue;
                    var b=BoundsOf(building);record.buildings++;
                    foreach(float z in new[]{b.min.z,b.center.z,b.max.z})foreach(float x in new[]{b.min.x,b.center.x,b.max.x})
                    {var p=new Vector3(x,0,z);float ground=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p);record.buildingFootSamples++;record.minimumBuildingFreeboard=Mathf.Min(record.minimumBuildingFreeboard,ground-seaLevel);if(!IntendedDrySettlementPosition(id,p)||ground<seaLevel+1.5f)record.floodedBuildingFootSamples++;}
                }
                foreach(Transform decoration in region.root)
                {
                    if(!IsDrySettlementDecoration(decoration.name))continue;
                    var quay=id=="harbor"?region.root.Cast<Transform>().SingleOrDefault(t=>t.name==HarborQuaysideName):null;
                    if(quay!=null&&(decoration.name.StartsWith("Owned quay freight ",StringComparison.Ordinal)||decoration.name.StartsWith("Owned working lantern harbor ",StringComparison.Ordinal)))
                    {var support=MeasureActualQuayArtSupport(world,decoration,quay,0);record.deckSupportedDecorations++;record.deckFootSamples+=support.Length;continue;}
                    record.dryDecorations++;var b=BoundsOf(decoration);
                    foreach(float z in new[]{b.min.z,b.center.z,b.max.z})foreach(float x in new[]{b.min.x,b.center.x,b.max.x})
                    {var p=new Vector3(x,0,z);float ground=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(terrains,p);record.decorationFootSamples++;record.minimumDecorationFreeboard=Mathf.Min(record.minimumDecorationFreeboard,ground-seaLevel);if(!IntendedDrySettlementPosition(id,p)||ground<seaLevel+1.5f)record.floodedDecorationFootSamples++;}
                }
                bool supportedQuay=id=="harbor"&&region.root.Cast<Transform>().Any(t=>t.name==HarborQuaysideName);int expectedDry=id=="village"?743:1073,expectedBuildings=id=="village"?13:8,expectedDecorations=id=="village"?44:supportedQuay?0:22,expectedDeck=supportedQuay?22:0;
                if(record.dryDecorations==0)record.minimumDecorationFreeboard=0;
                record.passed=record.samples==1073&&record.dryYardSamples==expectedDry&&record.unexpectedUnderSea==0&&record.minimumFreeboard>=1.5f&&record.buildings==expectedBuildings&&record.buildingFootSamples==expectedBuildings*9&&record.floodedBuildingFootSamples==0&&record.dryDecorations==expectedDecorations&&record.decorationFootSamples==expectedDecorations*9&&record.floodedDecorationFootSamples==0&&record.deckSupportedDecorations==expectedDeck&&record.deckFootSamples==expectedDeck*9;
                records.Add(record);
                if(!record.passed)throw new InvalidOperationException("Actual coherent settlement ground remains submerged or lacks a dry shoreline margin: "+JsonUtility.ToJson(record));
            }
            return records.ToArray();
        }
    }
}
#endif
