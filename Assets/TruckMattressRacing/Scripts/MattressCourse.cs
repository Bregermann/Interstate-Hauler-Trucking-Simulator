using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckMattressRacing {
public sealed class MattressCourse:MonoBehaviour {
 public const float Finish=650;public Terrain terrain;public SportsChaosWeather weather;public float[] checkpoints={80,160,240,320,400,480,560,650};public int Seed{get;private set;}TerrainData runtimeData;float[,,] surfaceMap;
 public float Center(float z)=>Mathf.Sin(z*.013f+Seed%5*.1f)*16;
 public float Height(Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;
 public Vector3 Surface(Vector3 p){p.y=Height(p);return p;}
 public Vector3 Spawn(int slot,float z){if(z==0)z=8;return Surface(new Vector3(Center(z)+(slot-3.5f)*5.2f,0,z))+Vector3.up*.40f;}
 public Quaternion Rotation(float z){if(z==0)z=8;var n=terrain.terrainData.GetInterpolatedNormal(Mathf.InverseLerp(-265,265,Center(z)),Mathf.InverseLerp(-15,725,z));return Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward,n),n);}
 public void Generate(int seed,int weatherChoice){Seed=seed;if(Application.isPlaying&&runtimeData==null){runtimeData=Instantiate(terrain.terrainData);terrain.terrainData=runtimeData;terrain.GetComponent<TerrainCollider>().terrainData=runtimeData;}var data=terrain.terrainData;int size=data.heightmapResolution;var heights=new float[size,size];for(int zi=0;zi<size;zi++)for(int xi=0;xi<size;xi++){float z=zi/(float)(size-1)*740-15,x=xi/(float)(size-1)*530-265;float h=285-Mathf.Clamp(z,0,650)*.4f;float bank=Mathf.Min(34,Mathf.Max(0,Mathf.Abs(x-Center(z))-24));h+=bank*bank*.013f;h+=Mathf.SmoothStep(0,1,Mathf.InverseLerp(15,40,z))*(Mathf.PerlinNoise(x*.08f+seed%30,z*.035f)*.55f+Mathf.Sin(z*.025f+x*.035f)*.4f);for(int k=0;k<3;k++){float dz=(z-(125+k*175+seed%7))/5.5f;h+=2.2f*Mathf.Exp(-dz*dz);}heights[zi,xi]=Mathf.Clamp01(h/330);}data.SetHeights(0,0,heights);if(surfaceMap==null)surfaceMap=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);weather.Configure(seed,weatherChoice,0);}
 public float ContactFactor(Vector3 p){if(surfaceMap==null)return 1;int x=Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(-265,265,p.x)*(surfaceMap.GetLength(1)-1)),0,surfaceMap.GetLength(1)-1),z=Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(-15,725,p.z)*(surfaceMap.GetLength(0)-1)),0,surfaceMap.GetLength(0)-1);return 1+surfaceMap[z,x,1]*.5f-surfaceMap[z,x,2]*.4f;}
 void OnDestroy(){if(runtimeData!=null)Destroy(runtimeData);}
}
}
