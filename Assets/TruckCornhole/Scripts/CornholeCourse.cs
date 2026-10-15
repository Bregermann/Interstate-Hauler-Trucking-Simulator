using UnityEngine;
using DG.Tweening;
using LWS.TruckBowling;
namespace LWS.TruckCornhole {
public sealed class CornholeCourse:MonoBehaviour {
 public CornholeMatch match;public int index;public float center;public Transform board,trampoline;public CornholeTruck[] trucks;public CornholeCamera chase;public CornholeReplay replay;public ParticleSystem launchEffect,holeEffect;
 public float holeZ=6,holeRadius=1.65f,launchUp=30,launchForwardScale=.65f,launchForwardAdd=1,aftertouchAcceleration=.65f,aftertouchBudget=1.6f;
 public CornholePhase Phase{get;private set;}=CornholePhase.Waiting;public int Attempts{get;private set;}public int RoundLocks{get;private set;}public int TimeoutCount{get;private set;}public string Notice{get;private set;}="";public CornholeTruck Current{get;private set;}
 public Vector3 StartPosition=>new Vector3(center,6.5f,-21);public BowlingPlayer Owner=>match.setup.players[index];public bool Done=>Phase==CornholePhase.Waiting;float elapsed,quiet,resultTimer,aiClock;Vector2 aiInput;float planX,planSpeed,planError;Tween compression;Vector3 trampolineRest;void Awake(){trampolineRest=trampoline.localPosition;}
 public float PhaseElapsed=>elapsed;
 public int Provisional{get{int score=0;for(int i=0;i<Attempts;i++)score+=trucks[i].Points;return score;}}
 public bool AllQuiet{get{for(int i=0;i<Attempts;i++)if(trucks[i].gameObject.activeSelf&&!trucks[i].Quiet)return false;return true;}}
 public void Clear(){if(replay!=null)replay.Clear();if(compression!=null)compression.Kill();trampoline.localScale=Vector3.one;trampoline.localPosition=trampolineRest;foreach(var t in trucks)t.Park();Current=null;Attempts=0;Phase=CornholePhase.Waiting;Notice="";elapsed=quiet=resultTimer=0;}
 public void BeginRound(){Clear();Spawn();}
 void Spawn(){if(Attempts>=CornholeRules.Deliveries){Phase=CornholePhase.Waiting;Notice="WAITING FOR OTHER COURSES";return;}Current=trucks[Attempts];Current.Spawn(Attempts);Attempts++;Phase=CornholePhase.Approach;elapsed=quiet=resultTimer=0;Notice="DRIVE DOWN THE RAMP · HIT THE TRAMPOLINE";aiInput=Vector2.zero;aiClock=0;float difficulty=(int)Owner.difficulty;float error=Mathf.Lerp(1.4f,.25f,difficulty/2);planX=Random.Range(-error,error);planSpeed=13.2f+Random.Range(-error,error)*.8f;planError=Random.Range(-error,error);if(Attempts>1){for(int i=0;i<Attempts-1;i++)if(trucks[i].Points==1){var p=board.InverseTransformPoint(trucks[i].body.position);if(Mathf.Abs(p.x)<holeRadius+1&&p.z<holeZ){planX=Mathf.Clamp(p.x,-1.1f,1.1f)+planError*.2f;break;}}}chase.NewAttempt();}
 public void Launched(){replay.Record();Phase=CornholePhase.Flight;elapsed=quiet=0;Notice="AFTERTOUCH · SMALL NUDGES ONLY";match.feedback.Play(2,0,true);Pulse(.35f,.18f);launchEffect.Play();if(compression!=null)compression.Kill();compression=DOTween.Sequence().Append(trampoline.DOLocalMoveY(trampolineRest.y-.25f,.07f)).Join(trampoline.DOScaleY(.3f,.07f)).Append(trampoline.DOLocalMoveY(trampolineRest.y,.22f).SetEase(Ease.OutBack)).Join(trampoline.DOScaleY(1,.22f));}
 public void Landed(){if(Phase==CornholePhase.Flight){Phase=CornholePhase.Settling;elapsed=quiet=0;Notice="PHYSICAL LANDING · CONTROLS OFF";}}
 public void Hole(CornholeTruck truck){match.feedback.Play(3,0,true);Pulse(.25f,.2f);holeEffect.Play();if(truck==Current){Phase=CornholePhase.Settling;elapsed=quiet=0;Notice="IN THE HOLE · +3 PROVISIONAL";}}
 public void Pulse(float strength,float time){if(Owner.source==BowlingSource.WholeController)match.feedback.Pulse(Owner.pad,strength,time);}
 void FixedUpdate(){if(!match.InMatch||match.Paused||match.Finished||match.RoundBreak||Done)return;float dt=Time.fixedDeltaTime;elapsed+=dt;for(int i=0;i<Attempts;i++){var missed=trucks[i];if(missed.gameObject.activeSelf&&missed.GroundDuration>1.2f&&missed.body.position.y< -7){missed.Exclude();missed.Park();}}
  if(Phase==CornholePhase.Approach||Phase==CornholePhase.Flight){if(Owner.source==BowlingSource.AI){aiClock-=dt;if(aiClock<=0){aiClock=Owner.difficulty==BowlingDifficulty.Easy?.22f:Owner.difficulty==BowlingDifficulty.Normal?.12f:.065f;aiInput=Plan();}Current.command=aiInput;}}
  if(Phase==CornholePhase.Approach){if(Current.body.position.y<-3||Mathf.Abs(Current.body.position.x-center)>10||Current.body.position.z>7||elapsed>60){Current.Exclude();Phase=CornholePhase.Settling;elapsed=quiet=0;Notice="MISSED THE TRAMPOLINE";}return;}
  if(Phase==CornholePhase.Flight&&(Current.body.position.y<-5||Current.Grounded||elapsed>15)){Current.Exclude();Phase=CornholePhase.Settling;elapsed=quiet=0;Notice="MISS";}
  if(Phase==CornholePhase.Settling){quiet=AllQuiet?quiet+dt:0;bool finished=Current.Holed||Current.Landed||Current.Grounded||Current.Excluded;if(finished&&quiet>=1.6f){FinishAttempt();}else if(elapsed>35){TimeoutCount++;for(int i=0;i<Attempts;i++)if(!trucks[i].Quiet)trucks[i].Exclude();Notice="RESOLUTION LIMIT · MOVING TRUCKS SCORE ZERO";FinishAttempt();}}
  if(Phase==CornholePhase.AttemptResult){resultTimer+=dt;if(resultTimer>3&&!replay.Playing)Spawn();}
 }
 Vector2 Plan(){var t=Current;if(Phase==CornholePhase.Approach){float x=t.body.position.x-center,heading=Vector3.SignedAngle(Vector3.forward,Vector3.ProjectOnPlane(t.transform.forward,Vector3.up),Vector3.up);float correction=Mathf.Clamp((planX-x)*.4f-heading*.035f,-.75f,.75f);float desired=planSpeed;float y=t.body.linearVelocity.z>desired+.35f?-.6f:t.body.linearVelocity.z<desired-.35f?1:.10f;return new Vector2(correction,y);}
  if(Phase!=CornholePhase.Flight)return Vector2.zero;var v=t.body.linearVelocity;float yTarget=board.TransformPoint(new Vector3(0,0,holeZ)).y;float discriminant=v.y*v.y+2*9.81f*Mathf.Max(0,t.body.position.y-yTarget);float time=(v.y+Mathf.Sqrt(discriminant))/9.81f;float predictedX=t.body.position.x+v.x*time-center;float predictedZ=t.body.position.z+v.z*time;float lateral=Mathf.Clamp((planX-predictedX)*.5f,-1,1);float range=Mathf.Clamp((board.position.z+holeZ-6+planError-predictedZ)*.12f,-1,1);if(Owner.difficulty==BowlingDifficulty.Easy){lateral*=.55f;range*=.4f;}return new Vector2(lateral,elapsed<.45f?0:range);
 }
 void FinishAttempt(){replay.Complete();Phase=CornholePhase.AttemptResult;Current.command=Vector2.zero;resultTimer=0;Notice=Current.Holed?"IN THE HOLE · 3":Current.Points==1?"ON BOARD · 1 PROVISIONAL":"MISS · 0";match.feedback.Play(4,0,true);}
 public void RoundLocksReset(){RoundLocks=TimeoutCount=0;}
 public int FinalizeRound(){RoundLocks++;return Provisional;}
 void OnDisable(){if(compression!=null)compression.Kill();}
}
}
