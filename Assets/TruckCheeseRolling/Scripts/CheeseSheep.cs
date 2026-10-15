using UnityEngine;
namespace LWS.TruckCheeseRolling {
// The sport-owned model slot references the genuine Haberdashery SheepV5 mesh and texture.
public sealed class CheeseSheep:MonoBehaviour {
 public CheeseMatch match;public Transform modelSlot;public GameObject warning;public bool Detonated{get;private set;}public bool Queued{get;private set;}
 public void ResetHazard(Vector3 p){transform.position=p;var normal=new Vector3(CheeseRules.Height(p.x-.5f,p.z)-CheeseRules.Height(p.x+.5f,p.z),1,CheeseRules.Height(p.x,p.z-.5f)-CheeseRules.Height(p.x,p.z+.5f)).normalized;modelSlot.localRotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,Mathf.Repeat(p.x*13+p.z*11,360),0);modelSlot.localPosition=normal*.7f-Vector3.up*.8f;Detonated=Queued=false;gameObject.SetActive(true);}
 public void Queue(){if(Queued||Detonated||!match.Live)return;Queued=true;match.QueueBlast(this);}
 public void Detonate(){if(Detonated)return;Detonated=true;match.Explode(transform.position);gameObject.SetActive(false);}
 void OnCollisionEnter(Collision c){if(c.rigidbody!=null&&c.rigidbody.GetComponent<CheeseTruck>()!=null)Queue();}
}
}
