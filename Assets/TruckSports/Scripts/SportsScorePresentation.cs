using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using Michsky.UI.Heat;
using TMPro;
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
        [Tooltip("Optional original sport lifecycle implementing the shared read-only score contract.")]
        public MonoBehaviour alternateSession;
        ISportsScorePresentationSource Source => session != null ? (ISportsScorePresentationSource)session : alternateSession as ISportsScorePresentationSource;
        public NotificationManager notification;
        public CanvasGroup visibility;
        public Image portrait, accent;
        public Animator notificationAnimator;
        [Tooltip("-1 keeps shared broadcast feedback. Otherwise show only the actual participant owning this camera viewport.")]
        public int viewport = -1;
        [Tooltip("Non-blocking notification in the upper screen margin above the standard results card.")]
        public Vector2 resultPosition = new Vector2(0, -8), resultSize = new Vector2(1120, 76);
        sealed class Pending
        {
            public SportsScoreEvent latest;
            public double delta;
            public int count;
        }
        readonly List<Pending> pending = new List<Pending>();
        string match, terminalOutcomeMatch;
        float next;
        RectTransform layout;
        Vector2 playAnchorMin, playAnchorMax, playPivot, playPosition, playSize;
        TMP_Text message;
        RectTransform messageLayout;
        Vector2 playTextAnchorMin, playTextAnchorMax, playTextPivot, playTextOffsetMin, playTextOffsetMax;
        Vector4 playTextMargin;
        float playTextSize, playTextMin, playTextMax;
        bool playTextAutoSize, textLayoutCached;
        bool resultLayout;
        int viewportOwner = -1;
        float audienceReady;
        public int ViewportOwner => viewportOwner;
        public int Presented { get; private set; }
        public SportsScoreEvent LastPresented { get; private set; }
        void Awake()
        {
            layout = (RectTransform)transform;
            playAnchorMin = layout.anchorMin; playAnchorMax = layout.anchorMax; playPivot = layout.pivot;
            playPosition = layout.anchoredPosition; playSize = layout.sizeDelta;
            CacheTextLayout();
        }
        void OnEnable() { var source = Source; if (source != null) source.ScoreConfirmed += Accept; }
        void OnDisable()
        {
            var source = Source; if (source != null) source.ScoreConfirmed -= Accept;
            pending.Clear(); if (portrait != null) portrait.transform.DOKill();
            SetResultLayout(false);
        }
        void CacheTextLayout()
        {
            if (textLayoutCached || notification == null) return;
            message = notification.GetComponentInChildren<TMP_Text>(true);
            if (message == null) return;
            messageLayout = message.rectTransform;
            playTextAnchorMin = messageLayout.anchorMin; playTextAnchorMax = messageLayout.anchorMax;
            playTextPivot = messageLayout.pivot; playTextOffsetMin = messageLayout.offsetMin; playTextOffsetMax = messageLayout.offsetMax;
            playTextMargin = message.margin; playTextSize = message.fontSize;
            playTextAutoSize = message.enableAutoSizing; playTextMin = message.fontSizeMin; playTextMax = message.fontSizeMax;
            textLayoutCached = true;
        }
        void SetResultLayout(bool result)
        {
            if (layout == null || resultLayout == result) return;
            CacheTextLayout(); resultLayout = result;
            layout.anchorMin = result ? new Vector2(.5f, 1) : playAnchorMin;
            layout.anchorMax = result ? new Vector2(.5f, 1) : playAnchorMax;
            layout.pivot = result ? new Vector2(.5f, 1) : playPivot;
            layout.anchoredPosition = result ? resultPosition : playPosition;
            layout.sizeDelta = result ? resultSize : playSize;
            if (!textLayoutCached) return;
            if (result)
            {
                // The compact result rail keeps the portrait and full two-line
                // award. Its former 12px insets left only 52px for 140% rich text.
                messageLayout.offsetMin = new Vector2(playTextOffsetMin.x, 4);
                messageLayout.offsetMax = new Vector2(playTextOffsetMax.x, -4);
                message.margin = Vector4.zero;
                message.enableAutoSizing = true; message.fontSizeMin = 16; message.fontSizeMax = 24;
            }
            else
            {
                messageLayout.anchorMin = playTextAnchorMin; messageLayout.anchorMax = playTextAnchorMax;
                messageLayout.pivot = playTextPivot; messageLayout.offsetMin = playTextOffsetMin; messageLayout.offsetMax = playTextOffsetMax;
                message.margin = playTextMargin; message.enableAutoSizing = playTextAutoSize;
                message.fontSizeMin = playTextMin; message.fontSizeMax = playTextMax; message.fontSize = playTextSize;
            }
        }
        void Accept(SportsScoreEvent receipt)
        {
            var source = Source; if (source == null || receipt.MatchId != source.MatchId) return;
            if (match != receipt.MatchId) ResetMatch(receipt.MatchId);
            if (viewport >= 0 && (!RefreshAudience() || receipt.IsTeam || receipt.Slot != viewportOwner)) return;
            // A final measured outcome replaces stale round measurements in
            // presentation only. Every receipt remains in the match journal;
            // earned positive/negative point changes retain their commit order.
            if (receipt.Kind == SportsScoreKind.Outcome)
            {
                if (receipt.MatchEnded)
                {
                    pending.RemoveAll(item => item.latest.MatchId == receipt.MatchId && item.latest.Kind == SportsScoreKind.Outcome && !item.latest.MatchEnded);
                    terminalOutcomeMatch = receipt.MatchId;
                }
                else if (terminalOutcomeMatch == receipt.MatchId) return;
            }
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
            match = id; terminalOutcomeMatch = null; pending.Clear(); Presented = 0; LastPresented = null;
            notification.MinimizeNotification(); next = Time.time + .7f;
            if (viewport >= 0) { viewportOwner = -1; WaitForAudience(); }
        }
        void WaitForAudience()
        {
            // Keep Heat active through its public Out transition. Disabling its
            // GameObject early would interrupt the vendor's internal reset.
            pending.Clear(); Presented = 0; LastPresented = null;
            notification.MinimizeNotification();
            audienceReady = Time.time + HeatUIInternalTools.GetAnimatorClipLength(notificationAnimator, "Notification_Out") + .15f;
            next = Mathf.Max(next, audienceReady);
            visibility.alpha = 0;
        }
        bool RefreshAudience()
        {
            int owner = -1;
            var source = Source; var camera = source?.ScoreCamera(viewport);
            bool valid = camera != null && camera.enabled && source.TryReadViewportCard(viewport, out owner, out _, out _);
            if (!valid) owner = -1;
            if (owner != viewportOwner) { viewportOwner = owner; WaitForAudience(); }
            if (valid && layout != null)
            {
                var view = camera.rect;
                layout.anchorMin = layout.anchorMax = new Vector2(view.center.x, view.yMax);
            }
            return valid;
        }
        void LateUpdate()
        {
            var source = Source; if (source == null || notification == null) return;
            if (match != source.MatchId) ResetMatch(source.MatchId);
            bool audience = viewport < 0 || RefreshAudience();
            bool result = viewport < 0 && source.ScoreFinished && source.ScoreResultPage;
            bool showing = audience && (viewport < 0 || Time.time >= audienceReady) && source.ScoreRunning && !source.ScorePaused && (result || !source.ScoreFinished && source.ScorePlayPage);
            visibility.alpha = showing ? 1 : 0;
            if (viewport < 0) SetResultLayout(result);
            // A final physical deposit can close the match in the same step.
            // Its already-confirmed receipt still belongs on the results page.
            if (!source.ScoreRunning) { pending.Clear(); return; }
            if (!showing || Time.time < next || pending.Count == 0) return;
            var item = pending[0]; pending.RemoveAt(0); var receipt = item.latest;
            if (receipt.MatchId != source.MatchId) return;
            Color color = receipt.Color;
            accent.color = color; portrait.color = Color.Lerp(color, Color.white, .4f);
            string delta = (item.delta >= 0 ? "+" : "") + item.delta.ToString("0.##", CultureInfo.InvariantCulture);
            string measurement = receipt.Kind == SportsScoreKind.Outcome
                ? "<size=140%><b>" + receipt.OutcomeValue.ToString("0.000", CultureInfo.InvariantCulture) + "</b></size> " + Escape(receipt.Unit)
                : "<size=140%><b>" + delta + "</b></size> " + Escape(CountUnit(receipt.Unit, item.delta)) + "     " + receipt.Total.ToString("0.##", CultureInfo.InvariantCulture) + " TOTAL" + (item.count > 1 ? "  ·  " + item.count + " AWARDS" : "");
            notification.notificationText = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + "><b>" + Escape(receipt.DisplayName) + "</b></color>  ·  " + Escape(receipt.Reason)
                + "\n" + measurement;
            float readableHold = receipt.Tier == SportsScoreTier.Small ? .85f : receipt.Tier == SportsScoreTier.Major ? 1.35f : 2;
            // Heat starts its minimize timer with In. Keep the full authored
            // animation, then give the actual score its intended reading time.
            notification.minimizeAfter = HeatUIInternalTools.GetAnimatorClipLength(notificationAnimator, "Notification_In") + readableHold;
            notification.UpdateUI(); notification.ExpandNotification();
            portrait.transform.DOKill(); portrait.transform.localScale = Vector3.one;
            portrait.transform.DOPunchScale(Vector3.one * (receipt.Tier == SportsScoreTier.Small ? .12f : .24f), .45f, 3, .4f).SetUpdate(true).SetLink(portrait.gameObject);
            next = Time.time + notification.minimizeAfter + HeatUIInternalTools.GetAnimatorClipLength(notificationAnimator, "Notification_Out") + .15f;
            LastPresented = receipt; Presented++;
        }
        static string CountUnit(string unit, double amount) => System.Math.Abs(amount) == 1 ? unit == "CUPS" ? "CUP" : unit == "FARES" ? "FARE" : unit : unit;
        static string Escape(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");
    }
}
