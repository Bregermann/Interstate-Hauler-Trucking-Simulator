using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckWifeCarrying {
public sealed class WifeCourse:MonoBehaviour {
 public Terrain terrain;public SportsChaosWeather weather;public readonly float[] checkpoints={60,120,180,240,310};public const float Finish=310;
 public float Center(float z)=>Mathf.Sin(z*.022f)*9;public float Height(Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;public Vector3 Surface(Vector3 p){p.y=Height(p);return p;}public Vector3 Spawn(int pair,float z=5)=>Surface(new Vector3(Center(z)+(pair-1.5f)*6,0,z))+Vector3.up*.55f;
 public Quaternion Rotation(float z){float x=Center(z);var n=terrain.terrainData.GetInterpolatedNormal(Mathf.InverseLerp(-265,265,x),Mathf.InverseLerp(-20,380,z));return Quaternion.LookRotation(Vector3.ProjectOnPlane(new Vector3(Center(z+2)-Center(z),0,2),n),n);}
 public float Grip(Vector3 p)=>weather.Grip*(p.z>110&&p.z<150?.70f:p.z>205&&p.z<227?.82f:1);
}
}
