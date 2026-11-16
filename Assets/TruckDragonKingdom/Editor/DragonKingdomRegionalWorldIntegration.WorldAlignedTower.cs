#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string TowerWorldAlignedTemplate="Assets/LeartesStudios/Garage Warehouse Environment/Art/ShaderGraphs/S_Wall.shadergraph";
        public const string TowerWorldAlignedSubgraph="Assets/LeartesStudios/Garage Warehouse Environment/Art/ShaderGraphs/SubG_WorldAlignedTexture.shadersubgraph";
        const string TowerWorldProjectionNode="1bde1197cd924e72a9500d27b5e80497",TowerWorldPositionNode="d21f5c3ada3445dcb6293aaeaf12f7d9",TowerWorldPeriodSlot="6ce8c9bbf5d3458193d5131591bc38d9";
        static readonly string[] TowerWorldCopiedIds={TowerWorldProjectionNode,"6422f0b102c0448694f94de1fd2e2db4","cf78a8e0e9b24ce487e7a6e2870a2e8e",TowerWorldPeriodSlot,"aec6a09909c74e5abe4566c988a56229",TowerWorldPositionNode,"48dc50e996f34207afe2406107bd05d5"};
        [Serializable]sealed class AlignedNodeRef{public string m_Id;}
        [Serializable]sealed class AlignedEndpoint{public AlignedNodeRef m_Node;public int m_SlotId;}
        [Serializable]sealed class AlignedEdge{public AlignedEndpoint m_OutputSlot,m_InputSlot;}
        [Serializable]sealed class AlignedHeader{public AlignedEdge[] m_Edges;}
        [Serializable]sealed class AlignedSlot{public int m_Id,m_SlotType;public string m_DisplayName;public Vector3 m_Value;}
        [Serializable]sealed class AlignedPosition{public int m_Space;public string m_Type;}
        [Serializable]public sealed class TowerWorldGraphProof
        {public string scope="PRIVATE_VENDOR_DERIVED_WORLD_ALIGNED_COLOR_ADAPTER_NOT_VENDOR_EDIT_OR_PUBLIC_PUBLICATION_APPROVAL",source,sourceSha256,template,templateSha256,subgraph,subgraphSha256,owned,ownedSha256;public float periodMeters;public bool sourceDeltaReversible,sourceVendorBytesExact,originalPrimaryColorWiringPreserved;public string[] copiedVendorObjectIds;public int sourceDocuments,ownedDocuments;public bool nativeMultiJsonBeforeWrite,nativeMultiJsonPersistedBeforeImport,nativeMultiJsonAfterImport;}
        [Serializable]public sealed class TowerWorldMaterial{public string source,mask0,mask12,mask20;public string[] assetSha256;}
        [Serializable]public sealed class TowerWorldAlignedReview
        {
            public string scope="SINGLE_VENDOR_WORLD_ALIGNED_TOWER_COLOR_COMPARISON_NOT_FINISHED_STYLE_OR_CANONICAL_ADOPTION",status="IN_PROGRESS",stage,error,cleanupError,ownedFolder;
            public int originalColliders,originalTerrains,originalPerches;public bool originalRendererMeshesExact,noRenderMeshSubstitution,noUvRemapping;
            public TowerWorldGraphProof graph;public TowerWorldMaterial[] materials;public TowerAppearanceReview appearance;public TowerStoneResponse[] responses;public TowerAppearanceFrame[] measuredTowerMasks;
        }
        public static TowerWorldAlignedReview LastWorldAlignedTowerReview{get;private set;}
        static int AlignedJsonEnd(string text,int start)
        {
            if(start<0||start>=text.Length||(text[start]!='{'&&text[start]!='['))throw new InvalidOperationException("Actual complete shader graph JSON object/array required.");char opening=text[start],closing=opening=='{'?'}':']';int depth=0;bool quoted=false,escaped=false;
            for(int i=start;i<text.Length;i++){char c=text[i];if(quoted){if(escaped)escaped=false;else if(c=='\\')escaped=true;else if(c=='\"')quoted=false;continue;}if(c=='\"'){quoted=true;continue;}if(c==opening)depth++;else if(c==closing&&--depth==0)return i+1;}throw new InvalidOperationException("Incomplete vendor graph JSON rejected.");
        }
        static string AlignedObject(string text,string id)
        {string token="\"m_ObjectId\": \""+id+"\"";int at=text.IndexOf(token,StringComparison.Ordinal);if(at<0||text.IndexOf(token,at+token.Length,StringComparison.Ordinal)>=0)throw new InvalidOperationException("Exactly one actual graph object required: "+id);int start=text.LastIndexOf('{',at);return text.Substring(start,AlignedJsonEnd(text,start)-start);}
        static string AlignedRoot(string source){int at=source.IndexOf('{');return source.Substring(at,AlignedJsonEnd(source,at)-at);}
        static IEnumerable<string> AlignedEdges(string source)
        {
            int marker=source.IndexOf("\"m_Edges\"",StringComparison.Ordinal),start=source.IndexOf('[',marker),end=AlignedJsonEnd(source,start);for(int i=start+1;i<end-1;i++)if(source[i]=='{'){int next=AlignedJsonEnd(source,i);yield return source.Substring(i,next-i);i=next-1;}
        }
        static string AlignedInsertArray(string source,string name,string items)
        {string marker="\""+name+"\": [";int at=source.IndexOf(marker,StringComparison.Ordinal);if(at<0)throw new InvalidOperationException("Actual original graph array required: "+name);return source.Insert(at+marker.Length,items);}
        static string AlignedConnection(string from,int output,string to,int input)=>JsonUtility.ToJson(new AlignedEdge{m_OutputSlot=new AlignedEndpoint{m_Node=new AlignedNodeRef{m_Id=from},m_SlotId=output},m_InputSlot=new AlignedEndpoint{m_Node=new AlignedNodeRef{m_Id=to},m_SlotId=input}});
        public static void ValidateTowerWorldPeriod(float meters)
        {if(!float.IsFinite(meters)||meters<8||meters>12)throw new ArgumentException("World-aligned source stone requires an explicit bounded8–12metre texture period.");}
        static string TowerWorldMultiJsonSeparator(string source)
        {
            // Installed MultiJsonInternal.Parse searches LF-LF globally BEFORE
            // trying its CRLF fallback. Mixing either format merges documents.
            bool lf=source.Contains("\n\n"),crlf=source.Contains("\n\r\n");if(lf==crlf)throw new InvalidOperationException("Actual single-format vendor MultiJson document separators required.");return lf?"\n\n":"\r\n\r\n";
        }
        static string[] TowerNativeMultiJsonIds(string text)
        {
            var type=Type.GetType("UnityEditor.ShaderGraph.Serialization.MultiJsonInternal, Unity.ShaderGraph.Editor",true);var parse=type.GetMethod("Parse",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);if(parse==null)throw new InvalidOperationException("Actual installed ShaderGraph MultiJson parser required.");
            try{var entries=((IEnumerable)parse.Invoke(null,new object[]{text})).Cast<object>().ToArray();return entries.Select(entry=>{var entryType=entry.GetType();string id=(string)entryType.GetProperty("id").GetValue(entry),kind=(string)entryType.GetProperty("type").GetValue(entry);if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(kind))throw new InvalidOperationException("Actual typed/object-identified native MultiJson entry required.");return id;}).ToArray();}
            catch(System.Reflection.TargetInvocationException error){throw new InvalidOperationException("Actual installed ShaderGraph MultiJson parser rejected generated graph before import.",error.InnerException??error);}
        }
        public static int ValidateTowerWorldAlignedNativeSerialization(string source,string owned)
        {
            var originals=TowerNativeMultiJsonIds(source);var entries=TowerNativeMultiJsonIds(owned);if(entries.Length!=originals.Length+TowerWorldCopiedIds.Length||entries.Distinct().Count()!=entries.Length||entries[0]!=originals[0]||!new HashSet<string>(entries).SetEquals(originals.Concat(TowerWorldCopiedIds)))throw new InvalidOperationException("Native MultiJson must contain every original identity plus exactly7copied vendor objects, one document each.");return entries.Length;
        }
        public static string BuildVendorWorldAlignedTowerGraph(string source,string template,float meters)
        {
            ValidateTowerWorldPeriod(meters);string corrected=CorrectDisconnectedTowerMaskInputs(source);var sourceEdges=AlignedEdges(source).Select(raw=>(raw,edge:JsonUtility.FromJson<AlignedEdge>(raw))).ToArray();
            var color=sourceEdges.Where(p=>p.edge.m_OutputSlot.m_Node.m_Id=="240416e0d5bb4e21934ffc6e19c5457e"&&p.edge.m_OutputSlot.m_SlotId==3).ToArray();if(color.Length!=1||color[0].edge.m_InputSlot.m_Node.m_Id!="ef4f46b84c544482a63d760ae2d12b1a"||color[0].edge.m_InputSlot.m_SlotId!=1)throw new InvalidOperationException("Actual unchanged secondary COLOR edge required; no primary atlas/normal/ARM rewiring is authorized.");
            foreach(string id in TowerWorldCopiedIds)if(source.Contains("\"m_ObjectId\": \""+id+"\""))throw new InvalidOperationException("Existing/customized world-aligned graph must not be overwritten.");
            var copied=TowerWorldCopiedIds.Select(id=>AlignedObject(template,id)).ToArray();var projection=copied[0];if(!projection.Contains("ed4350c79930b0a448aa5d4103d84d09"))throw new InvalidOperationException("Actual installed vendor world-aligned subgraph identity required.");var position=JsonUtility.FromJson<AlignedPosition>(AlignedObject(template,TowerWorldPositionNode));if(position.m_Type!="UnityEditor.ShaderGraph.PositionNode"||position.m_Space!=4)throw new InvalidOperationException("Reuse vendor ABSOLUTE WORLD position, never object/view/UV coordinates.");
            string periodObject=AlignedObject(template,TowerWorldPeriodSlot);var period=JsonUtility.FromJson<AlignedSlot>(periodObject);if(period.m_Id!=-822928854||period.m_SlotType!=0||period.m_DisplayName!="TextureSize"||period.m_Value!=Vector3.one*64)throw new InvalidOperationException("Actual untouched vendor texture-size input required.");
            var templateEdges=JsonUtility.FromJson<AlignedHeader>(AlignedRoot(template)).m_Edges;if(!templateEdges.Any(e=>e.m_OutputSlot.m_Node.m_Id==TowerWorldPositionNode&&e.m_OutputSlot.m_SlotId==0&&e.m_InputSlot.m_Node.m_Id==TowerWorldProjectionNode&&e.m_InputSlot.m_SlotId==507771418))throw new InvalidOperationException("Verified actual vendor world-position template edge required.");
            string number=meters.ToString("0.0########",CultureInfo.InvariantCulture);copied[3]=periodObject.Replace("64.0",number);if(copied[3]==periodObject)throw new InvalidOperationException("Explicit metre scale was not applied.");
            string replacement=AlignedConnection(TowerWorldProjectionNode,1,color[0].edge.m_InputSlot.m_Node.m_Id,color[0].edge.m_InputSlot.m_SlotId),nodes="{\"m_Id\":\""+TowerWorldProjectionNode+"\"},{\"m_Id\":\""+TowerWorldPositionNode+"\"},",edges=AlignedConnection(TowerWorldPositionNode,0,TowerWorldProjectionNode,507771418)+","+AlignedConnection("70e3a196bddc4cfdb310a54004e1c5e6",0,TowerWorldProjectionNode,1068583441)+",";
            string result=corrected.Replace(color[0].raw,replacement);result=AlignedInsertArray(result,"m_Nodes",nodes);result=AlignedInsertArray(result,"m_Edges",edges);string separator=TowerWorldMultiJsonSeparator(source);if(TowerWorldMultiJsonSeparator(template)!=separator)throw new InvalidOperationException("Source/template MultiJson line endings differ; preserve exact original vendor object bytes.");string suffix=separator+string.Join(separator,copied);result+=suffix;
            string reverse=result.Substring(0,result.Length-suffix.Length).Replace(nodes,"").Replace(edges,"").Replace(replacement,color[0].raw);if(reverse!=corrected)throw new InvalidOperationException("Owned graph delta exceeds2mask values,2vendor nodes/slots and3explicit color/projection edges.");ValidateTowerWorldAlignedNativeSerialization(source,result);return result;
        }
        public static bool OriginalVendorTowerInputsWouldSuppressTextureVariation(string source)
        {
            var amount=TowerGraphInputObject(source,"c3f6552eb2eb418580fe0dbfbe058d09");var contrast=TowerGraphInputObject(source,"e5d2026b498c45eb9ad5e3d7d6471738");var scale=TowerGraphInputObject(source,"8fcfd224a97b452ba9deeb3697af9433");var edges=JsonUtility.FromJson<AlignedHeader>(AlignedRoot(source)).m_Edges;return amount.Contains("\"m_Value\": 0.0,")&&contrast.Contains("\"m_Value\": 0.0,")&&scale.Contains("\"m_Value\": 0.0,")&&!edges.Any(e=>e.m_InputSlot.m_Node.m_Id=="8c4e4eadc0624699b9e992e3595b51f1"&&e.m_InputSlot.m_SlotId==447017714);
        }
        static Shader CreateWorldAlignedTowerGraph(string parent,TowerWorldAlignedReview review)
        {
            string tag=TowerAppearanceArg("-tsDragonTowerWorldCandidate","");if(string.IsNullOrEmpty(tag)||tag==parent||tag.Length>50||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Explicit unique owned world-aligned review candidate required.");string folder=Root+"VisualCandidates/"+tag;if(Directory.Exists(Io(folder))||File.Exists(Io(folder+".meta")))throw new InvalidOperationException("Preserve earlier material candidates; fresh owned folder required.");float period=float.Parse(TowerAppearanceArg("-tsDragonTowerWorldPeriod","10"),CultureInfo.InvariantCulture);ValidateTowerWorldPeriod(period);string source=File.ReadAllText(Io(TowerDetailVendorGraph)),template=File.ReadAllText(Io(TowerWorldAlignedTemplate)),derived=BuildVendorWorldAlignedTowerGraph(source,template,period);review.ownedFolder=folder;var proof=review.graph=new TowerWorldGraphProof{source=TowerDetailVendorGraph,sourceSha256=TowerFileHash(TowerDetailVendorGraph),template=TowerWorldAlignedTemplate,templateSha256=TowerFileHash(TowerWorldAlignedTemplate),subgraph=TowerWorldAlignedSubgraph,subgraphSha256=TowerFileHash(TowerWorldAlignedSubgraph),periodMeters=period,sourceDeltaReversible=true,copiedVendorObjectIds=TowerWorldCopiedIds,originalPrimaryColorWiringPreserved=true};Directory.CreateDirectory(Io(folder));string path=folder+"/Owned vendor world-aligned tower color.shadergraph";proof.sourceDocuments=TowerNativeMultiJsonIds(source).Length;proof.ownedDocuments=ValidateTowerWorldAlignedNativeSerialization(source,derived);proof.nativeMultiJsonBeforeWrite=true;File.WriteAllText(Io(path),derived,new UTF8Encoding(false));string persisted=File.ReadAllText(Io(path));if(persisted!=derived||ValidateTowerWorldAlignedNativeSerialization(source,persisted)!=proof.ownedDocuments)throw new InvalidOperationException("Saved graph bytes differ from native-parser-qualified source.");proof.nativeMultiJsonPersistedBeforeImport=true;AssetDatabase.ImportAsset(folder,ImportAssetOptions.ImportRecursive|ImportAssetOptions.ForceSynchronousImport);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);proof.owned=path;proof.ownedSha256=TowerFileHash(path);proof.sourceVendorBytesExact=TowerFileHash(proof.source)==proof.sourceSha256&&TowerFileHash(proof.template)==proof.templateSha256&&TowerFileHash(proof.subgraph)==proof.subgraphSha256;if(!proof.sourceVendorBytesExact||File.ReadAllText(Io(path))!=derived)throw new InvalidOperationException("Original vendor or owned exact graph content changed during import.");proof.nativeMultiJsonAfterImport=ValidateTowerWorldAlignedNativeSerialization(source,File.ReadAllText(Io(path)))==proof.ownedDocuments;var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);if(shader==null||!shader.isSupported)throw new InvalidOperationException("Actual supported world-aligned vendor-derived graph required.");return shader;
        }
        static Texture2D SaveWorldAlignedConstant(Texture2D temporary,string folder,string name)
        {var saved=UnityEngine.Object.Instantiate(temporary);saved.name=name;saved.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(saved,folder+"/"+name+".asset");return saved;}
        static Material SaveWorldAlignedMaterial(Material temporary,string path)
        {var saved=new Material(temporary){name=Path.GetFileNameWithoutExtension(path),hideFlags=HideFlags.None};AssetDatabase.CreateAsset(saved,path);return saved;}
        static void SaveWorldAlignedReview(TowerWorldAlignedReview review)
        {string output=Path.Combine(Output(),"WorldAlignedTower");Directory.CreateDirectory(Io(output));File.WriteAllText(Io(Path.Combine(output,"world-aligned-tower-review.json")),JsonUtility.ToJson(review,true));}
        public static IEnumerator CaptureWorldAlignedTowerReview()
        {
            var review=LastWorldAlignedTowerReview=new TowerWorldAlignedReview();var stack=new Stack<IEnumerator>();stack.Push(WorldAlignedTowerRoutine(review));try{while(stack.Count>0){bool moved;object current=null;try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;else(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){review.cleanupError=cleanup.ToString();}}review.error=error.ToString();review.status="REJECTED_WORLD_ALIGNED_COLOR_OR_PRESERVATION";if(review.appearance!=null){review.appearance.error=review.error;review.appearance.status=review.status;}SaveWorldAlignedReview(review);throw;}if(moved){if(current is IEnumerator nested)stack.Push(nested);else yield return current;}}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
        }
        static IEnumerator WorldAlignedTowerRoutine(TowerWorldAlignedReview review)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required; explicit review only.");
            string tag=TowerAppearanceArg("-tsDragonWorldCandidate","");if(string.IsNullOrEmpty(tag)||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Explicit physically passed candidate required.");string folder=Root+"VisualCandidates/"+tag,receipt=folder+"/Integration Receipt.json";var parent=JsonUtility.FromJson<TowerAppearanceParent>(File.ReadAllText(Io(receipt)));
            if(parent.tag!=tag||!parent.originalPhysicalGatesPassed||!parent.sourceBytesPreserved||parent.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW")throw new InvalidOperationException("Do not render rejected or unqualified worlds as appearance acceptance.");
            string size=TowerAppearanceArg("-tsDragonTowerDetailResolution","1920x1080");if(size!="1280x720"&&size!="1920x1080")throw new ArgumentException("Explicit720p/1080p comparison dimensions required.");int width=size=="1280x720"?1280:1920,height=size=="1280x720"?720:1080;
            string output=Path.Combine(Output(),"WorldAlignedTower");if(Directory.Exists(Io(output)))throw new InvalidOperationException("Preserve existing appearance evidence; use a fresh output.");Directory.CreateDirectory(Io(output));
            var proof=review.appearance=new TowerAppearanceReview{candidate=tag,vendorGraph=TowerDetailVendorGraph,scope="ABSOLUTE_WORLD_VENDOR_COLOR_PROJECTION_ORIGINAL_MESH_REFS_NOT_STYLE_ACCEPTANCE"};var files=new Dictionary<string,TowerAppearanceSource>(StringComparer.Ordinal);
            foreach(string path in AssetDatabase.GetDependencies(new[]{parent.prefab,parent.preview,receipt,TowerDetailVendorGraph,TowerDetailSourceLayer,TowerPaintedBrick,TowerWorldAlignedTemplate,TowerWorldAlignedSubgraph,"Packages/com.unity.shadergraph/Editor/Serialization/MultiJsonInternal.cs","Packages/com.unity.shadergraph/Editor/Serialization/MultiJsonEntry.cs"},true).Concat(new[]{parent.prefab,parent.preview,receipt,TowerDetailVendorGraph,TowerDetailSourceLayer,TowerPaintedBrick,TowerWorldAlignedTemplate,TowerWorldAlignedSubgraph,"Packages/com.unity.shadergraph/Editor/Serialization/MultiJsonInternal.cs","Packages/com.unity.shadergraph/Editor/Serialization/MultiJsonEntry.cs"}).Distinct())TowerWatchSource(files,path,true);
            foreach(string path in Directory.GetFiles(Io(parent.cavern.dataFolder),"*",SearchOption.AllDirectories))TowerWatchSource(files,path,false);
            TowerWatchPipelineShaderSources(files);
            var setup=PrepareValidQuayProbeSceneSetup();foreach(string path in setup.Select(s=>s.path).Where(p=>!string.IsNullOrEmpty(p)))TowerWatchSource(files,path,true);proof.watchedSources=files.Values.ToArray();Scene scene=default;GameObject cameraObject=null;var temporaries=new List<UnityEngine.Object>();var frames=new List<TowerAppearanceFrame>();bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;Dictionary<MeshFilter,Mesh> originalMeshes=null;Dictionary<Renderer,Material[]> originalMaterials=null;Dictionary<Component,string> worldState=null;DragonKingdomWorld world=null;
            try
            {
                scene=EditorSceneManager.OpenScene(parent.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();Physics.SyncTransforms();
                var towers=world.Region("castle").root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f.sharedMesh,out string guid,out long _)&&guid==TowerDetailMeshGuid&&f.GetComponent<Renderer>()?.enabled==true).ToArray();
                if(towers.Length!=4)throw new InvalidOperationException("Exactly four original retained perch-support tower renderers required.");var perches=world.Region("castle").perches.ToArray();var perchPoses=perches.Select(Pose).ToArray();if(perches.Length!=4||perches.Any(p=>!towers.Any(f=>p.IsChildOf(f.transform))))throw new InvalidOperationException("Every original perch must retain its original physical support.");proof.towers=4;proof.perches=4;
                originalMeshes=towers.ToDictionary(f=>f,f=>f.sharedMesh);originalMaterials=towers.Select(f=>f.GetComponent<Renderer>()).ToDictionary(v=>v,v=>v.sharedMaterials);var materialJson=originalMaterials.Values.SelectMany(v=>v).Distinct().ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));worldState=world.GetComponentsInChildren<Component>(true).Where(c=>c is Transform||c is Collider||c is Rigidbody||c is Renderer||c is MeshFilter||c is LODGroup).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));var colliders=world.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));var heights=world.GetComponentsInChildren<Terrain>(true).ToDictionary(t=>t,t=>DragonKingdomWorldVisualMigration.HeightHash(t.terrainData));
                review.originalColliders=colliders.Count;review.originalTerrains=heights.Count;review.originalPerches=perches.Length;review.stage="NATIVE_MATERIAL_CAPTURE";
                bool SupportsExact()=>colliders.All(p=>p.Key!=null&&EditorJsonUtility.ToJson(p.Key)==p.Value)&&heights.All(p=>p.Key!=null&&DragonKingdomWorldVisualMigration.HeightHash(p.Key.terrainData)==p.Value)&&perches.SequenceEqual(world.Region("castle").perches)&&perchPoses.SequenceEqual(perches.Select(Pose));
                proof.allColliderReferencesAndStateExact=proof.allHeightfieldsExact=proof.allPerchReferencesAndPosesExact=SupportsExact();review.originalRendererMeshesExact=originalMeshes.All(p=>p.Key.sharedMesh==p.Value);
                review.stage="OWNED_VENDOR_WORLD_ALIGNED_GRAPH";var shader=CreateWorldAlignedTowerGraph(tag,review);var rock=AssetDatabase.LoadAssetAtPath<TerrainLayer>(TowerDetailSourceLayer);var brick=AssetDatabase.LoadAssetAtPath<Texture2D>(TowerPaintedBrick);if(rock==null||brick==null||brick.wrapMode!=TextureWrapMode.Repeat)throw new InvalidOperationException("Actual installed source stone and repeating painted brick required.");
                var neutral=new Dictionary<Material,Material>();var detail=new Dictionary<Material,Material>();var highDetail=new Dictionary<Material,Material>();
                Texture2D Mask(Color color,string name){var temp=TowerConstant(color,"TEMP "+name);temporaries.Add(temp);return SaveWorldAlignedConstant(temp,review.ownedFolder,name);}
                Texture2D zeroMask=Mask(Color.clear,"Mask zero"),mask=Mask(new Color(.12f,.12f,.12f,.12f),"Mask twelve percent"),highMask=Mask(new Color(.2f,.2f,.2f,.2f),"Mask twenty percent"),orm=Mask(new Color(1,.8f,0,1),"Opaque rough stone ORM");proof.actualQuantizedDetailMask=mask.GetPixel(0,0).r;
                var saved=new List<TowerWorldMaterial>();int index=0;
                foreach(var source in materialJson.Keys){var record=new TowerWorldMaterial{source=AssetDatabase.GetAssetPath(source)};foreach(var pair in new[]{Tuple.Create("zero",zeroMask,neutral),Tuple.Create("twelve",mask,detail),Tuple.Create("twenty",highMask,highDetail)}){var temp=TowerVendorDetailMaterial(source,shader,pair.Item2,orm,rock,true);temporaries.Add(temp);temp.SetTexture("_Secondary_Base_Color",brick);string path=review.ownedFolder+"/Tower "+index+" "+pair.Item1+".mat";pair.Item3[source]=SaveWorldAlignedMaterial(temp,path);if(pair.Item1=="zero")record.mask0=path;else if(pair.Item1=="twelve")record.mask12=path;else record.mask20=path;}saved.Add(record);index++;}AssetDatabase.SaveAssets();foreach(var record in saved)record.assetSha256=new[]{record.mask0,record.mask12,record.mask20}.Select(TowerFileHash).ToArray();review.materials=saved.ToArray();
                proof.materials=materialJson.Keys.Select(source=>new TowerAppearanceMaterialProof{source=AssetDatabase.GetAssetPath(source),sourceShader=source.shader.name,vendorShader=shader.name,atlas=AssetDatabase.GetAssetPath(neutral[source].GetTexture("_Base_Color")),atlasGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(neutral[source].GetTexture("_Base_Color"))),sourceColor=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white,sourceSmoothness=source.HasProperty("_Smoothness")?source.GetFloat("_Smoothness"):source.HasProperty("_Glossiness")?source.GetFloat("_Glossiness"):.2f,sourceMetallic=source.HasProperty("_Metallic")?source.GetFloat("_Metallic"):0,atlasTransformRetained=neutral[source].GetFloat("_UV0_Scale")==1&&neutral[source].GetFloat("_UV0_U_Offset")==0&&neutral[source].GetFloat("_UV0_V_Offset")==0}).ToArray();
                proof.meshes=originalMeshes.Values.Distinct().Select(mesh=>new TowerAppearanceMeshProof{source=AssetDatabase.GetAssetPath(mesh),sourceGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)),sourceVertices=mesh.vertexCount,reviewVertices=mesh.vertexCount,generatedSecondaryUv=false,orientedTriangleCornersExceptUv1Exact=true}).ToArray();review.noRenderMeshSubstitution=review.noUvRemapping=true;
                proof.originalAtlasColorPreserved=true;var bounds=towers[0].GetComponent<Renderer>().bounds;foreach(var t in towers.Skip(1))bounds.Encapsulate(t.GetComponent<Renderer>().bounds);var close=towers.OrderBy(t=>t.transform.position.z).First().GetComponent<Renderer>().bounds;
                cameraObject=new GameObject("Temporary retained-tower material comparison");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=48;camera.aspect=(float)width/height;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in new[]{Tuple.Create("support-towers",bounds.center+new Vector3(0,65,-bounds.extents.magnitude*2.5f),bounds.center),Tuple.Create("source-stone-close",close.center+new Vector3(-close.extents.magnitude*.6f,15,-close.extents.magnitude*2.4f),close.center)})
                {
                    yield return TowerSettle(camera,view.Item2,view.Item3);
                    foreach(string intervention in new[]{"original","world-mask0","world-mask12","world-mask20","original-restored"})
                    {
                        review.originalRendererMeshesExact=originalMeshes.All(p=>p.Key.sharedMesh==p.Value);if(!review.originalRendererMeshesExact)throw new InvalidOperationException("Original renderer mesh identity must remain exact in every phase.");
                        foreach(var pair in originalMaterials)pair.Key.sharedMaterials=intervention=="world-mask0"?pair.Value.Select(m=>neutral[m]).ToArray():intervention=="world-mask12"?pair.Value.Select(m=>detail[m]).ToArray():intervention=="world-mask20"?pair.Value.Select(m=>highDetail[m]).ToArray():pair.Value;
                        proof.allColliderReferencesAndStateExact=proof.allHeightfieldsExact=proof.allPerchReferencesAndPosesExact=SupportsExact();if(!proof.allColliderReferencesAndStateExact)throw new InvalidOperationException("Appearance comparison changed original collider/heightfield/perch state.");
                        yield return TowerSettle(camera,view.Item2,view.Item3);var capture=TowerCapture(camera,output,view.Item1+"-"+intervention,width,height,view.Item3,materialJson.Keys.Select(m=>m.shader).Append(shader).Distinct().ToArray());frames.Add(capture);if(capture.shaderErrors.Length!=0)throw new InvalidOperationException("Native shader compiler error during vendor detail comparison.");
                    }
                }
                review.stage="ACTUAL_TOWER_PIXEL_RESPONSE";proof.allColliderReferencesAndStateExact=proof.allHeightfieldsExact=proof.allPerchReferencesAndPosesExact=SupportsExact();var measured=new List<TowerAppearanceFrame>();
                foreach(var view in new[]{Tuple.Create("support-towers",bounds.center+new Vector3(0,65,-bounds.extents.magnitude*2.5f),bounds.center),Tuple.Create("source-stone-close",close.center+new Vector3(-close.extents.magnitude*.6f,15,-close.extents.magnitude*2.4f),close.center)})
                {yield return TowerSettle(camera,view.Item2,view.Item3);measured.Add(CaptureMeasuredTowerPixels(camera,world,originalMaterials.Keys.ToArray(),output,view.Item1,width,height,view.Item3));}review.measuredTowerMasks=measured.ToArray();
                review.responses=new[]{"support-towers","source-stone-close"}.Select(view=>MeasureTowerStoneResponse(view,"world-aligned-painted-brick",measured.Single(f=>f.intervention==view+"-measured-tower-pixels"),frames.Single(f=>f.intervention==view+"-world-mask0"),frames.Single(f=>f.intervention==view+"-world-mask12"),frames.Single(f=>f.intervention==view+"-world-mask20"))).ToArray();
                if(review.responses.Any(r=>!r.measuredTowerResponsePassed))throw new InvalidOperationException("Actual isolated tower pixels lack bounded readable world-aligned response; preserve original global measurements and require direct style review.");review.status="WORLD_ALIGNED_ORIGINAL_MESH_COLOR_CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW";proof.status="CAPTURED_REQUIRES_DIRECT_STYLE_REVIEW_NOT_PRODUCTION_APPEARANCE_ACCEPTANCE";


            }
            finally
            {
                if(originalMeshes!=null)foreach(var p in originalMeshes)if(p.Key!=null)p.Key.sharedMesh=p.Value;if(originalMaterials!=null)foreach(var p in originalMaterials)if(p.Key!=null)p.Key.sharedMaterials=p.Value;
                proof.originalWorldStateRestored=worldState!=null&&worldState.All(p=>p.Key!=null&&EditorJsonUtility.ToJson(p.Key)==p.Value);
                foreach(var item in temporaries)if(item!=null)UnityEngine.Object.DestroyImmediate(item);if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);proof.temporaryObjectsDestroyed=temporaries.All(v=>v==null);ShaderUtil.allowAsyncCompilation=async;
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);proof.incomingSceneContextRestored=setup.Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive).SequenceEqual(EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive));proof.changedSources=files.Values.Where(p=>p.resourceKind!="UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE"&&(!File.Exists(p.resolvedPath)||TowerFileHash(p.resolvedPath)!=p.sha256)).Select(p=>p.path).ToArray();proof.sourceBytesExact=proof.changedSources.Length==0;proof.frames=frames.ToArray();
                if(!proof.originalWorldStateRestored||!proof.temporaryObjectsDestroyed||!proof.sourceBytesExact||!proof.incomingSceneContextRestored){proof.status="REJECTED_PRESERVATION_FAILURE";proof.error="Retain actual evidence: source/world/temporary-state preservation failed.";}
                if(review.graph!=null)review.graph.sourceVendorBytesExact=TowerFileHash(review.graph.source)==review.graph.sourceSha256&&TowerFileHash(review.graph.template)==review.graph.templateSha256&&TowerFileHash(review.graph.subgraph)==review.graph.subgraphSha256;review.status=proof.status=="REJECTED_PRESERVATION_FAILURE"?proof.status:review.status;SaveWorldAlignedReview(review);
                if(!proof.originalWorldStateRestored||!proof.temporaryObjectsDestroyed||!proof.sourceBytesExact||!proof.incomingSceneContextRestored)throw new InvalidOperationException(proof.error);
            }
        }
    }
}
#endif
