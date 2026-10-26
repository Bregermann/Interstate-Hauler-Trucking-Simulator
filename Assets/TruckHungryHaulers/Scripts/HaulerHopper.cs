using DG.Tweening;
using UnityEngine;
namespace LWS.TruckHungryHaulers
{
 // Reuses the authored cargo body; DOTween owns presentation interpolation.
 public sealed class HaulerHopper:MonoBehaviour
 {
  public Transform bed,gate;
  public Vector3[] storagePoints;
  public bool Open{get;private set;}
  public bool Ready=>Open&&bed.localEulerAngles.x>330&&bed.localEulerAngles.x<340&&gate.localEulerAngles.y>245&&gate.localEulerAngles.y<265;
  Tween tilt,door;
  public Vector3 StoragePoint(int order,float radius)=>bed.TransformPoint(storagePoints[Mathf.Clamp(order,0,storagePoints.Length-1)]+Vector3.up*radius);
  public void SetOpen(bool open,bool instant=false)
  {
   if(Open==open&&!instant)return;Open=open;tilt?.Kill();door?.Kill();var bedRotation=new Vector3(open?-24:0,0,0);var gateRotation=new Vector3(0,open?-105:0,0);
   if(instant){bed.localRotation=Quaternion.Euler(bedRotation);gate.localRotation=Quaternion.Euler(gateRotation);}
   else{tilt=bed.DOLocalRotate(bedRotation,.55f).SetEase(Ease.InOutSine).SetUpdate(UpdateType.Fixed);door=gate.DOLocalRotate(gateRotation,.3f).SetEase(Ease.OutCubic).SetUpdate(UpdateType.Fixed);}
  }
  void OnDestroy(){tilt?.Kill();door?.Kill();}
 }
}
