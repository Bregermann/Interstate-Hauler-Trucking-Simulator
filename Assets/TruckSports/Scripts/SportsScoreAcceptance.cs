#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using Michsky.UI.Heat;
using UnityEngine;

namespace LWS.TruckSports
{
    // Explicit acceptance helpers only; never generates an award or changes rules.
    public static class SportsScoreAcceptance
    {
        public static bool CaptureOnly => Array.IndexOf(Environment.GetCommandLineArgs(), "-tsScoreOnly") >= 0;
        public static void Journal(SportsEventSession match, int expected, Action<bool, string> check)
        {
            check(match.ScoreJournal != null && match.ScoreJournal.Count == expected, "confirmed award receipts match actual physical scoring events: " + expected);
            var receipt = match.ScoreJournal?.Latest;
            if (receipt == null) return;
            check(receipt.MatchId == match.MatchId && receipt.Slot >= 0 && receipt.Slot < match.setup.count && receipt.ParticipantId == ((ISportsParticipantSlot)match.setup.players[receipt.Slot]).ParticipantId, "last actual award retains the match and stable participant identity");
        }

        public static IEnumerator CaptureAward(SportsEventSession match, Action<bool, string> check, string folder)
        {
            var presenter = match.menus.GetComponentsInChildren<SportsScorePresentation>(true).SingleOrDefault();
            check(presenter != null && presenter.session == match, "saved sport uses the existing Heat confirmed-score presenter");
            if (presenter == null) yield break;
            float scale = Time.timeScale; Time.timeScale = 1;
            try
            {
                float deadline = Time.realtimeSinceStartup + 6;
                while ((presenter.Presented == 0 || !presenter.notification.gameObject.activeInHierarchy) && Time.realtimeSinceStartup < deadline) yield return null;
                check(presenter.Presented > 0 && presenter.LastPresented.MatchId == match.MatchId, "real committed award reaches the Heat animation");
                if (presenter.Presented == 0) yield break;
                yield return new WaitForSeconds(Mathf.Max(.1f, HeatUIInternalTools.GetAnimatorClipLength(presenter.notificationAnimator, "Notification_In")) + .03f);
                Time.timeScale = 0;
                check(presenter.visibility.alpha == 1 && !presenter.visibility.blocksRaycasts && !presenter.visibility.interactable, "visible score presentation leaves gameplay/menu inputs unblocked");
                var backgrounds = presenter.notification.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(image => image.name == "Background").ToArray();
                check(backgrounds.Length > 0 && backgrounds.All(image => image.color.a == 1), "opaque notification backing protects score text from bright venue artwork");
                Debug.Log("CONTROLLED_SCORE_CAPTURE: actual committed award and Heat In animation held for real backbuffers; original simulation speed restored afterward.");
                yield return SportsWindowCapture.Save(1280, 720, folder + "/confirmed-award-720.png");
                yield return SportsWindowCapture.Save(1920, 1080, folder + "/confirmed-award-1080.png");
            }
            finally { Time.timeScale = scale; }
        }
    }
}
#endif
