using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckKickball
{
    // Owns only presentation speed. NWH, PhysX and KickballRules still play every ball.
    public sealed class KickballAIPlayback : MonoBehaviour
    {
        public KickballMatch match;
        public const string Preference = "TruckKickball.AIPacing.v1";
        public static bool Enabled => SportsPrefs.GetInt(Preference, 0) == 1;
        public static string Label => Enabled ? "AI PLAY · FAST-FORWARD 2×" : "AI PLAY · NORMAL";
        public bool Accelerating { get; private set; }
        float previousScale = 1;
        public bool Eligible
        {
            get
            {
                if (match == null || !match.Live || match.ReplayControls || match.HomeRunCelebrating) return false;
                for (int i = 0; i < match.setup.count; i++)
                    if (match.setup.players[i].source != BowlingSource.AI && !match.WaitingToBat(i)) return false;
                return true;
            }
        }
        public void Cycle()
        {
            Release(); SportsPrefs.SetInt(Preference, Enabled ? 0 : 1); SportsPrefs.Save();
            match.RefreshExtraUI(); match.menus.Dirty();
        }
        public void Tick()
        {
            if (!Enabled || !Eligible) { Release(); return; }
            if (!Accelerating) { previousScale = Time.timeScale; Accelerating = true; }
            Time.timeScale = Mathf.Clamp(previousScale * 2, 1, 4);
        }
        public void Release()
        {
            if (!Accelerating) return;
            Accelerating = false; Time.timeScale = match != null && match.Paused ? 0 : previousScale;
        }
        void OnDisable() { Release(); }
        void OnDestroy() { Release(); }
    }
}
