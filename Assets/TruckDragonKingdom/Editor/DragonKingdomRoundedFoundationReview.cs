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
    // Actual vendor configuration review: rounded broad mountain masks first,
    // bounded regional clearings afterwards. No custom noise/height generator.
    public static class DragonKingdomRoundedFoundationReview
    {
        const string Source="Assets/TruckDragonKingdom/ReviewCandidates/VistaRegionalFoundation01";
        const string Prefix="Assets/TruckDragonKingdom/ReviewCandidates/";
        const string Private="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVista/";
        [Serializable]public sealed class Biome
        {public string id,graph,blend;public int order;public Vector3 center;public float coreRadius,falloff,desiredSurface,calibrationBefore,calibrationOffset;}
        [Serializable]public sealed class Tile
        {public string path;public Vector3 position;public int resolution,above200,above400,steeper45,steeper65,sourceSteeper65,ceilingSamples;public float minimum,maximum,maximumSlope;}
        [Serializable]public sealed class Center{public string id;public Vector3 point;public float requestedSurface,actualSurface;}
        [Serializable]public sealed class Receipt
        {
            public string scope="VENDOR_ROUNDED_FOUNDATION_BEFORE_ROADS_SCENERY_DIGGER_OR_FINAL_VISUAL_APPROVAL",folder,prefab,status="IN_PROGRESS",error;
            public bool sourceBytesPreserved,temporaryObjectsRemoved,requiresDigger=true;
            public int heightCallbacks;public Biome[] biomes;public Tile[] terrains;public Center[] centers;public string[] validationIssues;
        }
        public static Receipt LastReceipt{get;private set;}
        sealed class Plan{public LocalProceduralBiome biome;public TerrainGraph graph;public Biome record;}
        static readonly Dictionary<string,Vector3> Floors=new Dictionary<string,Vector3>{
            {"village",new Vector3(-560,0,-360)},{"castle",new Vector3(0,60,360)},{"harbor",new Vector3(600,0,-480)},
            {"forest",new Vector3(-760,24,280)},{"mountains",new Vector3(0,300,1040)},{"refuge",new Vector3(-360,330,1200)},
            {"caverns",new Vector3(520,105,780)},{"volcano",new Vector3(1040,240,840)},{"nests",new Vector3(-840,270,920)}};
        public static IEnumerator CreateReviewCandidate()=>CreateCandidate(Arg("-tsDragonRoundedCandidate","RoundedVista01"));
        public static IEnumerator CreateCandidate(string tag)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Fresh short rounded terrain tag required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||VistaManager.HasActiveTask()||Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Stopped isolated Editor without active terrain/vendor tasks required.");
            string folder=Prefix+tag,privateFolder=Private+tag;if(Directory.Exists(folder)||Directory.Exists(privateFolder))throw new InvalidOperationException("Preserve all existing candidates; choose a fresh tag.");
            string sourcePrefab=Source+"/Vista Regional Foundation.prefab";var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefab);if(source==null)throw new InvalidOperationException("Measured actual V102 foundation required.");
            var watched=AssetDatabase.GetDependencies(sourcePrefab,true).Concat(new[]{Source+"/Foundation Receipt.json","Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab","Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity"}).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(Io(p))));
            Directory.CreateDirectory(folder+"/Terrain");Directory.CreateDirectory(privateFolder);AssetDatabase.Refresh();
            var receipt=new Receipt{folder=folder,prefab=folder+"/Vista Regional Foundation.prefab"};LastReceipt=receipt;
            var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;VistaManager manager=null;bool outstanding=false;BiomeDataRequest request=null;
            var originals=new Dictionary<Terrain,TerrainData>();var cloned=new List<TerrainData>();var plans=new List<Plan>();Exception observationError=null;
            VistaManager.TexturePopulatedHandler observe=(sender,tile,texture)=>{if(sender!=manager)return;try{if(!originals.Keys.Any(t=>ReferenceEquals(t.GetComponent<TerrainTile>(),tile)))throw new InvalidOperationException("Vista touched unowned tile.");receipt.heightCallbacks++;}catch(Exception e){observationError=e;}};
            VistaManager.heightMapPopulated+=observe;
            try
            {
                root=(GameObject)PrefabUtility.InstantiatePrefab(source);SceneManager.MoveGameObjectToScene(root,scene);root.name="Rounded native Vista foundation REVIEW";
                manager=root.GetComponentInChildren<VistaManager>(true);if(manager==null)throw new InvalidOperationException("Actual configured vendor manager required.");
                foreach(string name in new[]{"m_beforeGeneratingUnityCallback","m_afterGeneratingUnityCallback"}){var f=typeof(VistaManager).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);if(f==null)throw new InvalidOperationException("Vista callback schema changed.");if(f.GetValue(manager)==null)f.SetValue(manager,new UnityEvent());}
                foreach(var terrain in root.GetComponentsInChildren<Terrain>(true))
                {
                    if(terrain.terrainData.heightmapResolution!=513)throw new InvalidOperationException("Actual Personal513 heightmaps required.");originals.Add(terrain,terrain.terrainData);
                    var data=UnityEngine.Object.Instantiate(terrain.terrainData);cloned.Add(data);terrain.terrainData=data;terrain.GetComponent<TerrainCollider>().terrainData=data;
                }
                if(originals.Count!=4)throw new InvalidOperationException("Actual four-tile configuration required.");
                var smoothMask=SmoothMask();AssetDatabase.CreateAsset(smoothMask,privateFolder+"/Actual vendor smooth mask.asset");
                int crestIndex=0;
                foreach(var biome in root.GetComponentsInChildren<LocalProceduralBiome>(true))
                {
                    string path=AssetDatabase.GetAssetPath(biome.terrainGraph),name=Path.GetFileNameWithoutExtension(path);
                    if(name.StartsWith("Full coverage",StringComparison.Ordinal))
                    {
                        var graph=UnityEngine.Object.Instantiate(biome.terrainGraph);Terminal(graph).number+=20f/600;graph.OnBeforeSerialize();AssetDatabase.CreateAsset(graph,privateFolder+"/Full coverage rounded lowland.asset");biome.terrainGraph=graph;continue;
                    }
                    string id=name.EndsWith(" MountainRange",StringComparison.Ordinal)?name.Substring(0,name.Length-" MountainRange".Length):null;
                    if(id==null){biome.order+=1000;continue;} // Real lake/canyon/lava remain the final beds.
                    bool region=Floors.TryGetValue(id,out var floor);var oldCenter=biome.anchors.Aggregate(Vector3.zero,(a,b)=>a+b)/biome.anchors.Length;
                    Vector3 center=region?floor:oldCenter;float desired=region?(id=="caverns"?205:floor.y):id=="wyrmspine-northern-crest"?440:id=="stoneheart-back-crest"?490:id=="ashen-crest"?460:430;
                    if(id=="castle")center.z+=140; // Actual sanctuary is north of its road hub.
                    float radius=region?(id=="castle"?230:id=="village"?170:id=="harbor"?185:id=="forest"?145:id=="caverns"?115:85):22;
                    float falloff=region?(id=="caverns"?300:260):1000;
                    biome.anchors=Circle(center,radius);biome.falloffDirection=FalloffDirection.Outer;biome.falloffDistance=falloff;biome.biomeMaskGraph=smoothMask;
                    var blend=biome.blendOptions;blend.heightMapBlendMode=region?BiomeBlendOptions.HeightBlendMode.Replace:BiomeBlendOptions.HeightBlendMode.KeepHigher;biome.blendOptions=blend;
                    biome.order=region?100+Array.IndexOf(new[]{"refuge","mountains","nests","volcano","caverns","castle","forest","village","harbor"},id):10+crestIndex++;
                    var owned=UnityEngine.Object.Instantiate(biome.terrainGraph);owned.name=id+" rounded calibrated native relief";string target=privateFolder+"/"+id+" MountainRange.asset";AssetDatabase.CreateAsset(owned,target);biome.terrainGraph=owned;
                    plans.Add(new Plan{biome=biome,graph=owned,record=new Biome{id=id,graph=target,blend=blend.heightMapBlendMode.ToString(),order=biome.order,center=center,coreRadius=radius,falloff=falloff,desiredSurface=desired}});
                }
                // Re-measure each actual graph in its changed vendor bounds;
                // guessing its normalized noise contribution would move floors.
                foreach(var plan in plans)
                {
                    Bounds bounds=plan.biome.worldBounds;bounds.size=new Vector3(bounds.size.x,600,bounds.size.z);
                    request=plan.biome.RequestData(bounds,513,512,BiomeDataMask.HeightMap);outstanding=true;double started=EditorApplication.timeSinceStartup;
                    while(!request.isCompleted){if(EditorApplication.timeSinceStartup-started>180)throw new TimeoutException("Actual rounded biome calibration timed out: "+plan.record.id);yield return null;}
                    outstanding=false;if(request.data?.heightMap==null)throw new InvalidOperationException("Real source height output missing.");
                    Vector3 point=Floors.TryGetValue(plan.record.id,out var floor)?floor:plan.record.center;
                    float measured=Read(request.data.heightMap,bounds,point)*600-20;request.data.Dispose();request=null;plan.biome.CleanUp();
                    plan.record.calibrationBefore=measured;plan.record.calibrationOffset=(plan.record.desiredSurface-measured)/600;Terminal(plan.graph).number+=plan.record.calibrationOffset;plan.graph.OnBeforeSerialize();EditorUtility.SetDirty(plan.graph);AssetDatabase.SaveAssetIfDirty(plan.graph);
                }
                receipt.biomes=plans.Select(p=>p.record).ToArray();
                var generation=manager.GenerateAll();outstanding=true;double generationStarted=EditorApplication.timeSinceStartup;
                while(!generation.isCompleted){if(EditorApplication.timeSinceStartup-generationStarted>900)throw new TimeoutException("Actual rounded vendor generation timed out.");yield return null;}
                outstanding=false;if(observationError!=null)throw observationError;if(receipt.heightCallbacks!=4)throw new InvalidOperationException("Four actual height population callbacks required.");
                var records=new List<Tile>();
                foreach(var pair in originals)
                {
                    var terrain=pair.Key;var data=terrain.terrainData;int n=data.heightmapResolution;var a=data.GetHeights(0,0,n,n);var old=pair.Value.GetHeights(0,0,n,n);
                    int x=Mathf.RoundToInt((terrain.transform.position.x+1600)/1600),z=Mathf.RoundToInt((terrain.transform.position.z+800)/1600);var row=new Tile{path=folder+"/Terrain/Kingdom "+x+" "+z+".asset",position=terrain.transform.position,resolution=n,minimum=float.PositiveInfinity,maximum=float.NegativeInfinity};
                    for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++)
                    {
                        float y=a[iz,ix]*600-20;if(float.IsNaN(y)||float.IsInfinity(y))throw new InvalidOperationException("Nonfinite actual vendor output.");row.minimum=Mathf.Min(row.minimum,y);row.maximum=Mathf.Max(row.maximum,y);if(y>=200)row.above200++;if(y>=400)row.above400++;if(a[iz,ix]>=.99997f)row.ceilingSamples++;
                        if(ix==0||iz==0)continue;float step=Mathf.Max(Mathf.Abs(a[iz,ix]-a[iz,ix-1]),Mathf.Abs(a[iz,ix]-a[iz-1,ix]))*600/3.125f;float prior=Mathf.Max(Mathf.Abs(old[iz,ix]-old[iz,ix-1]),Mathf.Abs(old[iz,ix]-old[iz-1,ix]))*600/3.125f;
                        float degrees=Mathf.Atan(step)*Mathf.Rad2Deg;row.maximumSlope=Mathf.Max(row.maximumSlope,degrees);if(degrees>45)row.steeper45++;if(degrees>65)row.steeper65++;if(Mathf.Atan(prior)*Mathf.Rad2Deg>65)row.sourceSteeper65++;
                    }
                    AssetDatabase.CreateAsset(data,row.path);records.Add(row);
                }
                receipt.terrains=records.ToArray();receipt.centers=Floors.Select(f=>new Center{id=f.Key,point=f.Value,requestedSurface=f.Key=="caverns"?205:f.Value.y,actualSurface=Sample(originals.Keys,f.Value)}).ToArray();
                var issues=new List<string>();if(records.Any(r=>r.ceilingSamples>0))issues.Add("Actual height ceiling clipped.");if(records.Max(r=>r.maximum)<450)issues.Add("No measured450m peak.");if(records.Sum(r=>r.above200)<90000)issues.Add("Broad actual mountain mass lost.");if(records.Sum(r=>r.steeper65)>=records.Sum(r=>r.sourceSteeper65)*.7f)issues.Add("Rounded vendor configuration did not reduce severe terrain slopes by30percent.");
                foreach(var center in receipt.centers)if(Mathf.Abs(center.actualSurface-center.requestedSurface)>12)issues.Add(center.id+" semantic surface changed beyond measured tolerance: "+center.actualSurface);
                receipt.validationIssues=issues.ToArray();PrefabUtility.SaveAsPrefabAsset(root,receipt.prefab);
                receipt.status=issues.Count==0?"MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW":"REJECTED_ROUNDED_FOUNDATION";
            }
            finally
            {
                VistaManager.heightMapPopulated-=observe;if(receipt.status=="IN_PROGRESS")receipt.status="FAILED_OR_INCOMPLETE_ROUNDED_FOUNDATION";
                if(!outstanding){request?.data?.Dispose();if(root!=null)UnityEngine.Object.DestroyImmediate(root);foreach(var data in cloned)if(data!=null&&!AssetDatabase.Contains(data))UnityEngine.Object.DestroyImmediate(data);if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);receipt.temporaryObjectsRemoved=true;}
                receipt.sourceBytesPreserved=watched.All(p=>File.Exists(Io(p.Key))&&Hash(File.ReadAllBytes(Io(p.Key)))==p.Value);
                string output=Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"rounded-foundation-"+tag+".json"),JsonUtility.ToJson(receipt,true));File.WriteAllText(folder+"/Foundation Receipt.json",JsonUtility.ToJson(receipt,true));
                if(!receipt.sourceBytesPreserved)throw new InvalidOperationException("Rounded review altered original vendor/world asset bytes.");
            }
            if(receipt.status!="MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW")throw new InvalidOperationException("Actual rounded foundation rejected: "+string.Join(" | ",receipt.validationIssues??Array.Empty<string>()));
        }
        static MathNode.MathConfig Terminal(TerrainGraph graph){var output=graph.GetNodesOfType<HeightOutputNode>().Single();var edge=graph.GetEdges().Single(e=>e.inputSlot.nodeId==output.id&&e.inputSlot.slotId==output.inputSlot.id);return graph.GetNodesOfType<MathNode>().Single(n=>n.id==edge.outputSlot.nodeId).maths.Single(m=>m.enabled&&m.ops==MathNode.Operator.Add);}
        static BiomeMaskGraph SmoothMask(){var graph=ScriptableObject.CreateInstance<BiomeMaskGraph>();graph.Reset();var input=new InputNode{inputName=GraphConstants.BIOME_MASK_INPUT_NAME};var curve=new CurveNode{curve=AnimationCurve.EaseInOut(0,0,1,1)};var output=new OutputNode{outputName=GraphConstants.BIOME_MASK_OUTPUT_NAME};graph.AddNode(input);graph.AddNode(curve);graph.AddNode(output);graph.AddEdge(new Edge{outputSlot=new SlotRef(input.id,input.outputSlot.id),inputSlot=new SlotRef(curve.id,curve.inputSlot.id)});graph.AddEdge(new Edge{outputSlot=new SlotRef(curve.id,curve.outputSlot.id),inputSlot=new SlotRef(output.id,output.inputSlot.id)});graph.OnBeforeSerialize();return graph;}
        static Vector3[] Circle(Vector3 center,float radius)=>Enumerable.Range(0,24).Select(i=>new Vector3(center.x+Mathf.Cos(i*Mathf.PI/12)*radius,0,center.z+Mathf.Sin(i*Mathf.PI/12)*radius)).ToArray();
        static float Sample(IEnumerable<Terrain> terrains,Vector3 p){foreach(var t in terrains){var d=p-t.transform.position;if(d.x>=0&&d.z>=0&&d.x<=1600&&d.z<=1600)return t.SampleHeight(p)+t.transform.position.y;}throw new InvalidOperationException("Sample outside owned review tiles.");}
        static float Read(RenderTexture texture,Bounds bounds,Vector3 p){var old=RenderTexture.active;var cpu=new Texture2D(texture.width,texture.height,TextureFormat.RFloat,false,true);try{RenderTexture.active=texture;cpu.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);cpu.Apply();return cpu.GetPixelBilinear((p.x-bounds.min.x)/bounds.size.x,(p.z-bounds.min.z)/bounds.size.z).r;}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(cpu);}}
        static string Arg(string flag,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,flag);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes));}
    }
}
#endif
