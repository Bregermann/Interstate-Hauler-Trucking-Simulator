using UnityEngine;
namespace LWS.TruckMinefield {
public sealed class MinefieldHazard:MonoBehaviour {
 public MinefieldMatch match;public int kind;public bool Spent{get;private set;}public Vector3 StartPosition;
 public void ResetHazard(){Spent=false;gameObject.SetActive(true);transform.position=StartPosition;}
 void OnTriggerEnter(Collider c){if(Spent||!match.Live||match.Phase!=MinefieldPhase.Driving)return;var racer=c.GetComponentInParent<MinefieldRacer>();if(racer!=null&&racer.Launch(transform.position)){Spent=true;gameObject.SetActive(false);}}
}
}
