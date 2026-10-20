using UnityEngine;
namespace LWS.TruckSheepHerding
{
 public sealed class HerdPen:MonoBehaviour
 {
  public HerdMatch match;public int owner;public BoxCollider scoring;public Renderer[] paint;
  public bool ContainsWhole(HerdSheep sheep)
  {
   var bounds=sheep.shape.bounds;var min=scoring.center-scoring.size*.5f;var max=scoring.center+scoring.size*.5f;
   for(int i=0;i<8;i++){var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var local=scoring.transform.InverseTransformPoint(corner);if(local.x<min.x||local.x>max.x||local.y<min.y||local.y>max.y||local.z<min.z||local.z>max.z)return false;}return true;
  }
  public void Colorize(Color color){var b=new MaterialPropertyBlock();b.SetColor("_BaseColor",color);b.SetColor("_Color",color);foreach(var r in paint)r.SetPropertyBlock(b);}
 }
}
