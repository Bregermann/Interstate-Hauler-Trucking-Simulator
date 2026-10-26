using System;
using System.Collections.Generic;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckNinjaWarrior
{
    // The real attempt still resets immediately. A bounded render-only visual
    // can finish its captured fall; this is not recorded motion or resimulation.
    public sealed class NinjaCarnivalEnvironment : MonoBehaviour
    {
        public NinjaMatch match;
        public Transform waterSurface;
        public Bounds waterBounds;
        public ParticleSystem[] splash = Array.Empty<ParticleSystem>();
        public LineRenderer[] rings = Array.Empty<LineRenderer>();
        public ParticleSystem[] entranceFire = Array.Empty<ParticleSystem>();
        public Animator[] newFans = Array.Empty<Animator>();
        public MeshFilter[] addedPosedFans = Array.Empty<MeshFilter>();
        public Mesh[] addedIdle = Array.Empty<Mesh>(), addedCheer = Array.Empty<Mesh>();
        public int CapturedFalls { get; private set; }
        public int WaterSplashes { get; private set; }
        public Vector3 CapturedPosition { get; private set; }
        public Vector3 CapturedVelocity { get; private set; }
        public float CapturedMass { get; private set; }
        public float CapturedVisualBottom { get; private set; }
        public Vector3 LastSplashPoint { get; private set; }
        public float LastVisualBottom { get; private set; }
        public int AmbientGestures { get; private set; }
        public bool FallShotActive => shotActive;
        public bool FallShotBorrowed => shotBorrowed;
        public float FallShotAge => shotAge;
        public float FallShotMaximumSeconds => 1.25f;
        public int CompletedFallShots { get; private set; }
        public int ActiveGhosts { get { int count = 0; foreach (var ghost in ghosts) if (ghost != null && ghost.active) count++; return count; } }
        sealed class Ghost
        {
            public SportsEventTruck source;
            public GameObject visual;
            public Transform[] live, parts;
            public Vector3[] envelope;
            public Vector3 velocity, angular;
            public float age, mass;
            public bool active;
        }
        readonly Ghost[] ghosts = new Ghost[2];
        float[] ringBorn = Array.Empty<float>();
        float nextAmbient, restorePose;
        string matchId;
        int nextGhost, nextSplash, ambientSerial;
        NinjaMatch subscribed;
        Ghost shotGhost;
        bool shotActive, shotBorrowed, shotSnap, shotContact;
        bool fitEnabled, fitArena, priorOrthographic;
        Rect priorBroadcastRect;
        float shotAge, shotContactAge;
        Vector3 shotPoint;
        public float WaterHeight => waterSurface != null ? waterSurface.position.y : waterBounds.center.y;
        void OnEnable()
        {
            if (match != null && (match.fallPresentation == null || match.fallPresentation == this))
            { match.fallPresentation = this; subscribed = match; subscribed.AttemptRestarting += Capture; }
            ringBorn = new float[rings.Length]; foreach (var ring in rings) if (ring != null) ring.enabled = false;
            foreach (var fire in entranceFire) if (fire != null) fire.Play(true);
        }
        void Capture(SportsEventTruck truck, bool water)
        {
            if (!water || waterSurface == null || match == null || !match.Running || match.Paused) return;
            try
            {
                EnsureMatch();
                CapturedPosition = truck.body.position; CapturedVelocity = truck.body.linearVelocity; CapturedMass = truck.body.mass; CapturedFalls++;
                int at = nextGhost++ % ghosts.Length; var ghost = ghosts[at];
                if (ghost == null || ghost.source != truck || ghost.visual == null || ghost.live.Length != truck.GetComponentsInChildren<Transform>(true).Length)
                {
                    if (ghost?.visual != null) Dispose(ghost.visual);
                    var copy = SportsReplayVisuals.Copy(truck.transform); copy.name = "Ninja render-only fall continuation"; copy.SetActive(false);
                    ghost = new Ghost { source = truck, visual = copy, live = truck.GetComponentsInChildren<Transform>(true), parts = copy.GetComponentsInChildren<Transform>(true) }; ghosts[at] = ghost;
                }
                if (ghost.live.Length != ghost.parts.Length) throw new InvalidOperationException("Fall rendering hierarchy differs from its source.");
                for (int i = 1; i < ghost.live.Length; i++) { ghost.parts[i].localPosition = ghost.live[i].localPosition; ghost.parts[i].localRotation = ghost.live[i].localRotation; ghost.parts[i].localScale = ghost.live[i].localScale; ghost.parts[i].gameObject.SetActive(ghost.live[i].gameObject.activeSelf); }
                var points = new List<Vector3>();
                foreach (var renderer in truck.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
                    var bounds = renderer.localBounds;
                    for (int i = 0; i < 8; i++) points.Add(truck.transform.InverseTransformPoint(renderer.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)))));
                }
                if (points.Count == 0) throw new InvalidOperationException("Fall source has no visible render envelope.");
                ghost.envelope = points.ToArray(); ghost.velocity = CapturedVelocity; ghost.angular = truck.body.angularVelocity; ghost.mass = CapturedMass; ghost.age = 0; ghost.active = true;
                ghost.visual.transform.SetPositionAndRotation(truck.transform.position, truck.transform.rotation); ghost.visual.transform.localScale = truck.transform.lossyScale; ghost.visual.SetActive(true);
                var bottom = Bottom(ghost); CapturedVisualBottom = bottom.y;
                CancelFallShot();
                if (CanBorrowShot() && InPool(bottom) && Finite(ghost.velocity) && Finite(CapturedPosition))
                { shotGhost = ghost; shotActive = shotSnap = true; shotAge = shotContactAge = 0; shotContact = false; shotPoint = new Vector3(bottom.x, WaterHeight, bottom.z); }
                else match.broadcast?.broadcastFraming?.Release();
                if (bottom.y <= WaterHeight) Impact(ghost, bottom);
            }
            catch (Exception exception)
            {
                // Presentation is optional; a content failure must not prevent
                // the original accepted-fall Prepare/reset from completing.
                Debug.LogWarning("Ninja fall presentation refused: " + exception.Message, this); CancelFallShot(); ClearGhosts(); match.broadcast?.broadcastFraming?.Release();
            }
        }
        bool CanBorrowShot() => match != null && match.fallPresentation == this && match.drivingViews != null && match.drivingViews.rally != null && match.broadcast != null && match.broadcast.view != null && match.broadcast.rig != null && match.broadcast.follow != null && match.broadcast.target != null && match.broadcast.aim != null && waterSurface != null && waterBounds.size.x > 0 && waterBounds.size.z > 0;
        static bool Finite(Vector3 point) => float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
        bool InPool(Vector3 point) => Finite(point) && point.x >= waterBounds.min.x && point.x <= waterBounds.max.x && point.z >= waterBounds.min.z && point.z <= waterBounds.max.z;
        // Called only by NinjaMatch.Present, before its normal camera path.
        // No camera transform is written by Environment.Update/LateUpdate.
        public bool TryPresentFall(float dt)
        {
            if (!shotActive) return false;
            if (!CanBorrowShot() || !match.Running || match.Finished || match.ReplayControls || match.rules.Phase != NinjaPhase.Countdown || match.rules.StateClock >= 2.8f)
            { CancelFallShot(); return false; }
            if (!match.Paused && float.IsFinite(dt) && dt > 0) shotAge = Mathf.Min(FallShotMaximumSeconds, shotAge + dt);
            if (shotAge >= FallShotMaximumSeconds || shotContact && shotAge - shotContactAge >= .8f || shotGhost == null || !shotContact && !shotGhost.active)
            { CancelFallShot(); return false; }
            if (!shotBorrowed)
            {
                // Borrow the existing broadcast camera, never create another
                // camera/listener/input owner or change the selected drive mode.
                priorOrthographic = match.broadcast.view.orthographic; priorBroadcastRect = match.broadcast.view.rect;
                if (match.framing != null)
                {
                    fitEnabled = match.framing.enabled; fitArena = match.framing.arenaFit; match.framing.enabled = false;
                    // Development controlled captures call Apply explicitly;
                    // suppress its arena-position fit as well as its LateUpdate.
                    match.framing.arenaFit = false;
                }
                match.broadcast.view.orthographic = false;
                match.broadcast.view.enabled = true; match.broadcast.view.rect = new Rect(0, 0, 1, 1);
                foreach (var view in match.drivingViews.rally.views) if (view != null) view.view.enabled = false;
                foreach (var cab in match.drivingViews.cabs) if (cab != null) cab.gameObject.SetActive(false);
                match.cameras = new[] { match.broadcast.view };
                if (match.atmosphere != null && match.atmosphere.vendor != null)
                { match.atmosphere.vendor.AllowCameras.Clear(); match.atmosphere.vendor.AllowCameras.Add(match.broadcast.view); }
                shotBorrowed = true;
            }
            if (!shotContact && shotGhost.visual != null)
            { var bottom = Bottom(shotGhost); shotPoint = new Vector3(bottom.x, WaterHeight, bottom.z); }
            // Keep the genuine water contact below the shared centre countdown.
            match.broadcast.Pose(shotPoint + Vector3.up * 2.5f, new Vector3(12, 11, -17), 58, shotSnap); shotSnap = false;
            return true;
        }
        public void CancelFallShot()
        {
            bool restore = shotBorrowed;
            if (shotActive && restore) CompletedFallShots++;
            shotActive = shotBorrowed = shotSnap = shotContact = false; shotGhost = null;
            if (match == null) return;
            if (restore)
            {
                if (match.framing != null) { match.framing.enabled = fitEnabled; match.framing.arenaFit = fitArena; }
                if (match.broadcast != null && match.broadcast.view != null) { match.broadcast.view.orthographic = priorOrthographic; match.broadcast.view.rect = priorBroadcastRect; }
            }
            if (restore && match.isActiveAndEnabled && match.drivingViews != null && match.drivingViews.rally != null) match.drivingViews.Layout();
            // Reacquire the reset truck through the original single authority
            // with a snap, rather than damping across the whole course/logo.
            match.broadcast?.broadcastFraming?.Release();
        }
        public void ClearFallPresentation() { EnsureMatch(); CancelFallShot(); ClearGhosts(); ClearFx(); }
        static Vector3 Bottom(Ghost ghost)
        {
            Vector3 result = new Vector3(0, float.PositiveInfinity, 0);
            foreach (var point in ghost.envelope) { var world = ghost.visual.transform.TransformPoint(point); if (world.y < result.y) result = world; }
            return result;
        }
        void Impact(Ghost ghost, Vector3 bottom)
        {
            LastVisualBottom = bottom.y; ghost.active = false; ghost.visual.SetActive(false);
            if (bottom.x < waterBounds.min.x || bottom.x > waterBounds.max.x || bottom.z < waterBounds.min.z || bottom.z > waterBounds.max.z) return;
            var point = new Vector3(bottom.x, WaterHeight + .035f, bottom.z); LastSplashPoint = point; WaterSplashes++;
            if (ghost == shotGhost) { shotPoint = point; shotContact = true; shotContactAge = shotAge; }
            float normalSpeed = Mathf.Max(0, -ghost.velocity.y);
            float strength = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(1, ghost.mass) * normalSpeed * normalSpeed / 1200), .55f, 2);
            int index = nextSplash++;
            if (splash.Length > 0)
            {
                var particles = splash[index % splash.Length]; particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); particles.transform.position = point; particles.Play(false);
                int count = Mathf.Clamp(Mathf.RoundToInt(70 * strength), 36, 150);
                for (int i = 0; i < count; i++)
                {
                    float angle = i * 2.39996323f, variation = (Mathf.Sin(i * 71.17f) + 1) * .5f;
                    var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    particles.Emit(new ParticleSystem.EmitParams { position = point + radial * (.8f + variation), velocity = radial * (2 + variation * 4) * Mathf.Sqrt(strength) + Vector3.up * (5 + variation * 4) + Vector3.ProjectOnPlane(ghost.velocity, Vector3.up) * .08f, startLifetime = 1.1f + variation, startSize = .12f + variation * .35f }, 1);
                }
            }
            if (rings.Length > 0) { int ring = index % rings.Length; rings[ring].transform.position = point; rings[ring].transform.localScale = Vector3.one; ringBorn[ring] = Time.time; rings[ring].enabled = true; }
        }
        void Update()
        {
            if (match == null) return;
            EnsureMatch();
            if (!match.Running) { CancelFallShot(); ClearGhosts(); ClearFx(); return; }
            if (match.Paused) return;
            TickVisuals(Time.deltaTime); Ambient();
        }
        public void TickVisuals(float dt)
        {
            if (!float.IsFinite(dt) || dt <= 0) return;
            foreach (var ghost in ghosts)
            {
                if (ghost == null || !ghost.active) continue;
                float remaining = Mathf.Min(dt, .2f);
                while (remaining > 0 && ghost.active)
                {
                    float step = Mathf.Min(remaining, 1f / 60); remaining -= step; ghost.age += step;
                    var transform = ghost.visual.transform; Vector3 priorPosition = transform.position; Quaternion priorRotation = transform.rotation;
                    Vector3 position = priorPosition + ghost.velocity * step + Physics.gravity * (.5f * step * step);
                    Quaternion rotation = ghost.angular.sqrMagnitude > .0001f ? Quaternion.AngleAxis(ghost.angular.magnitude * step * Mathf.Rad2Deg, ghost.angular.normalized) * priorRotation : priorRotation;
                    transform.SetPositionAndRotation(position, rotation); ghost.velocity += Physics.gravity * step;
                    var bottom = Bottom(ghost);
                    if (bottom.y <= WaterHeight)
                    {
                        float low = 0, high = 1;
                        for (int i = 0; i < 10; i++) { float middle = (low + high) * .5f; transform.SetPositionAndRotation(Vector3.Lerp(priorPosition, position, middle), Quaternion.Slerp(priorRotation, rotation, middle)); if (Bottom(ghost).y > WaterHeight) low = middle; else high = middle; }
                        transform.SetPositionAndRotation(Vector3.Lerp(priorPosition, position, high), Quaternion.Slerp(priorRotation, rotation, high)); Impact(ghost, Bottom(ghost));
                    }
                    else if (ghost.age > 2.5f) { ghost.active = false; ghost.visual.SetActive(false); }
                }
            }
            for (int i = 0; i < rings.Length; i++)
            {
                var ring = rings[i]; if (ring == null || !ring.enabled) continue; float age = Time.time - ringBorn[i];
                if (age > 1.6f) { ring.enabled = false; continue; }
                ring.transform.localScale = Vector3.one * (1 + age * 5); ring.startColor = ring.endColor = new Color(.75f, .93f, 1, (1 - age / 1.6f) * .65f);
            }
        }
        void Ambient()
        {
            if (match.crowd == null || match.crowd.Mood != "IDLE") return;
            if (restorePose > 0 && Time.time >= restorePose) { for (int i = 0; i < addedPosedFans.Length; i++) if (addedPosedFans[i] != null) addedPosedFans[i].sharedMesh = addedIdle[i]; restorePose = 0; }
            if (Time.time < nextAmbient) return; nextAmbient = Time.time + 7; AmbientGestures++;
            for (int i = 0; i < newFans.Length; i++) if (newFans[i] != null && i % 4 == ambientSerial % 4) { string state = ambientSerial % 3 == 0 ? "Dance" : ambientSerial % 3 == 1 ? "Cheer" : "Idle"; if (newFans[i].HasState(0, Animator.StringToHash(state))) newFans[i].CrossFadeInFixedTime(state, .25f); }
            for (int i = 0; i < addedPosedFans.Length; i++) if (addedPosedFans[i] != null && i % 11 == ambientSerial % 11) addedPosedFans[i].sharedMesh = addedCheer[i];
            restorePose = Time.time + 2; ambientSerial++;
        }
        void EnsureMatch()
        {
            if (matchId == match.MatchId) return;
            CancelFallShot(); ClearGhosts(); ClearFx();
            for (int i = 0; i < ghosts.Length; i++) { if (ghosts[i]?.visual != null) Dispose(ghosts[i].visual); ghosts[i] = null; }
            matchId = match.MatchId; nextAmbient = Time.time + 5; restorePose = 0; CapturedFalls = WaterSplashes = CompletedFallShots = 0;
        }
        void ClearGhosts() { foreach (var ghost in ghosts) if (ghost != null) { ghost.active = false; if (ghost.visual != null) ghost.visual.SetActive(false); } }
        void ClearFx() { foreach (var particles in splash) if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); foreach (var ring in rings) if (ring != null) ring.enabled = false; }
        void OnDisable() { if (subscribed != null) subscribed.AttemptRestarting -= Capture; subscribed = null; CancelFallShot(); if (match != null && match.fallPresentation == this) match.fallPresentation = null; ClearGhosts(); ClearFx(); foreach (var fire in entranceFire) if (fire != null) fire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        void OnDestroy() { foreach (var ghost in ghosts) if (ghost?.visual != null) Dispose(ghost.visual); }
        static void Dispose(GameObject visual) { visual.SetActive(false); if (Application.isPlaying) Destroy(visual); else DestroyImmediate(visual); }
    }
}
