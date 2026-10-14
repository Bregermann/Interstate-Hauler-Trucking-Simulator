namespace LWS.TruckWallball {
public enum WallballPhase {Setup,Countdown,Serving,Outgoing,Receiving,Point,Paused,Finished}
// The sole rally/scoring authority; physics reports contacts, never awards its own points.
public sealed class WallballRules {
 public const int Target=11;public readonly int[] Score=new int[2];
 public WallballPhase Phase{get;private set;}=WallballPhase.Setup;public WallballPhase BeforePause{get;private set;}
 public int Server{get;private set;}public int Eligible{get;private set;}=-1;public int Hitter{get;private set;}=-1;public int Receiver{get;private set;}=-1;
 public int Bounces{get;private set;}public int Shots{get;private set;}public int LegalWalls{get;private set;}public int Points{get;private set;}public int Winner{get;private set;}=-1;public int PointWinner{get;private set;}=-1;
 public float Timer{get;private set;}public float ServeSeconds=25;public string Reason{get;private set;}="";
 public bool Active=>Phase==WallballPhase.Serving||Phase==WallballPhase.Outgoing||Phase==WallballPhase.Receiving;
 public void Start(int opener){Score[0]=Score[1]=Shots=LegalWalls=Points=0;Winner=PointWinner=-1;Countdown(opener,2);}
 void Countdown(int server,float seconds){Server=server;Eligible=Hitter=Receiver=-1;Bounces=0;Phase=WallballPhase.Countdown;Timer=seconds;}
 public bool Strike(int slot){if(!Active||Eligible!=slot)return false;Hitter=slot;Receiver=1-slot;Eligible=-1;Bounces=0;Phase=WallballPhase.Outgoing;Shots++;return true;}
 public void Wall(bool valid){if(Phase!=WallballPhase.Outgoing)return;if(!valid){Fault(Hitter,"OUT");return;}Phase=WallballPhase.Receiving;Eligible=Receiver;Bounces=0;LegalWalls++;}
 public void Floor(){if(Phase==WallballPhase.Outgoing)Fault(Hitter,"MISSED WALL");else if(Phase==WallballPhase.Receiving&&++Bounces>=2)Fault(Receiver,"SECOND BOUNCE");}
 public void Out(){if(Active)Fault(Phase==WallballPhase.Serving?Server:Hitter,"OUT");}
 public bool Fault(int culprit,string reason){if(!Active||culprit<0||culprit>1)return false;PointWinner=1-culprit;Score[PointWinner]++;Points++;Reason=reason;Server=PointWinner;Eligible=-1;
  if(Score[PointWinner]>=Target&&System.Math.Abs(Score[0]-Score[1])>=2){Winner=PointWinner;Phase=WallballPhase.Finished;}else{Phase=WallballPhase.Point;Timer=1.6f;}return true;}
 public bool Tick(float dt){if(Phase==WallballPhase.Serving){Timer=System.Math.Max(0,Timer-dt);if(Timer<=0)return Fault(Server,"SERVE TIMEOUT");return false;}if(Phase!=WallballPhase.Countdown&&Phase!=WallballPhase.Point)return false;Timer=System.Math.Max(0,Timer-dt);if(Timer>0)return false;if(Phase==WallballPhase.Point)Countdown(Server,1.5f);else{Phase=WallballPhase.Serving;Eligible=Server;Timer=ServeSeconds;}return true;}
 public void Replay(){if(Active)Countdown(Server,2);}
 public void Pause(){if(Phase==WallballPhase.Setup||Phase==WallballPhase.Finished||Phase==WallballPhase.Paused)return;BeforePause=Phase;Phase=WallballPhase.Paused;}
 public void Resume(){if(Phase==WallballPhase.Paused)Phase=BeforePause;}
}
}
