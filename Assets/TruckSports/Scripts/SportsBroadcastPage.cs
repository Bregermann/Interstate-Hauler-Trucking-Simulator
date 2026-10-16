using UnityEngine;
using DG.Tweening;
namespace LWS.TruckSports {
[RequireComponent(typeof(CanvasGroup))]public sealed class SportsBroadcastPage:MonoBehaviour {
 CanvasGroup group;Tween fade;
 void OnEnable(){if(!Application.isPlaying)return;group=GetComponent<CanvasGroup>();fade?.Kill();group.alpha=.15f;fade=group.DOFade(1,.12f).SetUpdate(true).SetLink(gameObject);}
 void OnDisable(){fade?.Kill();if(group!=null)group.alpha=1;}
}
}
