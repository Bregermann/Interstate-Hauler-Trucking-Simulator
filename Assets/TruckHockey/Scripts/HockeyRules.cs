using UnityEngine;
namespace LWS.TruckHockey {
public enum HockeyPhase {Pregame,Live,Goal,Faceoff,Paused,Overtime,Finished}
public enum HockeyMode {PlayerAI,SharedKeyboard,SeparateControllers,KeyboardController}
public enum HockeyDifficulty {Easy,Normal,Hard}
// Single match authority; goals use a segment from the completed physics step, not trigger callback order.
public sealed class HockeyRules {
 public HockeyPhase Phase{get;private set;}=HockeyPhase.Pregame; public HockeyPhase BeforePause{get;private set;}
 public readonly int[] Score=new int[2];public float Remaining{get;private set;}public float PhaseTimer{get;private set;}
 public bool Overtime{get;private set;}public int Winner{get;private set;}=-1;public int LastScorer{get;private set;}=-1;
 public bool Swapped{get;private set;}public int Goals{get;private set;}public int Resets{get;private set;}
 public bool Live=>Phase==HockeyPhase.Live||Phase==HockeyPhase.Overtime;
 public int DefendingSign(int side)=>(side==0?-1:1)*(Swapped?-1:1);
 public int Attacker(int netSign)=>DefendingSign(0)==netSign?1:0;
 public void Start(float seconds,bool swapped){Remaining=seconds;Swapped=swapped;Score[0]=Score[1]=0;Winner=LastScorer=-1;Overtime=false;Goals=Resets=0;Phase=HockeyPhase.Pregame;PhaseTimer=3;}
 public void Pause(){if(Phase==HockeyPhase.Paused||Phase==HockeyPhase.Finished)return;BeforePause=Phase;Phase=HockeyPhase.Paused;}
 public void Resume(){if(Phase==HockeyPhase.Paused)Phase=BeforePause;}
 public bool Countdown(float dt){if(Phase==HockeyPhase.Pregame||Phase==HockeyPhase.Faceoff){PhaseTimer=Mathf.Max(0,PhaseTimer-dt);if(PhaseTimer<=0){Phase=Overtime?HockeyPhase.Overtime:HockeyPhase.Live;return true;}}else if(Phase==HockeyPhase.Goal){PhaseTimer=Mathf.Max(0,PhaseTimer-dt);if(PhaseTimer<=0){Faceoff();return true;}}return false;}
 public void Faceoff(){if(Phase==HockeyPhase.Finished)return;Phase=HockeyPhase.Faceoff;PhaseTimer=3;Resets++;}
 public static bool Crossing(Vector3 previous,Vector3 current,int sign,float line,float mouthHalf,float radius,out float fraction){fraction=0;float a=previous.x*sign,b=current.x*sign,threshold=line+radius;if(a>threshold||b<=threshold||b<=a)return false;fraction=(threshold-a)/(b-a);float z=Mathf.Lerp(previous.z,current.z,fraction);return Mathf.Abs(z)<=mouthHalf-radius;}
 // Exact deadline policy: a crossing at or before the remaining fraction of this step counts; later crossings do not.
 public bool Step(float dt,Vector3 previous,Vector3 current,float line,float mouthHalf,float radius){if(!Live)return false;int sign=0;float f=2;if(Crossing(previous,current,-1,line,mouthHalf,radius,out float l)){sign=-1;f=l;}if(Crossing(previous,current,1,line,mouthHalf,radius,out float r)&&r<f){sign=1;f=r;}
 if(sign!=0&&(Overtime||f*dt<=Remaining+0.000001f)){if(!Overtime)Remaining=Mathf.Max(0,Remaining-f*dt);LastScorer=Attacker(sign);Score[LastScorer]++;Goals++;if(Overtime||(Remaining<=.000001f&&Score[0]!=Score[1])){Winner=Overtime?LastScorer:Score[0]>Score[1]?0:1;Phase=HockeyPhase.Finished;}else{Phase=HockeyPhase.Goal;PhaseTimer=1.5f;}return true;}
 if(!Overtime){Remaining=Mathf.Max(0,Remaining-dt);if(Remaining<=0){if(Score[0]!=Score[1]){Winner=Score[0]>Score[1]?0:1;Phase=HockeyPhase.Finished;}else{Overtime=true;Faceoff();}}}return false;}
 public void EndAfterGoal(){if(Remaining<=0&&!Overtime){if(Score[0]!=Score[1]){Winner=Score[0]>Score[1]?0:1;Phase=HockeyPhase.Finished;}else{Overtime=true;Faceoff();}}}
}
}
