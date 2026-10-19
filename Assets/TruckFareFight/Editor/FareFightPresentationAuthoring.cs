using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports.Editor;
namespace LWS.TruckFareFight.Editor {
 public static class FareFightPresentationAuthoring {
  public static void Upgrade(){EditorSceneManager.OpenScene(FareFightAuthoring.Scene);var m=UnityEngine.Object.FindAnyObjectByType<FareFightMatch>();Install(m);EditorSceneManager.SaveScene(m.gameObject.scene);AssetDatabase.SaveAssets();}
  public static void Install(FareFightMatch m){SportsMatchAudioAuthoring.Install(m,m.trucks);FareFightHazardAuthoring.Install(m);
   if(m.menus.pages[6].GetComponentsInChildren<LWS.TruckSports.SportsMenuAction>(true).All(a=>a.action!="watchfare"))SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH WINNING FARE","watchfare",new Vector2(0,-245),new Vector2(520,48));
   string[] buildings={"SM_Buildings_Commercial_01","SM_Buildings_Commercial_03","SM_Buildings_Commercial_06","SM_Buildings_Commercial_08","SM_Buildings_Factory_01","SM_Buildings_Warehouse_01","SM_Buildings_Hotel_01","SM_Buildings_House_01_V1","SM_Buildings_House_02_V1","SM_Buildings_House_04_V1","SM_Buildings_House_06_V1"};int index=0;
   foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include).Where(r=>r.name=="City block").ToArray()){
    if(renderer.transform.Find("Owned city architecture")!=null)continue;var bounds=renderer.bounds;var root=new GameObject("Owned city architecture").transform;root.position=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);root.SetParent(renderer.transform,true);
    // Keep the original town footprint, roads and solid collision authority.
    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root,"Assets/JC_LP_MegaCity/Prefabs/Buildings/"+buildings[index++%buildings.Length]+".prefab",bounds.size,0,true,FareFightAuthoring.Folder);
    renderer.enabled=false;
   }
   var asphalt=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/NWH/Common/TerrainLayers/AsphaltLayer.terrainlayer");var road=SportsEventAuthoring.Mat(FareFightAuthoring.Folder,"Original town textured asphalt",new Color(.65f,.68f,.7f),.15f);road.mainTexture=asphalt.diffuseTexture;road.SetTexture("_BumpMap",asphalt.normalMapTexture);road.EnableKeyword("_NORMALMAP");road.mainTextureScale=Vector2.one*18;
   foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))if(renderer.name.Contains("Baked City Streets")){renderer.sharedMaterial=road;var marker=renderer.GetComponent<LWS.TruckTaxi.TruckTaxiSurface>()??renderer.gameObject.AddComponent<LWS.TruckTaxi.TruckTaxiSurface>();marker.isRoad=true;}
   if(GameObject.Find("Owned roadside district dressing")!=null){ConfigureProps(m,GameObject.Find("Owned roadside district dressing").transform);return;}var props=new GameObject("Owned roadside district dressing").transform;var random=new System.Random(817413);
   string[] sources={"Assets/RoadPropsPack/URP/Prefabs/TrafficCone_URP.prefab","Assets/RoadPropsPack/URP/Prefabs/TrashCan_URP.prefab","Assets/ConstructionPropsBundle/URP/Prefabs/SM_Barrel01 Variant.prefab"};
   for(int i=0;i<40;i++){int row=i/8;float x=-285+i%8*80,z=-285+row*140;var root=new GameObject("Physical roadside prop "+i).transform;root.SetParent(props,false);root.position=new Vector3(x,.2f,z);LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root,sources[i%sources.Length],new Vector3(1.2f,1.5f,1.2f),0,false,FareFightAuthoring.Folder);var shape=root.gameObject.AddComponent<BoxCollider>();shape.center=new Vector3(0,.65f,0);shape.size=new Vector3(.9f,1.3f,.9f);var body=root.gameObject.AddComponent<Rigidbody>();body.mass=i%3==0?5:35;body.maxDepenetrationVelocity=3;body.interpolation=RigidbodyInterpolation.Interpolate;}
   m.triggerRadius=16;for(int i=0;i<m.district.startingPositions.Length;i++)m.district.startingPositions[i].y=.35f;EditorUtility.SetDirty(m.district);
   foreach(var truck in m.trucks)foreach(var rb in truck.GetComponentsInChildren<Rigidbody>(true))if(rb.GetComponent<FareFightContact>()==null){var proxy=rb.gameObject.AddComponent<FareFightContact>();proxy.match=m;proxy.truck=truck;}
   ConfigureProps(m,props);EditorUtility.SetDirty(m);
  }
  static void ConfigureProps(FareFightMatch m,Transform root){
   foreach(Transform prop in root){if(!prop.name.StartsWith("Physical roadside prop "))continue;int i=int.Parse(prop.name.Substring("Physical roadside prop ".Length));float x=-320+(i%5)*160+(i%2==0?11:-11),z=-280+(i/5)*80;prop.position=new Vector3(x,.2f,z);}
   var reset=m.GetComponent<FareFightWorldReset>()??m.gameObject.AddComponent<FareFightWorldReset>();m.worldReset=reset;reset.props=root.GetComponentsInChildren<Rigidbody>(true).Select(body=>new FareFightWorldReset.Prop{body=body,position=body.position,rotation=body.rotation}).ToArray();EditorUtility.SetDirty(reset);EditorUtility.SetDirty(m);
  }

 }
}
