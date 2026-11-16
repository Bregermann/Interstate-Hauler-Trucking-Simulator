#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string FinishErosionGraph="Assets/PinwheelStudio/Vista/Personal/Samples/Graphs/ErodedMountainRange.asset";
        const float FinishFeather=32,FinishMaximumDelta=6;
        [Serializable]public sealed class VistaFinishFrame{public string view,stage,png,sha256;public Vector3 camera,target;}
        [Serializable]public sealed class VistaFinishTile{public string source,guid,sourceSha256,beforeHeightSha256,afterHeightSha256,restoredHeightSha256;public Vector3 origin;public int resolution,changed,held;public float maximumDelta;}
        [Serializable]public sealed class VistaFinishHold{public string kind,path;public Bounds bounds;public Vector3 point;public float radius;}
        [Serializable]public sealed class VistaFinishDiagnosis
        {
            public string scope="ACTUAL_INSTALLED_VISTA_EROSION_ON_TEMPORARY_CURRENT_WORLD_HEIGHTS_SAME_CAMERA_ABA_NOT_ADOPTION_OR_FINISHED_VISUAL_ACCEPTANCE",status="IN_PROGRESS",error,preview,graph=FinishErosionGraph,graphSha256,nodeJson,nodeJsonSha256,inputSha256,outputSha256,maskSha256;
            public int mosaicResolution,vendorResolution=1024,actualVendorOutputWidth,actualVendorOutputHeight,heldSamples,transitionSamples,changedSamples,nonFinite,protectedSamplesChanged,sourceFiles,roadGatesExecuted,startGatesExecuted,extractionGatesExecuted;
            public float maximumDelta,maximumSeamDelta,maximumAllowedDelta=FinishMaximumDelta,featherMeters=FinishFeather,beforeMeanAbsoluteLaplacian,afterMeanAbsoluteLaplacian;
            public bool vendorNodeSettingsExact,sourceBytesPreserved,originalPhysicsExact,originalTerrainBindingsRestored,originalHeightmapsExact,originalSceneSetupRestored,originalLayerAlphaHoleBindingsExact,physicalGatesPassed;
            public VistaFinishTile[] tiles;public VistaFinishHold[] holds;public VistaFinishFrame[] frames;public string[] changedSourcePaths;
        }
        public static VistaFinishDiagnosis LastVistaFinishDiagnosis{get;private set;}
        sealed class FinishTile
        {public Terrain terrain;public TerrainCollider collider;public TerrainData original,temporary;public float[,] heights;public string alpha,holes;public Vector3 origin;public VistaFinishTile proof;}

        // Explicit diagnostic only. The current accepted technical candidate and
        // its original graph, source art, layers, holes and colliders are never saved.
        public static IEnumerator CaptureActualBoundedVistaTerrainFinish()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            string tag=Argument("-tsDragonWorldCandidate","RoundedIntegrated29");
            if(tag!="RoundedIntegrated29")throw new InvalidOperationException("This first bounded finish diagnosis is pinned to the measured technical29 baseline.");
            string folder=Root+"VisualCandidates/"+tag,preview=folder+"/DragonKingdomPreview.unity";
            var prior=PrepareValidQuayProbeSceneSetup();
            CaptureArtistSourcePreflight();CaptureExact51AssemblyMaterialProof();var imports=PrepareValidatedQuayNativeImports();
            if(!imports.passed||!imports.nonMaterialSourceBytesExact)throw new InvalidOperationException("Exact existing approved native import preflight required before immutable source snapshots.");
            var graph=AssetDatabase.LoadAssetAtPath<TerrainGraph>(FinishErosionGraph);
            if(graph==null)throw new InvalidOperationException("Actual installed authored Vista ErodedMountainRange graph required.");
            var authored=graph.GetNodesOfType<ErosionNode>().Single();
            var paths=AssetDatabase.GetDependencies(preview,true).Concat(AssetDatabase.GetDependencies(FinishErosionGraph,true)).Concat(new[]{preview,folder+"/Integration Receipt.json"}).SelectMany(p=>new[]{p,p+".meta"}).Distinct().Where(p=>File.Exists(Io(p))).ToDictionary(p=>p,AssetSha);
            string output=Path.Combine(Output(),"ActualVistaTerrainFinish");Directory.CreateDirectory(output);
            var proof=new VistaFinishDiagnosis{preview=preview,graphSha256=AssetSha(FinishErosionGraph),sourceFiles=paths.Count,nodeJson=JsonUtility.ToJson(authored)};proof.nodeJsonSha256=FinishDigest(System.Text.Encoding.UTF8.GetBytes(proof.nodeJson));LastVistaFinishDiagnosis=proof;
            Scene scene=default;DragonKingdomWorld world=null;Camera camera=null;string physics=null;bool async=ShaderUtil.allowAsyncCompilation;
            var tiles=new List<FinishTile>();var allData=new Dictionary<Terrain,TerrainData>();var allColliders=new Dictionary<TerrainCollider,TerrainData>();var frames=new List<VistaFinishFrame>();
            TerrainGraph transientGraph=null;ExecutionHandle execution=null;Texture2D inputTexture=null,hardnessTexture=null;RenderTexture inputRt=null,hardnessRt=null;bool vendorOutstanding=false;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                foreach(var t in world.GetComponentsInChildren<Terrain>(true))allData.Add(t,t.terrainData);
                foreach(var c in world.GetComponentsInChildren<TerrainCollider>(true))allColliders.Add(c,c.terrainData);
                if(allData.Count!=16||allColliders.Count!=16)throw new InvalidOperationException("Current four-core/twelve-coast physical terrain foundation required.");
                foreach(var t in allData.Keys.Where(t=>new[]{"Kingdom 0 0.asset","Kingdom 1 0.asset","Kingdom 0 1.asset","Kingdom 1 1.asset"}.Select(name=>folder+"/Terrain/"+name).Contains(AssetDatabase.GetAssetPath(t.terrainData))).OrderBy(t=>t.transform.position.z).ThenBy(t=>t.transform.position.x))
                {
                    var d=t.terrainData;int n=d.heightmapResolution;
                    if(d.size!=new Vector3(1600,600,1600)||n!=513||t.transform.position.y!=-20)throw new InvalidOperationException("Exact measured513/1600m technical29 core required.");
                    var row=new FinishTile{terrain=t,collider=t.GetComponent<TerrainCollider>(),original=d,heights=d.GetHeights(0,0,n,n),origin=t.transform.position,alpha=FinishAlphaHash(d),holes=FinishHoleHash(d),proof=new VistaFinishTile{source=AssetDatabase.GetAssetPath(d),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d)),sourceSha256=AssetSha(AssetDatabase.GetAssetPath(d)),beforeHeightSha256=DragonKingdomWorldVisualMigration.HeightHash(d),origin=t.transform.position,resolution=n}};
                    if(row.collider==null||row.collider.terrainData!=d)throw new InvalidOperationException("Actual native core collider/data binding required.");tiles.Add(row);
                }
                if(tiles.Count!=4)throw new InvalidOperationException("Exactly four measured core tiles required.");
                var holds=FinishTerrainHolds(world);proof.holds=holds.ToArray();
                const int nMosaic=1025;float[,] mosaic=new float[nMosaic,nMosaic],weight=new float[nMosaic,nMosaic];proof.mosaicResolution=nMosaic;
                var seen=new bool[nMosaic,nMosaic];foreach(var tile in tiles)
                {
                    int ox=Mathf.RoundToInt((tile.origin.x+1600)/1600)*512,oz=Mathf.RoundToInt((tile.origin.z+800)/1600)*512;
                    if(ox<0||ox>512||oz<0||oz>512)throw new InvalidOperationException("Actual saved core coverage changed.");
                    for(int z=0;z<513;z++)for(int x=0;x<513;x++){float value=tile.heights[z,x];if(seen[oz+z,ox+x])proof.maximumSeamDelta=Mathf.Max(proof.maximumSeamDelta,Mathf.Abs(mosaic[oz+z,ox+x]-value)*600);mosaic[oz+z,ox+x]=value;seen[oz+z,ox+x]=true;}
                }
                if(proof.maximumSeamDelta>.04f||seen.Cast<bool>().Any(v=>!v))throw new InvalidOperationException("Actual complete continuous source mosaic and strict seam proof required.");
                for(int z=0;z<nMosaic;z++)for(int x=0;x<nMosaic;x++){float edge=Mathf.Min(x,z,1024-x,1024-z)*3.125f;weight[z,x]=Mathf.SmoothStep(0,1,(edge-20)/FinishFeather);if(mosaic[z,x]*600-20<-.5f)weight[z,x]=0;}
                foreach(var hold in holds)StampFinishHold(weight,hold);
                // Hydrology and the actual underground shell remain entirely
                // unchanged. The CPU mask is also reapplied after the GPU run.
                for(int z=0;z<nMosaic;z++)for(int x=0;x<nMosaic;x++)
                {var p=new Vector3(-1600+x*3.125f,0,-800+z*3.125f);weight[z,x]=Mathf.Min(weight[z,x],Mathf.SmoothStep(0,1,HydrologyDistance(p)/FinishFeather));if(weight[z,x]==0)proof.heldSamples++;else if(weight[z,x]<1)proof.transitionSamples++;}
                proof.inputSha256=FinishFloatHash(mosaic);proof.maskSha256=FinishFloatHash(weight);proof.beforeMeanAbsoluteLaplacian=FinishLaplacian(mosaic,weight);
                File.WriteAllBytes(Path.Combine(output,"original-heights.float32"),FinishFloatBytes(mosaic));File.WriteAllBytes(Path.Combine(output,"editable-weight.float32"),FinishFloatBytes(weight));
                transientGraph=ScriptableObject.CreateInstance<TerrainGraph>();transientGraph.hideFlags=HideFlags.HideAndDontSave;transientGraph.Reset();
                var input=new InputNode{inputName="Current measured kingdom height"};var hardness=new InputNode{inputName="Exact protected terrain hardness"};var erosion=new ErosionNode();JsonUtility.FromJsonOverwrite(proof.nodeJson,erosion);
                proof.vendorNodeSettingsExact=JsonUtility.ToJson(erosion)==proof.nodeJson;if(!proof.vendorNodeSettingsExact)throw new InvalidOperationException("Authored vendor erosion settings changed during clone.");
                transientGraph.AddNode(input);transientGraph.AddNode(hardness);transientGraph.AddNode(erosion);
                transientGraph.AddEdge(new Edge{outputSlot=new SlotRef(input.id,input.outputSlot.id),inputSlot=new SlotRef(erosion.id,erosion.inputHeightSlot.id)});
                transientGraph.AddEdge(new Edge{outputSlot=new SlotRef(hardness.id,hardness.outputSlot.id),inputSlot=new SlotRef(erosion.id,erosion.hardnessSlot.id)});
                inputTexture=FinishInputTexture(mosaic,false);hardnessTexture=FinishInputTexture(weight,true);inputRt=FinishInputRt(inputTexture);hardnessRt=FinishInputRt(hardnessTexture);
                var container=new GraphInputContainer();container.AddTexture(input.inputName,inputRt);container.AddTexture(hardness.inputName,hardnessRt);
                var config=TerrainGenerationConfigs.Create();config.resolution=1024;config.worldBounds=new Rect(-1600,-800,3200,3200);config.terrainHeight=600;config.seed=28082026;
                execution=transientGraph.Execute(new[]{erosion.id},config,container);vendorOutstanding=true;double started=EditorApplication.timeSinceStartup;
                while(!execution.isCompleted){if(EditorApplication.timeSinceStartup-started>240)throw new TimeoutException("Actual Vista erosion did not complete; its unsaved GPU resources are retained for native process teardown.");yield return null;}vendorOutstanding=false;
                var result=execution.data.GetRT(new SlotRef(erosion.id,erosion.outputHeightSlot.id));if(result==null||result.renderTexture==null)throw new InvalidOperationException("Actual vendor height output absent.");
                proof.actualVendorOutputWidth=result.renderTexture.width;proof.actualVendorOutputHeight=result.renderTexture.height;var eroded=FinishReadHeights(result.renderTexture,nMosaic);proof.outputSha256=FinishFloatHash(eroded);File.WriteAllBytes(Path.Combine(output,"actual-vista-output.float32"),FinishFloatBytes(eroded));
                var cameraObject=new GameObject("Owned temporary same-camera Vista finish diagnostic");SceneManager.MoveGameObjectToScene(cameraObject,scene);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.aspect=1280f/720;camera.clearFlags=CameraClearFlags.Skybox;
                RunFinishPhysicalGates(world,proof);CaptureFinishViews(camera,output,"before",frames);
                var composed=new float[nMosaic,nMosaic];
                for(int z=0;z<nMosaic;z++)for(int x=0;x<nMosaic;x++)
                {
                    float value=eroded[z,x];if(float.IsNaN(value)||float.IsInfinity(value)){proof.nonFinite++;continue;}
                    float delta=Mathf.Clamp((value-mosaic[z,x])*600,-FinishMaximumDelta,FinishMaximumDelta)*weight[z,x];composed[z,x]=mosaic[z,x]+delta/600;if(weight[z,x]>0&&mosaic[z,x]*600-20>=-.5f)composed[z,x]=Mathf.Max(composed[z,x],19.5f/600);
                    if(weight[z,x]==0&&composed[z,x]!=mosaic[z,x])proof.protectedSamplesChanged++;if(Mathf.Abs(delta)>.0001f)proof.changedSamples++;proof.maximumDelta=Mathf.Max(proof.maximumDelta,Mathf.Abs(delta));
                }
                if(proof.nonFinite!=0||proof.protectedSamplesChanged!=0||proof.changedSamples==0)throw new InvalidOperationException("Vendor result must be finite, actually affect editable terrain and preserve every protected sample exactly.");
                proof.afterMeanAbsoluteLaplacian=FinishLaplacian(composed,weight);File.WriteAllBytes(Path.Combine(output,"bounded-composed-heights.float32"),FinishFloatBytes(composed));
                foreach(var tile in tiles)
                {
                    int ox=Mathf.RoundToInt((tile.origin.x+1600)/1600)*512,oz=Mathf.RoundToInt((tile.origin.z+800)/1600)*512;var values=new float[513,513];
                    for(int z=0;z<513;z++)for(int x=0;x<513;x++){values[z,x]=composed[oz+z,ox+x];float d=Mathf.Abs(values[z,x]-tile.heights[z,x])*600;tile.proof.maximumDelta=Mathf.Max(tile.proof.maximumDelta,d);if(d>.0001f)tile.proof.changed++;if(weight[oz+z,ox+x]==0)tile.proof.held++;}
                    tile.temporary=UnityEngine.Object.Instantiate(tile.original);tile.temporary.hideFlags=HideFlags.HideAndDontSave;tile.temporary.SetHeights(0,0,values);tile.terrain.terrainData=tile.temporary;tile.collider.terrainData=tile.temporary;tile.terrain.Flush();tile.proof.afterHeightSha256=DragonKingdomWorldVisualMigration.HeightHash(tile.temporary);
                    if(FinishAlphaHash(tile.temporary)!=tile.alpha||FinishHoleHash(tile.temporary)!=tile.holes||!tile.temporary.terrainLayers.SequenceEqual(tile.original.terrainLayers))throw new InvalidOperationException("Temporary finish changed alpha/holes/source-layer identities.");
                }
                Physics.SyncTransforms();CaptureFinishViews(camera,output,"actual-vista-bounded",frames);RunFinishPhysicalGates(world,proof);proof.physicalGatesPassed=true;
                foreach(var tile in tiles){tile.terrain.terrainData=tile.original;tile.collider.terrainData=tile.original;tile.terrain.Flush();}
                Physics.SyncTransforms();RunFinishPhysicalGates(world,proof);CaptureFinishViews(camera,output,"restored",frames);
                proof.status="MEASURED_REQUIRES_ACTUAL_SAME_CAMERA_VISUAL_REVIEW_NOT_ADOPTED";
            }
            finally
            {
                foreach(var tile in tiles){if(tile.terrain!=null)tile.terrain.terrainData=tile.original;if(tile.collider!=null)tile.collider.terrainData=tile.original;if(tile.terrain!=null)tile.terrain.Flush();tile.proof.restoredHeightSha256=DragonKingdomWorldVisualMigration.HeightHash(tile.original);}
                Physics.SyncTransforms();proof.originalTerrainBindingsRestored=allData.Count==16&&allData.All(p=>p.Key.terrainData==p.Value)&&allColliders.All(p=>p.Key.terrainData==p.Value);proof.originalHeightmapsExact=tiles.Count==4&&tiles.All(t=>t.proof.beforeHeightSha256==t.proof.restoredHeightSha256);
                proof.originalLayerAlphaHoleBindingsExact=tiles.Count==4&&tiles.All(t=>FinishAlphaHash(t.original)==t.alpha&&FinishHoleHash(t.original)==t.holes);proof.originalPhysicsExact=world!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);
                if(camera!=null)UnityEngine.Object.DestroyImmediate(camera.gameObject);foreach(var tile in tiles)if(tile.temporary!=null)UnityEngine.Object.DestroyImmediate(tile.temporary);
                if(!vendorOutstanding){execution?.Dispose();if(transientGraph!=null)UnityEngine.Object.DestroyImmediate(transientGraph);if(inputTexture!=null)UnityEngine.Object.DestroyImmediate(inputTexture);if(hardnessTexture!=null)UnityEngine.Object.DestroyImmediate(hardnessTexture);foreach(var rt in new[]{inputRt,hardnessRt})if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(prior);ShaderUtil.allowAsyncCompilation=async;
                var restored=EditorSceneManager.GetSceneManagerSetup();proof.originalSceneSetupRestored=prior.Length==restored.Length&&prior.Zip(restored,(a,b)=>a.path==b.path&&a.isActive==b.isActive&&a.isLoaded==b.isLoaded).All(v=>v);
                proof.changedSourcePaths=paths.Where(p=>!File.Exists(Io(p.Key))||AssetSha(p.Key)!=p.Value).Select(p=>p.Key).ToArray();proof.sourceBytesPreserved=proof.changedSourcePaths.Length==0;proof.tiles=tiles.Select(t=>t.proof).ToArray();proof.frames=frames.ToArray();
                if(proof.status=="IN_PROGRESS")proof.status="REJECTED_DIAGNOSIS_INSPECT_NATIVE_EXCEPTION";File.WriteAllText(Path.Combine(output,"actual-vista-terrain-finish.json"),JsonUtility.ToJson(proof,true));
            }
            if(!proof.sourceBytesPreserved||!proof.originalPhysicsExact||!proof.originalTerrainBindingsRestored||!proof.originalHeightmapsExact||!proof.originalSceneSetupRestored||!proof.originalLayerAlphaHoleBindingsExact)throw new InvalidOperationException("Read-only Vista terrain finish failed exact source/scene/physics/data restoration.");
        }

        static List<VistaFinishHold> FinishTerrainHolds(DragonKingdomWorld world)
        {
            var holds=new List<VistaFinishHold>();
            foreach(var edge in world.definition.graph.edges)foreach(var sample in edge.samples)holds.Add(new VistaFinishHold{kind="ORIGINAL_ROAD_SAMPLE",path=edge.edgeId,point=sample.position,radius=sample.roadWidthMeters*.5f+10});
            foreach(var t in CriticalTransforms(world).Where(t=>t!=null).Distinct())if(t!=world.transform&&!world.regions.Any(r=>r.root==t))holds.Add(new VistaFinishHold{kind="ORIGINAL_START_EXTRACTION_AERIAL_OR_STRUCTURE",path=PathOf(t,world.transform),point=t.position,radius=16});
            foreach(var region in world.regions)foreach(Transform t in region.root)
            {var renderers=t.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();if(renderers.Length==0)continue;var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);bounds.Expand(new Vector3(8,0,8));holds.Add(new VistaFinishHold{kind="ACTUAL_EXISTING_SOURCE_ART_FOOTPRINT",path=PathOf(t,world.transform),bounds=bounds});}
            var cavern=DragonKingdomWorldAuthoring.CavernUndergroundBounds();cavern.Expand(40);holds.Add(new VistaFinishHold{kind="ACTUAL_DIGGER_ROOF_PORTAL_SHELL",path="Existing CavernUndergroundBounds plus20m",bounds=cavern});return holds;
        }
        static void StampFinishHold(float[,] weight,VistaFinishHold hold)
        {
            bool circle=hold.radius>0;var b=circle?new Bounds(hold.point,new Vector3(hold.radius*2,0,hold.radius*2)):hold.bounds;float f=FinishFeather;
            int x0=Mathf.Clamp(Mathf.FloorToInt((b.min.x-f+1600)/3.125f),0,1024),x1=Mathf.Clamp(Mathf.CeilToInt((b.max.x+f+1600)/3.125f),0,1024),z0=Mathf.Clamp(Mathf.FloorToInt((b.min.z-f+800)/3.125f),0,1024),z1=Mathf.Clamp(Mathf.CeilToInt((b.max.z+f+800)/3.125f),0,1024);
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){var p=new Vector3(-1600+x*3.125f,0,-800+z*3.125f);float distance=circle?Mathf.Max(0,Vector2.Distance(new Vector2(p.x,p.z),new Vector2(hold.point.x,hold.point.z))-hold.radius):DistanceTo(b,p);weight[z,x]=Mathf.Min(weight[z,x],Mathf.SmoothStep(0,1,distance/f));}
        }
        static void RunFinishPhysicalGates(DragonKingdomWorld world,VistaFinishDiagnosis proof)
        {DragonKingdomWorldAuthoring.ValidatePhysicalRoads(world.transform,world.definition.graph);proof.roadGatesExecuted++;DragonKingdomWorldAuthoring.ValidateTruckStarts(world,false);proof.startGatesExecuted++;ValidateContinuousExtractionApproaches(world,new Receipt());proof.extractionGatesExecuted++;}
        static void CaptureFinishViews(Camera camera,string output,string stage,List<VistaFinishFrame> frames)
        {
            var views=new[]{("forest-eye",new Vector3(-802.39154f,30,230.12761f),new Vector3(-760.36688f,54.64402f,275.89151f)),("castle-eye",new Vector3(30.76923f,66,286.15384f),new Vector3(0,102,540)),("mountains-eye",new Vector3(-58.48394f,306,1065.99292f),new Vector3(-5.68466f,320,1068.36157f)),("forest-overview",new Vector3(-920,460,20),new Vector3(-760,30,280)),("castle-overview",new Vector3(340,620,140),new Vector3(0,80,540))};
            foreach(var view in views)
            {
                camera.transform.position=view.Item2;camera.transform.LookAt(view.Item3);var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var prior=RenderTexture.active;
                try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();string png=Path.Combine(output,view.Item1+"-"+stage+".png");var bytes=texture.EncodeToPNG();File.WriteAllBytes(png,bytes);frames.Add(new VistaFinishFrame{view=view.Item1,stage=stage,png=png,sha256=FinishDigest(bytes),camera=view.Item2,target=view.Item3});}
                finally{camera.targetTexture=null;RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            }
        }
        static string FinishAlphaHash(TerrainData d){var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);var bytes=new byte[a.Length*4];Buffer.BlockCopy(a,0,bytes,0,bytes.Length);return FinishDigest(bytes);}
        static string FinishHoleHash(TerrainData d){var a=d.GetHoles(0,0,d.holesResolution,d.holesResolution);var bytes=new byte[a.Length];int i=0;foreach(bool value in a)bytes[i++]=value?(byte)1:(byte)0;return FinishDigest(bytes);}
        static string FinishDigest(byte[] bytes)=>Hash(bytes).Replace("-","").ToLowerInvariant();
        static byte[] FinishFloatBytes(float[,] values){var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);return bytes;}
        static string FinishFloatHash(float[,] values)=>FinishDigest(FinishFloatBytes(values));
        static Texture2D FinishInputTexture(float[,] values,bool inverse)
        {int n=values.GetLength(0);var texture=new Texture2D(n,n,TextureFormat.RFloat,false,true);var colors=new Color[n*n];for(int z=0;z<n;z++)for(int x=0;x<n;x++)colors[z*n+x]=new Color(inverse?1-values[z,x]:values[z,x],0,0,1);texture.SetPixels(colors);texture.Apply(false,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;return texture;}
        static RenderTexture FinishInputRt(Texture2D source){var rt=new RenderTexture(source.width,source.height,0,RenderTextureFormat.RFloat,RenderTextureReadWrite.Linear){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};rt.Create();Graphics.Blit(source,rt);return rt;}
        static float[,] FinishReadHeights(RenderTexture source,int n)
        {var before=RenderTexture.active;var t=new Texture2D(source.width,source.height,TextureFormat.RFloat,false,true);try{RenderTexture.active=source;t.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false);t.Apply(false,false);var result=new float[n,n];for(int z=0;z<n;z++)for(int x=0;x<n;x++)result[z,x]=t.GetPixelBilinear((float)x/(n-1),(float)z/(n-1)).r;return result;}finally{RenderTexture.active=before;UnityEngine.Object.DestroyImmediate(t);}}
        static float FinishLaplacian(float[,] values,float[,] weight)
        {double sum=0;int count=0,n=values.GetLength(0);for(int z=1;z<n-1;z++)for(int x=1;x<n-1;x++)if(weight[z,x]>=.99f){sum+=Math.Abs(4*values[z,x]-values[z-1,x]-values[z+1,x]-values[z,x-1]-values[z,x+1])*600;count++;}return count==0?0:(float)(sum/count);}
    }
}
#endif
