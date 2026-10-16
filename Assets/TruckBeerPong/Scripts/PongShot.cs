using UnityEngine;
namespace LWS.TruckBeerPong {
public sealed class PongShot:MonoBehaviour {
 public PongMatch match;public bool Launched{get;private set;}public bool Touched{get;private set;}public bool Bounced{get;private set;}public int Launches{get;private set;}public int Contacts{get;private set;}public Vector3 LaunchVelocity{get;private set;}public bool LaunchObserved{get;private set;}
 Vector3 previous;int candidate=-1;float quiet;
 public void ResetShot(){Launched=Touched=Bounced=LaunchObserved=false;Launches=Contacts=0;candidate=-1;quiet=0;LaunchVelocity=Vector3.zero;previous=match.truck.body.worldCenterOfMass;}
 public void Arm(){if(Launched)return;Launched=true;Launches++;previous=match.truck.body.worldCenterOfMass;}
 public void Observe(float dt){if(!Launched||match.Phase!=PongPhase.Flight||match.rules.ShotLocked)return;var body=match.truck.body;var now=body.worldCenterOfMass;if(!LaunchObserved&&match.FlightAge>Time.fixedDeltaTime*1.5f){LaunchObserved=true;LaunchVelocity=body.linearVelocity;}
  int side=1-match.rules.ActiveTeam;for(int i=0;i<10;i++){if(!match.rules.cups[side,i])continue;var cup=match.cups[side*10+i];float top=cup.Top;if(previous.y>top&&now.y<=top&&now.y<previous.y){float t=(previous.y-top)/(previous.y-now.y);var cross=Vector3.Lerp(previous,now,t)-cup.Center;if(new Vector2(cross.x,cross.z).sqrMagnitude<=2.15f*2.15f)candidate=i;}
   if(candidate!=i)continue;var local=now-cup.Center;float inside=Mathf.Lerp(1.4f,2.2f,Mathf.Clamp01((local.y-.25f)/3.3f));if(now.y>top+.15f||new Vector2(local.x,local.z).sqrMagnitude>(inside+.1f)*(inside+.1f)){candidate=-1;continue;}if(now.y<=top-.65f){match.Resolve(i,"IN THE BEER");break;}}
  previous=now;if(Touched&&body.linearVelocity.sqrMagnitude<.09f&&body.angularVelocity.sqrMagnitude<.16f)quiet+=dt;else quiet=0;if(quiet>=1.4f)match.Resolve(-1,"RIM / TABLE MISS");
 }
 void OnCollisionEnter(Collision c){if(!Launched||match.Phase!=PongPhase.Flight)return;Contacts++;Touched=true;if(c.collider==match.table){Bounced=true;match.TableImpact(c.relativeVelocity.magnitude);}if(c.collider==match.floor)match.Resolve(-1,"OFF THE TABLE");}
}
}
