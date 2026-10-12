using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace LWS.TruckBilliards
{
 public sealed partial class BilliardsSmoke:MonoBehaviour
 {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  BilliardsMatch match;BilliardsSession session;BilliardsMenus menus;IEnumerator routine;
  InputSettings originalInputSettings,probeInputSettings;Keyboard keyboard;Mouse mouse;Gamepad pad1,pad2,pad3;readonly List<string> results=new List<string>();
  string folder;bool finished;float oldMaster,oldSfx,oldSensitivity;bool oldFullscreen,oldVibration;BilliardsDifficulty oldDifficulty;int oldResolution;
  readonly Dictionary<string,string> oldPrefs=new Dictionary<string,string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Launch(){var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-tbFeatureSmoke")>=0||Array.IndexOf(args,"-tbLocalSmoke")>=0||Array.IndexOf(args,"-tbSmoke")>=0||Array.IndexOf(args,"-tbHandoffProbe")>=0){var m=UnityEngine.Object.FindAnyObjectByType<BilliardsMatch>();if(m!=null)m.gameObject.AddComponent<BilliardsSmoke>();}}
  void Start(){
   match=GetComponent<BilliardsMatch>();session=match.session;menus=session.menus;
   folder=Path.GetFullPath(Path.Combine(Application.dataPath,Array.IndexOf(Environment.GetCommandLineArgs(),"-tbFeatureSmoke")>=0?"../FeatureSmokeTest":"../LocalSmokeTest"));Directory.CreateDirectory(folder);
   // Keep opt-in injected input tests stable when desktop focus moves to the
   // report window. Clone settings; never edit the project's settings asset.
   originalInputSettings=InputSystem.settings;probeInputSettings=UnityEngine.Object.Instantiate(originalInputSettings);
   probeInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=probeInputSettings;
   Application.logMessageReceived+=Log;routine=Array.IndexOf(Environment.GetCommandLineArgs(),"-tbFeatureSmoke")>=0?RunExpansion():Array.IndexOf(Environment.GetCommandLineArgs(),"-tbHandoffProbe")>=0?HandoffOnly():Run();
  }
  void OnDestroy(){Application.logMessageReceived-=Log;}
  void Log(string text,string trace,LogType type){if(!finished&&(type==LogType.Error||type==LogType.Exception))results.Add("RUNTIME ERROR "+text);}
  void Update(){
   if(finished||routine==null)return;
   try{if(!routine.MoveNext())Finish(0);}catch(Exception e){results.Add("FAIL "+e.Message);Debug.LogError("TB_LOCAL_FAIL "+e);Finish(2);}
  }
  void Finish(int code){
   finished=true;File.WriteAllLines(Path.Combine(folder,"results.txt"),results);Debug.Log("TB_LOCAL_FINISHED code="+code);
   session.MainMenu();if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(pad1!=null)InputSystem.RemoveDevice(pad1);if(pad2!=null)InputSystem.RemoveDevice(pad2);if(pad3!=null)InputSystem.RemoveDevice(pad3);
   if(oldPrefs.Count>0)RestoreSettings();
   if(originalInputSettings!=null)InputSystem.settings=originalInputSettings;if(probeInputSettings!=null)UnityEngine.Object.Destroy(probeInputSettings);
   Time.timeScale=1;
#if UNITY_EDITOR
   UnityEditor.EditorApplication.Exit(code);
#else
   Application.Quit(code);
#endif
  }
  void Check(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS "+name);Debug.Log("TB_LOCAL_PASS "+name);}
  IEnumerator Wait(Func<bool> test,float seconds,string message){float until=Time.realtimeSinceStartup+seconds;while(!test()){if(Time.realtimeSinceStartup>until)throw new Exception("Timeout "+message);yield return null;}}
  IEnumerator Delay(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)yield return null;}
  void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
  void Pad(Gamepad pad,float gas=0,float turn=0,GamepadButton? button=null){var state=new GamepadState{rightTrigger=gas,leftStick=new Vector2(turn,0)};if(button.HasValue)state=state.WithButton(button.Value);InputSystem.QueueStateEvent(pad,state);}
  void Click(string key){
   var page=menus.pages[(int)session.Screen];
   foreach(var action in page.GetComponentsInChildren<BilliardsMenuAction>())if(action.action==key){action.button.onClick.Invoke();return;}
   throw new Exception("No active menu action "+key);
  }
  void Setup(BilliardsMode mode,int breaker=0){session.MainMenu();session.SelectMode(mode);session.breakChoice=breaker;session.StartMatch();}
  void PocketShot(int target){
   foreach(var b in match.balls)if(!b.Pocketed)b.Restore(b.initial);
   match.balls[0].Restore(new Vector3(-3.3f,.5f,0));match.balls[target].Restore(new Vector3(-4.4f,.5f,0));
   Physics.SyncTransforms();if(!match.rules.BeginShot())throw new Exception("Fixture shot not permitted");
   match.balls[0].body.linearVelocity=new Vector3(-4,0,0);
  }
  void ScratchShot(){
   foreach(var b in match.balls)if(!b.Pocketed)b.Restore(b.initial);
   match.balls[0].Restore(new Vector3(-4.8f,.5f,0));Physics.SyncTransforms();
   if(!match.rules.BeginShot())throw new Exception("Scratch fixture shot not permitted");match.balls[0].body.linearVelocity=new Vector3(-2,0,0);
  }
  void Capture(string name){ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));}
  void SaveSettingsBaseline(){
   oldVibration=session.settings.vibration;oldDifficulty=session.difficulty;oldMaster=session.settings.master;oldSfx=session.settings.sfx;oldSensitivity=session.settings.sensitivity;oldFullscreen=session.settings.fullscreen;oldResolution=session.settings.resolutionIndex;
   foreach(var key in new[]{"Master","SFX","Steering","Fullscreen","Width","Height","Vibration","Difficulty"}){
    string full="TruckBilliards.Settings."+key;
    oldPrefs[full]=!PlayerPrefs.HasKey(full)?null:(key=="Master"||key=="SFX"||key=="Steering")?"F:"+PlayerPrefs.GetFloat(full):"I:"+PlayerPrefs.GetInt(full);
   }
  }
  void RestoreSettings(){
   foreach(var pair in oldPrefs){if(pair.Value==null)PlayerPrefs.DeleteKey(pair.Key);else if(pair.Value.StartsWith("F:"))PlayerPrefs.SetFloat(pair.Key,float.Parse(pair.Value.Substring(2)));else PlayerPrefs.SetInt(pair.Key,int.Parse(pair.Value.Substring(2)));}
   PlayerPrefs.Save();session.settings.vibration=oldVibration;session.difficulty=oldDifficulty;session.settings.master=oldMaster;session.settings.sfx=oldSfx;session.settings.sensitivity=oldSensitivity;session.settings.fullscreen=oldFullscreen;session.settings.resolutionIndex=oldResolution;session.settings.ApplyAudio();
  }
  IEnumerator HandoffOnly(){
   IEnumerator w=Delay(1);while(w.MoveNext())yield return null;
   pad1=InputSystem.AddDevice<Gamepad>("TruckBilliards injected handoff pad");
   session.MainMenu();session.SelectMode(BilliardsMode.HotSeat);session.hotSeatGamepad=true;session.Assign(0,pad1);session.breakChoice=0;session.StartMatch();Pad(pad1);
   w=Delay(.4f);while(w.MoveNext())yield return null;Pad(pad1,button:GamepadButton.South);ScratchShot();
   w=Wait(()=>session.Screen==BilliardsScreen.Handoff,6,"focused scratch handoff");while(w.MoveNext())yield return null;
   session.Pause();Click("settings");InputSystem.RemoveDevice(pad1);w=Delay(.2f);while(w.MoveNext())yield return null;
   InputSystem.AddDevice(pad1);Click("resume");Pad(pad1);w=Delay(.25f);while(w.MoveNext())yield return null;
   results.Add("PROBE BEFORE screen="+session.Screen+" turn="+match.rules.Turn+" owner="+session.sharedOwner+" ready="+session.input.HandoffReady+" south="+pad1.buttonSouth.isPressed+" rawSouth="+pad1.buttonSouth.ReadValue()+" padEnabled="+pad1.enabled);
   Pad(pad1,button:GamepadButton.South);w=Delay(.25f);while(w.MoveNext())yield return null;
   results.Add("PROBE AFTER screen="+session.Screen+" turn="+match.rules.Turn+" owner="+session.sharedOwner+" ready="+session.input.HandoffReady+" control="+session.players[1].control+" sharedPad="+session.SharedPad.deviceId+" pad1="+pad1.deviceId+" added="+pad1.added+" enabled="+pad1.enabled+" south="+pad1.buttonSouth.isPressed+" rawSouth="+pad1.buttonSouth.ReadValue());
   Capture("probe-controller-handoff");w=Delay(.3f);while(w.MoveNext())yield return null;
   Check(session.GameplayEnabled&&session.sharedOwner==1,"focused reconnect handoff ready");
  }
  IEnumerator Run(){
   IEnumerator w=Delay(1);while(w.MoveNext())yield return null;
   results.Add("HARDWARE gamepads observed before injection: "+Gamepad.all.Count);
   Check(session.Screen==BilliardsScreen.Main&&EventSystem.current.currentSelectedGameObject!=null,"main menu and sensible initial focus");
   Capture("01-main");w=Delay(.3f);while(w.MoveNext())yield return null;
   keyboard=InputSystem.AddDevice<Keyboard>("TruckBilliards injected keyboard");
   pad1=InputSystem.AddDevice<Gamepad>("TruckBilliards injected pad 1");pad2=InputSystem.AddDevice<Gamepad>("TruckBilliards injected pad 2");
   Keys(Key.DownArrow);w=Delay(.18f);while(w.MoveNext())yield return null;Keys();
   Check(EventSystem.current.currentSelectedGameObject.name=="practice Button","injected keyboard menu navigation");w=Delay(.2f);while(w.MoveNext())yield return null;
   Pad(pad1,button:GamepadButton.DpadDown);w=Delay(.18f);while(w.MoveNext())yield return null;Pad(pad1);
   Check(EventSystem.current.currentSelectedGameObject.name=="help Button","controller navigation reaches help");w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad1,button:GamepadButton.DpadDown);w=Delay(.18f);while(w.MoveNext())yield return null;Pad(pad1);
   Check(EventSystem.current.currentSelectedGameObject.name=="settings Button","injected controller menu navigation (selected "+EventSystem.current.currentSelectedGameObject.name+")");
   Pad(pad1,button:GamepadButton.South);w=Delay(.18f);while(w.MoveNext())yield return null;Pad(pad1);
   Check(session.Screen==BilliardsScreen.Settings,"controller submit activates Heat settings button");
   SaveSettingsBaseline();menus.masterSlider.value=30;menus.sfxSlider.value=25;menus.sensitivitySlider.value=1.4f;
   Check(Mathf.Approximately(AudioListener.volume,.3f)&&Mathf.Approximately(match.audioSource.volume,.25f),"working master and SFX volume");
   // Persist settings without changing the command-line test resolution.
   int originalIndex=session.settings.resolutionIndex;session.settings.resolutionIndex=Array.FindIndex(session.settings.resolutions,r=>r.x==Screen.width&&r.y==Screen.height);
   if(session.settings.resolutionIndex<0)session.settings.resolutionIndex=originalIndex;
   session.settings.Save();Check(Mathf.Approximately(PlayerPrefs.GetFloat("TruckBilliards.Settings.Steering"),1.4f),"settings persist with mode-owned keys");RestoreSettings();
   Capture("02-settings");w=Delay(.3f);while(w.MoveNext())yield return null;menus.Back();
   Click("play");session.players[0].name="Alex";session.players[1].name="Morgan";session.SelectMode(BilliardsMode.SharedKeyboard);session.breakChoice=1;menus.RefreshSetup();
   Capture("03-setup");w=Delay(.3f);while(w.MoveNext())yield return null;
   Keys(Key.Enter);session.StartMatch();w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(match.rules.Turn==1&&!match.opponent.enabled&&match.rules.ShotId==0&&match.ai.transform.position==new Vector3(0,0,-8.3f),"Player 2 breaker and held Enter consumed");
   Keys();w=Delay(.2f);while(w.MoveNext())yield return null;
   Vector3 p1=match.player.transform.position,p2=match.ai.transform.position;
   Keys(Key.W,Key.UpArrow);w=Delay(.3f);while(w.MoveNext())yield return null;Keys(Key.Space,Key.RightShift);w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(Vector3.Distance(p1,match.player.transform.position)>.03f&&Vector3.Distance(p2,match.ai.transform.position)>.03f,"both shared-keyboard trucks move independently");
   Keys();w=Delay(.2f);while(w.MoveNext())yield return null;Keys(Key.UpArrow);
   w=Wait(()=>match.strikeCount==1,6,"Player 2 physical bumper break");while(w.MoveNext())yield return null;Keys();
   Check(match.rules.Turn==1&&match.rules.Phase==MatchPhase.Resolving,"human Player 2 uses common physical strike path");
   session.Pause();var frozen=match.balls[0].body.position;int id=match.rules.ShotId;
   w=Delay(.3f);while(w.MoveNext())yield return null;Check(match.balls[0].body.position==frozen&&match.rules.ShotId==id,"pause freezes physics without resetting shot");
   Click("help");Click("back");Check(session.Screen==BilliardsScreen.Pause,"pause Controls returns to paused state");Click("resume");
   w=Wait(()=>match.rules.Phase!=MatchPhase.Resolving,55,"break settles");while(w.MoveNext())yield return null;
   Check(match.rules.FirstContact>0,"real cue/object first contact");
   // Physical pot: motion and actual collision/pocket loop, no injected pocket records.
   Setup(BilliardsMode.SharedKeyboard);w=Delay(1);while(w.MoveNext())yield return null;PocketShot(1);
   w=Wait(()=>match.rules.Phase!=MatchPhase.Resolving,6,"physical object pot");while(w.MoveNext())yield return null;
   Check(match.balls[1].Pocketed&&match.rules.Pocketed.Contains(1)&&match.rules.PlayerGroup==1&&match.rules.Turn==0,"physical object capture assigns group and legally retains turn");
   int count=match.resolutionCount;w=Delay(.3f);while(w.MoveNext())yield return null;Check(match.resolutionCount==count,"physical pocket resolves once");
   ScratchShot();w=Wait(()=>match.rules.Phase==MatchPhase.Placement,6,"physical cue scratch");while(w.MoveNext())yield return null;
   Check(match.balls[0].Pocketed&&match.rules.Turn==1&&match.preview.gameObject.activeSelf,"physical cue capture opens incoming Player 2 placement");
   Keys();w=Delay(.2f);while(w.MoveNext())yield return null;var preview=match.placementPosition;
   Keys(Key.W,Key.F);w=Delay(.2f);while(w.MoveNext())yield return null;Check(match.rules.Phase==MatchPhase.Placement&&match.placementPosition==preview,"other keyboard player cannot move or confirm placement");
   Keys(Key.LeftArrow);w=Delay(.25f);while(w.MoveNext())yield return null;Keys();w=Delay(.15f);while(w.MoveNext())yield return null;
   Check(Vector3.Distance(preview,match.placementPosition)>.2f,"incoming player's keyboard placement (delta="+Vector3.Distance(preview,match.placementPosition)+" / focus="+Application.isFocused+" / keyboardEnabled="+keyboard.enabled+")");
   Keys(Key.RightCtrl,Key.UpArrow);w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(match.rules.Phase==MatchPhase.Aiming&&!match.balls[0].Pocketed,"Player 2 placement confirmation restores cue");
   var held=match.ai.transform.position;w=Delay(.3f);while(w.MoveNext())yield return null;Check(match.ai.transform.position==held,"held placement input cannot leak into driving");Keys();
   // Mouse placement follows the actual ray/plane input path for Player 1.
   mouse=InputSystem.AddDevice<Mouse>("TruckBilliards injected mouse");Setup(BilliardsMode.SharedKeyboard,1);Keys();
   w=Delay(1);while(w.MoveNext())yield return null;ScratchShot();
   w=Wait(()=>match.rules.Phase==MatchPhase.Placement,6,"mouse scratch placement");while(w.MoveNext())yield return null;
   w=Delay(.25f);while(w.MoveNext())yield return null;var mouseTarget=new Vector3(0,.5f,-7);
   var point=match.tableCamera.WorldToScreenPoint(mouseTarget);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(point.x,point.y)});
   w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(Vector3.Distance(match.placementPosition,mouseTarget)<.05f&&match.placementValid,"mouse ray positions a valid cue preview");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(point.x,point.y)}.WithButton(MouseButton.Left));
   w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(match.rules.Phase==MatchPhase.Aiming&&!match.balls[0].Pocketed,"mouse click confirms placement through incoming player's input");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(point.x,point.y)});
   // Two explicitly locked devices, including disconnect and replacement.
   session.MainMenu();session.SelectMode(BilliardsMode.SeparateControllers);session.players[0].pad=session.players[1].pad=null;
   Check(!session.StartMatch()&&session.Screen==BilliardsScreen.Join,"missing controller setup cannot start a broken match");
   Pad(pad1,button:GamepadButton.North);w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad1);w=Delay(.15f);while(w.MoveNext())yield return null;
   Check(session.players[0].pad==pad1&&!session.Assign(1,pad1),"join button locks pad 1 and rejects duplicate assignment");
   Pad(pad2,button:GamepadButton.North);w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad2);w=Delay(.15f);while(w.MoveNext())yield return null;
   Check(session.players[1].pad==pad2&&session.BothPadsReady,"join button independently locks pad 2");session.breakChoice=0;
   session.StartMatch();Pad(pad1);Pad(pad2);w=Delay(.3f);while(w.MoveNext())yield return null;
   p1=match.player.transform.position;p2=match.ai.transform.position;Pad(pad1,gas:.5f);w=Delay(.3f);while(w.MoveNext())yield return null;
   Check(Vector3.Distance(p1,match.player.transform.position)>.02f&&match.ai.transform.position==p2,"pad 1 controls only player 1");Pad(pad1);Pad(pad2,gas:.5f);
   w=Delay(.3f);while(w.MoveNext())yield return null;Check(Vector3.Distance(p2,match.ai.transform.position)>.02f,"pad 2 independently controls player 2");Pad(pad2);
   int oldShot=match.rules.ShotId;InputSystem.RemoveDevice(pad2);w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(session.Screen==BilliardsScreen.Disconnect&&Time.timeScale==0&&match.rules.ShotId==oldShot,"controller disconnect pauses and preserves match");
   Capture("04-disconnect");w=Delay(.3f);while(w.MoveNext())yield return null;
   InputSystem.AddDevice(pad2);Click("resume");w=Delay(.2f);while(w.MoveNext())yield return null;Check(session.GameplayEnabled&&session.players[1].pad==pad2,"same controller reconnect resumes safely");
   InputSystem.RemoveDevice(pad2);pad3=InputSystem.AddDevice<Gamepad>("TruckBilliards injected replacement");session.Reassign(1);session.Assign(1,pad3);Click("resume");Pad(pad1);Pad(pad3);
   w=Delay(.2f);while(w.MoveNext())yield return null;Check(session.GameplayEnabled&&session.players[1].pad==pad3&&match.rules.ShotId==oldShot,"explicit replacement preserves state and assignment");
   // Controller placement is owned by the incoming assigned pad.
   ScratchShot();w=Wait(()=>match.rules.Phase==MatchPhase.Placement,6,"controller physical scratch");while(w.MoveNext())yield return null;
   Pad(pad1);Pad(pad3);w=Delay(.2f);while(w.MoveNext())yield return null;preview=match.placementPosition;
   Pad(pad1,button:GamepadButton.South);w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(match.rules.Phase==MatchPhase.Placement,"wrong controller cannot confirm cue placement");Pad(pad1);Pad(pad3,turn:.5f);
   w=Delay(.3f);while(w.MoveNext())yield return null;Pad(pad3);w=Delay(.15f);while(w.MoveNext())yield return null;
   Check(Vector3.Distance(preview,match.placementPosition)>.2f,"assigned stick moves placement preview");
   Pad(pad3,gas:.6f,button:GamepadButton.South);w=Delay(.2f);while(w.MoveNext())yield return null;held=match.ai.transform.position;
   w=Delay(.2f);while(w.MoveNext())yield return null;Check(match.rules.Phase==MatchPhase.Aiming&&match.ai.transform.position==held,"controller confirm consumed before driving");Pad(pad3);
   // Hot seat keeps shooter through resolution and requires a fresh readiness edge.
   session.hotSeatGamepad=false;Setup(BilliardsMode.HotSeat);Keys();w=Delay(1);while(w.MoveNext())yield return null;
   PocketShot(1);w=Wait(()=>match.rules.Phase!=MatchPhase.Resolving,6,"hot-seat retained pot");while(w.MoveNext())yield return null;
   Check(session.sharedOwner==0&&session.Screen==BilliardsScreen.Match,"legal retained turn does not ask for handoff");
   Keys(Key.Enter);ScratchShot();Check(session.sharedOwner==0&&session.Screen==BilliardsScreen.Match,"shooter keeps device while balls resolve");
   w=Wait(()=>session.Screen==BilliardsScreen.Handoff,6,"hot-seat scratch handoff");while(w.MoveNext())yield return null;
   w=Delay(.2f);while(w.MoveNext())yield return null;Check(session.sharedOwner==0&&!match.preview.gameObject.activeSelf,"handoff precedes scratch placement and held Enter is ignored");
   Capture("05-handoff");w=Delay(.3f);while(w.MoveNext())yield return null;Keys();w=Delay(.2f);while(w.MoveNext())yield return null;Keys(Key.Enter);
   w=Wait(()=>session.GameplayEnabled&&session.sharedOwner==1,2,"fresh handoff confirmation");while(w.MoveNext())yield return null;Keys();
   w=Delay(.3f);while(w.MoveNext())yield return null;Check(match.preview.gameObject.activeSelf&&match.rules.Phase==MatchPhase.Placement,"incoming hot-seat player gets placement after confirmation");
   Keys(Key.F);w=Delay(.2f);while(w.MoveNext())yield return null;Keys();Check(match.rules.Phase==MatchPhase.Aiming,"hot-seat placement returns correct truck controls");
   session.Pause();Click("restart");Click("back");Check(session.Screen==BilliardsScreen.Pause&&match.rules.ShotId>0,"restart confirmation can cancel");Click("restart");Click("yes");if(session.Screen==BilliardsScreen.Handoff)session.ConfirmHandoff();Check(session.GameplayEnabled&&match.rules.ShotId==0&&match.rules.Pocketed.Count==0&&match.rules.Winner==-1&&!session.HandoffPending&&session.sharedOwner==session.openingBreaker&&!match.preview.gameObject.activeSelf&&!match.opponent.enabled,"accepted restart clears gameplay and handoff state");session.Pause();Click("main");Click("yes");
   Check(!session.InMatch&&!match.opponent.enabled&&!match.preview.gameObject.activeSelf,"return to main menu clears AI/input/placement activity");
   // One-controller hot seat, including a disconnect while paused inside Settings.
   session.hotSeatGamepad=true;session.MainMenu();session.SelectMode(BilliardsMode.HotSeat);session.Assign(0,pad1);session.breakChoice=0;session.StartMatch();Pad(pad1);
   w=Delay(1);while(w.MoveNext())yield return null;Pad(pad1,button:GamepadButton.South);ScratchShot();
   w=Wait(()=>session.Screen==BilliardsScreen.Handoff,6,"controller hot-seat handoff");while(w.MoveNext())yield return null;
   Check(session.sharedOwner==0&&session.HandoffPending,"held controller confirm cannot skip hot-seat handoff");
   session.Pause();Click("settings");InputSystem.RemoveDevice(pad1);w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(session.Screen==BilliardsScreen.Disconnect&&session.HandoffPending,"disconnect inside paused settings preserves pending handoff");
   InputSystem.AddDevice(pad1);Click("resume");Pad(pad1);w=Delay(.2f);while(w.MoveNext())yield return null;
   Check(session.Screen==BilliardsScreen.Handoff,"recovery restores handoff instead of routing the old shooter");
   Pad(pad1,button:GamepadButton.South);w=Delay(.2f);while(w.MoveNext())yield return null;
   results.Add("PROBE handoff screen="+session.Screen+" turn="+match.rules.Turn+" owner="+session.sharedOwner+" ready="+session.input.HandoffReady+" control="+session.players[1].control+" sharedPad="+session.SharedPad.deviceId+" pad1="+pad1.deviceId+" added="+pad1.added+" enabled="+pad1.enabled+" south="+pad1.buttonSouth.isPressed+" rawSouth="+pad1.buttonSouth.ReadValue());
   Capture("probe-controller-handoff");
   w=Wait(()=>session.GameplayEnabled&&session.sharedOwner==1,2,"controller readiness");while(w.MoveNext())yield return null;
   Pad(pad1);w=Delay(.25f);while(w.MoveNext())yield return null;Pad(pad1,turn:.5f);w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad1);
   w=Delay(.15f);while(w.MoveNext())yield return null;Pad(pad1,button:GamepadButton.South);w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad1);
   Check(match.rules.Phase==MatchPhase.Aiming&&session.sharedOwner==1,"shared controller handles incoming hot-seat placement");
   session.hotSeatGamepad=false;
   // A complete H-v-H match from the rack through a physical 8-ball win.
   Setup(BilliardsMode.SharedKeyboard);Keys();w=Delay(1);while(w.MoveNext())yield return null;
   for(int n=1;n<=7;n++){PocketShot(n);w=Wait(()=>match.rules.Phase!=MatchPhase.Resolving,6,"full match pot "+n);while(w.MoveNext())yield return null;Check(match.balls[n].Pocketed,"full H-v-H physical pot "+n);}
   PocketShot(8);w=Wait(()=>session.Screen==BilliardsScreen.Result,6,"H-v-H physical eight");while(w.MoveNext())yield return null;
   Check(match.rules.Winner==0&&match.rules.Phase==MatchPhase.Ended,"complete H-v-H match physically finishes");
   Capture("06-result");w=Delay(.3f);while(w.MoveNext())yield return null;Click("rematch");
   Check(match.rules.Turn==1&&match.rules.ShotId==0&&match.rules.Pocketed.Count==0&&session.Name(0)=="Alex"&&session.Name(1)=="Morgan","rematch keeps identity/mode and alternates breaker");
   // AI opening choice uses actual positioning and front bumper.
   Setup(BilliardsMode.SinglePlayer,1);Keys();
   w=Wait(()=>match.strikeCount==1,65,"AI physical opening shot");while(w.MoveNext())yield return null;
   Check(match.rules.Turn==1&&match.rules.Phase==MatchPhase.Resolving&&match.opponent.enabled,"single-player AI physically strikes with AI opening-break choice");
   // Finish a complete single-player match with actual physical captures.
   Setup(BilliardsMode.SinglePlayer);w=Delay(1);while(w.MoveNext())yield return null;
   for(int n=1;n<=7;n++){PocketShot(n);w=Wait(()=>match.rules.Phase!=MatchPhase.Resolving,6,"single-player pot "+n);while(w.MoveNext())yield return null;}
   PocketShot(8);w=Wait(()=>session.Screen==BilliardsScreen.Result,6,"single-player physical eight");while(w.MoveNext())yield return null;
   Check(match.rules.Winner==0&&match.balls[8].Pocketed,"complete single-player match physically finishes");
   Click("resultmain");Check(!session.InMatch&&!match.opponent.enabled&&session.Screen==BilliardsScreen.Main,"result Main Menu leaves no stale AI/match state");
   Check(!results.Exists(s=>s.StartsWith("RUNTIME ERROR")),"no runtime errors in local multiplayer smoke");
   results.Add("NOT RUN: native keyboard/mouse automation and two physical-controller hardware tests. All input/device checks above use injected Input System devices.");
   results.Add("Physical full-match fixtures reposition remaining balls/cue between shots; every pot then requires real motion, object contact and pocket capture. No recorded pocket facts are injected.");
   Capture("07-final-menu");w=Delay(.3f);while(w.MoveNext())yield return null;
  }
#endif
 }
}













