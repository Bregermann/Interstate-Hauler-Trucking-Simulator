using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckWifeCarrying {
public sealed class WifeCourse:MonoBehaviour {
 public Terrain terrain;public SportsChaosWeather weather;public readonly float[] checkpoints={60,120,180,240,310};public const float Finish=310;public bool loopCircuit;
 public float Center(float z)=>Mathf.Sin(z*.022f)*9;public float Height(Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;public Vector3 Surface(Vector3 p){p.y=Height(p);return p;}
 public Vector3 Point(float distance,float lateral=0){float a=distance/Finish*Mathf.PI*2;var p=new Vector3(Mathf.Sin(a)*70,0,150-Mathf.Cos(a)*140);var tangent=new Vector3(Mathf.Cos(a)*70,0,Mathf.Sin(a)*140).normalized;var right=Vector3.Cross(Vector3.up,tangent);return Surface(p+right*lateral);}
 public Vector3 Spawn(int pair,float z=5)=>loopCircuit?Point(z,(pair-1.5f)*5)+Vector3.up*.55f:Surface(new Vector3(Center(z)+(pair-1.5f)*6,0,z))+Vector3.up*.55f;
 public Quaternion Rotation(float z){if(loopCircuit){var tangent=Point(z+.5f)-Point(z-.5f);return Quaternion.LookRotation(tangent.normalized,Vector3.up);}float x=Center(z);var n=terrain.terrainData.GetInterpolatedNormal(Mathf.InverseLerp(-265,265,x),Mathf.InverseLerp(-20,380,z));return Quaternion.LookRotation(Vector3.ProjectOnPlane(new Vector3(Center(z+2)-Center(z),0,2),n),n);}
 public float Project(Vector3 p,float hint,out float lateral){if(!loopCircuit){lateral=p.x-Center(p.z);return p.z;}float a=Mathf.Atan2(p.x/70,(150-p.z)/140);if(a<0)a+=Mathf.PI*2;float distance=a/(Mathf.PI*2)*Finish;if(hint>Finish-30&&distance<30)distance+=Finish;if(hint<30&&distance>Finish-30)distance-=Finish;var center=Point(distance);var tangent=Point(distance+.5f)-Point(distance-.5f);lateral=Vector3.Dot(p-center,Vector3.Cross(Vector3.up,tangent.normalized));return distance;}
 public float Grip(Vector3 p){float s=Project(p,Finish*.5f,out var lateral);return weather.Grip*(s>110&&s<150?.70f:s>205&&s<245?.82f:1);}
}}
