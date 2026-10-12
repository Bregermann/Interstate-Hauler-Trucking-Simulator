using UnityEngine;
namespace LWS.TruckBilliards
{
 public sealed class BilliardsMatch:MonoBehaviour
 {
  public BilliardsBall[] balls;public BilliardsTruck player,ai;public BilliardsAI opponent;public Camera tableCamera;
  public BilliardsSession session;
  public Vector3[] pockets;public Transform preview;public AudioSource audioSource;public AudioClip impact,pocketSound;
  public float halfWidth=6,halfLength=12,pocketRadius=.85f,rollingResistance=.42f,settledSpeed=.09f,settledDuration=.8f;
  public readonly BilliardsRules rules=new BilliardsRules();
  public bool AreSettled{get;private set;} public bool InputReleased{get;private set;}
  public bool placementValid;public Vector3 placementPosition;public int strikeCount,resolutionCount,physicsStep;
  [System.NonSerialized] public bool smokeInput;
  public float openingBreakBoost=1.65f,maxBreakShotSpeed=24;public float LastStrikeSpeed{get;private set;}public bool LastStrikeOpening{get;private set;}
  float settleTimer,aiPlacementTimer;
  public void FreshMatch(int breaker=0)
  {
   foreach(var b in balls)b.Restore(b.initial);
   player.ResetTruck(breaker==0?new Vector3(0,0,-8.3f):new Vector3(4.4f,0,9.5f),breaker==0?Quaternion.identity:Quaternion.Euler(0,180,0));
   ai.ResetTruck(breaker==1?new Vector3(0,0,-8.3f):new Vector3(4.4f,0,9.5f),breaker==1?Quaternion.identity:Quaternion.Euler(0,180,0));
   rules.Reset(breaker,session!=null&&session.IsPractice);if(session!=null)session.ResetAIPlans();else opponent.ResetPlan();HidePlacement();
   settleTimer=aiPlacementTimer=0;AreSettled=false;InputReleased=false;strikeCount=resolutionCount=0;physicsStep=0;
   if(session!=null)session.input.GateAll();Debug.Log("TB_MATCH_RESET breaker="+breaker);
  }
  public void HidePlacement(){preview.gameObject.SetActive(false);}
  public void ResetTruck(int side)
  {
   if(session!=null&&(!session.GameplayEnabled||rules.Phase==MatchPhase.Placement))return;
   var truck=side==0?player:ai;
   truck.ResetTruck(new Vector3(side==0?-1.5f:1.5f,0,-9.5f),Quaternion.identity);
  }
  void FixedUpdate()
  {
   if(session!=null&&!session.GameplayEnabled)return;
   physicsStep++;bool low=true;
   foreach(var b in balls)if(!b.Pocketed&&b.body.linearVelocity.magnitude>settledSpeed){low=false;break;}
   settleTimer=low?settleTimer+Time.fixedDeltaTime:0;AreSettled=settleTimer>=settledDuration;
   if(rules.Phase==MatchPhase.Resolving&&AreSettled){
    foreach(var b in balls)if(!b.Pocketed){b.body.linearVelocity=Vector3.zero;b.body.angularVelocity=Vector3.zero;}
    int previous=rules.Turn;rules.Resolve();resolutionCount++;Debug.Log("TB_RESOLVE "+rules.ShotId+" "+rules.Notice);
    if(rules.RespotEight)balls[8].Restore(FindPlacement(8));
    if(session!=null)session.ShotResolved(previous);
    else if(rules.Phase==MatchPhase.Placement&&rules.Turn==1)PlaceCue(FindPlacement(0));
    aiPlacementTimer=0;
   }
   if(session!=null&&session.GameplayEnabled&&rules.Phase==MatchPhase.Placement&&session.IsAI(rules.Turn)){
    aiPlacementTimer+=Time.fixedDeltaTime;
    if(aiPlacementTimer>=1)PlaceCue(session.agents[rules.Turn].FindCuePlacement());
   }
  }
  public void Strike(BilliardsTruck truck,Vector3 velocity)
  {
   if(session!=null&&!session.GameplayEnabled)return;
   if(rules.Turn!=truck.side||!AreSettled||rules.Phase!=MatchPhase.Aiming)return;
   LastStrikeOpening=rules.Opening&&rules.ShotId==0;velocity=Vector3.ClampMagnitude(velocity*(LastStrikeOpening?openingBreakBoost:1),LastStrikeOpening?maxBreakShotSpeed:truck.maxShotSpeed);if(!rules.BeginShot())return;LastStrikeSpeed=velocity.magnitude;
   balls[0].body.WakeUp();balls[0].body.linearVelocity=velocity;settleTimer=0;AreSettled=false;strikeCount++;
   if(session!=null&&session.feedback!=null){if(LastStrikeOpening)session.feedback.BreakCue();else session.feedback.Impact(BilliardsSound.Strike,velocity.magnitude);session.feedback.Pulse(truck.side,LastStrikeOpening?.22f:.13f,LastStrikeOpening?.17f:.1f);if(LastStrikeOpening){session.celebration?.BreakBurst(session.players[truck.side].color);}}else if(impact!=null)audioSource.PlayOneShot(impact,.5f);
   Debug.Log("TB_STRIKE side="+truck.side+" shot="+rules.ShotId+" speed="+velocity.magnitude+" bumperDistance="+Vector3.Distance(truck.Bumper,balls[0].body.position));
  }
  public void Pocket(BilliardsBall ball)
  {
   int step=Mathf.RoundToInt(Time.fixedTime/Time.fixedDeltaTime);
   if(!rules.Pocket(ball.number,step))return;
   ball.SetPocketed(true);if(session!=null&&session.feedback!=null)session.feedback.Cue(BilliardsSound.Pocket);else if(pocketSound!=null)audioSource.PlayOneShot(pocketSound,.6f);Debug.Log("TB_POCKET "+ball.number+" step="+step);
  }
  public bool ValidPlacement(Vector3 p,int ignore)
  {
   if(Mathf.Abs(p.x)>halfWidth-.75f||Mathf.Abs(p.z)>halfLength-.75f)return false;
   foreach(var pocket in pockets)if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(pocket.x,pocket.z))<pocketRadius+.6f)return false;
   foreach(var b in balls)if(b.number!=ignore&&!b.Pocketed&&Vector3.Distance(p,b.body.position)<1.08f)return false;return true;
  }
  public Vector3 FindPlacement(int ignore)
  {
   Vector3 preferred=ignore==8?new Vector3(0,.5f,5):new Vector3(0,.5f,-6);
   if(ValidPlacement(preferred,ignore))return preferred;
   for(float z=-9;z<=9;z+=1.2f)for(float x=-4.8f;x<=4.8f;x+=1.2f){var p=new Vector3(x,.5f,z);if(ValidPlacement(p,ignore))return p;}
   throw new System.InvalidOperationException("No valid cue/8-ball respot location.");
  }
  public void PlaceCue(Vector3 p)
  {
   if(session!=null&&!session.GameplayEnabled)return;
   if(rules.Phase!=MatchPhase.Placement||!ValidPlacement(p,0))return;
   balls[0].Restore(p);rules.CompletePlacement();HidePlacement();player.Stop();ai.Stop();InputReleased=false;if(session!=null)session.ResetAIPlans();else opponent.ResetPlan();settleTimer=0;AreSettled=false;
   if(session!=null)session.input.GateAll();Debug.Log("TB_CUE_PLACED side="+rules.Turn);
  }
 }
}

