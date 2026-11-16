#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public sealed class DragonKingdomGradeObservation
    {public string stage,setting,assignedRoadName,assignedRoadTypeName;public ERRoad road;public int marker;public float requested,returned;public Terrain[] terrains;public GameObject network;}
    public sealed class DragonKingdomVendorGradingObservation
    {
        [Serializable]public sealed class Settings{public string objectName,field,value;}
        [Serializable]public sealed class Marker{public int index;public Vector3 position;public string control;}
        [Serializable]public sealed class Road{public int id;public string type,assignedRoadName,assignedRoadTypeName,actualNativeRoadObjectName;public bool publicRoadTypeResolved;public Marker[] markers;public Settings[] serialized;}
        [Serializable]public sealed class Setting{public int road,marker;public string field;public float requested,returned;}
        [Serializable]public sealed class Span{public string upperAndLower;public float start,end;}
        [Serializable]public sealed class Tile{public string name,heightSha256;public Vector3 origin,size;public int resolution;}
        [Serializable]public sealed class Point{public string focus;public Vector3 world;}
        [Serializable]public sealed class Stage{public string name;public Tile[] tiles;public Point[] patches;public Settings[] networkSettings;}
        [Serializable]public sealed class Evidence
        {public string scope="READ_ONLY_ACTUAL_VENDOR_STAGE_HEIGHTS_SETTER_RETURNS_MARKERS_AND_SPANS_NOT_A_TERRAIN_REPAIR",status="OBSERVING";public int actualVendorBuilds;public List<Stage> stages=new List<Stage>();public List<Setting> settings=new List<Setting>();public List<Span> spans=new List<Span>();public List<Road> roads=new List<Road>();}
        readonly Dictionary<ERRoad,DragonKingdomGradeObservation> createdRoads=new Dictionary<ERRoad,DragonKingdomGradeObservation>();
        readonly Dictionary<ERRoad,int> roadIds=new Dictionary<ERRoad,int>();readonly string output;readonly DragonKingdomNativeGradeFootprintObservation footprints;public Evidence Proof{get;}=new Evidence();
        public DragonKingdomVendorGradingObservation(string output){this.output=output;Directory.CreateDirectory(Path.GetDirectoryName(output));if(File.Exists(output))throw new InvalidOperationException("Fresh diagnostic output only.");if(Environment.GetCommandLineArgs().Contains("-tsDragonObserveNativeGradeFootprints"))footprints=new DragonKingdomNativeGradeFootprintObservation(Path.ChangeExtension(output,null)+"-native-footprints.json");}
        static string Digest(float[,] heights){var bytes=new byte[heights.Length*4];Buffer.BlockCopy(heights,0,bytes,0,bytes.Length);using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static Settings[] ReadSettings(UnityEngine.Object owner)
        {
            if(owner==null)return Array.Empty<Settings>();var rows=new List<Settings>();var data=new SerializedObject(owner);var p=data.GetIterator();while(p.Next(true))
            {string key=p.name.ToLowerInvariant();if(!new[]{"indent","surround","terrain","smooth","level","bridge"}.Any(key.Contains))continue;string value=null;switch(p.propertyType){case SerializedPropertyType.Float:value=p.floatValue.ToString("R",System.Globalization.CultureInfo.InvariantCulture);break;case SerializedPropertyType.Integer:value=p.longValue.ToString();break;case SerializedPropertyType.Boolean:value=p.boolValue.ToString();break;case SerializedPropertyType.Enum:value=p.enumValueIndex.ToString();break;}if(value!=null)rows.Add(new Settings{objectName=owner.name,field=p.propertyPath,value=value});}return rows.ToArray();
        }
        int Id(ERRoad road){if(!roadIds.TryGetValue(road,out int id)){id=roadIds.Count;roadIds.Add(road,id);}return id;}
        public void Observe(DragonKingdomGradeObservation observation)
        {
            footprints?.Observe(observation);
            if(observation.stage=="roadCreated"){createdRoads.Add(observation.road,observation);Id(observation.road);}
            else if(observation.stage=="setting")Proof.settings.Add(new Setting{road=Id(observation.road),marker=observation.marker,field=observation.setting,requested=observation.requested,returned=observation.returned});
            else if(observation.stage=="span")Proof.spans.Add(new Span{upperAndLower=observation.setting,start=observation.requested,end=observation.returned});
            else if(observation.stage=="roadUpdated")
            {
                var road=observation.road;var positions=road.GetMarkerPositions();var settings=new List<Settings>();string actualName=null;
                foreach(var field in road.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance))
                {var value=field.GetValue(road);if(value is Component component){settings.AddRange(ReadSettings(component));actualName=component.gameObject.name;}else if(value is GameObject go){settings.AddRange(go.GetComponents<Component>().SelectMany(ReadSettings));actualName=go.name;}}
                var actualType=road.GetRoadType();createdRoads.TryGetValue(road,out var assigned);
                Proof.roads.Add(new Road{id=Id(road),publicRoadTypeResolved=actualType!=null,type=actualType==null?null:actualType.roadTypeName,assignedRoadName=assigned?.assignedRoadName,assignedRoadTypeName=assigned?.assignedRoadTypeName,actualNativeRoadObjectName=actualName,markers=positions.Select((p,i)=>new Marker{index=i,position=p,control=road.GetMarkerControlType(i).ToString()}).ToArray(),serialized=settings.ToArray()});
            }
            else
            {
                var stage=new Stage{name=observation.stage,tiles=observation.terrains.Select(t=>new Tile{name=t.name,origin=t.transform.position,size=t.terrainData.size,resolution=t.terrainData.heightmapResolution,heightSha256=Digest(t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution))}).ToArray()};var points=new List<Point>();
                foreach(var focus in new[]{Tuple.Create("NE_VENDOR_WALL",new Vector3(1180,0,1120)),Tuple.Create("FOREST_JUNCTION",new Vector3(-820,0,250))})for(int z=0;z<=32;z++)for(int x=0;x<=32;x++)
                {var p=focus.Item2+new Vector3(x*3.125f,0,z*3.125f);var t=observation.terrains.Single(v=>p.x>=v.transform.position.x&&p.x<v.transform.position.x+v.terrainData.size.x&&p.z>=v.transform.position.z&&p.z<v.transform.position.z+v.terrainData.size.z);var local=p-t.transform.position;p.y=t.transform.position.y+t.terrainData.GetInterpolatedHeight(local.x/t.terrainData.size.x,local.z/t.terrainData.size.z);points.Add(new Point{focus=focus.Item1,world=p});}stage.patches=points.ToArray();
                stage.networkSettings=observation.network==null?Array.Empty<Settings>():observation.network.GetComponents<Component>().SelectMany(ReadSettings).ToArray();Proof.stages.Add(stage);
                if(observation.stage=="afterSingleVendorBuildBeforeDestroy")Proof.actualVendorBuilds++;if(observation.stage=="afterNetworkDestroyAndExactVendorOutputRestore")Proof.status="ACTUAL_VENDOR_STAGES_CAPTURED_REQUIRES_BOUNDARY_CAUSE_ANALYSIS_NOT_WORLD_ACCEPTANCE";
            }
            if(observation.stage!="setting"&&observation.stage!="span"&&observation.stage!="roadUpdated"&&observation.stage!="roadCreated")File.WriteAllText(output,JsonUtility.ToJson(Proof,true));
        }
    }
}
#endif
