#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Optional read-only measurement of actual PUBLIC vendor-generated contour
    // arrays. No terrain/road setting, Build call or source mutation is made.
    public sealed class DragonKingdomNativeGradeFootprintObservation
    {
        [Serializable]public sealed class Tile{public string name,heightSha256;public Vector3 origin;}
        [Serializable]public sealed class Point{public int index;public Vector3 position;}
        [Serializable]public sealed class Focus{public Vector3 position;}
        [Serializable]public sealed class Series
        {public string field,actualCollectionType,firstElementType,allVectorCoordinatesSha256;public int elements,vectors,nonFiniteVectors,nearPointsOmitted;public float nearestFocusDistance=float.PositiveInfinity;public bool hasVectorBounds;public Bounds bounds;public Point[] nearPoints;}
        [Serializable]public sealed class Road
        {public int componentId;public string name,typeName,typeId,stage;public float indent,surrounding;public bool terrainDeformation;public bool[] leftSurrounding,rightSurrounding;public Series[] contours;}
        [Serializable]public sealed class Event
        {public string stage,road;public Tile[] tiles;public Focus[] focus;public bool sameHeightfieldsAsPreviousEvent;public double captureSeconds;public Road[] roads;}
        [Serializable]public sealed class Evidence
        {public string scope="READ_ONLY_ACTUAL_PUBLIC_VENDOR_CONTOURS_AND_EVERY_ROAD_CREATION_UPDATE_HEIGHT_HASH_NOT_A_TERRAIN_REPAIR",status="OBSERVING";public Vector3 focus=new Vector3(1255,0,1166.875f);public float nearRadius=140;public int actualVendorBuilds,roadCreationEvents,roadUpdateEvents;public bool sourceBytesExact;public string[] sourcePaths;public List<Event> events=new List<Event>();}
        readonly string output;readonly Dictionary<string,string> sources;readonly List<ERRoad> roads=new List<ERRoad>();readonly Evidence proof=new Evidence();Terrain[] terrains;string[] priorHashes;
        public DragonKingdomNativeGradeFootprintObservation(string path)
        {
            output=path;if(File.Exists(output))throw new InvalidOperationException("Fresh native footprint diagnostic path required.");
            proof.sourcePaths=new[]{"Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll","Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll.meta","Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab","Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab.meta"};sources=proof.sourcePaths.ToDictionary(p=>p,Sha);
        }
        static string Sha(string path){using(var stream=File.OpenRead(path))using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        Series ReadSeries(string name,object value)
        {
            var row=new Series{field=name,actualCollectionType=value?.GetType().FullName};var near=new List<Point>();
            using(var bytes=new MemoryStream())using(var writer=new BinaryWriter(bytes))
            {
                if(value is IEnumerable values)foreach(object item in values)
                {
                    if(row.firstElementType==null&&item!=null)row.firstElementType=item.GetType().FullName;int index=row.elements++;
                    if(!(item is Vector3 p))continue;row.vectors++;writer.Write(index);writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);
                    if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)){row.nonFiniteVectors++;continue;}
                    if(!row.hasVectorBounds){row.bounds=new Bounds(p,Vector3.zero);row.hasVectorBounds=true;}else row.bounds.Encapsulate(p);
                    float distance=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(proof.focus.x,proof.focus.z));row.nearestFocusDistance=Mathf.Min(row.nearestFocusDistance,distance);
                    if(distance<=proof.nearRadius){if(near.Count<4096)near.Add(new Point{index=index,position=p});else row.nearPointsOmitted++;}
                }
                writer.Flush();using(var sha=System.Security.Cryptography.SHA256.Create())row.allVectorCoordinatesSha256=BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-","").ToLowerInvariant();
            }
            row.nearPoints=near.ToArray();return row;
        }
        Road ReadRoad(ERRoad road,string stage)
        {
            var actual=road.roadScript;if(actual==null)return null;
            return new Road{componentId=actual.GetInstanceID(),name=actual.name,typeName=actual.rt?.roadTypeName,typeId=actual.rt==null?null:actual.rt.id.ToString("R",System.Globalization.CultureInfo.InvariantCulture),stage=stage,indent=actual.indent,surrounding=actual.surrounding,terrainDeformation=actual.terrainDeformation,leftSurrounding=actual.doLeftSurrounding?.ToArray(),rightSurrounding=actual.doRightSurrounding?.ToArray(),
                contours=new[]{ReadSeries("splinePoints",actual.splinePoints),ReadSeries("leftIndentVecs",actual.leftIndentVecs),ReadSeries("rightIndentVecs",actual.rightIndentVecs),ReadSeries("middleIndentVecs",actual.middleIndentVecs),ReadSeries("leftSurroundingVecs",actual.leftSurroundingVecs),ReadSeries("rightSurroundingVecs",actual.rightSurroundingVecs)}};
        }
        public void Observe(DragonKingdomGradeObservation observation)
        {
            if(observation.stage=="setting"||observation.stage=="span")return;
            if(observation.terrains!=null)terrains=observation.terrains;
            if(terrains==null||terrains.Length!=4)throw new InvalidOperationException("Actual four native grade fields required before footprint observation.");
            var clock=System.Diagnostics.Stopwatch.StartNew();
            var tiles=terrains.OrderBy(t=>t.name,StringComparer.Ordinal).Select(t=>new Tile{name=t.name,origin=t.transform.position,heightSha256=DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)}).ToArray();
            var sample=new Event{stage=observation.stage,road=observation.assignedRoadName??observation.road?.gameObject?.name,tiles=tiles,sameHeightfieldsAsPreviousEvent=priorHashes==null||tiles.Select(t=>t.heightSha256).SequenceEqual(priorHashes)};priorHashes=tiles.Select(t=>t.heightSha256).ToArray();
            var focus=new List<Focus>();foreach(float x in new[]{1251.875f,1255,1258.125f})foreach(float z in new[]{1160.625f,1163.75f,1166.875f,1170})
            {var t=terrains.Single(v=>x>=v.transform.position.x&&x<v.transform.position.x+v.terrainData.size.x&&z>=v.transform.position.z&&z<v.transform.position.z+v.terrainData.size.z);focus.Add(new Focus{position=new Vector3(x,t.transform.position.y+t.terrainData.GetInterpolatedHeight((x-t.transform.position.x)/t.terrainData.size.x,(z-t.transform.position.z)/t.terrainData.size.z),z)});}sample.focus=focus.ToArray();
            if(observation.stage=="roadCreated"){roads.Add(observation.road);proof.roadCreationEvents++;}
            if(observation.stage=="roadUpdated")proof.roadUpdateEvents++;
            sample.roads=observation.road!=null?new[]{ReadRoad(observation.road,observation.stage)}:roads.Select(r=>ReadRoad(r,observation.stage)).Where(r=>r!=null).ToArray();
            if(observation.stage=="afterSingleVendorBuildBeforeDestroy")proof.actualVendorBuilds++;
            if(observation.stage=="afterNetworkDestroyAndExactVendorOutputRestore")proof.status="ACTUAL_PUBLIC_CONTOURS_EACH_CREATE_UPDATE_AND_SINGLE_VENDOR_BUILD_CAPTURED_REQUIRES_CAUSAL_ANALYSIS";
            sample.captureSeconds=clock.Elapsed.TotalSeconds;proof.events.Add(sample);
            if(observation.stage!="roadCreated"&&observation.stage!="roadUpdated"||proof.roadUpdateEvents%8==0)Persist();
        }
        void Persist(){proof.sourceBytesExact=sources.All(p=>File.Exists(p.Key)&&Sha(p.Key)==p.Value);File.WriteAllText(output,JsonUtility.ToJson(proof,true));if(!proof.sourceBytesExact)throw new InvalidOperationException("Read-only native footprint observation found changed original vendor source bytes.");}
    }
}
#endif
