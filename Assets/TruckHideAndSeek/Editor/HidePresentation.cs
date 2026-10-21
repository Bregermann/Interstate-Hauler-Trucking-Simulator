using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports.Editor;
using DigitalRuby.WeatherMaker;
namespace LWS.TruckHideAndSeek.Editor
{
 // Scene-owned fixture placement only. Weather Maker retains sky/light authority.
 public static class HidePresentation
 {
  public static void RoadSnow(){EditorSceneManager.OpenScene(HideAuthoring.Scene);var m=Object.FindAnyObjectByType<HideMatch>();BindRoadSnow(m);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
  public static void Repair(){EditorSceneManager.OpenScene(HideAuthoring.Scene);Install(Object.FindAnyObjectByType<HideMatch>());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
  public static void BindRoadSnow(HideMatch m){var snow=m.GetComponent<SportsSnowSurface>();if(snow!=null)snow.traceExposedRoads=true;var materials=new System.Collections.Generic.Dictionary<Material,Material>();int count=0;foreach(var road in Object.FindObjectsByType<LwsRoadSurface>(FindObjectsInactive.Include))foreach(var renderer in road.GetComponentsInChildren<MeshRenderer>(true)){var source=renderer.sharedMaterial;if(source==null)continue;if(LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(source,LwsWeatheradeSurfaceMaterialMode.Snow)&&AssetDatabase.GetAssetPath(source).StartsWith("Assets/"+HideAuthoring.Folder+"/")){source.SetFloat("_CoverageAmount",0);source.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(source);continue;}if(!materials.TryGetValue(source,out var material)){string path="Assets/"+HideAuthoring.Folder+"/Materials/Accumulating road snow "+materials.Count+".mat";material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){if(!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial(source.name+" Hide owned snow",source,Color.gray,LwsWeatheradeSurfaceMaterialMode.Snow,out material))throw new System.InvalidOperationException("Installed Weatherade snow shader required");AssetDatabase.CreateAsset(material,path);}materials.Add(source,material);}material.SetFloat("_CoverageAmount",0);material.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(material);renderer.sharedMaterial=material;EditorUtility.SetDirty(renderer);count++;}Debug.Log("HIDE_ACTUAL_WEATHERADE_ROADS_BOUND "+count+" unique="+materials.Count);}
  public static void Install(HideMatch m)
  {
   foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>t.name.StartsWith("Warm industrial loading-area light ")||t.name=="Loading lamp visible steel mount"||t.name=="Grounded night-shift lighting and sign supports").ToArray())Object.DestroyImmediate(t.gameObject);
   var root=new GameObject("Grounded night-shift lighting and sign supports").transform;
   var steel=SportsEventAuthoring.Mat(HideAuthoring.Folder,"Galvanized grounded night-shift fixtures",new Color(.22f,.27f,.31f),.5f);
   var lens=SportsEventAuthoring.Mat(HideAuthoring.Folder,"Warm emissive loading-light lens",new Color(1,.77f,.4f));lens.EnableKeyword("_EMISSION");lens.SetColor("_EmissionColor",new Color(1,.62f,.2f)*3);
   foreach(var hangar in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t=>t.name.StartsWith("Actual open modular industrial hangar ")))
    foreach(int side in new[]{-1,1}){var p=hangar.TransformPoint(new Vector3(side*16,10,-20.8f));Part("Wall-mounted loading floodlight",p,new Vector3(1.8f,.5f,.8f),steel);Part("Warm fixture lens",p+new Vector3(0,-.26f,-.1f),new Vector3(1.4f,.08f,.55f),lens);Spot(p+Vector3.back*.6f,new Vector3(70,180,0),75,45);}
   for(int i=0;i<12;i++)
   {
    var p=new Vector3(i%2==0?-18:18,0,-190+i/2*70);p.y=m.world.Height(p);Part("Grounded industrial road-light pole",p+Vector3.up*6,new Vector3(.38f,12,.38f),steel,true);float inward=p.x<0?1:-1;Part("Cantilever road-light arm",p+new Vector3(inward*2,11.8f,0),new Vector3(4,.22f,.26f),steel);var lamp=p+new Vector3(inward*4,11.6f,0);Part("Road-light housing",lamp,new Vector3(1.5f,.35f,.9f),steel);Part("Visible warm road-light lens",lamp+Vector3.down*.18f,new Vector3(1.25f,.08f,.7f),lens);Spot(lamp+Vector3.down*.25f,new Vector3(90,0,0),110,48);
   }
   foreach(var sign in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t=>t.name=="Original Hide & Seek industrial event artwork"))
   {
    foreach(int side in new[]{-1,1}){var p=sign.position+Vector3.right*side*16;float ground=m.world.Height(p);Part("Grounded event gantry column",new Vector3(p.x,(ground+sign.position.y+10)*.5f,p.z+.3f),new Vector3(.55f,sign.position.y+10-ground,.55f),steel,true);}Part("Event gantry top crossbeam",sign.position+new Vector3(0,10.3f,.3f),new Vector3(33,.6f,.6f),steel);
   }
   var profile=m.atmosphere.vendor.GetComponentInChildren<WeatherMakerDayNightCycleManagerScript>(true).DayNightProfile;profile.NightAmbientColorIntensity=.18f;EditorUtility.SetDirty(profile);
   foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))if(t.text.Contains("ORIGINAL")||t.text.Contains("DOWNTOWN"))t.text="NIGHT SHIFT · INDUSTRIAL DISTRICT";
   Debug.Log("HIDE_GROUNDED_FIXTURES_AND_SIGNS_SAVED");
   void Part(string name,Vector3 p,Vector3 size,Material material,bool collision=false){var g=SportsEventAuthoring.Box(name,p,size,material);g.transform.SetParent(root,true);if(collision&&g.GetComponent<Collider>()==null)g.AddComponent<BoxCollider>();}
   void Spot(Vector3 p,Vector3 rotation,float intensity,float range){var light=new GameObject("Supported warm night-shift floodlight").AddComponent<Light>();light.transform.SetParent(root);light.transform.SetPositionAndRotation(p,Quaternion.Euler(rotation));light.type=LightType.Spot;light.range=range;light.spotAngle=115;light.intensity=intensity;light.color=new Color(1,.75f,.43f);light.shadows=LightShadows.None;}
  }
 }
}
