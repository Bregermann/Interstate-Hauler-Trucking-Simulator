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
    // Before Digger excavation, measure an actual Vista massif above the
    // unchanged105m road. No custom CPU heights, voxel engine, or cave mesh.
    public static class DragonKingdomCavernFoundationReview
    {
        const string Source="Assets/TruckDragonKingdom/ReviewCandidates/VistaRegionalFoundation01";
        const string Prefix="Assets/TruckDragonKingdom/ReviewCandidates/";
        const string PrivatePrefix="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVista/";
        const float ElevationShift=100;
        [Serializable]public sealed class Tile
        {public string source,path;public Vector3 position;public int width,outsideBiomeSamples,coordinatedSamples;public float outsideBiomeMaximumDelta,outsideBiomeCoordinatedMaximumDelta,minimumY,maximumY;}
        [Serializable]public sealed class Roof {public Vector3 position;public float oldSurface,newSurface,clearanceAboveRoad;}
        [Serializable]public sealed class Receipt
        {
            public string scope="MEASURED_VENDOR_CAVERN_MASSIF_BEFORE_DIGGER_NOT_A_COMPLETED_CAVE",folder,prefab,graph,status="IN_PROGRESS",error;
            public float actualElevationShift=ElevationShift,semanticRoadFloor=105,centerBefore,centerAfter,minimumRoofHeight;
            public int populationCallbacks;public bool sourceBytesPreserved,outsideBiomePreserved,temporaryObjectsRemoved;
            public string coordination="V128 actual regeneration changed unrelated samples2.4–8.8m; cause unproven. Preserve saved original vendor output outside the actual vendor cavern mask and blend only its outermost mask band. Raw and coordinated deltas remain separately reported.";
            public Bounds affectedBiomeBounds;public Tile[] terrains;public Roof[] roof;
        }
        public static Receipt LastReceipt{get;private set;}
        public static IEnumerator CreateReviewCandidate()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonCavernCandidate");
            return CreateCandidate(at>=0&&at+1<args.Length?args[at+1]:"CavernMassif01");
        }
        public static IEnumerator CreateCandidate(string tag)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Fresh short cavern review tag required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||VistaManager.HasActiveTask()||Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("Stopped isolated compiled Editor without another terrain/vendor task required.");
            string folder=Prefix+tag,privateFolder=PrivatePrefix+tag;
            if(Directory.Exists(folder)||Directory.Exists(privateFolder))throw new InvalidOperationException("Preserve prior review candidates; select a fresh tag.");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"/Vista Regional Foundation.prefab");if(source==null)throw new InvalidOperationException("Actual measured V102 foundation setup required.");
            var files=AssetDatabase.GetDependencies(Source+"/Vista Regional Foundation.prefab",true)
                .Concat(new[]{Source+"/Foundation Receipt.json","Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab","Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity","Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset"})
                .SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(Io(p))));
            Directory.CreateDirectory(folder+"/Terrain");Directory.CreateDirectory(privateFolder);AssetDatabase.Refresh();
            var receipt=new Receipt{folder=folder,prefab=folder+"/Vista Cavern Massif.prefab",graph=privateFolder+"/Cavern elevated MountainRange.asset"};LastReceipt=receipt;
            Scene scene=EditorSceneManager.NewPreviewScene();GameObject root=null;VistaManager manager=null;bool outstanding=false;Texture2D mask=null;
            var originals=new Dictionary<Terrain,TerrainData>();var clones=new List<TerrainData>();var rows=new List<Tile>();Exception observationError=null;
            VistaManager.TexturePopulatedHandler observe=(sender,tile,texture)=>{if(sender!=manager)return;try{if(!originals.Keys.Any(t=>ReferenceEquals(t.GetComponent<TerrainTile>(),tile)))throw new InvalidOperationException("Vista touched an unowned terrain tile.");receipt.populationCallbacks++;}catch(Exception e){observationError=e;}};
            VistaManager.heightMapPopulated+=observe;
            try
            {
                root=(GameObject)PrefabUtility.InstantiatePrefab(source);SceneManager.MoveGameObjectToScene(root,scene);root.name="Actual Vista cavern massif REVIEW";
                manager=root.GetComponentInChildren<VistaManager>(true);if(manager==null)throw new InvalidOperationException("Measured setup lost its actual vendor manager.");
                foreach(string name in new[]{"m_beforeGeneratingUnityCallback","m_afterGeneratingUnityCallback"})
                {var field=typeof(VistaManager).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);if(field==null)throw new InvalidOperationException("Installed Vista callback schema changed.");if(field.GetValue(manager)==null)field.SetValue(manager,new UnityEvent());}
                var terrains=root.GetComponentsInChildren<Terrain>(true);if(terrains.Length!=4)throw new InvalidOperationException("Actual four measured Vista tiles required.");
                foreach(var terrain in terrains)
                {
                    var original=terrain.terrainData;if(original.heightmapResolution!=513)throw new InvalidOperationException("Use actual installed Personal513 resolution.");originals.Add(terrain,original);
                    var clone=UnityEngine.Object.Instantiate(original);clones.Add(clone);terrain.terrainData=clone;var collider=terrain.GetComponent<TerrainCollider>();if(collider==null)throw new InvalidOperationException("Real terrain collision required.");collider.terrainData=clone;
                }
                var cavern=root.GetComponentsInChildren<LocalProceduralBiome>(true).Single(b=>AssetDatabase.GetAssetPath(b.terrainGraph).EndsWith("/caverns MountainRange.asset",StringComparison.Ordinal));
                receipt.affectedBiomeBounds=cavern.worldBounds;
                var graph=UnityEngine.Object.Instantiate(cavern.terrainGraph);graph.name="Existing native cavern relief plus measured roof elevation";
                var output=graph.GetNodesOfType<HeightOutputNode>().Single();var edge=graph.GetEdges().Single(e=>e.inputSlot.nodeId==output.id&&e.inputSlot.slotId==output.inputSlot.id);
                var terminal=graph.GetNodesOfType<MathNode>().Single(n=>n.id==edge.outputSlot.nodeId);
                var addition=terminal.maths.Single(m=>m.enabled&&m.ops==MathNode.Operator.Add);addition.number+=ElevationShift/600;
                graph.OnBeforeSerialize();AssetDatabase.CreateAsset(graph,receipt.graph);cavern.terrainGraph=graph;
                receipt.centerBefore=Height(originals,new Vector3(520,0,780),false);
                var task=manager.GenerateAll();outstanding=true;double started=EditorApplication.timeSinceStartup;
                while(!task.isCompleted){if(EditorApplication.timeSinceStartup-started>900)throw new TimeoutException("Actual vendor cavern massif generation exceeded bounded wait.");yield return null;}
                outstanding=false;if(observationError!=null)throw observationError;if(receipt.populationCallbacks!=4)throw new InvalidOperationException("Four real vendor height callbacks required.");
                // Use Vista's actual final polygon/mask implementation. Do not
                // invent a second falloff/noise field or relax outside gates.
                mask=ReadActualVendorMask(cavern);
                foreach(var pair in originals)
                {
                    var terrain=pair.Key;var data=terrain.terrainData;int n=data.heightmapResolution;var before=pair.Value.GetHeights(0,0,n,n);var after=data.GetHeights(0,0,n,n);
                    int x=Mathf.RoundToInt((terrain.transform.position.x+1600)/1600),z=Mathf.RoundToInt((terrain.transform.position.z+800)/1600);
                    var row=new Tile{source=AssetDatabase.GetAssetPath(pair.Value),path=folder+"/Terrain/Kingdom "+x+" "+z+".asset",position=terrain.transform.position,width=n,minimumY=float.PositiveInfinity,maximumY=float.NegativeInfinity};
                    for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++)
                    {
                        var p=terrain.transform.position+new Vector3(ix*1600f/(n-1),0,iz*1600f/(n-1));
                        bool outside=p.x<receipt.affectedBiomeBounds.min.x||p.x>receipt.affectedBiomeBounds.max.x||p.z<receipt.affectedBiomeBounds.min.z||p.z>receipt.affectedBiomeBounds.max.z;
                        float w=outside?0:mask.GetPixelBilinear((p.x-receipt.affectedBiomeBounds.min.x)/receipt.affectedBiomeBounds.size.x,(p.z-receipt.affectedBiomeBounds.min.z)/receipt.affectedBiomeBounds.size.z).r;
                        if(float.IsNaN(w)||float.IsInfinity(w))throw new InvalidOperationException("Nonfinite actual vendor cavern mask.");
                        float rawDelta=Mathf.Abs(after[iz,ix]-before[iz,ix])*600;
                        if(outside){row.outsideBiomeSamples++;row.outsideBiomeMaximumDelta=Mathf.Max(row.outsideBiomeMaximumDelta,rawDelta);}
                        float blend=Mathf.Clamp01(w*64);float coordinated=Mathf.Lerp(before[iz,ix],after[iz,ix],blend);
                        if(coordinated!=after[iz,ix])row.coordinatedSamples++;after[iz,ix]=coordinated;
                        float value=after[iz,ix]*600-20;
                        if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidOperationException("Nonfinite vendor cavern massif sample.");
                        row.minimumY=Mathf.Min(row.minimumY,value);row.maximumY=Mathf.Max(row.maximumY,value);
                    }
                    data.SetHeights(0,0,after);var persisted=data.GetHeights(0,0,n,n);
                    for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++)
                    {
                        var p=terrain.transform.position+new Vector3(ix*1600f/(n-1),0,iz*1600f/(n-1));
                        if(p.x<receipt.affectedBiomeBounds.min.x||p.x>receipt.affectedBiomeBounds.max.x||p.z<receipt.affectedBiomeBounds.min.z||p.z>receipt.affectedBiomeBounds.max.z)
                            row.outsideBiomeCoordinatedMaximumDelta=Mathf.Max(row.outsideBiomeCoordinatedMaximumDelta,Mathf.Abs(persisted[iz,ix]-before[iz,ix])*600);
                    }
                    AssetDatabase.CreateAsset(data,row.path);rows.Add(row);
                }
                receipt.terrains=rows.ToArray();receipt.outsideBiomePreserved=rows.All(r=>r.outsideBiomeCoordinatedMaximumDelta==0);
                receipt.centerAfter=Height(originals,new Vector3(520,0,780),true);
                var roof=new List<Roof>();for(int z=-60;z<=60;z+=15)for(int x=-90;x<=90;x+=15)
                {var p=new Vector3(520+x,0,780+z);float y=Height(originals,p,true);roof.Add(new Roof{position=p,oldSurface=Height(originals,p,false),newSurface=y,clearanceAboveRoad=y-105});}
                receipt.roof=roof.ToArray();receipt.minimumRoofHeight=roof.Min(r=>r.newSurface);
                PrefabUtility.SaveAsPrefabAsset(root,receipt.prefab);AssetDatabase.SaveAssets();
                receipt.status=receipt.outsideBiomePreserved&&receipt.minimumRoofHeight>=160&&Mathf.Abs(receipt.centerAfter-receipt.centerBefore-ElevationShift)<2
                    ?"MEASURED_MASSIF_READY_FOR_SEPARATE_DIGGER_EXCAVATION_REVIEW":"REJECTED_MASSIF_REQUIRES_MEASURED_GEOMETRY_REPAIR";
            }
            finally
            {
                if(receipt.status=="IN_PROGRESS")receipt.status="FAILED_OR_INCOMPLETE_MASSIF_NOT_ACCEPTED";
                VistaManager.heightMapPopulated-=observe;
                if(mask!=null)UnityEngine.Object.DestroyImmediate(mask);
                if(!outstanding){if(root!=null)UnityEngine.Object.DestroyImmediate(root);foreach(var clone in clones)if(clone!=null&&!AssetDatabase.Contains(clone))UnityEngine.Object.DestroyImmediate(clone);if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);receipt.temporaryObjectsRemoved=true;}
                receipt.sourceBytesPreserved=files.All(p=>File.Exists(Io(p.Key))&&Hash(File.ReadAllBytes(Io(p.Key)))==p.Value);
                string output=Output();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"cavern-foundation-"+tag+".json"),JsonUtility.ToJson(receipt,true));File.WriteAllText(folder+"/Cavern Foundation Receipt.json",JsonUtility.ToJson(receipt,true));
                if(!receipt.sourceBytesPreserved)throw new InvalidOperationException("Cavern review modified an original source asset.");
            }
            if(receipt.status!="MEASURED_MASSIF_READY_FOR_SEPARATE_DIGGER_EXCAVATION_REVIEW")throw new InvalidOperationException("Actual roof/outside-biome gates rejected cavern foundation: "+JsonUtility.ToJson(receipt));
        }
        static Texture2D ReadActualVendorMask(LocalProceduralBiome biome)
        {
            var method=typeof(LocalProceduralBiome).GetMethod("RenderPostProcessedBiomeMask",BindingFlags.NonPublic|BindingFlags.Instance);
            if(method==null)throw new InvalidOperationException("Installed actual vendor mask-query schema changed.");
            var texture=(RenderTexture)method.Invoke(biome,null);if(texture==null)throw new InvalidOperationException("Actual vendor cavern mask missing.");
            var previous=RenderTexture.active;
            try{RenderTexture.active=texture;var cpu=new Texture2D(texture.width,texture.height,TextureFormat.RFloat,false,true){wrapMode=TextureWrapMode.Clamp};cpu.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);cpu.Apply(false,false);return cpu;}
            finally{RenderTexture.active=previous;texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
        }
        static float Height(Dictionary<Terrain,TerrainData> terrains,Vector3 p,bool current)
        {
            foreach(var pair in terrains){var origin=pair.Key.transform.position;if(p.x>=origin.x&&p.x<=origin.x+1600&&p.z>=origin.z&&p.z<=origin.z+1600)return origin.y+(current?pair.Key.terrainData:pair.Value).GetInterpolatedHeight((p.x-origin.x)/1600,(p.z-origin.z)/1600);}
            throw new InvalidOperationException("Actual cavern measurement outside four owned tiles.");
        }
        static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data));}
        static string Output(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):"Logs/TruckSports/DragonKingdom/CavernFoundation";}
    }
}
#endif
