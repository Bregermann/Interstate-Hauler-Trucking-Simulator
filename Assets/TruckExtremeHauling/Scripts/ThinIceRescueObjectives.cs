using System;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckHitchSports;

namespace LWS.TruckExtremeHauling
{
    // Rescue-only scene adapter. Cargo Crisis owns cable configuration, NWH
    // owns the rigs/contacts and Archimeds owns all recovery displacement.
    public sealed class ThinIceRescueObjectives : MonoBehaviour
    {
        public ThinIceMatch match;
        public Transform equipment, shore;
        public TrailerBody strandedCargo;
        public LoadSensitiveIce incidentIce;
        public SportsFloatingBody cargoFloat;
        public SportsFloatingBody[] playerFloats;
        public SportsFloatingBody[] auxiliaryFloats = Array.Empty<SportsFloatingBody>();
        public ThinIceRecoveryHoist frontLift;
        public Material cableMaterial;
        public Bounds waterBounds;
        bool[] trailerWasActive;
        public float cableReach = 26, reelSpeed = .7f, recoverySeconds = 45;
        public bool CargoRecovered { get; private set; }
        public bool CooperativeSuccess { get; private set; }
        public int RecoveredPlayers { get; private set; }
        public readonly float[] TowedMetres = new float[4];
        public readonly int[] Contributions = new int[4];
        readonly SpringJoint[] cables = new SpringJoint[4];
        readonly LineRenderer[] lines = new LineRenderer[4];
        readonly Rigidbody[] targets = new Rigidbody[4];
        readonly float[] ingress = new float[4];
        readonly bool[] wasSinking = new bool[4], lost = new bool[4], parkAI = new bool[4];
        readonly Vector3[] observedRoute = new Vector3[4];
        NWH.WheelController3D.WheelController[] cargoWheels;
        Vector3 lastCargo;
        float cargoTowDistance, stable, originalThickness;
        bool active;
        bool cargoStandWasActive;
        int lastTowOwner = -1;
        public string Status => !active ? "CONVOY RESCUE · ACTUAL WINCH RECOVERY" :
            CargoRecovered ? "CARGO SAFE · REGROUP ALL RIGS AT NORTHERN SHORE" :
            incidentIce != null && !incidentIce.Broken ? "STRANDED CONVOY · LOADED ICE IS FAILING" :
            "RECOVER THE STRANDED LOAD · ACTION HOOK/RELEASE · HOLD TO REEL";
        public string PlayerStatus(int slot) => lost[slot] ? "RESCUE WINDOW EXPIRED" : ingress[slot] > 0 ? "SINKING · " + Mathf.CeilToInt(recoverySeconds - ingress[slot]) + "s TO RECOVER" :
            frontLift!=null&&frontLift.Owns(slot)?"GANTRY "+frontLift.Phase+" · HOLD ACTION TO LIFT / HITCH · PRESS TO RELEASE":targets[slot] != null ? "WINCH CONNECTED · TOW TO SHORE" : Status;
        public bool PlayerLost(int slot) => lost[slot];

