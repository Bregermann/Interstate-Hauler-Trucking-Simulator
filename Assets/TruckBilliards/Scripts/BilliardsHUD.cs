using System.Text;
using UnityEngine;
using Michsky.UI.Heat;
namespace LWS.TruckBilliards
{
 public sealed class BilliardsHUD:MonoBehaviour
 {
  public BilliardsMatch match;public TMPro.TMP_Text banner,status,playerPanel,aiPanel,notice,speed,controls,scoreboard;
  // Legacy fields kept to preserve serialized compatibility; upgraded menus own their replacement.
  public GameObject overlay;public TMPro.TMP_Text overlayTitle,overlayBody;public ButtonManager playButton;
  public void Play(){if(match.session!=null)match.session.StartMatch();else match.FreshMatch();}
  float next;TMPro.TMP_Text[] liveTexts;readonly StringBuilder numbers=new StringBuilder(96);
  void Awake(){liveTexts=new[]{banner,playerPanel,aiPanel,notice,speed,scoreboard};}
  void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;Refresh();}
  public void Refresh()
  {
   var r=match.rules;var session=match.session;
   if(overlay!=null)overlay.SetActive(false);
   if(session==null)return;
   bool running=session.InMatch;
   bool display=running&&session.Screen==BilliardsScreen.Match;
   foreach(var text in liveTexts)if(text!=null)text.gameObject.SetActive(display);
   if(status!=null)status.gameObject.SetActive(false);if(controls!=null)controls.gameObject.SetActive(false);if(scoreboard!=null)scoreboard.text=session.IsPractice?"PRACTICE":session.series.Wins[0]+" : "+session.series.Wins[1]+" • RACK "+session.series.RackNumber+" • FIRST TO "+session.series.Required;if(session.IsPractice)aiPanel.gameObject.SetActive(false);
   banner.text=running?r.Phase==MatchPhase.Ended?session.Name(r.Winner)+" WINS":session.IsPractice?"PRACTICE":session.Name(r.Turn).ToUpper()+" — TURN":"TRUCK BILLIARDS";
   banner.color=running?session.players[r.Phase==MatchPhase.Ended?r.Winner:r.Turn].color:Color.white;
   status.text=!running?"MINI TRUCKS. REAL POOL.":r.Phase==MatchPhase.Resolving?"BALLS MOVING — BUMPERS DISARMED":r.Phase==MatchPhase.Placement?
    (session.IsAI(r.Turn)?"AI PLACING CUE BALL":"PLACE CUE BALL • "+session.Bindings(r.Turn,true)):
    session.IsAI(r.Turn)?"AI LINING UP":"LINE UP YOUR SHOT";
   playerPanel.text=Panel(0);aiPanel.text=Panel(1);playerPanel.color=session.players[0].color;aiPanel.color=session.players[1].color;
   notice.text=running?status.text+" • "+r.Notice:"Choose a mode. Line up. Drive your bumper into the white ball.";
   int side=session.mode==BilliardsMode.HotSeat?session.sharedOwner:r.Turn;var truck=session.players[side].truck;
   speed.text="SPEED "+Mathf.Abs(truck.Speed).ToString("0.0")+" • POWER "+Mathf.RoundToInt(Mathf.Clamp01(truck.Speed*truck.powerScale/truck.maxShotSpeed)*100)+"% • "+(truck.Armed?"ARMED":"DISARMED");
   if(controls!=null)controls.text=session.IsPractice?session.Bindings(0)+"\nEsc / Start: Reset Rack • Reposition Cue • Main Menu":session.mode==BilliardsMode.SharedKeyboard?session.Name(0)+": "+session.Bindings(0)+"\n"+session.Name(1)+": "+session.Bindings(1):session.Bindings(side);
  }
  string Panel(int side)
  {
   var r=match.rules;int group=r.Group(side);string label=group==0?"OPEN TABLE / UNASSIGNED":group==1?"SOLIDS":"STRIPES";
   numbers.Clear();for(int n=1;n<=15;n++)if(!r.Pocketed.Contains(n)&&(match.session.IsPractice||(group==0?n!=8:BilliardsRules.BallGroup(n)==group)))numbers.Append(n).Append(' ');string nums=numbers.ToString().TrimEnd();if(group!=0&&r.Remaining(side)==0)nums="8-BALL NEXT [8]";
   return match.session.IsPractice?"PRACTICE • POCKETS STAY DOWN\n"+nums:match.session.Name(side)+"\n"+label+" • "+nums;
  }
 }
}



