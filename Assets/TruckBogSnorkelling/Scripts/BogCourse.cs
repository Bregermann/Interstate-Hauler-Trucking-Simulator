using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckBogSnorkelling {
public sealed class BogCourse:MonoBehaviour {
 public Terrain terrain;public Transform water;public SportsChaosWeather weather;public float[] checkpoints={22,82,142,218,288};public float[] logs={45,103,165,254,311};public const float Finish=350,AirSeconds=14;public float WaterHeight=>water.position.y;
 public static float Center(float z)=>Mathf.Sin(z*.018f)*5;
 static readonly float[,] spans={{29,61},{89,120},{150,192},{236,270},{294,330}};public static float Floor(float x,float z){float h=.25f;for(int i=0;i<5;i++){float a=Mathf.SmoothStep(0,1,Mathf.InverseLerp(spans[i,0],spans[i,0]+10,z));float b=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(spans[i,1]-10,spans[i,1],z));h-=3.45f*a*b;}if(z>142&&z<204&&x-Center(z)>10){float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,15,x-Center(z)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(142,151,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(192,204,z)));h=Mathf.Lerp(h,-.55f,blend);}float bank=Mathf.Max(0,Mathf.Abs(x-Center(z))-25);h+=Mathf.Min(8,bank*bank*.045f);return h;}
 public Vector3 Surface(Vector3 p){p.y=terrain.SampleHeight(p)+terrain.transform.position.y;return p;}
 public bool Safe(float z)=>z<25||z>65&&z<87||z>123&&z<147||z>197&&z<232||z>275&&z<292||z>334;
 public bool MustDive(Vector3 p){foreach(float z in logs)if(p.z>z-11&&p.z<z+5&&!(z==165&&p.x-Center(p.z)>11))return true;return false;}
 public Vector3 Spawn(int slot)=>Surface(new Vector3(Center(8)+(slot-1.5f)*6,0,8))+Vector3.up*.55f;
}
}