        void Awake()
        {
            originalThickness = incidentIce != null ? incidentIce.thickness : .65f;
            cargoStandWasActive = strandedCargo != null && strandedCargo.Module.module.trailerStand != null && strandedCargo.Module.module.trailerStand.activeSelf;
            cargoWheels = strandedCargo != null ? strandedCargo.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true) : Array.Empty<NWH.WheelController3D.WheelController>();
            for (int i = 0; i < 4; i++)
            {
                var truck = match.trucks[i];
                cables[i] = truck.gameObject.AddComponent<SpringJoint>();
                cables[i].autoConfigureConnectedAnchor = false; cables[i].anchor = new Vector3(0, 1.3f, -2.7f);
                Detach(i);
                var line = new GameObject("Convoy rescue physical cable " + (i + 1)).AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false); line.useWorldSpace = true; line.positionCount = 12; line.widthMultiplier = .055f; line.sharedMaterial = cableMaterial; line.enabled = false; lines[i] = line;
            }
        }

        public void BeginRound()
        {
            if (strandedCargo == null || shore == null || incidentIce == null || cargoFloat == null)
                throw new InvalidOperationException("Install the saved Thin Ice rescue load, fracture site and shore before starting Convoy Rescue.");
            active = true; equipment.gameObject.SetActive(true); CargoRecovered = CooperativeSuccess = false; RecoveredPlayers = 0;
            cargoTowDistance = stable = 0; lastTowOwner = -1;
            Array.Clear(TowedMetres, 0, 4); Array.Clear(Contributions, 0, 4); Array.Clear(ingress, 0, 4); Array.Clear(wasSinking, 0, 4); Array.Clear(lost, 0, 4);Array.Clear(parkAI,0,4);
            for (int i = 0; i < 4; i++) Detach(i);
            strandedCargo.ResetForRound(); strandedCargo.SetBrakes(0); lastCargo = strandedCargo.Body.position;
            if(strandedCargo.Module.module.trailerStand!=null)strandedCargo.Module.module.trailerStand.SetActive(cargoStandWasActive);
            // A real heavy native trailer breaks a deliberately damaged incident
            // tile through the existing supported-load/stress system.
            incidentIce.thickness = .08f;
            cargoFloat.enabled = true;
            trailerWasActive = new bool[match.trailers.Length];
            for (int i = 0; i < match.trailers.Length; i++) { trailerWasActive[i] = match.trailers[i].gameObject.activeSelf; match.trailers[i].SetBrakes(1); match.trailers[i].Body.isKinematic = true; match.trailers[i].gameObject.SetActive(false); }
            foreach (var floating in playerFloats) if (floating != null) floating.enabled = true;
            foreach (var floating in auxiliaryFloats) if (floating != null) floating.enabled = true;
            frontLift?.BeginRound();
        }

        public void StopRound()
        {
            if (!active) return;
            active = false;
            frontLift?.StopRound();
            for (int i = 0; i < 4; i++) Detach(i);
            foreach(var truck in match.trucks){var hitch=truck.GetComponent<HitchCoupler>();if(hitch!=null&&hitch.Attached==strandedCargo)hitch.Release();}
            if (incidentIce != null) incidentIce.thickness = originalThickness;
            if (cargoFloat != null) cargoFloat.enabled = false;
            if (strandedCargo != null) { strandedCargo.SetBrakes(1); if(strandedCargo.Body != null)strandedCargo.Body.isKinematic = true;if(strandedCargo.Module.module.trailerStand!=null)strandedCargo.Module.module.trailerStand.SetActive(cargoStandWasActive); }
            if (playerFloats != null) foreach (var floating in playerFloats) if (floating != null) floating.enabled = false;
            if (auxiliaryFloats != null) foreach (var floating in auxiliaryFloats) if (floating != null) floating.enabled = false;
            if (match != null && match.trailers != null && trailerWasActive != null) for(int i=0;i<match.trailers.Length;i++)if(match.trailers[i]!=null)match.trailers[i].gameObject.SetActive(trailerWasActive[i]);
            if (equipment != null) equipment.gameObject.SetActive(false);
        }

        Rigidbody ChooseTarget(int slot, bool allowAssistance)
        {
            Rigidbody best = null; float distance = float.MaxValue;
            for (int other = 0; other < match.setup.count; other++)
            {
                if (other == slot || lost[other] || ingress[other] <= 0) continue;
                var candidate = match.trucks[other].body;
                float range = (candidate.worldCenterOfMass - match.trucks[slot].body.position).sqrMagnitude;
                if (range < distance) { distance = range; best = candidate; }
            }
            if (best != null) return best;
            if (!CargoRecovered)
            {
                if(frontLift!=null&&(frontLift.HoldsForLift||frontLift.Owner>=0&&frontLift.AttachedTo(frontLift.Owner)))return null;
                if (!allowAssistance) for (int i = 0; i < match.setup.count; i++) if (i != slot && targets[i] == strandedCargo.Body) return null;
                return strandedCargo.Body;
            }
            return null;
        }

        public Vector3 Destination(int slot)
        {
            var truck = match.trucks[slot];
            if(frontLift!=null){if(frontLift.AttachedTo(slot))return shore.position+truck.body.position-strandedCargo.Body.worldCenterOfMass;if(frontLift.Owns(slot))return frontLift.TractorDestination(slot);}
            if (targets[slot] != null)
                return shore.position + truck.body.position - targets[slot].worldCenterOfMass;
            var target = ChooseTarget(slot, false);
            if (target == null) return shore.position + Vector3.right * (slot - 1.5f) * 14;
            var towardShore = Vector3.ProjectOnPlane(shore.position - target.worldCenterOfMass, Vector3.up).normalized;
            return target.worldCenterOfMass + towardShore * 28 + Vector3.right * (slot - 1.5f) * 2;
        }

        public float TowedMass(int slot) => frontLift!=null&&frontLift.AttachedTo(slot)?strandedCargo.Body.mass:targets[slot] != null ? targets[slot].mass : 0;
        public float SupportedRecoveryMass(int slot)
        {
            float mass = TowedMass(slot);
            foreach (var part in match.trucks[slot].GetComponentsInChildren<Rigidbody>(true))
                if (part.gameObject.activeInHierarchy && !part.isKinematic) mass += part.mass;
            return mass;
        }
        public bool CanDriveDirect(Vector3 start,Vector3 end,float supportedMass=0){
            int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.ProjectOnPlane(end-start,Vector3.up).magnitude/5));
            for(int i=1;i<=samples;i++){
                Vector3 at=Vector3.Lerp(start,end,(float)i/samples);
                if(!Physics.Raycast(new Vector3(at.x,3,at.z),Vector3.down,out var hit,7,1,QueryTriggerInteraction.Ignore)||hit.point.y<.2f)return false;
                var tile=hit.collider.GetComponent<LoadSensitiveIce>();
                // Evaluate the whole driven corridor, not only the destination
                // tile. A thick waypoint may hide a weak loaded crossing en route.
                // Reserve matches the existing stress onset and5m/s AI speed;
                // no ice strength or actual failure rule is changed.
                if(tile!=null&&supportedMass>0){
                    float capacity=Mathf.Max(1,tile.bearingMass*tile.thickness*tile.thickness/(.45f*.45f));
                    if(tile.Broken||tile.Stress>.6f||supportedMass*1.1f>capacity*.65f)return false;
                }
            }
            return true;
        }

        public Vector2 DriveAI(int slot, Vector3 routeWaypoint)
        {
            observedRoute[slot]=routeWaypoint;
            if(frontLift!=null&&frontLift.Owns(slot)&&!frontLift.AttachedTo(slot)){parkAI[slot]=frontLift.ParkOperator;return frontLift.DriveAI(slot);}
            Vector3 final = Destination(slot), local = match.trucks[slot].transform.InverseTransformPoint(routeWaypoint);
            float distance = Vector3.ProjectOnPlane(final - match.trucks[slot].body.position, Vector3.up).magnitude;
            float angle = Mathf.Atan2(local.x, local.z), speed = Vector3.Dot(match.trucks[slot].body.linearVelocity, match.trucks[slot].transform.forward);
            bool blocked = Vector3.ProjectOnPlane(routeWaypoint-match.trucks[slot].body.position,Vector3.up).magnitude<2&&!CanDriveDirect(match.trucks[slot].body.position,final);
            float cargoShore = Vector3.ProjectOnPlane(strandedCargo.Body.worldCenterOfMass-shore.position,Vector3.up).magnitude;
            float truckShore = Vector3.ProjectOnPlane(match.trucks[slot].body.position-shore.position,Vector3.up).magnitude;
            parkAI[slot] = blocked || targets[slot]==null&&distance<5 || targets[slot]==strandedCargo.Body&&cargoShore<20 || CargoRecovered&&truckShore<40;
            if(parkAI[slot])return Vector2.zero;
            float desired = Mathf.Min(5, Mathf.Sqrt(2 * Mathf.Max(0, distance - 3))) * Mathf.Lerp(1, .35f, Mathf.Clamp01(Mathf.Abs(angle)));
            return new Vector2(Mathf.Clamp(angle * 1.3f, -1, 1), Mathf.Clamp((desired - speed) * .45f, -1, .8f));
        }

        // On-demand diagnostics for the existing bounded natural match test.
        // No runtime polling, physics changes, state qualification or score writes.
        public string DiagnosticState(int slot)
        {
            var truck=match.trucks[slot].body;var cargo=strandedCargo.Body;var cable=cables[slot];
            string state="park="+parkAI[slot]+" waypoint="+observedRoute[slot].ToString("F2")+" destination="+Destination(slot).ToString("F2")
                +" truckEuler="+truck.rotation.eulerAngles.ToString("F1")+" cargoEuler="+cargo.rotation.eulerAngles.ToString("F1")+" cargoUp="+strandedCargo.transform.up.ToString("F2")+" cargoWheel="+strandedCargo.WheelContact+" jackDeployed="+(strandedCargo.Module.module.trailerStand!=null&&strandedCargo.Module.module.trailerStand.activeSelf);
            if(cable!=null)state+=" cableMax="+cable.maxDistance.ToString("F2")+" cableForce="+cable.currentForce.ToString("F1")+" anchor="+truck.transform.TransformPoint(cable.anchor).ToString("F2")+" eye="+(cable.connectedBody==cargo?cargo.transform.TransformPoint(cable.connectedAnchor):Eye(cargo)).ToString("F2");
            state+=" supportToWaypoint="+SupportDiagnostic(truck.position,observedRoute[slot])+" supportToGoal="+SupportDiagnostic(truck.position,Destination(slot));
            if(frontLift!=null)state+=" "+frontLift.DiagnosticState();
            return state;
        }
        static string SupportDiagnostic(Vector3 start,Vector3 end)
        {
            int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.ProjectOnPlane(end-start,Vector3.up).magnitude/5));
            for(int i=1;i<=samples;i++)
            {
                Vector3 at=Vector3.Lerp(start,end,(float)i/samples);
                if(!Physics.Raycast(new Vector3(at.x,3,at.z),Vector3.down,out var hit,7,1,QueryTriggerInteraction.Ignore))return "GAP@"+at.ToString("F2");
                if(hit.point.y<.2f)return "LOW@"+hit.point.ToString("F2")+":"+hit.collider.name;
            }
            return "supported";
        }

        Vector3 Eye(Rigidbody load)
        {
            if (load == strandedCargo.Body)
            {
                // The upright load needs a low, centered tow point. Pulling
                // from the high rollover eye drives its nose into the ramp.
                if (Vector3.Dot(load.rotation * Vector3.up, Vector3.up) > .65f)
                    return strandedCargo.Kingpin.position;
                return load.transform.TransformPoint(new Vector3(.95f, 3.1f, -.5f));
            }
            return load.worldCenterOfMass + load.transform.up * 1.3f;
        }

        public void StepPlayer(int slot, bool pressed, bool held, bool ai, float dt)
        {
            if (!active) return;
            bool sinking = match.trucks[slot].body.position.y < -.9f;
            if (sinking)
            {
                ingress[slot] += dt;
                if (!wasSinking[slot]) SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "convoy_rescue_ingress", match.setup.players[slot].participantId, "Physical breakthrough; recovery window opened", recoverySeconds);
                wasSinking[slot] = true;
                if (ingress[slot] >= recoverySeconds) lost[slot] = true;
            }
            else if (wasSinking[slot] && match.trucks[slot].Grounded)
            {
                RecoveredPlayers++; wasSinking[slot] = false; ingress[slot] = 0;
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "convoy_player_physically_recovered", match.setup.players[slot].participantId, "Real wheel support restored", RecoveredPlayers);
            }
            if(frontLift!=null&&frontLift.Owns(slot)){
                frontLift.Step(slot,pressed,held,ai,dt);if(ai&&frontLift.Owns(slot)&&frontLift.ParkOperator)Park(slot);DrawCable(slot);return;
            }
            var joint = cables[slot];
            if (joint == null)
            {
                // A broken native cable is a real release, never a latent latch.
                cables[slot] = match.trucks[slot].gameObject.AddComponent<SpringJoint>(); joint = cables[slot]; joint.autoConfigureConnectedAnchor = false; joint.anchor = new Vector3(0, 1.3f, -2.7f); Detach(slot);
            }
            if (targets[slot] != null && joint.connectedBody != targets[slot]) Detach(slot);
            if (targets[slot] == null)
            {
                var target = ChooseTarget(slot, !ai);
                if (target != null)
                {
                    float gap = Vector3.Distance(match.trucks[slot].transform.TransformPoint(joint.anchor), Eye(target));
                    bool stopped = match.trucks[slot].body.linearVelocity.magnitude < 1.5f;
                    bool atApproach = Vector3.ProjectOnPlane(match.trucks[slot].body.position - Destination(slot), Vector3.up).magnitude < 6;
                    if (gap <= cableReach && (pressed || ai && atApproach && stopped))
                    {
                        CargoCrisisMatch.ConfigureRecoveryCable(joint, match.trucks[slot].body, target, Eye(target), cableReach);
                        targets[slot] = target;
                        SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "convoy_recovery_cable_hooked", match.setup.players[slot].participantId, target == strandedCargo.Body ? "Stranded original cargo trailer" : "Fallen player rig", gap);
                    }
                }
            }
            else
            {
                if (!ai && pressed) Detach(slot);
                else if (ai || held) joint.maxDistance = Mathf.Max(10, joint.maxDistance - reelSpeed * dt);
            }
            // A real tensioned towing cable replaces landing-gear support.
            // The detached native trailer otherwise keeps its parking jacks down,
            // ploughing into the extraction ramp as soon as the rear wheels engage.
            if(targets[slot]==strandedCargo.Body&&joint.connectedBody==strandedCargo.Body&&joint.currentForce.sqrMagnitude>10000&&strandedCargo.Module.module.trailerStand!=null)
                strandedCargo.Module.module.trailerStand.SetActive(false);
            if(frontLift!=null&&(ai||held&&!pressed)&&targets[slot]==strandedCargo.Body&&joint.connectedBody==strandedCargo.Body&&joint.currentForce.magnitude>100&&TowedMetres[slot]>6){
                bool uncontested=true;for(int other=0;other<match.setup.count;other++)if(other!=slot&&targets[other]==strandedCargo.Body)uncontested=false;
                if(uncontested&&frontLift.TryClaim(slot)&&ai)Park(slot);
            }
            if(ai&&parkAI[slot])Park(slot);
            DrawCable(slot);
        }

        void Park(int slot){
            match.trucks[slot].TickDrive(Vector2.zero);
            var input=match.trucks[slot].tractorMotor.vehicle.input;
            input.states.throttle=input.states.throttleRaw=0;
            input.states.brakes=input.states.brakesRaw=0;
            input.states.handbrake=input.states.handbrakeRaw=1;
        }

        public void StepObjectives(float dt)
        {
            if (!active) return;
            cargoFloat.Clock = match.RescueClock; cargoFloat.waterHeight = -.55f;cargoFloat.enabled = InsideWater(strandedCargo.Body.worldCenterOfMass);
            foreach (var floating in playerFloats) if (floating != null) { floating.Clock = match.RescueClock; floating.waterHeight = -.55f;floating.enabled = InsideWater(floating.body.worldCenterOfMass); }
            foreach (var floating in auxiliaryFloats) if (floating != null) { floating.Clock = match.RescueClock; floating.waterHeight = -.55f;floating.enabled = InsideWater(floating.body.worldCenterOfMass); }
            foreach (var wheel in cargoWheels) if (wheel.IsGrounded && Physics.Raycast(wheel.transform.position + Vector3.up, Vector3.down, out var hit, 5, 1, QueryTriggerInteraction.Ignore))
                hit.collider.GetComponent<LoadSensitiveIce>()?.RegisterSupportedLoad(strandedCargo.Body, 1f / cargoWheels.Length);
            float moved = Vector3.Distance(lastCargo, strandedCargo.Body.position); lastCargo = strandedCargo.Body.position;
            bool actualTow = false;
            if (moved < 3)
                for (int i = 0; i < match.setup.count; i++) if (frontLift!=null&&frontLift.AttachedTo(i)||targets[i] == strandedCargo.Body && cables[i] != null && cables[i].connectedBody == strandedCargo.Body && cables[i].currentForce.magnitude > 100)
                { TowedMetres[i] += moved; actualTow = true; if(lastTowOwner < 0 || TowedMetres[i] > TowedMetres[lastTowOwner])lastTowOwner = i; }
            if (actualTow) cargoTowDistance += moved;
            bool settled = cargoTowDistance > 20 && strandedCargo.CargoCondition > .1f &&
                Vector3.ProjectOnPlane(strandedCargo.Body.worldCenterOfMass - shore.position, Vector3.up).magnitude < 22 &&
                strandedCargo.WheelContact && Vector3.Dot(strandedCargo.transform.up, Vector3.up) > .8f &&
                strandedCargo.Body.linearVelocity.magnitude < 1 && strandedCargo.Body.angularVelocity.magnitude < .35f;
            stable = settled ? stable + dt : 0;
            if (!CargoRecovered && stable >= 2 && lastTowOwner >= 0)
            {
                CargoRecovered = true; Contributions[lastTowOwner]++; strandedCargo.SetBrakes(1);
                for (int i = 0; i < 4; i++) if (targets[i] == strandedCargo.Body) Detach(i);
                SportsSessionTelemetry.GameplayEvent(gameObject.scene.name, "stranded_convoy_cargo_rescued", match.setup.players[lastTowOwner].participantId, "Actual cable tow, upright wheel support and two-second stopped shore recovery", cargoTowDistance);
            }
            bool allSafe = CargoRecovered;
            for (int i = 0; i < match.setup.count; i++) allSafe &= !lost[i] && ingress[i] == 0 && match.trucks[i].Grounded &&
                Vector3.ProjectOnPlane(match.trucks[i].body.position - shore.position, Vector3.up).magnitude < 50 && match.trucks[i].body.linearVelocity.magnitude < 1.5f;
            CooperativeSuccess = allSafe;
        }

        bool InsideWater(Vector3 point) => point.x >= waterBounds.min.x && point.x <= waterBounds.max.x && point.z >= waterBounds.min.z && point.z <= waterBounds.max.z;

        void DrawCable(int slot)
        {
            if (lines[slot] == null) return;
            lines[slot].enabled = targets[slot] != null;
            if (targets[slot] == null) return;
            Vector3 a = match.trucks[slot].transform.TransformPoint(cables[slot].anchor), b = targets[slot].transform.TransformPoint(cables[slot].connectedAnchor);
            float slack = Mathf.Max(0, cables[slot].maxDistance - Vector3.Distance(a, b));
            for (int i = 0; i < 12; i++) { float t = i / 11f; lines[slot].SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * Mathf.Min(.7f, slack * .35f)); }
        }
        void Detach(int slot)
        {
            if (cables[slot] != null) { cables[slot].spring = cables[slot].damper = 0; cables[slot].connectedBody = null; cables[slot].autoConfigureConnectedAnchor = false; if(match != null && match.trucks[slot] != null)cables[slot].connectedAnchor = match.trucks[slot].transform.TransformPoint(cables[slot].anchor); }
            targets[slot] = null; if (lines[slot] != null) lines[slot].enabled = false;
        }
        public void ReleaseRecoveryCable(int slot){if(slot>=0&&slot<match.setup.count)Detach(slot);}
        void OnDisable() { if (active) StopRound(); }
    }
}
