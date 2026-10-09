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
    public enum TruckTaxiCabOccupantKind
    {
        Driver = 1, Passenger = 2, Companion = 4, StolenPassenger = 8,
        BonusObjectiveCompanion = 16, ExtraPassenger = 32
    }

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
            [Tooltip("Stable authored identity. Empty preserves the legacy anchor enum identity.")]
            public string slotId;
            public string StableSlotId => string.IsNullOrWhiteSpace(slotId) ? id.ToString() : slotId.Trim();
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
            [Tooltip("Explicitly validated extra travel seat; never the driver's or primary fare seat.")]
            public bool secondaryOccupantSeat;
            [Min(.3f)] public float occupantRadius = .65f;
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
        public const int MaximumOccupants = 8;
        public int AuthoredCapacity
        {
            get
            {
                int count = 0;
                foreach (var anchor in anchors) if (ValidAnchor(anchor)) count++;
                return count;
            }
        }
        public int OccupiedCount { get { Prune(); return placements.Count; } }
        public bool IsOccupied(string slotId)
        {
            Prune();
            foreach (var placement in placements.Values)
                if (placement.anchor.StableSlotId == slotId) return true;
            return false;
        }
        public bool TryGetSlot(Transform actor, out string slotId)
        {
            Prune();
            slotId = null;
            if (actor == null || !placements.TryGetValue(actor, out var placement)) return false;
            slotId = placement.anchor.StableSlotId;
            return true;
        }

        private bool ValidAnchor(Anchor anchor)
        {
            if (anchor == null || anchor.allowedOccupants == 0) return false;
            int index = Array.IndexOf(anchors, anchor);
            if (index < 0 || index >= MaximumOccupants ||
                !string.IsNullOrWhiteSpace(anchor.mountPath) && transform.Find(anchor.mountPath) == null) return false;
            // Ambiguous identities fail closed rather than advertising phantom capacity.
            for (int i = 0; i < anchors.Length; i++)
                if (i != index && anchors[i] != null && anchors[i].StableSlotId == anchor.StableSlotId) return false;
            return true;
        }

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

        public bool HasSeat(Transform actor, TruckTaxiCabOccupantKind kind, TruckTaxiCabAnchorId preferred,
            TruckTaxiSeatType seatType)
        {
            if (!HasSleeperCapability || actor == null || PrivateTransitionActive) return false;
            Prune();
            return Select(kind, preferred, seatType, actor, false, false, null) != null;
        }

        public bool HasSecondarySeat(TruckTaxiSeatType type, Transform actor = null,
            TruckTaxiCabOccupantKind kind = TruckTaxiCabOccupantKind.Companion)
        {
            Prune();
            return SelectSecondary(type, actor, kind) != null;
        }

        public bool CanSeatPassengerPair(Transform first, TruckTaxiSeatProfile firstProfile,
            Transform second, TruckTaxiSeatProfile secondProfile) =>
            SelectPassengerPair(first, firstProfile, second, secondProfile, out _, out _);

        public bool TrySeatPassengerPair(TruckTaxiPassengerActor first, TruckTaxiSeatProfile firstProfile,
            TruckTaxiPassengerActor second, TruckTaxiSeatProfile secondProfile)
        {
            if (first == null || second == null || !SelectPassengerPair(first.transform, firstProfile,
                second.transform, secondProfile, out var a, out var b)) return false;
            first.PrepareForSeat();
            second.PrepareForSeat();
            Release(first.transform);
            Release(second.transform);
            Place(first.transform, a, first.StandingWorldScale);
            Place(second.transform, b, second.StandingWorldScale);
            first.CompleteSeating();
            second.CompleteSeating();
            return true;
        }

        private bool SelectPassengerPair(Transform first, TruckTaxiSeatProfile firstProfile,
            Transform second, TruckTaxiSeatProfile secondProfile, out Anchor a, out Anchor b)
        {
            a = b = null;
            if (!HasSleeperCapability || PrivateTransitionActive || first == null || second == null || first == second)
                return false;
            Prune();
            var preferred = firstProfile != null ? firstProfile.primaryCabAnchor : TruckTaxiCabAnchorId.FrontPassengerSeat;
            foreach (var candidate in anchors)
            {
                if (!Usable(candidate, TruckTaxiCabOccupantKind.Passenger,
                    firstProfile != null ? firstProfile.seatType : TruckTaxiSeatType.StandardPassenger, first, false, false) ||
                    candidate.secondaryOccupantSeat && !HasTravelClearance(candidate, first)) continue;
                foreach (var extra in anchors)
                {
                    if (extra == null || extra == candidate || !extra.secondaryOccupantSeat ||
                        extra.id == TruckTaxiCabAnchorId.DriverSeat || extra.id == TruckTaxiCabAnchorId.FrontPassengerSeat ||
                        !Usable(extra, TruckTaxiCabOccupantKind.Passenger,
                            secondProfile != null ? secondProfile.seatType : TruckTaxiSeatType.StandardPassenger, second, false, false) ||
                        !HasTravelClearance(extra, second) ||
                        Vector3.Distance(AnchorPosition(candidate), AnchorPosition(extra)) < Radius(candidate, first) + Radius(extra, second))
                        continue;
                    if (a == null || candidate.id == preferred && a.id != preferred ||
                        (candidate.id == preferred) == (a.id == preferred) && candidate.priority > a.priority)
                    { a = candidate; b = extra; }
                }
            }
            return a != null;
        }

        public bool TrySeatSecondary(Transform actor, TruckTaxiSeatType type, Vector3 worldScale,
            TruckTaxiCabOccupantKind kind = TruckTaxiCabOccupantKind.Companion)
        {
            if (actor == null) return false;
            Prune();
            var anchor = SelectSecondary(type, actor, kind);
            if (anchor == null) return false;
            Release(actor);
            Place(actor, anchor, worldScale);
            return true;
        }

        private Anchor SelectSecondary(TruckTaxiSeatType type, Transform actor, TruckTaxiCabOccupantKind kind)
        {
            if (!isActiveAndEnabled || PrivateTransitionActive) return null;
            Anchor best = null;
            foreach (var anchor in anchors)
            {
                if (anchor == null || !anchor.secondaryOccupantSeat ||
                    anchor.id == TruckTaxiCabAnchorId.DriverSeat || anchor.id == TruckTaxiCabAnchorId.FrontPassengerSeat ||
                    !Usable(anchor, kind, type, actor, false, false)) continue;
                if (HasTravelClearance(anchor, actor) && (best == null || anchor.priority > best.priority)) best = anchor;
            }
            return best;
        }

        private float Radius(Anchor anchor, Transform actor)
        {
            var passenger = actor != null ? actor.GetComponent<TruckTaxiPassengerActor>() : null;
            return passenger == null ? anchor.occupantRadius : Mathf.Max(anchor.occupantRadius,
                passenger.StandingRadius * Mathf.Max(Mathf.Abs(anchor.localScale.x), Mathf.Abs(anchor.localScale.z)));
        }

        private bool HasTravelClearance(Anchor anchor, Transform actor)
        {
            Vector3 position = AnchorPosition(anchor);
            float radius = Radius(anchor, actor);
            // Preserve the primary seats even while a fare is temporarily outside.
            foreach (var other in anchors)
                if (ValidAnchor(other) && other != anchor &&
                    (other.id == TruckTaxiCabAnchorId.DriverSeat || other.id == TruckTaxiCabAnchorId.FrontPassengerSeat) &&
                    Vector3.Distance(position, AnchorPosition(other)) < radius + other.occupantRadius) return false;
            foreach (var entry in placements)
                if (entry.Key != actor && entry.Key != null &&
                    Vector3.Distance(position, AnchorPosition(entry.Value.anchor)) <
                    radius + Radius(entry.Value.anchor, entry.Key)) return false;
            foreach (var passenger in GetComponentsInChildren<TruckTaxiPassengerActor>(true))
                if (passenger.transform != actor && Vector3.Distance(position, passenger.transform.position) <
                    radius + SeatedRadius(passenger)) return false;
            var driver = GetComponent<TruckTaxiDriverPresentation>()?.Occupant;
            return driver == null || driver == actor || Vector3.Distance(position, driver.position) >= radius + .65f;
        }

        internal static float SeatedRadius(TruckTaxiPassengerActor actor)
        {
            Vector3 standing = actor.StandingWorldScale;
            Vector3 seated = actor.transform.lossyScale;
            float scale = Mathf.Max(Mathf.Abs(seated.x) / Mathf.Max(.0001f, Mathf.Abs(standing.x)),
                Mathf.Abs(seated.z) / Mathf.Max(.0001f, Mathf.Abs(standing.z)));
            return actor.StandingRadius * scale;
        }

        private Vector3 AnchorPosition(Anchor anchor)
        {
            var mount = string.IsNullOrWhiteSpace(anchor.mountPath) ? transform : transform.Find(anchor.mountPath);
            return mount != null ? mount.TransformPoint(anchor.localPosition) : transform.position;
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
                if (anchor == null || excluded != null && anchor.StableSlotId == excluded.StableSlotId ||
                    !Usable(anchor, kind, type, actor, sleeperOnly, privateOnly)) continue;
                if (!privateOnly && anchor.secondaryOccupantSeat && !HasTravelClearance(anchor, actor)) continue;
                if (best == null || anchor.id == preferred && best.id != preferred ||
                    (anchor.id == preferred) == (best.id == preferred) && anchor.priority > best.priority) best = anchor;
            }
            return best;
        }

        private bool Usable(Anchor anchor, TruckTaxiCabOccupantKind kind, TruckTaxiSeatType type,
            Transform actor, bool sleeperOnly, bool privateOnly)
        {
            if (!ValidAnchor(anchor) || (anchor.allowedOccupants & kind) != kind || kind == 0 ||
                sleeperOnly && !IsSleeper(anchor.id) || privateOnly && !anchor.privateEventEligible ||
                !string.IsNullOrWhiteSpace(anchor.mountPath) && transform.Find(anchor.mountPath) == null ||
                anchor.allowedSeatTypes != null && anchor.allowedSeatTypes.Length > 0 &&
                Array.IndexOf(anchor.allowedSeatTypes, type) < 0) return false;
            foreach (var entry in placements)
                if (entry.Key != actor && entry.Key != null && entry.Key.IsChildOf(transform) &&
                    entry.Value.anchor.StableSlotId == anchor.StableSlotId) return false;
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
                if (entry.Key == null || !entry.Key.IsChildOf(transform))
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
