using UnityEngine;
using LWS.TruckHockey;
namespace LWS.TruckAirHockey {
public sealed class AirHockeyAI:MonoBehaviour {
 public AirHockeyMatch match;public AirHockeyTruck truck;public Vector3 Target{get;private set;}public int Decisions{get;private set;}
 float next,error,stuck,recover,backoff,lastGas,bankUntil;int contacts;bool bankAttack;Vector3 previous,bankAnchor,bankDirection;
 public void ResetPlan(){next=error=stuck=recover=backoff=lastGas=bankUntil=0;bankAttack=false;contacts=truck.PuckContacts;previous=truck.body.position;Target=previous;truck.Stop();}
 readonly RaycastHit[] forecastHits=new RaycastHit[64];
 public Vector3 Predict(Vector3 p,Vector3 v,float time){v.y=0;float remaining=v.magnitude*time;var direction=v.sqrMagnitude>.001f?v.normalized:Vector3.forward;for(int bounce=0;bounce<4&&remaining>.001f;bounce++){int n=Physics.SphereCastNonAlloc(p,match.radius*.97f,direction,forecastHits,remaining,~0,QueryTriggerInteraction.Ignore);var hits=n==forecastHits.Length?Physics.SphereCastAll(p,match.radius*.97f,direction,remaining,~0,QueryTriggerInteraction.Ignore):forecastHits;if(n==forecastHits.Length)n=hits.Length;int nearest=-1;float distance=remaining;for(int i=0;i<n;i++){var c=hits[i].collider;if(c.attachedRigidbody!=null||(!c.name.StartsWith("Rail")&&!c.name.StartsWith("Goal"))||hits[i].distance<.002f||Vector3.Dot(direction,hits[i].normal)>=0)continue;if(hits[i].distance<distance){nearest=i;distance=hits[i].distance;}}if(nearest<0){p+=direction*remaining;break;}p+=direction*distance+hits[nearest].normal*.005f;remaining-=distance;direction=Vector3.Reflect(direction,hits[nearest].normal);direction.y=0;direction.Normalize();}p=AirHockeyArena.Project(p,match.goalLine,match.halfWidth,match.cornerRadius,match.radius+.18f);p.y=.36f;return p;}
 void FixedUpdate(){if(!match.rules.Live||match.MenuOpen||match.players[truck.slot].source!=HockeySource.AI)return;var p=truck.body.position;float dt=Time.fixedDeltaTime;if(truck.PuckContacts!=contacts){contacts=truck.PuckContacts;backoff=.7f;stuck=0;bankAttack=false;}if(backoff>0){backoff-=dt;truck.Control(new Vector2(0,-(lastGas<0?-1:1)*.6f));return;}if(bankAttack&&Time.time>bankUntil){bankAttack=false;recover=.8f;}if((p-previous).sqrMagnitude<.0003f&&Mathf.Abs(truck.throttle)>.3f)stuck+=dt;else stuck=Mathf.Max(0,stuck-dt);previous=p;if(stuck>2){recover=1;stuck=0;bankAttack=false;}if(recover>0){recover-=dt;truck.Control(new Vector2(truck.slot==0?.65f:-.65f,-.6f));return;}
  var difficulty=match.players[truck.slot].difficulty;float reaction=difficulty==HockeyDifficulty.Easy?.32f:difficulty==HockeyDifficulty.Hard?.07f:.15f,prediction=difficulty==HockeyDifficulty.Easy?.12f:difficulty==HockeyDifficulty.Hard?.6f:.35f,aimError=difficulty==HockeyDifficulty.Easy?.5f:difficulty==HockeyDifficulty.Hard?.04f:.18f;
  int own=truck.slot==0?-1:1;if(Time.time>=next){var puck=Predict(match.puck.position,match.puck.linearVelocity,prediction);var opponentGoal=new Vector3(-own*match.goalLine,.36f,0);var aim=(opponentGoal-puck).normalized;
   if(puck.x*own< -match.radius+.1f&&match.puck.position.x*own< -match.radius+.1f){bankAttack=false;Target=new Vector3(own*(match.goalLine-2.2f),.36f,Mathf.Clamp(puck.z*.65f,-match.goalHalfWidth+1,match.goalHalfWidth-1));}
   else if(puck.x*own>match.goalLine-2||Mathf.Abs(puck.z)>match.halfWidth-1.2f){
    // A perpendicular ram can pin the puck against a rail. A shallow angled
    // stroke lets the oblong truck's side make an inward-facing physical contact.
    Vector3 normal,tangent;float cx=match.goalLine-match.cornerRadius,cz=match.halfWidth-match.cornerRadius;
    if(Mathf.Abs(puck.x)>cx&&Mathf.Abs(puck.z)>cz){normal=(puck-new Vector3(Mathf.Sign(puck.x)*cx,puck.y,Mathf.Sign(puck.z)*cz)).normalized;tangent=Vector3.Cross(Vector3.up,normal);if(Vector3.Dot(tangent,opponentGoal-puck)>0)tangent=-tangent;}
    else if(Mathf.Abs(puck.z)>match.halfWidth-1.2f){normal=new Vector3(0,0,Mathf.Sign(puck.z));tangent=new Vector3(puck.x*own>match.goalLine-2?-own:own,0,0);}else{normal=new Vector3(own,0,0);tangent=new Vector3(0,0,puck.z>=0?1:-1);}
    var behind=puck-normal*2.2f+tangent;
    behind.x=own*Mathf.Clamp(behind.x*own,.7f,match.goalLine-1.2f);behind.z=Mathf.Clamp(behind.z,-match.halfWidth+1.2f,match.halfWidth-1.2f);behind=AirHockeyArena.Project(behind,match.goalLine,match.halfWidth,match.cornerRadius,1.2f);
    if(bankAttack&&Vector3.Distance(puck,bankAnchor)>1.4f)bankAttack=false;
    if(!bankAttack&&Vector3.Distance(p,behind)<.65f){bankAttack=true;bankAnchor=puck;bankDirection=(normal-tangent*.25f).normalized;bankUntil=Time.time+2.2f;}
    Target=bankAttack?puck+normal*.9f+tangent*.45f:behind;
   }
   else{bankAttack=false;float ahead=Vector3.Dot(p-puck,aim);if(ahead>-.7f){var lateral=Vector3.Cross(Vector3.up,aim);Target=puck-aim*1.8f+lateral*(Vector3.Dot(p-puck,lateral)>=0?1.7f:-1.7f);}else Target=puck+aim*1.3f;}
   // Approach the rails closely enough for a nose or corner bank. The truck's
   // rotation-aware whole-body projection, not this steering target, owns containment.
   Target+=new Vector3(Random.Range(-aimError,aimError),0,Random.Range(-aimError,aimError));Target=new Vector3(own*Mathf.Clamp(Target.x*own,.65f,match.goalLine-.53f),.36f,Mathf.Clamp(Target.z,-match.halfWidth+.53f,match.halfWidth-.53f));if(!bankAttack)Target=AirHockeyArena.Project(Target,match.goalLine,match.halfWidth,match.cornerRadius,1.2f);error=Random.Range(-aimError,aimError)*5;next=Time.time+reaction;Decisions++;}
  Vector3 delta=bankAttack?bankDirection*3:Target-p-truck.body.linearVelocity*.16f;delta.y=0;float angle=Vector3.SignedAngle(truck.transform.forward,delta,Vector3.up)+error;bool reverse=Mathf.Abs(angle)>110;if(reverse)angle=Mathf.DeltaAngle(0,angle-180);float gas=Mathf.Clamp(delta.magnitude*.55f,.18f,.95f);float speed=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);if(delta.magnitude<.6f&&Mathf.Abs(speed)>1)gas=0;lastGas=(reverse?-1:1)*gas;truck.Control(new Vector2(Mathf.Clamp(angle/35,-1,1)*(reverse?-1:1),lastGas));
 }
}
}
