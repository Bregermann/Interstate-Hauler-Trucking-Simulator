using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckFieldGoal.Editor {
public static class FieldGoalBoardAuthoring {
 const string Folder="TruckFieldGoal";
 public const float FieldLength=109.728f;
 [MenuItem("Truck Sports/Field Goal/Upgrade stadium scoreboard")]
 public static void Upgrade(){var scene=EditorSceneManager.OpenScene(FieldGoalAuthoring.Scene);Install(UnityEngine.Object.FindAnyObjectByType<FieldGoalMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Install(FieldGoalMatch m){
  if(m.stadiumBoard!=null){TuneDisplay(m.stadiumBoard);m.stadiumBoard.AlignField();FieldGoalFieldArtAuthoring.Install(m);return;}
  var assembly=new GameObject("Goal-line anchored field and stadium scoreboard").transform;
  assembly.position=new Vector3(0,0,m.GoalZ);
  // Only replace the obsolete field paint. Existing trucks, crowd, weather and venue remain intact.
  foreach(var root in m.gameObject.scene.GetRootGameObjects())if(root.name.StartsWith("Painted ten-yard reference stripe")||root.name=="Inlaid yard hash"||root.name=="Field sideline")UnityEngine.Object.DestroyImmediate(root);
  var field=m.ground.transform;field.SetParent(assembly,true);field.localPosition=new Vector3(0,-.5f,-FieldLength*.5f);field.localScale=new Vector3(64,1,FieldLength);
  var ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Warm field ivory.mat");
  var ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Painted end zone deep petrol.mat");
  m.endZone.SetParent(assembly,true);m.endZone.localPosition=new Vector3(0,.018f,-4.572f);
  for(int yard=0;yard<=100;yard+=10){float z=-9.144f-yard*.9144f;Part(assembly,"Regulation ten-yard field stripe "+yard,new Vector3(0,.009f,z),new Vector3(54,.012f,.12f),ivory);if(yard==100)continue;for(int n=1;n<10;n++)foreach(int side in new[]{-1,1})Part(assembly,"Inlaid single-yard hash",new Vector3(side*8,.009f,z-n*.9144f),new Vector3(.55f,.012f,.09f),ivory);}
  foreach(int side in new[]{-1,1})Part(assembly,"Full-field sideline",new Vector3(side*27,.012f,-FieldLength*.5f),new Vector3(.15f,.014f,FieldLength),ivory);
  Part(assembly,"Opposite end-zone paint",new Vector3(0,.018f,-FieldLength+4.572f),new Vector3(54,.012f,9.144f),ink);
  Part(assembly,"Opposite end line",new Vector3(0,.03f,-FieldLength),new Vector3(54,.015f,.18f),ivory);
  var steel=SportsEventAuthoring.Mat(Folder,"Stadium scoreboard charcoal steel",new Color(.035f,.05f,.065f),.42f);steel.SetFloat("_Metallic",.65f);
  var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Golden physical goalposts.mat");
  Part(assembly,"Fabricated scoreboard housing",new Vector3(0,28,13.2f),new Vector3(50.4f,21.8f,1.4f),steel);
  foreach(int side in new[]{-1,1}){
   FieldGoalModels.Cylinder(assembly,"Scoreboard steel support "+side,new Vector3(side*19,0,13.7f),new Vector3(side*19,18,13.7f),.36f,steel);
   Part(assembly,"Scoreboard gold edge "+side,new Vector3(side*24.6f,28,12.4f),new Vector3(.22f,20.6f,.15f),gold);
  }
  var board=m.gameObject.AddComponent<FieldGoalStadiumBoard>();board.match=m;board.fieldAssembly=assembly;m.stadiumBoard=board;
  var panel=new GameObject("Embedded stadium display",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster));
  panel.transform.SetParent(assembly,false);panel.transform.localPosition=new Vector3(0,28,12.35f);panel.transform.localScale=Vector3.one*.03f;
  var rect=(RectTransform)panel.transform;rect.sizeDelta=new Vector2(1600,660);board.screen=panel.GetComponent<Canvas>();board.screen.renderMode=RenderMode.WorldSpace;board.screen.worldCamera=m.cameraRig.view;
  Plate(panel.transform,"LED screen",Vector2.zero,new Vector2(1600,660),new Color(.006f,.016f,.025f));
  Plate(panel.transform,"Amber header rule",new Vector2(0,244),new Vector2(1500,4),new Color(1,.55f,.12f));
  board.title=Text(panel.transform,"Sport title","",new Vector2(0,285),new Vector2(1500,64),44);
  board.attempt=Text(panel.transform,"Round and attempts","",new Vector2(-505,206),new Vector2(490,55),42);
  board.current=Text(panel.transform,"Current shooter and distance","",new Vector2(250,206),new Vector2(1000,55),42);
  board.cards=new GameObject[4];board.names=new TMP_Text[4];board.scores=new TMP_Text[4];board.kicks=new TMP_Text[4];board.colors=new UnityEngine.UI.Image[4];
  for(int i=0;i<4;i++){
   var card=SportsUIAuthoring.Rect("Scoreboard participant "+(i+1),panel.transform,new Vector2(730,108),new Vector2(i%2==0?-385:385,119-i/2*120));board.cards[i]=card;
   Plate(card.transform,"Player card",Vector2.zero,new Vector2(730,108),new Color(.025f,.06f,.085f));board.colors[i]=Plate(card.transform,"Identity stripe",new Vector2(-357,0),new Vector2(6,86),Color.white);
   board.names[i]=Text(card.transform,"Participant name","",new Vector2(-58,23),new Vector2(570,44),38);board.names[i].alignment=TextAlignmentOptions.MidlineLeft;
   board.kicks[i]=Text(card.transform,"Attempts and active shooter","",new Vector2(-58,-23),new Vector2(570,38),30);board.kicks[i].alignment=TextAlignmentOptions.MidlineLeft;
   board.scores[i]=Text(card.transform,"Successful goals","",new Vector2(302,0),new Vector2(106,95),74);
  }
  board.status=Text(panel.transform,"Live play and result","",new Vector2(0,-100),new Vector2(1500,63),45);
  board.weather=Text(panel.transform,"Forecast wind speed and bearing","",new Vector2(0,-156),new Vector2(1500,44),31);
  board.controls=Text(panel.transform,"Actual input and replay prompts","",new Vector2(0,-204),new Vector2(1500,44),30);
  board.record=Text(panel.transform,"Career statistics","",new Vector2(0,-305),new Vector2(1500,34),27);board.record.color=new Color(.55f,.72f,.76f);
  board.buttons=new Michsky.UI.Heat.ButtonManager[5];string[] labels={"ANGLE","SPEED","REWIND","CONTINUE","QUICK RETRY"};string[] keys={"replayangle","replayspeed","replayrewind","replayskip","retry"};
  foreach(var old in m.menus.replayButtons)old.SetActive(false);m.practiceRetry.SetActive(false);
  for(int i=0;i<5;i++){var button=SportsUIAuthoring.Button(panel.transform,labels[i],keys[i],new Vector2(-555+i*370,-260),new Vector2(340,46));board.buttons[i]=button;if(i==4){button.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-260);m.practiceRetry=button.gameObject;}else m.menus.replayButtons[i]=button.gameObject;button.gameObject.SetActive(false);}
  for(int i=0;i<4;i++){var select=board.buttons[i].GetComponent<UnityEngine.UI.Button>();var navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnLeft=board.buttons[(i+3)%4].GetComponent<UnityEngine.UI.Button>(),selectOnRight=board.buttons[(i+1)%4].GetComponent<UnityEngine.UI.Button>(),selectOnUp=board.buttons[(i+3)%4].GetComponent<UnityEngine.UI.Button>(),selectOnDown=board.buttons[(i+1)%4].GetComponent<UnityEngine.UI.Button>()};select.navigation=navigation;var heat=board.buttons[i];heat.selectOnLeft=navigation.selectOnLeft.gameObject;heat.selectOnRight=navigation.selectOnRight.gameObject;heat.selectOnUp=navigation.selectOnUp.gameObject;heat.selectOnDown=navigation.selectOnDown.gameObject;}
  var presentation=m.GetComponent<FieldGoalPresentation>();if(presentation!=null){presentation.forecast.transform.parent.gameObject.SetActive(false);var pole=presentation.sock.parent;pole.position=new Vector3(-10,0,1);presentation.sock.localPosition=new Vector3(0,8.5f,0);var cylinder=pole.Find("Windsock pole");cylinder.localPosition=new Vector3(0,4.25f,0);cylinder.localScale=new Vector3(cylinder.localScale.x,4.25f,cylinder.localScale.z);}
  TuneDisplay(board);board.AlignField();FieldGoalFieldArtAuthoring.Install(m);board.Refresh();EditorUtility.SetDirty(m);EditorUtility.SetDirty(board);
 }
 static void TuneDisplay(FieldGoalStadiumBoard board){
  if(board.fieldAssembly.Find("Stadium concourse ground") == null){var concourse=board.match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).First(r=>r.name=="Union Field End-zone concourse");Part(board.fieldAssembly,"Stadium concourse ground",new Vector3(0,-.56f,-20),new Vector3(280,.1f,340),concourse.sharedMaterial);}
  var runoff=board.fieldAssembly.Find("Stadium concourse ground");
  var grass=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Stadium grass runoff.mat");if(grass==null){grass=new Material(board.match.ground.GetComponent<Renderer>().sharedMaterial);grass.name="Stadium grass runoff";AssetDatabase.CreateAsset(grass,"Assets/TruckFieldGoal/Materials/Stadium grass runoff.mat");}grass.mainTextureScale=new Vector2(22*280f/64,65*340f/FieldLength);EditorUtility.SetDirty(grass);
  runoff.localPosition=new Vector3(0,-.51f,-20);runoff.localScale=new Vector3(280,1,340);runoff.GetComponent<Renderer>().sharedMaterial=grass;var floor=runoff.GetComponent<BoxCollider>();if(floor==null)floor=runoff.gameObject.AddComponent<BoxCollider>();floor.sharedMaterial=board.match.ground.sharedMaterial;
  var groundSurfaces=new System.Collections.Generic.List<Collider>{floor};
  foreach(var r in board.match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.name=="Union Field End-zone concourse")){
   r.sharedMaterial=grass;var surface=r.GetComponent<MeshCollider>();if(surface==null)surface=r.gameObject.AddComponent<MeshCollider>();surface.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;surface.sharedMaterial=board.match.ground.sharedMaterial;groundSurfaces.Add(surface);EditorUtility.SetDirty(r);
  }
  board.runoff=groundSurfaces.ToArray();foreach(var surface in board.runoff){var weatherSurface=surface.GetComponent<LWS.TruckTaxi.TruckTaxiSurface>();if(weatherSurface==null)weatherSurface=surface.gameObject.AddComponent<LWS.TruckTaxi.TruckTaxiSurface>();weatherSurface.isRoad=false;EditorUtility.SetDirty(surface);}
  var wind=board.match.GetComponent<FieldGoalPresentation>();if(wind!=null){var pole=wind.sock.parent;pole.SetParent(board.match.posts,false);pole.localPosition=new Vector3(board.match.GoalHalfWidth+.4f,18,0);wind.sock.localPosition=new Vector3(0,1,0);var mast=pole.Find("Windsock pole");mast.localPosition=new Vector3(0,.5f,0);mast.localScale=new Vector3(mast.localScale.x,.5f,mast.localScale.z);EditorUtility.SetDirty(wind);}
  var screen=board.screen.GetComponent<RectTransform>();screen.anchoredPosition3D=new Vector3(0,33,12.35f);screen.localScale=Vector3.one*.04f;EditorUtility.SetDirty(screen);
  var housing=board.fieldAssembly.Find("Fabricated scoreboard housing");housing.localPosition=new Vector3(0,33,13.2f);housing.localScale=new Vector3(66.4f,28.4f,1.4f);
  foreach(int side in new[]{-1,1}){var edge=board.fieldAssembly.Find("Scoreboard gold edge "+side);edge.localPosition=new Vector3(side*32.6f,33,12.4f);edge.localScale=new Vector3(.22f,27.2f,.15f);var support=board.fieldAssembly.Find("Scoreboard steel support "+side);support.localPosition=new Vector3(side*26,10,13.7f);support.localScale=new Vector3(.72f,10,.72f);}
  foreach(var text in board.names){Size(text,42);text.fontSizeMin=26;text.textWrappingMode=TextWrappingModes.NoWrap;EditorUtility.SetDirty(text);}Size(board.title,50);Size(board.attempt,50);Size(board.current,46);Size(board.status,50);Size(board.weather,38);Size(board.controls,36);Size(board.record,36);
  foreach(var text in board.scores)Size(text,88);foreach(var text in board.kicks)Size(text,34);
  var forecast=board.match.GetComponent<FieldGoalPresentation>();
  if(forecast!=null&&forecast.windArrow!=null){forecast.windArrow.SetParent(screen,false);forecast.windArrow.anchorMin=forecast.windArrow.anchorMax=forecast.windArrow.pivot=new Vector2(.5f,.5f);forecast.windArrow.anchoredPosition=new Vector2(-716,-156);forecast.windArrow.sizeDelta=new Vector2(38,38);EditorUtility.SetDirty(forecast.windArrow);}
  EditorUtility.SetDirty(board);
 }
 static void Size(TMP_Text text,int size){text.fontSizeMax=size;text.fontSizeMin=size*.8f;text.fontSize=size;EditorUtility.SetDirty(text);}
 static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,Material mat){var g=SportsEventAuthoring.Box(name,Vector3.zero,size,mat);g.transform.SetParent(parent,false);g.transform.localPosition=position;foreach(var c in g.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);return g;}
 static UnityEngine.UI.Image Plate(Transform parent,string name,Vector2 pos,Vector2 size,Color color){var g=SportsUIAuthoring.Rect(name,parent,size,pos);var image=g.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;g.AddComponent<SportsBroadcastPlate>().cut=14;return image;}
 static TMP_Text Text(Transform parent,string name,string value,Vector2 pos,Vector2 size,int font){var label=SportsUIAuthoring.Text(name,parent,value,size,pos,font);var theme=Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");if(theme!=null)label.font=font>=40?theme.headingFont:theme.bodyFont;label.overflowMode=TextOverflowModes.Ellipsis;return label;}
}
}
