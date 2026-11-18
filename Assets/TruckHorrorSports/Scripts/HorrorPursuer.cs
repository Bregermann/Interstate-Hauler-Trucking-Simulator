using UnityEngine;
using UnityEngine.AI;
using LWS.TruckSports;
namespace LWS.TruckHorrorSports
{
    public enum HorrorPursuitState { Watching, Stalking, Flanking, Pursuing, Repelled }
    /// <summary>One physical antagonist; navigation supplies steering targets, never final player outcomes.</summary>
    public sealed class HorrorPursuer : MonoBehaviour
    {
        public Rigidbody body;
        public SportsEventTruck semi;
        public bool scarecrow;
        public float speed = 7, damage = 22;
        public Light[] signature;
        public Animator animator;
        public HorrorPursuitState State { get; private set; }
        HorrorMatch match;
        ScarecrowHeadlightLure deception;
        DeadEndPursuitAudio pursuitAudio;
        Vector3 initial;
        Quaternion rotation;
        NavMeshPath route;
        Vector3[] corners = System.Array.Empty<Vector3>();
        float nextPlan, repelledUntil;
        int target = -1;
        int roadIndex;
        float nextLunge;
        bool initialized;
        public int Target => target;
        public void ResetActor(HorrorMatch owner)
        {
            if (!initialized) { initial = transform.position; rotation = transform.rotation; initialized = true; }
            match = owner; route = new NavMeshPath(); nextPlan = repelledUntil = nextLunge = 0; target = -1; roadIndex = 0; State = HorrorPursuitState.Watching;
            body.isKinematic = false; body.position = initial; body.rotation = rotation; body.linearVelocity = body.angularVelocity = Vector3.zero;
            if (semi != null) { semi.Prepare(initial, rotation); semi.maxSpeed = owner.sport == HorrorSport.DeadEnd ? speed + 4 : 15; }
            SetActive(true);
            deception = GetComponent<ScarecrowHeadlightLure>();
            deception?.ResetDeception(owner);
            pursuitAudio = GetComponent<DeadEndPursuitAudio>();
            pursuitAudio?.ResetWarnings();
        }
        public void SetActive(bool enabled)
        { if (!enabled) { deception?.Freeze(); pursuitAudio?.Freeze(); } if (semi != null) semi.Power(enabled); if (signature != null) foreach (var light in signature) if (light != null) light.enabled = enabled; }
        public string PursuitCueFor(int slot) => pursuitAudio?.CaptionFor(slot);
        public void SilencePursuitCues() => pursuitAudio?.Silence();
        public void ReportPursuitCues() => pursuitAudio?.ReportFinal();
        public void Repel(float seconds) { if (match != null) repelledUntil = match.Age + seconds; }
        public void TickActor(float dt)
        {
            if (match == null || !match.Live) return;
            deception?.TickDeception(dt);
            pursuitAudio?.TickWarnings(dt);
            // The escape story opens quietly. Competitive pursuit begins
            // from the original physical start before either semi disappears
            // hundreds of metres down the road.
            float wakeDelay = match.sport == HorrorSport.DeadEnd && match.SurvivalMode ? 2.5f : 15f;
            if (match.Age < wakeDelay || match.Age < repelledUntil) { State = match.Age < repelledUntil ? HorrorPursuitState.Repelled : HorrorPursuitState.Watching; if (semi != null) semi.TickDrive(Vector2.zero); return; }
            if (match.Age >= nextPlan)
            {
                nextPlan = match.Age + .4f; target = -1; float best = float.MaxValue;
                for (int i = 0; i < match.setup.count; i++)
                {
                    if (match.rules.outcome[i] != HorrorOutcome.Active) continue;
                    float distance = Vector3.Distance(body.position, match.PlayerPosition(i));
                    // Noise and recent exposure alter target priority; all players remain eligible.
                    float attention = distance - match.trucks[i].body.linearVelocity.magnitude * .6f + (deception?.AttentionAdjustment(i) ?? 0);
                    if (attention < best) { best = attention; target = i; }
                }
                if (target >= 0 && NavMesh.SamplePosition(body.position, out var start, 10, NavMesh.AllAreas) && NavMesh.SamplePosition(deception != null ? deception.DestinationFor(target, match.PlayerPosition(target)) : match.PlayerPosition(target), out var end, 10, NavMesh.AllAreas)) { NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, route); corners = route.corners; }
            }
            if (target < 0) return;
            Vector3 destination = deception != null ? deception.DestinationFor(target, match.PlayerPosition(target)) : match.PlayerPosition(target);
            float targetDistance = Vector3.Distance(body.position, destination);
            bool lit = scarecrow && match.IsIlluminated(body.position);
            State = lit ? HorrorPursuitState.Watching : targetDistance > 45 ? HorrorPursuitState.Flanking : targetDistance > 18 ? HorrorPursuitState.Stalking : HorrorPursuitState.Pursuing;
            if (State == HorrorPursuitState.Flanking && scarecrow && (deception == null || !deception.Luring)) destination += Vector3.Cross((destination - body.position).normalized, Vector3.up) * 12;
            if (route != null && route.status == NavMeshPathStatus.PathComplete)
            { for (int c = 1; c < corners.Length; c++) if (Vector3.Distance(body.position, corners[c]) > 2.5f) { destination = corners[c]; break; } }
            if (match.sport == HorrorSport.DeadEnd && match.roadRoute != null && match.roadRoute.Length > 0)
            {
                int nearest = 0; float distance = float.MaxValue;
                if (roadIndex == 0) for (int i = 0; i < match.roadRoute.Length; i++) { float d = (match.roadRoute[i].position - body.position).sqrMagnitude; if (d < distance) { nearest = i; distance = d; } }
                if (roadIndex == 0) roadIndex = nearest + 1;
                destination = match.roadRoute[roadIndex % match.roadRoute.Length].position;
                if (Vector3.Distance(body.position, destination) < 8) roadIndex++;
                Vector3 intercept = match.PlayerPosition(target) + match.trucks[target].body.linearVelocity * .6f;
                Vector3 towardsTarget = Vector3.ProjectOnPlane(intercept - body.position, Vector3.up);
                if (targetDistance < 48 && match.DistanceFromHighway(intercept) < 22 && Vector3.Dot(transform.forward, towardsTarget.normalized) > -.2f)
                    destination = intercept;
            }
            Vector3 delta = Vector3.ProjectOnPlane(destination - body.position, Vector3.up);
            if (semi != null)
            {
                var local = transform.InverseTransformDirection(delta);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                semi.TickDrive(new Vector2(Mathf.Clamp(angle / 28, -1, 1), Mathf.Abs(angle) > 120 ? -.5f : body.linearVelocity.magnitude < speed + match.Escalation ? 1 : .1f));
            }
            else
            {
                Vector3 desired = delta.normalized * (lit ? speed * .12f : speed * (1 + match.Escalation * .14f));
                Vector3 current = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
                body.AddForce(Vector3.ClampMagnitude(desired - current, 9), ForceMode.Acceleration);
                if (delta.sqrMagnitude > 1) body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(delta), dt * 3));
                if (!lit && targetDistance < 12 && targetDistance > 4 && match.Age >= nextLunge)
                { nextLunge = match.Age + 12; match.Say("STRAW SCRAPES THE ROAD BEHIND YOU", 2); body.AddForce(delta.normalized * 4, ForceMode.VelocityChange); }
            }
            if (animator != null) animator.SetFloat("Speed", Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude);
        }
        void OnCollisionEnter(Collision collision)
        {
            if (match == null || !match.Live) return;
            var truck = collision.collider.GetComponentInParent<SportsEventTruck>();
            if (truck == null || truck == semi || truck.session != match || collision.relativeVelocity.magnitude < .8f) return;
            match.Hit(truck.slot, damage + Mathf.Min(20, collision.relativeVelocity.magnitude)); Repel(2);
        }
    }
}
