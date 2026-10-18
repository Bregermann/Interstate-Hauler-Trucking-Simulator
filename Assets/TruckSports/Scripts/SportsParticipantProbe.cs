#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckBowling;
namespace LWS.TruckSports {
public sealed class SportsParticipantProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){
  var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-tsParticipants")<0)return;
  var root=new GameObject("Explicit participant acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<SportsParticipantProbe>();
 }
 string folder,mode;int checks,errors,scenes;bool done;IDisposable lease;readonly StringBuilder log=new StringBuilder();
 [Serializable]sealed class SavedIdentity {public string[] ids;public string name;}
 void Awake(){lease=SportsPrefs.IsolateValidation("IdentityWindows");SportsParticipantRegistry.ForgetCache();folder=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"..":"../../..","Logs/TruckSports/Identity/Windows"));Directory.CreateDirectory(folder);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsParticipants");mode=at+1<args.Length?args[at+1]:"matrix";Application.logMessageReceived+=Error;log.AppendLine("PARTICIPANT_RUN mode="+mode+" PID="+System.Diagnostics.Process.GetCurrentProcess().Id+" editor="+Application.isEditor);File.WriteAllText(folder+"/result.txt","RUNNING");}
 void Error(string text,string trace,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;log.AppendLine("ERROR "+text);}
 void Check(bool value,string text){checks++;if(!value)errors++;log.AppendLine((value?"PASS ":"FAIL ")+text);File.WriteAllText(folder+"/progress.txt",log.ToString());}
 ISportsParticipantSlot[] Slots(out Action save){
  var s=FindAnyObjectByType<SportsEventSession>();if(s!=null){save=s.Save;return s.setup.players;}
  var bowl=FindAnyObjectByType<BowlingMatch>();if(bowl!=null){save=bowl.Save;return bowl.setup.players;}
  var corn=FindAnyObjectByType<LWS.TruckCornhole.CornholeMatch>();if(corn!=null){save=corn.Save;return corn.setup.players;}
  var cheese=FindAnyObjectByType<LWS.TruckCheeseRolling.CheeseMatch>();if(cheese!=null){save=cheese.Save;return cheese.setup.players;}
  var bill=FindAnyObjectByType<LWS.TruckBilliards.BilliardsSession>();if(bill!=null){save=bill.SaveSetup;return bill.players;}
  var ice=FindAnyObjectByType<LWS.TruckHockey.HockeyMatch>();if(ice!=null){save=ice.SaveSetup;return ice.players;}
  var air=FindAnyObjectByType<LWS.TruckAirHockey.AirHockeyMatch>();if(air!=null){save=air.Save;return air.players;}
  var wall=FindAnyObjectByType<LWS.TruckWallball.WallballMatch>();if(wall!=null){save=wall.Save;return wall.players;}
  save=null;return null;
 }
 IEnumerator Start(){var run=Run();while(true){bool more=false;object next=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){Check(false,e.ToString());Finish();more=false;}if(!more)yield break;yield return next;}}
 IEnumerator Run(){
  if(mode=="write"){
   var p=SportsParticipantRegistry.Default(0);SportsParticipantRegistry.Rename(p.id,"Big Daddy",Color.magenta,false);var q=SportsParticipantRegistry.Default(1);SportsParticipantRegistry.Rename(q.id,"Big Daddy",Color.yellow,true);
   var snapshot=new SavedIdentity{ids=Enumerable.Range(0,12).Select(i=>SportsParticipantRegistry.Default(i).id).ToArray(),name="Big Daddy"};File.WriteAllText(folder+"/restart.json",JsonUtility.ToJson(snapshot));Check(snapshot.ids.Distinct().Count()==12,"twelve unique identities saved for a second process");Finish();yield break;
  }
  if(mode=="read"){
   var snapshot=JsonUtility.FromJson<SavedIdentity>(File.ReadAllText(folder+"/restart.json"));for(int i=0;i<12;i++)Check(SportsParticipantRegistry.Default(i).id==snapshot.ids[i],"actual executable restart retains ID "+i);Check(SportsParticipantRegistry.Default(0).displayName==snapshot.name&&SportsParticipantRegistry.Default(1).displayName==snapshot.name,"duplicate names survive actual restart");Finish();yield break;
  }
  int fixtures=0;try{fixtures=SportsParticipantChecks.Run();}catch(Exception e){Check(false,e.ToString());}Check(fixtures>250,"isolated migration/ownership/result/recovery fixtures "+fixtures);
  var first=SportsParticipantRegistry.Default(0);SportsParticipantRegistry.Rename(first.id,"Alexandria Montgomery Smythe",Color.magenta,false);var second=SportsParticipantRegistry.Default(1);SportsParticipantRegistry.Rename(second.id,first.displayName,Color.yellow,true);
  var ids=Enumerable.Range(0,12).Select(i=>SportsParticipantRegistry.Default(i).id).ToArray();
  for(int index=0;index<SceneManager.sceneCountInBuildSettings;index++){
   string path=SceneUtility.GetScenePathByBuildIndex(index);yield return SceneManager.LoadSceneAsync(path);yield return null;yield return null;
   var slots=Slots(out var save);if(slots==null)continue;scenes++;var source=slots.Select(p=>p.IsBot).ToArray();
   for(int i=0;i<slots.Length;i++){Check(slots[i].ParticipantId==SportsParticipantRegistry.Default(i).id,path+" identity "+i);Check(!string.IsNullOrEmpty(slots[i].DisplayName),path+" name "+i);}
   Check(slots[0].DisplayName==SportsParticipantRegistry.Default(0).displayName&&slots[1].DisplayName==SportsParticipantRegistry.Default(1).displayName,path+" shared names and independent renames");
   save();Check(slots.Select(p=>p.IsBot).SequenceEqual(source),path+" saving identity preserves sources");
   // Real menu callback, without claiming physical keyboard/controller use.
   if(path.Contains("TruckKeepaway.unity")){
    var session=FindAnyObjectByType<SportsEventSession>();string other=slots[1].DisplayName;session.menus.names[0].onEndEdit.Invoke("Big Daddy");Check(SportsParticipantRegistry.Default(0).displayName=="Big Daddy","Keepaway real rename callback updates the registry");Check(slots[1].DisplayName==other,"renaming one duplicate name does not rename a different identity");first=SportsParticipantRegistry.Default(0);
   }
  }
  Check(scenes==SceneManager.sceneCountInBuildSettings-1,"every registered sport scene has a participant binding: "+scenes);
  yield return SceneManager.LoadSceneAsync("TruckSports");yield return null;yield return null;var front=FindAnyObjectByType<SportsFrontEnd>();Check(front!=null,"collection front end loaded");var menu=front.GetComponent<SportsParticipantsMenu>();Check(menu!=null,"saved crew page integrated");
  foreach(var entry in front.pages[0].GetComponentsInChildren<SportsMenuAction>())if(entry.action=="participants")entry.button.onClick.Invoke();yield return new WaitForSecondsRealtime(.25f);
  Check(menu.names[0].text=="Big Daddy","crew reflects sport rename");menu.names[0].onEndEdit.Invoke("Alexandria Montgomery Smythe");Check(SportsParticipantRegistry.Default(0).displayName=="Alexandria Montgomery Smythe","crew name callback updates shared identity");
  string old=SportsParticipantRegistry.Default(0).id;menu.profiles[0].onClick.Invoke();Check(front.Page==menu.choicePage,"real Heat button callback opens identity selector");
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)}){Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);Check(Screen.width==size.x&&Screen.height==size.y,"actual selector viewport "+size);Canvas.ForceUpdateCanvases();foreach(var choice in menu.choices)if(choice.gameObject.activeInHierarchy)Check(!choice.normalTextObj.isTextOverflowing,"selector name/identity fits "+size);ScreenCapture.CaptureScreenshot(folder+"/choose-driver-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.15f);}
  menu.choices[1].onClick.Invoke();Check(SportsParticipantRegistry.Default(0).id!=old,"real Heat selection callback changes global roster");SportsParticipantRegistry.UseInSeat(0,old);menu.Refresh();
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1024,768),new Vector2Int(1920,800)}){
   Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.4f);Check(Screen.width==size.x&&Screen.height==size.y,"actual crew viewport "+size);Canvas.ForceUpdateCanvases();
   foreach(var text in menu.GetComponentsInChildren<TMPro.TMP_Text>())if(text.isActiveAndEnabled)Check(!text.isTextOverflowing,"crew text fits "+text.name+" "+size);
   ScreenCapture.CaptureScreenshot(folder+"/crew-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.15f);
  }
  Check(ids.SequenceEqual(Enumerable.Range(0,12).Select(i=>SportsParticipantRegistry.Default(i).id)),"full collection tour preserves original stable identities");Finish();
 }
 void Finish(){if(done)return;done=true;log.AppendLine("SCENES="+scenes+" CHECKS="+checks);log.AppendLine("ERRORS="+errors);log.AppendLine(errors==0?"PASS":"FAIL");File.WriteAllText(folder+"/result.txt",log.ToString());Debug.Log("TS_PARTICIPANT_FINISHED mode="+mode+" errors="+errors);
#if UNITY_EDITOR
  if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
  Application.Quit(errors==0?0:2);
 }
 void OnDestroy(){Application.logMessageReceived-=Error;lease?.Dispose();SportsParticipantRegistry.ForgetCache();}
}
}
#endif
