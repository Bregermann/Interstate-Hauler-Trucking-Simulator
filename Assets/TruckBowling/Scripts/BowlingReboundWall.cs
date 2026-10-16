using UnityEngine;using DG.Tweening;
namespace LWS.TruckBowling {
// Only real pin/back-wall contacts enter this policy. PhysX already responded;
// apply the delta to one bounded desired return, never a second full bounce.
public sealed class BowlingReboundWall:MonoBehaviour {
 public BowlingLane lane;public Transform bed;public float returnMultiplier=1.18f,maxReturnSpeed=11.5f,energyRatio=1.35f,extraSpeedSquared=4,debounce=.55f;public int Returns{get;private set;}public float LastReturnAt{get;private set;}=-100;public Vector3 LastIncoming{get;private set;}public Vector3 LastOutgoing{get;private set;}public float LastNormalSpeed{get;private set;}
 readonly float[] ready=new float[10];Tween motion;Vector3 restScale;
 void Awake(){restScale=bed.localScale;for(int i=0;i<ready.Length;i++)ready[i]=-100;}
 public bool Contact(BowlingPin pin,Collision collision){if(!enabled||pin==null||pin.lane!=lane||!lane.ScoringLive||lane.match.Paused||pin.body.isKinematic||Time.time<ready[pin.index])return false;var incoming=pin.IncomingVelocity;float normal=0;for(int i=0;i<collision.contactCount;i++){var contact=collision.GetContact(i);if(contact.normal.z<-.7f)normal=Mathf.Max(normal,Vector3.Dot(incoming,-contact.normal));}if(normal<.8f||incoming.z<.6f)return false;
  var desired=new Vector3(Mathf.Clamp(incoming.x*.94f,-5.5f,5.5f),Mathf.Clamp(incoming.y,-.5f,2.3f),-Mathf.Clamp(incoming.z*returnMultiplier+1.0f,3,11));if(pin.body.position.y<.15f)desired.y=Mathf.Max(desired.y,2.5f);float square=Mathf.Min(maxReturnSpeed*maxReturnSpeed,incoming.sqrMagnitude*energyRatio+extraSpeedSquared);desired=Vector3.ClampMagnitude(desired,Mathf.Sqrt(square));pin.body.AddForce(desired-pin.body.linearVelocity,ForceMode.VelocityChange);pin.body.angularVelocity=Vector3.ClampMagnitude(pin.body.angularVelocity,10);ready[pin.index]=Time.time+debounce;LastIncoming=incoming;LastOutgoing=desired;LastNormalSpeed=normal;LastReturnAt=Time.time;Returns++;lane.match.feedback.Play(9,Mathf.Max(4,normal));if(motion!=null&&motion.IsActive())motion.Kill();bed.localScale=restScale;motion=bed.DOScaleZ(restScale.z*.65f,.07f).SetLoops(2,LoopType.Yoyo).SetEase(Ease.OutSine).SetUpdate(UpdateType.Fixed);return true;
 }
 public void ResetWall(){if(motion!=null&&motion.IsActive())motion.Kill();if(bed!=null){if(restScale==Vector3.zero)restScale=bed.localScale;bed.localScale=restScale;}for(int i=0;i<ready.Length;i++)ready[i]=-100;LastReturnAt=-100;}
 void OnDisable(){ResetWall();}
}
}
