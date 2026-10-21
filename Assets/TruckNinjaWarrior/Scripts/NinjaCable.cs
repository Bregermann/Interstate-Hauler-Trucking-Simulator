using UnityEngine;
namespace LWS.TruckNinjaWarrior
{
 public sealed class NinjaCable:MonoBehaviour
 {
  public Transform load;public Vector3 anchor;public LineRenderer cable;void LateUpdate(){if(load==null)return;cable.SetPosition(0,anchor);cable.SetPosition(1,load.position);}
 }
}
