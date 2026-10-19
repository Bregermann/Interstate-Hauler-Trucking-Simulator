using UnityEngine;
namespace LWS.TruckSnake {
// Test the speed and direction of the SAME solid contact. A hard floor landing
// cannot donate its normal speed to a simultaneous gentle frame/edge scrape.
public static class SnakeSolidImpact {
 public static bool Qualifies(Vector3 relativeIncoming,Vector3 forward,Vector3 normal,out float speed){speed=Mathf.Max(0,-Vector3.Dot(relativeIncoming,normal));return normal.y<.55f&&Vector3.Dot(-normal,forward)>.6f&&speed>8;}
}
}
