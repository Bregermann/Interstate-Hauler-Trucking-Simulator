using UnityEngine;
using LWS.InterstateHauler;
namespace LWS.TruckHideAndSeek
{
 public sealed class HideWorld:MonoBehaviour
 {
  public Terrain[] terrain;public LwsRoadGraph graph;public Bounds playableBounds;
  public Vector3[] starts,searchSpots,hidingSpots;public Quaternion[] rotations;public float verifiedUsableSquareMetres;
  public float Height(Vector3 p){foreach(var t in terrain){var local=p-t.transform.position;var size=t.terrainData.size;if(local.x>=0&&local.x<=size.x&&local.z>=0&&local.z<=size.z)return t.SampleHeight(p)+t.transform.position.y;}return 0;}
  public bool Contains(Vector3 p)=>p.x>playableBounds.min.x&&p.x<playableBounds.max.x&&p.z>playableBounds.min.z&&p.z<playableBounds.max.z&&p.y>Height(p)-8;
 }
}
