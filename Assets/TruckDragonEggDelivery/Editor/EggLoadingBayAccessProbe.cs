#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckFareFight;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonEggDelivery.Editor
{
    // Diagnostic queries only. The actual saved trucks/cargo, objective/road
    // transforms, graph, physics settings and delivery authorities are untouched.
    public static class EggLoadingBayAccessProbe
    {
        const string ScenePath="Assets/TruckDragonEggDelivery/Scenes/TruckDragonEggDelivery.unity";
        [Serializable] sealed class WheelFact {public string wheel,support;public Vector3 expected,contact,normal;public float verticalError;public bool supported;}
        [Serializable] sealed class Sample {public string leg,support;public Vector3 nominal,origin,normal;public Quaternion rotation;public float supportError,nestReach,approachDistance;public bool supported,bodyClear;public WheelFact[] wheels;public string[] blockers;}
        [Serializable] sealed class Bay {public int slot;public string id,startEdge,finishEdge;public bool routeBuilt;public Vector3 start,nest,approach,roadProjection;public Quaternion approachRotation;public float routeLength,connectorLength;public Vector3[] route;public Sample bayPose;public Sample[] connector;public int failures;}
        [Serializable] sealed class Receipt {public string scope="READ_ONLY saved Egg four-bay routes and continuous final road access, original full NWH wheel footprint/body envelope. No spawn/match/AI/hoist/scoring/geometry changes; finite static support does not prove driving success.";public bool sceneBytesExact,sceneMetadataExact,graphExact,truckPosesExact;public int failures;public Bay[] bays;}
        static string PathOf(Transform value)=>value==null?"none":value.parent==null?value.name:PathOf(value.parent)+"/"+value.name;
        static bool OwnTruck(Collider collider)=>collider.GetComponentInParent<SportsEventTruck>()!=null;
        static RaycastHit[] Supports(Scene scene,Vector3 point,float above,float down)
            =>Physics.RaycastAll(point+Vector3.up*above,Vector3.down,down,~0,QueryTriggerInteraction.Ignore).Where(hit=>hit.collider.gameObject.scene==scene&&!OwnTruck(hit.collider)&&hit.normal.y>.7f).OrderBy(hit=>Mathf.Abs(hit.point.y-point.y)).ToArray();
        static Sample Probe(Scene scene,SportsEventTruck truck,Vector3 nominal,Vector3 heading,string leg,Vector3 nest,Vector3 approach,Quaternion? exactRotation=null)
        {
            var ground=Supports(scene,nominal,8,30);var sample=new Sample{leg=leg,nominal=nominal,support=ground.Length==0?"NONE":PathOf(ground[0].transform),supportError=ground.Length==0?float.PositiveInfinity:Mathf.Abs(ground[0].point.y-nominal.y),normal=ground.Length==0?Vector3.up:ground[0].normal};
            sample.supported=ground.Length>0&&sample.supportError<1.5f;
            Vector3 forward=Vector3.ProjectOnPlane(heading,sample.normal).normalized;if(forward.sqrMagnitude<.5f)forward=Vector3.ProjectOnPlane(truck.transform.forward,sample.normal).normalized;
            sample.rotation=exactRotation??Quaternion.LookRotation(forward,sample.normal);
            float lift=.12f;foreach(var wheel in truck.wheels)lift=Mathf.Max(lift,.12f-(truck.transform.InverseTransformPoint(wheel.transform.position).y-wheel.wheel.radius-wheel.spring.maxLength));
            sample.origin=(ground.Length==0?nominal:ground[0].point)+sample.normal*lift;
            sample.nestReach=Vector3.Distance(sample.origin,nest);sample.approachDistance=Vector3.Distance(sample.origin,approach);
            var wheels=new List<WheelFact>();
            foreach(var wheel in truck.wheels)
            {
                Vector3 local=truck.transform.InverseTransformPoint(wheel.transform.position);Vector3 point=sample.origin+sample.rotation*Vector3.Scale(local,truck.transform.lossyScale);
                var found=Supports(scene,point,3,8);var viable=found.Where(hit=>Mathf.Abs(hit.point.y-(ground.Length==0?nominal.y:ground[0].point.y))<1.5f).ToArray();
                wheels.Add(new WheelFact{wheel=wheel.name,expected=point,support=viable.Length==0?"NONE":PathOf(viable[0].transform),contact=viable.Length==0?default:viable[0].point,normal=viable.Length==0?default:viable[0].normal,verticalError=viable.Length==0?float.PositiveInfinity:point.y-viable[0].point.y,supported=viable.Length>0});
            }
            sample.wheels=wheels.ToArray();sample.supported&=wheels.Count==4&&wheels.All(wheel=>wheel.supported);
            // Same real authored BoxCollider and conservative loading-bay margin;
            // no proxy tractor or relaxation of the accepted bay dimensions.
            Vector3 half=Vector3.Scale(truck.shape.size*.5f,truck.transform.lossyScale)+new Vector3(.45f,.1f,.6f);
            Vector3 center=sample.origin+sample.rotation*Vector3.Scale(truck.shape.center,truck.transform.lossyScale);
            sample.blockers=Physics.OverlapBox(center,half,sample.rotation,~0,QueryTriggerInteraction.Ignore).Where(collider=>collider.gameObject.scene==scene&&!collider.transform.IsChildOf(truck.transform)&&(ground.Length==0||collider!=ground[0].collider)).Select(collider=>PathOf(collider.transform)+" "+collider.bounds).Distinct().OrderBy(value=>value,StringComparer.Ordinal).ToArray();sample.bodyClear=sample.blockers.Length==0;return sample;
        }
        public static int CaptureSavedBays()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled read-only native Editor probe required.");
            if(!File.Exists(ScenePath)||!File.Exists(ScenePath+".meta"))throw new InvalidOperationException("Actual saved Egg scene required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve dirty user scenes before read-only additive probe.");
            byte[] before=File.ReadAllBytes(ScenePath),meta=File.ReadAllBytes(ScenePath+".meta");var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;bool opened=false;Receipt receipt=null;
            try
            {
                scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);opened=true;}
                var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonEggDeliveryMatch>(true)).Single();var graph=match.world.definition.graph;string graphBefore=JsonUtility.ToJson(graph);var trucks=match.trucks.Select(truck=>EditorJsonUtility.ToJson(truck.transform)).ToArray();Physics.SyncTransforms();var bays=new List<Bay>();
                for(int slot=0;slot<4;slot++)
                {
                    var truck=match.trucks[slot];var nest=match.nests[slot];var start=match.world.Region("castle").truckStarts[slot];var route=new FareFightRoute{useRoadConnectors=true};route.Update(graph,start.position,nest.approach.position,0,0,true,start.forward);
                    bool gotStart=LwsRoadGraphQuery.TryFindNearestRoad(graph,start.position,150,out var from);bool gotFinish=LwsRoadGraphQuery.TryFindNearestRoad(graph,nest.approach.position,150,out var to);
                    var bay=new Bay{slot=slot,id=nest.id,start=start.position,nest=nest.center.position,approach=nest.approach.position,approachRotation=nest.approach.rotation,startEdge=gotStart?from.EdgeId:"NONE",finishEdge=gotFinish?to.EdgeId:"NONE",roadProjection=gotFinish?to.NearestPosition:default,route=route.points.ToArray(),routeBuilt=route.points.Count>1};
                    for(int i=1;i<route.points.Count;i++)bay.routeLength+=Vector3.Distance(route.points[i-1],route.points[i]);
                    bay.bayPose=Probe(scene,truck,nest.approach.position,nest.approach.forward,"actual authored bay pose",bay.nest,bay.approach,nest.approach.rotation);
                    var samples=new List<Sample>();
                    if(gotFinish)
                    {
                        bay.connectorLength=Vector3.Distance(to.NearestPosition,bay.approach);int steps=Mathf.Max(1,Mathf.CeilToInt(bay.connectorLength));
                        if(steps>151)throw new InvalidOperationException("Nearest-road lookup exceeded its explicit150m connector bound.");
                        Vector3 direction=bay.approach-to.NearestPosition;
                        for(int i=0;i<=steps;i++)samples.Add(Probe(scene,truck,Vector3.Lerp(to.NearestPosition,bay.approach,i/(float)steps),direction,"continuous final graph-projection to bay",bay.nest,bay.approach,i==steps?(Quaternion?)nest.approach.rotation:null));
                        // Record the actual preceding road segment as well; the
                        // full ordered route remains in the receipt for diagnosis.
                        if(route.points.Count>2)
                        {
                            Vector3 a=route.points[route.points.Count-3],b=route.points[route.points.Count-2];float length=Vector3.Distance(a,b);int count=Mathf.Max(1,Mathf.CeilToInt(Mathf.Min(60,length)));Vector3 begin=length>60?Vector3.MoveTowards(b,a,60):a;
                            for(int i=0;i<=count;i++)samples.Add(Probe(scene,truck,Vector3.Lerp(begin,b,i/(float)count),b-a,"last60m actual ordered road leg",bay.nest,bay.approach));
                        }
                    }
                    bay.connector=samples.ToArray();bay.failures=(!bay.routeBuilt||!gotFinish?1:0)+(bay.bayPose.supported&&bay.bayPose.bodyClear&&bay.bayPose.nestReach<=25?0:1)+samples.Count(sample=>!sample.supported||!sample.bodyClear);bays.Add(bay);
                }
                receipt=new Receipt{bays=bays.ToArray(),failures=bays.Sum(bay=>bay.failures),graphExact=graphBefore==JsonUtility.ToJson(graph),truckPosesExact=trucks.SequenceEqual(match.trucks.Select(truck=>EditorJsonUtility.ToJson(truck.transform)))};
                if(!receipt.graphExact||!receipt.truckPosesExact||scene.isDirty)throw new InvalidOperationException("Read-only bay probe changed graph, tractor pose or scene dirtiness.");
            }
            finally{if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            receipt.sceneBytesExact=before.SequenceEqual(File.ReadAllBytes(ScenePath));receipt.sceneMetadataExact=meta.SequenceEqual(File.ReadAllBytes(ScenePath+".meta"));if(!receipt.sceneBytesExact||!receipt.sceneMetadataExact)throw new InvalidOperationException("Read-only probe changed saved scene or GUID metadata.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Unique explicit native output folder required.");string output=Path.Combine(args[at+1],"EggLoadingBayAccess","saved-bays.json");Directory.CreateDirectory(Path.GetDirectoryName(output));if(File.Exists(output))throw new IOException("Preserve previous bay diagnosis.");File.WriteAllText(output,JsonUtility.ToJson(receipt,true)+"\n");Debug.Log("EGG_LOADING_BAY_ACCESS_READ_ONLY "+output+" failures="+receipt.failures);return receipt.failures;
        }
    }
}
#endif
