using TMPro;
using UnityEngine;

namespace LWS.TruckHitchSports
{
    [DisallowMultipleComponent]
    public sealed class HitchRunOwnershipBadge : MonoBehaviour
    {
        public HitchRunMatch Match;
        public TrailerBody Trailer;
        public TMP_Text Label;
        float nextRefresh;

        void LateUpdate()
        {
            if (Label == null || Match == null || Trailer == null) return;
            bool show = Match.Running && Trailer.gameObject.activeInHierarchy && Trailer.Deliveries == 0;
            Label.enabled = show;
            if (!show || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .1f;
            string caption = Match.CargoOwnershipLabel(Trailer, out Color color);
            if (Label.text != caption) Label.text = caption;
            Label.color = color;
        }
    }
}
