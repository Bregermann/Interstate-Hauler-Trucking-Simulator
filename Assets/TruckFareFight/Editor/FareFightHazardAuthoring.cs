using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LWS.TruckSports.Editor;
namespace LWS.TruckFareFight.Editor {
 public static class FareFightHazardAuthoring {
  public static void Install(FareFightMatch match){if(match.hazards!=null)return;var host=match.gameObject.AddComponent<FareFightHazards>();host.match=match;match.hazards=host;var list=new List<FareFightHazard>();
   var oil=SportsEventAuthoring.Mat(FareFightAuthoring.Folder,"Readable iridescent oil",new Color(.06f,.035f,.12f),.96f);var peel=SportsEventAuthoring.Mat(FareFightAuthoring.Folder,"Oversized yellow banana peel",new Color(1,.78f,.035f),.4f);
   string meshPath="Assets/TruckFareFight/Prefabs/Original curved banana peel.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(mesh==null){mesh=Banana();AssetDatabase.CreateAsset(mesh,meshPath);}
   for(int i=0;i<24;i++){var root=new GameObject(i<8?"Clearly marked oil slick":i<18?"Oversized banana peel":"Existing exploding sheep");root.transform.SetParent(host.transform,false);var hazard=root.AddComponent<FareFightHazard>();hazard.host=host;hazard.kind=i<8?FareFightHazardKind.Oil:i<18?FareFightHazardKind.Banana:FareFightHazardKind.Sheep;
    if(hazard.kind==FareFightHazardKind.Oil){var model=GameObject.CreatePrimitive(PrimitiveType.Cylinder);UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());model.transform.SetParent(root.transform,false);model.transform.localScale=new Vector3(4,.025f,3);model.GetComponent<Renderer>().sharedMaterial=oil;var shape=root.AddComponent<BoxCollider>();shape.isTrigger=true;shape.center=Vector3.up*.3f;shape.size=new Vector3(4,.6f,3);}
    else if(hazard.kind==FareFightHazardKind.Banana){root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=peel;var shape=root.AddComponent<BoxCollider>();shape.isTrigger=true;shape.center=Vector3.up*.25f;shape.size=new Vector3(3,.6f,3);}
    else{LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root.transform,"Assets/TruckCheeseRolling/Art/Sheep/SheepV5.fbx",new Vector3(1.2f,1.5f,2),0,false,FareFightAuthoring.Folder);var body=root.AddComponent<Rigidbody>();body.isKinematic=true;var shape=root.AddComponent<BoxCollider>();shape.center=Vector3.up*.65f;shape.size=new Vector3(1.1f,1.3f,1.7f);SheepMotionAuthoring.Install(root,root.transform.GetChild(0));}
    root.transform.position=new Vector3(0,-100-i*4,0);list.Add(hazard);
   }host.hazards=list.ToArray();EditorUtility.SetDirty(match);
  }
  static Mesh Banana(){var vertices=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();
   for(int leaf=0;leaf<3;leaf++){float yaw=leaf*Mathf.PI*2/3;var q=Quaternion.Euler(0,yaw*Mathf.Rad2Deg,0);for(int row=0;row<=12;row++){float t=row/12f,width=Mathf.Sin(Mathf.PI*t)*.28f+.025f,height=.18f+Mathf.Sin(t*Mathf.PI)*.18f+(1-t)*(1-t)*.75f;for(int side=0;side<2;side++){vertices.Add(q*new Vector3((side==0?-1:1)*width,height,t*1.65f));uv.Add(new Vector2(side,t));}if(row>0){int a=leaf*26+(row-1)*2;indices.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2,a+2,a+1,a,a+2,a+3,a+1});}}}
   var mesh=new Mesh{name="Original three curved banana peels"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
 }
}
