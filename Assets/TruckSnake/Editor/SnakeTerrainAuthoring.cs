#if VISTA
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.UnityTerrain;
using LWS.TruckSports.Editor;
namespace LWS.TruckSnake.Editor {
 public static class SnakeTerrainAuthoring {
  public static ProgressiveTask Generate(SnakeMatch match,List<Vector3[]> plannedRoads){
   Directory.CreateDirectory("Assets/TruckSnake/Terrain");Directory.CreateDirectory("Assets/TruckSnake/Prefabs");
   var manager=new GameObject("Vista authored rolling countryside").AddComponent<VistaManager>();manager.terrainMaxHeight=60;manager.heightMapResolution=513;manager.textureResolution=256;foreach(var field in new[]{"m_beforeGeneratingUnityCallback","m_afterGeneratingUnityCallback"})typeof(VistaManager).GetField(field,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(manager,new UnityEngine.Events.UnityEvent());
   var source=AssetDatabase.LoadAssetAtPath<TerrainGraph>("Assets/PinwheelStudio/Vista/Personal/Samples/Graphs/MoundOfEarth.asset");if(source==null)throw new InvalidOperationException("Installed Vista MoundOfEarth foundation is required.");
   var graph=UnityEngine.Object.Instantiate(source);graph.name="Snake Vista countryside and flat original town";
   var height=graph.GetNodesOfType<HeightOutputNode>().Single();var old=graph.GetEdges().Single(edge=>edge.inputSlot.nodeId==height.id);graph.RemoveEdge(old.id);
   var mask=new ShapeNode{shape=ShapeNode.Shape.Square,scale=new Vector2(.32f,.4f),innerSize=.85f};var flat=new FlattenNode{targetHeight=0,useWorldHeight=true};graph.AddNode(mask);graph.AddNode(flat);graph.AddEdge(new Edge(old.outputSlot,new SlotRef(flat.id,flat.inputSlot.id)));graph.AddEdge(new Edge(new SlotRef(mask.id,mask.outputSlot.id),new SlotRef(flat.id,flat.maskSlot.id)));graph.AddEdge(new Edge(new SlotRef(flat.id,flat.outputSlot.id),new SlotRef(height.id,height.inputSlot.id)));
   var treeTemplate=ScriptableObject.CreateInstance<TreeTemplate>();treeTemplate.name="Owned Vista country pine";treeTemplate.prefab=TreePrefab();AssetDatabase.CreateAsset(treeTemplate,"Assets/TruckSnake/Terrain/Country pine template.asset");
   var locations=new SelectPositionNode();var trees=new TreeOutputNode{treeTemplate=treeTemplate,verticalScaleMultiplier=1,horizontalScaleMultiplier=1};var random=new System.Random(918171);var points=new List<Vector2>();
   for(int i=0;i<1700;i++){float x=-1460+(float)random.NextDouble()*2920,z=-1160+(float)random.NextDouble()*2320;if(Mathf.Abs(x)<480&&Mathf.Abs(z)<480||x>450&&z<550||z<-450&&x> -500)continue;bool near=false;foreach(var line in plannedRoads){for(int k=1;k<line.Length;k++)if(Distance(new Vector2(x,z),new Vector2(line[k-1].x,line[k-1].z),new Vector2(line[k].x,line[k].z))<40){near=true;break;}if(near)break;}if(!near)points.Add(new Vector2((x+1500)/3000,(z+1200)/2400));}
   locations.positions=points.ToArray();graph.AddNode(locations);graph.AddNode(trees);graph.AddEdge(new Edge(new SlotRef(locations.id,locations.outputSlot.id),new SlotRef(trees.id,trees.positionInputSlot.id)));AssetDatabase.CreateAsset(graph,"Assets/TruckSnake/Terrain/Countryside Vista graph.asset");
   var biome=LocalProceduralBiome.CreateInstanceInScene(manager);biome.name="Original country biome with reserved town footprint";biome.terrainGraph=graph;biome.baseResolution=1024;biome.seed=918171;biome.falloffDistance=0;biome.dataMask=BiomeDataMask.HeightMap|BiomeDataMask.TreeInstances;biome.anchors=new[]{new Vector3(-1500,0,-1200),new Vector3(-1500,0,1200),new Vector3(1500,0,1200),new Vector3(1500,0,-1200)};
   match.world.terrain=new Terrain[4];var tiles=new List<ITile>();for(int z=0;z<2;z++)for(int x=0;x<2;x++){
    var data=new TerrainData{name="Snake Vista tile "+x+" "+z,heightmapResolution=513,alphamapResolution=256,size=new Vector3(1500,60,1200)};AssetDatabase.CreateAsset(data,"Assets/TruckSnake/Terrain/Playable tile "+x+" "+z+".asset");
    var root=Terrain.CreateTerrainGameObject(data);root.name=data.name;root.transform.position=new Vector3(-1500+x*1500,0,-1200+z*1200);var tile=root.AddComponent<TerrainTile>();tile.managerId=manager.id;match.world.terrain[z*2+x]=root.GetComponent<Terrain>();tiles.Add(tile);
   }
   for(int z=0;z<2;z++)for(int x=0;x<2;x++){var tile=match.world.terrain[z*2+x];tile.SetNeighbors(x>0?match.world.terrain[z*2+x-1]:null,z<1?match.world.terrain[2+x]:null,x<1?match.world.terrain[z*2+x+1]:null,z>0?match.world.terrain[x]:null);}
   return manager.Generate(tiles);
  }
  public static void Finish(SnakeMatch match){
   var source=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/NWH/Common/TerrainLayers/GrassLayer.asset");if(source==null||source.diffuseTexture==null)throw new InvalidOperationException("Installed grass terrain layer required.");var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TruckSnake/Terrain/Country grass.terrainlayer");if(layer==null){layer=UnityEngine.Object.Instantiate(source);layer.name="Snake broad country grass";layer.tileSize=Vector2.one*20;AssetDatabase.CreateAsset(layer,"Assets/TruckSnake/Terrain/Country grass.terrainlayer");}
   var material=SportsEventAuthoring.Mat("TruckSnake","Vista URP terrain",Color.white,.15f);material.shader=Shader.Find("Universal Render Pipeline/Terrain/Lit");
   foreach(var terrain in match.world.terrain){terrain.terrainData.terrainLayers=new[]{layer};int size=terrain.terrainData.alphamapResolution;var alpha=new float[size,size,1];for(int z=0;z<size;z++)for(int x=0;x<size;x++)alpha[z,x,0]=1;terrain.terrainData.SetAlphamaps(0,0,alpha);terrain.materialTemplate=material;terrain.drawInstanced=true;terrain.heightmapPixelError=6;terrain.basemapDistance=1400;terrain.treeDistance=650;terrain.treeBillboardDistance=160;terrain.treeCrossFadeLength=45;EditorUtility.SetDirty(terrain.terrainData);}
  }
  static GameObject TreePrefab(){string path="Assets/TruckSnake/Prefabs/Original Vista pine.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;var root=new GameObject("Vista country pine");try{LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root.transform,"Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_PineTree.prefab",new Vector3(6,14,6),0,false,"TruckSnake");var trunk=root.AddComponent<CapsuleCollider>();trunk.radius=.45f;trunk.height=8;trunk.center=Vector3.up*4;return PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEngine.Object.DestroyImmediate(root);}}
  static float Distance(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));}
 }
}
#endif
