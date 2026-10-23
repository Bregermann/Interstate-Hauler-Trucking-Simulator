using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckSports
{
    // Opt-in scene binding. Heat owns notification transitions; the game owns
    // confirmed receipts. This component never reads HUD text or writes points.
    [DefaultExecutionOrder(150)]
    public sealed class SportsScorePresentation : MonoBehaviour
    {
        public SportsEventSession session;
        public NotificationManager notification;
        public CanvasGroup visibility;
        public Image portrait, accent;
        public Animator notificationAnimator;
        [Tooltip("Non-blocking notification in the upper screen margin above the standard results card.")]
        public Vector2 resultPosition = new Vector2(0, -8), resultSize = new Vector2(1120, 76);
        sealed class Pending
        {
            public SportsScoreEvent latest;
            public double delta;
            public int count;
        }
        readonly List<Pending> pending = new List<Pending>();
        string match;
        float next;
        RectTransform layout;
        Vector2 playAnchorMin, playAnchorMax, playPivot, playPosition, playSize;
        bool resultLayout;
        public int Presented { get; private set; }
        public SportsScoreEvent LastPresented { get; private set; }
        void Awake()
        {
            layout = (RectTransform)transform;
            playAnchorMin = layout.anchorMin; playAnchorMax = layout.anchorMax; playPivot = layout.pivot;
            playPosition = layout.anchoredPosition; playSize = layout.sizeDelta;
        }
        void OnEnable() { if (session != null) session.ScoreConfirmed += Accept; }
        void OnDisable()
        {
            if (session != null) session.ScoreConfirmed -= Accept;
            pending.Clear(); if (portrait != null) portrait.transform.DOKill();
        }
        void Accept(SportsScoreEvent receipt)
        {
            if (receipt.MatchId != session.MatchId) return;
            if (match != receipt.MatchId) ResetMatch(receipt.MatchId);
            // Combine rapid awards only when their actual identities, reason,
            // unit and tier agree. Names may deliberately be identical.
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var item = pending[i];
                if (item.latest.RecipientId != receipt.RecipientId) continue;
                // Do not merge a later ordinary award into one queued before
                // this recipient's winner/other event. Totals follow commits.
                if (receipt.Kind == SportsScoreKind.Points && item.latest.Kind == SportsScoreKind.Points && receipt.Tier <= SportsScoreTier.Major && item.latest.Reason == receipt.Reason && item.latest.Tier == receipt.Tier && item.latest.Unit == receipt.Unit)
                { item.delta += receipt.Delta; item.count++; item.latest = receipt; return; }
                break;
            }
            int position = pending.Count;
            if (receipt.Tier >= SportsScoreTier.RoundWinner)
            {
                position = 0;
                while (position < pending.Count && pending[position].latest.Tier >= receipt.Tier) position++;
                // Prioritize across recipients while preserving the winner's
                // earlier committed awards, including real negative changes.
                for (int i = position; i < pending.Count; i++)
                    if (pending[i].latest.RecipientId == receipt.RecipientId) position = i + 1;
            }
            pending.Insert(position, new Pending { latest = receipt, delta = receipt.Delta, count = 1 });
        }
        void ResetMatch(string id)
        {
            match = id; pending.Clear(); Presented = 0; LastPresented = null;
            notification.MinimizeNotification(); next = Time.time + .7f;
        }
        void LateUpdate()
        {
            if (session == null || notification == null) return;
            if (match != session.MatchId) ResetMatch(session.MatchId);
            bool result = session.Finished && session.menus.Screen == SportsEventScreen.Result;
            bool showing = session.Running && !session.Paused && (result || !session.Finished && session.menus.Screen == SportsEventScreen.Play);
            visibility.alpha = showing ? 1 : 0;
            if (layout != null && resultLayout != result)
            {
                resultLayout = result;
                layout.anchorMin = result ? new Vector2(.5f, 1) : playAnchorMin;
                layout.anchorMax = result ? new Vector2(.5f, 1) : playAnchorMax;
                layout.pivot = result ? new Vector2(.5f, 1) : playPivot;
                layout.anchoredPosition = result ? resultPosition : playPosition;
                layout.sizeDelta = result ? resultSize : playSize;
            }
            // A final physical deposit can close the match in the same step.
            // Its already-confirmed receipt still belongs on the results page.
            if (!session.Running) { pending.Clear(); return; }
            if (!showing || Time.time < next || pending.Count == 0) return;
            var item = pending[0]; pending.RemoveAt(0); var receipt = item.latest;
            if (receipt.MatchId != session.MatchId) return;
            Color color = receipt.Color;
            accent.color = color; portrait.color = Color.Lerp(color, Color.white, .4f);
            string delta = (item.delta >= 0 ? "+" : "") + item.delta.ToString("0.##", CultureInfo.InvariantCulture);
            string measurement = receipt.Kind == SportsScoreKind.Outcome
                ? "<size=140%><b>" + receipt.OutcomeValue.ToString("0.000", CultureInfo.InvariantCulture) + "</b></size> " + Escape(receipt.Unit)
                : "<size=140%><b>" + delta + "</b></size> " + Escape(receipt.Unit) + "     " + receipt.Total.ToString("0.##", CultureInfo.InvariantCulture) + " TOTAL" + (item.count > 1 ? "  ·  " + item.count + " AWARDS" : "");
            notification.notificationText = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + "><b>" + Escape(receipt.DisplayName) + "</b></color>  ·  " + Escape(receipt.Reason)
                + "\n" + measurement;
            notification.minimizeAfter = receipt.Tier == SportsScoreTier.Small ? .85f : receipt.Tier == SportsScoreTier.Major ? 1.35f : 2;
            notification.UpdateUI(); notification.ExpandNotification();
            portrait.transform.DOKill(); portrait.transform.localScale = Vector3.one;
            portrait.transform.DOPunchScale(Vector3.one * (receipt.Tier == SportsScoreTier.Small ? .12f : .24f), .45f, 3, .4f).SetUpdate(true).SetLink(portrait.gameObject);
            next = Time.time + notification.minimizeAfter + HeatUIInternalTools.GetAnimatorClipLength(notificationAnimator, "Notification_Out") + .15f;
            LastPresented = receipt; Presented++;
        }
        static string Escape(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");
    }
}
