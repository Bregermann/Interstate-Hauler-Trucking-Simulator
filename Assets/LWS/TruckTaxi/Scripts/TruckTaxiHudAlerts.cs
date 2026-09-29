using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiAlertPriority { Normal, High, Critical }

    // One bounded notification queue; permanent controls never share alert coordinates.
    public sealed class TruckTaxiHudAlerts : MonoBehaviour
    {
        private sealed class Notice
        {
            public string Key, Text;
            public TruckTaxiAlertPriority Priority;
            public float Remaining;
        }
        private readonly List<Notice> pending = new List<Notice>();
        private Notice critical, secondary;
        private TruckTaxiHud hud;
        private RectTransform root;
        private TextMeshProUGUI criticalText, secondaryText;
        public RectTransform NeedsSlot { get; private set; }
        public RectTransform CommandsSlot { get; private set; }
        public event Action<string, TruckTaxiAlertPriority> Published;
        public string CurrentText => critical?.Text ?? secondary?.Text ?? "";
        public int PendingCount => pending.Count;

        public void Initialize(TruckTaxiHud view)
        {
            hud = view;
            root = TruckTaxiHud.Rect(view.Root, "Top HUD layout", new Vector2(.31f, 1), new Vector2(.99f, 1));
            root.pivot = new Vector2(.5f, 1); root.anchoredPosition = new Vector2(0, -12);
            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            layout.childControlWidth = true; layout.childForceExpandWidth = true;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var permanent = TruckTaxiHud.Rect(root, "Time needs and commands", Vector2.zero, Vector2.one);
            permanent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 72);
            var height = permanent.gameObject.AddComponent<LayoutElement>(); height.minHeight = height.preferredHeight = 72;
            var row = permanent.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12; row.childControlHeight = row.childControlWidth = true;
            row.childForceExpandHeight = true; row.childForceExpandWidth = false;
            NeedsSlot = TruckTaxiHud.Rect(permanent, "Needs slot", Vector2.zero, Vector2.one);
            var needs = NeedsSlot.gameObject.AddComponent<LayoutElement>(); needs.minWidth = 600; needs.flexibleWidth = 1;
            CommandsSlot = TruckTaxiHud.Rect(permanent, "Command slot", Vector2.zero, Vector2.one);
            var commands = CommandsSlot.gameObject.AddComponent<LayoutElement>(); commands.minWidth = commands.preferredWidth = 380;
            criticalText = CreateNotice(view, "Critical alert", new Color(.30f, .07f, .05f, .95f));
            secondaryText = CreateNotice(view, "Secondary alert", new Color(.06f, .12f, .14f, .94f));
            Refresh();
        }
        private TextMeshProUGUI CreateNotice(TruckTaxiHud view, string name, Color color)
        {
            var lane = TruckTaxiHud.Rect(root, name+" lane", Vector2.zero, Vector2.one);
            var laneLayout = lane.gameObject.AddComponent<HorizontalLayoutGroup>();
            laneLayout.childControlHeight = laneLayout.childControlWidth = true;
            laneLayout.childForceExpandHeight = laneLayout.childForceExpandWidth = false;
            var panel = view.Panel(lane, name, Vector2.zero, Vector2.one);
            var width = panel.gameObject.AddComponent<LayoutElement>(); width.minWidth = width.preferredWidth = 730;
            panel.GetComponent<Image>().color = color;
            var row = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            row.padding = new RectOffset(16, 16, 10, 10);
            row.childControlHeight = row.childControlWidth = true;
            row.childForceExpandHeight = false;
            var text = view.Text(panel, "Message", Vector2.zero, Vector2.one, 23);
            text.raycastTarget = false;
            text.gameObject.AddComponent<LayoutElement>().minHeight = 32;
            return text;
        }
        public void Publish(string key, string text, TruckTaxiAlertPriority priority = TruckTaxiAlertPriority.Normal, float seconds = 8)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            key = string.IsNullOrEmpty(key) ? text : key;
            if (critical?.Key == key || secondary?.Key == key || pending.Exists(n => n.Key == key)) return;
            if (pending.Count >= 24)
            {
                int low = pending.FindIndex(n => n.Priority < priority);
                if (low < 0) return;
                pending.RemoveAt(low);
            }
            pending.Add(new Notice { Key = key, Text = text, Priority = priority, Remaining = Mathf.Clamp(seconds, 3, 20) });
            Published?.Invoke(text, priority);
            Promote(); Refresh();
        }
        private void Promote()
        {
            if (critical == null)
            {
                int i = pending.FindIndex(n => n.Priority == TruckTaxiAlertPriority.Critical);
                if (i >= 0) { critical = pending[i]; pending.RemoveAt(i); }
            }
            if (secondary != null) return;
            int best = -1;
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Priority != TruckTaxiAlertPriority.Critical && (best < 0 || pending[i].Priority > pending[best].Priority)) best = i;
            if (best >= 0) { secondary = pending[best]; pending.RemoveAt(best); }
        }
        private void Update()
        {
            if (hud == null) return;
            root.gameObject.SetActive(!hud.NormalHudSuppressed);
            if (hud.NormalHudSuppressed) return;
            bool dirty = false;
            if (critical != null && (critical.Remaining -= Time.unscaledDeltaTime) <= 0) { critical = null; dirty = true; }
            if (secondary != null && (secondary.Remaining -= Time.unscaledDeltaTime) <= 0) { secondary = null; dirty = true; }
            if (dirty) { Promote(); Refresh(); }
        }
        private void Refresh()
        {
            if (criticalText == null) return;
            criticalText.text = critical?.Text ?? "";
            secondaryText.text = secondary?.Text ?? "";
            criticalText.transform.parent.parent.gameObject.SetActive(critical != null);
            secondaryText.transform.parent.parent.gameObject.SetActive(secondary != null);
            LayoutRebuilder.MarkLayoutForRebuild(root);
        }
    }
}
