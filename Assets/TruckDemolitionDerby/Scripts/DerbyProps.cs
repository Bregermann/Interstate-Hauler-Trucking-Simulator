using UnityEngine;
namespace LWS.TruckDerby {public sealed class DerbyProps:MonoBehaviour {
 public Transform[] sources;public Rigidbody[] movable;public Vector3[] positions;public Quaternion[] rotations;public DerbyBreakableProp[] breakable;
 public int Mask=>((1<<sources.Length)-1)<<8;
 public void ResetProps(){for(int i=0;i<movable.Length;i++){var b=movable[i];b.gameObject.SetActive(true);b.isKinematic=false;b.detectCollisions=true;b.position=positions[i];b.rotation=rotations[i];b.linearVelocity=b.angularVelocity=Vector3.zero;}foreach(var p in breakable){p.gameObject.SetActive(true);p.ResetProp();}}
 public void Freeze(){foreach(var b in movable){b.isKinematic=true;b.detectCollisions=false;}foreach(var p in breakable)p.Freeze();}
 public void Hide(){Freeze();foreach(var p in sources)p.gameObject.SetActive(false);}
}}
