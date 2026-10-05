using System;

namespace LWS.TruckTaxi
{
    // Ownership bookkeeping only; Bootstrap remains the pause authority and UIInput the input authority.
    public sealed class TruckTaxiModalPauseGuard
    {
        private object owner;
        private string context;
        private long nextToken, token;
        private bool mayResume;
        private int dispatchedFrame = int.MinValue, releasedFrame = int.MinValue;
        public bool IsHeld => owner != null;
        public bool IsOwnedBy(object value) => value != null && ReferenceEquals(owner, value);

        public long TryAcquire(object value, string currentContext, bool paused, bool blocked, int frame)
        {
            if (value == null || IsHeld || paused || blocked) return 0;
            owner = value; context = currentContext; token = ++nextToken; mayResume = true;
            dispatchedFrame = frame;
            return token;
        }
        public void Observe(string currentContext, bool paused, bool blocked)
        {
            if (IsHeld && (!paused || blocked || !string.Equals(context, currentContext, StringComparison.Ordinal))) mayResume = false;
        }
        public void InvalidateResume() => mayResume = false;
        public bool Release(object value, long leaseToken, string currentContext, bool paused, bool blocked, int frame)
        {
            if (!IsOwnedBy(value) || token != leaseToken) return false;
            Observe(currentContext, paused, blocked);
            bool resume = mayResume && paused;
            owner = null; context = null; token = 0; mayResume = false; releasedFrame = frame;
            return resume;
        }
        // 0: pass to normal HUD; 1: dispatch once; -1: consume an already-handled/open/close frame.
        public int Dispatch(int frame)
        {
            if (!IsHeld) return releasedFrame == frame ? -1 : 0;
            if (dispatchedFrame == frame) return -1;
            dispatchedFrame = frame;
            return 1;
        }
    }
}
