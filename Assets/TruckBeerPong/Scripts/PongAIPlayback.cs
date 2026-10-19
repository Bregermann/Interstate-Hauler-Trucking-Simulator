using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckBeerPong {
 // Presentation pacing only. The match and PhysX still resolve every AI shot.
 public sealed class PongAIPlayback:MonoBehaviour {
  public PongMatch match;public int ManualSteps{get;private set;}bool ownsScale;float previousScale=1;
  public const string Preference="TruckBeerPong.AIPacing.v1";
  public static int Mode=>Mathf.Clamp(SportsPrefs.GetInt(Preference,0),0,2);
  public static string Label=>new[]{"NORMAL","FAST-FORWARD","SIMULATE TO RESULT"}[Mode];
  public void Cycle(){Release();SportsPrefs.SetInt(Preference,(Mode+1)%3);SportsPrefs.Save();match.menus.Dirty();match.RefreshExtraUI();}
  public void ResetMatch(){Release();ManualSteps=0;}
  public void Tick(){bool committed=match.Phase==PongPhase.Spring||match.Phase==PongPhase.Flight||match.Phase==PongPhase.Aftermath||match.Phase==PongPhase.Replay;
   // NWH approach/aiming runs at normal cadence until charge is committed.
   // Accelerating that decision phase changes how long an AI holds its charge.
   bool eligible=match.Live&&committed&&match.setup.players[match.Current].source==BowlingSource.AI;
   if(!eligible||Mode==0){Release();return;}if(!ownsScale){previousScale=Time.timeScale;ownsScale=true;}Time.timeScale=Mathf.Clamp(previousScale*(Mode==1?3:8),1,16);
   if(Mode==2&&match.Phase==PongPhase.Flight){ManualSteps+=match.SimulateAIFlight(64);if(match.rules.ShotLocked)Release();}
  }
  public void Release(){if(!ownsScale)return;ownsScale=false;Time.timeScale=match!=null&&match.Paused?0:previousScale;}
  void OnDisable(){Release();}void OnDestroy(){Release();}
 }
}
