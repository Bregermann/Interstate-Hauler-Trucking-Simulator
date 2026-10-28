using UnityEngine;
namespace LWS.TruckSubmarine {
 public sealed class SubmarineCargo:MonoBehaviour {
  public SubmarineMatch match; public Rigidbody body; public Collider shape; public int id; public SubmarineCargoKind kind; public Vector3 spawn; public Quaternion spawnRotation=Quaternion.identity;
  public int Owner{get;private set;}=-1; public bool Banked{get;private set;} public ConfigurableJoint Tether{get;private set;} public float OriginalDepth=>Mathf.Max(0,-spawn.y);
  public void ResetCargo(){if(Owner>=0)RestoreContacts(Owner);RemoveTether();Owner=-1;Banked=false;gameObject.SetActive(true);body.isKinematic=false;body.detectCollisions=true;body.position=spawn;body.rotation=spawnRotation;transform.SetPositionAndRotation(spawn,spawnRotation);body.linearVelocity=body.angularVelocity=Vector3.zero;}
  public bool Attach(SubmarineDiver diver){if(Banked||Owner>=0||diver==null||!match.rules.Grab(diver.truck.slot,id))return false;Owner=diver.truck.slot;
   // The native contact/gripper has already placed cargo nearby. Lock its
   // CURRENT relative pose; never teleport treasure to a synthetic inventory.
   Tether=gameObject.AddComponent<ConfigurableJoint>();Tether.connectedBody=diver.truck.body;Tether.autoConfigureConnectedAnchor=true;Tether.anchor=Vector3.zero;
   Tether.xMotion=Tether.yMotion=Tether.zMotion=ConfigurableJointMotion.Locked;Tether.angularXMotion=Tether.angularYMotion=Tether.angularZMotion=ConfigurableJointMotion.Locked;Tether.enableCollision=false;Tether.projectionMode=JointProjectionMode.PositionAndRotation;Tether.projectionDistance=.08f;Tether.massScale=1;Tether.connectedMassScale=1;
   foreach(var collider in diver.physicalColliders)if(collider!=null)Physics.IgnoreCollision(shape,collider,true);return true;}
  public bool Drop(){if(Owner<0)return false;int slot=Owner;match.rules.Release(slot,id);RestoreContacts(slot);Owner=-1;RemoveTether();return true;}
  public void RetireDelivered(){int slot=Owner;if(slot>=0)RestoreContacts(slot);Owner=-1;Banked=true;gameObject.SetActive(false);RemoveTether();body.isKinematic=true;}
  public void Freeze(){RemoveTether();if(!body.isKinematic)body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;}
  void RestoreContacts(int slot){if(match.divers==null||slot>=match.divers.Length)return;foreach(var collider in match.divers[slot].physicalColliders)if(collider!=null)Physics.IgnoreCollision(shape,collider,false);}
  void RemoveTether(){if(Tether==null)return;Tether.xMotion=Tether.yMotion=Tether.zMotion=ConfigurableJointMotion.Free;Tether.angularXMotion=Tether.angularYMotion=Tether.angularZMotion=ConfigurableJointMotion.Free;Tether.connectedBody=null;Destroy(Tether);Tether=null;}
 }
}
