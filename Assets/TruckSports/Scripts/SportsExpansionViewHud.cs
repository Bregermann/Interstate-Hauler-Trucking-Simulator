using UnityEngine;
using UnityEngine.UI;
namespace LWS.TruckSports
{
    // Saved-scene presentation adapter over existing viewport/HUD ownership.
    // Heat/TMP and the shared rounded broadcast plate remain the UI foundation.
    public sealed class SportsExpansionViewHud : MonoBehaviour
    {
        public SportsEventSession match;
        public SportsDrivingViews views;
        public SportsRallyViews rally;
        public bool minimalDistance;
        SportsRallyViews Roster=>views!=null?views.rally:rally;
        bool ReplacedByCompactCard=>match!=null&&match.CompactBroadcastStatus&&match.CompactViewportHud;
        readonly Image[] portraits = new Image[4], stripes = new Image[4];
        void Start()
        {
            if(ReplacedByCompactCard)return;
            var sprite = Resources.Load<Sprite>("Broadcast truck portrait");
            for (int i = 0; i < 4; i++)
            {
                var panel = match.menus.viewPanels[i];
                var backing = panel.Find("View data backing") as RectTransform;
                if (backing == null) continue;
                backing.sizeDelta = minimalDistance?new Vector2(260,68):new Vector2(810, 145);
                var plate = backing.GetComponent<SportsBroadcastPlate>() ?? backing.gameObject.AddComponent<SportsBroadcastPlate>(); plate.cut = 15;
                Image Image(string name, Vector2 position, Vector2 size)
                {
                    var root = new GameObject(name, typeof(RectTransform), typeof(Image)); root.transform.SetParent(panel, false);
                    var rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
                    var image = root.GetComponent<Image>(); image.raycastTarget = false; return image;
                }
                portraits[i] = Image("Assigned truck portrait", minimalDistance?new Vector2(20,-18):new Vector2(22, -24), minimalDistance?new Vector2(44,44):new Vector2(52, 52)); portraits[i].sprite = sprite; portraits[i].preserveAspect = true;
                stripes[i] = Image("Assigned participant color", minimalDistance?new Vector2(12,-18):new Vector2(12, -24), minimalDistance?new Vector2(4,44):new Vector2(4, 112));
                var label = match.menus.viewTexts[i]; label.rectTransform.anchoredPosition = minimalDistance?new Vector2(78,-16):new Vector2(90, -18); label.rectTransform.sizeDelta = minimalDistance?new Vector2(180,48):new Vector2(706, 120); label.fontSizeMax = minimalDistance?28:22; label.fontSizeMin = 18;
                if(minimalDistance)label.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
            }
        }
        void LateUpdate()
        {
            for (int view = 0; view < 4; view++) if (portraits[view] != null)
            {
                bool active = !ReplacedByCompactCard&&match.MultiViewHUD && Roster!=null&&view < Roster.Humans;
                portraits[view].gameObject.SetActive(active); stripes[view].gameObject.SetActive(active);
                if (!active) continue;
                var color = match.setup.players[Roster.owners[view]].color;
                portraits[view].color = Color.Lerp(color, Color.white, .25f); stripes[view].color = color;
            }
        }
    }
}
