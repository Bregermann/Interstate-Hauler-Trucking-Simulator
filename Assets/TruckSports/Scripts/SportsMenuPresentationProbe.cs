#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace LWS.TruckSports {
public sealed class SportsMenuPresentationProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){bool run=Array.IndexOf(Environment.GetCommandLineArgs(),"-tsRetroMenu")>=0;
#if UNITY_EDITOR
 run|=UnityEditor.SessionState.GetBool("Sports.MenuPresentation.Probe",false);UnityEditor.SessionState.SetBool("Sports.MenuPresentation.Probe",false);
#endif
 if(!run)return;var root=new GameObject("Explicit retro logo showcase acceptance");DontDestroyOnLoad(root);root.AddComponent<SportsProbeInputIsolation>();root.AddComponent<SportsMenuPresentationProbe>();}
 const string Folder="Logs/TruckSports/PresentationSprint/Menu";IDisposable preferences;InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
 InputSettings.EditorInputBehaviorInPlayMode previousEditor;
#endif
 Keyboard keyboard;Gamepad pad;int checks,errors;readonly StringBuilder report=new StringBuilder();
 void Awake(){preferences=SportsPrefs.IsolateValidation("RetroMenu");SportsParticipantRegistry.ForgetCache();previousBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
 previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
 }
 void Check(bool ok,string text){checks++;if(!ok)errors++;report.AppendLine((ok?"PASS ":"FAIL ")+text);File.WriteAllText(Folder+"/progress.txt",report.ToString());}
 void Error(string text,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;report.AppendLine("ERROR "+text+"\n"+stack);}
 IEnumerator Start(){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/result.txt","RUNNING "+DateTime.UtcNow.ToString("o"));Application.logMessageReceived+=Error;keyboard=InputSystem.AddDevice<Keyboard>("Injected menu keyboard");pad=InputSystem.AddDevice<Gamepad>("Injected menu pad");yield return SportsProbeScene.Ensure<SportsFrontEnd>("TruckSports");yield return null;var front=FindAnyObjectByType<SportsFrontEnd>();yield return SportsWindowCapture.Save(1280,720,Folder+"/title-720.png");front.Action("play");yield return new WaitForSecondsRealtime(.4f);
  var choices=front.GetComponentsInChildren<SportsCatalogueChoice>(true);var scenes=typeof(SportsNavigation).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.IsLiteral&&f.FieldType==typeof(string)&&f.Name!="Front").Select(f=>(string)f.GetRawConstantValue()).Where(s=>s.StartsWith("Truck")).Distinct().ToArray();
  Check(scenes.Length>=41&&choices.Select(c=>c.scene).Distinct().Count()==scenes.Length&&scenes.All(s=>choices.Any(c=>c.scene==s)),"all registered sport scenes have a real browse choice, including cab variants and Scrap Heap");
  foreach(var choice in choices)Check(choice.logo!=null&&choice.logo.name==choice.scene&&choice.preview!=null&&choice.GetComponent<SportsMenuAction>()!=null,"original logo, gameplay preview and existing launch action: "+choice.scene);
  var showcase=front.GetComponentInChildren<SportsCatalogueHero>();foreach(var choice in choices){showcase.Select(choice,true);Canvas.ForceUpdateCanvases();showcase.title.ForceMeshUpdate();showcase.description.ForceMeshUpdate();Check(!showcase.title.isTextTruncated&&!showcase.description.isTextTruncated,"selected title, description and participant information fit: "+choice.scene);}
  front.Action("participants");yield return null;var crew=front.GetComponent<SportsParticipantsMenu>();Check(front.Page==crew.page&&crew.names[0].isActiveAndEnabled,"Your Crew opens from sport selection");crew.names[0].onEndEdit.Invoke("Arcade Captain");front.Action("peoplecolor0");string crewId=SportsParticipantRegistry.Default(0).id;Color crewColor=SportsParticipantRegistry.Default(0).color;front.Action("peopleback");Check(front.Page==1&&SportsParticipantRegistry.Default(0).displayName=="Arcade Captain","Crew edits persist and Back returns to sport selection");
  SportsParticipantRegistry.ForgetCache();Check(SportsParticipantRegistry.Default(0).id==crewId&&SportsParticipantRegistry.Default(0).color==crewColor&&SportsParticipantRegistry.Default(0).displayName=="Arcade Captain","Crew identity/name/color survive an actual shared-preference reload");
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(2560,1080)}){
   front.catalogue.Step(-front.catalogue.Pages);front.Focus();yield return new WaitForSecondsRealtime(.3f);yield return SportsWindowCapture.Save(size.x,size.y,Folder+"/showcase-"+size.x+"x"+size.y+".png");
   var hero=front.GetComponentInChildren<SportsCatalogueHero>();Check(hero!=null&&hero.logo.sprite!=null&&hero.preview.sprite!=null,"selected artwork and screenshot render at actual "+Screen.width+"x"+Screen.height);
   var canvas=front.GetComponent<Canvas>();foreach(var rect in new[]{hero.logo.rectTransform,hero.preview.rectTransform,hero.description.rectTransform})Check(Inside(rect,canvas),rect.name+" is screen-safe at "+Screen.width+"x"+Screen.height);
  }
  for(int page=0;page<front.catalogue.Pages;page++){
   front.catalogue.Step(page==0?-front.catalogue.Pages:1);front.Focus();yield return new WaitForSecondsRealtime(.3f);yield return SportsWindowCapture.Save(1280,720,Folder+"/page-"+page+"-720.png");var focused=EventSystem.current.currentSelectedGameObject?.GetComponent<SportsCatalogueChoice>();Check(focused!=null&&focused.gameObject.activeInHierarchy&&front.GetComponentInChildren<SportsCatalogueHero>().SelectedScene==focused.scene,"page navigation selects a visible sport and refreshes the corresponding showcase: "+page);
   foreach(var card in front.catalogue.cards.Where(c=>c.activeInHierarchy)){var image=card.transform.Find("Original sport logo")?.GetComponent<Image>();Check(image!=null&&image.sprite!=null&&Inside(card.GetComponent<RectTransform>(),front.GetComponent<Canvas>()),card.name+" displays its uncropped logo within the screen");}
  }
  front.catalogue.Step(-front.catalogue.Pages);front.Focus();yield return new WaitForSecondsRealtime(.2f);var selected=EventSystem.current.currentSelectedGameObject;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow));yield return new WaitForSecondsRealtime(.12f);report.AppendLine("MENU_KEY_EVENT enabled="+keyboard.enabled+" focused="+Application.isFocused+" held="+keyboard[Key.DownArrow].isPressed+" selected="+EventSystem.current.currentSelectedGameObject);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.15f);Check(EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject!=selected,"injected keyboard moves real menu focus");
  selected=EventSystem.current.currentSelectedGameObject;InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Vector2.down});yield return new WaitForSecondsRealtime(.15f);report.AppendLine("MENU_PAD_EVENT enabled="+pad.enabled+" focused="+Application.isFocused+" stick="+pad.leftStick.ReadValue()+" selected="+EventSystem.current.currentSelectedGameObject);InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(.2f);Check(EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject!=selected,"injected gamepad moves real menu focus; not physical hardware acceptance");
  var launches=choices.GroupBy(c=>c.scene).Select(g=>new[]{g.Key,g.First().GetComponent<SportsMenuAction>().action}).ToArray();
  foreach(var launch in launches){front.Action(launch[1]);yield return null;yield return null;yield return new WaitForSecondsRealtime(.2f);Check(SceneManager.GetActiveScene().name==launch[0]&&FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"existing action opens correct saved setup with one listener: "+launch[0]);var artwork=FindObjectsByType<Image>(FindObjectsInactive.Include).Where(i=>i.name=="Original sport title artwork").ToArray();Check(artwork.Length>0&&artwork.All(i=>i.sprite!=null&&i.sprite.name==launch[0]),"sport setup/info uses its own imported logo: "+launch[0]);SportsNavigation.FrontEnd();yield return null;yield return null;yield return new WaitForSecondsRealtime(.2f);front=FindAnyObjectByType<SportsFrontEnd>();Check(front!=null,"returns to collection after "+launch[0]);front.Action("play");}
  report.AppendLine("CHECKS="+checks+" ERRORS="+errors+"; platform="+Application.platform+"; capture kind="+SportsWindowCapture.EvidenceKind+"; injected keyboard/gamepad, actual saved-scene launch/return. No full-collection gameplay, physical-device, audio-listening or human claim.");File.WriteAllText(Folder+"/result.txt",report.ToString());Application.logMessageReceived-=Error;
#if UNITY_EDITOR
 if(Application.isEditor)UnityEditor.EditorApplication.isPlaying=false;else
#endif
 Application.Quit(errors==0?0:2);
 }
 static bool Inside(RectTransform rect,Canvas canvas){var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,corner);if(p.x<-.5f||p.y<-.5f||p.x>Screen.width+.5f||p.y>Screen.height+.5f)return false;}return true;}
 void OnDestroy(){InputSystem.settings.backgroundBehavior=previousBackground;
#if UNITY_EDITOR
 InputSystem.settings.editorInputBehaviorInPlayMode=previousEditor;
#endif
 Application.logMessageReceived-=Error;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);preferences?.Dispose();SportsParticipantRegistry.ForgetCache();}
}
}
#endif
