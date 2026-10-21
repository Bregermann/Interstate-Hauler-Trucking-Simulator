using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckFrogger.Editor
{
 public static class FrogPresentation
 {
  public static void FinalBindings(){EditorSceneManager.OpenScene(FrogAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<FrogMatch>();var foliage=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFrogger/Materials/Correctly clipped original Vista pine foliage.mat");foliage.SetFloat("_Cutoff",.117f);EditorUtility.SetDirty(foliage);SportsRallyAuthoring.TuneCamera(match.broadcast);EditorSceneManager.SaveScene(match.gameObject.scene);AssetDatabase.SaveAssets();}
  public static void Repair()
  {
   EditorSceneManager.OpenScene(FrogAuthoring.Scene);var m=UnityEngine.Object.FindAnyObjectByType<FrogMatch>();var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFishing/Materials/Clear fishing river.mat");if(source==null)throw new InvalidOperationException("Existing verified URP river material required.");string path="Assets/TruckFrogger/Materials/Animated crossing URP water.mat";var water=AssetDatabase.LoadAssetAtPath<Material>(path);if(water==null){water=UnityEngine.Object.Instantiate(source);water.name="Animated crossing URP water";water.color=new Color(.025f,.27f,.37f,.85f);water.mainTextureScale=new Vector2(60,6);AssetDatabase.CreateAsset(water,path);}
   var yellow=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFrogger/Materials/Safety yellow industrial enamel.mat");var white=SportsEventAuthoring.Mat("TruckFrogger","Scuffed road marking white",new Color(.83f,.86f,.76f),.07f);var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFrogger/Materials/Worn galvanized crossing machinery.mat");
   foreach(var c in m.world.courses)
   {
    foreach(var t in c.root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Existing animated River Modeler crossing water")){t.GetComponent<Renderer>().sharedMaterial=water;if(t.GetComponent<LWS.TruckFishing.FishingWaterFlow>()==null)t.gameObject.AddComponent<LWS.TruckFishing.FishingWaterFlow>();}
    foreach(var mover in c.movers)if(mover.loop){mover.halfWidth=56;mover.body.collisionDetectionMode=CollisionDetectionMode.Discrete;}
    foreach(var text in c.root.GetComponentsInChildren<TextMesh>(true))if(text.name=="Readable crossing warning"||text.name=="Unmistakable crossing finish")text.characterSize=.12f;
    if(c.root.Find("Finished crossing lane markings and roadside fixtures")!=null)continue;var dressing=new GameObject("Finished crossing lane markings and roadside fixtures").transform;dressing.SetParent(c.root,false);
    for(float z=-70;z<c.finish.z+35;z+=12){if(c.water.Any(w=>z>w.x-3&&z<w.y+3))continue;foreach(int side in new[]{-1,1}){SportsEventAuthoring.Box("Readable actual roadside edge marking",new Vector3(side*15.2f,.065f,z),new Vector3(.18f,.025f,10),white,null,dressing);if(!c.gates.Any(g=>Mathf.Abs(g.z-z)<26)){SportsEventAuthoring.Box("Physical road-edge reflector post",new Vector3(side*22,1.1f,z),new Vector3(.15f,2.2f,.15f),steel,null,dressing);SportsEventAuthoring.Box("Reflector yellow face",new Vector3(side*22,1.8f,z-.1f),new Vector3(.32f,.22f,.05f),yellow,null,dressing);}}SportsEventAuthoring.Box("Dashed centre road guidance",new Vector3(0,.07f,z),new Vector3(.2f,.025f,5),yellow,null,dressing);}
    foreach(var range in c.water)for(int bank=0;bank<2;bank++){float z=bank==0?range.x:range.y;foreach(int side in new[]{-1,1}){var p=new Vector3(side*29,-1,z);SportsEventAuthoring.Box("Grounded quay mooring bollard",p,new Vector3(.8f,3,.8f),yellow,null,dressing);SportsEventAuthoring.Box("Steel mooring bollard cap",p+Vector3.up*1.4f,new Vector3(1.3f,.3f,1.3f),steel,null,dressing);}}
   }
   var foliageSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSnake/Materials/Imported VistaSample_TreeLeaves.mat");string foliagePath="Assets/TruckFrogger/Materials/Correctly clipped original Vista pine foliage.mat";var foliage=AssetDatabase.LoadAssetAtPath<Material>(foliagePath);if(foliage==null){foliage=UnityEngine.Object.Instantiate(foliageSource);foliage.name="Correctly clipped original Vista pine foliage";foliage.SetFloat("_AlphaClip",1);foliage.SetFloat("_Cutoff",.117f);foliage.SetFloat("_Cull",0);foliage.SetOverrideTag("RenderType","TransparentCutout");foliage.EnableKeyword("_ALPHATEST_ON");foliage.renderQueue=2450;AssetDatabase.CreateAsset(foliage,foliagePath);}foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include)){var materials=renderer.sharedMaterials;bool changed=false;for(int j=0;j<materials.Length;j++)if(materials[j]==foliageSource){materials[j]=foliage;changed=true;}if(changed)renderer.sharedMaterials=materials;}
   Districts(m);EditorSceneManager.SaveScene(m.gameObject.scene);AssetDatabase.SaveAssets();
  }
  static void Districts(FrogMatch match)
  {
   foreach(var c in match.world.courses)
   {
    if(c.root.Find("Installed detailed crossing district")!=null)continue;var root=new GameObject("Installed detailed crossing district").transform;root.SetParent(c.root,false);int index=Array.IndexOf(match.world.courses,c);
    var rows=index==0?new[]{89f,149,209,269}:index==1?new[]{87f,152,217,285}:index==2?new[]{10f,140,255,305}:index==3?new[]{85f,150,215,285}:new[]{135f,205,300,370,460};
    foreach(float z in rows)foreach(int side in new[]{-1,1})for(int j=0;j<3;j++)
    {
     string path=index==0?"Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Commercial_03.prefab":index==1?"Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Factory_01.prefab":index==2?"Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_House_02_V1.prefab":"Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab";
     var holder=new GameObject("Owned installed crossing district building").transform;holder.SetParent(root,false);holder.localPosition=new Vector3(side*(58+j*35),-.2f,z);LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder,path,new Vector3(27,19+j%2*9,14),0,false,"TruckFrogger");foreach(var mesh in holder.GetComponentsInChildren<MeshFilter>()){var shape=mesh.gameObject.AddComponent<MeshCollider>();shape.sharedMesh=mesh.sharedMesh;}
    }
   }
  }
 }
}
