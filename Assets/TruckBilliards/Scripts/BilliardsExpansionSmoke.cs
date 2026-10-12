using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace LWS.TruckBilliards
{
 public sealed partial class BilliardsSmoke
 {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  // Series workflow fixtures deliberately supply rule facts. Physical practice/AI tests below do not.
  void FinishRack(int winner,bool scratchEight=false){
   var r=match.rules;int shooter=r.Turn;if(!r.BeginShot())throw new Exception("Rack fixture cannot start");r.Contact(1);
   if(winner==shooter){for(int n=1;n<=7;n++)r.Pocket(n,n);}else r.Pocket(1,1);
   r.Resolve();session.ShotResolved(shooter);if(!r.BeginShot())throw new Exception("Eight fixture cannot start");r.Contact(8);r.Pocket(8,10);if(scratchEight)r.Pocket(0,11);r.Resolve();session.ShotResolved(shooter);
  }
  IEnumerator ReadyForRack(){
   if(session.Screen!=BilliardsScreen.Handoff)yield break;
   Keys();if(pad1!=null)Pad(pad1);IEnumerator w=Delay(.25f);while(w.MoveNext())yield return null;
   if(session.hotSeatGamepad)Pad(pad1,button:GamepadButton.South);else Keys(Key.Enter);
   w=Wait(()=>session.GameplayEnabled,2,"between-rack readiness");while(w.MoveNext())yield return null;
   Keys();if(pad1!=null)Pad(pad1);w=Delay(.2f);while(w.MoveNext())yield return null;
  }
  IEnumerator RunExpansion(){
   IEnumerator w=Delay(1);while(w.MoveNext())yield return null;
   results.Add("HARDWARE gamepads before injection: "+Gamepad.all.Count);SaveSettingsBaseline();
   keyboard=InputSystem.AddDevice<Keyboard>("TruckBilliards feature keyboard");mouse=InputSystem.AddDevice<Mouse>("TruckBilliards feature mouse");pad1=InputSystem.AddDevice<Gamepad>("TruckBilliards feature pad 1");pad2=InputSystem.AddDevice<Gamepad>("TruckBilliards feature pad 2");Keys();Pad(pad1);Pad(pad2);
   Capture("01-main");w=Delay(.25f);while(w.MoveNext())yield return null;Click("play");Click("format");Check(session.format==BilliardsFormat.BestOf3,"setup match length selection");Click("difficulty");Check(PlayerPrefs.GetInt("TruckBilliards.Settings.Difficulty")== (int)session.difficulty,"difficulty selection persists");var savedDifficulty=session.difficulty;session.settings.Load();Check(session.difficulty==savedDifficulty,"difficulty reload matches saved selection");
   session.difficulty=BilliardsDifficulty.Normal;session.players[0].name="Alex";session.players[1].name="Morgan";menus.RefreshSetup();Capture("02-setup");w=Delay(.25f);while(w.MoveNext())yield return null;
   var feedback=session.feedback;Check(feedback.clips.Length==8&&Array.TrueForAll(feedback.clips,c=>c!=null&&c.length>0),"eight distinct authored feedback clips wired");
   session.settings.master=session.settings.sfx=1;session.settings.ApplyAudio();feedback.StopAudio();feedback.Impact(BilliardsSound.Ball,.5f);float gentle=feedback.LastVolume;feedback.Impact(BilliardsSound.Ball,12);Check(feedback.LastVolume>gentle*5,"impact volume follows contact strength");Check(feedback.LastVolume<=.18f,"maximum impact leaves six-voice mix headroom");
   feedback.StopAudio();int before=feedback.Played;for(int i=0;i<20;i++)feedback.Impact(BilliardsSound.Ball,8);Check(feedback.ActiveVoices<=6&&feedback.Played-before<=6&&feedback.Dropped>0,"break effects use at most six voices");feedback.StopAudio();
   session.settings.sfx=0;session.settings.ApplyAudio();before=feedback.Played;feedback.Cue(BilliardsSound.SeriesWin);Check(feedback.Played==before,"SFX mute blocks new feedback");session.settings.sfx=1;session.settings.master=0;session.settings.ApplyAudio();feedback.Cue(BilliardsSound.Pocket);Check(feedback.Played==before,"master mute blocks new feedback");session.settings.master=1;session.settings.ApplyAudio();
   int[][] scores={new[]{0},new[]{0,0},new[]{0,1,0},new[]{1,1,1},new[]{1,0,1,0,1}};
   for(int test=0;test<scores.Length;test++){
    session.format=test==0?BilliardsFormat.SingleRack:test<3?BilliardsFormat.BestOf3:BilliardsFormat.BestOf5;Setup(BilliardsMode.SharedKeyboard);
    for(int rack=0;rack<scores[test].Length;rack++){
     FinishRack(scores[test][rack],test==1&&rack==1);int total=session.series.Wins[0]+session.series.Wins[1],bursts=session.celebration.Bursts;
     session.ShotResolved(match.rules.Turn);session.ShotResolved(match.rules.Turn);Check(total==rack+1&&session.series.Wins[0]+session.series.Wins[1]==total&&session.celebration.Bursts==bursts,"exactly one award/celebration test "+test+" rack "+rack);
     if(rack<scores[test].Length-1){if(test==2&&rack==0){Capture("03-rack-result");w=Delay(.2f);while(w.MoveNext())yield return null;}Click("nextrack");Check(match.rules.Turn==(rack+1)%2&&match.rules.ShotId==0&&match.rules.PlayerGroup==0&&match.rules.Pocketed.Count==0&&Array.TrueForAll(match.balls,b=>!b.Pocketed&&b.body.linearVelocity==Vector3.zero),"next-rack reset and alternate breaker");}
    }
    Check(session.series.Finished&&session.series.Wins[session.series.Winner]==session.series.Required&&session.Screen==BilliardsScreen.Result,"series outcome "+test);
    if(test==4){Capture("04-series-result");Check(session.celebration.Strong,"series celebration distinct from rack");w=Delay(.25f);while(w.MoveNext())yield return null;}
    int first=session.series.OriginalBreaker;Click("rematch");Check(session.series.Wins[0]+session.series.Wins[1]==0&&match.rules.Turn==1-first&&session.Name(0)=="Alex","rematch series clears score and alternates original opener");Check(!session.celebration.Active,"continuation immediately clears celebration");
   }
   session.format=BilliardsFormat.BestOf3;Setup(BilliardsMode.SharedKeyboard);FinishRack(0);Click("nextrack");match.rules.BeginShot();match.balls[0].body.linearVelocity=Vector3.right*4;
   session.Pause();Click("restart");Click("yes");Check(session.series.Wins[0]==1&&session.series.RackNumber==2&&match.rules.Turn==1&&match.rules.ShotId==0,"Restart Rack preserves earlier score/current breaker");
   session.Pause();Click("restartseries");Click("yes");Check(session.series.Wins[0]+session.series.Wins[1]==0&&session.series.RackNumber==1&&match.rules.Turn==0,"Restart Series restores original opener and clears scores");
   // Distinct pads survive next rack and result-screen disconnect.
   session.MainMenu();session.SelectMode(BilliardsMode.SeparateControllers);session.Show(BilliardsScreen.Setup);Capture("07-pad-setup");w=Delay(.25f);while(w.MoveNext())yield return null;session.Assign(0,pad1);session.Assign(1,pad2);session.breakChoice=0;session.StartMatch();FinishRack(0);Click("nextrack");Check(session.players[0].pad==pad1&&session.players[1].pad==pad2&&match.rules.Turn==1,"separate-controller series preserves assignments");FinishRack(1);InputSystem.RemoveDevice(pad2);w=Delay(.1f);while(w.MoveNext())yield return null;InputSystem.AddDevice(pad2);Click("resume");Check(session.Screen==BilliardsScreen.Result&&session.series.Wins[0]==1&&session.series.Wins[1]==1,"result reconnect returns to between-rack screen");Click("nextrack");
   session.settings.vibration=true;feedback.Pulse(1,.2f,.3f);Check(feedback.LastDeviceId==pad2.deviceId&&feedback.RunningMotors==1,"haptic request routes only to assigned player pad");session.Pause();Check(feedback.RunningMotors==0,"pause stops haptic requests");Click("settings");Click("vibration");Check(!session.settings.vibration&&PlayerPrefs.GetInt("TruckBilliards.Settings.Vibration")==0,"vibration setting persists off");Capture("09-settings");w=Delay(.25f);while(w.MoveNext())yield return null;int pulses=feedback.PulseCount;feedback.Pulse(0,.2f,.3f);Check(feedback.PulseCount==pulses,"vibration off blocks requests");Click("vibration");Click("back");Click("resume");feedback.Pulse(0,.2f,.1f);w=Delay(.2f);while(w.MoveNext())yield return null;Check(feedback.RunningMotors==0,"haptic request expires on unscaled time");feedback.Pulse(0,.2f,.3f);InputSystem.RemoveDevice(pad1);Check(feedback.RunningMotors==0,"disconnect stops haptic requests");InputSystem.AddDevice(pad1);Click("resume");feedback.Pulse(0,.2f,.3f);feedback.enabled=false;Check(feedback.RunningMotors==0,"disable stops haptic requests");feedback.enabled=true;
   // Between-rack hot-seat ownership and consumed readiness.
   Setup(BilliardsMode.HotSeat);session.hotSeatGamepad=false;session.RestartSeries();FinishRack(0);Keys(Key.Enter);Click("nextrack");w=Delay(.2f);while(w.MoveNext())yield return null;Check(session.Screen==BilliardsScreen.Handoff&&session.sharedOwner==0&&session.HandoffPending,"between-rack hot-seat fresh readiness required");feedback.Pulse(0,.2f,.3f);w=ReadyForRack();while(w.MoveNext())yield return null;Check(session.sharedOwner==1&&match.rules.Turn==1,"between-rack ownership goes to alternate breaker");
   session.MainMenu();session.hotSeatGamepad=true;session.SelectMode(BilliardsMode.HotSeat);session.Assign(0,pad1);session.breakChoice=0;session.StartMatch();FinishRack(0);Pad(pad1,button:GamepadButton.South);Click("nextrack");w=Delay(.2f);while(w.MoveNext())yield return null;Check(session.Screen==BilliardsScreen.Handoff&&feedback.RunningMotors==0,"one-pad next rack ignores held South/stops vibration");w=ReadyForRack();while(w.MoveNext())yield return null;Check(session.sharedOwner==1&&session.SharedPad==pad1,"hot-seat series retains shared assignment");
   session.MainMenu();session.hotSeatGamepad=false;Click("practice");Click("start");Check(session.IsPractice&&!match.opponent.enabled&&!match.ai.gameObject.activeSelf&&match.rules.Practice,"Practice enters with exactly one truck and no AI");Capture("05-practice");
   w=Delay(1);while(w.MoveNext())yield return null;session.Pause();Capture("08-practice-pause");w=Delay(.25f);while(w.MoveNext())yield return null;Click("resume");Keys();w=Delay(.25f);while(w.MoveNext())yield return null;
   int clacks=feedback.SoundCounts[(int)BilliardsSound.Ball];PocketShot(8);Keys(Key.W);w=Delay(.2f);while(w.MoveNext())yield return null;Check(match.rules.Phase==MatchPhase.Resolving&&match.player.Speed>0&&match.rules.ShotId==1,"Practice drives during motion but permits one shot");session.RepositionCue();Check(match.rules.Phase==MatchPhase.Resolving,"Practice manual placement blocked during motion");Keys();w=Wait(()=>match.rules.Phase==MatchPhase.Aiming,6,"practice eight settles");while(w.MoveNext())yield return null;
   Check(match.balls[8].Pocketed&&match.rules.Pocketed.Contains(8)&&match.rules.Winner==-1&&match.rules.PlayerGroup==0&&match.rules.Turn==0&&session.GameplayEnabled,"physical Practice 8 stays down without win/group/turn pass");
   Check(feedback.SoundCounts[(int)BilliardsSound.Ball]-clacks==1,"real ball pair reports exactly one clack");
   int oldBursts=session.celebration.Bursts;ScratchShot();w=Wait(()=>match.rules.Phase==MatchPhase.Placement,6,"practice physical scratch");while(w.MoveNext())yield return null;Check(match.balls[0].Pocketed&&match.rules.Turn==0&&session.celebration.Bursts==oldBursts,"Practice physical scratch opens same-player placement without celebration");
   Keys();w=Delay(.25f);while(w.MoveNext())yield return null;Keys(Key.F);w=Delay(.2f);while(w.MoveNext())yield return null;Keys();Check(match.rules.Phase==MatchPhase.Aiming&&!match.balls[0].Pocketed,"Practice keyboard scratch placement restores cue");
   w=Delay(1);while(w.MoveNext())yield return null;session.Pause();Click("reposition");Check(match.rules.Phase==MatchPhase.Placement&&session.GameplayEnabled&&match.preview.gameObject.activeSelf,"Practice pause manual placement opens valid preview");Keys();w=Delay(.2f);while(w.MoveNext())yield return null;Keys(Key.F);w=Delay(.2f);while(w.MoveNext())yield return null;Keys();Check(match.rules.Phase==MatchPhase.Aiming&&!match.preview.gameObject.activeSelf,"Practice manual placement confirms normally");
   match.rules.BeginShot();match.balls[0].body.linearVelocity=Vector3.forward*8;session.Pause();Click("practicereset");Click("yes");w=Delay(.2f);while(w.MoveNext())yield return null;Check(match.rules.ShotId==0&&match.rules.Pocketed.Count==0&&match.resolutionCount==0&&!match.preview.gameObject.activeSelf&&Array.TrueForAll(match.balls,b=>!b.Pocketed&&b.body.linearVelocity==Vector3.zero),"Practice mid-shot Reset Rack clears velocity/events/preview");
   int cushions=feedback.SoundCounts[(int)BilliardsSound.Cushion];match.balls[0].Restore(new Vector3(-2,.5f,-6));Physics.SyncTransforms();match.rules.BeginShot();match.balls[0].body.linearVelocity=Vector3.right*8;w=Delay(1.4f);while(w.MoveNext())yield return null;Check(feedback.SoundCounts[(int)BilliardsSound.Cushion]>cushions,"real cushion collision routes distinct impact effect");
   session.MainMenu();session.hotSeatGamepad=true;Click("practice");session.Assign(0,pad1);session.Show(BilliardsScreen.Join);Pad(pad2,button:GamepadButton.North);w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad2);Check(session.SharedPad==pad1,"Practice join keeps the first assigned pad locked");session.StartMatch();Check(session.players[1].pad==null&&session.players[1].control!=BilliardsControl.Gamepad,"inactive Practice slot holds no controller");Pad(pad1);w=Delay(1);while(w.MoveNext())yield return null;session.Pause();Click("reposition");w=Delay(.25f);while(w.MoveNext())yield return null;Pad(pad1,turn:.6f);w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad1);w=Delay(.1f);while(w.MoveNext())yield return null;Pad(pad1,button:GamepadButton.South);w=Delay(.2f);while(w.MoveNext())yield return null;Pad(pad1);Check(match.rules.Phase==MatchPhase.Aiming&&!match.preview.gameObject.activeSelf,"Practice assigned controller placement restores control");
   feedback.Pulse(0,.2f,.3f);session.MainMenu();Check(feedback.RunningMotors==0&&!session.celebration.Active&&!match.opponent.enabled,"Main Menu clears vibration/celebration/AI");session.hotSeatGamepad=false;Setup(BilliardsMode.SharedKeyboard);w=Delay(1);while(w.MoveNext())yield return null;session.RepositionCue();Check(match.rules.Phase==MatchPhase.Aiming&&!match.preview.gameObject.activeSelf,"competitive settled manual placement remains forbidden");
   // Identical layouts, fixed seeds; report measured plans, not invented win rates.
   var csv=new System.Text.StringBuilder("layout,difficulty,target,pocket,candidates,fallback,meanAbsAimError,meanAbsPowerError,power\n");
   for(int layout=0;layout<3;layout++){
    Setup(BilliardsMode.SinglePlayer,1);match.balls[0].Restore(new Vector3(0,.5f,-6));foreach(var b in match.balls)if(b.number!=0&&b.number!=1&&b.number!=2&&b.number!=9)b.SetPocketed(true);
    match.balls[1].Restore(layout==0?new Vector3(0,.5f,-1):layout==1?new Vector3(-3,.5f,-2):new Vector3(0,.5f,2));match.balls[2].Restore(layout==2?new Vector3(0,.5f,-1):new Vector3(3,.5f,4));match.balls[9].Restore(new Vector3(-2,.5f,7));Physics.SyncTransforms();
    float previousAim=float.PositiveInfinity,previousPower=float.PositiveInfinity;
    for(int level=0;level<3;level++){float aim=0,powerError=0;BilliardsAI.ShotPlan plan=default;var tune=match.opponent.Tuning((BilliardsDifficulty)level);
     for(int sample=0;sample<12;sample++){plan=match.opponent.EvaluateLayout((BilliardsDifficulty)level,123+sample);if(!plan.valid)throw new Exception("No plan in comparison layout");aim+=Mathf.Abs(plan.aimError);powerError+=Mathf.Abs(plan.powerError);if(Mathf.Abs(plan.aimError)>tune.aimErrorDegrees+.001f||plan.power<.25f||plan.power>1)throw new Exception("Difficulty outside bounds");}
     aim/=12;powerError/=12;Check(aim<previousAim&&powerError<previousPower,"difficulty measured error ordering layout "+layout+" level "+level);previousAim=aim;previousPower=powerError;
     csv.AppendLine(layout+","+(BilliardsDifficulty)level+","+plan.target+","+plan.pocket+","+plan.candidates+","+plan.fallback+","+aim.ToString("F4")+","+powerError.ToString("F4")+","+plan.power.ToString("F4"));
    }
   }File.WriteAllText(Path.Combine(folder,"ai-comparison.csv"),csv.ToString());
   session.format=BilliardsFormat.SingleRack;
   for(int level=0;level<3;level++){session.difficulty=(BilliardsDifficulty)level;Setup(BilliardsMode.SinglePlayer,1);w=Wait(()=>match.strikeCount==1,65,"physical AI strike "+level);while(w.MoveNext())yield return null;Check(match.rules.Phase==MatchPhase.Resolving&&match.rules.Turn==1&&match.balls[0].body.linearVelocity.magnitude<=match.ai.maxShotSpeed+.1f,"genuine physical AI opening strike "+(BilliardsDifficulty)level);}
   Check(!results.Exists(s=>s.StartsWith("RUNTIME ERROR")),"no runtime errors in feature smoke");session.MainMenu();Capture("06-final-main");w=Delay(.3f);while(w.MoveNext())yield return null;
   results.Add("Series outcomes use explicitly recorded authoritative rule facts; physical Practice pots/scratches and physical AI strikes require real motion. Input and haptics checks are injected-device/software requests, not hardware vibration proof. Mix listening and native desktop input remain manual.");
  }
#endif
 }
}






