using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LWS.TruckBowling;
using LWS.TruckCornhole;
using LWS.TruckHockey;
using LWS.TruckAirHockey;
using LWS.TruckWallball;
using LWS.TruckBilliards;
using LWS.TruckCheeseRolling;
namespace LWS.TruckSports {
// InputSystem Disabled includes temporary focus suspension. Only Removed/Disconnected
// establish missing hardware. Existing sport-specific modal gates still own resumption.
[DefaultExecutionOrder(-300)]public sealed class SportsFocusPolicy:MonoBehaviour {
 static SportsFocusPolicy current;bool previousBackground,ownsBackground;
 SportsEventSession sport;BowlingMatch bowling;CornholeMatch cornhole;HockeyMatch hockey;AirHockeyMatch air;WallballMatch wall;BilliardsSession billiards;CheeseMatch cheese;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){current=null;SceneManager.sceneLoaded-=Loaded;InputSystem.onDeviceChange-=DeviceChanged;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){SceneManager.sceneLoaded+=Loaded;InputSystem.onDeviceChange+=DeviceChanged;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Initial(){InstallScene();}
 static void Loaded(Scene scene,LoadSceneMode mode){InstallScene();}
 public static SportsFocusPolicy InstallScene(){MonoBehaviour owner=Object.FindAnyObjectByType<SportsEventSession>();if(owner==null)owner=Object.FindAnyObjectByType<BowlingMatch>();if(owner==null)owner=Object.FindAnyObjectByType<CornholeMatch>();if(owner==null)owner=Object.FindAnyObjectByType<HockeyMatch>();if(owner==null)owner=Object.FindAnyObjectByType<AirHockeyMatch>();if(owner==null)owner=Object.FindAnyObjectByType<WallballMatch>();if(owner==null)owner=Object.FindAnyObjectByType<BilliardsSession>();if(owner==null)owner=Object.FindAnyObjectByType<CheeseMatch>();if(owner==null)return null;var policy=owner.GetComponent<SportsFocusPolicy>();if(policy==null)policy=owner.gameObject.AddComponent<SportsFocusPolicy>();return policy;}
 void Awake(){sport=GetComponent<SportsEventSession>();bowling=GetComponent<BowlingMatch>();cornhole=GetComponent<CornholeMatch>();hockey=GetComponent<HockeyMatch>();air=GetComponent<AirHockeyMatch>();wall=GetComponent<WallballMatch>();billiards=GetComponent<BilliardsSession>();cheese=GetComponent<CheeseMatch>();}
 void OnEnable(){current=this;previousBackground=Application.runInBackground;Application.runInBackground=true;ownsBackground=true;}
 void OnDisable(){if(current==this)current=null;if(ownsBackground){Application.runInBackground=previousBackground;ownsBackground=false;}}
 bool Humans(BowlingSetup setup){for(int i=0;i<setup.count;i++)if(setup.players[i].source!=BowlingSource.AI)return true;return false;}
 public void PauseHuman(){if(sport!=null){if(!sport.AllAI)sport.Pause();}else if(bowling!=null){if(!bowling.AllAI&&bowling.InMatch&&!bowling.Finished&&!bowling.Paused)bowling.Pause(bowling.setup.mode==BowlingMode.HotSeat?bowling.ActiveSlot:0);}else if(cornhole!=null){if(Humans(cornhole.setup)&&cornhole.InMatch&&!cornhole.Finished&&!cornhole.Paused)cornhole.Pause();}else if(cheese!=null){if(Humans(cheese.setup)&&cheese.InMatch&&!cheese.Finished&&!cheese.Paused)cheese.Pause();}else if(hockey!=null){if(!hockey.AIOnly&&hockey.InMatch&&!hockey.MenuOpen)hockey.Pause();}else if(air!=null){if(!air.AllAI&&air.InMatch&&!air.MenuOpen)air.Pause();}else if(wall!=null){if(!wall.AllAI&&wall.InMatch&&!wall.MenuOpen)wall.Pause();}else if(billiards!=null&&!billiards.AIOnly&&(billiards.Screen==BilliardsScreen.Match||billiards.Screen==BilliardsScreen.Handoff||billiards.Screen==BilliardsScreen.GroupSelection))billiards.Pause();}
 bool UsesKeyboard(){if(sport!=null){for(int i=0;i<sport.setup.count;i++)if(sport.setup.players[i].source==BowlingSource.Keyboard)return true;}else if(bowling!=null){for(int i=0;i<bowling.setup.count;i++)if(bowling.setup.players[i].source==BowlingSource.Keyboard)return true;}else if(cornhole!=null){for(int i=0;i<cornhole.setup.count;i++)if(cornhole.setup.players[i].source==BowlingSource.Keyboard)return true;}else if(cheese!=null){for(int i=0;i<cheese.setup.count;i++)if(cheese.setup.players[i].source==BowlingSource.Keyboard)return true;}else if(hockey!=null){foreach(var p in hockey.players)if(p.source==HockeySource.Keyboard)return true;}else if(air!=null){foreach(var p in air.players)if(p.source==HockeySource.Keyboard)return true;}else if(wall!=null){for(int i=0;i<wall.Count;i++)if(wall.players[i].source==HockeySource.Keyboard)return true;}else if(billiards!=null){for(int i=0;i<(billiards.IsPractice?1:2);i++)if(!billiards.IsAI(i)&&billiards.players[i].control!=BilliardsControl.Gamepad)return true;}return false;}
 void MissingKeyboard(){if(!UsesKeyboard()||Keyboard.current!=null&&Keyboard.current.added&&Keyboard.current.enabled)return;if(sport!=null&&sport.Running&&!sport.Finished){if(!sport.Paused)sport.Pause(0,true);else sport.menus.Show(SportsEventScreen.Join);}else if(bowling!=null&&bowling.InMatch&&!bowling.Finished)bowling.Pause(0,true);else if(cornhole!=null&&cornhole.InMatch&&!cornhole.Finished)cornhole.Pause(0,true);else if(cheese!=null&&cheese.InMatch&&!cheese.Finished)cheese.Pause(0,true);else if(hockey!=null&&hockey.InMatch)hockey.Pause(true);else if(air!=null&&air.InMatch)air.Pause(true);else if(wall!=null&&wall.InMatch)wall.Pause(true);else if(billiards!=null&&billiards.InMatch)billiards.Disconnect();}
 static void DeviceChanged(InputDevice device,InputDeviceChange change){if(current==null)return;if(change==InputDeviceChange.Disabled&&device.added)current.PauseHuman();else if(device is Keyboard&&(change==InputDeviceChange.Removed||change==InputDeviceChange.Disconnected))current.MissingKeyboard();}
 public static bool Suspended(InputDevice device,InputDeviceChange change){if(change!=InputDeviceChange.Disabled||device==null||!device.added)return false;if(current!=null)current.PauseHuman();return true;}
 void OnApplicationFocus(bool focus){if(!focus)PauseHuman();}
 void OnApplicationPause(bool paused){if(paused)PauseHuman();}
 }
}
