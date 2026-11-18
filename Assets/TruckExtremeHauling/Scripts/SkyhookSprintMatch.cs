using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckExtremeHauling
{
    public sealed class SkyhookSprintMatch : ExtremeHaulingMatch
    {
        public PhysicalCableCar[] cableCars;
        public SkyhookTransferRoute[] transferRoutes;
        [Min(150)] public float transferMatchSeconds=240;
        float ordinaryMatchSeconds;
        [Min(1)] public int slalomGatesPerLane = 4;
        public Transform[] slalomGates;
        public GameObject[] slalomMarkers;
        public Vector2 slalomHalfOpening = new Vector2(3.5f, 5);
        readonly int[] passed = new int[4], missed = new int[4], nextGate = new int[4];
        readonly float[] gateQuality = new float[4], quiet = new float[4], impactSeverity = new float[4], maximumImpact = new float[4], swingIntegral = new float[4], windMetres = new float[4], landingError = new float[4];
        readonly bool[] entering = new bool[4], blocked = new bool[4], crossed = new bool[4], delivered = new bool[4];
        readonly Vector3[] previous = new Vector3[4];
        Collider[][] loadShapes;
        string reportedMatch;
        protected override Vector3 CameraFocus(int slot) => cableCars[CameraParticipant(slot)].load.position;
        public override string Title => "SKYHOOK SPRINT";
        public override string Status => option==1&&transferRoutes!=null&&transferRoutes.Length>0?"HOOK · HOIST · TRANSFER THE SAME LOAD · RECAPTURE":option==0?base.Status:option==2?"CLEAR EVERY OPENING · RELEASE AFTER FINAL GATE":option==1?"BRAKE THE SWING · CENTER THE LANDING":"HAUL THROUGH GUSTS · PRESERVE CARGO";
        protected override string[] Modes => new[] { "SKYHOOK SPRINT", "PRECISION CABLE DELIVERY", "SUSPENDED SLALOM", "HIGH-WIND HAULING" };
        protected override string ActionHint => option==1&&transferRoutes!=null&&transferRoutes.Length>0?"HOOK / RELEASE · HOLD + DRIVE: WINCH":"RELEASE CABLE";
        public override string Help => option==1&&transferRoutes!=null&&transferRoutes.Length>0 ? "TWO-STATION PRECISION: drive forward/reverse along the active trolley rail; steer sways its physical hook. HOLD ACTION + forward/back to winch up/down. With drive released, TAP ACTION near the lifting eye to attach/release. Lift from the loading platform, carry to the middle platform, lower and release the original load there. Lower the second hook and recapture that SAME load, then haul, lower and release it at the final platform. Close hook distance and matched velocity are required. A supported dropped load can be recaptured; falls into the ravine are final. No transported load is teleported." : "Throttle accelerates the physical overhead trolley; brake controls momentum and steering shifts the real suspended load. ACTION releases its cable. Sprint rewards an upright settled delivery and speed. Precision judges the final landing offset, tilt, preserved cargo, actual impacts and accumulated swing; rushing adds no precision bonus. Suspended Slalom requires the entire trailer to clear all four alternating marked openings in order before a successful platform landing can score. High-Wind Hauling applies real extra gusts and rewards successful forward transport through them, with impact and landing penalties. Release timing, swing and momentum matter; an early release or missed slalom opening is a failed slalom attempt.";
        public int SlalomGatesPassed(int slot) => passed[slot];
        public int SlalomGatesMissed(int slot) => missed[slot];
        public float LandingErrorMetres(int slot) => landingError[slot];
        public float WindWeightedMetres(int slot) => windMetres[slot];
        public override bool TryReadScoreCard(int slot, out string value, out string detail)
        {
            if (option == 0) return base.TryReadScoreCard(slot, out value, out detail);
            if(option==1&&transferRoutes!=null&&slot>=0&&slot<setup.count&&slot<transferRoutes.Length&&transferRoutes[slot]!=null)
            {value=scores[slot].ToString("F0");detail=transferRoutes[slot].Instruction;return true;}
            if (slot < 0 || slot >= setup.count) { value = detail = null; return false; }
            value = scores[slot].ToString("F0");
            if (option == 2)
            {
                detail = passed[slot] + "/" + slalomGatesPerLane + " GATES · " + missed[slot] + " MISSED";
                var gate = NextGate(slot);
                if (!retired[slot] && gate != null) detail += gate.position.x < cableCars[slot].load.worldCenterOfMass.x ? " · SWING LEFT" : " · SWING RIGHT";
            }
            else if (delivered[slot]) detail = landingError[slot].ToString("F1") + "m LANDING ERROR · " + maximumImpact[slot].ToString("F1") + "m/s IMPACT";
            else detail = option == 1 ? "CONTROL SWING · BRAKE · LAND AT CENTER" : windMetres[slot].ToString("F0") + " WIND-WEIGHTED METRES";
            return true;
        }
        protected override void BeginMechanic()
        {
            if(ordinaryMatchSeconds<=0)ordinaryMatchSeconds=matchSeconds;
            matchSeconds=option==1&&transferRoutes!=null&&transferRoutes.Length>0?Mathf.Max(ordinaryMatchSeconds,transferMatchSeconds):ordinaryMatchSeconds;
            for(int i=0;i<setup.count;i++){trucks[i].Power(false);foreach(var body in trucks[i].GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;}
            foreach (var car in cableCars) { car.trolley.isKinematic = false; car.ResetCable(); }
            System.Array.Clear(passed, 0, 4); System.Array.Clear(missed, 0, 4); System.Array.Clear(nextGate, 0, 4); System.Array.Clear(gateQuality, 0, 4); System.Array.Clear(quiet, 0, 4); System.Array.Clear(impactSeverity, 0, 4); System.Array.Clear(maximumImpact, 0, 4); System.Array.Clear(swingIntegral, 0, 4); System.Array.Clear(windMetres, 0, 4); System.Array.Clear(landingError, 0, 4); System.Array.Clear(entering, 0, 4); System.Array.Clear(blocked, 0, 4); System.Array.Clear(crossed, 0, 4); System.Array.Clear(delivered, 0, 4);
            reportedMatch = null; loadShapes = new Collider[cableCars.Length][];
            if (slalomMarkers != null) for (int i = 0; i < slalomMarkers.Length; i++) if (slalomMarkers[i] != null) slalomMarkers[i].SetActive(option == 2 && i < setup.count);
            for (int i = 0; i < cableCars.Length; i++)
            {
                previous[i] = cableCars[i].load.worldCenterOfMass;
                loadShapes[i] = cableCars[i].load.GetComponentsInChildren<Collider>(true);
                if (option == 0) continue;
                var impact = cableCars[i].load.GetComponent<SkyhookLoadImpact>();
                if (impact == null) impact = cableCars[i].load.gameObject.AddComponent<SkyhookLoadImpact>();
                impact.match = this; impact.slot = i;
            }
            if(transferRoutes!=null)for(int i=0;i<transferRoutes.Length;i++)if(transferRoutes[i]!=null)
            {bool active=option==1&&i<setup.count;transferRoutes[i].SetMode(active);if(active)transferRoutes[i].BeginRoute();}
        }
        protected override void StopGame()
        {
            bool all = true; for (int i = 0; i < setup.count; i++) all &= retired[i];
            if (option != 0 && Running && !Finished && (all || age >= matchSeconds) && reportedMatch != MatchId)
            {
                reportedMatch = MatchId;
                for (int i = 0; i < setup.count; i++)
                {
                    if(option==1&&transferRoutes!=null&&i<transferRoutes.Length&&transferRoutes[i]!=null)
                    {var route=transferRoutes[i];Metric(i,"physical_hook_captures",route.firstHook.Captures+route.secondHook.Captures);Metric(i,"supported_cable_transfers",route.Transfers);Metric(i,"cable_load_recaptures",route.Recoveries);}
                    Metric(i, "cable_maximum_impact_mps", maximumImpact[i]);
                    Metric(i, "cable_swing_degree_seconds", swingIntegral[i]);
                    if (delivered[i]) Metric(i, "cable_landing_error_metres", landingError[i]);
                    if (option == 2) { Metric(i, "suspended_gates_passed", passed[i]); Metric(i, "suspended_gates_missed", missed[i]); }
                    if (option == 3) Metric(i, "wind_weighted_forward_metres", windMetres[i]);
                }
            }
            if(transferRoutes!=null)foreach(var route in transferRoutes)if(route!=null&&route.gameObject.activeSelf)route.Freeze();
            base.StopGame(); if (cableCars != null) foreach (var car in cableCars) car.trolley.isKinematic = true;
        }
        void Metric(int i, string name, double value) => SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, name, setup.players[i].participantId, "mode=" + OptionLabel, value);
        protected override Vector2 AICommand(int i, float dt)
        {
            if(option==1&&transferRoutes!=null&&i<transferRoutes.Length&&transferRoutes[i]!=null)return Vector2.zero;
            if (option == 0) return new Vector2(-Vector3.Dot(trailers[i].Body.linearVelocity, Vector3.right) * .1f, Vector3.Distance(cableCars[i].trolley.position, cableCars[i].routeEnd) > 16 ? 1 : -.6f);
            var car = cableCars[i]; var gate = option == 2 ? NextGate(i) : null;
            Vector3 desired = gate != null ? gate.position : objectives[i].position;
            float error = desired.x - car.load.worldCenterOfMass.x;
            float steer = Mathf.Clamp(error * .65f - car.load.linearVelocity.x * .9f, -1, 1);
            Vector3 direction = Vector3.ProjectOnPlane(car.routeEnd - car.trolley.position, Vector3.up).normalized;
            float speed = Vector3.Dot(car.trolley.linearVelocity, direction);
            float distance = Vector3.ProjectOnPlane(car.routeEnd - car.trolley.position, Vector3.up).magnitude;
            float requested;
            if (gate == null)
            {
                // Match actual forward velocity to the ballistic drop, rather
                // than parking short of the target and never finding a release.
                float gravity = Mathf.Max(.1f, Mathf.Abs(Physics.gravity.y));
                float height = Mathf.Max(0, car.load.worldCenterOfMass.y - objectives[i].position.y);
                float fall = (car.load.linearVelocity.y + Mathf.Sqrt(car.load.linearVelocity.y * car.load.linearVelocity.y + 2 * gravity * height)) / gravity;
                float remaining = Vector3.Dot(objectives[i].position - car.load.worldCenterOfMass, direction);
                requested = Mathf.Clamp((remaining - 1) / Mathf.Max(.5f, fall), 0, 12);
            }
            else requested = Mathf.Min(12, Mathf.Max(0, distance - 9) * .3f);
            if (gate != null && Mathf.Abs(error) > 1.5f) requested = Mathf.Min(requested, 3.5f);
            return new Vector2(steer, Mathf.Clamp((requested - speed) * .4f, -1, 1));
        }
        protected override void StepMechanic(int i, Vector2 drive, bool ai, float dt)
        {
            if(option==1&&transferRoutes!=null&&i<transferRoutes.Length&&transferRoutes[i]!=null)
            {
                var route=transferRoutes[i];route.Tick(drive,ActionHeld(i),actions[i],ai,(int)setup.players[i].difficulty,dt);
                if(route.Delivered)
                {delivered[i]=true;landingError[i]=route.LandingError;maximumImpact[i]=route.MaximumImpact;swingIntegral[i]=route.SwingSeconds;
                    Award(i,Mathf.Max(1,3500*trailers[i].CargoCondition-route.LandingError*150-route.MaximumImpact*12-route.SwingSeconds+route.Transfers*500),"physical_two_station_winch_delivery");retired[i]=true;}
                else if(route.Failed)retired[i]=true;
                return;
            }
            // Keep the already-passing default physics, AI release and award
            // path unchanged. Variant rules never alter Sprint's result.
            if (option == 0)
            {
                var car = cableCars[i]; car.Drive(Mathf.Max(0, drive.y), Mathf.Max(0, -drive.y));
                car.load.AddForce(Vector3.right * drive.x * 1.2f, ForceMode.Acceleration);
                if (option == 3) car.load.AddForce(Vector3.right * Mathf.Sin(age * .7f) * 2, ForceMode.Acceleration);
                float fallSeconds = Mathf.Sqrt(2 * Mathf.Max(0, car.load.position.y - objectives[i].position.y) / Mathf.Max(.1f, Mathf.Abs(Physics.gravity.y)));
                Vector3 predicted = car.load.worldCenterOfMass + car.load.linearVelocity * fallSeconds;
                float horizontalError = Vector3.ProjectOnPlane(predicted - objectives[i].position, Vector3.up).magnitude;
                if (actions[i] || ai && horizontalError < 8) car.Release();
                if (car.Released && AtRestIn(i, car.load, objectives[i], 12) && Vector3.Dot(car.load.transform.up, Vector3.up) > .7f)
                { Award(i, 1000 * trailers[i].CargoCondition + Mathf.Max(0, matchSeconds - age) * 5, "cable_cargo_landed"); retired[i] = true; }
                if(car.load.position.y < -20)retired[i]=true;
                return;
            }
            StepVariant(i, drive, ai, dt);
        }
        void StepVariant(int i, Vector2 drive, bool ai, float dt)
        {
            var car = cableCars[i];
            car.Drive(Mathf.Max(0, drive.y), Mathf.Max(0, -drive.y));
            car.load.AddForce(Vector3.right * drive.x * 1.2f, ForceMode.Acceleration);
            float gust = option == 3 ? Mathf.Sin(age * .7f) * 2 : 0;
            if (option == 3) car.load.AddForce(Vector3.right * gust, ForceMode.Acceleration);
            Vector3 current = car.load.worldCenterOfMass;
            if (!car.Released)
            {
                Vector3 eye = car.load.transform.TransformPoint(car.cable.anchor);
                float swing = Vector3.Angle(Vector3.down, eye - car.trolley.position);
                swingIntegral[i] += swing * dt;
                Vector3 direction = Vector3.ProjectOnPlane(car.routeEnd - car.trolley.position, Vector3.up).normalized;
                windMetres[i] += Mathf.Abs(gust) * Mathf.Max(0, Vector3.Dot(current - previous[i], direction));
                if (option == 2) ObserveSlalom(i, current);
            }
            previous[i] = current;
            float gravity = Mathf.Max(.1f, Mathf.Abs(Physics.gravity.y));
            float height = Mathf.Max(0, current.y - objectives[i].position.y);
            float fall = (car.load.linearVelocity.y + Mathf.Sqrt(car.load.linearVelocity.y * car.load.linearVelocity.y + 2 * gravity * height)) / gravity;
            Vector3 predicted = current + car.load.linearVelocity * fall;
            float error = Vector3.ProjectOnPlane(predicted - objectives[i].position, Vector3.up).magnitude;
            bool ready = option != 2 || nextGate[i] >= slalomGatesPerLane;
            if (actions[i] || ai && ready && error < (option == 1 ? 3.5f : 8)) car.Release();
            bool landed = car.Released && AtRestIn(i, car.load, objectives[i], 12) && Vector3.Dot(car.load.transform.up, Vector3.up) > .7f;
            quiet[i] = landed ? quiet[i] + dt : 0;
            if (quiet[i] >= 1.5f)
            {
                landingError[i] = Vector3.ProjectOnPlane(current - objectives[i].position, Vector3.up).magnitude;
                delivered[i] = true;
                if (option == 1)
                {
                    float tilt = Vector3.Angle(car.load.transform.up, Vector3.up);
                    Award(i, Mathf.Max(1, 3500 * trailers[i].CargoCondition - landingError[i] * 150 - tilt * 15 - impactSeverity[i] * 12 - swingIntegral[i]), "precision_cable_delivery");
                }
                else if (option == 2)
                {
                    if (passed[i] == slalomGatesPerLane && missed[i] == 0)
                        Award(i, passed[i] * 600 + gateQuality[i] * 200 + trailers[i].CargoCondition * 500 + Mathf.Max(0, matchSeconds - age) * 2, "complete_physical_suspended_slalom");
                    else SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "suspended_slalom_incomplete", setup.players[i].participantId, "Delivered but did not physically clear every opening", passed[i]);
                }
                else Award(i, Mathf.Max(1, 1000 * trailers[i].CargoCondition + Mathf.Max(0, matchSeconds - age) * 5 + windMetres[i] * 3 * trailers[i].CargoCondition - impactSeverity[i] * 15 - landingError[i] * 15), "high_wind_cargo_delivered");
                retired[i] = true;
            }
            if (car.load.position.y < -20) retired[i] = true;
        }
        Transform NextGate(int slot)
        {
            int index = slot * slalomGatesPerLane + nextGate[slot];
            return slalomGates != null && nextGate[slot] < slalomGatesPerLane && index >= 0 && index < slalomGates.Length ? slalomGates[index] : null;
        }
        Bounds LoadBounds(int slot)
        {
            Bounds result = new Bounds(cableCars[slot].load.worldCenterOfMass, Vector3.zero); bool first = true;
            foreach (var shape in loadShapes[slot]) if (shape != null && shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy)
            { if (first) { result = shape.bounds; first = false; } else result.Encapsulate(shape.bounds); }
            return result;
        }
        static float Extent(Vector3 axis, Vector3 extents) => Mathf.Abs(axis.x) * extents.x + Mathf.Abs(axis.y) * extents.y + Mathf.Abs(axis.z) * extents.z;
        void ObserveSlalom(int slot, Vector3 current)
        {
            var gate = NextGate(slot); if (gate == null) return;
            Bounds load = LoadBounds(slot);
            Vector3 center = load.center, previousCenter = previous[slot] + (center - current);
            Vector3 normal = gate.forward;
            float oldSide = Vector3.Dot(previousCenter - gate.position, normal), side = Vector3.Dot(center - gate.position, normal);
            float radius = Extent(normal, load.extents);
            if (side + radius >= 0 && oldSide - radius <= 0 && side >= oldSide)
            {
                entering[slot] = true;
                float lateral = Mathf.Abs(Vector3.Dot(center - gate.position, gate.right)) + Extent(gate.right, load.extents);
                float vertical = Mathf.Abs(Vector3.Dot(center - gate.position, gate.up)) + Extent(gate.up, load.extents);
                blocked[slot] |= lateral > slalomHalfOpening.x || vertical > slalomHalfOpening.y;
                if (oldSide < 0 && side >= 0) crossed[slot] = true;
            }
            if (side - radius <= 0) return;
            bool valid = entering[slot] && crossed[slot] && !blocked[slot];
            if (valid)
            {
                passed[slot]++;
                float offset = Mathf.Abs(Vector3.Dot(center - gate.position, gate.right));
                gateQuality[slot] += Mathf.Clamp01(1 - offset / Mathf.Max(.1f, slalomHalfOpening.x));
            }
            else missed[slot]++;
            nextGate[slot]++; entering[slot] = blocked[slot] = crossed[slot] = false;
        }
        public void RegisterImpact(int slot, Collision collision)
        {
            if (option == 0 || !Live || slot < 0 || slot >= setup.count || retired[slot]) return;
            if(option==1&&transferRoutes!=null&&slot<transferRoutes.Length&&transferRoutes[slot]!=null)transferRoutes[slot].RegisterImpact(collision);
            float speed = collision.relativeVelocity.magnitude;
            maximumImpact[slot] = Mathf.Max(maximumImpact[slot], speed);
            impactSeverity[slot] += Mathf.Max(0, speed - 1.5f);
        }
    }
    public sealed class SkyhookLoadImpact : MonoBehaviour
    {
        public SkyhookSprintMatch match;
        public int slot;
        void OnCollisionEnter(Collision collision) { if (match != null) match.RegisterImpact(slot, collision); }
    }
}
