using UnityEngine;using ArchimedsLab;
namespace LWS.TruckSports {
// Adapter over the owned Floating Entities public hull-splitting/force solver.
// Proxy vertices are baked in metres; this object has unit scale. No second buoyancy force is added.
[DefaultExecutionOrder(-150)]public sealed class SportsFloatingBody:MonoBehaviour {
 public Rigidbody body;public Mesh proxy;public SportsEventSession session;public float waterHeight;public float waveAmplitude=.035f;public float Clock;public int Steps{get;private set;}public uint WetTriangles{get;private set;}
 tri[] local,world,wet,dry;WaterSurface.GetWaterHeight sample;
 void Awake(){WaterCutter.CookCache(proxy,ref local,ref world,ref wet,ref dry);sample=Height;}
 public float Height(Vector3 p)=>waterHeight+waveAmplitude*(Mathf.Sin(p.z*.18f+Clock*.7f)+.5f*Mathf.Sin(p.x*.28f+Clock*.43f));
 // The unchanged legacy DLL computes triangle area using Sin(Vector3.Angle), mixing radians/degrees.
 // Correct only its public geometry metadata; hull splitting and all forces remain vendor-owned.
 public static void CorrectAreas(tri[] triangles,uint count){for(int i=0;i<count;i++){var t=triangles[i];t.area=Vector3.Cross(t.b-t.a,t.c-t.a).magnitude*.5f;triangles[i]=t;}}
 void FixedUpdate(){if(session!=null&&!session.Live||body.isKinematic)return;body.WakeUp();WaterCutter.CookMesh(body.position,body.rotation,ref local,ref world);WaterCutter.SplitMesh(world,ref wet,ref dry,out var wetCount,out var dryCount,sample);CorrectAreas(wet,wetCount);CorrectAreas(dry,dryCount);Archimeds.ComputeAllForces(wet,dry,wetCount,dryCount,body.linearVelocity,body);WetTriangles=wetCount;Steps++;}
}
}
