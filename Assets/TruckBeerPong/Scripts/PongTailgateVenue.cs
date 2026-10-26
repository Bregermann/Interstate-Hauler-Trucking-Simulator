using DG.Tweening;
using LWS.TruckFieldGoal;
using UnityEngine;

namespace LWS.TruckBeerPong
{
 // Presentation consumes a completed result; it never calls rules, changes a
 // cup, drives a vehicle, seeks replay data or advances a turn.
 public sealed class PongTailgateVenue:MonoBehaviour
 {
  public PongMatch match;public FieldGoalCrowd crowd;public Transform[] jugs;
  public Transform[] parkedVehicles,partyAreas;public AudioSource atmosphere;
  [Range(0,.08f)]public float jugChance=.08f;
  public int Reactions{get;private set;}public int Cheers{get;private set;}public int Boos{get;private set;}public int Tosses{get;private set;}
  public int LastShooter{get;private set;}=-1;
  public int LastShot{get;private set;}
  string matchId;System.Random random;int tossedShot;bool subscribed,paused;Vector3 missPoint;
  void OnEnable(){Bind();}
  void Start(){Bind();}
  void Bind(){if(subscribed||match==null)return;match.ShotResolved+=Resolved;subscribed=true;}
  void ClearJugs(){if(jugs!=null)foreach(var jug in jugs)if(jug!=null){jug.DOKill();jug.gameObject.SetActive(false);}}
  public void ResetPresentation()
  {
   ClearJugs();Reactions=Cheers=Boos=Tosses=LastShot=tossedShot=0;LastShooter=-1;paused=false;matchId=match.MatchId;random=new System.Random(match.seed);crowd?.ResetAttempt();if(atmosphere!=null)atmosphere.Stop();
  }
  void Resolved(int shooter,int shot,bool hit,Vector3 point)
  {
   if(match==null||!match.Live||!match.rules.ShotLocked)return;
   if(matchId!=match.MatchId)ResetPresentation();if(shot<=LastShot)return;
   ClearJugs();LastShot=shot;LastShooter=shooter;Reactions++;if(hit)Cheers++;else Boos++;missPoint=point;crowd.React(hit,match.Outcome,point);
   if(!hit&&random.NextDouble()<Mathf.Clamp(jugChance,0,.08f))TryJugToss();
  }
  public bool TryJugToss()
  {
   if(match==null||!match.Live||!match.rules.ShotLocked||match.rules.LastHit||match.Phase!=PongPhase.Aftermath&&match.Phase!=PongPhase.TurnResult||LastShot<=0||tossedShot==LastShot||jugs==null||jugs.Length==0||crowd.nearFans.Length==0)return false;
   // Reuse the installed Field Goal canister choreography. Collider-free art
   // tumbles toward the captured unsuccessful player only AFTER score lock.
   var actor=crowd.nearFans[LastShot%crowd.nearFans.Length];var jug=jugs[LastShot%jugs.Length];actor.CrossFade("Throw",.12f);jug.DOKill();jug.gameObject.SetActive(true);jug.position=actor.transform.position+Vector3.up*4.2f;var destination=missPoint;destination.y=Mathf.Max(destination.y,match.floor.bounds.max.y+.4f);tossedShot=LastShot;Tosses++;
   jug.DOJump(destination,5,1,1.1f).SetEase(Ease.Linear).OnComplete(()=>jug.gameObject.SetActive(false)).SetLink(jug.gameObject);jug.DORotate(new Vector3(720,190,240),1.1f,RotateMode.FastBeyond360).SetLink(jug.gameObject);return true;
  }
  void Update()
  {
   Bind();if(match==null)return;
   if(match.Running&&matchId!=match.MatchId)ResetPresentation();
   if(!match.Running||match.Finished){ClearJugs();if(atmosphere!=null)atmosphere.Stop();return;}
   if(!match.rules.ShotLocked||match.Current!=LastShooter||match.Phase==PongPhase.Replay)ClearJugs();
   if(match.Paused!=paused){paused=match.Paused;if(jugs!=null)foreach(var jug in jugs)if(paused)jug.DOPause();else jug.DOPlay();foreach(var fan in crowd.nearFans)fan.speed=paused?0:1;if(atmosphere!=null){if(paused)atmosphere.Pause();else atmosphere.UnPause();}}
   if(atmosphere!=null){atmosphere.volume=match.settings.sfx*.045f;if(!paused&&!atmosphere.isPlaying)atmosphere.Play();}
  }
  void OnDisable(){if(subscribed&&match!=null)match.ShotResolved-=Resolved;subscribed=false;ClearJugs();if(atmosphere!=null)atmosphere.Stop();}
 }
}
