using UnityEngine;using UnityEditor;using LWS.TruckSports.Editor;
namespace LWS.TruckCheeseRolling.Editor {
public static class CheeseRepairAuthoring {
 public static void Install(CheeseMatch m){var terrain=m.weather.terrain;var data=terrain.terrainData;const int resolution=513;data.heightmapResolution=resolution;data.size=new Vector3(140,500,360);var heights=new float[resolution,resolution];for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)heights[z,x]=CheeseRules.Height(x/(resolution-1f)*140-70,z/(resolution-1f)*360-20)/500;data.SetHeights(0,0,heights);int n=data.alphamapResolution;var alpha=new float[n,n,2];for(int z=0;z<n;z++)for(int x=0;x<n;x++){float mud=Mathf.SmoothStep(0,1,(z/(n-1f)*360-20-214)/20);alpha[z,x,0]=1-mud;alpha[z,x,1]=mud;}data.SetAlphamaps(0,0,alpha);terrain.name="High flat starting meadow into continuous enormous downhill";terrain.basemapDistance=900;terrain.heightmapPixelError=4;terrain.Flush();foreach(var camera in m.cameras)camera.view.farClipPlane=1000;
  for(int i=0;i<m.trucks.Length;i++){var t=m.trucks[i];InstallWheels(t);t.transform.SetPositionAndRotation(CheeseRules.Surface((i%8-3.5f)*10,i<8?8:-4,t.StartLift),Quaternion.identity);EditorUtility.SetDirty(t.transform);}m.cheese.transform.position=CheeseRules.Surface(0,27,2);
  foreach(var prop in m.scenery.props){var p=prop.position;p.y=CheeseRules.Height(p.x,p.z);prop.position=p;EditorUtility.SetDirty(prop);}
  foreach(var s in m.sheep)InstallSheep(s);foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(t.gameObject.scene==m.gameObject.scene&&t.name.StartsWith("SM_Nature_Grass_01")){var p=t.position;p.y=CheeseRules.Height(p.x,p.z);t.position=p;}
  EditorUtility.SetDirty(data);EditorUtility.SetDirty(terrain);EditorUtility.SetDirty(m);
 }
 public static void InstallWheels(CheeseTruck truck){
  string[] names={"Whl HD FL","Whl HD FR","Whl HD RL","Whl HD RR"};
  for(int i=0;i<truck.wheels.Length;i++){
   var wheel=truck.wheels[i];wheel.FindOrSpawnVisualContainers();var container=wheel.wheel.rotatingContainer;
   if(container.GetComponentInChildren<MeshRenderer>()!=null)continue;
   MeshRenderer source=null;foreach(var renderer in truck.GetComponentsInChildren<MeshRenderer>(true))if(renderer.name==names[i]){source=renderer;break;}
   if(source==null)throw new System.InvalidOperationException("Owned truck wheel mesh missing: "+names[i]);
   source.transform.SetParent(container,true);var size=source.bounds.size;
   source.transform.localScale=Vector3.Scale(source.transform.localScale,new Vector3(wheel.wheel.width/size.x,wheel.wheel.radius*2/size.y,wheel.wheel.radius*2/size.z));
   source.transform.position+=container.position-source.bounds.center;
   EditorUtility.SetDirty(wheel);EditorUtility.SetDirty(source.transform);
  }
  foreach(var renderer in truck.GetComponentsInChildren<MeshRenderer>(true))if(renderer.name=="Rubber tire")Object.DestroyImmediate(renderer.gameObject);
 }
 public static void InstallSheep(CheeseSheep sheep){var animator=sheep.modelSlot.GetComponentInChildren<Animator>(true);var root=animator!=null?animator.transform:sheep.modelSlot.GetChild(0);SheepMotionAuthoring.Install(root.gameObject,sheep.transform);}
}
}
