using Assets.CoreCollapse.Scripts.Structural;

namespace LWS.TruckHoardHeist
{
    // Only initializes the vendor's supported isolated-prop configuration.
    public sealed class HeistPotterySupport : Support
    {
        void Awake() { ClearSupports(); if (onCollapse == null) onCollapse = new OnCollapseEvent(); }
    }
}
