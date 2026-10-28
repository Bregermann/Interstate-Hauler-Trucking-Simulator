using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckCamping.Editor
{
    // Explicit render-only repair. No callbacks/build hooks, water simulation,
    // physics, terrain, original timber asset or vendor source are changed.
    public static class CampingWaterPresentationAuthoring
    {
        public const string ReviewedWorldSha = "a8448bc181521b1f01734f143c0bbf108893636e47324d44c65875b7574e0cdd";
        const string ReviewedMetaSha = "e49c815a758743e10c9aa4ac9878f61d3af1da5adf8aca016dca291ae5001ff1";
        public const string Timber = "Assets/TruckCamping/Models/Camp Haul bound rounded timber.asset";
        public const string OldWater = "Assets/TruckCamping/Materials/Camp Haul River Modeler flowing water.mat";
        public const string FishingFoundation = "Assets/TruckFishing/Materials/Clear fishing river.mat";
        public const string RiverFoundation = "Assets/RiverModeler/ShaderGraph/Default River.mat";
        public const string WaveNormal = "Assets/RiverModeler/Textures/WaveNormal.png";
        public const string CascadeFoam = "Assets/RiverModeler/Textures/WaterFoam.png";
        public const string CascadeLook = "flowing-foam";
        public const bool CascadeLookReviewed = true; // R56: lead inspected actual720/1080 comparison; selected flowing foam.
        public const string WaterMesh = "Assets/TruckCamping/Models/Camp Haul water-only UV tangent surface.asset";
        public const string LakeMaterial = "Assets/TruckCamping/Materials/Camp Haul calm lake water.mat";
        public const string CreekMaterial = "Assets/TruckCamping/Materials/Camp Haul calm creek water.mat";
        public const string CascadeMaterial = "Assets/TruckCamping/Materials/Camp Haul clear cascade water.mat";
        static readonly string[] Inputs = { Timber, OldWater, FishingFoundation, RiverFoundation, WaveNormal, CascadeFoam };
        static readonly string[] InputHashes = { "356086f4a7a8e22fa8579ade86394a65283e3b8470a0e47308f2aabfad84e2ae", "9a15ea778228f7a2d3c2878c674c8e215a76fdcabc062be76fff00be3586afa7", "0ba1bae8a23a0c25c6f1371ebcd4111287c5b7e9a5b2e314d04768674d606bfd", "da19c5c6ad9439e5a91decd34728a53c188dab1625390a7ab92b7fda38cda2f2", "00faaa24a1b700fab3db532db41219ca99ff0a064832b5efffc80d974f36d481", "db689d3092a65387239247782d49864ecbc26bc05c0baf3344624ca1bf827aa3" };
        static readonly string[] InputMetaHashes = { "f0c6a82db67dd41ae003364c9fa59cb0d7395123fbbc5b773d671afd5c281c64", "da0215012e157c0884f7656ab81f6263aa0d66bc4db745a997243464803753e6", "c137dacefd8ec67b617ca2daf3321ba402d568293393a92a3f042ea5b40d335f", "de7b8853a26327909a0ca308d95636a4b93e0e03cbd0b92999fd2184a5468ea3", "2035f1cb767dff3be4a975e2816df76f7ddd013a23f665d5e65cfdba5ce791ed", "c877afbb173e54ba1f999a35cf24e07e0d984f74c0a8cf2a4bd677238163385f" };
        static readonly string[] Outputs = { WaterMesh, LakeMaterial, CreekMaterial, CascadeMaterial };
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        [Serializable] sealed class Change { public long id; public int type; public string name, kind, before, after; }
        [Serializable] sealed class Fact { public string path, sha256; }
        [Serializable] sealed class Receipt
        {
            public string status, asset, beforeSha256, afterSha256, metaSha256, beforeFile, afterFile;
            public int waters, changedDocuments, originalTrees;
            public Fact[] retainedInputs, outputs;
            public Change[] changes;
        }
        [Serializable] sealed class MeshFact { public int vertices, triangles, topTriangles, degenerateTopUvTriangles, tangentCount; public float minimumUvArea; }
        static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException("Camping water-only rendering preflight: " + message + "; custom content preserved."); }
        public static string Digest(string path)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        static void KnownInputs()
        {
            for (int i = 0; i < Inputs.Length; i++)
            {
                string metaHash=Digest(Inputs[i]+".meta");
                bool exactMeta=metaHash==InputMetaHashes[i];
                // The pre-existing validation copy of this one vendor importer
                // uses CRLF. Both actual byte hashes and LF-normalized original
                // content were reviewed after R57 refused before any writes.
                // Never rewrite vendor bytes or accept other unknown importers.
                if (!exactMeta && Inputs[i]==CascadeFoam &&
                    metaHash=="2c7bd140d7c303be02d1735322c713770c33c43e2c5dbd13b8a8eb5bbff1a56d" &&
                    InputMetaHashes[i]=="c877afbb173e54ba1f999a35cf24e07e0d984f74c0a8cf2a4bd677238163385f")
                {
                    byte[] original=Utf8.GetBytes(File.ReadAllText(Inputs[i]+".meta").Replace("\r\n","\n"));
                    using(var sha=SHA256.Create())exactMeta=BitConverter.ToString(sha.ComputeHash(original)).Replace("-","").ToLowerInvariant()==InputMetaHashes[i];
                }
                Require(Digest(Inputs[i])==InputHashes[i] && exactMeta,"exact original material/mesh/vendor input "+Inputs[i]);
            }
        }
        public static string Kind(string name)
        {
            if (name == "Flowing reflective Willow Lake") return "lake";
            if (name.StartsWith("Flowing connected mountain creek ", StringComparison.Ordinal)) return "creek";
            if (name == "River Modeler flowing silver cascade") return "cascade";
            throw new InvalidOperationException("Unknown water renderer name: " + name);
        }
        public static MeshFilter[] WaterFilters(GameObject world)
        {
            var names = new[] { "Flowing reflective Willow Lake", "River Modeler flowing silver cascade" }.Concat(Enumerable.Range(0, 17).Select(i => "Flowing connected mountain creek " + i)).ToArray();
            var result = world.GetComponentsInChildren<MeshFilter>(true).Where(f => names.Contains(f.name)).OrderBy(f => f.name, StringComparer.Ordinal).ToArray();
            Require(result.Length == 19 && result.Select(f => f.name).Distinct().Count() == 19, "exact original nineteen water renderers");
            return result;
        }
        static float UvArea(Vector2 a, Vector2 b, Vector2 c) => Mathf.Abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*.5f;
        public static string MeshDiagnostics(Mesh mesh)
        {
            var p = mesh.vertices; var uv = mesh.uv; var indices = mesh.triangles; int top = 0, badTop = 0; float minimum = float.PositiveInfinity;
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = indices[i], b = indices[i+1], c = indices[i+2]; float area = UvArea(uv[a],uv[b],uv[c]); minimum = Mathf.Min(minimum,area);
                if (p[a].y > .499f && p[b].y > .499f && p[c].y > .499f) { top++; if(area <= 1e-9f) badTop++; }
            }
            return JsonUtility.ToJson(new MeshFact { vertices=p.Length,triangles=indices.Length/3,topTriangles=top,degenerateTopUvTriangles=badTop,tangentCount=mesh.tangents.Length,minimumUvArea=minimum });
        }
        public static Mesh CreateTransientWaterMesh(Mesh source)
        {
            Require(source != null && source.isReadable && source.subMeshCount == 1 && source.GetTopology(0) == MeshTopology.Triangles, "readable original triangle mesh");
            var positions=source.vertices; var normals=source.normals; var triangles=source.triangles;
            Require(positions.Length == 34 && normals.Length == positions.Length && source.tangents.Length == 0 && source.colors.Length == 0 && source.bindposes.Length == 0 && source.boneWeights.Length == 0, "reviewed original timber channels");
            for(int channel=1;channel<8;channel++){var values=new List<Vector4>();source.GetUVs(channel,values);Require(values.Count==0,"unsupported extra source UV channel");}
            var v=new Vector3[triangles.Length];var n=new Vector3[v.Length];var uv=new Vector2[v.Length];
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=positions[triangles[i]];var b=positions[triangles[i+1]];var c=positions[triangles[i+2]];
                var face=Vector3.Cross(b-a,c-a);Require(face.sqrMagnitude>1e-12f,"nondegenerate original triangle");
                var absolute=new Vector3(Mathf.Abs(face.x),Mathf.Abs(face.y),Mathf.Abs(face.z));
                for(int j=0;j<3;j++)
                {
                    int at=i+j;v[at]=positions[triangles[at]];n[at]=normals[triangles[at]];
                    // Dominant-plane box UVs with explicit per-triangle seams.
                    // Geometry/normals/winding stay exact; caps no longer map
                    // every vertex onto the single V=1 texture scanline.
                    uv[at]=absolute.y>=absolute.x&&absolute.y>=absolute.z?new Vector2(v[at].x+.5f,v[at].z+.5f):
                        absolute.z>=absolute.x?new Vector2(v[at].x+.5f,v[at].y+.5f):new Vector2(v[at].z+.5f,v[at].y+.5f);
                }
                Require(UvArea(uv[i],uv[i+1],uv[i+2])>1e-9f,"valid derived UV triangle");
            }
            var mesh=new Mesh { name="Camp Haul WATER ONLY valid UV tangent surface" };mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.SetTriangles(Enumerable.Range(0,v.Length).ToArray(),0);mesh.RecalculateTangents();mesh.bounds=source.bounds;
            ValidateDerivedMesh(source,mesh);return mesh;
        }
        public static void ValidateDerivedMesh(Mesh source,Mesh derived)
        {
            Require(source!=null&&derived!=null&&source!=derived,"separate owned derivative, never shared timber");
            var p=source.vertices;var n=source.normals;var ti=source.triangles;var v=derived.vertices;var dn=derived.normals;var dt=derived.triangles;var uv=derived.uv;var tangents=derived.tangents;
            Require(derived.subMeshCount==1&&v.Length==ti.Length&&dt.Length==ti.Length&&dn.Length==v.Length&&uv.Length==v.Length&&tangents.Length==v.Length,"bounded complete derived streams");
            for(int i=0;i<v.Length;i++)
            {
                Require(v[i].Equals(p[ti[i]])&&dn[i].Equals(n[ti[i]])&&dt[i]==i,"every original triangle position/normal/winding exact");
                var t=tangents[i];Require(!float.IsNaN(t.x)&&!float.IsInfinity(t.x)&&!float.IsNaN(t.y)&&!float.IsInfinity(t.y)&&!float.IsNaN(t.z)&&!float.IsInfinity(t.z)&&Mathf.Abs(t.w)==1,"finite actual tangent handedness");
                var tangent=new Vector3(t.x,t.y,t.z);Require(Mathf.Abs(tangent.magnitude-1)<1e-4f&&Mathf.Abs(Vector3.Dot(tangent,dn[i]))<1e-4f,"unit orthogonal normal-map frame");
                if(i%3==0)Require(UvArea(uv[i],uv[i+1],uv[i+2])>1e-9f,"all derived UV triangles have real area");
            }
            Require(derived.bounds.Equals(source.bounds),"original render bounds exact");
        }
        static string MaterialPath(string kind) => kind=="lake"?LakeMaterial:kind=="creek"?CreekMaterial:kind=="cascade"?CascadeMaterial:throw new InvalidOperationException("Unknown water kind");
        public static Material CreateTransientWaterMaterial(string kind)
        {
            if(kind=="cascade")return CreateTransientCascadeCandidate(CascadeLook);
            var source=AssetDatabase.LoadAssetAtPath<Material>(FishingFoundation);var river=AssetDatabase.LoadAssetAtPath<Material>(RiverFoundation);var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(WaveNormal);
            Require(source!=null&&source.shader.name=="Universal Render Pipeline/Lit"&&river!=null&&normal!=null&&river.GetTexture("_BumpMap")==normal&&source.GetTexture("_BumpMap")==normal,"actual working Fishing adapter and installed River Modeler WaveNormal foundation");
            var material=new Material(source) { name="Camp Haul calm "+kind+" water" };
            Vector2 size=kind=="lake"?new Vector2(79,63):kind=="creek"?new Vector2(7,11):kind=="cascade"?new Vector2(5,13):throw new InvalidOperationException("Unknown water kind");
            // Reuse the working adapter's 4.35m normal repeat in the respective
            // horizontal/vertical surface axes. The source/vendor bytes stay exact.
            material.SetTexture("_BaseMap",null);material.SetTexture("_MainTex",null);material.SetTextureScale("_BaseMap",size*.23f);material.SetTextureScale("_MainTex",size*.23f);
            material.SetTexture("_BumpMap",normal);material.SetFloat("_Cull",2);return material;
        }
        public static Material CreateTransientCascadeCandidate(string look)
        {
            Require(look=="legacy-dark"||look=="pale-cyan"||look=="flowing-foam","explicit known cascade comparison choice");
            var source=AssetDatabase.LoadAssetAtPath<Material>(FishingFoundation);var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(WaveNormal);
            Require(source!=null&&normal!=null&&source.GetTexture("_BumpMap")==normal,"actual unchanged Fishing/River Modeler foundation");
            var material=new Material(source) { name="Camp Haul cascade comparison "+look };
            material.SetTexture("_BaseMap",null);material.SetTexture("_MainTex",null);material.SetTexture("_BumpMap",normal);material.SetFloat("_Cull",2);
            var tiling=new Vector2(5,13)*.23f;
            if(look!="legacy-dark")
            {
                material.SetFloat("_Cull",0); // thin falling sheet readable from both sides
                Color tint=look=="pale-cyan"?new Color(.62f,.86f,.94f,.86f):new Color(.86f,.96f,1,.92f);
                material.SetColor("_BaseColor",tint);material.SetColor("_Color",tint);material.SetFloat("_Metallic",0);
                material.SetFloat("_Smoothness",look=="pale-cyan"?.45f:.36f);material.SetFloat("_BumpScale",look=="pale-cyan"?.12f:.1f);
                if(look=="flowing-foam")
                {
                    var foam=AssetDatabase.LoadAssetAtPath<Texture2D>(CascadeFoam);Require(foam!=null,"actual continuous vendor foam texture, not splash flipbook");
                    material.SetTexture("_BaseMap",foam);material.SetTexture("_MainTex",foam);tiling=new Vector2(1.5f,4);
                }
            }
            // No emissive lighting/shader swap, new lights, receive-shadow
            // change or physics. These are cascade-only albedo/alpha/roughness.
            material.SetTextureScale("_BaseMap",tiling);material.SetTextureScale("_MainTex",tiling);return material;
        }
        static void ValidateMaterial(Material material,string kind)
        {
            var expected=CreateTransientWaterMaterial(kind);
            try
            {
                Require(material!=null&&material.shader==expected.shader&&material.shader.isSupported,"supported original URP foundation");
                Require(material.GetTexture("_BaseMap")==expected.GetTexture("_BaseMap")&&material.GetTexture("_BumpMap")==expected.GetTexture("_BumpMap")&&material.GetColor("_BaseColor").Equals(expected.GetColor("_BaseColor")),"exact reviewed mode-owned material recipe");
                foreach(string property in new[]{"_Smoothness","_BumpScale","_Metallic","_Surface","_SrcBlend","_DstBlend","_ZWrite","_ReceiveShadows"})Require(material.GetFloat(property)==expected.GetFloat(property),"reviewed surface property "+property);
                Require(material.IsKeywordEnabled("_NORMALMAP")&&material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")&&material.renderQueue==3000&&material.GetFloat("_Cull")==expected.GetFloat("_Cull"),"transparent surface with reviewed per-kind culling");
                Require(material.GetTextureScale("_BaseMap").Equals(expected.GetTextureScale("_BaseMap")),"reviewed physical repeat or cascade foam repeat");
            }
            finally { UnityEngine.Object.DestroyImmediate(expected); }
        }
        static Match Document(string text,int type,long id)
        { var doc=Regex.Match(text,@"(?m)^--- !u!"+type+" &"+id+@"\r?$[\s\S]*?(?=^--- !u!|\z)");Require(doc.Success,"exact owned native component document");return doc; }
        static long Id(UnityEngine.Object value)
        {Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id)&&guid==AssetDatabase.AssetPathToGUID(CampingWilderness.SharedPrefab),"persistent original water component identity");return id;}
        public static string RewriteWaterReference(string document,bool mesh,string oldGuid,string newGuid)
        {
            string pattern=mesh?@"(?m)^  m_Mesh: \{fileID: 4300000, guid: "+oldGuid+@", type: 2\}\r?$":@"(?m)^  m_Materials:\r?\n  - \{fileID: 2100000, guid: "+oldGuid+@", type: 2\}\r?$";
            var match=Regex.Match(document,pattern);Require(match.Success&&Regex.Matches(document,pattern).Count==1,"single known original water reference");
            string changed=match.Value.Replace(oldGuid,newGuid);return document.Remove(match.Index,match.Length).Insert(match.Index,changed);
        }
        static Fact[] Snapshot(IEnumerable<string> paths) => paths.Select(p=>new Fact { path=p,sha256=Digest(p) }).ToArray();
        public static void InstallKnownWaterUvAndVendorSurface()
        {
            Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped compiled explicit author only");string path=CampingWilderness.SharedPrefab;
            Require(CascadeLookReviewed,"R55 rejected dark cascade; a specific native-reviewed cascade choice must be frozen before production author");
            Require(Digest(path)==ReviewedWorldSha&&Digest(path+".meta")==ReviewedMetaSha,"exact R52 world and GUID; customized/already repaired world refused");KnownInputs();
            foreach(string output in Outputs)Require(!File.Exists(output)&&!File.Exists(output+".meta"),"new owned output path must be absent "+output);
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(path);Require(world!=null,"existing shared world");var filters=WaterFilters(world);var timber=AssetDatabase.LoadAssetAtPath<Mesh>(Timber);var oldMaterial=AssetDatabase.LoadAssetAtPath<Material>(OldWater);
            foreach(var filter in filters)Require(filter.sharedMesh==timber&&filter.GetComponent<MeshRenderer>().sharedMaterials.Length==1&&filter.GetComponent<MeshRenderer>().sharedMaterial==oldMaterial,"known original water binding "+filter.name);
            byte[] before=File.ReadAllBytes(path),meta=File.ReadAllBytes(path+".meta");
            string originalText=Utf8.GetString(before);string timberGuid=AssetDatabase.AssetPathToGUID(Timber),waterGuid=AssetDatabase.AssetPathToGUID(OldWater);
            foreach(var filter in filters)foreach(bool mesh in new[]{true,false})
            {
                long id=Id(mesh?(UnityEngine.Object)filter:filter.GetComponent<MeshRenderer>());var doc=Document(originalText,mesh?33:23,id);
                Require(RewriteWaterReference(doc.Value,mesh,mesh?timberGuid:waterGuid,mesh?timberGuid:waterGuid)==doc.Value,"all38 reference rewrites preflight BEFORE asset creation");
            }
            var retained=Inputs.SelectMany(p=>new[]{p,p+".meta"}).Concat(world.GetComponent<CampingWilderness>().terrains.SelectMany(t=>new[]{AssetDatabase.GetAssetPath(t.terrainData),AssetDatabase.GetAssetPath(t.terrainData)+".meta"})).Distinct().ToArray();var retainedFacts=Snapshot(retained);
            Mesh derived=CreateTransientWaterMesh(timber);var materials=new Dictionary<string,Material>();foreach(string kind in new[]{"lake","creek","cascade"}){var material=CreateTransientWaterMaterial(kind);ValidateMaterial(material,kind);materials.Add(kind,material);}
            string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingWaterRenderRepair/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            string original=Path.Combine(folder,"original.prefab"),afterFile=Path.Combine(folder,"water-render.prefab");File.WriteAllBytes(original,before);File.WriteAllBytes(original+".meta",meta);
            var receipt=new Receipt { status="PREFLIGHT_COMPLETE_BEFORE_NEW_ASSETS_OR_WORLD_MUTATION",asset=path,beforeSha256=Digest(path),metaSha256=Digest(path+".meta"),beforeFile=original,afterFile=afterFile,waters=19,changedDocuments=38,originalTrees=world.GetComponent<CampingWilderness>().terrains.Sum(t=>t.terrainData.treeInstanceCount),retainedInputs=retainedFacts };
            string receiptPath=Path.Combine(folder,"receipt.json");File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true),Utf8);
            AssetDatabase.CreateAsset(derived,WaterMesh);AssetDatabase.SaveAssetIfDirty(derived);
            foreach(var pair in materials){AssetDatabase.CreateAsset(pair.Value,MaterialPath(pair.Key));AssetDatabase.SaveAssetIfDirty(pair.Value);}
            Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(derived,out string ignoredMeshGuid,out long meshFileId)&&meshFileId==4300000,"new main mesh identity");
            foreach(var material in materials.Values)Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material,out string ignoredMatGuid,out long matFileId)&&matFileId==2100000,"new main material identity");
            string text=Utf8.GetString(before);var changes=new List<Change>();string oldMeshGuid=AssetDatabase.AssetPathToGUID(Timber),newMeshGuid=AssetDatabase.AssetPathToGUID(WaterMesh),oldMatGuid=AssetDatabase.AssetPathToGUID(OldWater);
            foreach(var filter in filters)
            {
                string kind=Kind(filter.name);var renderer=filter.GetComponent<MeshRenderer>();
                foreach(bool mesh in new[]{true,false})
                {
                    long id=Id(mesh?(UnityEngine.Object)filter:renderer);int type=mesh?33:23;var doc=Document(text,type,id);
                    string changed=RewriteWaterReference(doc.Value,mesh,mesh?oldMeshGuid:oldMatGuid,mesh?newMeshGuid:AssetDatabase.AssetPathToGUID(MaterialPath(kind)));
                    changes.Add(new Change { id=id,type=type,name=filter.name,kind=kind,before=doc.Value,after=changed });text=text.Remove(doc.Index,doc.Length).Insert(doc.Index,changed);
                }
            }
            string reverse=text;foreach(var change in changes){var doc=Document(reverse,change.type,change.id);Require(doc.Value==change.after,"reviewed water component");reverse=reverse.Remove(doc.Index,doc.Length).Insert(doc.Index,change.before);}
            Require(Utf8.GetBytes(reverse).SequenceEqual(before),"EVERY non-water reference byte preserved");byte[] after=Utf8.GetBytes(text);File.WriteAllBytes(afterFile,after);
            receipt.afterSha256=HashBytes(after);receipt.changes=changes.ToArray();receipt.outputs=Snapshot(Outputs.SelectMany(p=>new[]{p,p+".meta"}));receipt.status="NEW_OWNED_WATER_ASSETS_READY_BEFORE_WORLD_REFERENCE_MUTATION";File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true),Utf8);
            Require(File.ReadAllBytes(path).SequenceEqual(before)&&File.ReadAllBytes(path+".meta").SequenceEqual(meta),"unchanged original world race guard");foreach(var fact in retainedFacts)Require(Digest(fact.path)==fact.sha256,"retained input changed "+fact.path);
            File.WriteAllBytes(path,after);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);Require(File.ReadAllBytes(path).SequenceEqual(after)&&File.ReadAllBytes(path+".meta").SequenceEqual(meta),"exact water-only reference output/meta");
            foreach(var fact in retainedFacts)Require(Digest(fact.path)==fact.sha256,"retained terrain/tree/vendor input changed across import "+fact.path);
            receipt.status="ONLY_19_WATER_MESH_AND_19_MATERIAL_REFERENCES_CHANGED_ALL_OTHER_WORLD_BYTES_EXACT";File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true),Utf8);VerifyWaterDescendsFrom(ReviewedWorldSha);Debug.Log("CAMPING_WATER_RENDER_REPAIR "+JsonUtility.ToJson(receipt,true));
        }
        static string HashBytes(byte[] value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","").ToLowerInvariant();}
        public static string VerifyWaterDescendsFrom(string exactPreviousSha256)
        {
            Require(exactPreviousSha256==ReviewedWorldSha,"explicit exact R52 predecessor required");KnownInputs();
            string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/CampingWaterRenderRepair");string record=Directory.GetFiles(folder,"receipt.json",SearchOption.AllDirectories).OrderBy(File.GetLastWriteTimeUtc).Last();var r=JsonUtility.FromJson<Receipt>(File.ReadAllText(record,Utf8));
            Require(r.status=="ONLY_19_WATER_MESH_AND_19_MATERIAL_REFERENCES_CHANGED_ALL_OTHER_WORLD_BYTES_EXACT"&&r.asset==CampingWilderness.SharedPrefab&&r.waters==19&&r.changedDocuments==38&&r.originalTrees==2297&&r.beforeSha256==ReviewedWorldSha&&Digest(r.beforeFile)==ReviewedWorldSha,"strict completed known R52 water-only chain");
            Require(Digest(r.asset)==r.afterSha256&&Digest(r.afterFile)==r.afterSha256&&Digest(r.asset+".meta")==ReviewedMetaSha&&Digest(r.beforeFile+".meta")==ReviewedMetaSha,"current/retained after/meta exact");
            Require(r.changes.Length==38&&r.changes.Select(c=>c.id).Distinct().Count()==38,"exact38 water component identities");Require(r.outputs.Length==8&&r.outputs.Select(f=>f.path).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(Outputs.SelectMany(p=>new[]{p,p+".meta"}).OrderBy(x=>x,StringComparer.Ordinal)),"exact four owned outputs and their original metadata");foreach(var fact in r.outputs)Require(Digest(fact.path)==fact.sha256,"owned generated water asset/meta exact");
            // Terrain prototype upgrades may be independently reviewed later;
            // they are not prefab mutations. The author itself checks all four
            // TerrainData bytes stay exact across this render-only operation.
            string text=Utf8.GetString(File.ReadAllBytes(r.asset));var asset=AssetDatabase.LoadAssetAtPath<GameObject>(r.asset);var filters=WaterFilters(asset);var source=AssetDatabase.LoadAssetAtPath<Mesh>(Timber);var derived=AssetDatabase.LoadAssetAtPath<Mesh>(WaterMesh);ValidateDerivedMesh(source,derived);
            foreach(var filter in filters)
            {
                string kind=Kind(filter.name);var renderer=filter.GetComponent<MeshRenderer>();Require(filter.sharedMesh==derived&&renderer.sharedMaterials.Length==1&&renderer.sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(kind)),"actual current water-only binding");ValidateMaterial(renderer.sharedMaterial,kind);
                foreach(bool mesh in new[]{true,false})
                {
                    long id=Id(mesh?(UnityEngine.Object)filter:renderer);var change=r.changes.Single(c=>c.id==id);Require(change.type==(mesh?33:23)&&change.name==filter.name&&change.kind==kind,"only actual known nineteen water renderers");
                    string expected=RewriteWaterReference(change.before,mesh,AssetDatabase.AssetPathToGUID(mesh?Timber:OldWater),AssetDatabase.AssetPathToGUID(mesh?WaterMesh:MaterialPath(kind)));
                    Require(change.after==expected,"only authorized mesh/material reference scalar");var doc=Document(text,change.type,id);Require(doc.Value==change.after,"exact current water component document");text=text.Remove(doc.Index,doc.Length).Insert(doc.Index,change.before);
                }
            }
            Require(Utf8.GetBytes(text).SequenceEqual(File.ReadAllBytes(r.beforeFile)),"all non-water-reference bytes, physics, ramp transforms and custom dressing exact");return r.status;
        }
    }
}
