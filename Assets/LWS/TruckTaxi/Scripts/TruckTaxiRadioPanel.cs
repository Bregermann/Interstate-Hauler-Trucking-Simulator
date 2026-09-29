using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiRadioPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel;
        private TextMeshProUGUI state, card;
        private bool wasPaused;
        public bool IsOpen => panel!=null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public void Initialize(TruckTaxiBootstrap value,TruckTaxiHud view)
        {
            host=value; hud=view;
            panel=hud.Panel(hud.Root,"Truck radio",new Vector2(.32f,.40f),new Vector2(.70f,.83f));
            hud.Text(panel,"Heading",new Vector2(.04f,.84f),new Vector2(.96f,.98f),32).text="TRUCK RADIO";
            state=hud.Text(panel,"Program",new Vector2(.04f,.54f),new Vector2(.96f,.84f),24);
            hud.Button(panel,"PREVIOUS",new Vector2(.04f,.39f),new Vector2(.32f,.51f),()=>host.Radio.ChangeStation(-1));
            hud.Button(panel,"ON / OFF",new Vector2(.36f,.39f),new Vector2(.64f,.51f),()=>host.Radio.Toggle());
            hud.Button(panel,"NEXT",new Vector2(.68f,.39f),new Vector2(.96f,.51f),()=>host.Radio.ChangeStation(1));
            hud.Button(panel,"VOL -",new Vector2(.04f,.23f),new Vector2(.32f,.35f),()=>host.Radio.SetVolume(host.Radio.Volume-.05f));
            hud.Button(panel,"MUTE",new Vector2(.36f,.23f),new Vector2(.64f,.35f),()=>host.Radio.ToggleMute());
            hud.Button(panel,"VOL +",new Vector2(.68f,.23f),new Vector2(.96f,.35f),()=>host.Radio.SetVolume(host.Radio.Volume+.05f));
            hud.Button(panel,"WEATHER",new Vector2(.04f,.05f),new Vector2(.32f,.18f),()=>host.Radio.WeatherBulletin());
            hud.Button(panel,"NEXT TRACK",new Vector2(.36f,.05f),new Vector2(.64f,.18f),()=>host.Radio.NextTrack());
            hud.Button(panel,"CLOSE",new Vector2(.68f,.05f),new Vector2(.96f,.18f),Close);
            card=hud.Text(hud.Root,"Radio station card",new Vector2(.025f,.20f),new Vector2(.295f,.32f),22);
            panel.gameObject.SetActive(false);
        }
        public void Open()
        {
            if(IsOpen || host.Radio==null || host.FastTravel?.IsTravelling==true || hud.NormalHudSuppressed ||
                host.Companions?.IsBusy==true || host.Kaiju?.IsCapturingPlayer==true || host.Fuel?.IsRescuing==true ||
                host.Roadside?.IsRecovering==true || host.Session.State==TruckTaxiState.RideOffered) return;
            wasPaused=host.Paused; panel.gameObject.SetActive(true); panel.SetAsLastSibling(); host.SetPaused(true); hud.UIInput.Focus(panel);
        }
        public void Close() { if(!IsOpen) return; panel.gameObject.SetActive(false); host.SetPaused(wasPaused); }
        private void Update()
        {
            if(host.Radio==null) return;
            if(IsOpen) state.text=host.Radio.Display+"\nVOLUME "+Mathf.RoundToInt(host.Radio.Volume*100)+"%"+(host.Radio.Muted ? "  MUTED" : "");
            card.gameObject.SetActive(host.Radio.CardVisible && !hud.NormalHudSuppressed && !IsOpen);
            if(card.gameObject.activeSelf) card.text=host.Radio.Display;
        }
    }
}
