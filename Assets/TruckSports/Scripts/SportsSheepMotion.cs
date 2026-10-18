using UnityEngine;
namespace LWS.TruckSports {
// Runs the installed SheepV5 skeletal clip at a rate driven by actual displacement.
public sealed class SportsSheepMotion:MonoBehaviour {
 public Animator animator;public Transform mover;Vector3 previous;float speed;
 void OnEnable(){previous=mover!=null?mover.position:transform.position;speed=0;if(animator!=null){animator.Rebind();animator.Update(0);}}
 void Update(){if(animator==null||mover==null||Time.deltaTime<=0)return;float movement=Vector3.ProjectOnPlane(mover.position-previous,Vector3.up).magnitude;previous=mover.position;float actual=movement>5?0:movement/Time.deltaTime;speed=Mathf.Lerp(speed,actual,1-Mathf.Exp(-Time.deltaTime*9));animator.SetFloat("Speed",speed);animator.SetFloat("CycleSpeed",Mathf.Clamp(speed/1.5f,.25f,2));}
}
}
