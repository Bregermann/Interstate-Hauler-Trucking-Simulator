using UnityEngine;
namespace LWS.TruckSkyTow {
public sealed class SkyTowRing:MonoBehaviour {
 public int slot,index;public float at,radius=4.8f;public bool bonus;public Renderer art;
 public float rimThickness=.15f;public Renderer[] details;
 public float ClearRadius=>radius-rimThickness;
 MaterialPropertyBlock block;
 public void Tint(bool hit){block??=new MaterialPropertyBlock();var color=hit?new Color(.2f,1,.55f):new Color(.3f,.32f,.38f);block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*(hit?1.2f:.05f));art.SetPropertyBlock(block);if(details!=null)foreach(var detail in details)if(detail!=null)detail.SetPropertyBlock(block);}
 public void ResetRing(){art.SetPropertyBlock(null);if(details!=null)foreach(var detail in details)if(detail!=null)detail.SetPropertyBlock(null);}
}
}
