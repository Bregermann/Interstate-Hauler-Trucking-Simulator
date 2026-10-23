using System;
namespace LWS.TruckVolleyball
{
 [Serializable]public sealed class VolleyRules
 {
  public int Target{get;private set;}public int Cap{get;private set;}public int Server{get;private set;}public int LastTeam{get;private set;}=-1;public int LastPlayer{get;private set;}=-1;public int Touches{get;private set;}public int Winner{get;private set;}=-1;public bool Live{get;private set;}public bool Complete{get;private set;}public int Rally{get;private set;}public string Call{get;private set;}="";
  public readonly int[] points=new int[2],spikes=new int[2],saves=new int[2],teamPlays=new int[2];float lastAt=-100;
  public event Action<int,string> PointAwarded;
  public void Reset(int target=15,int cap=21){if(target<3||target>25||cap<target+2||cap>35)throw new ArgumentOutOfRangeException();Target=target;Cap=cap;Server=0;Winner=LastTeam=LastPlayer=-1;Touches=Rally=0;Live=Complete=false;Call="";lastAt=-100;foreach(var a in new[]{points,spikes,saves,teamPlays})Array.Clear(a,0,2);}
  public bool Serve(){if(Complete||Live||Target==0)return false;Live=true;LastTeam=Server;LastPlayer=-1;Touches=0;lastAt=-100;Rally++;Call="SERVE";return true;}
  public bool Touch(int team,int player,float seconds,bool aerial,bool lowSave)
  {
   if(!Live||team<0||team>1||player<0||float.IsNaN(seconds)||float.IsInfinity(seconds))return false;
   if(player==LastPlayer&&seconds-lastAt<.3f)return false;
   if(team==LastTeam&&player==LastPlayer){Award(1-team,"DOUBLE TOUCH");return false;}
   if(LastTeam!=team)Touches=0;LastTeam=team;LastPlayer=player;lastAt=seconds;Touches++;
   if(Touches>3){Award(1-team,"FOUR TOUCHES");return false;}
   if(aerial)spikes[team]++;if(lowSave)saves[team]++;Call=aerial?"AERIAL STRIKE":lowSave?"LOW SAVE":"TOUCH "+Touches+" / 3";return true;
  }
  public void Cross(int from,int to){if(!Live||from<0||from>1||to!=1-from)return;if(Touches==3&&LastTeam==from)teamPlays[from]++;Touches=0;LastPlayer=-1;}
  public bool Land(int court){if(!Live||court<0||court>1)return false;return Award(1-court,"BALL DOWN");}
  public bool Out(){if(!Live)return false;return Award(1-(LastTeam>=0?LastTeam:Server),"OUT OF BOUNDS");}
  bool Award(int team,string reason){if(!Live||Complete)return false;points[team]++;Server=team;Live=false;Call=reason;if(points[team]>=Cap||points[team]>=Target&&points[team]-points[1-team]>=2){Winner=team;Complete=true;}PointAwarded?.Invoke(team,reason);return true;}
 }
}
