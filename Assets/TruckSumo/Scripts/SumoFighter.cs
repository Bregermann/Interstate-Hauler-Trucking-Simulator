using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckSumo {
public enum SumoFace{Neutral,Focus,Shove,Impact,Worried,Relief,Victory,Eliminated}
public sealed class SumoFighter:MonoBehaviour {
 public AudioSource strain;public SportsEventTruck truck;public SumoMatch match;public Transform[] brows,tails;public Renderer[] trim;public float cooldown;public SumoFace Face{get;private set;}public int Shoves{get;private set;}public bool ShovingThisStep{get;private set;}public float LastNormalSpeed{get;private set;}public Vector3 IncomingVelocity{get;private set;}public Vector3 IncomingPosition{get;private set;}
 // IncomingVelocity includes an already queued shove for contact attribution;
 // updating that observation never applies a second force to the Rigidbody.
 float hold;int priority;bool edgeWarning;Quaternion[] browRest,tailRest;float contactAt=-10;MaterialPropertyBlock block;
 public void ResetFighter(Vector3 position,Quaternion rotation){truck.Prepare(position,rotation);IncomingVelocity=Vector3.zero;IncomingPosition=truck.body.worldCenterOfMass;cooldown=hold=0;priority=0;edgeWarning=false;Shoves=0;ShovingThisStep=false;Face=SumoFace.Neutral;if(browRest==null){browRest=new Quaternion[brows.Length];for(int i=0;i<brows.Length;i++)browRest[i]=brows[i].localRotation;tailRest=new Quaternion[tails.Length];for(int i=0;i<tails.Length;i++)tailRest[i]=tails[i].localRotation;}for(int i=0;i<brows.Length;i++)brows[i].localRotation=browRest[i];block??=new MaterialPropertyBlock();block.SetColor("_BaseColor",match.setup.players[truck.slot].color);foreach(var renderer in trim)renderer.SetPropertyBlock(block);}
 public void Expression(SumoFace face,int rank,float seconds){if(hold>0&&rank<priority)return;Face=face;priority=rank;hold=seconds;}
 public void Tick(Vector2 input,bool shove,float dt){ShovingThisStep=false;IncomingVelocity=truck.body.linearVelocity;IncomingPosition=truck.body.worldCenterOfMass;cooldown=Mathf.Max(0,cooldown-dt);hold=Mathf.Max(0,hold-dt);if(hold==0)priority=0;truck.TickDrive(input);var centre=truck.body.worldCenterOfMass;float danger=new Vector2(centre.x,centre.z).magnitude;if(danger>match.Radius-2.5f){edgeWarning=true;Expression(SumoFace.Worried,2,.12f);}else if(edgeWarning&&danger<match.Radius-3){edgeWarning=false;Expression(SumoFace.Relief,3,.65f);}else if(priority==0)Expression(input.sqrMagnitude>.15f?SumoFace.Focus:SumoFace.Neutral,0,0);
  if(shove&&cooldown<=0&&truck.Grounded&&Vector3.Dot(truck.body.rotation*Vector3.up,Vector3.up)>.45f){cooldown=3.2f;Shoves++;var forward=truck.body.rotation*Vector3.forward;float addition=Mathf.Clamp(13-Vector3.Dot(truck.body.linearVelocity,forward),0,3.8f);truck.body.AddForce(forward*addition,ForceMode.VelocityChange);IncomingVelocity+=forward*addition;ShovingThisStep=addition>.001f;Expression(SumoFace.Shove,4,.6f);match.Shoved(truck.slot);}
 }
 void LateUpdate(){ApplyFace(Time.deltaTime);}
 public void SnapFace(){ApplyFace(1);}
 void ApplyFace(float dt){if(browRest==null)return;float amount=Face==SumoFace.Shove?28:Face==SumoFace.Focus?14:Face==SumoFace.Worried?-24:Face==SumoFace.Impact?-35:Face==SumoFace.Victory?-18:Face==SumoFace.Eliminated?-32:Face==SumoFace.Relief?-10:5;for(int i=0;i<brows.Length;i++){float side=i%2==0?-1:1;var target=browRest[i]*Quaternion.Euler(0,0,amount*side);brows[i].localRotation=Quaternion.Slerp(brows[i].localRotation,target,1-Mathf.Exp(-12*dt));}for(int i=0;i<tails.Length;i++)tails[i].localRotation=tailRest[i]*Quaternion.Euler(Mathf.Sin(Time.time*7+i)*Mathf.Min(12,truck.body.linearVelocity.magnitude*1.5f),0,0);}
 void Update(){if(strain==null)return;float volume=match.Live&&match.Stage==SumoStage.Fight&&match.rules.alive[truck.slot]&&Application.isFocused&&truck.Grounded&&Time.time-contactAt<.25f&&Mathf.Abs(truck.command.y)>.5f&&truck.body.linearVelocity.magnitude<2?match.settings.sfx*.07f:0;strain.volume=volume;if(volume>.001f){if(!strain.isPlaying)strain.Play();}else strain.Stop();}
 public void StopAudio(){if(strain!=null)strain.Stop();if(truck!=null)truck.StopSound();}
 void OnDisable(){StopAudio();}
 void OnCollisionStay(Collision collision){if(collision.rigidbody!=null)contactAt=Time.time;match.RecordContact(truck.slot,collision,true);}
 void OnCollisionEnter(Collision collision){if(collision.rigidbody!=null)contactAt=Time.time;if(!match.Live||match.Stage!=SumoStage.Fight||!match.rules.alive[truck.slot])return;float normalSpeed=match.RecordContact(truck.slot,collision,false);LastNormalSpeed=normalSpeed;if(collision.rigidbody!=null&&normalSpeed>2.5f){Expression(SumoFace.Impact,5,.45f);match.Impacted(truck.slot,normalSpeed);}}
}
}
