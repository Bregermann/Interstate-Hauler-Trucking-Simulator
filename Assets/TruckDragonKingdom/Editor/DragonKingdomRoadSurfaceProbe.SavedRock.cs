#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Digger.Modules.Core.Sources;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSurfaceProbe
    {
        [Serializable]public sealed class CavernSavedRockMaterialsReceipt
        {
            public string scope="EXPLICIT_OWNED_CANDIDATE_DIGGER_MATERIAL_CONFIGURATION_NOT_FINAL_WORLD_RENDER_RUNTIME_OR_HUMAN_ACCEPTANCE",folder,geometryBefore,geometryAfter,failure;
            public string[] materials,originalMaterials,sourcePaths,sourceSha256;
            public int diggerSystems,rendererSlots;
            public bool originalSourcesExact,geometryExact,onlyApprovedInputs,providerAndRendererBindingsExact,savedPrefabBindingsExact;
            public List<SavedRockMismatch> mismatches=new List<SavedRockMismatch>();
        }
        [Serializable]public sealed class SavedRockMismatch
        {public string kind,path,expectedSha256,actualSha256,expectedContext,actualContext;public int firstDifference,expectedLength,actualLength;}
        [Serializable]public sealed class SavedRockPropertyDiagnostic
        {public string scope="READ_ONLY_RETAINED_NATIVE_MATERIAL_PAIR_PROPERTY_DIAGNOSIS_NOT_ACCEPTANCE",failure;public bool originalBytesExact;public int pairs,approvedPairs;public List<SavedRockMismatch> mismatches=new List<SavedRockMismatch>();}
        public static CavernSavedRockMaterialsReceipt LastSavedRockMaterials{get;private set;}
        internal static Terrain[] OwnedCavernMaterialCoreFields(DragonKingdomWorld world,string candidateFolder)=>DragonKingdomRegionalWorldIntegration.NativeFinishedOwnedCoreFields(world,candidateFolder);
        // Digger owns chunk creation and persistence. Its public Materials array
        // is the authority used by Chunk.CreateChunk and runtime system clones;
        // configure that array as well as the already-created chunk renderers.
        public static CavernSavedRockMaterialsReceipt ApplyOwnedCavernRockMaterials(DragonKingdomWorld world,string candidateFolder)
        {
            LastSavedRockMaterials=null;
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||DragonKingdomDiggerCavernAuthoring.IsBusy||world==null)throw new InvalidOperationException("Stopped compiled, fully completed owned Digger candidate required.");
            candidateFolder=candidateFolder.Replace('\\','/').TrimEnd('/');const string prefix="Assets/TruckDragonKingdom/VisualCandidates/";
            if(!candidateFolder.StartsWith(prefix,StringComparison.Ordinal)||candidateFolder.Substring(prefix.Length).Contains('/')||candidateFolder.Contains(".."))throw new InvalidOperationException("An explicit direct owned VisualCandidates folder is required.");
            // Palette copies are also owned assets below Terrain/. Reuse the
            // semantic core selection instead of treating ownership as a role.
            var terrains=OwnedCavernMaterialCoreFields(world,candidateFolder);
            var systems=world.GetComponentsInChildren<DiggerSystem>(true).Where(d=>d.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null)).ToArray();
            var renderers=systems.SelectMany(d=>d.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();if(systems.Length==0||renderers.Length==0||systems.Any(d=>d.Terrain==null||!terrains.Contains(d.Terrain)))throw new InvalidOperationException("Actual completed Digger chunk renderers required.");RequireCavernStandardLitNoPropertyBlocks(renderers);
            var originals=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();string dataPrefix="Assets/DiggerData/Scenes/DK-"+candidateFolder.Substring(prefix.Length)+"/";
            if(originals.Any(m=>m==null||!AssetDatabase.GetAssetPath(m).StartsWith(dataPrefix,StringComparison.Ordinal))||systems.Any(d=>d.Materials==null||d.Materials.Any(m=>!originals.Contains(m))))throw new InvalidOperationException("Only the exact fresh candidate's native Digger material providers may be configured.");
            string folder=candidateFolder+"/Materials/Reviewed cavern rock";if(Directory.Exists(folder)||File.Exists(folder+".meta"))throw new InvalidOperationException("Preserve existing/customized candidate materials; explicit absent output required.");
            var sourcePaths=originals.Select(AssetDatabase.GetAssetPath).Concat(new[]{CavernRoughRockDiffuse,CavernRoughRockNormal}).SelectMany(p=>new[]{p,p+".meta"}).ToArray();foreach(var p in sourcePaths)if(!File.Exists(IoPath(p)))throw new InvalidOperationException("Required original material/texture source missing: "+p);
            var proof=LastSavedRockMaterials=new CavernSavedRockMaterialsReceipt{folder=folder,diggerSystems=systems.Length,sourcePaths=sourcePaths,sourceSha256=sourcePaths.Select(p=>Hash(File.ReadAllBytes(IoPath(p)))).ToArray(),geometryBefore=OwnedCavernGeometrySignature(world,systems)};
            var diggerJson=systems.ToDictionary(d=>d,d=>EditorJsonUtility.ToJson(d));var rendererJson=renderers.ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r));var copies=new Dictionary<Material,Material>();
            var geometryObjects=world.GetComponentsInChildren<Component>(true).Where(c=>c is Transform||c is Collider||c is Rigidbody||c is LODGroup).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
            var geometryPaths=geometryObjects.Keys.ToDictionary(c=>c,c=>AnimationUtility.CalculateTransformPath(c.transform,world.transform));
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(var original in originals)
            {
                var copy=CreateCavernOwnedRoughRockMaterial(original);copy.hideFlags=HideFlags.None;copy.name="Kingdom cavern rock12m "+copies.Count;AssetDatabase.CreateAsset(copy,folder+"/"+copy.name+".mat");AssetDatabase.SaveAssetIfDirty(copy);copies.Add(original,copy);
            }
            proof.originalMaterials=copies.Keys.Select(AssetDatabase.GetAssetPath).ToArray();proof.materials=copies.Values.Select(AssetDatabase.GetAssetPath).ToArray();var ReplaceRefs=SavedRockMaterialReferenceRemapper(copies);
            foreach(var digger in systems){digger.Materials=digger.Materials.Select(m=>copies[m]).ToArray();EditorUtility.SetDirty(digger);}
            foreach(var renderer in renderers){proof.rendererSlots+=renderer.sharedMaterials.Length;renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>copies[m]).ToArray();EditorUtility.SetDirty(renderer);}
            proof.onlyApprovedInputs=copies.All(p=>CavernOwnedRockPropertiesExact(p.Key,p.Value));proof.providerAndRendererBindingsExact=diggerJson.All(p=>ReplaceRefs(p.Value)==EditorJsonUtility.ToJson(p.Key))&&rendererJson.All(p=>ReplaceRefs(p.Value)==EditorJsonUtility.ToJson(p.Key));
            proof.geometryAfter=OwnedCavernGeometrySignature(world,systems);proof.geometryExact=proof.geometryBefore==proof.geometryAfter;proof.originalSourcesExact=sourcePaths.Select((p,i)=>Hash(File.ReadAllBytes(IoPath(p)))==proof.sourceSha256[i]).All(v=>v);
            if(!proof.onlyApprovedInputs)foreach(var pair in copies.Where(p=>!CavernOwnedRockPropertiesExact(p.Key,p.Value)))RecordSavedRockMaterialMismatch(pair.Key,pair.Value,proof.mismatches);
            if(!proof.providerAndRendererBindingsExact){foreach(var pair in diggerJson)RecordSavedRockMismatch(proof.mismatches,"DIGGER_PROVIDER",AnimationUtility.CalculateTransformPath(pair.Key.transform,world.transform),ReplaceRefs(pair.Value),EditorJsonUtility.ToJson(pair.Key));foreach(var pair in rendererJson)RecordSavedRockMismatch(proof.mismatches,"CHUNK_RENDERER",AnimationUtility.CalculateTransformPath(pair.Key.transform,world.transform),ReplaceRefs(pair.Value),EditorJsonUtility.ToJson(pair.Key));}
            if(!proof.geometryExact)foreach(var pair in geometryObjects)RecordSavedRockMismatch(proof.mismatches,"GEOMETRY_"+pair.Key.GetType().Name,geometryPaths[pair.Key],pair.Value,pair.Key!=null?EditorJsonUtility.ToJson(pair.Key):"DESTROYED");
            if(!proof.onlyApprovedInputs||!proof.providerAndRendererBindingsExact||!proof.geometryExact||!proof.originalSourcesExact){proof.failure="ApprovedInputs="+proof.onlyApprovedInputs+" ProviderRendererBindings="+proof.providerAndRendererBindingsExact+" Geometry="+proof.geometryExact+" OriginalSources="+proof.originalSourcesExact;WriteSavedRockMaterialProof(proof);throw new InvalidOperationException("Owned cave material configuration guard failed: "+proof.failure+". Exact bounded mismatches retained; no guard waived.");}WriteSavedRockMaterialProof(proof);return proof;
        }
        static void WriteSavedRockMaterialProof(CavernSavedRockMaterialsReceipt proof)
        {Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-owned-saved-rock-material-configuration.json"),JsonUtility.ToJson(proof,true));}
        static Func<string,string> SavedRockMaterialReferenceRemapper(IReadOnlyDictionary<Material,Material> copies)
        {
            var transient=copies.ToDictionary(pair=>pair.Key.GetInstanceID(),pair=>pair.Value.GetInstanceID());var persistent=new Dictionary<string,(string guid,long id)>(StringComparer.OrdinalIgnoreCase);
            foreach(var pair in copies)
            {
                if(!AssetDatabase.Contains(pair.Key)||!AssetDatabase.Contains(pair.Value)||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pair.Key,out string sourceGuid,out long sourceId)||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pair.Value,out string ownedGuid,out long ownedId))throw new InvalidOperationException("Exact saved source/owned material identities required for provider reference remapping.");
                persistent.Add(sourceGuid+":"+sourceId.ToString(System.Globalization.CultureInfo.InvariantCulture),(ownedGuid,ownedId));
            }
            return json=>
            {
                string mapped=Regex.Replace(json,"\\\"instanceID\\\"\\s*:\\s*(-?[0-9]+)",match=>transient.TryGetValue(int.Parse(match.Groups[1].Value),out int id)?match.Value.Remove(match.Groups[1].Index-match.Index,match.Groups[1].Length).Insert(match.Groups[1].Index-match.Index,id.ToString(System.Globalization.CultureInfo.InvariantCulture)):match.Value);
                return Regex.Replace(mapped,"\\{\\s*\\\"fileID\\\"\\s*:\\s*(?<file>-?[0-9]+)\\s*,\\s*\\\"guid\\\"\\s*:\\s*\\\"(?<guid>[0-9a-fA-F]{32})\\\"\\s*,\\s*\\\"type\\\"\\s*:\\s*(?<type>[0-9]+)\\s*\\}",match=>
                {
                    // Native .mat assets use type2. Never rewrite a texture,
                    // another local object, an unknown GUID, or descriptive text.
                    if(match.Groups["type"].Value!="2"||!persistent.TryGetValue(match.Groups["guid"].Value+":"+match.Groups["file"].Value,out var target))return match.Value;
                    string value=match.Value;var guid=match.Groups["guid"];value=value.Remove(guid.Index-match.Index,guid.Length).Insert(guid.Index-match.Index,target.guid);var file=match.Groups["file"];return value.Remove(file.Index-match.Index,file.Length).Insert(file.Index-match.Index,target.id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                });
            };
        }
        static void RecordSavedRockMismatch(List<SavedRockMismatch> rows,string kind,string path,string expected,string actual)
        {
            if(expected==actual||rows.Count>=16)return;expected=expected??"NULL";actual=actual??"NULL";int at=0;while(at<Math.Min(expected.Length,actual.Length)&&expected[at]==actual[at])at++;string Context(string text){int start=Math.Max(0,at-96);return text.Substring(Math.Min(start,text.Length),Math.Min(320,Math.Max(0,text.Length-start)));}rows.Add(new SavedRockMismatch{kind=kind,path=path,firstDifference=at,expectedLength=expected.Length,actualLength=actual.Length,expectedSha256=Hash(System.Text.Encoding.UTF8.GetBytes(expected)),actualSha256=Hash(System.Text.Encoding.UTF8.GetBytes(actual)),expectedContext=Context(expected),actualContext=Context(actual)});
        }
        static void RecordSavedRockMaterialMismatch(Material source,Material candidate,List<SavedRockMismatch> rows)
        {
            var expected=new Material(source){name=candidate.name,hideFlags=candidate.hideFlags};try{ApplyCavernOwnedRockInputs(expected);string path=AssetDatabase.GetAssetPath(candidate);RecordSavedRockMismatch(rows,"APPROVED_MATERIAL_JSON",path,EditorJsonUtility.ToJson(expected),EditorJsonUtility.ToJson(candidate));RecordSavedRockMismatch(rows,"TEXTURE_PROPERTY_ORDER",path,string.Join("|",expected.GetTexturePropertyNames()),string.Join("|",candidate.GetTexturePropertyNames()));foreach(string name in expected.GetTexturePropertyNames()){string Texture(Material material)=>"texture="+(material.GetTexture(name)!=null?material.GetTexture(name).GetInstanceID().ToString():"null")+" scale="+material.GetTextureScale(name).ToString("R")+" offset="+material.GetTextureOffset(name).ToString("R");RecordSavedRockMismatch(rows,"TEXTURE_PROPERTY_"+name,path,Texture(expected),Texture(candidate));}}finally{UnityEngine.Object.DestroyImmediate(expected);}
        }
        public static SavedRockPropertyDiagnostic DiagnoseRetainedSavedRockMaterialProperties(string[] sources,string[] candidates)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||sources==null||candidates==null||sources.Length!=2||candidates.Length!=2)throw new InvalidOperationException("Stopped Editor and exact two retained native material pairs required.");
            var paths=sources.Concat(candidates).SelectMany(p=>new[]{p,p+".meta"}).ToArray();if(paths.Any(p=>!File.Exists(IoPath(p))))throw new InvalidOperationException("Retained native source/candidate material missing.");var hashes=paths.Select(p=>Hash(File.ReadAllBytes(IoPath(p)))).ToArray();var proof=new SavedRockPropertyDiagnostic{pairs=2};
            try{for(int i=0;i<2;i++){var source=AssetDatabase.LoadAssetAtPath<Material>(sources[i]);var candidate=AssetDatabase.LoadAssetAtPath<Material>(candidates[i]);if(source==null||candidate==null)throw new InvalidOperationException("Actual retained native material pair failed to load.");if(CavernOwnedRockPropertiesExact(source,candidate))proof.approvedPairs++;else RecordSavedRockMaterialMismatch(source,candidate,proof.mismatches);}}
            catch(Exception error){proof.failure=error.ToString();throw;}
            finally{proof.originalBytesExact=paths.Select((p,i)=>Hash(File.ReadAllBytes(IoPath(p)))==hashes[i]).All(v=>v);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-retained-saved-rock-material-properties.json"),JsonUtility.ToJson(proof,true));}
            if(!proof.originalBytesExact)throw new InvalidOperationException("Read-only retained material diagnosis changed original bytes.");return proof;
        }
        public static void ValidateSavedOwnedCavernRockMaterials(DragonKingdomWorld world,CavernSavedRockMaterialsReceipt proof)
        {
            if(world==null||proof==null||proof.materials==null||proof.materials.Length==0)throw new InvalidOperationException("Actual saved owned cavern material receipt required.");
            var systems=world.GetComponentsInChildren<DiggerSystem>(true).Where(d=>d.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null)).ToArray();
            var allowed=new HashSet<string>(proof.materials,StringComparer.Ordinal);bool MaterialExact(Material m)=>m!=null&&allowed.Contains(AssetDatabase.GetAssetPath(m))&&m.shader.name.StartsWith("Digger/Mesh/URP/Lit-Pass",StringComparison.Ordinal)&&m.IsKeywordEnabled("_NORMALMAP")&&AssetDatabase.GetAssetPath(m.GetTexture("_Splat3"))==CavernRoughRockDiffuse&&AssetDatabase.GetAssetPath(m.GetTexture("_Normal3"))==CavernRoughRockNormal&&m.GetTextureScale("_Splat3").Equals(Vector2.one/12f);
            var renderers=systems.SelectMany(d=>d.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();proof.savedPrefabBindingsExact=systems.Length==proof.diggerSystems&&systems.All(d=>d.Materials!=null&&d.Materials.Length>0&&d.Materials.All(MaterialExact))&&renderers.Sum(r=>r.sharedMaterials.Length)==proof.rendererSlots&&renderers.All(r=>r.sharedMaterials.All(MaterialExact));
            proof.originalSourcesExact=proof.sourcePaths!=null&&proof.sourceSha256!=null&&proof.sourcePaths.Length==proof.sourceSha256.Length&&proof.sourcePaths.Select((p,i)=>File.Exists(IoPath(p))&&Hash(File.ReadAllBytes(IoPath(p)))==proof.sourceSha256[i]).All(exact=>exact);
            proof.onlyApprovedInputs=proof.originalMaterials!=null&&proof.originalMaterials.Length==proof.materials.Length&&proof.materials.Select((p,i)=>CavernOwnedRockPropertiesExact(AssetDatabase.LoadAssetAtPath<Material>(proof.originalMaterials[i]),AssetDatabase.LoadAssetAtPath<Material>(p))).All(exact=>exact);
            if(!proof.savedPrefabBindingsExact||!proof.onlyApprovedInputs||!proof.originalSourcesExact)throw new InvalidOperationException("Saved prefab lost its native provider/chunk bindings, changed approved material properties or original source bytes.");
        }
        static string OwnedCavernGeometrySignature(DragonKingdomWorld world,DiggerSystem[] systems)
        {
            var renderers=systems.SelectMany(d=>d.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null).ToArray();var meshes=renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct().ToArray();
            var payload=string.Join("|",meshes.Select(m=>AssetDatabase.GetAssetPath(m)+":"+StandardLitMeshPayload(m)));
            var physical=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);var heights=string.Join("|",world.GetComponentsInChildren<Terrain>(true).Select(t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData)));
            var lods=string.Join("|",systems.SelectMany(d=>d.GetComponentsInChildren<LODGroup>(true)).Select(lod=>EditorJsonUtility.ToJson(lod)));
            return Hash(System.Text.Encoding.UTF8.GetBytes(payload+"|"+physical+"|"+heights+"|"+lods));
        }
    }
}
#endif
