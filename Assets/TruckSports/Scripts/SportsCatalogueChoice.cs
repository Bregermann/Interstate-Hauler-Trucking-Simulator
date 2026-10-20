using UnityEngine;
using UnityEngine.EventSystems;
namespace LWS.TruckSports {
public sealed class SportsCatalogueChoice:MonoBehaviour,ISelectHandler,IPointerEnterHandler {
 public SportsCatalogueHero hero;public Sprite logo,preview;public string scene,title;[TextArea]public string description;
 public void OnSelect(BaseEventData data){hero?.Select(this);}
 public void OnPointerEnter(PointerEventData data){hero?.Select(this);}
}
}
