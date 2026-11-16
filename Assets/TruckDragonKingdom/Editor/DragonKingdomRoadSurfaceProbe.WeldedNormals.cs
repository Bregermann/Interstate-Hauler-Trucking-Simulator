#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable]public sealed class WeldedNormalGroup
        {public int lod,inputMeshes,inputVertices,uniqueProxyVertices,triangles,exactDuplicateVerticesWelded,unusedProxyVertices,invalidNormalProxyIndex=-1;public Vector3 invalidNormalProxyPosition,invalidNormalValue;public bool unityRecalculateNormalsCalled,proxyPositionsAndOrientedTrianglesExact,allCopyNonNormalAttributesExact,allCopiedNormalsMatchNativeProxy;public float maximumUnitSquaredError;}
        public sealed class WeldedNormalCopies:IDisposable
        {
            public Mesh[] copies,proxies;public WeldedNormalGroup[] groups;public int changedNormalMeshes;
            public void Dispose(){if(copies!=null)foreach(var m in copies)if(m!=null)UnityEngine.Object.DestroyImmediate(m);if(proxies!=null)foreach(var m in proxies)if(m!=null)UnityEngine.Object.DestroyImmediate(m);}
        }
        struct ExactNormalPosition:IEquatable<ExactNormalPosition>
        {
            readonly int x,y,z;public ExactNormalPosition(Vector3 p){x=BitConverter.SingleToInt32Bits(p.x);y=BitConverter.SingleToInt32Bits(p.y);z=BitConverter.SingleToInt32Bits(p.z);}
            public bool Equals(ExactNormalPosition p)=>x==p.x&&y==p.y&&z==p.z;public override bool Equals(object value)=>value is ExactNormalPosition p&&Equals(p);public override int GetHashCode()=>unchecked((x*397^y)*397^z);
        }
        static bool SameNormalBits(Vector3 a,Vector3 b)=>new ExactNormalPosition(a).Equals(new ExactNormalPosition(b));
        static WeldedNormalGroup[] LastWeldedNormalBuildGroups=Array.Empty<WeldedNormalGroup>();
        static bool NormalTranslationFrame(Matrix4x4 m)
        {for(int row=0;row<4;row++)for(int col=0;col<4;col++){if(row<3&&col==3){if(!float.IsFinite(m[row,col]))return false;continue;}if(m[row,col]!=(row==col?1:0))return false;}return true;}
        // Creates disposable copies only. Unity owns normal generation; the
        // adapter only supplies exact shared positions and index correspondence.
        public static WeldedNormalCopies BuildNativeWeldedNormalCopies(Mesh[] originals,Matrix4x4[] frames,int[] lods)
        {
            LastWeldedNormalBuildGroups=Array.Empty<WeldedNormalGroup>();
            if(originals==null||originals.Length==0||frames==null||lods==null||frames.Length!=originals.Length||lods.Length!=originals.Length||originals.Any(m=>m==null||m.vertexCount==0)||frames.Any(m=>!NormalTranslationFrame(m))||lods.Any(l=>l<0||l>2))throw new InvalidOperationException("Actual populated native meshes with matching per-LOD translation-only frames required; no coordinate tolerance or transformed-normal approximation.");
            var result=new WeldedNormalCopies{copies=new Mesh[originals.Length]};var proxies=new List<Mesh>();var groups=new List<WeldedNormalGroup>();
            try
            {
                foreach(int lod in lods.Distinct().OrderBy(v=>v))
                {
                    int[] members=Enumerable.Range(0,originals.Length).Where(i=>lods[i]==lod).ToArray();var unique=new Dictionary<ExactNormalPosition,int>();var positions=new List<Vector3>();var faces=new List<int>();var map=new Dictionary<int,int[]>();var sourcePositions=new Dictionary<int,Vector3[]>();
                    foreach(int index in members)
                    {
                        var vertices=originals[index].vertices;sourcePositions.Add(index,vertices);var correspondence=new int[vertices.Length];map.Add(index,correspondence);
                        for(int v=0;v<vertices.Length;v++){Vector3 p=frames[index].MultiplyPoint3x4(vertices[v]);if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))throw new InvalidOperationException("Finite actual native proxy positions required.");var key=new ExactNormalPosition(p);if(!unique.TryGetValue(key,out int at)){at=positions.Count;positions.Add(p);unique.Add(key,at);}correspondence[v]=at;}
                        for(int submesh=0;submesh<originals[index].subMeshCount;submesh++){var indices=originals[index].GetIndices(submesh);if(originals[index].GetTopology(submesh)!=MeshTopology.Triangles||indices.Length%3!=0||indices.Any(i=>i<0||i>=vertices.Length))throw new InvalidOperationException("Complete original oriented native triangle indices required.");foreach(int vertex in indices)faces.Add(correspondence[vertex]);}
                    }
                    var proxy=new Mesh{name="Temporary exact-position native normal proxy LOD"+lod,hideFlags=HideFlags.HideAndDontSave,indexFormat=IndexFormat.UInt32};proxies.Add(proxy);result.proxies=proxies.ToArray();proxy.SetVertices(positions);proxy.SetTriangles(faces,0);proxy.RecalculateNormals();
                    var generatedPositions=proxy.vertices;var generatedIndices=proxy.triangles;var normals=proxy.normals;var used=new bool[positions.Count];foreach(int index in faces)used[index]=true;var row=new WeldedNormalGroup{lod=lod,inputMeshes=members.Length,inputVertices=members.Sum(i=>originals[i].vertexCount),uniqueProxyVertices=positions.Count,triangles=faces.Count/3,unusedProxyVertices=used.Count(v=>!v),unityRecalculateNormalsCalled=true,proxyPositionsAndOrientedTrianglesExact=generatedPositions.Length==positions.Count&&generatedPositions.Where((p,i)=>!SameNormalBits(p,positions[i])).Count()==0&&generatedIndices.SequenceEqual(faces),allCopyNonNormalAttributesExact=true,allCopiedNormalsMatchNativeProxy=true};row.exactDuplicateVerticesWelded=row.inputVertices-row.uniqueProxyVertices;groups.Add(row);result.groups=groups.ToArray();LastWeldedNormalBuildGroups=result.groups;
                    if(!row.proxyPositionsAndOrientedTrianglesExact||normals.Length!=positions.Count)throw new InvalidOperationException("Unity proxy normal calculation changed original proxy position bits/triangle order or produced incomplete normals.");
                    for(int at=0;at<normals.Length;at++){var n=normals[at];float length=n.sqrMagnitude;if(!float.IsFinite(length)||Mathf.Abs(length-1)>.00001f){row.invalidNormalProxyIndex=at;row.invalidNormalProxyPosition=positions[at];row.invalidNormalValue=n;throw new InvalidOperationException("Native proxy normals must be finite unit vectors; unused/cancelling geometry cannot be silently substituted. LOD="+lod+", vertex="+at+", position="+positions[at].ToString("R")+", normal="+n.ToString("R")+", unused="+row.unusedProxyVertices);}row.maximumUnitSquaredError=Mathf.Max(row.maximumUnitSquaredError,Mathf.Abs(length-1));}
                    foreach(int index in members)
                    {
                        var assigned=new Vector3[map[index].Length];for(int v=0;v<assigned.Length;v++){if(!SameNormalBits(generatedPositions[map[index][v]],frames[index].MultiplyPoint3x4(sourcePositions[index][v])))throw new InvalidOperationException("Original corner lost exact native-proxy position correspondence.");assigned[v]=normals[map[index][v]];}
                        var copy=UnityEngine.Object.Instantiate(originals[index]);copy.name=originals[index].name+" temporary Unity welded normals";copy.hideFlags=HideFlags.HideAndDontSave;result.copies[index]=copy;copy.normals=assigned;
                        var proof=DragonKingdomNativeFaceGeometry.Compare(originals[index],copy);row.allCopyNonNormalAttributesExact&=proof.geometryExact&&proof.nonNormalAttributesExact;row.allCopiedNormalsMatchNativeProxy&=copy.normals.Length==assigned.Length&&copy.normals.Where((n,i)=>!SameNormalBits(n,assigned[i])).Count()==0;if(!proof.originalNormalAwareCanonicalExact)result.changedNormalMeshes++;
                        if(!row.allCopyNonNormalAttributesExact||!row.allCopiedNormalsMatchNativeProxy||copy.bounds!=originals[index].bounds)throw new InvalidOperationException("Intended smooth-normal change altered original-layout geometry/UV/color/barycentric/splat/submesh data or failed exact Unity proxy mapping: "+JsonUtility.ToJson(proof));
                    }
                }
                return result;
            }
            catch{result.Dispose();throw;}
        }
        static void CaptureCaveWeldedNormalDiagnostic(Camera camera,DragonKingdomWorld world,HashSet<Collider> roads,Shader shader,string output,RoundedCavernFrames proof,MeshRenderer[] renderers)
        {
            var filters=renderers.Select(r=>r.GetComponent<MeshFilter>()).ToArray();var originals=filters.Select(f=>f.sharedMesh).ToArray();var frames=filters.Select(f=>f.transform.localToWorldMatrix).ToArray();var levels=filters.Select(f=>{var chunk=f.GetComponent<Digger.Modules.Core.Sources.ChunkObject>();var group=f.GetComponentInParent<Digger.Modules.Core.Sources.ChunkLODGroup>();int level=f.transform.GetSiblingIndex();if(chunk==null||group==null||level>=group.LODCount)throw new InvalidOperationException("Actual native saved chunk LOD identity required.");return level;}).ToArray();
            var filterPoses=filters.ToDictionary(f=>f,f=>EditorJsonUtility.ToJson(f.transform));var rendererJson=renderers.ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r));var materialBindings=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);var materials=materialBindings.Values.SelectMany(v=>v).Where(m=>m!=null).Distinct().ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));var colliderBindings=world.GetComponentsInChildren<MeshCollider>(true).ToDictionary(c=>c,c=>c.sharedMesh);var colliderJson=colliderBindings.Keys.ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));var lodJson=world.GetComponentsInChildren<Digger.Modules.Core.Sources.DiggerSystem>(true).SelectMany(s=>s.GetComponentsInChildren<LODGroup>(true)).ToDictionary(g=>g,g=>EditorJsonUtility.ToJson(g));var lightJson=world.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToDictionary(l=>l,l=>EditorJsonUtility.ToJson(l)+EditorJsonUtility.ToJson(l.transform));string cameraJson=EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform);
            bool Restored()=>filters.Where((f,i)=>f==null||f.sharedMesh!=originals[i]||filterPoses[f]!=EditorJsonUtility.ToJson(f.transform)).Count()==0&&rendererJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)&&p.Key.sharedMaterials.SequenceEqual(materialBindings[p.Key]))&&materials.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key))&&colliderBindings.All(p=>p.Key!=null&&p.Key.sharedMesh==p.Value&&colliderJson[p.Key]==EditorJsonUtility.ToJson(p.Key))&&lodJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key))&&lightJson.All(p=>p.Key!=null&&p.Value==EditorJsonUtility.ToJson(p.Key)+EditorJsonUtility.ToJson(p.Key.transform))&&cameraJson==EditorJsonUtility.ToJson(camera)+EditorJsonUtility.ToJson(camera.transform);
            WeldedNormalCopies temporary=null;var paired=new List<Frame>();proof.weldedNormalDiagnosticPerformed=true;
            try
            {
                try{temporary=BuildNativeWeldedNormalCopies(originals,frames,levels);}catch{proof.weldedNormalGroups=LastWeldedNormalBuildGroups;throw;}proof.weldedNormalGroups=temporary.groups;proof.weldedNormalMeshes=temporary.copies.Length;proof.weldedNormalChangedMeshes=temporary.changedNormalMeshes;if(temporary.changedNormalMeshes==0)throw new InvalidOperationException("Meaningful native normal diagnosis requires an actual stored-normal change.");for(int i=0;i<filters.Length;i++)filters[i].sharedMesh=temporary.copies[i];
                var smooth=Capture(camera,world,roads,shader,output,"rounded-entry-welded-normal-diagnostic","unity-exact-welded-normals");paired.Add(smooth);proof.weldedNormalFrames=paired.ToArray();if(smooth.shaderErrors.Length!=0)throw new InvalidOperationException("Actual Digger shader errors in native welded-normal diagnosis.");
            }
            finally
            {
                for(int i=0;i<filters.Length;i++)if(filters[i]!=null)filters[i].sharedMesh=originals[i];temporary?.Dispose();proof.weldedNormalTemporaryObjectsDestroyed=temporary==null||temporary.copies.All(m=>m==null)&&temporary.proxies.All(m=>m==null);proof.weldedNormalStateRestored=Restored();
            }
            if(!proof.weldedNormalTemporaryObjectsDestroyed||!proof.weldedNormalStateRestored)throw new InvalidOperationException("Native welded-normal diagnosis failed exact mesh/material/renderer/physics/LOD/light/camera restoration or temporary cleanup.");var restored=Capture(camera,world,roads,shader,output,"rounded-entry-welded-normal-diagnostic","original-normals-restored");paired.Add(restored);proof.weldedNormalFrames=paired.ToArray();proof.weldedNormalStateRestored&=Restored();if(restored.shaderErrors.Length!=0||!proof.weldedNormalStateRestored)throw new InvalidOperationException("Restored native-normal pair changed original state or reported shader errors.");
        }
    }
}
#endif
