using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Assets.CoreCollapse.Scripts.Structural;
using LWS.TruckPedestrianPlowing;

namespace LWS.TruckKeepaway.Editor
{
    // Explicit migration of the two existing authored scenes, never a builder
    // hook. Existing props, models, materials, UI and shared camera rig survive.
    public static class KeepawayArenaRepairAuthoring
    {
        const string Art="Assets/TruckKeepaway/Materials/";
        public static float Footprint(Mesh mesh)
        {
            float area=0;var points=mesh.vertices;var indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
                area+=Mathf.Abs(Vector3.Cross(points[indices[i+1]]-points[indices[i]],points[indices[i+2]]-points[indices[i]]).y)*.5f;
            return area;
        }
        public static void Upgrade()
        {
            if(Type.GetType("Assets.CoreCollapse.EzySlice.SlicerExtensions, Assembly-CSharp")==null)
                throw new InvalidOperationException("Keepaway authoring requires the project's owned Core-Collapse EzySlice tool. Existing pre-cut scenes/builds do not require that editor tool.");
            var report=new System.Text.StringBuilder();
            foreach(var path in new[]{KeepawayAuthoring.Scene,KeepawayChaosAuthoring.CabScene})
            {
                EditorSceneManager.OpenScene(path);var match=UnityEngine.Object.FindAnyObjectByType<KeepawayMatch>();var field=match.chaosField;
                if(field.arenaRepairVersion>=1){report.AppendLine("PRESERVED existing version1 customizations "+path);continue;}
                var surface=field.ground.surface;var filter=surface.GetComponent<MeshFilter>();
                field.baselinePlayableArea=Footprint(filter.sharedMesh);
                field.baselineObstacleCount=field.solidProps.Length+field.ramps.Length+field.movable.Length+field.sweepers.Length;
                var expanded=UnityEngine.Object.Instantiate(filter.sharedMesh);var vertices=expanded.vertices;var uv=expanded.uv;
                for(int i=0;i<vertices.Length;i++){vertices[i]=Expand(vertices[i]);uv[i]=new Vector2(vertices[i].x,vertices[i].z)*.5f;}
                expanded.vertices=vertices;expanded.uv=uv;expanded.name="Three-area contoured Keepaway surface";expanded.RecalculateNormals();expanded.RecalculateTangents();expanded.RecalculateBounds();
                var mesh=Save(expanded,"Expanded contoured arena.asset");filter.sharedMesh=mesh;surface.GetComponent<MeshCollider>().sharedMesh=mesh;
                var boundary=GameObject.Find("Rounded continuous Keepaway boards").transform;
                foreach(Transform child in boundary)
                {
                    if(child==surface.transform)continue;
                    child.position=Expand(child.position);child.localScale=Expand(child.localScale);
                }
                var floor=GameObject.Find("Keepaway arena").transform;floor.localScale=Expand(floor.localScale);
                var scenery=GameObject.Find("Installed Keepaway environment");if(scenery!=null)scenery.transform.localScale=Expand(scenery.transform.localScale);
                foreach(var ring in UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include))
                    if(ring.name=="Sweeper warning perimeter")for(int i=0;i<ring.positionCount;i++)ring.SetPosition(i,Expand(ring.GetPosition(i)));
                foreach(var prop in field.solidProps)prop.position=Expand(prop.position);
                foreach(var prop in field.ramps)prop.position=Expand(prop.position);
                foreach(var prop in field.movable)prop.position=Expand(prop.position);
                foreach(var prop in field.sweepers)prop.position=Expand(prop.position);
                foreach(var prop in field.patches)prop.position=Expand(prop.position);
                var all=field.solidProps.Concat(field.ramps).Concat(field.movable.Select(v=>v.transform)).Concat(field.sweepers.Select(v=>v.transform)).ToArray();
                var breakables=new List<KeepawayArenaProp>();
                for(int i=0;i<all.Length;i++)breakables.Add(InstallProp(all[i],i,field.ramps.Contains(all[i])||field.sweepers.Any(v=>v.transform==all[i]),match.trucks[0].body.mass));
                field.breakableProps=breakables.ToArray();match.arenaX=KeepawayBoundary.HalfX;match.arenaZ=KeepawayBoundary.HalfZ;
                foreach(var truck in match.trucks)truck.transform.position=Expand(truck.transform.position);
                field.arenaRepairVersion=1;
                float ratio=Footprint(mesh)/field.baselinePlayableArea;
                if(Mathf.Abs(ratio-3)>.002f||breakables.Count!=field.baselineObstacleCount)throw new Exception("Keepaway physical footprint/count migration mismatch");
                report.AppendLine(path+" baselineArea="+field.baselinePlayableArea+" expandedArea="+Footprint(mesh)+" ratio="+ratio+" props="+breakables.Count+" densityRatio="+(1/ratio)+"; camera profile, menus and rules retained");
                EditorUtility.SetDirty(field);EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/keepaway-arena-migration.txt",report.ToString());Debug.Log(report.ToString());
        }
        static Vector3 Expand(Vector3 p)=>new Vector3(p.x*KeepawayBoundary.Expansion,p.y,p.z*KeepawayBoundary.Expansion);
        static Mesh Save(Mesh mesh,string name)
        {
            string path=Art+name;var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }
        static KeepawayArenaProp InstallProp(Transform root,int index,bool anchored,float truckMass)
        {
            var shape=root.GetComponent<Collider>();if(shape==null)throw new Exception("Missing obstacle collision: "+root.name);
            var model=new GameObject("Intact owned arena model").transform;model.SetParent(root,false);
            foreach(Transform child in root.Cast<Transform>().Where(t=>t!=model).ToArray())child.SetParent(model,true);
            var rootRenderer=root.GetComponent<MeshRenderer>();var rootFilter=root.GetComponent<MeshFilter>();
            if(rootRenderer!=null&&rootRenderer.enabled&&rootFilter!=null)
            {
                var copy=new GameObject("Existing modeled surface");copy.transform.SetParent(model,false);copy.AddComponent<MeshFilter>().sharedMesh=rootFilter.sharedMesh;copy.AddComponent<MeshRenderer>().sharedMaterials=rootRenderer.sharedMaterials;rootRenderer.enabled=false;
            }
            var source=model.GetComponentsInChildren<MeshFilter>(true).Where(v=>v.sharedMesh!=null&&v.GetComponent<MeshRenderer>()!=null&&v.GetComponent<MeshRenderer>().enabled).ToArray();
            if(source.Length==0)throw new Exception("No actual model for Keepaway destruction: "+root.name);
            var bounds=source[0].GetComponent<Renderer>().bounds;foreach(var sourceMesh in source)bounds.Encapsulate(sourceMesh.GetComponent<Renderer>().bounds);
            var body=root.GetComponent<Rigidbody>();if(body==null)body=root.gameObject.AddComponent<Rigidbody>();body.isKinematic=anchored;body.mass=Mathf.Clamp(truckMass*.12f,1,200);body.maxLinearVelocity=18;body.maxAngularVelocity=7;body.angularDamping=.6f;body.collisionDetectionMode=CollisionDetectionMode.Discrete;
            var support=root.gameObject.AddComponent<Support>();var settings=new SerializedObject(support);settings.FindProperty("supportedBy").arraySize=0;settings.ApplyModifiedPropertiesWithoutUndo();typeof(Support).GetField("onCollapse",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(support,new Support.OnCollapseEvent());EditorUtility.SetDirty(support);
            var collapse=root.gameObject.AddComponent<PlowBreakable>();collapse.intactBody=body;collapse.model=model;var health=new SerializedObject(collapse);health.FindProperty("startingHp").floatValue=anchored?45:root.name.Contains("tire")?18:25;health.ApplyModifiedPropertiesWithoutUndo();
            collapse.fragments=new Rigidbody[2];var halves=new Transform[2];
            for(int side=0;side<2;side++)
            {
                var fragment=new GameObject("Pooled actual-model fracture "+side);fragment.layer=2;fragment.transform.SetParent(root,false);halves[side]=fragment.transform;
                var piece=fragment.AddComponent<Rigidbody>();piece.mass=Mathf.Max(.25f,body.mass*.5f);piece.isKinematic=true;piece.maxLinearVelocity=12;piece.maxAngularVelocity=7;piece.linearDamping=.5f;piece.angularDamping=.6f;collapse.fragments[side]=piece;
            }
            int partIndex=0;
            foreach(var sourceMesh in source)
            {
                var renderer=sourceMesh.GetComponent<MeshRenderer>();var cap=renderer.sharedMaterials[0];
                // Vendor EzySlice cuts the real owned prop geometry offline.
                // No generic debris shape or runtime mesh-fracture expense.
                var slicer=Type.GetType("Assets.CoreCollapse.EzySlice.SlicerExtensions, Assembly-CSharp",true);
                var hull=slicer.GetMethod("Slice",new[]{typeof(GameObject),typeof(Vector3),typeof(Vector3),typeof(Material)}).Invoke(null,new object[]{sourceMesh.gameObject,bounds.center,root.right,cap});
                if(hull==null)
                {
                    int side=Vector3.Dot(renderer.bounds.center-bounds.center,root.right)>=0?0:1;
                    var copy=new GameObject(sourceMesh.name+" whole fragment");Place(copy.transform,sourceMesh.transform,halves[side]);copy.AddComponent<MeshFilter>().sharedMesh=sourceMesh.sharedMesh;copy.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                }
                else for(int side=0;side<2;side++)
                {
                    var piece=(GameObject)hull.GetType().GetMethod(side==0?"CreateUpperHull":"CreateLowerHull",new[]{typeof(GameObject),typeof(Material)}).Invoke(hull,new object[]{sourceMesh.gameObject,cap});
                    if(piece==null)continue;var generated=piece.GetComponent<MeshFilter>();generated.sharedMesh=Save(generated.sharedMesh,"Expanded prop "+index+" mesh "+partIndex+" side "+side+".asset");Place(piece.transform,sourceMesh.transform,halves[side]);piece.name=sourceMesh.name+" cut fragment";
                }
                partIndex++;
            }
            for(int side=0;side<2;side++)
            {
                var meshes=halves[side].GetComponentsInChildren<MeshFilter>();if(meshes.Length==0)throw new Exception("Vendor slice produced an empty obstacle half: "+root.name);
                var local=new Bounds();bool first=true;
                foreach(var mesh in meshes)
                {
                    var box=mesh.sharedMesh.bounds;for(int corner=0;corner<8;corner++)
                    {
                        var point=halves[side].InverseTransformPoint(mesh.transform.TransformPoint(box.center+Vector3.Scale(box.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
                        if(first){local=new Bounds(point,Vector3.zero);first=false;}else local.Encapsulate(point);
                    }
                }
                var collider=halves[side].gameObject.AddComponent<BoxCollider>();collider.center=local.center;collider.size=local.size*.94f;
                foreach(var part in halves[side].GetComponentsInChildren<Transform>())part.gameObject.layer=2;
                halves[side].gameObject.SetActive(false);
            }
            var adapter=root.gameObject.AddComponent<KeepawayArenaProp>();adapter.breakable=collapse;adapter.intactShape=shape;adapter.anchored=anchored;EditorUtility.SetDirty(collapse);EditorUtility.SetDirty(adapter);return adapter;
        }
        static void Place(Transform copy,Transform original,Transform parent)
        {copy.SetPositionAndRotation(original.position,original.rotation);copy.localScale=original.lossyScale;copy.SetParent(parent,true);}
    }
}
