using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckWaterSports.Editor
{
    /// <summary>Explicit use of the installed Fantastic Village rowboat art; no source prefab edits.</summary>
    public static class BlackwaterAbandonedCraftAuthoring
    {
        const string Source="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/props/P_PROP_rowboat.prefab";
        const string RootName="Owned abandoned Blackwater shoreline craft";
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this explicit addition.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/TruckWaterSports/Scenes/Blackwater.unity")throw new InvalidOperationException("Open the existing Blackwater scene.");
            if(scene.GetRootGameObjects().Any(g=>g.name==RootName)){Debug.Log("BLACKWATER_ABANDONED_CRAFT_PRESERVED");return;}
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Source)==null)throw new InvalidOperationException("Installed Fantastic Village rowboat required; never substitute placeholder geometry.");
            var match=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BlackwaterMatch>(true)).Single();
            var water=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Single(r=>r.name=="Owned River Modeler shaded flowing surface");
            var lanes=new List<(Vector3 a,Vector3 b)>();
            for(int i=1;i<match.course.stations.Length;i++)lanes.Add((match.course.stations[i-1],match.course.stations[i]));
            foreach(var station in match.course.stations)lanes.Add((match.dock.position,station));
            foreach(var supply in match.supplies)if(supply!=null)lanes.Add((match.dock.position,supply.position));
            var points=new List<Vector3>();
            for(int i=0;i<16;i++)
            {
                float x=i%2==0?water.bounds.min.x+18:water.bounds.max.x-18;
                float z=Mathf.Lerp(water.bounds.min.z+24,water.bounds.max.z-24,(i/2+.5f)/8);
                var point=new Vector3(x,match.course.SurfacePoint(new Vector3(x,0,z)).y,z);
                if(lanes.All(lane=>Distance(point,lane.a,lane.b)>65))points.Add(point);
            }
            if(points.Count<2)throw new InvalidOperationException("No safe shoreline craft placements found outside original navigation/delivery approaches.");
            var root=new GameObject(RootName).transform;
            for(int i=0;i<Mathf.Min(6,points.Count);i++)
            {
                var holder=new GameObject("Abandoned owned rowboat "+(i+1)).transform;holder.SetParent(root,false);holder.position=points[i]+Vector3.down*(i%3==0 ? .7f : .2f);
                holder.rotation=Quaternion.Euler(i%3==0?12:0,31+i*137,i%2==0?7:-5);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder,Source,new Vector3(3.5f,1.4f,8),0,false,"TruckWaterSports");
                foreach(var mesh in holder.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh!=null)
                {var shape=mesh.gameObject.AddComponent<MeshCollider>();shape.sharedMesh=mesh.sharedMesh;}
            }
            Undo.RegisterCreatedObjectUndo(root.gameObject,"Add installed Blackwater abandoned craft");EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("BLACKWATER_ABANDONED_CRAFT_ADDED "+root.childCount+" installed rowboats, original source intact, static hull contacts outside 65m protected gameplay approaches. Caller saves and inspects appearance.");
        }
        static float Distance(Vector3 point,Vector3 a,Vector3 b)
        {point.y=a.y=b.y=0;Vector3 delta=b-a;float t=delta.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(point-a,delta)/delta.sqrMagnitude):0;return Vector3.Distance(point,a+delta*t);}
    }
}
