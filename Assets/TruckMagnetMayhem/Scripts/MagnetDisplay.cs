using UnityEngine;
namespace LWS.TruckMagnetMayhem
{
 public sealed class MagnetDisplay:MonoBehaviour
 {
  public MagnetMetal metal;public Renderer[] poles;public LineRenderer[] rings;MaterialPropertyBlock block;int prior=99;
  void Awake(){block=new MaterialPropertyBlock();}
  void Update()
  {
   int mode=metal.polarity;Color color=mode>0?new Color(1,.12f,.06f):mode<0?new Color(.04f,.7f,1):new Color(.28f,.3f,.32f);
   if(mode!=prior){prior=mode;block.SetColor("_BaseColor",color);block.SetColor("_Color",color);block.SetColor("_EmissionColor",color*(mode==0?0:1.5f));foreach(var pole in poles)pole.SetPropertyBlock(block);foreach(var ring in rings){ring.enabled=mode!=0;ring.startColor=color*.65f;ring.endColor=color*.15f;}}
   if(mode==0)return;
   for(int ring=0;ring<rings.Length;ring++){float radius=1+Mathf.Repeat(Time.time*.9f+ring,3)*1.5f;for(int p=0;p<rings[ring].positionCount;p++){float a=p*2*Mathf.PI/(rings[ring].positionCount-1);rings[ring].SetPosition(p,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}}
  }
 }
}
