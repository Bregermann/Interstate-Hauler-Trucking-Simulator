#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.UnityTerrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit REVIEW candidate only. Vista owns every height sample, noise,
    // erosion, polygon mask, falloff and blend. LWS supplies regional semantics
    // and measured offsets. Roads/scenery/habitats are a later owned integration.
    public static class DragonVistaRegionalFoundationAuthoring
    {
        public const string DefaultCandidateFolder="Assets/TruckDragonKingdom/ReviewCandidates/VistaRegionalFoundation01";
        const string CandidatePrefix="Assets/TruckDragonKingdom/ReviewCandidates/";
        const string PrivatePrefix="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVista/";
        const string SourceGraph="Assets/PinwheelStudio/Vista/Personal/Samples/Graphs/MountainRange.asset";
        const string SourceWorld="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";
        const int HeightResolution=513,GraphResolution=512,Seed=28082026;
        const float ScaleY=600,BaseY=-20;
        [Serializable] public sealed class Receipt
        {
            public string scope="EXPLICIT_VISTA_REGIONAL_FOUNDATION_CANDIDATE_BEFORE_ROAD_GRADING_SCENERY_OR_GAMEPLAY_ACCEPTANCE";
            public string candidateFolder,privateGraphsFolder,sourceGraph=SourceGraph,unityVersion,status,error,prefab;
            public int seed=Seed,effectiveGraphResolution,effectiveHeightResolution,heightPopulationCallbacks;
            public double seconds;public bool sourceBytesPreserved,originalScenePreserved,temporaryObjectsRemoved;
            public BiomeRecord[] biomes;public TerrainRecord[] terrains;public RegionRecord[] regions;public WaterRecord[] water;public SourceRecord[] sources;
            public string[] validationIssues;
        }
        [Serializable] public sealed class BiomeRecord
        {
            public string id,graphPath,kind,heightBlend;public int order;public Vector3 center;public Vector3[] anchors;public Bounds worldBounds;
            public float falloff,desiredCenterY,sourceCenterNormalizedHeight,reliefMultiplier,normalizedOffset;
        }
        [Serializable] public sealed class TerrainRecord
        {
            public string id,path,png,floatMap,sha256;public Vector3 position,size;public int resolution,samples,nonFinite,zero,positive,over200,over400,ceilingSamples;
            public float minimumY,maximumY,meanY,standardDeviationY,maximumAdjacentStep,maximumSlopeDegrees;
            public Cell[] coverage;
        }
        [Serializable] public sealed class Cell { public int x,z,count,over200,over400;public float minimumY,maximumY,meanY; }
        [Serializable] public sealed class RegionRecord
        {public string id;public Vector3 center;public float requestedY,actualY,neighborhoodMinimumY,neighborhoodMaximumY,neighborhoodMeanY;public int neighborhoodSamples;}
        [Serializable] public sealed class WaterRecord {public string id;public Vector3 position;public float waterY,desiredBedY,actualBedY,depth;}
        [Serializable] public sealed class SourceRecord {public string path,before,after;}
        sealed class Tile {public int x,z;public TerrainData data;public GameObject gameObject;public TerrainTile vendor;public string id=>x+"-"+z;public Vector3 position=>new Vector3(-1600+x*1600,BaseY,-800+z*1600);}
        sealed class Region {public string id;public Vector3 center;public Bounds bounds;}
        sealed class Plan {public string id;public Vector3 center;public Vector2 half;public float falloff,relief;public bool crest;}

        public static IEnumerator CreateReviewCandidate()
        {
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonVistaCandidate");
            return CreateCandidate(at>=0&&at+1<args.Length?args[at+1]:DefaultCandidateFolder);
        }

        public static IEnumerator CreateCandidate(string outputFolder)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required for explicit candidate generation.");
            if(VistaManager.HasActiveTask()||Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Isolate vendor generation from all existing tasks and loaded terrains.");
            outputFolder=NormalizeCandidate(outputFolder);string leaf=outputFolder.Substring(CandidatePrefix.Length);string privateFolder=PrivatePrefix+leaf;
            if(Directory.Exists(outputFolder)||Directory.Exists(privateFolder))throw new InvalidOperationException("Preserve existing review outputs; select a fresh explicit candidate folder: "+outputFolder);
            var source=AssetDatabase.LoadAssetAtPath<TerrainGraph>(SourceGraph);var savedWorld=AssetDatabase.LoadAssetAtPath<GameObject>(SourceWorld);
            if(source==null||savedWorld==null)throw new InvalidOperationException("Actual owned source Vista graph and accepted world references are required.");
            var sourceTerrains=savedWorld.GetComponentsInChildren<Terrain>(true);if(sourceTerrains.Length!=4)throw new InvalidOperationException("Expected four accepted terrain references for material/layer/source preservation.");
            var sourceLayer=sourceTerrains.SelectMany(value=>value.terrainData.terrainLayers).FirstOrDefault(value=>value!=null&&value.diffuseTexture!=null);
            var sourceMaterial=sourceTerrains.Select(value=>value.materialTemplate).FirstOrDefault(value=>value!=null&&value.shader!=null&&value.shader.isSupported);
            if(sourceLayer==null||sourceMaterial==null)throw new InvalidOperationException("Existing genuine terrain material/diffuse layer required; no visual placeholder generation.");
            var regions=ReadRegions();var plans=BuildPlans(regions);var before=new Dictionary<string,string>();
            foreach(string path in AssetDatabase.GetDependencies(SourceGraph,true).Concat(AssetDatabase.GetDependencies(SourceWorld,true)).Concat(new[]{SourceGraph,SourceWorld,"Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset"}))Watch(path,before);
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Supply explicit -tsDragonOutput for actual native candidate evidence.");
            string evidence=Path.GetFullPath(Path.Combine(args[at+1],"VistaRegionalFoundation",leaf));
            if(evidence.StartsWith(Path.GetFullPath("Assets")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Diagnostic evidence must be outside Assets.");
            Directory.CreateDirectory(evidence);Directory.CreateDirectory(outputFolder+"/Terrain");Directory.CreateDirectory(privateFolder);AssetDatabase.Refresh();
            var receipt=new Receipt{candidateFolder=outputFolder,privateGraphsFolder=privateFolder,unityVersion=Application.unityVersion,status="generating_real_vendor_candidate",effectiveGraphResolution=Mathf.Min(GraphResolution,Constants.RES_MAX),effectiveHeightResolution=Mathf.Min(HeightResolution,Constants.HM_RES_MAX)};
            var original=SceneManager.GetActiveScene();var roots=original.IsValid()?original.GetRootGameObjects():Array.Empty<GameObject>();bool originalDirty=original.IsValid()&&original.isDirty;
            var preview=EditorSceneManager.NewPreviewScene();var root=new GameObject("Vista regional foundation REVIEW candidate");SceneManager.MoveGameObjectToScene(root,preview);
            var tiles=new List<Tile>();var biomeRecords=new List<BiomeRecord>();bool vendorOutstanding=false;double started=EditorApplication.timeSinceStartup;VistaManager manager=null;BiomeDataRequest request=null;Exception observationError=null;
            VistaManager.TexturePopulatedHandler observe=(sender,tile,texture)=>{if(sender!=manager)return;try{if(!tiles.Any(value=>ReferenceEquals(value.vendor,tile)))throw new InvalidOperationException("Vista touched an unowned tile.");receipt.heightPopulationCallbacks++;}catch(Exception error){observationError=error;}};
            VistaManager.heightMapPopulated+=observe;
            try
            {
                manager=VistaManager.CreateInstanceInScene();MoveUnder(manager.gameObject,root.transform,preview);manager.Reset();InitializeCallback(manager,"m_beforeGeneratingUnityCallback");InitializeCallback(manager,"m_afterGeneratingUnityCallback");
                manager.terrainMaxHeight=ScaleY;manager.heightMapResolution=HeightResolution;manager.textureResolution=512;manager.missingTextureAction=MissingOutputAction.Keep;
                for(int z=0;z<2;z++)for(int x=0;x<2;x++)
                {
                    var tile=new Tile{x=x,z=z,data=new TerrainData{heightmapResolution=HeightResolution,size=new Vector3(1600,ScaleY,1600)}};tiles.Add(tile);tile.data.name="Vista regional Kingdom "+x+" "+z;tile.data.terrainLayers=new[]{sourceLayer};
                    tile.gameObject=Terrain.CreateTerrainGameObject(tile.data);tile.gameObject.name="Vista terrain tile "+x+" "+z;MoveUnder(tile.gameObject,root.transform,preview);tile.gameObject.transform.position=tile.position;tile.gameObject.GetComponent<Terrain>().materialTemplate=sourceMaterial;
                    tile.vendor=tile.gameObject.AddComponent<TerrainTile>();tile.vendor.managerId=manager.id;
                }
                var baseGraph=CloneRelief(source,"Full coverage native MountainRange",1,0);SaveGraph(baseGraph,privateFolder+"/Full coverage MountainRange.asset");
                var baseBiome=CreateBiome(manager,baseGraph,new[]{new Vector3(-1800,0,-1000),new Vector3(1800,0,-1000),new Vector3(1800,0,2600),new Vector3(-1800,0,2600)},0,200,FalloffDirection.Inner,BiomeBlendOptions.HeightBlendMode.Replace);
                biomeRecords.Add(Record("full-coverage",baseBiome,"base",1,0,0,0));
                foreach(var plan in plans.OrderBy(value=>value.center.y).ThenBy(value=>value.id,StringComparer.Ordinal))
                {
                    // Measure the same genuine source graph in the final local
                    // biome bounds. Offset calibration changes graph parameters,
                    // never generates or edits a CPU height field.
                    var anchors=Rectangle(plan.center,plan.half);var calibration=CreateBiome(null,source,anchors,0,plan.falloff,FalloffDirection.Outer,BiomeBlendOptions.HeightBlendMode.Replace);
                    MoveUnder(calibration.gameObject,root.transform,preview);Bounds bounds=calibration.worldBounds;bounds.size=new Vector3(bounds.size.x,ScaleY,bounds.size.z);
                    request=calibration.RequestData(bounds,HeightResolution,512,BiomeDataMask.HeightMap);vendorOutstanding=true;double requestStarted=EditorApplication.timeSinceStartup;
                    while(!request.isCompleted){if(EditorApplication.timeSinceStartup-requestStarted>180)throw new TimeoutException("Actual Vista regional calibration did not finish: "+plan.id);yield return null;}
                    vendorOutstanding=false;if(request.data?.heightMap==null)throw new InvalidOperationException("Source graph did not supply actual calibration height: "+plan.id);
                    float sample=SampleTexture(request.data.heightMap,bounds,plan.center);request.data.Dispose();request=null;UnityEngine.Object.DestroyImmediate(calibration.gameObject);
                    float offset=(plan.center.y-BaseY)/ScaleY-sample*plan.relief;
                    var graph=CloneRelief(source,plan.id+" native relief and regional elevation",plan.relief,offset);SaveGraph(graph,privateFolder+"/"+plan.id+" MountainRange.asset");
                    var biome=CreateBiome(manager,graph,anchors,10+biomeRecords.Count,plan.falloff,FalloffDirection.Outer,BiomeBlendOptions.HeightBlendMode.Replace);
                    biomeRecords.Add(Record(plan.id,biome,plan.crest?"crest":"region",plan.relief,offset,sample,plan.center.y));
                    receipt.biomes=biomeRecords.ToArray();Write(evidence,receipt);
                }
                // Genuine vendor axial gradients follow the actual authored
                // RiverModeler centerline. Narrow bed + broad bank transition,
                // with no independent noise or hand-written height sampling.
                var canyon=ReadCanyon();
                for(int i=0;i<canyon.Length-1;i++)
                {
                    var a=canyon[i];var b=canyon[i+1];var graph=WaterGraph("Canyon bed "+i,a,b,a.y-3,i==canyon.Length-2?-8:b.y-3);SaveGraph(graph,privateFolder+"/Canyon bed "+i+".asset");
                    var biome=CreateBiome(manager,graph,SegmentRectangle(a,b,32,12),100+i,90,FalloffDirection.Outer,BiomeBlendOptions.HeightBlendMode.Replace);
                    biomeRecords.Add(Record("canyon-"+i,biome,"water-axial-gradient",1,0,0,(a.y+b.y)*.5f-3));
                }
                var lakeGraph=WaterGraph("Lake bed below actual vessel",new Vector3(-200,0,-930),new Vector3(1600,0,-930),-8,-8);SaveGraph(lakeGraph,privateFolder+"/Lake bed.asset");
                var lake=CreateBiome(manager,lakeGraph,new[]{new Vector3(-200,0,-1250),new Vector3(1800,0,-1250),new Vector3(1800,0,-655),new Vector3(-200,0,-655)},150,160,FalloffDirection.Outer,BiomeBlendOptions.HeightBlendMode.Replace);
                biomeRecords.Add(Record("lake-and-vessel-basin",lake,"water-lake",1,0,0,-8));receipt.biomes=biomeRecords.ToArray();
                var lava=ReadLava();for(int i=0;i<lava.Length-1;i++)
                {
                    var a=lava[i];var b=lava[i+1];var graph=WaterGraph("Actual molten channel bed "+i,a,b,a.y-3,b.y-3);SaveGraph(graph,privateFolder+"/Molten channel bed "+i+".asset");
                    var biome=CreateBiome(manager,graph,SegmentRectangle(a,b,30,12),160+i,90,FalloffDirection.Outer,BiomeBlendOptions.HeightBlendMode.Replace);biomeRecords.Add(Record("molten-channel-"+i,biome,"lava-axial-gradient",1,0,0,(a.y+b.y)*.5f-3));
                }
                receipt.biomes=biomeRecords.ToArray();
                if(manager.GetTileArray().Length!=4||manager.GetBiomes().Length!=biomeRecords.Count)throw new InvalidOperationException("Vendor candidate must collect exactly its four own tiles and all authored biomes.");
                var generation=manager.GenerateAll();vendorOutstanding=true;double generateStarted=EditorApplication.timeSinceStartup;
                while(!generation.isCompleted){if(EditorApplication.timeSinceStartup-generateStarted>900)throw new TimeoutException("Actual vendor regional foundation generation did not finish.");yield return null;}
                vendorOutstanding=false;if(observationError!=null)throw observationError;if(receipt.heightPopulationCallbacks!=4)throw new InvalidOperationException("Four genuine Vista height population callbacks required.");
                receipt.terrains=tiles.Select(tile=>Measure(tile,outputFolder,evidence)).ToArray();receipt.regions=regions.Select(region=>MeasureRegion(region,tiles)).ToArray();receipt.water=MeasureWater(canyon,lava,tiles);
                var issues=Validate(receipt);receipt.validationIssues=issues.ToArray();receipt.seconds=EditorApplication.timeSinceStartup-started;
                foreach(var tile in tiles){string path=outputFolder+"/Terrain/Kingdom "+tile.x+" "+tile.z+".asset";AssetDatabase.CreateAsset(tile.data,path);AssetDatabase.SaveAssetIfDirty(tile.data);}
                receipt.prefab=outputFolder+"/Vista Regional Foundation.prefab";if(PrefabUtility.SaveAsPrefabAsset(root,receipt.prefab)==null)throw new InvalidOperationException("Failed to retain actual candidate configuration/terrain references.");
                receipt.status=issues.Count==0?"MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW":"MEASURED_CANDIDATE_REJECTED_BY_PREGRADING_GATES";
            }
            finally
            {
                VistaManager.heightMapPopulated-=observe;
                // Installed cancellation does not stop nested GPU work. Retain
                // unsaved preview on timeout for batch-process teardown instead
                // of disposing resources that a vendor coroutine still owns.
                if(!vendorOutstanding)
                {
                    request?.data?.Dispose();if(root!=null)UnityEngine.Object.DestroyImmediate(root);foreach(var tile in tiles)if(tile.data!=null&&!AssetDatabase.Contains(tile.data))UnityEngine.Object.DestroyImmediate(tile.data);
                    if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
                }
                else receipt.error="Vendor work still running; unsaved isolated preview retained for process teardown, never claimed cancelled.";
                receipt.sources=before.Select(pair=>new SourceRecord{path=pair.Key,before=pair.Value,after=File.Exists(IoPath(pair.Key))?Hash(File.ReadAllBytes(IoPath(pair.Key))):"MISSING"}).ToArray();receipt.sourceBytesPreserved=receipt.sources.All(value=>value.before==value.after);
                receipt.originalScenePreserved=SceneManager.GetActiveScene()==original&&(!original.IsValid()||(original.isDirty==originalDirty&&roots.SequenceEqual(original.GetRootGameObjects())));
                receipt.temporaryObjectsRemoved=!vendorOutstanding&&!VistaManager.HasActiveTask()&&Terrain.activeTerrains.Length==0;Write(evidence,receipt);
                if(receipt.prefab!=null)File.WriteAllText(outputFolder+"/Foundation Receipt.json",JsonUtility.ToJson(receipt,true));
            }
            if(!receipt.sourceBytesPreserved||!receipt.originalScenePreserved||!receipt.temporaryObjectsRemoved)throw new InvalidOperationException("Candidate authoring changed protected sources or did not clean up; inspect receipt.");
            if(receipt.validationIssues.Length!=0)throw new InvalidOperationException("Actual pregrading regional candidate rejected: "+string.Join(" | ",receipt.validationIssues));
            Debug.Log("VISTA_REGIONAL_CANDIDATE_MEASURED "+receipt.prefab+" (no road/scenery/visual/gameplay acceptance)");
        }

        static Plan[] BuildPlans(Region[] regions)
        {
            var plans=new List<Plan>();
            foreach(var region in regions)
            {
                var half=new Vector2(Mathf.Clamp(region.bounds.extents.x*.65f,75,150),Mathf.Clamp(region.bounds.extents.z*.65f,75,150));float separation=float.MaxValue;
                foreach(var other in regions)if(other!=region){float dx=Mathf.Max(0,Mathf.Abs(other.center.x-region.center.x)-half.x),dz=Mathf.Max(0,Mathf.Abs(other.center.z-region.center.z)-half.y);separation=Mathf.Min(separation,Mathf.Sqrt(dx*dx+dz*dz));}
                plans.Add(new Plan{id=region.id,center=region.center,half=half,falloff=Mathf.Clamp(separation*.8f,200,500),relief=region.center.y>=100?3f:1.25f});
            }
            plans.Add(new Plan{id="wyrmspine-northern-crest",center=new Vector3(120,440,1540),half=new Vector2(120,120),falloff=350,relief=2.5f,crest=true});
            plans.Add(new Plan{id="stoneheart-back-crest",center=new Vector3(-680,490,1560),half=new Vector2(120,120),falloff=300,relief=2.5f,crest=true});
            plans.Add(new Plan{id="ashen-crest",center=new Vector3(1280,460,1160),half=new Vector2(100,100),falloff=250,relief=2.5f,crest=true});
            plans.Add(new Plan{id="skycradle-back-crest",center=new Vector3(-1180,430,1220),half=new Vector2(100,100),falloff=260,relief=2.5f,crest=true});return plans.ToArray();
        }
        static TerrainGraph CloneRelief(TerrainGraph source,string name,float multiplier,float offset)
        {
            var graph=UnityEngine.Object.Instantiate(source);graph.name=name;var output=graph.GetNodesOfType<HeightOutputNode>().Single();var edge=graph.GetEdges().Single(value=>value.inputSlot.nodeId==output.id&&value.inputSlot.slotId==output.inputSlot.id);var upstream=edge.outputSlot;graph.RemoveEdge(edge.id);
            var math=new MathNode{maths=new List<MathNode.MathConfig>{new MathNode.MathConfig{enabled=true,ops=MathNode.Operator.Multiply,number=multiplier},new MathNode.MathConfig{enabled=true,ops=MathNode.Operator.Add,number=offset}}};graph.AddNode(math);
            graph.AddEdge(new Edge{outputSlot=upstream,inputSlot=new SlotRef(math.id,math.inputSlot.id)});graph.AddEdge(new Edge{outputSlot=new SlotRef(math.id,math.outputSlot.id),inputSlot=new SlotRef(output.id,output.inputSlot.id)});graph.OnBeforeSerialize();return graph;
        }
        static TerrainGraph WaterGraph(string name,Vector3 a,Vector3 b,float bedA,float bedB)
        {
            var graph=ScriptableObject.CreateInstance<TerrainGraph>();graph.Reset();graph.name=name;
            var gradient=new AxialGradientNode{startPoint=new Vector2(a.x,a.z),endPoint=new Vector2(b.x,b.z),useWorldPosition=true,repeat=false};
            var math=new MathNode{maths=new List<MathNode.MathConfig>{new MathNode.MathConfig{enabled=true,ops=MathNode.Operator.Multiply,number=(bedB-bedA)/ScaleY},new MathNode.MathConfig{enabled=true,ops=MathNode.Operator.Add,number=(bedA-BaseY)/ScaleY}}};var output=new HeightOutputNode();
            graph.AddNode(gradient);graph.AddNode(math);graph.AddNode(output);graph.AddEdge(new Edge{outputSlot=new SlotRef(gradient.id,gradient.outputSlot.id),inputSlot=new SlotRef(math.id,math.inputSlot.id)});graph.AddEdge(new Edge{outputSlot=new SlotRef(math.id,math.outputSlot.id),inputSlot=new SlotRef(output.id,output.inputSlot.id)});graph.OnBeforeSerialize();return graph;
        }
        static LocalProceduralBiome CreateBiome(VistaManager manager,TerrainGraph graph,Vector3[] anchors,int order,float falloff,FalloffDirection direction,BiomeBlendOptions.HeightBlendMode blend)
        {
            var biome=LocalProceduralBiome.CreateInstanceInScene(manager);biome.Reset();biome.name=graph.name;biome.terrainGraph=graph;biome.seed=Seed;biome.baseResolution=GraphResolution;biome.dataMask=BiomeDataMask.HeightMap;biome.anchors=anchors;biome.order=order;biome.falloffDirection=direction;biome.falloffDistance=falloff;
            var options=biome.blendOptions;options.heightMapBlendMode=blend;biome.blendOptions=options;return biome;
        }
        static BiomeRecord Record(string id,LocalProceduralBiome biome,string kind,float relief,float offset,float sourceCenter,float desired)
        {return new BiomeRecord{id=id,graphPath=AssetDatabase.GetAssetPath(biome.terrainGraph),kind=kind,order=biome.order,center=biome.worldBounds.center,anchors=biome.anchors,worldBounds=biome.worldBounds,falloff=biome.falloffDistance,heightBlend=biome.blendOptions.heightMapBlendMode.ToString(),reliefMultiplier=relief,normalizedOffset=offset,sourceCenterNormalizedHeight=sourceCenter,desiredCenterY=desired};}
        static Region[] ReadRegions()
        {
            var definition=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset");if(definition==null)throw new InvalidOperationException("Accepted semantic regions required.");
            var values=(Array)definition.GetType().GetField("regions").GetValue(definition);return values.Cast<object>().Select(value=>new Region{id=(string)value.GetType().GetField("id").GetValue(value),center=(Vector3)value.GetType().GetField("center").GetValue(value),bounds=(Bounds)value.GetType().GetField("bounds").GetValue(value)}).ToArray();
        }
        static Vector3[] ReadCanyon()
        {var type=typeof(DragonVistaRegionalFoundationAuthoring).Assembly.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring",true);return (Vector3[])type.GetMethod("CanyonWaterPolyline",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);}
        // Exact existing RiverModeler controls/55m width supplied by world owner.
        // These are composition anchors, never an alternative river generator.
        static Vector3[] ReadLava()=>new[]{new Vector3(1370,330,1050),new Vector3(1450,260,900),new Vector3(1410,160,600),new Vector3(1300,90,430)};
        static Vector3[] Rectangle(Vector3 center,Vector2 half)=>new[]{new Vector3(center.x-half.x,0,center.z-half.y),new Vector3(center.x+half.x,0,center.z-half.y),new Vector3(center.x+half.x,0,center.z+half.y),new Vector3(center.x-half.x,0,center.z+half.y)};
        static Vector3[] SegmentRectangle(Vector3 a,Vector3 b,float half,float extension)
        {var along=new Vector3(b.x-a.x,0,b.z-a.z).normalized;var side=Vector3.Cross(Vector3.up,along)*half;a=new Vector3(a.x,0,a.z)-along*extension;b=new Vector3(b.x,0,b.z)+along*extension;return new[]{a-side,a+side,b+side,b-side};}
        static float SampleTexture(RenderTexture source,Bounds bounds,Vector3 point)
        {
            var prior=RenderTexture.active;var texture=new Texture2D(source.width,source.height,TextureFormat.RFloat,false,true);
            try{RenderTexture.active=source;texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false);texture.Apply(false,false);float value=texture.GetPixelBilinear((point.x-bounds.min.x)/bounds.size.x,(point.z-bounds.min.z)/bounds.size.z).r;if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidOperationException("Nonfinite genuine source calibration.");return value;}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);}
        }
        static TerrainRecord Measure(Tile tile,string folder,string output)
        {
            int n=tile.data.heightmapResolution;float[,] values=tile.data.GetHeights(0,0,n,n);var record=new TerrainRecord{id=tile.id,path=folder+"/Terrain/Kingdom "+tile.x+" "+tile.z+".asset",position=tile.position,size=tile.data.size,resolution=n,samples=n*n,minimumY=float.MaxValue,maximumY=float.MinValue};
            record.coverage=Enumerable.Range(0,256).Select(index=>new Cell{x=index%16,z=index/16,minimumY=float.MaxValue,maximumY=float.MinValue}).ToArray();var raw=new float[n*n];var colors=new Color32[n*n];double sum=0,squares=0;float spacing=1600f/(n-1);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float normalized=values[z,x],height=tile.position.y+tile.data.size.y*normalized;int index=z*n+x;raw[index]=normalized;
                if(float.IsNaN(height)||float.IsInfinity(height)){record.nonFinite++;colors[index]=new Color32(255,0,255,255);continue;}
                if(normalized==0)record.zero++;if(normalized>0)record.positive++;if(normalized>=.99997f)record.ceilingSamples++;if(height>=200)record.over200++;if(height>=400)record.over400++;
                record.minimumY=Mathf.Min(record.minimumY,height);record.maximumY=Mathf.Max(record.maximumY,height);sum+=height;squares+=(double)height*height;
                float step=Mathf.Max(x>0?Mathf.Abs(normalized-values[z,x-1])*tile.data.size.y:0,z>0?Mathf.Abs(normalized-values[z-1,x])*tile.data.size.y:0);record.maximumAdjacentStep=Mathf.Max(record.maximumAdjacentStep,step);
                var cell=record.coverage[Mathf.Min(15,z*16/n)*16+Mathf.Min(15,x*16/n)];cell.count++;cell.minimumY=Mathf.Min(cell.minimumY,height);cell.maximumY=Mathf.Max(cell.maximumY,height);cell.meanY+=height;if(height>=200)cell.over200++;if(height>=400)cell.over400++;
                byte c=(byte)Mathf.RoundToInt(Mathf.Clamp01(normalized)*255);colors[index]=new Color32(c,c,c,255);
            }
            record.meanY=(float)(sum/(record.samples-record.nonFinite));record.standardDeviationY=(float)Math.Sqrt(Math.Max(0,squares/(record.samples-record.nonFinite)-(double)record.meanY*record.meanY));record.maximumSlopeDegrees=Mathf.Atan(record.maximumAdjacentStep/spacing)*Mathf.Rad2Deg;foreach(var cell in record.coverage)if(cell.count>0)cell.meanY/=cell.count;
            var bytes=new byte[raw.Length*sizeof(float)];Buffer.BlockCopy(raw,0,bytes,0,bytes.Length);record.sha256=Hash(bytes);record.floatMap="ungraded-"+tile.id+".float32";File.WriteAllBytes(Path.Combine(output,record.floatMap),bytes);
            var image=new Texture2D(n,n,TextureFormat.RGBA32,false,true);try{image.SetPixels32(colors);image.Apply(false,false);record.png="ungraded-"+tile.id+".png";File.WriteAllBytes(Path.Combine(output,record.png),image.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(image);}return record;
        }
        static RegionRecord MeasureRegion(Region region,List<Tile> tiles)
        {
            var result=new RegionRecord{id=region.id,center=region.center,requestedY=region.center.y,actualY=Sample(tiles,region.center),neighborhoodMinimumY=float.MaxValue,neighborhoodMaximumY=float.MinValue};
            for(int z=-240;z<=240;z+=40)for(int x=-240;x<=240;x+=40){float y=Sample(tiles,region.center+new Vector3(x,0,z));result.neighborhoodSamples++;result.neighborhoodMinimumY=Mathf.Min(result.neighborhoodMinimumY,y);result.neighborhoodMaximumY=Mathf.Max(result.neighborhoodMaximumY,y);result.neighborhoodMeanY+=y;}result.neighborhoodMeanY/=result.neighborhoodSamples;return result;
        }
        static WaterRecord[] MeasureWater(Vector3[] canyon,Vector3[] lava,List<Tile> tiles)
        {
            var values=new List<WaterRecord>();for(int i=0;i<canyon.Length-1;i++)for(int j=0;j<=4;j++){float t=j*.25f;var point=Vector3.Lerp(canyon[i],canyon[i+1],t);if(point.z< -800)continue;float bed=Mathf.Lerp(canyon[i].y-3,i==canyon.Length-2?-8:canyon[i+1].y-3,t);float actual=Sample(tiles,point);values.Add(new WaterRecord{id="canyon-"+i+"-"+j,position=point,waterY=point.y,desiredBedY=bed,actualBedY=actual,depth=point.y-actual});}
            foreach(var point in new[]{new Vector3(720,-2,-685),new Vector3(708,-2,-711),new Vector3(732,-2,-711),new Vector3(708,-2,-661),new Vector3(732,-2,-661),new Vector3(100,-2,-760),new Vector3(1300,-2,-760)}){float actual=Sample(tiles,point);values.Add(new WaterRecord{id="lake-vessel",position=point,waterY=-2,desiredBedY=-8,actualBedY=actual,depth=-2-actual});}
            for(int i=0;i<lava.Length-1;i++)for(int j=0;j<=4;j++){var point=Vector3.Lerp(lava[i],lava[i+1],j*.25f);float actual=Sample(tiles,point);values.Add(new WaterRecord{id="molten-channel-"+i+"-"+j,position=point,waterY=point.y,desiredBedY=point.y-3,actualBedY=actual,depth=point.y-actual});}return values.ToArray();
        }
        static float Sample(List<Tile> tiles,Vector3 point)
        {var tile=tiles.FirstOrDefault(value=>point.x>=value.position.x&&point.x<=value.position.x+1600&&point.z>=value.position.z&&point.z<=value.position.z+1600);if(tile==null)throw new InvalidOperationException("Measurement outside candidate footprint: "+point);return tile.position.y+tile.data.GetInterpolatedHeight((point.x-tile.position.x)/1600,(point.z-tile.position.z)/1600);}
        static List<string> Validate(Receipt receipt)
        {
            var issues=new List<string>();foreach(var tile in receipt.terrains){if(tile.nonFinite!=0)issues.Add(tile.id+" nonfinite vendor heights");if(tile.resolution!=HeightResolution)issues.Add(tile.id+" actual supported resolution mismatch");if(tile.ceilingSamples!=0)issues.Add(tile.id+" actual height ceiling clipping");}
            foreach(var region in receipt.regions){float tolerance=region.requestedY>=100?60:30;if(Mathf.Abs(region.actualY-region.requestedY)>tolerance)issues.Add(region.id+" center "+region.actualY+" differs from regional base "+region.requestedY);}
            if(!receipt.terrains.Any(tile=>tile.maximumY>=400&&tile.maximumY<580))issues.Add("No actual unclipped400–580m native mountain peak");
            if(receipt.terrains.Sum(tile=>tile.over200)<receipt.terrains.Sum(tile=>tile.samples)*.04f)issues.Add("Insufficient broad actual highland mass above200m");
            foreach(var water in receipt.water){if(water.actualBedY>water.waterY+1.5f)issues.Add(water.id+" actual terrain buries visible water");if(water.id=="lake-vessel"&&Mathf.Abs(water.actualBedY+8)>1)issues.Add("Lake/vessel actual bed must remain−8m without static raised hull support");}return issues;
        }
        static void SaveGraph(TerrainGraph graph,string path){AssetDatabase.CreateAsset(graph,path);EditorUtility.SetDirty(graph);AssetDatabase.SaveAssetIfDirty(graph);}
        static void MoveUnder(GameObject value,Transform parent,Scene scene){SceneManager.MoveGameObjectToScene(value,scene);value.transform.SetParent(parent,false);}
        static string NormalizeCandidate(string path){if(string.IsNullOrEmpty(path))throw new ArgumentException("Explicit candidate path required.");path=path.Replace('\\','/').TrimEnd('/');if(!path.StartsWith(CandidatePrefix,StringComparison.Ordinal)||path.Contains("..")||path.Substring(CandidatePrefix.Length).Contains("/"))throw new ArgumentException("Use one fresh caller-owned ReviewCandidates/<id> folder.");return path;}
        static void InitializeCallback(VistaManager manager,string name){var field=typeof(VistaManager).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(field==null||field.FieldType!=typeof(UnityEvent))throw new InvalidOperationException("Installed Vista callback schema changed.");if(field.GetValue(manager)==null)field.SetValue(manager,new UnityEvent());}
        // The isolated validation root makes some genuine vendor metadata paths
        // reach MAX_PATH. Preserve their bytes too, rather than skipping them.
        static string IoPath(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static void Watch(string path,Dictionary<string,string> before){if(string.IsNullOrEmpty(path)||!File.Exists(IoPath(path)))return;before[path]=Hash(File.ReadAllBytes(IoPath(path)));if(File.Exists(IoPath(path+".meta")))before[path+".meta"]=Hash(File.ReadAllBytes(IoPath(path+".meta")));}
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static void Write(string evidence,Receipt receipt)=>File.WriteAllText(Path.Combine(evidence,"report.json"),JsonUtility.ToJson(receipt,true));
    }
}
#endif
