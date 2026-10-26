using System;
using UnityEngine;

namespace LWS.TruckFishing
{
    // Ambient route semantics over genuine vendor skeletal clips. Species is
    // explicit: an animated wolf does not fulfill the missing bear requirement.
    public sealed class FishingCarnivalWildlife : MonoBehaviour
    {
        public FishingMatch match;
        public FishingStream stream;
        public FishingCarnivalWater water;
        public Animator animator;
        public GameObject visual;
        public string species = "Wolf";
        public Vector3[] route = Array.Empty<Vector3>();
        public bool[] swimSegments = Array.Empty<bool>();
        public Transform[] feet = Array.Empty<Transform>();
        public Terrain terrain;
        public int initialSeed = 176;
        public float initialDelay = 8, walkSpeed = .85f, swimSpeed = .7f;
        public int Visits { get; private set; }
        public int WaterSteps { get; private set; }
        public int Segment { get; private set; }
        public string AnimationState { get; private set; } = "Idle";
        float wait, stepClock;
        bool started;
        System.Random random;
        Vector3 previous;
        bool[] wasWet;
        static readonly int Idle = Animator.StringToHash("Base Layer.Idle"), Walk = Animator.StringToHash("Base Layer.Walk"),
            Left = Animator.StringToHash("Base Layer.Left"), Right = Animator.StringToHash("Base Layer.Right"), Swim = Animator.StringToHash("Base Layer.Swim"), SwimIdle = Animator.StringToHash("Base Layer.SwimIdle");
        void OnEnable()
        {
            random = new System.Random(initialSeed); wait = initialDelay; started = false; Segment = 0;
            wasWet = new bool[feet.Length];
            if (animator != null) { animator.applyRootMotion = false; animator.fireEvents = false; }
            if (visual != null) visual.SetActive(false);
        }
        void Update() { if (match != null && match.Paused) return; TickPresentation(Time.deltaTime); }
        public void TickPresentation(float dt)
        {
            if (!isActiveAndEnabled || animator == null || visual == null || stream == null || route.Length < 2 || !float.IsFinite(dt) || dt <= 0) return;
            if (wait > 0)
            {
                wait -= dt; if (wait > 0) return;
                if (!started) { transform.localPosition = route[0]; previous = transform.position; Segment = 1; visual.SetActive(true); started = true; Visits++; }
            }
            float groundHere = terrain != null ? terrain.SampleHeight(transform.position) + terrain.transform.position.y : transform.position.y;
            bool swimming = water != null && Segment < swimSegments.Length && swimSegments[Segment] && groundHere < water.WaterY - .35f;
            Vector3 target = stream.transform.TransformPoint(route[Segment]);
            Vector3 delta = target - transform.position; delta.y = 0;
            if (delta.magnitude < .08f)
            {
                Segment++;
                if (Segment >= route.Length) { visual.SetActive(false); started = false; Segment = 0; wait = 18 + (float)random.NextDouble() * 20; return; }
                wait = 2 + (float)random.NextDouble() * 3; SetState(swimming ? "SwimIdle" : "Idle", swimming ? SwimIdle : Idle, 1); return;
            }
            float angle = Vector3.SignedAngle(transform.forward, delta, Vector3.up);
            Quaternion rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, dt * 75);
            float speed = swimming ? swimSpeed : walkSpeed;
            // Turn clips visibly move legs while the root changes heading;
            // straight travel waits for alignment rather than sliding sideways.
            SetState(swimming ? "Swim" : Mathf.Abs(angle) > 22 ? angle < 0 ? "Left" : "Right" : "Walk", swimming ? Swim : Mathf.Abs(angle) > 22 ? angle < 0 ? Left : Right : Walk, Mathf.Clamp(speed / .85f, .6f, 1.4f));
            var next = Vector3.MoveTowards(transform.position, target, speed * dt * Mathf.Clamp01(1 - Mathf.Abs(angle) / 100));
            float ground = terrain != null ? terrain.SampleHeight(next) + terrain.transform.position.y : target.y;
            next.y = swimming ? Mathf.Max(ground, water.WaterY - .42f) : ground;
            transform.position = next;
            var velocity = (transform.position - previous) / dt; previous = transform.position;
            stepClock += dt;
            if (water != null && stepClock > .22f)
            {
                stepClock = 0;
                bool inChannel = Mathf.Abs(stream.transform.InverseTransformPoint(transform.position).x - FishingStream.ChannelCenter(stream.transform.InverseTransformPoint(transform.position).z)) < 4.15f;
                if (inChannel)
                {
                    water.Disturb(transform.position, velocity, 1, swimming ? .6f : .3f, !swimming); WaterSteps++;
                    for (int i = 0; i < feet.Length; i++)
                    {
                        if (feet[i] == null) continue;
                        bool wet = feet[i].position.y <= water.WaterY + .02f;
                        if (wet && !wasWet[i]) { water.Disturb(feet[i].position, velocity, .8f, .15f, true); WaterSteps++; }
                        wasWet[i] = wet;
                    }
                }
            }
        }
        void SetState(string label, int state, float speed)
        {
            animator.speed = speed;
            if (AnimationState == label && animator.isInitialized) return;
            AnimationState = label; animator.CrossFadeInFixedTime(state, .2f);
        }
        void OnDisable() { if (visual != null) visual.SetActive(false); }
    }
}
