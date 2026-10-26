using UnityEngine;
namespace LWS.TruckCornhole
{
 // Identity only: original mesh contacts, whole-body hole crossing and final
 // round scoring remain the authorities. This component never moves a truck.
 public sealed class CornholeSharedTarget:MonoBehaviour
 {
  public CornholeMatch match;public CornholeSurface boardSurface,groundSurface;
  public const int SharedLayer=28,TruckLayer=29,PrivateLayerStart=24;
  public bool Owns(CornholeSurface surface)=>surface!=null&&(surface==boardSurface||surface==groundSurface)&&surface.course!=null&&surface.course.match==match;
 }
}
