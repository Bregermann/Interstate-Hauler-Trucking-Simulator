using System;using System.IO;using System.Text;using UnityEngine;
namespace LWS.TruckTerrierRacing {
// Fixed ring buffer. Opt-in file logging; no per-frame allocations or writes in normal play.
public sealed class TerrierFinishDiagnostics:MonoBehaviour {
 const int Limit=192;readonly string[] lines=new string[Limit];int count;public bool verbose;public int Contacts{get;private set;}
 public void Contact(int slot,Collider other,Vector3 point,Vector3 normal,float incomingNormalSpeed,Bounds body,float eventTime,string classification){Contacts++;Add("CONTACT slot="+slot+" object="+other.name+" type="+other.GetType().Name+" trigger="+other.isTrigger+" point="+point.ToString("F4")+" normal="+normal.ToString("F4")+" incomingNormalSpeed="+incomingNormalSpeed.ToString("F5")+" boundsCenter="+body.center.ToString("F5")+" boundsSize="+body.size.ToString("F5")+" eventTime="+eventTime.ToString("F6")+" class="+classification);}
 public void Event(int slot,string reason,float time,Bounds body){Add("EVENT slot="+slot+" reason="+reason+" time="+time.ToString("F6")+" boundsCenter="+body.center.ToString("F5")+" boundsSize="+body.size.ToString("F5"));}
 void Add(string value){lines[count++%Limit]=value;if(verbose)Debug.Log("TERRIER_FINISH_TRACE "+value);}
 public string Report(){var sb=new StringBuilder();for(int i=Mathf.Max(0,count-Limit);i<count;i++)sb.AppendLine(lines[i%Limit]);return sb.ToString();}
 public void ResetTrace(){count=0;Contacts=0;Array.Clear(lines,0,Limit);}
}
}
