using UnityEngine;
namespace LWS.TruckSkyTow {
public sealed class SkyTowRing:MonoBehaviour {
 public int slot,index;public float at,radius=4.8f;public bool bonus;public Renderer art;
 public void Tint(bool hit){var block=new MaterialPropertyBlock();var color=hit?new Color(.2f,1,.55f):new Color(.3f,.32f,.38f);block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*(hit?1.2f:.05f));art.SetPropertyBlock(block);}
 public void ResetRing(){art.SetPropertyBlock(null);}
}
}
