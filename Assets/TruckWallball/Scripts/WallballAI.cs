using UnityEngine;
using LWS.TruckHockey;
namespace LWS.TruckWallball {
public sealed class WallballAI:MonoBehaviour {
 public WallballMatch match;public WallballTruck truck;public Vector3 Target{get;private set;}public int Decisions{get;private set;}public int JumpDecisions{get;private set;}
 float next,stuck,recover,wallAim,error;Vector3 previous;bool leaveOut;
 public void ResetPlan(){next=stuck=recover=0;previous=truck.body.position;wallAim=0;truck.Stop();}
 void Decide(){leaveOut=false;var difficulty=match.players[truck.slot].difficulty;float noise=difficulty==HockeyDifficulty.Hard?.12f:difficulty==HockeyDifficulty.Normal?.3f:.65f;float reaction=difficulty==HockeyDifficulty.Hard?.08f:difficulty==HockeyDifficulty.Normal?.16f:.3f;
  var ball=match.ball.body.position;var velocity=match.ball.body.linearVelocity;var position=truck.body.position;bool eligible=match.rules.Eligible==match.Team(truck.slot);var aim=new Vector3(Mathf.Clamp(wallAim-ball.x,-2,2),0,-ball.z).normalized;
  if(match.rules.Phase==WallballPhase.Serving&&!eligible){Target=new Vector3(ball.x,.36f,-9);}
  else if(match.rules.Phase==WallballPhase.Serving&&eligible){wallAim=Mathf.Clamp(ball.x*.85f+Mathf.Sin(match.rules.Points*1.7f+truck.slot)*1.1f,-6,6);aim=new Vector3(wallAim-ball.x,0,-ball.z).normalized;var delta=position-ball;float ahead=Vector3.Dot(delta,aim);float lateral=Vector3.Dot(delta,Vector3.Cross(Vector3.up,aim));Target=ahead> -1.5f||Mathf.Abs(lateral)>.75f?ball-aim*2:ball+aim*.8f;}
  else{var predicted=ball;var v=velocity;int bounces=match.rules.Bounces;float best=float.PositiveInfinity;Vector3 fallback=position;for(int i=1;i<=120;i++){float t=i*.025f;if(!WallballFlight.Step(ref predicted,ref v,.025f,match.ball.gravity,match.ball.radius,match.ball.wallBounce,match.ball.floorBounce,match.halfWidth,match.courtLength,out bool floor))break;if(floor&&++bounces>=2)break;if(match.rules.Phase==WallballPhase.Outgoing&&v.z>=0||predicted.z> -1.8f||predicted.y>2.5f)continue;
    var direction=new Vector3(Mathf.Clamp(-predicted.x*.18f,-1,1),0,-predicted.z).normalized;var behind=predicted-direction*1.5f;behind.y=.36f;fallback=behind;
    float distance=Vector3.Distance(position,behind);float turn=Mathf.Abs(Vector3.SignedAngle(truck.transform.forward,behind-position,Vector3.up));float reach=distance/7+Mathf.Min(turn,180-turn)/truck.turnDegrees+reaction;
    if(predicted.y>.9f&&(!truck.Grounded||!eligible))continue;float cost=Mathf.Abs(reach-t)+(predicted.y>.9f?.2f:0);if(reach<t+.3f&&cost<best){best=cost;Target=behind;}}
   if(float.IsPositiveInfinity(best))Target=fallback;
   // Once positioned behind a close ball, drive through it with a wall-facing nose.
   if(eligible&&ball.y<1.1f&&Vector3.Dot(position-ball,aim)<-.9f&&Vector3.Distance(new Vector3(position.x,0,position.z),new Vector3(ball.x,0,ball.z))<3.2f)Target=ball+aim*.7f;
  }
  if(eligible&&match.rules.Phase==WallballPhase.Receiving){var forecast=ball;var speed=velocity;int count=match.rules.Bounces;for(int i=0;i<160;i++){if(!WallballFlight.Step(ref forecast,ref speed,.025f,match.ball.gravity,match.ball.radius,match.ball.wallBounce,match.ball.floorBounce,match.halfWidth,match.courtLength,out bool floor)){leaveOut=true;break;}if(floor&&++count>=2)break;}if(leaveOut)Target=new Vector3(Mathf.Clamp(ball.x+(ball.x>0?-3:3),-match.halfWidth+1,match.halfWidth-1),.36f,Mathf.Clamp(position.z+2,-match.courtLength+1,-2));}
  if(match.Interceptor(match.Team(truck.slot))!=truck.slot){int lane=truck.slot-match.First(match.Team(truck.slot));Target=new Vector3(-5.4f+lane*3.6f,.36f,-14+(match.Team(truck.slot)==0?0:2));}
  if(eligible&&!match.Armed[truck.slot]&&Vector3.Distance(match.ContactPoint(truck.slot,ball),ball)<match.ball.radius+.1f){var away=position-ball;away.y=0;Target=position+away.normalized*1.8f;}
  Target+=new Vector3(Random.Range(-noise,noise),0,Random.Range(-noise,noise));Target=new Vector3(Mathf.Clamp(Target.x,-match.halfWidth+1,match.halfWidth-1),.36f,Mathf.Clamp(Target.z,-match.courtLength+1,-1.8f));error=Random.Range(-noise,noise)*4;next=Time.time+reaction;Decisions++;}
 void FixedUpdate(){if(match.players[truck.slot].source!=HockeySource.AI)return;if(!match.rules.Active||match.MenuOpen){truck.Stop();return;}float dt=Time.fixedDeltaTime;var p=truck.body.position;if((p-previous).sqrMagnitude<.0003f&&Mathf.Abs(truck.throttle)>.3f)stuck+=dt;else stuck=Mathf.Max(0,stuck-dt);previous=p;if(stuck>1.6f){recover=.7f;stuck=0;}if(recover>0){recover-=dt;truck.Control(new Vector2(.55f,-.6f));return;}if(Time.time>=next)Decide();
  Vector3 delta=Target-p-truck.body.linearVelocity*.12f;delta.y=0;float angle=Vector3.SignedAngle(truck.transform.forward,delta,Vector3.up)+error;bool reverse=Mathf.Abs(angle)>110;if(reverse)angle=Mathf.DeltaAngle(0,angle-180);float gas=Mathf.Clamp(delta.magnitude*.65f,.22f,1);float signedSpeed=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);bool stop=delta.magnitude<.8f&&Mathf.Abs(signedSpeed)>.6f; if(delta.magnitude<.3f)gas=0;
  bool jump=false;var ball=match.ball.body.position;var bv=match.ball.body.linearVelocity;var flat=new Vector3(ball.x-p.x,0,ball.z-p.z);float closing=Mathf.Max(4,new Vector2(bv.x,bv.z).magnitude+new Vector2(truck.body.linearVelocity.x,truck.body.linearVelocity.z).magnitude);float arrival=Mathf.Clamp((flat.magnitude-1.3f)/closing,.06f,.35f);float y=ball.y+bv.y*arrival-.5f*match.ball.gravity*arrival*arrival;float center=p.y+truck.jumpSpeed*arrival-.5f*truck.gravity*arrival*arrival;
  if(!leaveOut&&match.rules.Eligible==match.Team(truck.slot)&&truck.Grounded&&flat.magnitude<3.7f&&flat.magnitude>1.3f&&ball.y>2&&y>center-.65f&&y<center+.7f&&Mathf.Abs(angle)<50&&match.players[truck.slot].difficulty!=HockeyDifficulty.Easy){jump=true;JumpDecisions++;}
  truck.Control(new Vector2(Mathf.Clamp(angle/35,-1,1)*(reverse?-1:1),stop?-Mathf.Sign(signedSpeed)*.7f:(reverse?-1:1)*gas),jump);
 }
}
}
