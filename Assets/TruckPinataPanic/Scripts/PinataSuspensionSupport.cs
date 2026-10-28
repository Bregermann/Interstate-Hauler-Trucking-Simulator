using Assets.CoreCollapse.Scripts.Structural;

namespace LWS.TruckPinataPanic
{
    // CoreCollapse's structural event remains the collapse boundary. A hanging
    // paper prop has no supported buildings; initialize that documented API.
    public sealed class PinataSuspensionSupport : Support
    {
        void Awake() { ClearSupports(); if (onCollapse == null) onCollapse = new OnCollapseEvent(); }
    }
}
