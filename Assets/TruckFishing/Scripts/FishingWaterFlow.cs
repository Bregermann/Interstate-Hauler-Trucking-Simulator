using UnityEngine;using DG.Tweening;
namespace LWS.TruckFishing {
// Shallow-water material adapter: the installed river graph's depth intersection
// disappears against this orthographic riverbed. Reuse its normal map on URP Lit.
public sealed class FishingWaterFlow:MonoBehaviour {
 Material original,material;Renderer surface;Tween flow;
 void OnEnable(){surface=GetComponent<Renderer>();if(original==null)original=surface.sharedMaterial;material=new Material(original);surface.sharedMaterial=material;flow=material.DOOffset(new Vector2(0,-1),12).SetEase(Ease.Linear).SetLoops(-1,LoopType.Restart);}
 void OnDisable(){flow?.Kill();if(surface!=null)surface.sharedMaterial=original;if(material!=null)Destroy(material);material=null;}
}
}
