using System;
using TMPro;
using DG.Tweening;
using UnityEngine;

namespace LWS.TruckSports
{
    // Saved Heat-derived plates and TMP references; runtime only reads, lays out and animates them.
    [DefaultExecutionOrder(500)] public sealed class SportsCarnivalLeaderboard : MonoBehaviour
    {
        [Serializable] public sealed class Card
        {
            public RectTransform root;
            public TMP_Text name, place, value, detail;
            public UnityEngine.UI.Image portrait, stripe, progress;
            [NonSerialized] public string id;
            [NonSerialized] public int rank = -1;
        }
        public SportsCarnivalHudSource source;
        public SportsCarnivalHudPolicy policy;
        public RectTransform root, table, contextRoot;
        public TMP_Text context;
        public Card[] cards;
        public float reorderSeconds = .32f;
        readonly SportsCarnivalHudRow[] rows = new SportsCarnivalHudRow[12];
        float next;
        bool wasShowing;
        string matchId;
        public bool Showing { get; private set; }
        public int VisibleCards { get; private set; }
        public SportsCarnivalHudRow Row(int rank) => rows[rank];

        void LateUpdate()
        {
            Showing = source != null && source.Showing;
            if (root == null) return;
            root.gameObject.SetActive(Showing);
            policy?.Present(Showing);
            if (!Showing)
            {
                VisibleCards = 0;
                if (wasShowing) StopTweens();
                wasShowing = false; return;
            }
            if (Time.unscaledTime < next && wasShowing && matchId == source.match.MatchId) return;
            next = Time.unscaledTime + .1f;
            bool snap = !wasShowing || matchId != source.match.MatchId;
            wasShowing = true; matchId = source.match.MatchId;
            VisibleCards = source.Read(rows);
            if (cards == null || cards.Length < VisibleCards) throw new InvalidOperationException("Author the complete competitor field before play.");
            bool vertical = source.Vertical;
            float width = vertical ? 420 : Mathf.Min(1860, VisibleCards * 280f);
            table.anchorMin = table.anchorMax = table.pivot = vertical ? Vector2.right : new Vector2(.5f, 0);
            table.anchoredPosition = vertical ? new Vector2(-18, 18) : new Vector2(0, 14);
            table.sizeDelta = new Vector2(width, vertical ? VisibleCards * 74 - 6 : 104);
            context.text = source.Context; context.color = source.ContextColor;
            contextRoot.gameObject.SetActive(context.text.Length > 0);
            for (int rank = 0; rank < VisibleCards; rank++)
            {
                var row = rows[rank];
                // Each saved card belongs to a scored competitor, not a changing rank or name.
                int index = row.participant;
                if (source.match is LWS.TruckWifeCarrying.WifeMatch wife) index = row.participant % 4;
                var card = cards[index]; card.root.gameObject.SetActive(true);
                float cardWidth = vertical ? width : (width + 6) / VisibleCards - 6;
                card.root.anchorMin = card.root.anchorMax = card.root.pivot = Vector2.up;
                card.root.sizeDelta = new Vector2(cardWidth, vertical ? 68 : 104);
                Vector2 target = vertical ? new Vector2(0, -rank * 74) : new Vector2(rank * (cardWidth + 6), 0);
                if (snap || card.id != row.id) { card.root.DOKill(); card.root.anchoredPosition = target; }
                else if (card.rank != rank) { card.root.DOKill(); card.root.DOAnchorPos(target, Mathf.Clamp(reorderSeconds, .1f, .6f)).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(card.root.gameObject); }
                card.id = row.id; card.rank = rank;
                card.name.text = row.name; card.name.color = card.stripe.color = row.color;
                card.place.text = row.place > 0 ? "#" + row.place : "OUT";
                card.value.text = row.value; card.detail.text = row.detail;
                card.portrait.color = Color.Lerp(row.color, Color.white, .35f);
                card.progress.gameObject.SetActive(row.progress >= 0);
                card.progress.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, cardWidth - 16) * Mathf.Clamp01(row.progress), 3);
                card.progress.color = row.color;
                Layout(card, cardWidth, vertical);
            }
            for (int slot = VisibleCards; slot < cards.Length; slot++) cards[slot].root.gameObject.SetActive(false);
        }
        static void Rect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        static void Layout(Card card, float width, bool vertical)
        {
            Rect(card.stripe.rectTransform, 0, 6, 4, vertical ? 56 : 92);
            Rect(card.portrait.rectTransform, vertical ? 38 : 8, 8, vertical ? 42 : 32, vertical ? 42 : 32);
            Rect(card.name.rectTransform, vertical ? 88 : 46, 5, width - (vertical ? 204 : 54), 29);
            Rect(card.place.rectTransform, 8, vertical ? 15 : 42, vertical ? 28 : 39, 30);
            Rect(card.value.rectTransform, vertical ? width - 108 : 50, vertical ? 6 : 42, vertical ? 100 : width - 58, 31);
            Rect(card.detail.rectTransform, vertical ? 88 : 8, vertical ? 39 : 77, width - (vertical ? 96 : 16), vertical ? 24 : 22);
            card.progress.rectTransform.anchorMin = card.progress.rectTransform.anchorMax = card.progress.rectTransform.pivot = Vector2.up;
            card.progress.rectTransform.anchoredPosition = new Vector2(8, vertical ? -65 : -100);
        }
        void StopTweens() { if (cards != null) foreach (var card in cards) if (card?.root != null) card.root.DOKill(); }
        void OnDisable() { Showing = false; VisibleCards = 0; wasShowing = false; StopTweens(); policy?.Restore(); if (root != null) root.gameObject.SetActive(false); }
        void OnDestroy() { StopTweens(); policy?.Restore(); }
    }
}
