using UnityEngine;using DG.Tweening;
namespace LWS.TruckHungryHaulers
{
 public sealed class HaulerScoop:MonoBehaviour
 {
  public HaulerMatch match;public int slot;public BoxCollider mouth;public Transform moving;public Renderer[] paint;public bool Extended{get;private set;}Tween animation;
  public bool Ready=>Extended&&moving.localScale.z>.92f;
  public void Deploy(bool extend,bool instant=false){if(Extended==extend&&!instant)return;Extended=extend;animation?.Kill();if(instant){var scale=moving.localScale;scale.z=extend?1:.4f;moving.localScale=scale;}else animation=moving.DOScaleZ(extend?1:.4f,.32f).SetEase(Ease.OutCubic).SetUpdate(UpdateType.Fixed);}
  public void Colorize(Color color){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);foreach(var renderer in paint)renderer.SetPropertyBlock(block);}
  void OnDestroy(){animation?.Kill();}
 }
}
