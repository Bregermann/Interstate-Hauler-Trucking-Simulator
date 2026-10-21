using UnityEngine;using UnityEngine.Splines;
namespace LWS.TruckSkyTow {
public sealed class SkyTowCourse:MonoBehaviour {
 public SplineContainer route;public SplineContainer[] laneRoutes;public float[] laneLengths;public SkyTowRing[] rings;public float duration=120,length;public float laneSpacing=14;public Terrain terrain;public Transform water;
 [HideInInspector]public int playtestRepairVersion;
 public bool followDelayedTowPath;
 [Min(1)]public float trailingDistance=29;
 public readonly string[] sections={"FROZEN HARBOR","ICEBERG FJORD","GLACIER PASSAGE","ARCTIC CANYON","FROZEN RUINS","THE GRAND FINALE"};
 public float Cruise=>length/duration;public float LaneDuration(int slot)=>laneLengths!=null&&laneLengths.Length==4?laneLengths[slot]/Cruise:duration;
 public void Frame(float seconds,int slot,out Vector3 point,out Quaternion rotation){bool own=laneRoutes!=null&&laneRoutes.Length==4&&laneRoutes[slot]!=null;var path=own?laneRoutes[slot]:route;float distance=Mathf.Clamp(seconds*Cruise,0,own?laneLengths[slot]:length);float t=SplineUtility.GetNormalizedInterpolation(path.Spline,distance,PathIndexUnit.Distance);path.Evaluate(t,out var p,out var forward,out var up);Vector3 tangent=Vector3.ProjectOnPlane((Vector3)forward,Vector3.up);if(tangent.sqrMagnitude<.0001f)tangent=Vector3.forward;rotation=Quaternion.LookRotation(tangent.normalized,Vector3.up);point=(Vector3)p+(own?Vector3.zero:rotation*Vector3.right*((slot-1.5f)*laneSpacing));}
 public void NominalFrame(float seconds,int slot,out Vector3 point,out Quaternion rotation){float delay=trailingDistance/Mathf.Max(1,Cruise);float earlier=seconds-delay;Frame(Mathf.Max(0,earlier),slot,out point,out rotation);if(earlier<0)point+=rotation*Vector3.forward*(earlier*Cruise);point+=Vector3.up*18;}
 public Vector3 NominalTruck(float seconds,int slot){if(followDelayedTowPath){NominalFrame(seconds,slot,out var point,out var rotation);return point;}Frame(seconds,slot,out var p,out var q);return p+q*new Vector3(0,18,-29);}
 public SkyTowRing Ring(int slot,int index)=>rings[slot*36+index];
}
}
