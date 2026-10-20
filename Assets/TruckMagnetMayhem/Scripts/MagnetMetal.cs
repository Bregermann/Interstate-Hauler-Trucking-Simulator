using UnityEngine;
namespace LWS.TruckMagnetMayhem
{
 public sealed class MagnetMetal:MonoBehaviour
 {
  public Rigidbody body;public Collider shape;public Transform fieldPoint;public int polarity,points=1,index,truckSlot=-1;public Renderer[] tint;
  public Vector3 Point=>fieldPoint==null?body.worldCenterOfMass:fieldPoint.position;
  public void Colorize(Color color){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);foreach(var renderer in tint)renderer.SetPropertyBlock(block);}
 }
}
