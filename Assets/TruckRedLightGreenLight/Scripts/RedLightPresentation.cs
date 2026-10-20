using UnityEngine;
using TMPro;
using Cinemachine;
namespace LWS.TruckRedLightGreenLight
{
    // Read-only venue presentation. Rules and destruction keep their existing
    // authorities; Cinemachine supplies the impulse signal and camera response.
    public sealed class RedLightPresentation : MonoBehaviour
    {
        public RedLightMatch match;
        public TMP_Text[] boards;
        public CinemachineImpulseSource impulse;
        public CinemachineIndependentImpulseListener[] listeners;
        int previousExplosions; float nextRefresh;
        void Update()
        {
            bool live = match.Live && !match.ReplayControls;
            foreach (var listener in listeners) listener.enabled = live;
            if (match.Explosions < previousExplosions) previousExplosions = match.Explosions;
            if (live && match.Explosions > previousExplosions)
            {
                previousExplosions = match.Explosions;
                impulse.GenerateImpulseWithVelocity(new Vector3(.9f, 1.2f, .3f));
            }
            if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .2f;
            for (int board = 0; board < boards.Length; board++)
            {
                var text = new System.Text.StringBuilder("<color=#FFCF66>LIVE STADIUM STANDINGS</color>\n");
                int first = board * 6, end = Mathf.Min(first + 6, match.setup.count);
                for (int i = first; i < end; i++)
                {
                    match.TryReadScoreCard(i, out string value, out string detail);
                    text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(match.setup.players[i].color)).Append('>').Append(i + 1).Append(". ").Append(match.setup.players[i].name.Replace("<", "")).Append("</color>  ").Append(value).Append("\n<size=70%>").Append(detail).Append("</size>\n");
                }
                text.Append("<size=65%>").Append(match.LegalFinishes).Append(" FINISHED  /  ").Append(match.Explosions).Append(" ELIMINATED</size>"); boards[board].text = text.ToString();
            }
        }
    }
}
