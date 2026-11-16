using UnityEngine;

namespace LWS.TruckDragonRiders
{
    public sealed partial class DragonRidersMatch
    {
        // Reuse the existing compact uGUI card, inset and DOTween feedback.
        // Each card belongs at its own viewport corner, away from the shared
        // screen centre where inner-edge cards cover the cab windscreen.
        public override Vector2 ViewportCardAnchor(int view,Rect viewport)=>Vector2.zero;
    }
}
