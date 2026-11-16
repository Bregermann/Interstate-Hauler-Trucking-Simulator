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
    // Explicit diagnostic, not a world builder or a replacement terrain algorithm.
    // All generation below is the installed Vista graph/manager/tile pipeline.
    // No source scene, prefab, graph, TerrainData or other asset is saved.
    public static class DragonVistaFoundationProbe
    {
        const string GraphPath="Assets/PinwheelStudio/Vista/Personal/Samples/Graphs/MountainRange.asset";
        const string WorldPrefab="Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";
        const string DefinitionPath="Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset";
        const int Resolution=1025, Seed=28082026;
        const float TileWidth=1600, HeightScale=600, WorldBase=-20;
        [Serializable] sealed class Report
        {
            public string scope="READ_ONLY_UNGRADED_INSTALLED_VISTA_OUTPUT_DIAGNOSIS_NOT_WORLD_VISUAL_OR_GAMEPLAY_ACCEPTANCE";
            public string unityVersion,graph=GraphPath,error;
            public int seed=Seed,biomeBaseResolution=1024,tileHeightResolution=Resolution;
            public float currentSouthernTileExcludedFraction=.90625f;
            public string comparison="Only the biome anchor polygon changes. Full-coverage case is a diagnostic counterfactual, not an adopted world repair. Both cases retain the graph, seed, World space, defaults, 100m inner falloff, 600m height range and installed Vista pipeline. No road grading runs.";
            public Dataset[] savedTerrains; public Run[] runs; public SourceBytes[] sourceBytes;
            public bool savedSourceBytesPreserved,originalScenePreserved,temporaryObjectsRemoved;
        }
        [Serializable] sealed class SourceBytes { public string path,before,after; }
        [Serializable] sealed class Run
        {
            public string id,status,error;public Vector3[] anchors;public Bounds biomeBounds;
            public string biomeSpace,falloffDirection,dataMask;public float falloffDistance;
            public int collectedTiles,collectedBiomes,heightPopulationCallbacks;public double seconds;
            public Dataset rawGraphHeight,rawBiomeMask;public Dataset[] blendedTileOutputs,ungradedTiles;
            public Region[] regions;
        }
        [Serializable] sealed class Region
        {
            public string id;public Vector3 authoredCenter;public bool insideAnchorRectangle;
            public float distanceToAnchorBoundary,savedWorldHeight,ungradedWorldHeight;
        }
        [Serializable] sealed class Dataset
        {
            public string id,stage,source,png,sha256,units="normalized_height";public int width,height,samples,nonFinite,zero,positive,negative,aboveOne;
            public float minimum,maximum,mean,standardDeviation,worldMinimum,worldMaximum,nonzeroFraction;
            public float[] histogramUpperBounds={0,.001f,.01f,.05f,.1f,.25f,.5f,.75f,1,float.MaxValue};public int[] histogram;
            public int coverageColumns=16,coverageRows=16;public Cell[] coverage;
        }
        [Serializable] sealed class Cell { public int x,z,samples,positive;public float minimum,maximum,mean; }
        sealed class Tile
        { public int x,z; public TerrainData data;public GameObject gameObject;public TerrainTile vendor;public Vector3 position;public string id=>x+"-"+z; }

        // Caller must yield this iterator in the native Editor. This method does
        // not start a second Editor/process or schedule an automatic regeneration.
        public static IEnumerator CompareUngradedVendorOutput()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            if(VistaManager.HasActiveTask())throw new InvalidOperationException("An existing vendor generation task must finish before this isolated probe.");
            if(Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Close loaded terrain scenes first: vendor MatchSeams must never see an accepted terrain in this probe.");
            var graph=AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphPath);if(graph==null)throw new InvalidOperationException("The actual installed MountainRange graph is required.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1==args.Length)throw new InvalidOperationException("Supply explicit -tsDragonOutput evidence folder.");
            string output=Path.GetFullPath(Path.Combine(args[at+1],"VistaFoundation"));
            if(output.StartsWith(Path.GetFullPath("Assets")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Evidence must be outside Assets.");
            Directory.CreateDirectory(output);
            var report=new Report{unityVersion=Application.unityVersion};var runs=new List<Run>();var sources=new Dictionary<string,string>();
            var saved=new List<Tile>();var original=SceneManager.GetActiveScene();var originalRoots=original.IsValid()?original.GetRootGameObjects():Array.Empty<GameObject>();bool originalDirty=original.IsValid()&&original.isDirty;
            foreach(string path in AssetDatabase.GetDependencies(GraphPath,true))Watch(path,sources);
            foreach(string path in new[]{GraphPath,DefinitionPath,WorldPrefab,"Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity"})Watch(path,sources);
            var savedWorld=AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefab);if(savedWorld==null)throw new InvalidOperationException("Existing accepted world prefab required; no generation by this probe.");
            var actualTerrains=savedWorld.GetComponentsInChildren<Terrain>(true);if(actualTerrains.Length!=4)throw new InvalidOperationException("Accepted prefab must identify its four actual terrain references.");
            for(int z=0;z<2;z++)for(int x=0;x<2;x++)
            {
                var actual=actualTerrains.SingleOrDefault(value=>(value.transform.position-Position(x,z)).sqrMagnitude<.0001f);
                if(actual==null||actual.terrainData==null)throw new InvalidOperationException("Actual saved world tile position/data does not match builder bounds: "+x+","+z);
                var data=actual.terrainData;string path=AssetDatabase.GetAssetPath(data);
                if(string.IsNullOrEmpty(path)||(data.size-new Vector3(TileWidth,HeightScale,TileWidth)).sqrMagnitude>.0001f)throw new InvalidOperationException("Persistent actual 1600x600x1600 terrain required: "+path);
                Watch(path,sources);foreach(var layer in data.terrainLayers)if(layer!=null)Watch(AssetDatabase.GetAssetPath(layer),sources);
                saved.Add(new Tile{x=x,z=z,data=data,position=Position(x,z)});
            }
            report.savedTerrains=saved.Select(tile=>ReadTerrain(tile,"saved_after_world_grading",output)).ToArray();WriteReport(output,report);
            // No ordinary scene is opened, closed, saved or deactivated. The
            // temporary preview owns every generated object and TerrainData.
            try
            {
                foreach(bool fullCoverage in new[]{false,true})
                {
                    var run=new Run{id=fullCoverage?"full_coverage_counterfactual":"exact_builder_coverage",status="started"};runs.Add(run);report.runs=runs.ToArray();WriteReport(output,report);
                    var iterator=RunCase(run,graph,saved,output,fullCoverage);
                    using var iteratorLifetime=iterator as IDisposable;
                    // Advance explicitly so a vendor/diagnostic exception is
                    // retained in JSON without converting it to a success.
                    while(true)
                    {
                        bool next;object current;
                        try{next=iterator.MoveNext();current=next?iterator.Current:null;}
                        catch(Exception error){run.error=error.ToString();run.status="failed";report.error=error.ToString();WriteReport(output,report);throw;}
                        if(!next)break;yield return current;
                    }
                    WriteReport(output,report);
                }
            }
            finally
            {
                report.runs=runs.ToArray();report.sourceBytes=sources.Select(pair=>new SourceBytes{path=pair.Key,before=pair.Value,after=File.Exists(pair.Key)?Hash(File.ReadAllBytes(pair.Key)):"MISSING"}).ToArray();
                report.savedSourceBytesPreserved=report.sourceBytes.All(value=>value.before==value.after);
                report.originalScenePreserved=SceneManager.GetActiveScene()==original&&(!original.IsValid()||(original.isDirty==originalDirty&&originalRoots.SequenceEqual(original.GetRootGameObjects())));
                report.temporaryObjectsRemoved=!VistaManager.HasActiveTask()&&Terrain.activeTerrains.Length==0;
                WriteReport(output,report);
            }
            if(!report.savedSourceBytesPreserved||!report.originalScenePreserved||!report.temporaryObjectsRemoved)throw new InvalidOperationException("Read-only Vista probe preservation failed; inspect receipt.");
            Debug.Log("VISTA_FOUNDATION_DIAGNOSIS_RECORDED "+Path.Combine(output,"report.json")+" (measurements only, no world acceptance)");
        }

        static IEnumerator RunCase(Run run,TerrainGraph graph,List<Tile> saved,string output,bool full)
        {
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Read-only Vista foundation "+run.id);SceneManager.MoveGameObjectToScene(root,scene);
            var tiles=new List<Tile>();VistaManager manager=null;BiomeDataRequest raw=null;ProgressiveTask generated=null;bool vendorOutstanding=false;double started=EditorApplication.timeSinceStartup;
            var blended=new List<Dataset>();Exception callbackError=null;
            VistaManager.TexturePopulatedHandler observer=(sender,tile,texture)=>
            {
                if(sender!=manager)return;
                try
                {
                    var own=tiles.SingleOrDefault(value=>ReferenceEquals(value.vendor,tile));if(own==null)throw new InvalidOperationException("Vendor population attempted a tile outside the probe.");
                    run.heightPopulationCallbacks++;blended.Add(ReadTexture(texture,run.id+"_tile_"+own.id+"_blended","actual_Vista_heightMapPopulated_callback",GraphPath,output));
                }
                catch(Exception error){callbackError=error;}
            };
            VistaManager.heightMapPopulated+=observer;
            try
            {
                manager=VistaManager.CreateInstanceInScene();SceneManager.MoveGameObjectToScene(manager.gameObject,scene);manager.transform.SetParent(root.transform,false);manager.Reset();
                InitializeCallback(manager,"m_beforeGeneratingUnityCallback");InitializeCallback(manager,"m_afterGeneratingUnityCallback");manager.terrainMaxHeight=HeightScale;manager.heightMapResolution=Resolution;manager.missingTextureAction=MissingOutputAction.Keep;
                var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Dark/Grass_Dark_VistaSample.terrainlayer");if(layer==null)throw new InvalidOperationException("Actual initial vendor grass layer required.");
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/PinwheelStudio/Vista/Personal/Samples/Materials/VistaSample_Terrain.mat");if(material==null)throw new InvalidOperationException("Actual vendor initial terrain material required.");
                for(int z=0;z<2;z++)for(int x=0;x<2;x++)
                {
                    var tile=new Tile{x=x,z=z,position=Position(x,z),data=new TerrainData{heightmapResolution=Resolution,size=new Vector3(TileWidth,HeightScale,TileWidth)}};
                    tiles.Add(tile);tile.data.terrainLayers=new[]{layer};tile.gameObject=Terrain.CreateTerrainGameObject(tile.data);tile.gameObject.name="Vista isolated tile "+tile.id;
                    SceneManager.MoveGameObjectToScene(tile.gameObject,scene);tile.gameObject.transform.SetParent(root.transform,false);tile.gameObject.transform.position=tile.position;tile.gameObject.GetComponent<Terrain>().materialTemplate=material;tile.vendor=tile.gameObject.AddComponent<TerrainTile>();tile.vendor.managerId=manager.id;
                }
                var biome=LocalProceduralBiome.CreateInstanceInScene(manager);biome.Reset();biome.terrainGraph=graph;biome.baseResolution=1024;biome.seed=Seed;
                float south=full?-800:650;biome.anchors=new[]{new Vector3(-1600,0,south),new Vector3(1600,0,south),new Vector3(1600,0,2400),new Vector3(-1600,0,2400)};
                run.anchors=biome.anchors;run.biomeBounds=biome.worldBounds;run.biomeSpace=biome.space.ToString();run.falloffDirection=biome.falloffDirection.ToString();run.falloffDistance=biome.falloffDistance;run.dataMask=biome.dataMask.ToString();run.collectedTiles=manager.GetTileArray().Length;
                if(run.collectedTiles!=4||manager.GetTileArray().Any(value=>!tiles.Any(own=>ReferenceEquals(own.vendor,value))))throw new InvalidOperationException("Vista must collect exactly the four in-memory probe tiles.");
                var collectedBiomes=manager.GetBiomes();run.collectedBiomes=collectedBiomes.Length;
                if(collectedBiomes.Length!=1||!ReferenceEquals(collectedBiomes[0],biome))throw new InvalidOperationException("Vista must consume exactly the actual probe biome, without other loaded/free biomes.");
                // Request genuine pre-blending graph output in the biome's own
                // bounds. The manager then reuses this vendor-owned cache; no
                // consumer can synthesize or replace a height texture here.
                var bounds=biome.worldBounds;bounds.size=new Vector3(bounds.size.x,HeightScale,bounds.size.z);
                raw=biome.RequestData(bounds,Resolution,manager.textureResolution);vendorOutstanding=true;
                while(!raw.isCompleted){if(EditorApplication.timeSinceStartup-started>300)throw new TimeoutException("Actual Vista biome RequestData did not finish; no synthetic cancellation or success.");yield return null;}
                vendorOutstanding=false;
                if(raw.data==null||raw.data.heightMap==null||raw.data.biomeMaskMap==null)throw new InvalidOperationException("Installed graph must actually produce height and biome-mask textures; completion alone is insufficient.");
                run.rawGraphHeight=ReadTexture(raw.data.heightMap,run.id+"_raw_graph_height","actual_biome_RequestData_before_tile_blending",GraphPath,output);
                run.rawBiomeMask=ReadTexture(raw.data.biomeMaskMap,run.id+"_raw_biome_mask","actual_biome_RequestData_mask",GraphPath,output);raw.data.Dispose();raw=null;
                generated=manager.GenerateAll();vendorOutstanding=true;
                while(!generated.isCompleted){if(EditorApplication.timeSinceStartup-started>600)throw new TimeoutException("Actual Vista GenerateAll did not finish; no synthetic cancellation or success.");yield return null;}
                vendorOutstanding=false;if(callbackError!=null)throw callbackError;
                run.blendedTileOutputs=blended.ToArray();run.ungradedTiles=tiles.Select(tile=>ReadTerrain(tile,run.id+"_ungraded",output)).ToArray();run.regions=ReadRegions(saved,tiles,south);run.seconds=EditorApplication.timeSinceStartup-started;
                if(run.heightPopulationCallbacks!=4||blended.Select(value=>value.id).Distinct().Count()!=4)throw new InvalidOperationException("Expected four actual vendor height-population callbacks, not task-completion-only evidence.");
                run.status="measured_not_world_accepted";
            }
            finally
            {
                VistaManager.heightMapPopulated-=observer;
                // Installed CancelActiveGenerateTask only marks a task complete;
                // it does not cancel nested graph coroutines. On timeout retain
                // the unsaved isolated preview for process teardown, never free
                // GPU resources still owned by running vendor work.
                if(!vendorOutstanding)
                {
                    raw?.data?.Dispose();if(root!=null)UnityEngine.Object.DestroyImmediate(root);foreach(var tile in tiles)if(tile.data!=null)UnityEngine.Object.DestroyImmediate(tile.data);
                    if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
                }
                else run.status="failed_vendor_still_running_unsaved_preview_retained";
            }
        }
        static Region[] ReadRegions(List<Tile> saved,List<Tile> generated,float south)
        {
            var definition=AssetDatabase.LoadAssetAtPath<ScriptableObject>(DefinitionPath);if(definition==null)throw new InvalidOperationException("Saved definition required for genuine region sample positions.");
            var values=(Array)definition.GetType().GetField("regions").GetValue(definition);
            return values.Cast<object>().Select(value=>
            {
                var type=value.GetType();var center=(Vector3)type.GetField("center").GetValue(value);return new Region{id=(string)type.GetField("id").GetValue(value),authoredCenter=center,insideAnchorRectangle=center.x>=-1600&&center.x<=1600&&center.z>=south&&center.z<=2400,
                    distanceToAnchorBoundary=Mathf.Min(center.x+1600,1600-center.x,center.z-south,2400-center.z),savedWorldHeight=Sample(saved,center),ungradedWorldHeight=Sample(generated,center)};
            }).ToArray();
        }
        static float Sample(List<Tile> tiles,Vector3 position)
        {
            var tile=tiles.FirstOrDefault(value=>position.x>=value.position.x&&position.x<=value.position.x+TileWidth&&position.z>=value.position.z&&position.z<=value.position.z+TileWidth);
            if(tile==null)throw new InvalidOperationException("Declared region lies outside actual four-tile bounds: "+position);
            return tile.position.y+tile.data.GetInterpolatedHeight((position.x-tile.position.x)/TileWidth,(position.z-tile.position.z)/TileWidth);
        }
        static Dataset ReadTerrain(Tile tile,string stage,string output)
        {
            int n=tile.data.heightmapResolution;float[,] map=tile.data.GetHeights(0,0,n,n);var samples=new float[n*n];for(int z=0;z<n;z++)for(int x=0;x<n;x++)samples[z*n+x]=map[z,x];
            return Summarize(samples,n,n,stage+"_"+tile.id,stage,AssetDatabase.GetAssetPath(tile.data),output);
        }
        static Dataset ReadTexture(RenderTexture source,string id,string stage,string path,string output)
        {
            var prior=RenderTexture.active;var texture=new Texture2D(source.width,source.height,TextureFormat.RFloat,false,true);
            try{RenderTexture.active=source;texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false);texture.Apply(false,false);return Summarize(texture.GetPixels().Select(value=>value.r).ToArray(),source.width,source.height,id,stage,path,output);}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);}
        }
        static Dataset Summarize(float[] samples,int width,int height,string id,string stage,string source,string output)
        {
            var data=new Dataset{id=id,stage=stage,source=source,width=width,height=height,samples=samples.Length,minimum=float.MaxValue,maximum=float.MinValue,units=id.EndsWith("_mask",StringComparison.Ordinal)?"unit_biome_weight":"normalized_height"};data.histogram=new int[data.histogramUpperBounds.Length];
            data.coverage=Enumerable.Range(0,256).Select(index=>new Cell{x=index%16,z=index/16,minimum=float.MaxValue,maximum=float.MinValue}).ToArray();double sum=0,squares=0;var pixels=new Color32[samples.Length];
            for(int z=0;z<height;z++)for(int x=0;x<width;x++)
            {
                int index=z*width+x;float value=samples[index];if(float.IsNaN(value)||float.IsInfinity(value)){data.nonFinite++;pixels[index]=new Color32(255,0,255,255);continue;}
                if(value==0)data.zero++;else if(value>0)data.positive++;else data.negative++;if(value>1)data.aboveOne++;
                data.minimum=Mathf.Min(data.minimum,value);data.maximum=Mathf.Max(data.maximum,value);sum+=value;squares+=(double)value*value;
                for(int bucket=0;bucket<data.histogram.Length;bucket++)if(value<=data.histogramUpperBounds[bucket]){data.histogram[bucket]++;break;}
                var cell=data.coverage[Mathf.Min(15,z*16/height)*16+Mathf.Min(15,x*16/width)];cell.samples++;if(value>0)cell.positive++;cell.minimum=Mathf.Min(cell.minimum,value);cell.maximum=Mathf.Max(cell.maximum,value);cell.mean+=value;
                byte color=(byte)Mathf.RoundToInt(Mathf.Clamp01(value)*255);pixels[index]=new Color32(color,color,color,255);
            }
            int finite=samples.Length-data.nonFinite;if(finite==0)throw new InvalidOperationException("No finite actual height samples: "+id);
            data.mean=(float)(sum/finite);data.standardDeviation=(float)Math.Sqrt(Math.Max(0,squares/finite-(sum/finite)*(sum/finite)));if(data.units=="normalized_height"){data.worldMinimum=WorldBase+HeightScale*data.minimum;data.worldMaximum=WorldBase+HeightScale*data.maximum;}data.nonzeroFraction=(float)(data.positive+data.negative)/finite;
            foreach(var cell in data.coverage)if(cell.samples>0)cell.mean/=cell.samples;
            var bytes=new byte[samples.Length*sizeof(float)];Buffer.BlockCopy(samples,0,bytes,0,bytes.Length);data.sha256=Hash(bytes);File.WriteAllBytes(Path.Combine(output,id+".float32"),bytes);
            var image=new Texture2D(width,height,TextureFormat.RGBA32,false,true);try{image.SetPixels32(pixels);image.Apply(false,false);data.png=id+".png";File.WriteAllBytes(Path.Combine(output,data.png),image.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(image);}
            // PNG is a linear normalized data visualization, not a beauty image.
            File.WriteAllText(Path.Combine(output,id+".json"),JsonUtility.ToJson(data,true));return data;
        }
        static Vector3 Position(int x,int z)=>new Vector3(-1600+x*TileWidth,WorldBase,-800+z*TileWidth);
        static void InitializeCallback(VistaManager manager,string name)
        {var field=typeof(VistaManager).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(field==null||field.FieldType!=typeof(UnityEvent))throw new InvalidOperationException("Installed Vista serialized callback schema changed: "+name);if(field.GetValue(manager)==null)field.SetValue(manager,new UnityEvent());}
        static void Watch(string path,Dictionary<string,string> sources)
        {if(string.IsNullOrEmpty(path)||!File.Exists(path))return;sources[path]=Hash(File.ReadAllBytes(path));if(File.Exists(path+".meta"))sources[path+".meta"]=Hash(File.ReadAllBytes(path+".meta"));}
        static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static void WriteReport(string output,Report report)=>File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));
    }
}
#endif
