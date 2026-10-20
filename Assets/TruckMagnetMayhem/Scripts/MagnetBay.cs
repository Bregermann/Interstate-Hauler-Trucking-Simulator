using UnityEngine;
namespace LWS.TruckMagnetMayhem
{
 public sealed class MagnetBay:MonoBehaviour
 {
  public BoxCollider zone;public Renderer[] paint;public int owner;
  public bool Contains(Collider shape)
  {
   if(shape is BoxCollider box){for(int i=0;i<8;i++){var corner=box.center+Vector3.Scale(box.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));if(!PointInside(box.transform.TransformPoint(corner)))return false;}return true;}
   return Contains(shape.bounds);
  }
  bool PointInside(Vector3 world){var p=zone.transform.InverseTransformPoint(world)-zone.center;var half=zone.size*.5f+Vector3.one*.06f;return Mathf.Abs(p.x)<=half.x&&Mathf.Abs(p.y)<=half.y&&Mathf.Abs(p.z)<=half.z;}
  public bool Contains(Bounds bounds)
  {
   var half=zone.size*.5f;for(int i=0;i<8;i++){var world=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=zone.transform.InverseTransformPoint(world)-zone.center;if(Mathf.Abs(p.x)>half.x||Mathf.Abs(p.y)>half.y||Mathf.Abs(p.z)>half.z)return false;}return true;
  }
  public void Colorize(Color color){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);foreach(var renderer in paint)renderer.SetPropertyBlock(block);}
 }
}
