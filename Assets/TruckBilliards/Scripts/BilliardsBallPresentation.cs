using UnityEngine;
using DG.Tweening;
namespace LWS.TruckBilliards {
// Displacement-driven visual rotation; the existing planar Rigidbody remains authoritative.
public sealed class BilliardsBallPresentation:MonoBehaviour {
 public BilliardsBall ball;public Transform visual,shadow;public float radius=.5f;public Quaternion rackRotation=Quaternion.identity;
 Vector3 previous;bool initialized,dropping;Tween drop;public int RollSteps{get;private set;}public int Drops{get;private set;}
 public bool Dropping=>dropping;public float LastRollAngle{get;private set;}
 void Awake(){Restore();}
 public void Restore(){drop?.Kill();dropping=false;visual.gameObject.SetActive(true);visual.localPosition=Vector3.zero;visual.localRotation=rackRotation;shadow.gameObject.SetActive(true);previous=transform.position;initialized=false;LastRollAngle=0;}
 public void Pocket(bool pocketed){if(!pocketed){Restore();return;}if(dropping)return;dropping=true;Drops++;shadow.gameObject.SetActive(false);drop?.Kill();drop=visual.DOLocalMoveY(-1.45f,.38f).SetEase(Ease.InQuad).OnComplete(()=>{if(dropping)visual.gameObject.SetActive(false);});}
 void LateUpdate(){if(visual==null||shadow==null)return;var p=transform.position;shadow.position=new Vector3(p.x,.018f,p.z);if(ball.Pocketed){previous=p;return;}var delta=p-previous;delta.y=0;previous=p;if(!initialized||delta.sqrMagnitude>4*radius*radius){initialized=true;LastRollAngle=0;return;}LastRollAngle=delta.magnitude/Mathf.Max(.001f,radius)*Mathf.Rad2Deg;if(LastRollAngle<.0001f)return;visual.rotation=Quaternion.AngleAxis(LastRollAngle,Vector3.Cross(Vector3.up,delta.normalized))*visual.rotation;RollSteps++;}
 void OnDisable(){drop?.Kill();}
}
}