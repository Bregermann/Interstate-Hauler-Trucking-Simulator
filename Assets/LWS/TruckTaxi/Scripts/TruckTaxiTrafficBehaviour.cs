using System;
using System.Reflection;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiDriverPersonality { Cautious, Normal, Impatient, Aggressive, Reckless }

    // Taxi policy around UTS public controls; UTS still steers its MovePath and drives its wheels.
    [DefaultExecutionOrder(210), DisallowMultipleComponent]
    public sealed class TruckTaxiTrafficBehaviour : MonoBehaviour
    {
        public TruckTaxiDriverPersonality Personality { get; private set; }
        public float PreferredSpeed { get; private set; }
        public float FollowingDistance { get; private set; }
        public float ObstacleDistance { get; private set; }
        public bool EmergencyBraking { get; private set; }
        public int LaneChanges { get; private set; }
        public int HornsPlayed { get; private set; }
        public int LaneIndex { get; private set; }
        public bool ChangingLane => Time.time < changeUntil;
        public float Speed => body != null ? body.linearVelocity.magnitude : 0;
        public float VehicleLength { get; private set; } = 4.5f;
        public float Headway => (Personality == TruckTaxiDriverPersonality.Cautious ? 2.4f :
            Personality == TruckTaxiDriverPersonality.Normal ? 1.9f :
            Personality == TruckTaxiDriverPersonality.Impatient ? 1.55f : 1.25f) *
            (1 + (owner != null ? owner.WeatherCaution : 0) * .55f) *
            (rage != null && rage.IsActive ? .8f : 1f) *
            (VehicleLength > 6 ? 1.2f : 1f);
        private TruckTaxiTrafficAdapter owner;
        private Rigidbody body;
        private Component ai, movePath;
        private Behaviour aiBehaviour;
        private Action<float> setSpeed, setFollowing, setDeceleration;
        private Func<bool> getStopped;
        private Action<float, float, float, float> drive;
        private FieldInfo pathField;
        private MethodInfo initializePath;
        private TruckTaxiTrafficHorn horn;
        private TruckTaxiRoadRage rage;
        private float nextSense, desiredSpeed, brake, blockedSeconds, nextLaneChange, changeUntil;
        private bool honkedForBlock;
        private uint identityHash;
        private float reversingUntil, nextRecovery;
        public int RecoveryAttempts { get; private set; }
        private Vector3 progressAnchor;
        private float lastProgressAt, recoverySteering;
        private TruckTaxiTrafficPooledActor pooledActor;
        private float presentationStopUntil;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] gaps = new Collider[48];
        private WheelCollider frontWheel;
        private WheelCollider[] wheels;
        private bool ownsBrakeHold;
        private bool ready;

        public bool Initialize(TruckTaxiTrafficAdapter traffic, int lane, string stableId, AudioClip hornClip)
        {
            if (ready)
            {
                owner = traffic; LaneIndex = lane;
                ResetForPool();
                horn?.Configure(hornClip, owner.longTrafficHorn, owner.angryTrafficHorn);
                SetIdentity(stableId);
                return true;
            }
            owner = traffic; LaneIndex = lane;
            pooledActor = GetComponent<TruckTaxiTrafficPooledActor>();
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Name == "CarAIController") ai = component;
                if (component.GetType().Name == "MovePath") movePath = component;
                if (component.GetType().Name == "CarMove")
                    drive = (Action<float, float, float, float>)Delegate.CreateDelegate(typeof(Action<float, float, float, float>), component, component.GetType().GetMethod("Move"));
            }
            if (ai == null || movePath == null || drive == null) return false;
            aiBehaviour = ai as Behaviour;
            body = ai.GetComponent<Rigidbody>();
            if (body == null) return false;
            setSpeed = Setter("MOVE_SPEED"); setFollowing = Setter("TO_CAR"); setDeceleration = Setter("DECREASE");
            getStopped = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), ai, ai.GetType().GetProperty("TEMP_STOP").GetGetMethod());
            pathField = movePath.GetType().GetField("walkPath");
            initializePath = movePath.GetType().GetMethod("InitStartPosition");
            float longest = 0;
            foreach (var box in ai.GetComponentsInChildren<BoxCollider>())
                longest = Mathf.Max(longest, box.size.z * Mathf.Abs(box.transform.lossyScale.z));
            if (longest > 0) VehicleLength = Mathf.Clamp(longest, 3, 18);
            wheels=ai.GetComponentsInChildren<WheelCollider>();
            foreach (var wheel in wheels)
                if (ai.transform.InverseTransformPoint(wheel.transform.position).z > 0) { frontWheel = wheel; break; }
            rage = GetComponent<TruckTaxiRoadRage>();
            foreach (var source in ai.GetComponentsInChildren<AudioSource>(true))
            {
                source.spatialBlend = 1;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = Mathf.Min(source.minDistance, 6);
                source.maxDistance = Mathf.Min(source.maxDistance, 65);
                TruckTaxiAudioController.Instance?.Route(source, TruckTaxiAudioCategory.World);
            }
            horn = GetComponent<TruckTaxiTrafficHorn>() ?? gameObject.AddComponent<TruckTaxiTrafficHorn>();
            horn.Configure(hornClip, owner.longTrafficHorn, owner.angryTrafficHorn);
            SetIdentity(stableId);
            ready = true; return true;
        }

        private void SetIdentity(string stableId)
        {
            uint hash = 2166136261;
            foreach (char c in stableId) hash = (hash ^ c) * 16777619;
            identityHash = hash;
            Personality = owner.Tuning.Choose(hash, stableId.StartsWith("taxi.rival.", StringComparison.Ordinal));
            float factor = Personality == TruckTaxiDriverPersonality.Cautious ? .78f : Personality == TruckTaxiDriverPersonality.Normal ? .95f :
                Personality == TruckTaxiDriverPersonality.Impatient ? 1.05f : Personality == TruckTaxiDriverPersonality.Aggressive ? 1.12f : 1.18f;
            PreferredSpeed = owner.LaneSpeed(LaneIndex) * factor * Mathf.Lerp(.95f, 1.05f, (hash % 103) / 102f);
            desiredSpeed = PreferredSpeed;
            progressAnchor = body.position; lastProgressAt = Time.time;
            if (rage != null) rage.collisionTriggerChance = Personality >= TruckTaxiDriverPersonality.Aggressive ? .2f : .02f;
            nextSense = Time.time + (hash % 100) * .003f;
            nextLaneChange = Time.time + 3 + hash % 9;
        }

        public void ResetForPool()
        {
            ReleaseBrakeHold();
            if (horn != null) horn.ResetForPool();
            if (reversingUntil > 0 && aiBehaviour != null) aiBehaviour.enabled = true;
            reversingUntil = nextRecovery = blockedSeconds = brake = changeUntil = 0;
            RecoveryAttempts = 0;
            if (body != null) progressAnchor = body.position;
            lastProgressAt = Time.time;
            EmergencyBraking = ownsBrakeHold = honkedForBlock = false;
            FollowingDistance = ObstacleDistance = 0;
            LaneChanges = HornsPlayed = 0;
            presentationStopUntil = 0;
        }

        public void HoldForPresentation(float seconds)
        {
            presentationStopUntil = Mathf.Max(presentationStopUntil, Time.time + Mathf.Clamp(seconds, 0, 8));
        }

        public void ReactToHazard(float seconds)
        {
            HoldForPresentation(seconds);
            Honk();
        }

        private Action<float> Setter(string name) => (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), ai, ai.GetType().GetProperty(name).GetSetMethod());

        public static float SafeFollowingDistance(float speed, float obstacleSpeed, float headway, float braking, float length)
        {
            float stoppingDifference = Mathf.Max(0, speed * speed - obstacleSpeed * obstacleSpeed) / (2 * Mathf.Max(1, braking));
            return Mathf.Max(3, length * .35f) + speed * Mathf.Max(.8f, headway) + stoppingDifference;
        }

        private void LateUpdate()
        {
            if (!ready || owner == null || body == null || aiBehaviour == null) return;
            if (body.isKinematic) return;
            if (pooledActor == null) pooledActor = GetComponent<TruckTaxiTrafficPooledActor>();
            if(reversingUntil>0)
            {
                bool clear = GapClear(body.position-ai.transform.forward*(VehicleLength*.5f+2),ai.transform.forward,1,3);
                if(Time.time<reversingUntil && Speed<1.3f && clear)
                { drive(0,-.35f,0,recoverySteering); return; }
                reversingUntil=0; drive(0,0,1,0); ownsBrakeHold=true;
                owner.TryReacquireTrafficRoute(this);
                aiBehaviour.enabled=true; blockedSeconds=0;
            }
            if(!aiBehaviour.enabled) return;
            var player = TruckTaxiBootstrap.Instance?.Player;
            float range = player != null ? Vector3.Distance(body.position, player.transform.position) : 0;
            if (Time.time >= nextSense)
            {
                float interval = range < 100 ? .1f : range < 220 ? .3f : .8f;
                Sense(interval, range < 160); nextSense = Time.time + interval;
                if (!gameObject.activeInHierarchy || !aiBehaviour.enabled) return;
            }
            if (Time.time < presentationStopUntil)
            {
                desiredSpeed = 0;
                brake = 1;
            }
            // Vendor PushRay runs in Update; preserve its red-light stop, then apply the stricter Taxi envelope.
            bool vendorStop = getStopped();
            setFollowing(FollowingDistance);
            setDeceleration(EmergencyBraking ? 7 : 3.5f);
            setSpeed(desiredSpeed);
            if (brake > .01f || vendorStop)
            {
                float strength=Mathf.Max(brake,vendorStop ? .8f : 0);
                // UTS foot brake becomes reverse torque below 5 MPH. Hold the handbrake instead at walking speed.
                drive(0, Speed>3 ? -strength : 0, Speed<=3 ? strength : 0, frontWheel != null ? frontWheel.steerAngle / 25f : 0);
                ownsBrakeHold=true;
            }
            else ReleaseBrakeHold();
        }

        private void Sense(float interval, bool allowLaneChanges)
        {
            float speed = Speed;
            FollowingDistance = SafeFollowingDistance(speed, 0, Headway, 6, VehicleLength);
            float reach = Mathf.Clamp(FollowingDistance + 12, 18, 95);
            Vector3 forward = ai.transform.forward;
            Vector3 origin = body.position + Vector3.up * .7f + forward * (VehicleLength * .5f + .3f);
            int count = Physics.BoxCastNonAlloc(origin, new Vector3(.85f, .4f, .1f), forward, hits, ai.transform.rotation, reach, ~0, QueryTriggerInteraction.Ignore);
            ObstacleDistance = float.PositiveInfinity;
            float obstacleSpeed = PreferredSpeed;
            bool pedestrian = false;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i]; var collider = hit.collider;
                if (collider == null || collider.transform.IsChildOf(transform) || collider is WheelCollider) continue;
                var other = collider.attachedRigidbody;
                var person = collider.GetComponentInParent<TruckTaxiPedestrian>();
                // Ignore road surfaces, but not static walls / street furniture in the swept body envelope.
                if (other == null && person == null && (hit.normal.y > .55f || collider.bounds.max.y < origin.y - .3f)) continue;
                if (hit.distance >= ObstacleDistance) continue;
                ObstacleDistance = hit.distance; pedestrian = person != null;
                obstacleSpeed = other != null ? Mathf.Max(0, Vector3.Dot(other.linearVelocity, forward)) : 0;
            }
            float stopLine = owner.SignalStoppingDistance(origin, forward, reach);
            bool signal = stopLine < ObstacleDistance;
            if (signal) { ObstacleDistance = stopLine; obstacleSpeed = 0; }
            desiredSpeed = PreferredSpeed * (1 - (owner != null ? owner.WeatherCaution : 0) * .35f);
            brake = 0; EmergencyBraking = false;
            if (!float.IsPositiveInfinity(ObstacleDistance))
            {
                float safe = SafeFollowingDistance(speed, obstacleSpeed, Headway, 6, VehicleLength);
                float stopGap = pedestrian ? 5 : 3;
                float permitted = Mathf.Sqrt(Mathf.Max(0, obstacleSpeed * obstacleSpeed + 2 * 4.5f * (ObstacleDistance - stopGap)));
                desiredSpeed = Mathf.Min(desiredSpeed, permitted, obstacleSpeed + Mathf.Max(0, ObstacleDistance - stopGap) / Headway);
                EmergencyBraking = ObstacleDistance < stopGap + speed * speed / 14f;
                brake = EmergencyBraking ? 1 : Mathf.Clamp01((speed - desiredSpeed) / 3);
                if (ObstacleDistance < stopGap) { desiredSpeed = 0; brake = 1; }
                blockedSeconds = desiredSpeed < PreferredSpeed * .65f ? blockedSeconds + interval : 0;
                if (blockedSeconds == 0) honkedForBlock = false;
                if (!signal && !pedestrian && !honkedForBlock && blockedSeconds > 3)
                    honkedForBlock = Honk();
                if (!signal && !pedestrian && allowLaneChanges && Time.time >= nextLaneChange &&
                    (EmergencyBraking || (obstacleSpeed < PreferredSpeed * .7f &&
                    blockedSeconds > (Personality == TruckTaxiDriverPersonality.Cautious ? 12 :
                        Personality == TruckTaxiDriverPersonality.Normal ? 9 : 5))))
                {
                    nextLaneChange = Time.time + 2;
                    owner.TryChangeLane(this, EmergencyBraking);
                }
                FollowingDistance = safe;
            }
            else { blockedSeconds = 0; honkedForBlock = false; }
            bool legitimateWait = signal || pedestrian || Time.time < presentationStopUntil || rage?.IsActive == true ||
                pooledActor?.MissionReserved == true;
            if (legitimateWait || (body.position-progressAnchor).sqrMagnitude > 36)
            { progressAnchor=body.position; lastProgressAt=Time.time; RecoveryAttempts=0; }
            if (!legitimateWait && Time.time-lastProgressAt > 12 && Time.time>=nextRecovery && speed<.5f)
            {
                nextRecovery=Time.time+8;
                if (!TryCountRecoveryAttempt()) { owner.TryRecycleStuckTraffic(this); return; }
                if (!GapClear(body.position-ai.transform.forward*(VehicleLength*.5f+2),ai.transform.forward,1,3)) return;
                recoverySteering = (identityHash + RecoveryAttempts) % 2 == 0 ? .45f : -.45f;
                reversingUntil=Time.time+1.5f; aiBehaviour.enabled=false; Honk(); ReleaseBrakeHold();
            }
        }

        public bool ReacquireRoute(Component path, int pointBeforeTarget)
        {
            if (!ready || path == null) return false;
            try
            {
                pathField.SetValue(movePath, path);
                initializePath.Invoke(movePath, new object[] { 0, pointBeforeTarget, true, true });
                return true;
            }
            catch (Exception error) { Debug.LogWarning("UTS route reacquisition failed: " + error.Message, this); return false; }
        }

        private bool TryCountRecoveryAttempt()
        {
            if (RecoveryAttempts >= 3) return false;
            RecoveryAttempts++; return true;
        }

        public bool GapClear(Vector3 position, Vector3 forward, float forwardGap, float rearGap)
        {
            Vector3 center = position + forward * (forwardGap - rearGap) * .5f + Vector3.up;
            int count = Physics.OverlapBoxNonAlloc(center, new Vector3(1.15f, .8f, (forwardGap + rearGap) * .5f), gaps,
                Quaternion.LookRotation(forward, Vector3.up), ~0, QueryTriggerInteraction.Ignore);
            if (count == gaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var collider = gaps[i];
                if (collider == null || collider.transform.IsChildOf(transform) || collider is WheelCollider) continue;
                if (collider.attachedRigidbody != null || collider.GetComponentInParent<TruckTaxiPedestrian>() != null || collider.bounds.max.y > position.y + .8f) return false;
            }
            return true;
        }

        public bool SwitchLane(Component path, int pointBeforeTarget, int newLane)
        {
            if (!ready || path == null || ChangingLane) return false;
            pathField.SetValue(movePath, path);
            initializePath.Invoke(movePath, new object[] { 0, pointBeforeTarget, true, true });
            LaneIndex = newLane; LaneChanges++; changeUntil = Time.time + 4; blockedSeconds = 0;
            nextLaneChange = Time.time + owner.Tuning.LaneChangeCooldown(Personality, identityHash + (uint)LaneChanges * 37);
            return true;
        }

        public bool Honk(TruckTaxiHonkPattern pattern = TruckTaxiHonkPattern.Short, bool highPriority = false)
        {
            if (horn == null || !horn.TryHonk(pattern, Personality, highPriority)) return false;
            HornsPlayed++;
            return true;
        }
        private void ReleaseBrakeHold()
        {
            if(!ownsBrakeHold) return;
            // CarMove does not clear handbrake torque when releasing it below 5 MPH.
            // Release only the braking owned by this adapter, once, through the public wheel API.
            foreach(var wheel in wheels) if(wheel!=null) wheel.brakeTorque=0;
            ownsBrakeHold=false;
        }
        private void OnDisable() { if(reversingUntil>0 && aiBehaviour!=null) aiBehaviour.enabled=true; reversingUntil=0; ReleaseBrakeHold(); }
    }
}
