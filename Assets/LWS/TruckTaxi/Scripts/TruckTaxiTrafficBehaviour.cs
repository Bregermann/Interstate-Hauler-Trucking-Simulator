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
        public float Headway => Personality == TruckTaxiDriverPersonality.Cautious ? 2.2f : Personality == TruckTaxiDriverPersonality.Normal ? 1.65f : 1.15f;
        private TruckTaxiTrafficAdapter owner;
        private Rigidbody body;
        private Component ai, movePath;
        private Behaviour aiBehaviour;
        private Action<float> setSpeed, setFollowing, setDeceleration;
        private Func<bool> getStopped;
        private Action<float, float, float, float> drive;
        private FieldInfo pathField;
        private MethodInfo initializePath;
        private AudioSource horn;
        private TruckTaxiRoadRage rage;
        private float nextSense, desiredSpeed, brake, blockedSeconds, nextHorn, nextLaneChange, changeUntil;
        private float reversingUntil, nextRecovery;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] gaps = new Collider[48];
        private WheelCollider frontWheel;
        private WheelCollider[] wheels;
        private bool ownsBrakeHold;
        private bool ready;

        public bool Initialize(TruckTaxiTrafficAdapter traffic, int lane, string stableId, AudioClip hornClip)
        {
            if (ready) return true;
            owner = traffic; LaneIndex = lane;
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
            uint hash = 2166136261;
            foreach (char c in stableId) hash = (hash ^ c) * 16777619;
            int pick = (int)(hash % 100);
            Personality = pick < 20 ? TruckTaxiDriverPersonality.Cautious : pick < 65 ? TruckTaxiDriverPersonality.Normal :
                pick < 84 ? TruckTaxiDriverPersonality.Impatient : pick < 96 ? TruckTaxiDriverPersonality.Aggressive : TruckTaxiDriverPersonality.Reckless;
            float factor = Personality == TruckTaxiDriverPersonality.Cautious ? .78f : Personality == TruckTaxiDriverPersonality.Normal ? .95f :
                Personality == TruckTaxiDriverPersonality.Impatient ? 1.05f : Personality == TruckTaxiDriverPersonality.Aggressive ? 1.12f : 1.18f;
            PreferredSpeed = traffic.LaneSpeed(lane) * factor * Mathf.Lerp(.95f, 1.05f, (hash % 103) / 102f);
            desiredSpeed = PreferredSpeed;
            var box = ai.GetComponent<BoxCollider>();
            if (box != null) VehicleLength = Mathf.Clamp(box.size.z * Mathf.Abs(ai.transform.lossyScale.z), 3, 18);
            wheels=ai.GetComponentsInChildren<WheelCollider>();
            foreach (var wheel in wheels)
                if (ai.transform.InverseTransformPoint(wheel.transform.position).z > 0) { frontWheel = wheel; break; }
            rage = GetComponent<TruckTaxiRoadRage>();
            if (rage != null) rage.collisionTriggerChance = Personality >= TruckTaxiDriverPersonality.Aggressive ? .2f : .02f;
            if (hornClip != null)
            {
                var sound = new GameObject("Taxi Traffic Horn"); sound.transform.SetParent(ai.transform, false);
                horn = sound.AddComponent<AudioSource>(); horn.playOnAwake = false; horn.spatialBlend = 1;
                horn.clip = hornClip; horn.volume = .35f; horn.minDistance = 6; horn.maxDistance = 65;
                TruckTaxiAudioController.Instance?.Route(horn, TruckTaxiAudioCategory.World);
            }
            nextSense = Time.time + (hash % 100) * .003f;
            nextHorn = Time.time + 2 + hash % 7;
            nextLaneChange = Time.time + 3 + hash % 9;
            ready = true; return true;
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
            if(reversingUntil>0)
            {
                if(Time.time<reversingUntil && Speed<1.3f) { drive(0,-.35f,0,0); return; }
                reversingUntil=0; drive(0,0,1,0); ownsBrakeHold=true; aiBehaviour.enabled=true; blockedSeconds=0;
            }
            if(!aiBehaviour.enabled) return;
            var player = TruckTaxiBootstrap.Instance?.Player;
            float range = player != null ? Vector3.Distance(body.position, player.transform.position) : 0;
            if (Time.time >= nextSense)
            {
                float interval = range < 100 ? .1f : range < 220 ? .3f : .8f;
                Sense(interval, range < 160); nextSense = Time.time + interval;
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
            desiredSpeed = PreferredSpeed;
            brake = 0; EmergencyBraking = false;
            if (!float.IsPositiveInfinity(ObstacleDistance))
            {
                float safe = SafeFollowingDistance(speed, obstacleSpeed, Headway, 6, VehicleLength);
                float stopGap = pedestrian ? 5 : 3;
                float permitted = Mathf.Sqrt(Mathf.Max(0, obstacleSpeed * obstacleSpeed + 2 * 4.5f * (ObstacleDistance - stopGap)));
                desiredSpeed = Mathf.Min(PreferredSpeed, permitted, obstacleSpeed + Mathf.Max(0, ObstacleDistance - stopGap) / Headway);
                EmergencyBraking = ObstacleDistance < stopGap + speed * speed / 14f;
                brake = EmergencyBraking ? 1 : Mathf.Clamp01((speed - desiredSpeed) / 3);
                if (ObstacleDistance < stopGap) { desiredSpeed = 0; brake = 1; }
                blockedSeconds = desiredSpeed < PreferredSpeed * .65f ? blockedSeconds + interval : 0;
                if (!signal && (EmergencyBraking || blockedSeconds > 3)) Honk();
                if (!signal && !pedestrian && allowLaneChanges && Time.time >= nextLaneChange &&
                    (blockedSeconds > (Personality == TruckTaxiDriverPersonality.Cautious ? 7 : 2) || EmergencyBraking))
                {
                    nextLaneChange = Time.time + (Personality >= TruckTaxiDriverPersonality.Aggressive ? 6 : 12);
                    owner.TryChangeLane(this, EmergencyBraking);
                }
                FollowingDistance = safe;
                if(!signal && !pedestrian && blockedSeconds>20 && Time.time>=nextRecovery && speed<.2f &&
                    Personality>=TruckTaxiDriverPersonality.Impatient &&
                    GapClear(body.position-ai.transform.forward*(VehicleLength+2),ai.transform.forward,1,3))
                {
                    nextRecovery=Time.time+45; reversingUntil=Time.time+.85f; aiBehaviour.enabled=false; Honk();
                    ReleaseBrakeHold();
                }
            }
            else blockedSeconds = 0;
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
            return true;
        }

        public void Honk()
        {
            if (horn == null || horn.clip == null || Time.time < nextHorn) return;
            nextHorn = Time.time + (Personality == TruckTaxiDriverPersonality.Cautious ? 25 : Personality == TruckTaxiDriverPersonality.Normal ? 16 : 9);
            horn.PlayOneShot(horn.clip); HornsPlayed++;
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
