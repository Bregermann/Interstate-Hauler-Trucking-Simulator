using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckSports
{
    // An explicit initial seven-mode policy. Other sports keep their specialized HUDs.
    // Graphics are suppressed rather than altering any menu/view/controller component.
    public sealed class SportsCarnivalHudPolicy : MonoBehaviour
    {
        public SportsCarnivalHudSource source;
        public Graphic[] legacyGraphics = System.Array.Empty<Graphic>();
        readonly Dictionary<Graphic, bool> original = new Dictionary<Graphic, bool>();
        readonly List<Graphic> scratch = new List<Graphic>(96);
        bool suppressed;
        public int SuppressedGraphics => original.Count;
        public void Present(bool active)
        {
            if (source == null || !source.Supported) active = false;
            if (!active) { Restore(); return; }
            suppressed = true;
            foreach (var graphic in legacyGraphics) Hide(graphic);
            // Existing broadcast/compact cards are created by their established foundation.
            // They are not serialized editor objects; bind their public hierarchy after its update.
            var menus = source.match.menus;
            HideTree(menus.hud.transform.Find("Broadcast participant cards"));
            HideTree(menus.hud.transform.Find("Broadcast status backplate"));
            foreach (var panel in menus.viewPanels)
            {
                HideTree(panel.Find("Compact owned viewport score"));
                HideTree(panel.Find("View data backing"));
                HideTree(panel.Find("View portrait backing"));
                HideTree(panel.Find("Assigned truck portrait"));
                HideTree(panel.Find("Assigned participant color"));
            }
        }
        void HideTree(Transform root)
        {
            if (root == null) return;
            root.GetComponentsInChildren(true, scratch);
            foreach (var graphic in scratch) Hide(graphic);
        }
        void Hide(Graphic graphic)
        {
            if (graphic == null) return;
            if (!original.ContainsKey(graphic)) original.Add(graphic, graphic.enabled);
            graphic.enabled = false;
        }
        public void Restore()
        {
            if (!suppressed) return;
            foreach (var pair in original) if (pair.Key != null) pair.Key.enabled = pair.Value;
            original.Clear(); suppressed = false;
        }
        void OnDisable() => Restore();
        void OnDestroy() => Restore();
    }
}
