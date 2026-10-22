using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckPoker
{
    [Serializable] public sealed class PokerPublicSeat
    {
        public RectTransform root;
        public TMP_Text name, stack, action, button;
        public UnityEngine.UI.Image accent, portrait, acting;
    }
    [Serializable] public sealed class PokerTableHudView
    {
        public RectTransform design, seats;
        public TMP_Text owner, stack, pot, phase;
        public PokerPublicSeat[] publicSeats;
    }
    // Public, read-only presentation. Private cards remain exclusively owned by
    // PokerPresentation's original hold-to-peek / handoff / focus lifecycle.
    public sealed class PokerTableHud : MonoBehaviour
    {
        public PokerMatch match;
        public int authoredVersion;
        public PokerTableHudView[] views;
        public RectTransform broadcastRoot, broadcastDesign, header;
        public Renderer venueLogo;
        public PokerPublicSeat[] broadcastSeats;
        public TMP_Text hand, broadcastPot, broadcastPhase, broadcastActor, broadcastStack;
        public GameObject[] legacyDocks;
        float next;
        PokerTable awardTable; int awardVersion = -1; string awardCaption;
        void LateUpdate()
        {
            if (match == null || match.presentation == null) return;
            foreach (var dock in legacyDocks) if (dock != null && dock.activeSelf) dock.SetActive(false);
            bool play = match.menus.Screen == SportsEventScreen.Play && match.Table != null;
            header.gameObject.SetActive(play && match.HumanViews == 0);
            broadcastRoot.gameObject.SetActive(play && match.HumanViews == 0);
            if (!play) return;
            for (int v = 0; v < views.Length && v < match.HumanViews; v++)
            {
                float width = match.presentation.panes[v].root.rect.width;
                float scale = Mathf.Min(1, Mathf.Max(100, width - 24) / 1180f);
                views[v].design.localScale = Vector3.one * scale;
                views[v].seats.localScale = Vector3.one * scale;
                views[v].design.anchoredPosition = new Vector2(0, match.presentation.panes[v].actions.activeSelf ? 10 : 10 - 140 * scale);
            }
            broadcastDesign.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(100, ((RectTransform)match.menus.hud.transform).rect.width - 32) / 1180f);
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .1f;
            Refresh();
        }
        public void Refresh()
        {
            if (match.Table == null) return;
            var t = match.Table;
            string phase = match.Dealing ? "DEALING" : t.HandDone ? "HAND COMPLETE" : t.Street.ToString().ToUpperInvariant();
            hand.text = "TRUCK POKER   /   HAND " + t.HandNumber + "   /   BLINDS " + t.SmallBlind + " / " + t.BigBlind;
            broadcastPot.text = PotCaption(); broadcastPhase.text = phase;
            int actor = match.Dealing && match.dealerDrive.CurrentSeat >= 0 ? match.dealerDrive.CurrentSeat : t.Actor;
            broadcastActor.text = actor >= 0 ? match.setup.players[actor].name : AwardWinners();
            broadcastActor.color = actor >= 0 ? match.setup.players[actor].color : SportsBroadcastCanvas.Cream;
            broadcastStack.text = actor >= 0 ? t.Stack(actor).ToString("N0") + " CHIPS" : PublicAwardCaption();
            Seats(broadcastSeats);
            for (int v = 0; v < views.Length; v++)
            {
                if (v >= match.HumanViews) continue;
                int owner = match.ViewOwners[v]; var pane = views[v];
                pane.owner.text = match.setup.players[owner].name; pane.owner.color = match.setup.players[owner].color;
                pane.stack.text = t.Stack(owner).ToString("N0") + " CHIPS";
                pane.pot.text = PotCaption(); pane.phase.text = "HAND " + t.HandNumber + "  /  " + (t.HandDone ? AwardWinners() + " / " + PublicAwardCaption() : phase) + "  /  " + t.SmallBlind + " / " + t.BigBlind;
                Seats(pane.publicSeats);
            }
        }
        string AwardWinners()
        {
            if (match.Table == null || !match.Table.HandDone) return "POTS AWARDED";
            var winners = new List<int>(); foreach (var pot in match.Table.Awards) foreach (int seat in pot.Winners) if (!winners.Contains(seat)) winners.Add(seat);
            if (winners.Count == 1) return match.setup.players[winners[0]].name + " WINS";
            return winners.Count > 1 ? "SPLIT / SIDE POTS AWARDED" : "POTS AWARDED";
        }
        static readonly string[] RankNames = { "HIGH CARD", "ONE PAIR", "TWO PAIR", "THREE OF A KIND", "STRAIGHT", "FLUSH", "FULL HOUSE", "FOUR OF A KIND", "STRAIGHT FLUSH" };
        public string PublicAwardCaption()
        {
            var table = match.Table; if (table == null || !table.HandDone) return "";
            if (awardTable == table && awardVersion == match.Version) return awardCaption;
            if (!table.Showdown) return CacheAward("LAST LIVE HAND");
            var publicBoard = table.PublicBoard; if (publicBoard.Length != 5) return "SHOWDOWN";
            var categories = new List<string>();
            foreach (var pot in table.Awards) foreach (int seat in pot.Winners)
            {
                // Read only card values already legitimately revealed by the
                // original world renderer. Folded/mucked/private hands are
                // never queried, even when this is a split-screen spectator.
                if (!table.PubliclyRevealed(seat)) continue;
                var a = match.presentation.seatCards[seat * 2]; var b = match.presentation.seatCards[seat * 2 + 1]; if (a.Card < 0 || b.Card < 0) continue;
                int[] seven = { a.Card, b.Card, publicBoard[0], publicBoard[1], publicBoard[2], publicBoard[3], publicBoard[4] };
                string category = RankNames[(int)(PokerHand.Evaluate(seven) >> 20)]; if (!categories.Contains(category)) categories.Add(category);
            }
            return categories.Count > 0 ? CacheAward(string.Join(" / ", categories)) : "SHOWDOWN";
        }
        string CacheAward(string value) { awardTable = match.Table; awardVersion = match.Version; return awardCaption = value; }
        string PotCaption()
        {
            var t = match.Table;
            if (!t.HandDone) return "POT  " + t.Pot.ToString("N0");
            int awarded = 0; foreach (var pot in t.Awards) awarded += pot.Amount;
            return "AWARDED  " + awarded.ToString("N0");
        }
        void Seats(PokerPublicSeat[] seats)
        {
            var t = match.Table;
            for (int i = 0; i < seats.Length; i++)
            {
                var seat = seats[i]; bool active = i < match.setup.count; seat.root.gameObject.SetActive(active); if (!active) continue;
                var player = match.setup.players[i]; bool acting = !match.Dealing && !t.HandDone && t.Actor == i;
                seat.name.text = player.name; seat.name.color = seat.accent.color = player.color;
                seat.portrait.color = Color.Lerp(player.color, Color.white, .35f);
                seat.stack.text = t.Stack(i).ToString("N0");
                seat.action.text = !t.IsInHand(i) ? "OUT" : t.IsFolded(i) ? "FOLDED" : acting ? "ACTING" : match.PublicAction[i] ?? "WAITING";
                seat.button.text = i == t.Button ? "D" : i == t.SmallBlindSeat ? "SB" : i == t.BigBlindSeat ? "BB" : "";
                seat.button.gameObject.SetActive(seat.button.text.Length > 0); seat.acting.gameObject.SetActive(acting);
            }
        }
    }
}
