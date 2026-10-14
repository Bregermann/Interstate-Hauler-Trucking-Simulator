using UnityEngine;
using LWS.TruckHockey;
namespace LWS.TruckAirHockey {
public enum AirHockeyPhase {Setup,Countdown,Live,Goal,Paused,Finished}
// Single scoring authority. Segment crossings reuse the tested full-puck geometry rule.
public sealed class AirHockeyRules {
 public readonly int[] Score=new int[2];public const int Target=7;public AirHockeyPhase Phase{get;private set;}=AirHockeyPhase.Setup;
 public AirHockeyPhase BeforePause{get;private set;}public int Server{get;private set;}public int Winner{get;private set;}=-1;public int Scorer{get;private set;}=-1;public float Timer{get;private set;}public int Goals{get;private set;}
 public bool Live=>Phase==AirHockeyPhase.Live;
 public void Start(int opening){Score[0]=Score[1]=Goals=0;Winner=Scorer=-1;Phase=AirHockeyPhase.Setup;Serve(opening);}
 public void Serve(int slot){if(Phase==AirHockeyPhase.Finished)return;Server=slot;Phase=AirHockeyPhase.Countdown;Timer=2;}
 public bool Tick(float dt){if(Phase!=AirHockeyPhase.Countdown&&Phase!=AirHockeyPhase.Goal)return false;Timer=Mathf.Max(0,Timer-dt);if(Timer>0)return false;if(Phase==AirHockeyPhase.Goal){Serve(Server);return true;}Phase=AirHockeyPhase.Live;return true;}
 public bool Step(Vector3 previous,Vector3 current,float line,float halfMouth,float radius){if(!Live)return false;int net=-1;if(HockeyRules.Crossing(previous,current,-1,line,halfMouth,radius,out _))net=0;else if(HockeyRules.Crossing(previous,current,1,line,halfMouth,radius,out _))net=1;if(net<0)return false;Scorer=1-net;Server=net;Score[Scorer]++;Goals++;if(Score[Scorer]>=Target){Winner=Scorer;Phase=AirHockeyPhase.Finished;}else{Phase=AirHockeyPhase.Goal;Timer=1.2f;}return true;}
 public void Pause(){if(Phase==AirHockeyPhase.Paused||Phase==AirHockeyPhase.Finished||Phase==AirHockeyPhase.Setup)return;BeforePause=Phase;Phase=AirHockeyPhase.Paused;}
 public void Resume(){if(Phase==AirHockeyPhase.Paused)Phase=BeforePause;}
}
}
