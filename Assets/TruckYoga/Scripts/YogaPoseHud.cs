using TMPro;
using UnityEngine;

namespace LWS.TruckYoga
{
    public sealed class YogaPoseHud : MonoBehaviour
    {
        public YogaMatch match;
        public TMP_Text target;
        public TMP_Text[] cards;
        float next;
        RectTransform targetBacking;
        void TargetContrast()
        {
            if (target == null) return;
            if (targetBacking == null)
            {
                var owned = new GameObject("Yoga actual pose target backing", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                owned.transform.SetParent(target.transform.parent, false); targetBacking = owned.GetComponent<RectTransform>();
                var ink = owned.GetComponent<UnityEngine.UI.Image>(); ink.color = LWS.TruckSports.SportsBroadcastCanvas.Ink; ink.raycastTarget = false;
                owned.AddComponent<LWS.TruckSports.SportsBroadcastPlate>().cut = 8;
                owned.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
            }
            var rect = target.rectTransform; targetBacking.anchorMin = rect.anchorMin; targetBacking.anchorMax = rect.anchorMax; targetBacking.pivot = rect.pivot;
            targetBacking.anchoredPosition = rect.anchoredPosition;
            targetBacking.sizeDelta = new Vector2(Mathf.Min(rect.rect.width, target.GetPreferredValues(target.text).x + 36), rect.rect.height + 8);
            targetBacking.gameObject.SetActive(target.isActiveAndEnabled && !string.IsNullOrWhiteSpace(target.text));
        }

        void Update()
        {
            if (!match.Live || Time.unscaledTime < next) return; next = Time.unscaledTime + .1f;
            var wanted = YogaPoses.Target(match.rules.Pose);
            target.text = $"TARGET · PITCH {wanted.x:0}° · ROLL {wanted.y:0}° · LIFT {wanted.z:0.00}m · TWIST {wanted.w:0}°";
            TargetContrast();
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].transform.parent.gameObject.SetActive(i < match.setup.count);
                if (i >= match.setup.count) continue;
                var rig = match.rigs[i]; var actual = rig.Achieved;
                cards[i].text = $"<color=#{ColorUtility.ToHtmlStringRGB(match.setup.players[i].color)}>{match.setup.players[i].name}</color> · {match.rules.accuracy[i] * 100:0}%\n{rig.BankLabel} · HOLD {Mathf.Min(3, match.rules.hold[i]):0.0}/3s\nP{actual.x:0}° R{actual.y:0}° L{actual.z:0.00}m T{actual.w:0}°";
            }
        }
    }
}
