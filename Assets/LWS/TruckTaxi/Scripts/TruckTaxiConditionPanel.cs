using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // WorldLife owns access/modal routing. This replaces its old condition view, not its lifecycle.
    public sealed class TruckTaxiConditionPanel : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel, content;
        private TextMeshProUGUI summary, detail;
        private readonly Dictionary<string, TextMeshProUGUI> labels = new Dictionary<string, TextMeshProUGUI>();
        private string selected;
        private bool wasPaused;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public event Action VisibilityChanged;

        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud view)
        {
            if (panel != null) return;
            if (owner == null || view == null) throw new ArgumentNullException(nameof(owner));
            host = owner; hud = view;
            panel = hud.Panel(hud.Root, "Truck condition", new Vector2(.08f,.07f), new Vector2(.92f,.93f));
            summary = hud.Text(panel, "Summary", new Vector2(.04f,.87f), new Vector2(.96f,.98f), 23);
            summary.enableAutoSizing = true; summary.fontSizeMin = 14; summary.fontSizeMax = 23;
            var scroll = TruckTaxiStoreScrollView.Create(panel, out content);
            scroll.viewport.anchorMin = new Vector2(.05f,.38f);
            detail = hud.Text(panel, "Part details", new Vector2(.05f,.14f), new Vector2(.95f,.36f), 20);
            detail.enableAutoSizing = true; detail.fontSizeMin = 14; detail.fontSizeMax = 20;
            detail.richText = false;
            hud.Button(panel, "REPAIR SELECTED", new Vector2(.05f,.025f), new Vector2(.46f,.115f), Repair);
            hud.Button(panel, "BACK", new Vector2(.74f,.025f), new Vector2(.95f,.115f), Close);
            panel.gameObject.SetActive(false);
        }
        public void Open()
        {
            if (panel == null || IsOpen || host.ComponentDamage == null) return;
            if (labels.Count == 0)
            {
                var parts = new List<TruckTaxiDamagePart>(host.ComponentDamage.Parts.Values);
                parts.Sort((a,b) => string.CompareOrdinal(a.Id,b.Id));
                foreach (var part in parts)
                {
                    string id = part.Id;
                    var button = hud.Button(content, part.DisplayName, Vector2.zero, Vector2.one, () => { selected = id; Refresh(); });
                    var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null) { label.enableAutoSizing = true; label.fontSizeMin = 13; label.fontSizeMax = 21; }
                    labels[id] = label;
                    if (selected == null) selected = id;
                }
            }
            string inspected=host.WorldLife?.InspectedPartId;
            if(!string.IsNullOrEmpty(inspected) && host.ComponentDamage.Parts.ContainsKey(inspected)) selected=inspected;
            wasPaused = host.Paused; host.SetPaused(true);
            Refresh(); panel.gameObject.SetActive(true); panel.SetAsLastSibling();
            hud.UIInput?.Focus(panel); VisibilityChanged?.Invoke();
        }
        public void Close()
        {
            if (!IsOpen) return;
            panel.gameObject.SetActive(false); host.SetPaused(wasPaused);
            hud.UIInput?.Focus(null); VisibilityChanged?.Invoke();
        }
        public bool HandleInput(TruckTaxiUIInput input)
        {
            if (!IsOpen) return false;
            input.Focus(panel);
            if (input.Cancel.WasPressedThisFrame() || input.Pause.WasPressedThisFrame()) Close();
            else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
            return true;
        }
        private void Refresh()
        {
            var damage = host.ComponentDamage;
            summary.text = TruckTaxiConditionPresentation.Summary(host);
            foreach (var pair in labels)
                if (pair.Value != null && damage.Parts.TryGetValue(pair.Key, out var part))
                    pair.Value.text = $"{part.DisplayName}\n{part.Condition:P0}  {part.State}";
            detail.text = TruckTaxiConditionPresentation.Part(damage, selected);
        }
        private void Repair()
        {
            if (selected == null) return;
            bool repaired = host.ComponentDamage.TryRepair(selected);
            Refresh();
            if (repaired) host.Persistence?.RequestAutosave("Component repair");
            else detail.text += "\nRepair not completed: check service, condition and funds.";
        }
        private void OnDisable() { if (IsOpen) Close(); }
    }
}
