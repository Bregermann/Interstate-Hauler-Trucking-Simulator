using UnityEngine;
namespace LWS.TruckSkyTow {
public sealed class SkyTowRing:MonoBehaviour {
 public int slot,index;public float at,radius=4.8f;public bool bonus;public Renderer art;
 public float rimThickness=.15f;public Renderer[] details;
 public float ClearRadius=>radius-rimThickness;
 MaterialPropertyBlock block;
 [System.NonSerialized]public SkyTowRingEffects effects;public int Outcome{get;private set;}
 public void Tint(bool hit){if(Outcome!=0)return;Outcome=hit?1:-1;block??=new MaterialPropertyBlock();var color=hit?new Color(.2f,1,.55f):new Color(.3f,.32f,.38f);block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*(hit?1.2f:.05f));art.SetPropertyBlock(block);if(details!=null)foreach(var detail in details)if(detail!=null)detail.SetPropertyBlock(block);effects?.Resolved(this,hit);}
 public void Animate(float pulse){block??=new MaterialPropertyBlock();var color=Outcome>0?new Color(.2f,1,.55f):Outcome<0?new Color(.3f,.32f,.38f):bonus?new Color(1,.57f,.1f):new Color(.12f,.65f,1);block.SetColor("_EmissionColor",color*(Outcome<0?.03f:Mathf.Lerp(.18f,.7f,pulse)));art.SetPropertyBlock(block);}
 public void ResetRing(){Outcome=0;block?.Clear();art.SetPropertyBlock(null);if(details!=null)foreach(var detail in details)if(detail!=null)detail.SetPropertyBlock(null);}
}
}
