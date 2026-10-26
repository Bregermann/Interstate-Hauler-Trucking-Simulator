using DG.Tweening;
using UnityEngine;

namespace LWS.TruckSkeeBall
{
 // The venue supplies physical surfaces only. SkeeRunner's existing whole-body
 // opening observation and quiet final-rest rule remain the only score source.
 public sealed class SkeeCarnivalVenue:MonoBehaviour
 {
  public SkeeMatch match;
  public BoxCollider[] zeroReturns,retainingNets;
  public Renderer[] arcadeBulbs;
  public bool suppliedReferenceAvailable;
  [HideInInspector]public Object[] transientAssets;
  MaterialPropertyBlock block;Tween glow;float brightness;
  void OnEnable(){block??=new MaterialPropertyBlock();glow=DOVirtual.Float(.45f,1.25f,1.2f,value=>brightness=value).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(gameObject);}
  void LateUpdate(){if(arcadeBulbs==null)return;block.SetColor("_EmissionColor",new Color(1,.46f,.12f)*brightness);foreach(var bulb in arcadeBulbs)if(bulb!=null)bulb.SetPropertyBlock(block);}
  void OnDisable(){glow?.Kill();glow=null;if(arcadeBulbs!=null)foreach(var bulb in arcadeBulbs)if(bulb!=null)bulb.SetPropertyBlock(null);}
  void OnDestroy(){if(transientAssets!=null)foreach(var asset in transientAssets)if(asset!=null)Destroy(asset);}
 }
}
