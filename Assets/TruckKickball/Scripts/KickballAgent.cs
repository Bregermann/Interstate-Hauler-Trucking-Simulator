using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckKickball
{
    public sealed class KickballAgent : MonoBehaviour
    {
        public KickballMatch match;
        public SportsEventTruck truck;
        public Transform catchingCup;
        public Renderer teamMarker, replayTeamMarker;
        public Material blueTeam, orangeTeam;
        public Vector3 PreviousPosition { get; private set; }
        public Vector3 IncomingVelocity { get; private set; }
        public float blocked, reversing;

        public void ApplyTeam(int team)
        {
            var material = team == 0 ? blueTeam : orangeTeam;
            if (teamMarker != null) teamMarker.sharedMaterial = material;
            if (replayTeamMarker != null) replayTeamMarker.sharedMaterial = material;
        }

        public void Observe()
        {
            PreviousPosition = truck.body.position;
            IncomingVelocity = truck.body.linearVelocity;
        }

        void OnCollisionEnter(Collision collision) { TagContact(collision); }
        void OnCollisionStay(Collision collision) { TagContact(collision); }
        void TagContact(Collision collision)
        {
            if (!match.Live || match.Carrier != truck.slot || collision.rigidbody == null) return;
            var other = collision.rigidbody.GetComponent<KickballAgent>();
            if (other != null && other.match == match) match.QueueTag(other.truck.slot);
        }
    }
}
