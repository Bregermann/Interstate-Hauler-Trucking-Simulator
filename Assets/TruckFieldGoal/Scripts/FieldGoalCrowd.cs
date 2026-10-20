using System;using UnityEngine;using UnityEngine.Rendering;using DG.Tweening;
namespace LWS.TruckFieldGoal {
// Owned presentation using posed copies of the existing Taxi crowd. No collider
// or scoring authority. Distant fans use instancing rather than live animators.
public sealed class FieldGoalCrowd:MonoBehaviour {
 [Serializable]public sealed class Batch {public Mesh idle,cheer;public Material[] materials;public Matrix4x4[] seats;public MeshFilter[] fans;}
 public int authoredVersion;public FieldGoalMatch match;public LWS.TruckSports.SportsEventSession eventSession;
 LWS.TruckSports.SportsEventSession Session=>match!=null?match:eventSession;public Batch[] batches;public Animator[] nearFans;public Transform runner;public Animator runnerAnimator;public AudioSource response;public AudioClip cheer,boo;
 [Range(0,.2f)]public float runnerChance=.035f;
 public int Reactions{get;private set;}public int RunOns{get;private set;}public int Spectators{get{int total=nearFans==null?0:nearFans.Length;if(batches!=null)foreach(var b in batches)total+=b.seats.Length;return total;}}
 public string Mood{get;private set;}="IDLE";float reactionAge;Sequence run;bool soundPaused;
 void Start(){ResetAttempt();}
 public void ResetAttempt(){GetComponent<FieldGoalStadiumAntics>()?.ResetPresentation();run?.Kill();run=null;if(runner!=null)runner.gameObject.SetActive(false);if(response!=null)response.Stop();Mood="IDLE";reactionAge=0;soundPaused=false;SetPose(false);for(int i=0;i<nearFans.Length;i++)if(nearFans[i]!=null&&nearFans[i].HasState(0,Animator.StringToHash("Idle")))nearFans[i].Play("Idle",0,i*.137f%1);}
 public void React(bool good,string reason,Vector3 point){Reactions++;reactionAge=4.5f;bool close=!good&&match!=null&&(match.Projectile.PostContacts>0||Mathf.Abs(point.z-match.GoalZ)<3&&Mathf.Abs(point.x)<match.GoalHalfWidth+2);bool powerful=match!=null&&match.Projectile.LaunchVelocity.magnitude>27;Mood=good?(powerful?"ROAR":"CHEER"):close?"SO CLOSE":"BOO";SetPose(good);foreach(var fan in nearFans)if(fan!=null){string state=good?"Cheer":"Disappointed";if(fan.HasState(0,Animator.StringToHash(state)))fan.CrossFade(state,.18f);}
  if(response!=null){response.clip=good?cheer:boo;response.pitch=close?.86f:powerful?1.06f:1;response.volume=Session.settings.sfx*(good?.4f:close?.18f:.28f);if(response.clip!=null)response.Play();}
  GetComponent<FieldGoalStadiumAntics>()?.AfterKick();
 }
 public void RunOn(){if(runner==null||run!=null)return;RunOns++;runner.gameObject.SetActive(true);runner.position=new Vector3(34,0,-27);runner.rotation=Quaternion.Euler(0,270,0);if(runnerAnimator!=null)runnerAnimator.Play("Run",0,0);run=DOTween.Sequence().Append(runner.DOMove(new Vector3(24,0,-27),2).SetEase(Ease.Linear)).Append(runner.DOMove(new Vector3(24,0,7),5).SetEase(Ease.Linear)).Join(runner.DORotate(new Vector3(0,0,0),.35f)).Append(runner.DOMove(new Vector3(34,0,7),2).SetEase(Ease.Linear)).Join(runner.DORotate(new Vector3(0,90,0),.35f)).OnComplete(()=>{run=null;runner.gameObject.SetActive(false);});}
 void Update(){var session=Session;if(session==null)return;if(session.Paused){if(response!=null&&response.isPlaying)response.Pause();soundPaused=true;run?.Pause();return;}if(soundPaused){if(response!=null)response.UnPause();soundPaused=false;}if(run!=null&&!run.IsPlaying())run.Play();if(!session.Live){if(response!=null)response.Stop();return;}if(reactionAge>0){reactionAge-=Time.deltaTime;if(reactionAge<=0){SetPose(false);Mood="IDLE";foreach(var fan in nearFans)if(fan!=null)fan.CrossFade("Idle",.3f);}}if(response!=null)response.volume=Session.settings.sfx*(Mood=="BOO"?.28f:Mood=="SO CLOSE"?.18f:.4f);}
 void SetPose(bool celebrating){if(batches==null)return;foreach(var batch in batches)if(batch.fans!=null)foreach(var fan in batch.fans)if(fan!=null)fan.sharedMesh=celebrating?batch.cheer:batch.idle;}
 void OnDisable(){run?.Kill();run=null;if(runner!=null)runner.gameObject.SetActive(false);if(response!=null)response.Stop();}
}
}
