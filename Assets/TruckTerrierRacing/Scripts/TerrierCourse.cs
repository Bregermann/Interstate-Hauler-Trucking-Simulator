using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckTerrierRacing {
public sealed class TerrierCourse:MonoBehaviour {
 public Transform geometry;public Transform[] sideRails;public MeshFilter groundMesh;public MeshCollider groundCollider;
 public Transform[] hurdles,ramps,patches,gates;public Transform finish;public SportsChaosWeather weather;
 [field:SerializeField]public float Length{get;private set;}=168;public int Seed{get;private set;}public int LayoutHash{get;private set;}
 Mesh runtimeGround;
 public Quaternion DrivingRotation=>geometry.rotation;
 public Quaternion Heading=>Quaternion.Euler(0,geometry.eulerAngles.y,0);
 public Vector3 Forward=>geometry.forward;
 public Vector3 Local(Vector3 world)=>geometry.InverseTransformPoint(world);
 public Vector3 World(Vector3 local)=>geometry.TransformPoint(local);
 public float FunnelStart {get{float last=0;if(hurdles!=null)foreach(var wall in hurdles)if(wall!=null)last=Mathf.Max(last,wall.localPosition.z);return Mathf.Clamp(Mathf.Max(Length*.68f,last+6),Length*.68f,Length-25);}}
 public float HalfWidth(float z)=>Mathf.Lerp(34,TerrierRules.TunnelHalfWidth+.35f,Mathf.InverseLerp(FunnelStart,Length,z));
 public void TruckCorners(BoxCollider shape,Vector3[] points){for(int i=0;i<8;i++)points[i]=Local(shape.transform.TransformPoint(shape.center+Vector3.Scale(shape.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));}
 public Bounds TruckBounds(BoxCollider shape){Bounds result=default;for(int i=0;i<8;i++){var p=Local(shape.transform.TransformPoint(shape.center+Vector3.Scale(shape.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));if(i==0)result=new Bounds(p,Vector3.zero);else result.Encapsulate(p);}return result;}
 public void Generate(int seed,int heat,bool fallback){Seed=seed;var random=new System.Random(seed);Length=fallback?160:160+random.Next(3)*8;finish.localPosition=new Vector3(0,0,Length);LayoutHash=Mathf.RoundToInt(Length);
 for(int i=0;i<hurdles.Length;i++){float z=32+i*27+(fallback?0:random.Next(-2,3));float h=fallback?.45f:.4f+(float)random.NextDouble()*.25f;hurdles[i].localPosition=new Vector3(0,h*.5f,z);hurdles[i].localScale=new Vector3((HalfWidth(z)-.3f)*2,h,.4f);LayoutHash=unchecked(LayoutHash*31+Mathf.RoundToInt(z*100+h*100));}
 for(int i=0;i<ramps.Length;i++){float z=44+i/3*55;ramps[i].localPosition=new Vector3((i%3-1)*HalfWidth(z)*.58f,0,z);ramps[i].localRotation=Quaternion.identity;ramps[i].gameObject.SetActive(!fallback||i<3);}
 for(int i=0;i<patches.Length;i++){float z=55+i/3*54;patches[i].localPosition=new Vector3((i%3-1)*HalfWidth(z)*.62f,.015f,z);patches[i].localScale=new Vector3(Mathf.Min(8,HalfWidth(z)*.5f),.025f,10);}
 for(int i=0;i<gates.Length;i++){gates[i].localPosition=new Vector3((i-5.5f)*5.2f,1.1f,4);gates[i].localScale=new Vector3(4.8f,2.2f,.15f);gates[i].gameObject.SetActive(true);}UpdateGeometry();weather.Configure(seed,0,1);Debug.Log("TERRIER_LAYOUT seed="+seed+" hash="+LayoutHash+" length="+Length+" fallback="+fallback);}
 public void UpdateGeometry(){if(groundMesh==null)return;var mesh=groundMesh.sharedMesh;if(Application.isPlaying){if(runtimeGround==null)runtimeGround=Instantiate(mesh);mesh=runtimeGround;groundMesh.sharedMesh=mesh;}var points=new Vector3[20];var uv=new Vector2[20];float[] z={-8,0,FunnelStart,Length,Length+6};float[] width={34,34,34,TerrierRules.TunnelHalfWidth+.35f,5};for(int row=0;row<5;row++)for(int k=0;k<4;k++){int i=row*4+k;points[i]=new Vector3(k%2==0?-width[row]:width[row],k<2?0:-.6f,z[row]);uv[i]=new Vector2(points[i].x*.15f,z[row]*.15f);}var triangles=new System.Collections.Generic.List<int>(84);for(int row=0;row<4;row++){int a=row*4,b=a+4;Quad(a,b,b+1,a+1);Quad(a+2,a+3,b+3,b+2);Quad(a,a+2,b+2,b);Quad(a+1,b+1,b+3,a+3);}Quad(0,1,3,2);Quad(16,18,19,17);mesh.Clear();mesh.vertices=points;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();groundCollider.sharedMesh=null;groundCollider.sharedMesh=mesh;for(int side=0;side<2;side++){float sign=side==0?-1:1;int stride=sideRails.Length/2;Rail(sideRails[side*stride],new Vector3(sign*34.3f,.8f,-8),new Vector3(sign*34.3f,.8f,0));if(stride>=3){Rail(sideRails[side*stride+1],new Vector3(sign*34.3f,.8f,0),new Vector3(sign*34.3f,.8f,FunnelStart));Rail(sideRails[side*stride+2],new Vector3(sign*34.3f,.8f,FunnelStart),new Vector3(sign*(TerrierRules.TunnelHalfWidth+.65f),.8f,Length));}else Rail(sideRails[side*stride+1],new Vector3(sign*34.3f,.8f,0),new Vector3(sign*(TerrierRules.TunnelHalfWidth+.65f),.8f,Length));}

 void Quad(int a,int b,int c,int d){triangles.AddRange(new[]{a,b,c,a,c,d});}}
 static void Rail(Transform t,Vector3 a,Vector3 b){var d=b-a;t.localPosition=(a+b)*.5f;t.localRotation=Quaternion.LookRotation(d,Vector3.up);t.localScale=new Vector3(.6f,1.6f,d.magnitude+.08f);}
 public bool Valid(){if(geometry==null||Length<150||Length>190||hurdles.Length<3||gates.Length!=12||groundMesh==null)return false;float previous=0;foreach(var h in hurdles){if(h.localScale.y>.8f||h.localPosition.z-previous<20||h.localPosition.z>Length-30)return false;previous=h.localPosition.z;}return HalfWidth(0)>30&&HalfWidth(Length)<TerrierRules.TunnelHalfWidth+.5f;}
 public Vector3 StartPosition(int slot,int heat)=>World(new Vector3((TerrierRules.Stall(slot,heat)-5.5f)*5.2f,.48f,0));
 public float Lane(float z,int slot,int heat){float fraction=(TerrierRules.Stall(slot,heat)-5.5f)/5.5f;float bend=Mathf.Sin(z*.031f+Seed%7)*Mathf.Min(1.4f,HalfWidth(z)*.1f);return Mathf.Lerp(fraction*Mathf.Max(0,HalfWidth(z)-3)+bend,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(FunnelStart-14,Length-15,z)));}
 public float NextHurdle(float z){float nearest=float.PositiveInfinity;foreach(var h in hurdles)if(h.localPosition.z>z-.4f)nearest=Mathf.Min(nearest,h.localPosition.z);return nearest;}
 public float Grip(Vector3 p){float value=weather.Grip;p=Local(p);foreach(var patch in patches)if(Mathf.Abs(p.x-patch.localPosition.x)<patch.localScale.x*.5f&&Mathf.Abs(p.z-patch.localPosition.z)<5)value*=.78f;return Mathf.Max(.5f,value);}
 public void OpenGates(){foreach(var g in gates)g.gameObject.SetActive(false);}
 void OnDestroy(){if(runtimeGround!=null)Destroy(runtimeGround);}
}
}
