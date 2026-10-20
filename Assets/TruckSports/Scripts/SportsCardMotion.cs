using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
namespace LWS.TruckSports
{
    public sealed class SportsCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        bool hover, selected; Tween scaleTween, colorTween; Image background;
        void Awake() { background = GetComponent<Image>(); }
        void Update(){var focus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;bool inside=focus!=null&&focus.transform.IsChildOf(transform);if(inside!=selected){selected=inside;Refresh();}}
        public void OnPointerEnter(PointerEventData data) { hover = true; GetComponentInChildren<SportsCatalogueChoice>(true)?.OnPointerEnter(data); Refresh(); }
        public void OnPointerExit(PointerEventData data) { hover = false; Refresh(); }
        public void Select(bool value) { selected = value; Refresh(); }
        void Refresh()
        {
            bool focus = hover || selected; scaleTween?.Kill(); colorTween?.Kill();
            scaleTween = transform.DOScale(focus ? 1.025f : 1, .18f).SetEase(Ease.OutQuad).SetUpdate(true);
            if (background != null) colorTween = background.DOColor(focus ? new Color(.10f, .135f, .19f, .99f) : SportsBroadcastCanvas.Ink, .18f).SetUpdate(true);
        }
        void OnDisable() { scaleTween?.Kill(); colorTween?.Kill(); hover = selected = false; transform.localScale = Vector3.one; if (background != null) background.color = SportsBroadcastCanvas.Ink; }
    }
}
