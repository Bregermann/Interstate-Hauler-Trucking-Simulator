#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using UnityEngine;

namespace LWS.TruckSports
{
    // Explicit acceptance helpers only; never generates an award or changes rules.
    public static class SportsScoreAcceptance
    {
        public static bool CaptureOnly => Array.IndexOf(Environment.GetCommandLineArgs(), "-tsScoreOnly") >= 0;
        public sealed class TeamTally : IDisposable
        {
            readonly SportsEventSession match;
            readonly double[] totals = new double[2];
            readonly HashSet<string> ids = new HashSet<string>();
            string matchId; int count, invalid;
            public TeamTally(SportsEventSession owner) { match = owner; match.ScoreConfirmed += Record; }
            void CurrentMatch()
            {
                if (matchId == match.MatchId) return;
                matchId = match.MatchId; Array.Clear(totals, 0, totals.Length); ids.Clear(); count = invalid = 0;
            }
            void Record(SportsScoreEvent receipt)
            {
                CurrentMatch(); count++;
                int team = receipt.TeamId == "blue" ? 0 : receipt.TeamId == "orange" ? 1 : -1;
                if (receipt.MatchId != matchId || !ids.Add(receipt.EventId) || !receipt.IsTeam || receipt.Slot != -1 || receipt.ParticipantId != null || team < 0) { invalid++; return; }
                if (receipt.RecipientId != "team:" + receipt.TeamId || receipt.DisplayName != (team == 0 ? "BLUE TEAM" : "ORANGE TEAM")) invalid++;
                totals[team] += receipt.Delta;
                if (Math.Abs(totals[team] - receipt.Total) > .001) invalid++;
            }
            public void Verify(int[] expected, Action<bool, string> check)
            {
                CurrentMatch();
                check(invalid == 0 && match.ScoreJournal != null && match.ScoreJournal.Count == count, "every team award has one semantic ID and a team recipient without fictitious player credit; receipts=" + count);
                check(Math.Abs(totals[0] - expected[0]) < .001 && Math.Abs(totals[1] - expected[1]) < .001, "confirmed team award deltas exactly reconstruct both authoritative team totals");
            }
            public void Dispose() { if (match != null) match.ScoreConfirmed -= Record; }
        }
        public sealed class Tally : IDisposable
        {
            readonly SportsEventSession match;
            readonly bool strictTotals;
            readonly double[] totals = new double[12];
            readonly HashSet<string> ids = new HashSet<string>();
            string matchId; int count, invalid;
            public Tally(SportsEventSession owner, bool strict = true) { match = owner; strictTotals = strict; match.ScoreConfirmed += Record; }
            void CurrentMatch()
            {
                if (matchId == match.MatchId) return;
                matchId = match.MatchId; Array.Clear(totals, 0, totals.Length); ids.Clear(); count = invalid = 0;
            }
            void Record(SportsScoreEvent receipt)
            {
                CurrentMatch(); count++;
                if (receipt.MatchId != matchId || !ids.Add(receipt.EventId) || receipt.Slot < 0 || receipt.Slot >= match.setup.count) { invalid++; return; }
                if (receipt.ParticipantId != ((ISportsParticipantSlot)match.setup.players[receipt.Slot]).ParticipantId) invalid++;
                totals[receipt.Slot] += receipt.Delta;
                double difference = receipt.Total - totals[receipt.Slot];
                if (strictTotals ? Math.Abs(difference) > .001 : difference < -.001 || difference >= 10.001) invalid++;
            }
            public void Verify(Func<int, double> expected, Action<bool, string> check)
            {
                CurrentMatch();
                check(invalid == 0 && match.ScoreJournal != null && match.ScoreJournal.Count == count, "every confirmed award has one semantic ID, correct participant and consistent total; receipts=" + count);
                bool correct = true; for (int slot = 0; slot < match.setup.count; slot++) correct &= Math.Abs(totals[slot] - expected(slot)) < .001;
                check(correct, "confirmed award deltas account exactly for the authoritative scores or completed control milestones");
            }
            public void Dispose() { if (match != null) match.ScoreConfirmed -= Record; }
        }
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
