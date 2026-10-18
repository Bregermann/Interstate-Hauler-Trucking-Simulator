using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Heat;
namespace LWS.TruckSports.Editor {
public static class SportsParticipantAuthoring {
 [MenuItem("Truck Sports/Upgrade Shared Participants Menu")]
 public static void InstallFront(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scene edits before upgrading the front end.");
  var previous=EditorSceneManager.GetSceneManagerSetup();try{var scene=EditorSceneManager.OpenScene(SportsBuilder.FrontScene);Install(UnityEngine.Object.FindAnyObjectByType<SportsFrontEnd>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}
 }
 public static void Install(SportsFrontEnd front){
  var existing=front.GetComponent<SportsParticipantsMenu>();if(existing!=null){InstallChoices(existing);return;}
  var menu=front.gameObject.AddComponent<SportsParticipantsMenu>();menu.front=front;menu.page=front.pages.Length;
  var page=SportsUIAuthoring.Page("Participants",front.transform,"YOUR CREW","One identity. Every sport. Your names stay with you.");
  Array.Resize(ref front.pages,menu.page+1);front.pages[menu.page]=page;
  menu.heading=page.transform.Find("Title").GetComponent<TMP_Text>();
  menu.names=new TMP_InputField[4];menu.details=new TMP_Text[4];menu.profiles=new ButtonManager[4];menu.colors=new ButtonManager[4];menu.kinds=new ButtonManager[4];
  for(int i=0;i<4;i++){
   Vector2 pos=new Vector2(i%2==0?-350:350,i<2?130:-115);
   var card=SportsUIAuthoring.Rect("Driver card "+(i+1),page.transform,new Vector2(660,225),pos);card.AddComponent<Image>().color=new Color(.025f,.075f,.095f);card.AddComponent<SportsBroadcastPlate>().cut=20;
   menu.names[i]=SportsUIAuthoring.Name(card.transform,"DRIVER NAME","",new Vector2(-65,35));menu.names[i].characterLimit=40;menu.names[i].GetComponent<RectTransform>().sizeDelta=new Vector2(500,54);
   menu.details[i]=SportsUIAuthoring.Text("Identity",card.transform,"",new Vector2(600,48),new Vector2(0,-17),18);
   menu.profiles[i]=SportsUIAuthoring.Button(card.transform,"CHOOSE DRIVER","peopleprofile"+i,new Vector2(-200,-66),new Vector2(220,46));
   menu.colors[i]=SportsUIAuthoring.Button(card.transform,"COLOR","peoplecolor"+i,new Vector2(-20,-66),new Vector2(120,46));
   menu.kinds[i]=SportsUIAuthoring.Button(card.transform,"AI PREFERENCE","peoplekind"+i,new Vector2(175,-66),new Vector2(245,46));
   SportsUIAuthoring.Button(card.transform,"NEW","peoplenew"+i,new Vector2(255,35),new Vector2(100,54));
   foreach(var label in card.GetComponentsInChildren<TMP_Text>(true)){if(label.transform.parent.GetComponent<ButtonManager>()!=null){label.fontSize=18;label.fontSizeMax=18;label.fontSizeMin=15;}label.richText=false;}
  }
  menu.notice=SportsUIAuthoring.Text("Help",page.transform,"",new Vector2(1280,96),new Vector2(0,-300),20);
  SportsUIAuthoring.Button(page.transform,"PREVIOUS 4","peopleprev",new Vector2(-450,-385),new Vector2(300,54));SportsUIAuthoring.Button(page.transform,"BACK","peopleback",new Vector2(0,-385),new Vector2(300,54));SportsUIAuthoring.Button(page.transform,"NEXT 4","peoplenext",new Vector2(450,-385),new Vector2(300,54));
  // Compact entry in the title page, leaving the established Play/Settings rows intact.
  SportsUIAuthoring.Button(front.pages[0].transform,"YOUR CREW","participants",new Vector2(525,-385),new Vector2(300,54));
  foreach(var image in page.GetComponentsInChildren<Image>(true))if(image.GetComponent<SportsBroadcastPlate>()==null)image.gameObject.AddComponent<SportsBroadcastPlate>().cut=12;
  page.SetActive(false);InstallChoices(menu);EditorUtility.SetDirty(front);EditorUtility.SetDirty(menu);
 }
 static void InstallChoices(SportsParticipantsMenu menu){
  if(menu.choices!=null&&menu.choices.Length==12)return;var front=menu.front;menu.choicePage=front.pages.Length;Array.Resize(ref front.pages,menu.choicePage+1);
  var page=SportsUIAuthoring.Page("Choose Driver",front.transform,"CHOOSE DRIVER","Saved identities, including names preserved from older sport setups.");front.pages[menu.choicePage]=page;menu.choiceHeading=page.transform.Find("Title").GetComponent<TMP_Text>();menu.choices=new ButtonManager[12];
  for(int i=0;i<12;i++){menu.choices[i]=SportsUIAuthoring.Button(page.transform,"DRIVER","peoplepick"+i,new Vector2(-510+(i%4)*340,150-(i/4)*140),new Vector2(315,110));var text=menu.choices[i].normalTextObj;text.fontSize=22;text.fontSizeMax=22;text.fontSizeMin=16;text.richText=false;menu.choices[i].gameObject.AddComponent<SportsBroadcastPlate>().cut=16;}
  SportsUIAuthoring.Text("Choice help",page.transform,"Drivers keep their own ID even when two names match. Selecting an occupied driver swaps seats.",new Vector2(1300,70),new Vector2(0,-285),22);
  SportsUIAuthoring.Button(page.transform,"PREVIOUS","peoplechooseprev",new Vector2(-450,-385),new Vector2(300,54));SportsUIAuthoring.Button(page.transform,"BACK","peoplechooseback",new Vector2(0,-385),new Vector2(300,54));SportsUIAuthoring.Button(page.transform,"NEXT","peoplechoosenext",new Vector2(450,-385),new Vector2(300,54));page.SetActive(false);EditorUtility.SetDirty(menu);EditorUtility.SetDirty(front);
 }
}
}
