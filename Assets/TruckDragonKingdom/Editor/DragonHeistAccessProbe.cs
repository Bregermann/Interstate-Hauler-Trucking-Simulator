#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckFareFight;
using LWS.TruckHoardHeist;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Read-only actual saved-world route/support diagnosis. No match start,
    // truck/cargo pose, graph, scoring or destination is changed.
    public static class DragonHeistAccessProbe
    {
        [Serializable]sealed class Support {public Vector3 point;public string collider;public bool supported;public float error;}
        [Serializable]sealed class Route {public int slot;public Vector3 origin,goal;public string startEdge,finishEdge;public Vector3 startProjection,finishProjection;public float length,straightDistance,largestGap;public int unsupported;public Vector3[] points;public Support[] failures;}
        [Serializable]sealed class Receipt {public string scope="READ_ONLY_ACTUAL_SAVED_HEIST_ROUTE_AND_PHYSICAL_SUPPORT_DIAGNOSIS";public Vector3 reception;public Support receptionSupport;public Route[] routes;public bool sceneBytesPreserved;}
        static Support Probe(Scene scene,Vector3 point)
        {
            var hits=Physics.RaycastAll(point+Vector3.up*8,Vector3.down,50,~0,QueryTriggerInteraction.Ignore).Where(hit=>hit.collider.gameObject.scene==scene&&hit.normal.y>.6f).OrderBy(hit=>Mathf.Abs(hit.point.y-point.y)).ToArray();
            return new Support{point=point,collider=hits.Length==0?"NONE":hits[0].collider.name,error=hits.Length==0?float.PositiveInfinity:Mathf.Abs(hits[0].point.y-point.y),supported=hits.Length>0&&Mathf.Abs(hits[0].point.y-point.y)<4};
        }
        public static void CaptureSavedReceptionAccess()
        {
            if(Application.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            const string path="Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity";byte[] before=File.ReadAllBytes(path);var prior=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<HoardHeistMatch>(true)).Single();Physics.SyncTransforms();var graph=match.world.definition.graph;var facts=new List<Route>();
                foreach(var entry in match.world.Region("caverns").truckStarts.Select((start,slot)=>(start,slot)))
                {
                    var route=new FareFightRoute{useRoadConnectors=true};Vector3 origin=entry.start.position,goal=match.extractionApproach.position;route.Update(graph,origin,goal,0,0,true,entry.start.forward);
                    LwsRoadGraphQuery.TryFindNearestRoad(graph,origin,150,out var start);LwsRoadGraphQuery.TryFindNearestRoad(graph,goal,150,out var finish);
                    var probes=route.points.Select(point=>Probe(scene,point)).ToArray();float length=0,gap=0;for(int i=1;i<route.points.Count;i++){float segment=Vector3.Distance(route.points[i-1],route.points[i]);length+=segment;gap=Mathf.Max(gap,segment);}
                    facts.Add(new Route{slot=entry.slot,origin=origin,goal=goal,startEdge=start.EdgeId,finishEdge=finish.EdgeId,startProjection=start.NearestPosition,finishProjection=finish.NearestPosition,length=length,straightDistance=Vector3.Distance(origin,goal),largestGap=gap,unsupported=probes.Count(probe=>!probe.supported),points=route.points.ToArray(),failures=probes.Where(probe=>!probe.supported).ToArray()});
                }
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Unique dragon output required.");string output=Path.Combine(args[at+1],"HeistRoadAccess");Directory.CreateDirectory(output);string receipt=Path.Combine(output,"saved-reception.json");if(File.Exists(receipt))throw new IOException("Preserve prior diagnosis.");
                if(!File.ReadAllBytes(path).SequenceEqual(before))throw new InvalidOperationException("Read-only reception probe changed saved scene bytes.");
                File.WriteAllText(receipt,JsonUtility.ToJson(new Receipt{reception=match.extractionApproach.position,receptionSupport=Probe(scene,match.extractionApproach.position),routes=facts.ToArray(),sceneBytesPreserved=true},true));
                Debug.Log("HEIST_RECEPTION_PHYSICAL_DIAGNOSIS "+receipt);
            }
            finally{if(opened&&scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
        }
    }
}
#endif
