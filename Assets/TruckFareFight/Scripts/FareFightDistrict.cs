using System;
using UnityEngine;
using LWS.InterstateHauler;
namespace LWS.TruckFareFight {
 [CreateAssetMenu(menuName="Truck Sports/Fare Fight/Original District")]
 public sealed class FareFightDistrict:ScriptableObject {
  [Serializable]public struct Stop {public string id,label;public Vector3 position;}
  public LwsRoadGraph graph;public Stop[] stops;public Vector3[] startingPositions;public Vector3[] startingForwards;
  public Bounds playableBounds;
  public bool Contains(Vector3 point)=>point.y>-14&&point.y<80&&point.x>=playableBounds.min.x&&point.x<=playableBounds.max.x&&point.z>=playableBounds.min.z&&point.z<=playableBounds.max.z;
 }
}
