using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckHotPotato
{
    // Bounds policy only. HotPotatoMatch.Present remains the single camera coordinator;
    // Cinemachine and SportsCameraFit retain their existing evaluation/projection work.
    public sealed class HotPotatoHumanArenaCamera
    {
        public bool Acquired { get; private set; }
        public Bounds RequiredBounds { get; private set; }
        public Bounds FramedBounds { get; private set; }
        public int NearbyAI { get; private set; }
        Transform cachedBomb;
        Renderer[] bombRenderers;
        public void Reset() { Acquired = false; NearbyAI = 0; }

        public void Present(HotPotatoMatch match, float dt)
        {
            if (match.Paused && Acquired) return;
            bool any = false; Bounds required = default;
            for (int i = 0; i < match.setup.count; i++)
                if (match.rules.alive[i] && match.setup.players[i].source != BowlingSource.AI)
                    Include(ref required, ref any, TruckBounds(match, i));
            int carrier = match.rules.Carrier;
            if (carrier >= 0 && match.rules.alive[carrier]) Include(ref required, ref any, TruckBounds(match, carrier));
            if (cachedBomb != match.bomb)
            { cachedBomb = match.bomb; bombRenderers = cachedBomb != null ? cachedBomb.GetComponentsInChildren<Renderer>() : null; }
            if (match.bomb != null && match.bomb.gameObject.activeInHierarchy)
                foreach (var renderer in bombRenderers)
                    if (renderer.enabled && !(renderer is ParticleSystemRenderer)) Include(ref required, ref any, renderer.bounds);
            if (!any) return;
            RequiredBounds = required;
            Bounds target = required; NearbyAI = 0;
            // Priority three cannot substantially enlarge the mandatory human/bomb shot.
            // Test proximity to required subjects, never chain distant AI into the group.
            for (int i = 0; i < match.setup.count; i++)
            {
                if (!match.rules.alive[i] || i == carrier || match.setup.players[i].source != BowlingSource.AI) continue;
                Bounds candidate = TruckBounds(match, i);
                if (Vector3.Distance(required.ClosestPoint(candidate.center), candidate.center) > 16) continue;
                Bounds expanded = target; expanded.Encapsulate(candidate);
                Vector3 budget = required.size * 1.15f + Vector3.one * 4;
                if (expanded.size.x > budget.x || expanded.size.y > budget.y || expanded.size.z > budget.z) continue;
                target = expanded; NearbyAI++;
            }
            target.Expand(3); // physical model, hats and bomb retain a small viewport margin.
            bool snap = !Acquired;
            if (snap) FramedBounds = target;
            else
            {
                float blend = 1 - Mathf.Exp(-3 * Mathf.Max(0, dt));
                var smooth = new Bounds(Vector3.Lerp(FramedBounds.center, target.center, blend),
                    Vector3.Lerp(FramedBounds.size, target.size, blend));
                // Expansion is immediate for safety; only obsolete space shrinks smoothly.
                smooth.Encapsulate(target); FramedBounds = smooth;
            }
            Acquired = true;
            var camera = match.broadcast;
            camera.view.orthographic = true;
            camera.Pose(FramedBounds.center, new Vector3(0, 90, -135), 60, snap);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0, -90, 135), Vector3.up);
            match.framing.arenaBounds = FramedBounds;
            match.framing.Apply();
        }

        public static Bounds TruckBounds(HotPotatoMatch match, int slot)
        {
            if (match.broadcast.broadcastFraming != null) return match.broadcast.broadcastFraming.TruckBounds(slot);
            var truck = match.trucks[slot]; bool any = false;
            Bounds bounds = new Bounds(truck.body.worldCenterOfMass, Vector3.one * 2);
            foreach (var renderer in truck.GetComponentsInChildren<Renderer>())
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && !(renderer is ParticleSystemRenderer))
                    Include(ref bounds, ref any, renderer.bounds);
            return bounds;
        }
        static void Include(ref Bounds bounds, ref bool any, Bounds addition)
        { if (!any) { bounds = addition; any = true; } else bounds.Encapsulate(addition); }
    }
}
