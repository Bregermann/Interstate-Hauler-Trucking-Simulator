using UnityEngine;
namespace LWS.TruckPedestrianPlowing {
 public sealed class PlowSpawnZone:MonoBehaviour {
  public Vector2 size=new Vector2(20,15);[Min(0)]public float weight=1;
  [Tooltip("Crowd clusters use this zone's transform and size. Positive targets remain on the connected road/plaza navigation mesh.")]
  public bool enabledForCrowd=true;
  void OnDrawGizmos(){Gizmos.color=new Color(.1f,1,.7f,.45f);Gizmos.matrix=transform.localToWorldMatrix;Gizmos.DrawWireCube(Vector3.zero,new Vector3(size.x,.2f,size.y));}
 }
}
