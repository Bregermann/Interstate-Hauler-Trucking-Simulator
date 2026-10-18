using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
namespace LWS.TruckBilliards
{
 public sealed class BilliardsButtonVisual:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
 {
  public UnityEngine.UI.Image surface;public Color normal=new Color(.035f,.19f,.19f),highlight=new Color(.06f,.43f,.4f);
  bool hovered,selected;
  void OnEnable(){hovered=selected=false;Paint();}
  Tween transition;
  void Paint(){transition?.Kill();if(surface!=null)transition=surface.DOColor(hovered||selected?highlight:normal,.1f).SetUpdate(true);}
  void OnDisable(){transition?.Kill();transition=null;if(surface!=null)surface.color=normal;}
  public void OnPointerEnter(PointerEventData e){hovered=true;Paint();}
  public void OnPointerExit(PointerEventData e){hovered=false;Paint();}
  public void OnSelect(BaseEventData e){selected=true;Paint();}
  public void OnDeselect(BaseEventData e){selected=false;Paint();}
 }
}
