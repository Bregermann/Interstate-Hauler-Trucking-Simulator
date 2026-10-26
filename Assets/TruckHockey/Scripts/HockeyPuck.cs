using UnityEngine;
namespace LWS.TruckHockey {
public sealed class HockeyPuck:MonoBehaviour {
 public HockeyMatch match;public Rigidbody body;public float radius=.55f,drag=.28f,maxSpeed=16;float nextAudio;readonly float[] contactTime={-100,-100,-100,-100,-100,-100};
 public int LastTouch{get;private set;}=-1;public int ActiveContacts{get{int count=0;for(int i=0;i<6;i++)if(Time.time-contactTime[i]<.08f)count++;return count;}}
 public string LastTouchParticipantId{get;private set;}public int LastTouchTeam{get;private set;}=-1;
 readonly string[] lastTeamTouch=new string[2];
 // Keep play history by identity, not display name or a slot that can be rebound.
 void RecordTouch(int slot){if(slot<0||slot>=match.players.Length||!match.rules.Live||match.MenuOpen)return;LastTouch=slot;LastTouchTeam=match.TeamOf(slot);LastTouchParticipantId=match.players[slot].participantId;if(!string.IsNullOrWhiteSpace(LastTouchParticipantId))lastTeamTouch[LastTouchTeam]=LastTouchParticipantId;}
 public int ScorerSlot(int team){if(team<0||team>1||string.IsNullOrWhiteSpace(lastTeamTouch[team]))return -1;for(int i=team*3;i<team*3+3;i++)if(string.Equals(match.players[i].participantId,lastTeamTouch[team],System.StringComparison.Ordinal))return i;return -1;}
 public int PushingContacts{get{int count=0;for(int i=0;i<6;i++)if(Time.time-contactTime[i]<.08f&&Mathf.Abs(match.trucks[i].throttle)>.25f)count++;return count;}}
 public bool ContactBothTrucks=>ActiveContacts>=2;
 public void ClearContacts(){for(int i=0;i<6;i++)contactTime[i]=-100;LastTouch=-1;LastTouchTeam=-1;LastTouchParticipantId=null;lastTeamTouch[0]=lastTeamTouch[1]=null;}public void ForgetTruckContact(int slot){contactTime[slot]=-100;}
 int Owner(Rigidbody b){if(b==null)return -1;var stick=b.GetComponent<HockeyStick>();if(stick!=null)return stick.owner.side;for(int i=0;i<6;i++)if(b==match.trucks[i].body)return i;return -1;}
 void OnCollisionStay(Collision c){int slot=Owner(c.rigidbody);if(slot>=0){contactTime[slot]=Time.time;RecordTouch(slot);}}
 void OnCollisionExit(Collision c){for(int i=0;i<6;i++)if(Owner(c.rigidbody)==i)contactTime[i]=-100;}
 public int BoardContacts{get;private set;}public int TruckContacts{get;private set;}
 void FixedUpdate(){if(!match.rules.Live||match.MenuOpen)return;body.linearVelocity=Vector3.ClampMagnitude(body.linearVelocity/(1+drag*Time.fixedDeltaTime),maxSpeed);}
 void OnCollisionEnter(Collision c){if(!match.rules.Live||match.MenuOpen)return;if(c.rigidbody==null)BoardContacts++;else{TruckContacts++;RecordTouch(Owner(c.rigidbody));}if(Time.time>=nextAudio){nextAudio=Time.time+.075f;match.feedback.Play(c.rigidbody==null?2:1,c.relativeVelocity.magnitude);}}
}
}
