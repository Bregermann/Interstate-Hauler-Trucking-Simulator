using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckFieldGoal.Editor {
// Additive upgrade of inspected saved objects. No venue or participant regeneration.
public static class FieldGoalRepairAuthoring {
 const string Folder="TruckFieldGoal";
 public static void Install(FieldGoalMatch match){
  match.weatherPresets=SportsWeatherPresetsAuthoring.EnsureCatalog();
  SportsWeatherRenderingAuthoring.Install(match,Folder);
  SportsWeatherPickerAuthoring.Install(match,match.weatherPresets,new Vector2(360,145));
  var snowSurface=match.ground.GetComponent<LWS.TruckTaxi.TruckTaxiSurface>()??match.ground.gameObject.AddComponent<LWS.TruckTaxi.TruckTaxiSurface>();snowSurface.isRoad=false;EditorUtility.SetDirty(snowSurface);
  UprightBall(match);Holder(match);EndZone(match);WeatherFeedback(match);
  var help=match.menus.pages[4].transform.Find("Rules and controls").GetComponent<TMPro.TMP_Text>();help.text=match.Help;EditorUtility.SetDirty(help);EditorUtility.SetDirty(match);
 }
 static void UprightBall(FieldGoalMatch match){
  var shape=match.football.shape as CapsuleCollider;if(shape==null)throw new System.InvalidOperationException("Existing authoritative football capsule is required.");
  shape.direction=1;match.football.body.position=new Vector3(0,1.23f,0);match.football.body.rotation=Quaternion.identity;match.football.transform.SetPositionAndRotation(match.football.body.position,Quaternion.identity);
  var root=match.football.transform.Find("Upright leather football presentation");
  if(root==null){root=new GameObject("Upright leather football presentation").transform;root.SetParent(match.football.transform,false);foreach(Transform child in match.football.transform.Cast<Transform>().ToArray())if(child!=root)child.SetParent(root,false);root.localRotation=Quaternion.Euler(-90,0,0);}
  // Replay is render-only but must use the same visual orientation/hierarchy.
  var ghost=match.replay.ghosts[1];var ghostRoot=ghost.Find("Upright leather football presentation");
  if(ghostRoot==null){ghostRoot=new GameObject("Upright leather football presentation").transform;ghostRoot.SetParent(ghost,false);foreach(Transform child in ghost.Cast<Transform>().ToArray())if(child!=ghostRoot)child.SetParent(ghostRoot,false);ghostRoot.localRotation=Quaternion.Euler(-90,0,0);}
  EditorUtility.SetDirty(shape);EditorUtility.SetDirty(match.football.body);
 }
 static void Holder(FieldGoalMatch match){
  if(match.transform.Find("Stationary football holder truck")!=null)return;
  var root=new GameObject("Stationary football holder truck").transform;root.SetParent(match.transform,false);root.position=new Vector3(3.15f,0,-.15f);root.rotation=Quaternion.Euler(0,-90,0);
  var model=Object.Instantiate(match.truck.visuals,root,false);model.name="Existing NWH truck model · holder";model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;
  // Existing visual subtree has no driving authority. Defensively retain render components only.
  foreach(var component in model.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(component);foreach(var collider in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
  var box=new Bounds();bool first=true;foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){if(first){box=renderer.bounds;first=false;}else box.Encapsulate(renderer.bounds);}root.position+=Vector3.up*(.04f-box.min.y);
  var rubber=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Kicking tee rubber.mat");var arm=new GameObject("Holder padded steadying arm").transform;arm.SetParent(root,false);
  // The truck steadies the ball visually; this arm is deliberately non-colliding.
  FieldGoalModels.Cylinder(arm,"Slim holder arm",root.InverseTransformPoint(new Vector3(1.5f,1.55f,.2f)),root.InverseTransformPoint(new Vector3(.38f,1.98f,.1f)),.055f,rubber);
 }
 static void EndZone(FieldGoalMatch match){
  if(match.endZone!=null)return;
  var root=new GameObject("Ten-yard branded end zone · goalposts at back").transform;root.position=new Vector3(0,.018f,match.GoalZ-4.572f);match.endZone=root;
  var teal=SportsEventAuthoring.Mat(Folder,"Painted end zone deep petrol",new Color(.018f,.105f,.13f),.08f);var ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Warm field ivory.mat");
  var surface=SportsEventAuthoring.Box("Ten-yard end-zone paint",Vector3.zero,new Vector3(54,.012f,9.144f),teal);surface.transform.SetParent(root,false);surface.transform.localPosition=Vector3.zero;
  foreach(int side in new[]{-1,1}){var line=SportsEventAuthoring.Box("End-zone boundary stripe",Vector3.zero,new Vector3(54,.015f,.18f),ivory);line.transform.SetParent(root,false);line.transform.localPosition=new Vector3(0,.01f,side*4.572f);}
  var material=SportsEventAuthoring.Mat(Folder,"Original Party wordmark turf ink",Color.white,0);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(SportsBrandAuthoring.WordmarkPath));material.SetFloat("_Surface",1);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.15f);material.SetFloat("_ZWrite",0);material.EnableKeyword("_ALPHATEST_ON");material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.renderQueue=3000;EditorUtility.SetDirty(material);
  var wordmark=GameObject.CreatePrimitive(PrimitiveType.Quad);wordmark.name="Original graphical Party end-zone wordmark";wordmark.transform.SetParent(root,false);wordmark.transform.localPosition=new Vector3(0,.03f,0);wordmark.transform.localRotation=Quaternion.Euler(90,0,0);wordmark.transform.localScale=new Vector3(11,7.333f,1);Object.DestroyImmediate(wordmark.GetComponent<Collider>());wordmark.GetComponent<Renderer>().sharedMaterial=material;
 }
 static void WeatherFeedback(FieldGoalMatch match){
  var current=match.GetComponent<FieldGoalPresentation>();if(current!=null){FixSockFaces(current);return;}
  var presentation=match.gameObject.AddComponent<FieldGoalPresentation>();presentation.match=match;
  var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Golden physical goalposts.mat");var white=IvoryCloth();var red=SportsEventAuthoring.Mat(Folder,"Windsock vermilion cloth",new Color(.85f,.12f,.055f),.1f);red.SetFloat("_Cull",0);EditorUtility.SetDirty(red);
  var root=new GameObject("Actual forecast windsock").transform;root.position=new Vector3(-5,0,1);FieldGoalModels.Cylinder(root,"Windsock pole",Vector3.zero,new Vector3(0,3.8f,0),.045f,gold);
  var sock=new GameObject("Open tapered striped cloth sock").transform;sock.SetParent(root,false);sock.localPosition=new Vector3(0,3.8f,0);presentation.sock=sock;
  const int rings=10,sides=20;for(int band=0;band<rings;band++){
   var vertices=new Vector3[(sides+1)*2];var triangles=new int[sides*6];for(int row=0;row<2;row++){float t=(band+row)/(float)rings;float radius=Mathf.Lerp(.35f,.12f,t);for(int side=0;side<=sides;side++){float a=side*Mathf.PI*2/sides;vertices[row*(sides+1)+side]=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius-.22f*t*t,t*2.3f);}}
   for(int side=0;side<sides;side++){int k=side*6;triangles[k]=side;triangles[k+1]=side+1;triangles[k+2]=side+sides+1;triangles[k+3]=side+1;triangles[k+4]=side+sides+2;triangles[k+5]=side+sides+1;}
   FieldGoalModels.Part(sock,"Cloth band "+band,FieldGoalModels.Persist("Tapered windsock band "+band,vertices,triangles),band%2==0?red:white);
  }
  var hud=SportsUIAuthoring.Rect("Forecast and actual wind",match.menus.hud.transform,new Vector2(340,124),Vector2.zero);var rect=hud.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-20,110);hud.AddComponent<UnityEngine.UI.Image>().color=SportsBroadcastCanvas.Ink;hud.AddComponent<SportsBroadcastPlate>();
  presentation.forecast=SportsUIAuthoring.Text("Forecast",hud.transform,"MATCH FORECAST",new Vector2(310,28),new Vector2(0,41),23);
  presentation.speed=SportsUIAuthoring.Text("Actual wind MPH",hud.transform,"0.0 MPH",new Vector2(240,34),new Vector2(25,5),30);
  presentation.direction=SportsUIAuthoring.Text("Wind direction",hud.transform,"CALM",new Vector2(300,28),new Vector2(0,-38),21);presentation.direction.color=new Color(.55f,.88f,.9f);
  var arrow=SportsUIAuthoring.Rect("Wind arrow relative to view",hud.transform,new Vector2(38,38),new Vector2(-125,0));presentation.windArrow=arrow.GetComponent<RectTransform>();
  FieldArrow(arrow.transform,"Stem",new Vector2(0,-3),new Vector2(5,30),0);FieldArrow(arrow.transform,"Left head",new Vector2(-5,8),new Vector2(5,18),-40);FieldArrow(arrow.transform,"Right head",new Vector2(5,8),new Vector2(5,18),40);EditorUtility.SetDirty(presentation);
 }
 static void FieldArrow(Transform parent,string name,Vector2 position,Vector2 size,float angle){var g=SportsUIAuthoring.Rect(name,parent,size,position);g.transform.localRotation=Quaternion.Euler(0,0,angle);var image=g.AddComponent<UnityEngine.UI.Image>();image.color=SportsBroadcastCanvas.Cream;image.raycastTarget=false;}
 static Material IvoryCloth(){var material=SportsEventAuthoring.Mat(Folder,"Windsock ivory cloth",new Color(.94f,.93f,.83f),.1f);material.SetFloat("_Cull",0);EditorUtility.SetDirty(material);return material;}
 static void FixSockFaces(FieldGoalPresentation presentation){
  // Both cloth colors must remain visible from inside the open cone too.
  // Give the sock its own material rather than changing every white field marking.
  var legacy=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Warm field ivory.mat");var ivory=IvoryCloth();
  foreach(var renderer in presentation.sock.GetComponentsInChildren<MeshRenderer>(true))if(renderer.sharedMaterial==legacy){renderer.sharedMaterial=ivory;EditorUtility.SetDirty(renderer);}
  foreach(var filter in presentation.sock.GetComponentsInChildren<MeshFilter>(true)){
   var mesh=filter.sharedMesh;if(mesh==null||!AssetDatabase.GetAssetPath(mesh).StartsWith("Assets/TruckFieldGoal/Models/"))continue;
   var vertices=mesh.vertices;var triangles=mesh.triangles;var normal=Vector3.Cross(vertices[triangles[1]]-vertices[triangles[0]],vertices[triangles[2]]-vertices[triangles[0]]);
   if(Vector3.Dot(normal,Vector3.right)>=0)continue;for(int i=0;i<triangles.Length;i+=3){int b=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=b;}mesh.triangles=triangles;mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
  }
 }
}
}
