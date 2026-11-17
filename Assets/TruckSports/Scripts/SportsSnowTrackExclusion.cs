using UnityEngine;
namespace LWS.TruckSports
{
    // Authored loads that never drive on their wheels (upright dominoes and
    // Plinko projectiles) keep native vehicle components but emit no tyre tracks.
    [DisallowMultipleComponent]
    public sealed class SportsSnowTrackExclusion : MonoBehaviour { }
}
