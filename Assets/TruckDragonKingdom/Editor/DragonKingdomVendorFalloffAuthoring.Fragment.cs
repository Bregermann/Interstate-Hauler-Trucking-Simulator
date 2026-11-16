#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyRoads3Dv3;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomVendorFalloffAuthoring
    {
        [Serializable]public sealed class FragmentEvidence
        {
            public string source="ACTUAL_RETURNED_CAVERNS_VOLCANO_NATIVE_MARKER_SLICE_PUBLIC_CREATE_REFRESH_NO_SECOND_BUILD",originalRoad,temporaryRoad,sourceTypeId;
            public int nearestOriginalSegment,firstMarker,lastMarker,actualComponentId;
            public int[] originalMarkerIndices;public Vector3[] originalMarkerPositions,selectedMarkerPositions,actualMarkerPositions;
            public float originalRoadWidth,fragmentRoadWidth,indent,surrounding;
            public Bounds generatedContourBounds,declaredToolFootprint;
            public bool sourceSliceExact,controlsExact,sourceIntervalsNotExcluded,creationHeightfieldsExact,originalRoadAuthoritiesExact,originalTypeRegistryExact,footprintExcludesOriginalRoof,temporaryRoadRemoved;
            public string[] beforeCreationHeightSha256,afterCreationHeightSha256;
        }
        sealed class FragmentState
        {public ERRoad fragment;public Dictionary<ERModularRoad,string> originals;public string[] registry;}
        static string RoadJsonSha(ERModularRoad road)=>Sha(Encoding.UTF8.GetBytes(JsonUtility.ToJson(road)));
        static float SegmentDistanceXZ(Vector3 p,Vector3 a,Vector3 b)
        {var d=new Vector2(b.x-a.x,b.z-a.z);var v=new Vector2(p.x-a.x,p.z-a.z);float t=d.sqrMagnitude==0?0:Mathf.Clamp01(Vector2.Dot(v,d)/d.sqrMagnitude);return (v-d*t).sqrMagnitude;}
        static FragmentState CreateMeasuredNeFragment(ERRoadNetwork network,ERModularBase owner,Terrain[] terrains,IReadOnlyList<ERRoad> returned,float distance,Bounds? roof,Evidence evidence)
        {
            if(!roof.HasValue)throw new InvalidOperationException("Fragment profile must declare the original pre-SDK cavern roof before any temporary road or tool.");
            var original=returned.Single(r=>r.gameObject.name=="caverns-volcano");var positions=original.GetMarkerPositions();var type=original.GetRoadType();
            if(type==null||original.roadScript==null||positions.Length<4||type.roadWidth!=original.roadScript.roadWidth)throw new InvalidOperationException("Use this exact existing native main road, public type and marker geometry.");
            var state=new FragmentState{originals=returned.ToDictionary(r=>r.roadScript,r=>RoadJsonSha(r.roadScript)),registry=owner.roadTypes.Select(t=>JsonUtility.ToJson(t)).ToArray()};
            int nearest=Enumerable.Range(0,positions.Length-1).OrderBy(i=>SegmentDistanceXZ(NeFailureCellReference,positions[i],positions[i+1])).ThenBy(i=>i).First();
            int first=Math.Max(0,nearest-2),last=Math.Min(positions.Length-1,nearest+2);var points=positions.Skip(first).Take(last-first+1).ToArray();
            var row=new FragmentEvidence{originalRoad=original.gameObject.name,sourceTypeId=type.id.ToString("R",System.Globalization.CultureInfo.InvariantCulture),nearestOriginalSegment=nearest,firstMarker=first,lastMarker=last,originalMarkerPositions=positions,originalMarkerIndices=Enumerable.Range(first,last-first+1).ToArray(),selectedMarkerPositions=points,originalRoadWidth=original.roadScript.roadWidth,beforeCreationHeightSha256=terrains.Select(t=>HeightSha(t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution))).ToArray()};evidence.fragment=row;
            try
            {
                var controls=Enumerable.Range(first,points.Length).Select(original.GetMarkerControlType).ToArray();if(controls.Any(c=>c!=ERMarkerControlType.StraightXZY))throw new InvalidOperationException("Measured source fragment must retain original straight XYZ controls.");
                var sourceRoad=original.roadScript;row.sourceIntervalsNotExcluded=sourceRoad.markerInts.Count==sourceRoad.bridgeElement.Count&&Enumerable.Range(0,sourceRoad.markerInts.Count).Where(i=>sourceRoad.markerInts[i]>=first&&sourceRoad.markerInts[i]<last).All(i=>!sourceRoad.bridgeElement[i]);if(!row.sourceIntervalsNotExcluded)throw new InvalidOperationException("Never turn an original excluded bridge/underground interval into a graded fragment.");
                state.fragment=network.CreateRoad("Temporary exact NE source marker fragment",type,points,controls);state.fragment.SetTerrainDeformation(true,false);state.fragment.SnapToTerrain(false,false);state.fragment.SetResolution(.35f,false);
                for(int i=0;i<points.Length;i++)if(state.fragment.SetIndent(28,i)!=28||state.fragment.SetSurrounding(distance,i)!=distance)throw new InvalidOperationException("Temporary fragment marker settings must retain original28/110 policy.");
                var actual=DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(state.fragment,type,owner,28,distance);state.fragment.Refresh();row.temporaryRoad=state.fragment.gameObject.name;row.actualComponentId=actual.GetInstanceID();row.actualMarkerPositions=state.fragment.GetMarkerPositions();row.sourceSliceExact=row.actualMarkerPositions.SequenceEqual(points);row.controlsExact=Enumerable.Range(0,points.Length).All(i=>state.fragment.GetMarkerControlType(i)==controls[i]);row.fragmentRoadWidth=actual.roadWidth;row.indent=actual.indent;row.surrounding=actual.surrounding;row.generatedContourBounds=Contours(actual);var footprint=row.generatedContourBounds;footprint.Expand(new Vector3(distance*2,0,distance*2));row.declaredToolFootprint=footprint;
                row.footprintExcludesOriginalRoof=!IntersectsXZ(footprint,roof.Value)&&terrains.All(t=>!IntersectsXZ(CoverTerrainGridCells(footprint,t.transform.position,new Vector3(t.terrainData.size.x/(t.terrainData.heightmapResolution-1),0,t.terrainData.size.z/(t.terrainData.heightmapResolution-1))),roof.Value));
                row.afterCreationHeightSha256=terrains.Select(t=>HeightSha(t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution))).ToArray();row.creationHeightfieldsExact=row.beforeCreationHeightSha256.SequenceEqual(row.afterCreationHeightSha256);row.originalRoadAuthoritiesExact=state.originals.All(p=>p.Key!=null&&RoadJsonSha(p.Key)==p.Value);row.originalTypeRegistryExact=owner.roadTypes.Select(t=>JsonUtility.ToJson(t)).SequenceEqual(state.registry);
                if(!row.sourceSliceExact||!row.controlsExact||row.fragmentRoadWidth!=row.originalRoadWidth||!row.creationHeightfieldsExact||!row.originalRoadAuthoritiesExact||!row.originalTypeRegistryExact||!row.footprintExcludesOriginalRoof)throw new InvalidOperationException("Temporary source-derived fragment changed original authorities/heights or its actual110m tool footprint reaches protected cavern roof.");
                return state;
            }
            catch{var temporary=state.fragment==null?null:state.fragment.gameObject;if(temporary!=null)UnityEngine.Object.DestroyImmediate(temporary);row.temporaryRoadRemoved=temporary==null;throw;}
        }
        static void CloseMeasuredNeFragment(FragmentState state,ERModularBase owner,Evidence evidence)
        {
            var row=evidence.fragment;row.originalRoadAuthoritiesExact&=state.originals.All(p=>p.Key!=null&&RoadJsonSha(p.Key)==p.Value);row.originalTypeRegistryExact&=owner.roadTypes.Select(t=>JsonUtility.ToJson(t)).SequenceEqual(state.registry);var temporary=state.fragment==null?null:state.fragment.gameObject;if(temporary!=null)UnityEngine.Object.DestroyImmediate(temporary);row.temporaryRoadRemoved=temporary==null;
        }
    }
}
#endif
