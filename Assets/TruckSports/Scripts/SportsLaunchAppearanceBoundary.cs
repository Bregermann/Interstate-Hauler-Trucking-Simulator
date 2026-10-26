using System;
using System.Collections.Generic;
using LWS.TruckBowling;

namespace LWS.TruckSports
{
    // Explicit attempt-boundary glue for Sky Tow / Swing / Field Goal / Beer Pong.
    // No Update callback, physical fitting, input or replay timing authority.
    public static class SportsLaunchAppearanceBoundary
    {
        public static void PrepareTurn(SportsEventTruck truck, BowlingPlayer participant, int slot, SportsMotionReplay replay)
        {
            if (truck.appearanceHost == null) return;
            // A different owner can replace the model with identical names and
            // child counts. Drop cached Transform instances before composition;
            // the mode initializes its fresh record after equipment reset.
            if (replay.Playing || !replay.InvalidateFreshRecord()) throw new InvalidOperationException("Stop the original replay before preparing its next owner.");
            truck.appearanceHost.participantSlot = slot;
            truck.appearanceHost.Prepare(participant.participantId, participant.color);
            foreach (var framing in truck.session.appearanceFraming)
                if (framing != null && framing.session == truck.session) framing.RefreshAppearanceSubjects();
        }

        public static void RefreshRecord(SportsMotionReplay replay, params int[] candidates)
        {
            var indices = new List<int>();
            foreach (int index in candidates)
                if (replay.sources[index].GetComponentInParent<SportsTruckAppearanceHost>() != null) indices.Add(index);
            if (indices.Count == 0) return; // unauthored original scenes retain their original path.
            if (replay.Playing || !replay.ResetFreshAppearanceRecord()) throw new InvalidOperationException("Appearance refresh requires a stopped fresh record.");
            int[] actual = indices.ToArray();
            // Existing adapter first validates every hierarchy. Same-topology paint
            // refresh retains buffers; a changed body/accessory rebuilds only these roots.
            if (!replay.RefreshAppearanceSources(actual, false) && !replay.RefreshAppearanceSources(actual, true))
                throw new InvalidOperationException("The original launch replay appearance binding is invalid.");
        }
    }
}
