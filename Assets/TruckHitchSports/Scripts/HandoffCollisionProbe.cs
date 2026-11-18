using UnityEngine;

namespace LWS.TruckHitchSports
{
    // Relays PhysX contact data to the existing match rule authority. This
    // component owns no score, physics response or persistent state.
    [DisallowMultipleComponent]
    public sealed class HandoffCollisionProbe : MonoBehaviour
    {
        HandoffMatch match;
        Rigidbody body;
        int team;

        public void Configure(HandoffMatch owner, int teamIndex)
        {
            match = owner;
            team = teamIndex;
            body = GetComponent<Rigidbody>();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (match != null && body != null)
                match.ReportPhysicalCollision(team, body, collision);
        }
    }
}
