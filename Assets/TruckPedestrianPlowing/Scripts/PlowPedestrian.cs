using UnityEngine;
using UnityEngine.AI;
using TMPro;
using LWS.TruckTaxi;
namespace LWS.TruckPedestrianPlowing {
 public sealed class PlowPedestrian:MonoBehaviour {
  public TruckTaxiPedestrian ragdoll;public NavMeshAgent agent;public Animator animator;
  public bool useDistanceRendering=true;public LODGroup presentationLOD;public Renderer distanceModel;public Renderer[] detailedRenderers=System.Array.Empty<Renderer>();
  public Transform hips;public Renderer vest,costumeNose;public TMP_Text frontBadge,backBadge,beacon;public GameObject goldSymbols,stopSymbols;
  public PlowMatch match;public int id;public PlowTargetKind role;public string characterName,panicLine;public TruckTaxiDialogueLine[] reactions=System.Array.Empty<TruckTaxiDialogueLine>();
  public bool Claimed{get;private set;}public bool RetireRequested{get;private set;}
  public Vector3 Velocity=>agent!=null&&agent.enabled&&agent.isOnNavMesh?agent.velocity:Vector3.zero;
  public Vector3 DisplayPosition=>Claimed&&hips!=null?hips.position:transform.position+Vector3.up;
  public float ClaimedAt{get;private set;}float nextThink,nextDestination,nextBark,collectionTime,calmUntil,nextCalm;bool collecting;Vector3 collectionStart,originalScale;int state=-1;
  static readonly int[] states={Animator.StringToHash("idle1"),Animator.StringToHash("talk1"),Animator.StringToHash("cheer"),Animator.StringToHash("walk"),Animator.StringToHash("run")};
  MaterialPropertyBlock block;
  public bool Resting{get;private set;}Mesh restMesh,originalDistanceMesh;
  void Awake(){ragdoll.Expired+=Expired;block=new MaterialPropertyBlock();}
  void Expired(TruckTaxiPedestrian value){RetireRequested=true;}
  public void ResetTarget(PlowMatch owner,int targetId,PlowTargetKind kind,Vector3 position,Quaternion rotation,string label,string reaction){
   if(restMesh!=null){distanceModel.GetComponent<MeshFilter>().sharedMesh=originalDistanceMesh;Destroy(restMesh);restMesh=null;}Resting=false;
   if(presentationLOD!=null){presentationLOD.enabled=useDistanceRendering;foreach(var r in detailedRenderers)r.enabled=true;distanceModel.enabled=useDistanceRendering;}match=owner;id=targetId;role=kind;characterName=label;panicLine=reaction;Claimed=RetireRequested=collecting=false;state=-1;transform.localScale=Vector3.one;
   if(agent.enabled)agent.enabled=false;
   ragdoll.Reactivate(position,rotation,"plowing-"+targetId,true);
   GetComponent<Rigidbody>().isKinematic=true;
   agent.enabled=true;agent.Warp(position);agent.isStopped=false;agent.speed=owner.crowdSettings.walkSpeed;agent.acceleration=7;agent.angularSpeed=280;
   Color c=kind==PlowTargetKind.BonusRunner?new Color(1,.68f,.04f):kind==PlowTargetKind.PenaltyMarshal?new Color(.95f,.045f,.06f):new Color(.12f,.7f,.85f);
   block.SetColor("_BaseColor",c);block.SetColor("_Color",c);vest.SetPropertyBlock(block);if(costumeNose!=null)costumeNose.gameObject.SetActive(kind==PlowTargetKind.Standard);
   string badge=kind==PlowTargetKind.BonusRunner?"+100":kind==PlowTargetKind.PenaltyMarshal?"−100":"+10";
   frontBadge.text=backBadge.text=badge;if(goldSymbols!=null)goldSymbols.SetActive(kind==PlowTargetKind.BonusRunner);if(stopSymbols!=null)stopSymbols.SetActive(kind==PlowTargetKind.PenaltyMarshal);beacon.text=kind==PlowTargetKind.BonusRunner?"STAR 100":kind==PlowTargetKind.PenaltyMarshal?"STOP −100":"+10";beacon.color=c;
   nextThink=Time.time+(id%17)/17f;nextDestination=0;calmUntil=0;nextCalm=Time.time+id%7;nextBark=Time.time+id%4;
  }
  public void Tick(float dt){
   if(Claimed){if(match.crowdSettings.keepRagdolls){if(!Resting&&Time.time-ClaimedAt>6&&Settled)Rest();return;}if(collecting){collectionTime+=dt;float t=Mathf.Clamp01(collectionTime/.9f);transform.localScale=Vector3.Lerp(originalScale,Vector3.zero,t*t);transform.position=collectionStart-Vector3.up*t*.4f;if(t>=1)ragdoll.PrepareForPool();return;}if(Time.time-ClaimedAt>6&&Settled){foreach(var body in ragdoll.RagdollBodies)body.Sleep();}if(gameObject.activeSelf&&RetireRequested){if(match.SafeToRetire(DisplayPosition))ragdoll.PrepareForPool();else if(Settled||Time.time-ClaimedAt>30)Collect();}return;}
   if(!agent.enabled||!agent.isOnNavMesh)return;
   if(Time.time<nextThink)return;nextThink=Time.time+1/match.crowdSettings.crowdThinkRate;
   Vector3 escape=Vector3.zero;float danger=0;
   for(int i=0;i<match.setup.count;i++){var t=match.trucks[i];Vector3 delta=transform.position-t.body.position;delta.y=0;float distance=delta.magnitude;
    if(distance>match.crowdSettings.threatRadius||distance<.01f)continue;
    float closing=Vector3.Dot(t.body.linearVelocity,delta/distance);if(closing<1)continue;
    if(Physics.Linecast(transform.position+Vector3.up,t.body.position+Vector3.up,1,QueryTriggerInteraction.Ignore))continue;
    float weight=(match.crowdSettings.threatRadius-distance)*Mathf.Clamp01(closing/8);danger+=weight;escape+=delta.normalized*weight;
   }
   bool panic=danger>.2f||match.NearImpact(transform.position);if(panic){calmUntil=0;agent.isStopped=false;}else if(!agent.pathPending&&agent.remainingDistance<.6f&&Time.time>=nextCalm){calmUntil=Time.time+Random.Range(2,5);nextCalm=calmUntil+Random.Range(8,14);}bool social=!panic&&Time.time<calmUntil;agent.isStopped=social;
   agent.speed=panic?(role==PlowTargetKind.BonusRunner?match.crowdSettings.bonusSpeed:match.crowdSettings.panicSpeed):match.crowdSettings.walkSpeed;
   if(!social&&(panic||Time.time>=nextDestination||!agent.pathPending&&agent.remainingDistance<.5f)){
    Vector3 desired=panic?transform.position+escape.normalized*9:match.RandomDestination(transform.position,18);
    if(match.TryRoute(transform.position,desired,out var destination))agent.SetDestination(destination);
    nextDestination=Time.time+(panic?.7f:Random.Range(2.5f,6));
   }
   if(panic&&Time.time>nextBark){nextBark=Time.time+Random.Range(7,13);match.Reaction(this);}
   bool close=match.NearView(DisplayPosition,match.crowdSettings.animationDetailDistance);if(presentationLOD!=null&&presentationLOD.enabled&&detailedRenderers.Length>0)close&=detailedRenderers[0].isVisible;
   animator.enabled=close;if(close){int next=agent.velocity.sqrMagnitude>.15f?(panic?4:3):id%3;if(next!=state){state=next;animator.CrossFade(states[next],.17f);}}
   beacon.gameObject.SetActive(role!=PlowTargetKind.Standard||match.Round.PositiveRemaining<=5);
   if(beacon.gameObject.activeSelf)beacon.transform.rotation=Quaternion.Euler(90,0,0);
  }
  public bool Strike(Vector3 relative,Vector3 point){if(Claimed)return false;Claimed=true;if(presentationLOD!=null){presentationLOD.enabled=false;distanceModel.enabled=false;foreach(var r in detailedRenderers)r.enabled=true;}ClaimedAt=Time.time;agent.enabled=false;animator.enabled=true;beacon.gameObject.SetActive(false);return ragdoll.TryStrike(relative,point);}
  public bool Settled{get{foreach(var body in ragdoll.RagdollBodies)if(!body.isKinematic&&(body.linearVelocity.sqrMagnitude>.08f||body.angularVelocity.sqrMagnitude>.12f))return false;return true;}}
  public bool Collect(){if(collecting||!Claimed)return false;collecting=true;collectionTime=0;collectionStart=transform.position;originalScale=transform.localScale;foreach(var body in ragdoll.RagdollBodies)body.isKinematic=true;foreach(var collider in ragdoll.GetComponentsInChildren<Collider>(true))collider.enabled=false;return true;}
  public bool Rest(){
   if(Resting||!Claimed||!Settled)return false;
   foreach(var body in ragdoll.RagdollBodies){if(!body.isKinematic){body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;}}
   foreach(var collider in ragdoll.GetComponentsInChildren<Collider>(true))collider.enabled=false;
   Resting=true;
   if(distanceModel!=null&&useDistanceRendering){originalDistanceMesh=distanceModel.GetComponent<MeshFilter>().sharedMesh;restMesh=PlowRestPose.Bake(this);if(restMesh!=null){foreach(var renderer in detailedRenderers)renderer.enabled=false;distanceModel.GetComponent<MeshFilter>().sharedMesh=restMesh;distanceModel.enabled=true;}}
   return true;
  }
  public void ReactionCooldown(float seconds){nextBark=Mathf.Max(nextBark,Time.time+seconds);}
  void OnCollisionEnter(Collision collision){if(!Claimed&&match!=null)match.Contact(this,collision);}
  void OnDestroy(){if(ragdoll!=null)ragdoll.Expired-=Expired;if(restMesh!=null)Destroy(restMesh);}
 }
}
