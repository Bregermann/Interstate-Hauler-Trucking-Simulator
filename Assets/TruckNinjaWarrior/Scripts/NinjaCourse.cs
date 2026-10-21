using UnityEngine;
namespace LWS.TruckNinjaWarrior
{
 public sealed class NinjaCourse:MonoBehaviour
 {
  public Transform motionGroup;public Vector3 start;public Vector3[] checkpoints;public string[] obstacles;public Rigidbody[] movingBodies;public bool[] dynamicAtGo;public NinjaRotator[] rotators;public Rigidbody bell,finaleLanding;public float waterLevel=-5;
  public Collider warpedWall;
  void Awake(){ResetCourse();}
  Vector3[] positions;Quaternion[] rotations;bool[] kinematic;
  public void ResetCourse(){if(positions==null){positions=new Vector3[movingBodies.Length];rotations=new Quaternion[movingBodies.Length];kinematic=new bool[movingBodies.Length];for(int i=0;i<movingBodies.Length;i++){positions[i]=movingBodies[i].position;rotations[i]=movingBodies[i].rotation;kinematic[i]=dynamicAtGo==null||i>=dynamicAtGo.Length?movingBodies[i].isKinematic:!dynamicAtGo[i];}}for(int i=0;i<movingBodies.Length;i++){var body=movingBodies[i];if(!body.isKinematic)body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;body.position=positions[i];body.rotation=rotations[i];body.transform.SetPositionAndRotation(positions[i],rotations[i]);}foreach(var r in rotators)r.ResetRotation();Physics.SyncTransforms();}
  public void Release(){for(int i=0;i<movingBodies.Length;i++)movingBodies[i].isKinematic=kinematic[i];}
  public void Step(float age){foreach(var r in rotators)r.Step(age);}
 }
}
