using System;
using System.Collections.Generic;
using NWH.Common.Cameras;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiCabAnchorId
    {
        DriverSeat, FrontPassengerSeat, FrontCenter, SleeperLeft, SleeperRight,
        SleeperRear, SleeperBed, FloorPerch
    }
    [Flags]
    public enum TruckTaxiCabOccupantKind { Driver = 1, Passenger = 2, Companion = 4 }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiCabOccupantRig : MonoBehaviour
    {
        [Serializable]
        public sealed class PosePart
        {
            public string path;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
        }
        [Serializable]
        public sealed class Anchor
        {
            public TruckTaxiCabAnchorId id;
            [Tooltip("Relative to this rig's truck root. Empty uses the root.")]
            public string mountPath = "Cab";
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
            public int priority;
            public TruckTaxiCabOccupantKind allowedOccupants =
                TruckTaxiCabOccupantKind.Passenger | TruckTaxiCabOccupantKind.Companion;
            [Tooltip("Empty accepts every seat profile type.")]
            public TruckTaxiSeatType[] allowedSeatTypes = Array.Empty<TruckTaxiSeatType>();
            public bool firstPersonVisible = true;
            public bool privateEventEligible;
            public PosePart[] pose = Array.Empty<PosePart>();
        }
        private sealed class PoseSnapshot
        {
            public Transform part;
            public Vector3 position;
            public Quaternion rotation;
        }
        private sealed class Placement
        {
            public Anchor anchor;
            public Vector3 worldScale;
            public Transform[] parts;
            public int[] layers;
            public readonly List<PoseSnapshot> poses = new List<PoseSnapshot>();
        }
        private sealed class Transition
        {
            public Transform actor, parent, destinationParent;
            public Vector3 position, scale, startPosition, destinationPosition;
            public Quaternion rotation, startRotation, destinationRotation;
            public Placement originalPlacement;
        }

        [SerializeField] private bool hasSleeperCapability;
        [SerializeField] private Anchor[] anchors = Array.Empty<Anchor>();
        [Min(.1f)] public float privateTransitionSeconds = .9f;
        [Tooltip("Optional existing divider. Its active state is restored on every exit.")]
        public GameObject privacyDivider;
        private readonly Dictionary<Transform, Placement> placements = new Dictionary<Transform, Placement>();
        private readonly Dictionary<Camera, int> cameraMasks = new Dictionary<Camera, int>();
        private readonly List<Transform> stale = new List<Transform>();
        private Transition driverTransition, companionTransition;
        private float transitionTime;
        private bool dividerWasActive;
        public bool HasSleeperCapability => hasSleeperCapability && isActiveAndEnabled;
        public bool PrivateTransitionActive => driverTransition != null;
        public bool PrivateTransitionValid => HasSleeperCapability && PrivateTransitionActive && companionTransition != null &&
            driverTransition.actor != null && companionTransition.actor != null &&
            driverTransition.actor.gameObject.activeInHierarchy && companionTransition.actor.gameObject.activeInHierarchy &&
            driverTransition.destinationParent != null && companionTransition.destinationParent != null &&
            driverTransition.actor.parent == driverTransition.destinationParent &&
            companionTransition.actor.parent == companionTransition.destinationParent;
        public bool PrivateTransitionComplete { get; private set; }
        public IReadOnlyList<Anchor> Anchors => anchors;

        // Prefab tooling and tests set this explicitly; truck names never imply sleeper capability.
        public void Configure(bool sleeperCapable, Anchor[] authoredAnchors)
        {
            EndPrivateTransition();
            ClearPlacements();
            hasSleeperCapability = sleeperCapable;
            anchors = authoredAnchors ?? Array.Empty<Anchor>();
        }

        public bool TrySeat(Transform actor, TruckTaxiCabOccupantKind kind, TruckTaxiCabAnchorId preferred,
            TruckTaxiSeatType seatType, Vector3 worldScale, bool sleeperOnly = false)
        {
            if (!HasSleeperCapability || actor == null || PrivateTransitionActive) return false;
            Prune();
            Anchor anchor = Select(kind, preferred, seatType, actor, sleeperOnly, false, null);
            if (anchor == null) return false;
            Release(actor);
            Place(actor, anchor, worldScale);
            return true;
        }

        public bool TryBeginPrivateTransition(Transform driver, Transform companion, TruckTaxiSeatType companionType)
        {
            if (!HasSleeperCapability || PrivateTransitionActive || driver == null ||
                companion == null || driver == companion || !driver.IsChildOf(transform) ||
                !companion.IsChildOf(transform)) return false;
            Prune();
            Anchor driverSeat = null, companionSeat = null;
            // Reserve a compatible pair before moving either occupant.
            foreach (var candidate in anchors)
            {
                if (!Usable(candidate, TruckTaxiCabOccupantKind.Driver, TruckTaxiSeatType.StandardPassenger,
                    driver, true, true)) continue;
                var other = Select(TruckTaxiCabOccupantKind.Companion, TruckTaxiCabAnchorId.SleeperRight,
                    companionType, companion, true, true, candidate);
                if (other == null || driverSeat != null && candidate.priority <= driverSeat.priority) continue;
                driverSeat = candidate; companionSeat = other;
            }
            if (driverSeat == null || companionSeat == null) return false;
            dividerWasActive = privacyDivider != null && privacyDivider.activeSelf;
            driverTransition = BeginTransition(driver, driverSeat);
            companionTransition = BeginTransition(companion, companionSeat);
            transitionTime = 0;
            PrivateTransitionComplete = false;
            return true;
        }

        public bool StepPrivateTransition(float seconds)
        {
            if (!PrivateTransitionValid) return false;
            if (!float.IsFinite(seconds)) return false;
            transitionTime += Mathf.Max(0, seconds);
            float duration = float.IsFinite(privateTransitionSeconds) ? Mathf.Max(.1f, privateTransitionSeconds) : .9f;
            float t = Mathf.Clamp01(transitionTime / duration);
            float smooth = t * t * (3 - 2 * t);
            Step(driverTransition, smooth);
            Step(companionTransition, smooth);
            PrivateTransitionComplete = t >= 1;
            if (PrivateTransitionComplete && privacyDivider != null) privacyDivider.SetActive(true);
            return PrivateTransitionComplete;
        }

        public void EndPrivateTransition()
        {
            if (!PrivateTransitionActive) return;
            Restore(driverTransition);
            Restore(companionTransition);
            driverTransition = companionTransition = null;
            PrivateTransitionComplete = false;
            if (privacyDivider != null) privacyDivider.SetActive(dividerWasActive);
        }

        private Transition BeginTransition(Transform actor, Anchor destination)
        {
            var state = new Transition { actor = actor, parent = actor.parent, position = actor.localPosition,
                rotation = actor.localRotation, scale = actor.localScale };
            placements.TryGetValue(actor, out state.originalPlacement);
            Vector3 worldPosition = actor.position;
            Vector3 worldScale = state.originalPlacement != null ? state.originalPlacement.worldScale : actor.lossyScale;
            Quaternion worldRotation = actor.rotation;
            Release(actor);
            Place(actor, destination, worldScale);
            state.destinationParent = actor.parent;
            state.destinationPosition = actor.localPosition;
            state.destinationRotation = actor.localRotation;
            state.startPosition = actor.parent.InverseTransformPoint(worldPosition);
            state.startRotation = Quaternion.Inverse(actor.parent.rotation) * worldRotation;
            actor.localPosition = state.startPosition;
            actor.localRotation = state.startRotation;
            return state;
        }

        private static void Step(Transition state, float t)
        {
            if (state?.actor == null || state.destinationParent == null) return;
            state.actor.localPosition = Vector3.Lerp(state.startPosition, state.destinationPosition, t);
            state.actor.localRotation = Quaternion.Slerp(state.startRotation, state.destinationRotation, t);
        }

        private void Restore(Transition state)
        {
            if (state?.actor == null) return;
            Release(state.actor);
            state.actor.SetParent(state.parent, false);
            state.actor.localPosition = state.position;
            state.actor.localRotation = state.rotation;
            state.actor.localScale = state.scale;
            if (state.originalPlacement != null)
            {
                ApplyVisibility(state.originalPlacement, state.originalPlacement.anchor.firstPersonVisible);
                ApplyPose(state.actor, state.originalPlacement);
                placements[state.actor] = state.originalPlacement;
            }
        }

        private Anchor Select(TruckTaxiCabOccupantKind kind, TruckTaxiCabAnchorId preferred,
            TruckTaxiSeatType type, Transform actor, bool sleeperOnly, bool privateOnly, Anchor excluded)
        {
            Anchor best = null;
            foreach (var anchor in anchors)
            {
                if (anchor == null || excluded != null && anchor.id == excluded.id ||
                    !Usable(anchor, kind, type, actor, sleeperOnly, privateOnly)) continue;
                if (best == null || anchor.id == preferred && best.id != preferred ||
                    (anchor.id == preferred) == (best.id == preferred) && anchor.priority > best.priority) best = anchor;
            }
            return best;
        }

        private bool Usable(Anchor anchor, TruckTaxiCabOccupantKind kind, TruckTaxiSeatType type,
            Transform actor, bool sleeperOnly, bool privateOnly)
        {
            if (anchor == null || (anchor.allowedOccupants & kind) == 0 ||
                sleeperOnly && !IsSleeper(anchor.id) || privateOnly && !anchor.privateEventEligible ||
                !string.IsNullOrWhiteSpace(anchor.mountPath) && transform.Find(anchor.mountPath) == null ||
                anchor.allowedSeatTypes != null && anchor.allowedSeatTypes.Length > 0 &&
                Array.IndexOf(anchor.allowedSeatTypes, type) < 0) return false;
            foreach (var entry in placements)
                if (entry.Key != actor && entry.Key != null && entry.Key.IsChildOf(transform) &&
                    entry.Value.anchor.id == anchor.id) return false;
            return true;
        }

        private void Place(Transform actor, Anchor anchor, Vector3 worldScale)
        {
            Transform mount = string.IsNullOrWhiteSpace(anchor.mountPath) ? transform : transform.Find(anchor.mountPath);
            var state = new Placement { anchor = anchor, worldScale = worldScale,
                parts = actor.GetComponentsInChildren<Transform>(true) };
            state.layers = new int[state.parts.Length];
            for (int i = 0; i < state.parts.Length; i++) state.layers[i] = state.parts[i].gameObject.layer;
            actor.SetParent(mount, false);
            actor.localPosition = anchor.localPosition;
            actor.localRotation = Quaternion.Euler(anchor.localEulerAngles);
            Vector3 scale = mount.lossyScale;
            actor.localScale = new Vector3(worldScale.x * Mathf.Max(.01f, anchor.localScale.x) / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
                worldScale.y * Mathf.Max(.01f, anchor.localScale.y) / Mathf.Max(.0001f, Mathf.Abs(scale.y)),
                worldScale.z * Mathf.Max(.01f, anchor.localScale.z) / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
            foreach (var body in actor.GetComponentsInChildren<Rigidbody>(true))
            {
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = true;
            }
            foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            EnsureCameraMasks();
            ApplyVisibility(state, anchor.firstPersonVisible);
            ApplyPose(actor, state);
            placements[actor] = state;
        }

        private static void ApplyPose(Transform actor, Placement state)
        {
            state.poses.Clear();
            foreach (var pose in state.anchor.pose ?? Array.Empty<PosePart>())
            {
                if (pose == null || string.IsNullOrWhiteSpace(pose.path)) continue;
                var part = actor.Find(pose.path);
                if (part == null) continue;
                state.poses.Add(new PoseSnapshot { part = part, position = part.localPosition, rotation = part.localRotation });
                part.localPosition = pose.localPosition;
                part.localRotation = Quaternion.Euler(pose.localEulerAngles);
            }
        }

        private static void ApplyVisibility(Placement state, bool visible)
        {
            for (int i = 0; i < state.parts.Length; i++)
                if (state.parts[i] != null) state.parts[i].gameObject.layer =
                    visible ? (state.layers[i] == 29 ? 0 : state.layers[i]) : 29;
        }

        public void Release(Transform actor)
        {
            if (ReferenceEquals(actor, null) || !placements.TryGetValue(actor, out var state)) return;
            for (int i = 0; i < state.parts.Length; i++)
                if (state.parts[i] != null) state.parts[i].gameObject.layer = state.layers[i];
            foreach (var pose in state.poses)
                if (pose.part != null) { pose.part.localPosition = pose.position; pose.part.localRotation = pose.rotation; }
            placements.Remove(actor);
            // PassengerActor owns exit/ejection physics; releasing a seat never re-enables a ragdoll.
        }

        private void EnsureCameraMasks()
        {
            foreach (var inside in GetComponentsInChildren<CameraInsideVehicle>(true))
            {
                if (!inside.isInsideVehicle) continue;
                var camera = inside.GetComponent<Camera>() ?? inside.GetComponentInChildren<Camera>(true);
                if (camera == null || cameraMasks.ContainsKey(camera)) continue;
                cameraMasks[camera] = camera.cullingMask;
                camera.cullingMask &= ~(1 << 29);
            }
        }

        public static bool IsSleeper(TruckTaxiCabAnchorId id) => id >= TruckTaxiCabAnchorId.SleeperLeft;
        private void Prune()
        {
            stale.Clear();
            foreach (var entry in placements)
                if (entry.Key == null || !entry.Key.IsChildOf(transform) || !entry.Key.gameObject.activeInHierarchy)
                    stale.Add(entry.Key);
            foreach (var actor in stale) Release(actor);
        }
        private void ClearPlacements()
        {
            stale.Clear();
            foreach (var actor in placements.Keys) stale.Add(actor);
            foreach (var actor in stale) Release(actor);
        }
        private void LateUpdate() => Prune();
        private void OnDisable() => EndPrivateTransition();
        private void OnDestroy()
        {
            EndPrivateTransition();
            ClearPlacements();
            foreach (var entry in cameraMasks) if (entry.Key != null) entry.Key.cullingMask = entry.Value;
        }
    }
}
