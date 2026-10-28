using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckCamping.Editor
{
    // Explicit render-only addition to the ONE shared prefab. Never called by
    // builds, scene startup, asset import, OnValidate or editor initialization.
    public static class CampingCampsiteDressingAuthoring
    {
        public const string OwnedRoot="Owned purchased campsite dressing V1";
        const string Shared=CampingWilderness.SharedPrefab;
        const string SharedGuid="2ec3264f79b522a49bcddfec51fc1cda";
        static readonly int[] Sites={0,3,10,11};
        public static readonly string[] Sources={
            "Assets/IndieKit/Crates and Barrels/Prefabs/Non-Destructible/crate_01.prefab",
            "Assets/IndieKit/Crates and Barrels/Prefabs/Non-Destructible/crate_03.prefab",
            "Assets/Toon Suburban Pack/Prefabs/Interiors/Kitchen Props/TSP_Pot_01A.prefab",
            "Assets/Toon Suburban Pack/Prefabs/Exterior Props/TSP_Bench_01A.prefab",
            "Assets/Toon Suburban Pack/Prefabs/Exterior Props/TSP_Bench_01B.prefab"};
        static readonly string[] Guids={"474b836f8361a7843a7a9a3707baf5ba","f9277a307cdb5fc479dc0c89a9e38260","93242d8c3ecd8df46ba7b7768f2edaa7","0e683deca8680434c99498caf67d1dc0","f469e043cdec02544b4d8c08c98f936a"};
        [Serializable]public sealed class Receipt {public string scope="EXPLICIT RENDER-ONLY SHARED PREFAB; native/Windows/human visual acceptance pending.",before,after,originalGraph,backup;public int models,trees;}
        sealed class Placement {public string name,restOn;public int source,landmark;public Vector3 offset;public float footprint,yaw;}
        static Placement[] Plan()
        {
            var result=new List<Placement>();foreach(int i in Sites){float side=i==11?1:-1;result.Add(new Placement{name="Site"+i+" original timber supply crate",source=0,landmark=i,offset=new Vector3(side*19.2f,0,18),footprint=1.8f,yaw=12});result.Add(new Placement{name="Site"+i+" small timber supply crate",source=1,landmark=i,offset=new Vector3(side*17,0,20.5f),footprint=1.3f,yaw=-15});result.Add(new Placement{name="Site"+i+" enamel cooking vessel",source=2,landmark=i,restOn="Site"+i+" original timber supply crate",footprint=.48f,yaw=25});}
            result.Add(new Placement{name="Eagle Ridge slatted overlook bench",source=3,landmark=4,offset=new Vector3(17,0,-10),footprint=3.6f,yaw=145});result.Add(new Placement{name="Meadow slatted rest bench",source=4,landmark=7,offset=new Vector3(17,0,-10),footprint=3.6f,yaw=145});return result.ToArray();
        }
        static void Stopped(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required; no live world changes.");}
        static string Digest(byte[] b){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(b).Select(x=>x.ToString("x2")));}
        static string FileHash(string p)=>Digest(File.ReadAllBytes(p));
        static GameObject[] RequireSources()
        {
            var result=new GameObject[Sources.Length];for(int i=0;i<Sources.Length;i++){if(AssetDatabase.AssetPathToGUID(Sources[i])!=Guids[i]||(result[i]=AssetDatabase.LoadAssetAtPath<GameObject>(Sources[i]))==null)throw new InvalidOperationException("Exact reviewed private purchased source required before dressing: "+Sources[i]);if(result[i].GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==null))throw new InvalidOperationException("Source mesh dependency incomplete: "+Sources[i]);foreach(var r in result[i].GetComponentsInChildren<Renderer>(true))if(r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>m==null||m.shader==null))throw new InvalidOperationException("Source material/shader dependency incomplete: "+Sources[i]);}return result;
        }
        static CampingWilderness ValidateWorld(GameObject root)
        {
            var w=root.GetComponent<CampingWilderness>();if(w==null||w.terrains==null||w.landmarks==null||w.campsites==null||w.resources==null||w.hammocks==null||w.terrains.Length!=4||w.landmarks.Length!=12||w.campsites.Length!=4||w.resources.Length<48||w.hammocks.Length!=4||w.terrains.Any(t=>t==null||t.terrainData==null)||w.landmarks.Any(t=>t==null)||w.campsites.Any(t=>t==null)||w.resources.Any(t=>t==null)||w.terrains.Sum(t=>t.terrainData.treeInstanceCount)!=2297||root.GetComponentsInChildren<Component>(true).Count(c=>c!=null&&c.GetType().FullName=="Unity.AI.Navigation.NavMeshSurface")!=1)throw new InvalidOperationException("Preserve unknown/custom wilderness: expected shared4terrains/2297trees/12landmarks/4sites/originalnav.");
            foreach(int i in Sites){var site=w.campsites.SingleOrDefault(s=>s.id=="camp.fire."+i);if(site==null||site.landmark!=w.landmarks[i]||site.GetComponentsInChildren<Collider>(true).Count(c=>c.name=="Bound cedar camp bench")!=4)throw new InvalidOperationException("Original campsite identity/bench colliders required: "+i);}return w;
        }
        static string PathOf(Transform t){string p=t.name+"#"+t.GetSiblingIndex();while(t.parent!=null){t=t.parent;p=t.name+"#"+t.GetSiblingIndex()+"/"+p;}return p;}
        static string Reference(UnityEngine.Object value)
        {
            if(value==null)return "NULL";if(EditorUtility.IsPersistent(value)&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id))return guid+":"+id;
            if(value is Component c)return PathOf(c.transform)+":"+c.GetType().FullName+":"+Array.IndexOf(c.GetComponents(c.GetType()),c);if(value is GameObject g)return PathOf(g.transform)+":GameObject";return value.GetType().FullName+":"+value.name;
        }
        static string Canonical(Component c)=>Regex.Replace(EditorJsonUtility.ToJson(c),@"""instanceID"":\s*(-?[0-9]+)",m=>"object="+Reference(EditorUtility.InstanceIDToObject(int.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture))));
        static string V(Vector3 p)=>p.x.ToString("R",CultureInfo.InvariantCulture)+","+p.y.ToString("R",CultureInfo.InvariantCulture)+","+p.z.ToString("R",CultureInfo.InvariantCulture);
        public static string OriginalGraphDigest(GameObject root)
        {
            var owned=root.transform.Find(OwnedRoot);var b=new StringBuilder();foreach(var t in root.GetComponentsInChildren<Transform>(true)){if(owned!=null&&(t==owned||t.IsChildOf(owned)))continue;b.Append(PathOf(t)).Append('|').Append(t.gameObject.activeSelf).Append('|').Append(t.gameObject.layer).Append('|').Append(t.tag).Append('|').Append((int)GameObjectUtility.GetStaticEditorFlags(t.gameObject)).Append('|').Append(V(t.localPosition)).Append('|').Append(V(t.localScale)).Append('|').Append(t.localRotation.x.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(t.localRotation.y.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(t.localRotation.z.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(t.localRotation.w.ToString("R",CultureInfo.InvariantCulture));foreach(var c in t.GetComponents<Component>()){if(c==null)throw new InvalidOperationException("Preserve original missing script for explicit repair; dressing cannot repair it.");if(c is Transform)continue;b.Append('|').Append(c.GetType().FullName).Append(':').Append(Canonical(c));}b.AppendLine();}return Digest(Encoding.UTF8.GetBytes(b.ToString()));
        }
        static Dictionary<string,string> Dependencies()=>AssetDatabase.GetDependencies(Shared,true).Where(p=>p!=Shared&&File.Exists(p)).ToDictionary(p=>p,FileHash);
        static void ExactDependencies(Dictionary<string,string> before){foreach(var p in before)if(!File.Exists(p.Key)||FileHash(p.Key)!=p.Value)throw new InvalidOperationException("Original world dependency changed; preserve backup: "+p.Key);}
        static Bounds BoundsOf(GameObject g){var r=g.GetComponentsInChildren<Renderer>(true);if(r.Length==0)throw new InvalidOperationException("Genuine source renderers required.");var b=r[0].bounds;foreach(var a in r.Skip(1))b.Encapsulate(a.bounds);return b;}
        static bool Allowed(Component c)=>c is Transform||c is MeshFilter||c is MeshRenderer||c is LODGroup;
        static GameObject CreateModel(GameObject source,Transform wrapper,float footprint)
        {
            var instance=UnityEngine.Object.Instantiate(source,wrapper,false);instance.name=source.name;foreach(var c in instance.GetComponentsInChildren<Component>(true).Reverse())if(c!=null&&!Allowed(c))UnityEngine.Object.DestroyImmediate(c);
            foreach(var t in instance.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            var b=BoundsOf(instance);instance.transform.localScale*=footprint/Mathf.Max(b.size.x,b.size.z);b=BoundsOf(instance);instance.transform.position+=new Vector3(wrapper.position.x-b.center.x,wrapper.position.y-b.min.y,wrapper.position.z-b.center.z);return instance;
        }
        static Vector3 Position(Placement p,CampingWilderness w,Transform owned)
        {
            if(p.restOn!=null){var crate=owned.Find(p.restOn);if(crate==null)throw new InvalidOperationException("Owned crate must precede its purely visual pot.");var b=BoundsOf(crate.gameObject);return new Vector3(b.center.x,b.max.y+.025f,b.center.z);}
            var point=w.landmarks[p.landmark].transform.TransformPoint(p.offset);point.y=w.Height(point)+.025f;return point;
        }
        static void Build(CampingWilderness w,GameObject[] sources)
        {
            if(w.transform.Find(OwnedRoot)!=null)throw new InvalidOperationException("Existing owned/custom dressing preserved; validate it instead of regenerating.");var owned=new GameObject(OwnedRoot).transform;owned.SetParent(w.transform,false);
            foreach(var p in Plan()){var node=new GameObject(p.name).transform;node.SetParent(owned,false);node.position=Position(p,w,owned);node.rotation=Quaternion.Euler(0,p.yaw,0);CreateModel(sources[p.source],node,p.footprint);}Verify(w,sources);
        }
        static void Verify(CampingWilderness w,GameObject[] sources)
        {
            var owned=w.transform.Find(OwnedRoot);var plan=Plan();if(owned==null||!owned.gameObject.activeSelf||owned.gameObject.layer!=0||owned.localPosition!=Vector3.zero||owned.localRotation!=Quaternion.identity||owned.localScale!=Vector3.one||owned.childCount!=plan.Length||owned.GetComponents<Component>().Length!=1||owned.GetComponentsInChildren<Component>(true).Any(c=>c==null||!Allowed(c)))throw new InvalidOperationException("Preserve unknown/custom dressing hierarchy or non-render-only components.");
            foreach(var p in plan){var node=owned.Find(p.name);if(node==null||node.GetComponents<Component>().Length!=1||!node.gameObject.activeSelf||node.gameObject.layer!=0||node.childCount!=1||(node.position-Position(p,w,owned)).sqrMagnitude>1e-7f||Quaternion.Angle(node.rotation,Quaternion.Euler(0,p.yaw,0))>.001f||node.localScale!=Vector3.one)throw new InvalidOperationException("Preserve custom owned prop placement: "+p.name);var instance=node.GetChild(0).gameObject;var source=sources[p.source];var actualTransforms=instance.GetComponentsInChildren<Transform>(true);var sourceTransforms=source.GetComponentsInChildren<Transform>(true);if(actualTransforms.Length!=sourceTransforms.Length)throw new InvalidOperationException("Preserve customized source hierarchy: "+p.name);for(int t=0;t<actualTransforms.Length;t++){var a=actualTransforms[t];var e=sourceTransforms[t];if(a.name!=e.name||a.gameObject.activeSelf!=e.gameObject.activeSelf||a.gameObject.layer!=e.gameObject.layer||GameObjectUtility.GetStaticEditorFlags(a.gameObject)!=0||(t>0&&(a.localPosition!=e.localPosition||a.localRotation!=e.localRotation||a.localScale!=e.localScale)))throw new InvalidOperationException("Preserve customized source transform: "+p.name);}var actualRenderers=instance.GetComponentsInChildren<Renderer>(true);var sourceRenderers=source.GetComponentsInChildren<Renderer>(true);if(actualRenderers.Length!=sourceRenderers.Length||actualRenderers.Where((r,i)=>r.enabled!=sourceRenderers[i].enabled).Any())throw new InvalidOperationException("Preserve customized source visibility: "+p.name);var meshes=instance.GetComponentsInChildren<MeshFilter>(true).Select(x=>x.sharedMesh).ToArray();var expectedMeshes=source.GetComponentsInChildren<MeshFilter>(true).Select(x=>x.sharedMesh).ToArray();var materials=instance.GetComponentsInChildren<Renderer>(true).SelectMany(x=>x.sharedMaterials).ToArray();var expectedMaterials=source.GetComponentsInChildren<Renderer>(true).SelectMany(x=>x.sharedMaterials).ToArray();if(!meshes.SequenceEqual(expectedMeshes)||!materials.SequenceEqual(expectedMaterials))throw new InvalidOperationException("Preserve customized source mesh/material bindings: "+p.name);
                var b=BoundsOf(instance);if(Mathf.Abs(Mathf.Max(b.size.x,b.size.z)-p.footprint)>.002f||Vector2.Distance(new Vector2(b.center.x,b.center.z),new Vector2(node.position.x,node.position.z))>.001f||Mathf.Abs(b.min.y-node.position.y)>.001f)throw new InvalidOperationException("Preserve changed prop fit: "+p.name);
                foreach(var resource in w.resources){var expanded=b;expanded.Expand(2);if(expanded.Contains(resource.home))throw new InvalidOperationException("Review actual resource/prop clearance instead of moving resources: "+p.name+" / "+resource.id);}foreach(var place in w.landmarks)if(b.SqrDistance(place.parking.position)<16)throw new InvalidOperationException("Preserve original truck activity parking clearance: "+p.name);
            }
        }
        // Tests and explicit preflight may operate only on a disposable prefab contents clone.
        // This method never saves, imports, creates assets, or edits original asset data.
        public static string ValidateOrBuildDetached(GameObject root)
        {
            Stopped();if(EditorUtility.IsPersistent(root)||root==null)throw new InvalidOperationException("Detached prefab contents required.");var sources=RequireSources();var w=ValidateWorld(root);var before=OriginalGraphDigest(root);if(w.transform.Find(OwnedRoot)==null)Build(w,sources);else Verify(w,sources);if(OriginalGraphDigest(root)!=before)throw new InvalidOperationException("Original world graph changed.");return before;
        }
        public static string InspectSavedDressing()
        {
            Stopped();var sources=RequireSources();var root=PrefabUtility.LoadPrefabContents(Shared);try{var w=ValidateWorld(root);Verify(w,sources);return OriginalGraphDigest(root);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        public static string DryRunOnDetachedOriginal()
        {
            Stopped();var sources=RequireSources();var root=PrefabUtility.LoadPrefabContents(Shared);try{var w=ValidateWorld(root);var before=OriginalGraphDigest(root);if(w.transform.Find(OwnedRoot)==null)Build(w,sources);else Verify(w,sources);if(OriginalGraphDigest(root)!=before)throw new InvalidOperationException("Detached dressing changed original world graph.");return before;}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        [MenuItem("Truck Sports/Impossible Games/Install reviewed render-only campsite dressing")]
        public static void InstallMissingOwnedCampsiteDressing()
        {
            Stopped();var sources=RequireSources();if(AssetDatabase.AssetPathToGUID(Shared)!=SharedGuid)throw new InvalidOperationException("Exact one shared Camping/Pet world GUID required.");var bytes=File.ReadAllBytes(Shared);var meta=File.ReadAllBytes(Shared+".meta");var deps=Dependencies();string beforeFile=Digest(bytes),beforeGraph;var root=PrefabUtility.LoadPrefabContents(Shared);
            try{var w=ValidateWorld(root);beforeGraph=OriginalGraphDigest(root);if(root.transform.Find(OwnedRoot)!=null){Verify(w,sources);Debug.Log("CAMP_DRESSING_EXISTING_VERIFIED no regeneration or save.");PrefabUtility.UnloadPrefabContents(root);return;}Build(w,sources);if(OriginalGraphDigest(root)!=beforeGraph)throw new InvalidOperationException("Original component/pose/physics graph changed before save.");}
            catch{PrefabUtility.UnloadPrefabContents(root);throw;}
            string backup=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingPurchasedDressing/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(backup);File.WriteAllBytes(Path.Combine(backup,"original.prefab"),bytes);File.WriteAllBytes(Path.Combine(backup,"original.prefab.meta"),meta);
            try{try{if(PrefabUtility.SaveAsPrefabAsset(root,Shared)==null)throw new IOException("Could not save explicit owned dressing.");}finally{PrefabUtility.UnloadPrefabContents(root);}root=PrefabUtility.LoadPrefabContents(Shared);try{var w=ValidateWorld(root);Verify(w,sources);if(OriginalGraphDigest(root)!=beforeGraph)throw new InvalidOperationException("Reloaded original graph differs; restore backup.");}finally{PrefabUtility.UnloadPrefabContents(root);}ExactDependencies(deps);if(!File.ReadAllBytes(Shared+".meta").SequenceEqual(meta))throw new InvalidOperationException("Original shared prefab GUID/meta changed.");var receipt=new Receipt{before=beforeFile,after=FileHash(Shared),originalGraph=beforeGraph,backup=backup,models=Plan().Length,trees=2297};File.WriteAllText(Path.Combine(backup,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("CAMP_PURCHASED_DRESSING_SAVED models="+receipt.models+"; one shared prefab; old graph/dependencies/2297trees/nav/colliders/activities retained; controlled/native visual acceptance pending.");}
            catch{File.WriteAllBytes(Shared,bytes);File.WriteAllBytes(Shared+".meta",meta);AssetDatabase.ImportAsset(Shared,ImportAssetOptions.ForceSynchronousImport);throw;}
        }
    }
}
