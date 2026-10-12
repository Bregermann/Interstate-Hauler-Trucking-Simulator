using UnityEngine;
namespace LWS.TruckHockey {
public sealed class HockeyContactFeedback:MonoBehaviour {
 struct Spark {public LineRenderer line;public float until;}
 public int Impacts{get;private set;}Spark[] pool;int cursor;Material material;
 void Awake(){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));pool=new Spark[12];for(int i=0;i<pool.Length;i++){var g=new GameObject("Contact spark");g.transform.SetParent(transform);var line=g.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=2;line.startWidth=.05f;line.endWidth=.015f;line.enabled=false;pool[i].line=line;}}
 public void Impact(Vector3 point,float speed,Color color){Impacts++;for(int j=0;j<3;j++){int i=cursor++%pool.Length;var s=pool[i];point.y=.5f;var direction=Quaternion.Euler(0,cursor*137,0)*Vector3.forward;float length=Mathf.Clamp(speed*.07f,.12f,.5f);s.line.SetPosition(0,point);s.line.SetPosition(1,point+direction*length);s.line.startColor=color;s.line.endColor=Color.white;s.line.enabled=true;s.until=Time.time+.12f;pool[i]=s;}}
 void Update(){for(int i=0;i<pool.Length;i++)if(pool[i].line.enabled&&Time.time>pool[i].until)pool[i].line.enabled=false;}
 void OnDestroy(){if(material!=null)Destroy(material);}
}
}