using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiWorldTravelPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel;
        private TextMeshProUGUI status;
        private bool wasPaused;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud view)
        {
            host = owner; hud = view;
            panel = hud.Panel(hud.Root,"Travel to another map",new Vector2(.23f,.20f),new Vector2(.77f,.80f));
            hud.Text(panel,"Title",new Vector2(.08f,.78f),new Vector2(.92f,.95f),34).text="TRAVEL TO ANOTHER MAP";
            status = hud.Text(panel,"Region and travel status",new Vector2(.08f,.40f),new Vector2(.92f,.75f),25);
            hud.Button(panel,"TRAVEL",new Vector2(.08f,.23f),new Vector2(.92f,.37f),Confirm);
            hud.Button(panel,"CANCEL / BACK",new Vector2(.08f,.05f),new Vector2(.92f,.19f),Close);
            panel.gameObject.SetActive(false);
        }
        public void Open()
        {
            if (TruckTaxiWorldTravel.IsBusy || host?.CanResume != true) return;
            wasPaused = host.Paused; host.SetPaused(true);
            string scene = host.gameObject.scene.name;
            status.text = "CURRENT REGION: " + TruckTaxiWorldCatalog.Label(scene).ToUpperInvariant() +
                "\n\nTRAVEL TO: " + TruckTaxiWorldCatalog.Label(TruckTaxiWorldCatalog.Other(scene)).ToUpperInvariant() +
                "\n\n" + (host.Persistence.WorldTransferBlockReason ?? "Your career and truck will be preserved.");
            panel.gameObject.SetActive(true); panel.SetAsLastSibling(); hud.UIInput.Focus(panel);
        }
        public void Close()
        { panel.gameObject.SetActive(false); host.SetPaused(wasPaused); }
        private void Confirm()
        {
            if (TruckTaxiWorldTravel.TryTravel(host,TruckTaxiWorldCatalog.Other(host.gameObject.scene.name))) panel.gameObject.SetActive(false);
            else status.text = TruckTaxiWorldTravel.LastStatus;
        }
        public bool HandleInput(TruckTaxiUIInput input)
        {
            if (!IsOpen) return false;
            if (input.Cancel.WasPressedThisFrame() || input.Pause.WasPressedThisFrame()) Close();
            else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
            input.Focus(IsOpen ? panel : null); return true;
        }
    }
}
