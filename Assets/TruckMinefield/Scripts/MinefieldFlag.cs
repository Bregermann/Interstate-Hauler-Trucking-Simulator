using UnityEngine;using LWS.TruckSports;
namespace LWS.TruckMinefield {
public sealed class MinefieldFlag:MonoBehaviour {
 public TMPro.TMP_Text attachedName;public Transform[] bones;public TextMesh caption,shadow;public Transform backing;public Renderer cloth;public SportsChaosWeather weather;Quaternion[] rest;
 public bool Recorded{get;private set;}public int HeatMark{get;private set;}public float BornAt{get;private set;}public Color MarkColor{get;private set;}public string MarkText{get;private set;}public Vector3 MarkPosition{get;private set;}
 void Awake(){rest=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++)rest[i]=bones[i].localRotation;}
 public void ClearMark(){Recorded=false;gameObject.SetActive(false);}
 public void Reveal(Vector3 p,Color color,string text,int heat=0,float bornAt=0){Recorded=true;HeatMark=heat;BornAt=bornAt;MarkColor=color;MarkText=text;MarkPosition=p;transform.position=p;gameObject.SetActive(true);caption.text=text;caption.color=color;if(attachedName!=null){attachedName.text=text;attachedName.color=color;}if(shadow!=null)shadow.text=text;if(backing!=null){var lines=text.Split('\n');int longest=0;foreach(var line in lines)longest=Mathf.Max(longest,line.Length);backing.localScale=new Vector3(longest*.85f+1.6f,lines.Length*1.65f+.6f,1);}var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);cloth.SetPropertyBlock(block);}
 void LateUpdate(){if(rest==null)return;float gain=weather==null?.3f:Mathf.Clamp(weather.Flow.magnitude*.06f,.15f,.8f);for(int i=0;i<bones.Length;i++)bones[i].localRotation=rest[i]*Quaternion.Euler(0,Mathf.Sin(Time.time*3-i*.55f)*gain*12,Mathf.Sin(Time.time*4-i*.7f)*gain*5);}
}
}
