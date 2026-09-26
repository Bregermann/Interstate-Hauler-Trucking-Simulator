using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // A view of the session deadline, never a second countdown.
    public sealed class TruckTaxiOfferCountdown : MonoBehaviour
    {
        private TruckTaxiSession session;
        private Image ring;
        private TextMeshProUGUI number;
        public float FillAmount => ring!=null ? ring.fillAmount : 0;
        public void Initialize(TruckTaxiSession value,TruckTaxiHud hud,RectTransform parent,Sprite stroke)
        {
            session=value;
            var rect=(RectTransform)transform;
            rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.9f,.91f);
            rect.sizeDelta=new Vector2(98,98);
            var track=TruckTaxiHud.Rect(rect,"Timer track",Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
            track.sprite=stroke; track.color=new Color(.25f,.3f,.32f,1); track.raycastTarget=false;
            ring=TruckTaxiHud.Rect(rect,"Remaining time",Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
            ring.sprite=stroke; ring.type=Image.Type.Filled; ring.fillMethod=Image.FillMethod.Radial360;
            ring.fillOrigin=(int)Image.Origin360.Top; ring.fillClockwise=false; ring.raycastTarget=false;
            number=hud.Text(rect,"Seconds remaining",new Vector2(.12f,.12f),new Vector2(.88f,.88f),30);
            number.alignment=TextAlignmentOptions.Center;
            if(stroke==null) Debug.LogError("Truck Taxi offer timer needs the Heat radial outline sprite.",this);
            Refresh();
        }
        private void Update() => Refresh();
        public void Refresh()
        {
            if(session==null || ring==null) return;
            ring.fillAmount=session.OfferRemainingNormalized;
            bool urgent=session.OfferRemaining<=3;
            ring.color=urgent ? new Color(1,.34f,.27f,1) : new Color(.25f,.85f,.72f,1);
            number.text=Mathf.CeilToInt(session.OfferRemaining).ToString();
        }
    }
}
