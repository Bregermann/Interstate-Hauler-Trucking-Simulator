using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if VISTA
using Pinwheel.Vista.Graph;
#endif

namespace LWS.TruckMinefield.Editor
{
    // Explicit saved-landscape tooling only; never a build/Play Mode hook.
    public static class MinefieldSideRangeAuthoring
    {
        const string GraphPath="Assets/PinwheelStudio/Vista/Personal/Samples/Scenes/Demo_MultiLayerBiomeBlending Biomes/Mountain_TerrainGraph.asset";
        const string LogFolder="Logs/TruckSports/PlaytestRepair";
        const string WestData="Assets/TruckMinefield/Materials/West meadow and Vista mountains.asset";
        const string EastData="Assets/TruckMinefield/Materials/East meadow and Vista mountains.asset";
        const string Pine="Assets/TruckMinefield/Prefabs/Outer mountain pine.prefab";
        const string SeamlessRunout="Assets/TruckMinefield/Materials/Seamless mountain landing runout.asset";

        [MenuItem("Truck Sports/Minefield/Author Vista outer mountain ranges")]
        public static void Upgrade()
        {
#if !VISTA
            throw new InvalidOperationException("Enable the installed Vista SDK to author these ranges. Saved terrains do not need runtime graph evaluation.");
#else
            EditorSceneManager.OpenScene(MinefieldAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<MinefieldMatch>();
            var west=match.grounds.Single(t=>t.name=="West meadow");var east=match.grounds.Single(t=>t.name=="East meadow");
            if(AssetDatabase.GetAssetPath(west.terrainData)==WestData&&AssetDatabase.GetAssetPath(east.terrainData)==EastData){RefinePresentation();Debug.Log("MINEFIELD_SIDE_RANGES_PRESERVED authored customizations");return;}
            if(AssetDatabase.GetAssetPath(west.terrainData)!="Assets/TruckMinefield/Materials/West meadow.asset"||AssetDatabase.GetAssetPath(east.terrainData)!="Assets/TruckMinefield/Materials/East meadow.asset")throw new InvalidOperationException("Preserve customized terrain assignments; this explicit migration only upgrades the two stock side meadows.");
            if(File.Exists(WestData)||File.Exists(EastData))throw new InvalidOperationException("Existing unassigned mountain terrain assets require review; do not overwrite designer work.");
            var rock=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/RockFace/RockFace_VistaSample.terrainlayer");
            if(rock==null)throw new InvalidOperationException("Owned Vista rock terrain layer is missing.");
            var pine=CreatePine();
            var dressing=GameObject.Find("Minefield surrounding meadow and landing runout");
            var props=dressing!=null?dressing.transform.Cast<Transform>().Where(t=>t.name=="Installed Mega City tree"||t.name=="Installed Toon Gas Station rock").ToArray():Array.Empty<Transform>();
            var oldHeights=props.Select(t=>match.GroundHeight(t.position)).ToArray();
            TerrainData left=null,right=null;
            try
            {
                left=CreateRange(west,match.seed,rock,pine);right=CreateRange(east,match.seed+173,rock,pine);
                AssetDatabase.CreateAsset(left,WestData);AssetDatabase.CreateAsset(right,EastData);
                west.terrainData=west.GetComponent<TerrainCollider>().terrainData=left;east.terrainData=east.GetComponent<TerrainCollider>().terrainData=right;
                foreach(var terrain in new[]{west,east}){terrain.treeDistance=650;terrain.treeBillboardDistance=150;terrain.treeCrossFadeLength=30;terrain.treeMaximumFullLODCount=80;EditorUtility.SetDirty(terrain);EditorUtility.SetDirty(terrain.GetComponent<TerrainCollider>());}
                for(int i=0;i<props.Length;i++){var p=props[i].position;p.y+=match.GroundHeight(p)-oldHeights[i];props[i].position=p;}
                west.Flush();east.Flush();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
                Debug.Log("MINEFIELD_SIDE_RANGES_AUTHORED installed Vista graph;central course/runout,mines,forces,controls,cameras unchanged;614m width/8.4km length retained;two terrain-native1000-pine forests");
                RefinePresentation();
            }
            finally{if(left!=null&&!AssetDatabase.Contains(left))UnityEngine.Object.DestroyImmediate(left);if(right!=null&&!AssetDatabase.Contains(right))UnityEngine.Object.DestroyImmediate(right);}
#endif
        }
        [MenuItem("Truck Sports/Minefield/Refine owned range surfaces and stitch runout")]
        public static void RefinePresentation()
        {
            EditorSceneManager.OpenScene(MinefieldAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<MinefieldMatch>();var runout=match.grounds.Single(t=>t.name=="Mile landing runout");
            if(AssetDatabase.GetAssetPath(runout.terrainData)==SeamlessRunout){Debug.Log("MINEFIELD_RANGE_PRESENTATION_PRESERVED customized surfaces,forests andrunout");return;}
            var sides=match.grounds.Where(t=>t.name=="West meadow"||t.name=="East meadow").ToArray();
            if(AssetDatabase.GetAssetPath(runout.terrainData)!="Assets/TruckMinefield/Materials/Mile landing runout.asset"||sides.Length!=2||sides.Any(t=>t.terrainData.treeInstanceCount!=1000)||File.Exists(SeamlessRunout))throw new InvalidOperationException("Preserve customized landscape; explicit initial-range refinement requires the known stock runout and two1000-tree generated ranges.");
            var grass=Layer("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer","Range meadow grass",.65f,6);
            var rock=Layer("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/RockFace/RockFace_VistaSample.terrainlayer","Range exposed rock",.8f,8);
            foreach(var side in sides)
            {
                var data=side.terrainData;data.terrainLayers=data.terrainLayers.Select(l=>l.name=="Grass_Light_VistaSample"?grass:l.name=="RockFace_VistaSample"?rock:l).ToArray();
                var trees=data.treeInstances.ToList();var random=new System.Random(match.seed+(side.transform.position.x<0?411:877));var size=data.size;
                for(int attempt=0;attempt<50000&&trees.Count<2500;attempt++)
                {
                    float x=Mathf.Lerp(170,300,(float)random.NextDouble())*Mathf.Sign(side.transform.position.x),u=(x-side.transform.position.x)/size.x,v=(float)random.NextDouble();if(data.GetSteepness(u,v)>42)continue;float scale=Mathf.Lerp(.8f,1.4f,(float)random.NextDouble());
                    trees.Add(new TreeInstance{position=new Vector3(u,data.GetInterpolatedHeight(u,v)/size.y,v),prototypeIndex=0,widthScale=scale,heightScale=scale,rotation=(float)random.NextDouble()*Mathf.PI*2,color=Color.white,lightmapColor=Color.white});
                }
                if(trees.Count!=2500)throw new InvalidOperationException("Insufficient grounded forest positions.");data.SetTreeInstances(trees.ToArray(),true);side.heightmapPixelError=4;EditorUtility.SetDirty(data);EditorUtility.SetDirty(side);side.Flush();
            }
            // The old runout added a full hill even at x=+/-57, while each side
            // meadow kept its baseline at that same edge: a visible solid step.
            // Preserve the centre and blend only its outer17metres to the exact
            // neighbouring height. Central minefield and all forces stay intact.
            var stitched=UnityEngine.Object.Instantiate(runout.terrainData);int resolution=stitched.heightmapResolution;var heights=stitched.GetHeights(0,0,resolution,resolution);var span=stitched.size;var west=sides.Single(t=>t.name=="West meadow");var east=sides.Single(t=>t.name=="East meadow");
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
            {
                float u=x/(float)(resolution-1),worldX=runout.transform.position.x+u*span.x,worldZ=runout.transform.position.z+z/(float)(resolution-1)*span.z;
                float left=west.SampleHeight(new Vector3(-57,0,worldZ))+west.transform.position.y,right=east.SampleHeight(new Vector3(57,0,worldZ))+east.transform.position.y;
                float baseline=Mathf.Lerp(left,right,u)-runout.transform.position.y,keep=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,57,Mathf.Abs(worldX)));heights[z,x]=Mathf.Lerp(baseline,heights[z,x]*span.y,keep)/span.y;
            }
            stitched.SetHeights(0,0,heights);stitched.terrainLayers=stitched.terrainLayers.Select(l=>l.name=="Grass_Light_VistaSample"?grass:l).ToArray();AssetDatabase.CreateAsset(stitched,SeamlessRunout);runout.terrainData=runout.GetComponent<TerrainCollider>().terrainData=stitched;runout.heightmapPixelError=4;runout.Flush();EditorUtility.SetDirty(runout);EditorUtility.SetDirty(runout.GetComponent<TerrainCollider>());
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("MINEFIELD_RANGE_PRESENTATION_REFINED 2500pines per side,owned surface variants,stockrunout outeredge steps stitched;centre±40m and original minefield preserved");
        }
        static TerrainLayer Layer(string source,string name,float normal,float tile)
        {
            string path="Assets/TruckMinefield/Materials/"+name+".terrainlayer";var existing=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);if(existing!=null)return existing;var original=AssetDatabase.LoadAssetAtPath<TerrainLayer>(source);if(original==null)throw new InvalidOperationException("Owned terrain source missing: "+source);
            var layer=UnityEngine.Object.Instantiate(original);layer.name=name;layer.normalScale=normal;layer.tileSize=Vector2.one*tile;layer.metallic=0;layer.smoothness=0;AssetDatabase.CreateAsset(layer,path);return layer;
        }
        public static void Audit()
        {
            EditorSceneManager.OpenScene(MinefieldAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<MinefieldMatch>();var report=new StringBuilder();
            foreach(var terrain in match.grounds)
            {
                var data=terrain.terrainData;var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution).Cast<float>();
                report.AppendLine(terrain.name+" position="+terrain.transform.position+" size="+data.size+" heightRange="+(heights.Min()*data.size.y)+".."+(heights.Max()*data.size.y)+" colliderMatches="+(terrain.GetComponent<TerrainCollider>().terrainData==data)+" trees="+data.treeInstanceCount+" asset="+AssetDatabase.GetAssetPath(data));
            }
            report.AppendLine("Decorative scene trees="+UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(t=>t.name=="Installed Mega City tree"));
            Directory.CreateDirectory(LogFolder);File.WriteAllText(LogFolder+"/minefield-side-range-audit.txt",report.ToString());
#if VISTA
            var west=match.grounds.First(t=>t.name=="West meadow");var texture=VistaMountains(west,match.seed);
            try
            {
                var heights=texture.GetPixels().Select(p=>p.r).ToArray();report.AppendLine("Installed Vista mountain graph generated "+texture.width+"x"+texture.height+" range="+heights.Min()+".."+heights.Max()+" source="+GraphPath);
                File.WriteAllBytes(LogFolder+"/minefield-vista-mountain-audit.exr",texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
#else
            report.AppendLine("Vista graph SDK is not enabled; no terrain changes made.");
#endif
            File.WriteAllText(LogFolder+"/minefield-side-range-audit.txt",report.ToString());Debug.Log(report.ToString());
        }
#if VISTA
        static GameObject CreatePine()
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Pine);if(existing!=null)return existing;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_PineTree.prefab");if(source==null)throw new InvalidOperationException("Owned Vista pine prefab is missing.");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var renderers=instance.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float scale=12/Mathf.Max(.01f,bounds.size.y);instance.transform.localScale*=scale;instance.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
                // Forest dressing uses native terrain instancing. It introduces
                // no new physical blockers into the accepted distance challenge.
                foreach(var shape in instance.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(shape);
                foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                Directory.CreateDirectory(Path.GetDirectoryName(Pine));AssetDatabase.Refresh();return PrefabUtility.SaveAsPrefabAsset(instance,Pine);
            }
            finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        static TerrainData CreateRange(Terrain terrain,int seed,TerrainLayer rock,GameObject pine)
        {
            var data=UnityEngine.Object.Instantiate(terrain.terrainData);var texture=VistaMountains(terrain,seed);
            try
            {
                var pixels=texture.GetPixels();float min=pixels.Min(p=>p.r),max=pixels.Max(p=>p.r);if(max-min<.02f)throw new InvalidOperationException("Vista mountain output lacks usable height variation.");
                int resolution=data.heightmapResolution;var heights=data.GetHeights(0,0,resolution,resolution);float oldScale=data.size.y;var size=data.size;size.y=260;data.size=size;
                for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
                {
                    float u=x/(float)(resolution-1),v=z/(float)(resolution-1),worldX=terrain.transform.position.x+u*size.x;
                    float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(145,307,Mathf.Abs(worldX)));
                    float vendorHeight=Mathf.InverseLerp(min,max,texture.GetPixelBilinear(u,v).r);
                    heights[z,x]=(heights[z,x]*oldScale+blend*(65+vendorHeight*115))/size.y;
                }
                data.SetHeights(0,0,heights);
                var oldLayers=data.terrainLayers;int layers=oldLayers.Length;int alphaSize=data.alphamapResolution;var originalAlpha=data.GetAlphamaps(0,0,alphaSize,alphaSize);data.terrainLayers=oldLayers.Concat(new[]{rock}).ToArray();var alpha=new float[alphaSize,alphaSize,layers+1];
                for(int z=0;z<alphaSize;z++)for(int x=0;x<alphaSize;x++)
                {
                    float u=x/(float)(alphaSize-1),v=z/(float)(alphaSize-1),worldX=terrain.transform.position.x+u*size.x;
                    float exposed=Mathf.SmoothStep(0,1,Mathf.InverseLerp(145,250,Mathf.Abs(worldX)))*Mathf.Clamp01((data.GetSteepness(u,v)-15)/25+(data.GetInterpolatedHeight(u,v)-55)/100)*.92f;
                    float total=0;for(int i=0;i<layers;i++)total+=originalAlpha[z,x,i];
                    for(int i=0;i<layers;i++)alpha[z,x,i]=(total>.001f?originalAlpha[z,x,i]/total:i==0?1:0)*(1-exposed);alpha[z,x,layers]=exposed;
                }
                data.SetAlphamaps(0,0,alpha);
                data.treePrototypes=new[]{new TreePrototype{prefab=pine,bendFactor=.15f}};
                var trees=new System.Collections.Generic.List<TreeInstance>(1000);var random=new System.Random(seed);
                for(int attempt=0;attempt<15000&&trees.Count<1000;attempt++)
                {
                    float worldX=Mathf.Lerp(170,300,(float)random.NextDouble())*Mathf.Sign(terrain.transform.position.x);float u=(worldX-terrain.transform.position.x)/size.x,v=(float)random.NextDouble();
                    if(data.GetSteepness(u,v)>42)continue;float scale=Mathf.Lerp(.8f,1.4f,(float)random.NextDouble());
                    trees.Add(new TreeInstance{position=new Vector3(u,data.GetInterpolatedHeight(u,v)/size.y,v),prototypeIndex=0,widthScale=scale,heightScale=scale,rotation=(float)random.NextDouble()*Mathf.PI*2,color=Color.white,lightmapColor=Color.white});
                }
                if(trees.Count!=1000)throw new InvalidOperationException("Not enough usable mountain forest positions: "+trees.Count);
                data.SetTreeInstances(trees.ToArray(),true);return data;
            }
            catch{UnityEngine.Object.DestroyImmediate(data);throw;}
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
        static Texture2D VistaMountains(Terrain terrain,int seed)
        {
            var source=AssetDatabase.LoadAssetAtPath<TerrainGraph>(GraphPath);if(source==null)throw new InvalidOperationException("Installed Vista mountain sample graph is missing: "+GraphPath);
            var graph=UnityEngine.Object.Instantiate(source);var active=RenderTexture.active;
            try
            {
                var output=graph.GetNode<HeightOutputNode>() as HeightOutputNode;if(output==null)throw new InvalidOperationException("Vista mountain graph has no height output.");
                var origin=terrain.transform.position;var size=terrain.terrainData.size;var config=new TerrainGenerationConfigs{resolution=512,terrainHeight=220,seed=seed,worldBounds=new Rect(origin.x,origin.z,size.x,size.z)};
                using(var pool=graph.ExecuteImmediate(new[]{output.id},config))
                {
                    var result=pool.GetRT(output.mainOutputSlot);if(result==null)throw new InvalidOperationException("Vista did not produce the requested mountain height map.");
                    var target=result.renderTexture;var texture=new Texture2D(target.width,target.height,TextureFormat.RFloat,false,true);
                    try{RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply(false,false);return texture;}
                    catch{UnityEngine.Object.DestroyImmediate(texture);throw;}
                    finally{RenderTexture.active=active;}
                }
            }
            finally{RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(graph);}
        }
#endif
    }
}
