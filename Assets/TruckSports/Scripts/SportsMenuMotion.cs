using DG.Tweening;
using UnityEngine;
namespace LWS.TruckSports {
 // A presentation-only transition over the existing Heat menu and its input/navigation ownership.
 [DisallowMultipleComponent]public sealed class SportsMenuMotion:MonoBehaviour {
  CanvasGroup group;Tween fade;
  void OnEnable(){if(group==null)group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();fade?.Kill();group.alpha=.3f;fade=group.DOFade(1,.16f).SetEase(Ease.OutQuad).SetUpdate(true);}
  void OnDisable(){fade?.Kill();fade=null;if(group!=null)group.alpha=1;}
 }
}
