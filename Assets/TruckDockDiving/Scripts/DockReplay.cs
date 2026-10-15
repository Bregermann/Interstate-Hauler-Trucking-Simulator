using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckDockDiving {
// Fixed-capacity transform recording adapted from Cornhole. Ghosts have meshes only.
public sealed class DockReplay:MonoBehaviour {
 const int Capacity=1000;struct Pose{public Vector3 p;public Quaternion q;public float split,time;}
 readonly Pose[] frames=new Pose[Capacity];public Transform ghost;public DockSplitVisual ghostSplit;public Renderer[] live;public bool Playing{get;private set;}public int Angle{get;private set;}public float Speed{get;private set;}=1;public float Clock{get;private set;}public float Duration=>count>0?frames[count-1].time:0;public Vector3 Position=>ghost.position;public int Count=>count;
 Renderer[] ghostPaint;MaterialPropertyBlock colorBlock;void Awake(){ghostPaint=ghost.GetComponentsInChildren<Renderer>(true);colorBlock=new MaterialPropertyBlock();}
 int count;float started,next,age;bool recording,manualSpeed,manualAngle;int rate=2;static readonly float[] Rates={.25f,.5f,1,2,4};
 public void Begin(){Stop();count=0;started=Time.time;next=0;recording=true;}
 public void Sample(Transform truck,float split){if(!recording||Time.time<next||count>=Capacity)return;frames[count++]=new Pose{p=truck.position,q=truck.rotation,split=split,time=Time.time-started};next=Time.time+.04f;}
 public void Complete(){recording=false;}
 public bool Play(){if(count<2)return false;Playing=true;Clock=Mathf.Max(0,Duration-7);age=0;Angle=0;manualSpeed=manualAngle=false;ghost.gameObject.SetActive(true);foreach(var r in live)if(r.name=="Cab"){r.GetPropertyBlock(colorBlock);break;}foreach(var r in ghostPaint)if(r.name=="Cab")r.SetPropertyBlock(colorBlock);foreach(var r in live)r.forceRenderingOff=true;Apply();return true;}
 public void NextAngle(){Angle=(Angle+1)%4;manualAngle=true;}public void NextSpeed(){rate=(rate+1)%Rates.Length;Speed=Rates[rate];manualSpeed=true;}
 public void Rewind(){Clock=Mathf.Max(0,Duration-7);age=0;Apply();}
 public bool Tick(float dt,bool automatic=true){if(!Playing)return true;age+=dt;if(!manualSpeed)Speed=Clock<Duration-3.8f?1.4f:Clock<Duration-.6f?.45f:.65f;if(!manualAngle)Angle=Clock<Duration-4?0:Clock<Duration-1?1:2;Clock=Mathf.Min(Duration,Clock+dt*Speed);Apply();return automatic?age>16||Clock>=Duration&&age>7:!manualAngle&&!manualSpeed&&Clock>=Duration&&age>7;}
 void Apply(){int a=0;while(a<count-2&&frames[a+1].time<Clock)a++;int b=Mathf.Min(a+1,count-1);float f=Mathf.InverseLerp(frames[a].time,frames[b].time,Clock);ghost.SetPositionAndRotation(Vector3.Lerp(frames[a].p,frames[b].p,f),Quaternion.Slerp(frames[a].q,frames[b].q,f));ghostSplit.Apply(Mathf.Lerp(frames[a].split,frames[b].split,f));}
 public void Stop(){Playing=false;recording=false;if(ghost!=null)ghost.gameObject.SetActive(false);if(live!=null)foreach(var r in live)if(r!=null)r.forceRenderingOff=false;}
 void OnDisable(){Stop();}
}
}
