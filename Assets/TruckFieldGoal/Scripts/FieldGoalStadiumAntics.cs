using System;
using UnityEngine;
using DG.Tweening;
using LWS.TruckTaxi;

namespace LWS.TruckFieldGoal {
// Bounded, pooled stadium choreography. No authority over the kick or score.
public sealed class FieldGoalStadiumAntics:MonoBehaviour {
 public FieldGoalMatch match;public FieldGoalCrowd crowd;
 public Transform[] jugs;public Transform referee;public TruckTaxiPedestrian invader;public Animator invaderAnimator;
 public ParticleSystem tackleBurst;
 [Range(0,.2f)]public float refereeChance=.045f;
 [Range(4,30)]public float secondsBetweenAntics=8;
 public int Jumps{get;private set;}public int Scuffles{get;private set;}public int Tosses{get;private set;}public int Tackles{get;private set;}
 public bool VignetteActive=>vignette!=null;
 public Vector3 InvaderPosition=>invader.IsRagdoll?invader.RagdollBodies[0].position:invader.transform.position;
 Vector3[] home;Quaternion[] facing;Animator[] actors;Sequence[] antics;Sequence vignette;System.Random random;float next;int serial;bool paused;
 Vector3[] velocity,angular;
 void Awake(){actors=crowd.nearFans;home=new Vector3[actors.Length];facing=new Quaternion[actors.Length];antics=new Sequence[actors.Length];for(int i=0;i<actors.Length;i++){home[i]=actors[i].transform.position;facing[i]=actors[i].transform.rotation;}random=new System.Random(match.seed);invader.gameObject.SetActive(true);ResetPresentation();}
 public void ResetPresentation(){
  vignette?.Kill();vignette=null;paused=false;
  if(antics!=null)for(int i=0;i<antics.Length;i++){antics[i]?.Kill();antics[i]=null;if(actors[i]!=null){actors[i].transform.SetPositionAndRotation(home[i],facing[i]);actors[i].speed=1;}}
  if(jugs!=null)foreach(var jug in jugs)if(jug!=null){jug.DOKill();jug.gameObject.SetActive(false);}
  if(invader!=null)invader.PrepareForPool();if(referee!=null){referee.DOKill();referee.gameObject.SetActive(false);}
  if(tackleBurst!=null)tackleBurst.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  next=Time.time+5;
 }
 public void AfterKick(){
  // Called only after the existing authoritative result lock succeeds.
  if(random==null||vignette!=null)return;
  if(random.NextDouble()<refereeChance)StartRefereeVignette();
 }
 public void PerformAntic(int kind){
  if(actors==null||actors.Length<2)return;
  int group=(serial++%(actors.Length/4))*4;var a=actors[group].transform;
  antics[group]?.Kill();a.SetPositionAndRotation(home[group],facing[group]);
  if(kind%3==0){Jumps++;actors[group].CrossFade("Cheer",.15f);antics[group]=DOTween.Sequence().Append(a.DOJump(home[group],1.4f,3,2.1f)).OnComplete(()=>{a.SetPositionAndRotation(home[group],facing[group]);antics[group]=null;});}
  else if(kind%3==1){
   Scuffles++;int other=group+1;var b=actors[other].transform;antics[other]?.Kill();b.SetPositionAndRotation(home[other],facing[other]);
   actors[group].CrossFade("Shout",.12f);actors[other].CrossFade("Shout",.12f);
   var mid=(home[group]+home[other])*.5f;var delta=(home[other]-home[group]).normalized;
   a.LookAt(b.position);b.LookAt(a.position);
   var sequence=DOTween.Sequence().Append(a.DOMove(mid-delta*.65f,.45f)).Join(b.DOMove(mid+delta*.65f,.45f))
    .Append(a.DOPunchRotation(new Vector3(0,15,18),.9f,4,.8f)).Join(b.DOPunchRotation(new Vector3(0,-15,-18),.9f,4,.8f))
    .Append(a.DOMove(home[group],.5f)).Join(b.DOMove(home[other],.5f)).Join(a.DORotateQuaternion(facing[group],.5f)).Join(b.DORotateQuaternion(facing[other],.5f))
    .OnComplete(()=>{a.SetPositionAndRotation(home[group],facing[group]);b.SetPositionAndRotation(home[other],facing[other]);antics[group]=antics[other]=null;});
   antics[group]=antics[other]=sequence;
  }else{
   int index=group/4%jugs.Length;var jug=jugs[index];jug.DOKill();jug.gameObject.SetActive(true);jug.position=a.position+Vector3.up*1.5f;var end=a.position+(a.position.x>0?Vector3.right:Vector3.left)*8;
   actors[group].CrossFade("Throw",.12f);Tosses++;
   // Closed lightweight props tumble into the outside concourse, never the kick lane.
   jug.DOJump(end,5,1,1.8f).SetEase(Ease.Linear).OnComplete(()=>jug.gameObject.SetActive(false));jug.DORotate(new Vector3(720,190,240),1.8f,RotateMode.FastBeyond360);
  }
 }
 public bool StartRefereeVignette(){
  if(vignette!=null||!match.Live||!match.rules.AttemptLocked)return false;
  var start=new Vector3(35,.05f,match.GoalZ-6);invader.Reactivate(start,Quaternion.Euler(0,-90,0),"stadium-pitch-invader",false);
  foreach(var body in invader.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
  foreach(var collider in invader.GetComponentsInChildren<Collider>(true))collider.enabled=false;
  invaderAnimator.enabled=true;invaderAnimator.speed=1;invaderAnimator.Play("Run",0,0);
  referee.gameObject.SetActive(true);referee.SetPositionAndRotation(new Vector3(48,0,start.z),Quaternion.Euler(0,-90,0));
  vignette=DOTween.Sequence().Append(invader.transform.DOMove(new Vector3(20,.05f,start.z),1.55f).SetEase(Ease.Linear))
   .Join(referee.DOMove(new Vector3(23,0,start.z),1.55f).SetEase(Ease.InQuad))
   .AppendCallback(()=>{
    if(invader.TryStrike(new Vector3(-16,0,0),invader.transform.position+Vector3.up)){
     Tackles++;foreach(var body in invader.RagdollBodies){body.detectCollisions=false;body.maxLinearVelocity=45;body.linearVelocity=new Vector3(-21,29,7);}
     tackleBurst.transform.position=invader.transform.position+Vector3.up;tackleBurst.Play();
     match.feedback.Play(1,12,true);
    }
   }).Append(referee.DOMove(new Vector3(20,0,start.z),.3f).SetEase(Ease.OutQuad))
   .AppendInterval(5).Append(referee.DOMove(new Vector3(48,0,start.z),1.8f).SetEase(Ease.InOutSine))
   .OnComplete(()=>{referee.gameObject.SetActive(false);vignette=null;});
  return true;
 }
 void Update(){
  if(actors==null)return;
  if(!match.Running){if(vignette!=null||invader.gameObject.activeSelf)ResetPresentation();return;}
  if(match.Paused!=paused){paused=match.Paused;
   foreach(var a in actors)a.speed=paused?0:1;
   foreach(var s in antics)if(paused)s?.Pause();else s?.Play();
   foreach(var jug in jugs)if(paused)jug.DOPause();else jug.DOPlay();
   if(paused)vignette?.Pause();else vignette?.Play();
   invaderAnimator.speed=paused?0:1;
   if(invader.IsRagdoll){int n=invader.RagdollBodies.Count;if(paused){velocity=new Vector3[n];angular=new Vector3[n];for(int i=0;i<n;i++){var b=invader.RagdollBodies[i];velocity[i]=b.linearVelocity;angular[i]=b.angularVelocity;b.isKinematic=true;}}else if(velocity!=null)for(int i=0;i<n;i++){var b=invader.RagdollBodies[i];b.isKinematic=false;b.linearVelocity=velocity[i];b.angularVelocity=angular[i];}}
   if(paused)tackleBurst.Pause();else if(tackleBurst.isPaused)tackleBurst.Play();
  }
  if(paused)return;
  if(Time.time>=next){next=Time.time+secondsBetweenAntics;PerformAntic(random.Next(3));}
  // Retire only after the launch has left the gameplay camera, with a bounded lifetime.
  if(invader.IsRagdoll&&invader.gameObject.activeSelf){var p=match.cameraRig.view.WorldToViewportPoint(InvaderPosition);if(InvaderPosition.y< -8||InvaderPosition.y>8&&(p.z<0||p.x<-.2f||p.x>1.2f||p.y>1.2f))invader.PrepareForPool();}
 }
 void OnDisable(){ResetPresentation();}
}
}
