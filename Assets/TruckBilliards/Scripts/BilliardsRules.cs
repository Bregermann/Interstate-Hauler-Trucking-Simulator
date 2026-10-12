using System.Collections.Generic;
using System.Linq;
namespace LWS.TruckBilliards {
public enum MatchPhase { Menu, Aiming, Resolving, Placement, Ended }
public sealed class PocketRecord { public int Number, Step; }
public sealed class BilliardsRules {
 public readonly HashSet<int> Pocketed=new HashSet<int>();
 public int Turn {get;private set;}
 public int PlayerGroup {get;private set;}
 public int ShotId {get;private set;}
 public int FirstContact {get;private set;}=-1;
 public bool Opening {get;private set;}=true;
 public bool EligibleAtStart {get;private set;}
 public MatchPhase Phase {get;private set;}=MatchPhase.Menu;
 public string Notice {get;private set;}="Drive your truck into the cue ball.";
 public int Winner {get;private set;}=-1;
 public bool RespotEight {get;private set;}
 public bool Practice {get;private set;} public bool LastFoul {get;private set;} public bool LastScored {get;private set;}
 readonly List<PocketRecord> records=new List<PocketRecord>();
 public int Group(int side)=>PlayerGroup==0?0:side==0?PlayerGroup:3-PlayerGroup;
 public static int BallGroup(int n)=>n>=1&&n<=7?1:n>=9&&n<=15?2:0;
 public int Remaining(int side)=>Enumerable.Range(1,15).Count(n=>BallGroup(n)==Group(side)&&BallGroup(n)!=0&&!Pocketed.Contains(n));
 public bool LegalTarget(int n)=>Group(Turn)==0?BallGroup(n)!=0:Remaining(Turn)==0?n==8:BallGroup(n)==Group(Turn);
 public void Reset(int breaker=0,bool practice=false) {Practice=practice;LastFoul=LastScored=false;Pocketed.Clear();records.Clear();Turn=breaker;PlayerGroup=0;ShotId=0;FirstContact=-1;Opening=true;EligibleAtStart=false;Winner=-1;RespotEight=false;Phase=MatchPhase.Aiming;Notice="Opening break — line up and accelerate!";}
 public bool BeginShot() {if(Phase!=MatchPhase.Aiming)return false;EligibleAtStart=Group(Turn)!=0&&Remaining(Turn)==0;FirstContact=-1;records.Clear();ShotId++;Phase=MatchPhase.Resolving;Notice="Balls moving — both bumpers disarmed.";return true;}
 public void Contact(int n) {if(Phase==MatchPhase.Resolving&&FirstContact<0&&n>0)FirstContact=n;}
 public bool Pocket(int n,int step) {if(Phase!=MatchPhase.Resolving||!Pocketed.Add(n))return false;records.Add(new PocketRecord{Number=n,Step=step});return true;}
 public void Resolve() {
  if(Phase!=MatchPhase.Resolving)return;
  bool scratch=records.Any(p=>p.Number==0);
  LastFoul=scratch;LastScored=records.Any(p=>p.Number>0);
  if(Practice){Turn=0;Opening=false;Phase=scratch?MatchPhase.Placement:MatchPhase.Aiming;Notice=scratch?"SCRATCH — place the cue ball.":"Practice — line up another shot.";return;}
  bool valid=FirstContact>0&&(Group(Turn)==0?BallGroup(FirstContact)!=0:EligibleAtStart?FirstContact==8:BallGroup(FirstContact)==Group(Turn));
  bool foul=scratch||!valid, eight=records.Any(p=>p.Number==8);LastFoul=foul;LastScored=!foul&&records.Any(p=>BallGroup(p.Number)!=0&&(Group(Turn)==0||BallGroup(p.Number)==Group(Turn)));
  RespotEight=eight&&Opening;if(RespotEight)Pocketed.Remove(8);
  if(eight&&!Opening){Winner=EligibleAtStart&&!foul?Turn:1-Turn;Phase=MatchPhase.Ended;Notice=Winner==Turn?"Legal 8-ball — rack won!":"Illegal / early 8-ball — shooter loses.";return;}
  if(PlayerGroup==0&&!foul){var first=records.Where(p=>BallGroup(p.Number)!=0).OrderBy(p=>p.Step).ThenBy(p=>p.Number).FirstOrDefault();if(first!=null)PlayerGroup=Turn==0?BallGroup(first.Number):3-BallGroup(first.Number);}
  bool keep=!foul&&records.Any(p=>BallGroup(p.Number)!=0&&BallGroup(p.Number)==Group(Turn));
  if(!keep)Turn=1-Turn;Opening=false;Phase=scratch?MatchPhase.Placement:MatchPhase.Aiming;
  Notice=scratch?"SCRATCH — incoming truck places the cue ball.":!valid?(FirstContact<0?"FOUL: no object contact — turn passes.":"FOUL: wrong first ball — turn passes."):keep?"Legal pot — keep your turn!":"No own ball pocketed — turn passes.";
  if(RespotEight)Notice+=" Break 8-ball respotted.";
 }
 public bool PracticePlacement(){if(!Practice||Phase!=MatchPhase.Aiming)return false;Phase=MatchPhase.Placement;Notice="Practice — reposition cue ball.";return true;}
 public void CompletePlacement(){if(Phase==MatchPhase.Placement){Pocketed.Remove(0);Phase=MatchPhase.Aiming;Notice="Cue ball placed. Release controls, then line up.";}}
 public void Forfeit(string reason){if(Phase==MatchPhase.Aiming){Turn=1-Turn;Notice="AI forfeits: "+reason;}}
}}



