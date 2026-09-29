using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRoadPepOverlay : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform rail;
        private RectTransform cursor;
        private RectTransform target;
        private RectTransform timingMarker;
        private TMP_Text caption;
        private float resultUntil;

        public void Initialize(TruckTaxiHud hud)
        {
            if (panel != null) return;
            if (hud == null || hud.Root == null) throw new ArgumentNullException(nameof(hud));
            panel = hud.Panel(hud.Root, "Road Pep challenge", new Vector2(.34f, .64f), new Vector2(.66f, .75f));
            panel.GetComponent<Image>().color = new Color(.045f, .055f, .07f, .9f);
            panel.GetComponent<Image>().raycastTarget = false;
            caption = hud.Text(panel, "Caption", new Vector2(.04f, .54f), new Vector2(.96f, .94f), 23);
            caption.fontSizeMin = 16;
            caption.alignment = TextAlignmentOptions.Center;
            caption.color = Color.white;
            rail = TruckTaxiHud.Rect(panel, "Steady rail", new Vector2(.08f, .22f), new Vector2(.92f, .34f));
            var railImage = rail.gameObject.AddComponent<Image>();
            railImage.color = new Color(.3f, .33f, .36f);
            railImage.raycastTarget = false;
            target = Indicator(rail, "Target", 62, 20, new Color(.38f, .78f, .57f));
            cursor = Indicator(rail, "Cursor", 7, 30, Color.white);
            timingMarker = Indicator(rail, "Timing marker", 7, 30, new Color(1, .76f, .26f));
            panel.gameObject.SetActive(false);
        }

        public void Show(TruckTaxiRoadPepChallenge challenge)
        {
            if (panel == null || challenge == null) return;
            resultUntil = 0;
            ShowPanel();
            bool timing = challenge.TimingPhase;
            caption.text = timing ? "ROAD PEP  |  TIMING" : "ROAD PEP  |  STEADY";
            target.gameObject.SetActive(true);
            cursor.gameObject.SetActive(!timing);
            timingMarker.gameObject.SetActive(timing);
            float width = rail.rect.width;
            target.anchoredPosition = new Vector2(((timing ? .62f : challenge.Target01) - .5f) * width, 0);
            cursor.anchoredPosition = new Vector2((challenge.Cursor01 - .5f) * width, 0);
            timingMarker.anchoredPosition = new Vector2((challenge.Marker01 - .5f) * width, 0);
        }

        public void ShowResult(TruckTaxiRoadPepChallenge challenge)
        {
            if (panel == null || challenge == null) return;
            ShowPanel();
            target.gameObject.SetActive(false);
            cursor.gameObject.SetActive(false);
            timingMarker.gameObject.SetActive(false);
            caption.text = "ROAD PEP  |  " + challenge.Rating.ToString().ToUpperInvariant() +
                "  |  EFFECT " + Mathf.RoundToInt(TruckTaxiTemporaryEffects.RoadPepQuality(challenge.Score) * 100) + "%";
            resultUntil = Time.unscaledTime + 1.5f;
        }

        private void Update()
        {
            if (panel != null && resultUntil > 0 && Time.unscaledTime >= resultUntil)
            { resultUntil = 0; panel.gameObject.SetActive(false); }
        }

        public void Hide()
        {
            resultUntil = 0;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void ShowPanel()
        {
            if (panel.gameObject.activeSelf) return;
            panel.SetAsLastSibling();
            panel.gameObject.SetActive(true);
        }

        private void OnDisable() => Hide();
        private void OnDestroy()
        {
            if (panel != null) Destroy(panel.gameObject);
        }

        private static RectTransform Indicator(RectTransform parent, string name,
            float width, float height, Color color)
        {
            var rect = TruckTaxiHud.Rect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            rect.sizeDelta = new Vector2(width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
