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
            [Tooltip("Explicit extra travel seat. A shared front passenger seat must opt in; never the driver's seat.")]
            public bool secondaryOccupantSeat;
            [Tooltip("Explicitly allows a secondary role to use the primary passenger seat while it is free.")]
            public bool sharedPrimarySeat;
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
            public TruckTaxiCabOccupantKind kind;
            public string stableId;
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
        private readonly Dictionary<Transform, Placement> reservations = new Dictionary<Transform, Placement>();
        private readonly HashSet<Transform> releasedVisuals = new HashSet<Transform>();
        private readonly Dictionary<Camera, int> cameraMasks = new Dictionary<Camera, int>();
        private readonly List<Transform> stale = new List<Transform>();
        private Transition driverTransition, companionTransition;
        private float transitionTime;
        private bool dividerWasActive;
        public bool HasSleeperCapability => hasSleeperCapability && isActiveAndEnabled;
        public bool AuthoredSleeperCapability => hasSleeperCapability;
        public bool HasAuthoredPrimarySeat
        {
            get
            {
                if(!isActiveAndEnabled) return false;
                foreach(var anchor in anchors)
                    if(ValidAnchor(anchor) && anchor.id==TruckTaxiCabAnchorId.FrontPassengerSeat) return true;
                return false;
            }
        }
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
        // The player always retains a place, including while walking. Legacy driver presentation
        // does not call Place, so counting only placements incorrectly permits a ninth person.
        public int UsedCapacity
        {
            get
            {
                Prune();
                int count=placements.Count+reservations.Count;
                bool driver=false;
                foreach(var placement in placements.Values) driver |= placement.kind==TruckTaxiCabOccupantKind.Driver;
                if(!driver) count++;
                foreach(var actor in GetComponentsInChildren<TruckTaxiPassengerActor>(true))
                    if(IsLegacyOccupant(actor)) count++;
                return count;
            }
        }
        public bool SetOccupantIdentity(Transform actor,string stableId)
        {
            if(actor==null || string.IsNullOrWhiteSpace(stableId) || !placements.TryGetValue(actor,out var placement)) return false;
            placement.stableId=stableId.Trim();
            return true;
        }
        public void CopyOccupancySnapshot(List<TruckTaxiOccupantSlotSnapshot> output)
        {
            if(output==null) throw new ArgumentNullException(nameof(output));
            Prune(); output.Clear();
            bool driverSlot=false;
            foreach(var anchor in anchors)
            {
                if(!ValidAnchor(anchor)) continue;
                Placement occupant=null;
                Transform actor=null;
                foreach(var entry in placements)
                    if(entry.Value.anchor==anchor) { occupant=entry.Value; actor=entry.Key; break; }
                bool reserved=false;
                if(occupant==null) foreach(var entry in reservations)
                    if(entry.Value.anchor==anchor) { occupant=entry.Value; actor=entry.Key; reserved=true; break; }
                bool isDriver=anchor.id==TruckTaxiCabAnchorId.DriverSeat &&
                    (anchor.allowedOccupants & TruckTaxiCabOccupantKind.Driver)!=0;
                driverSlot |= isDriver;
                var passenger=actor!=null ? actor.GetComponent<TruckTaxiPassengerActor>() : null;
                var state=occupant==null ? (isDriver ? TruckTaxiOccupantSlotState.Reserved : TruckTaxiOccupantSlotState.Free) :
                    reserved || passenger!=null && passenger.Lifecycle==TruckTaxiActorLifecycle.Boarding ? TruckTaxiOccupantSlotState.Reserved :
                    TruckTaxiOccupantSlotState.Occupied;
                output.Add(new TruckTaxiOccupantSlotSnapshot(anchor.StableSlotId,
                    occupant?.kind ?? (isDriver ? TruckTaxiCabOccupantKind.Driver : 0),
                    occupant?.stableId ?? (isDriver ? "player" : occupant!=null ? "<unbound>" : ""),state));
            }
            if(!driverSlot) output.Add(new TruckTaxiOccupantSlotSnapshot("driver.legacy",TruckTaxiCabOccupantKind.Driver,
                "player",TruckTaxiOccupantSlotState.Reserved));
            foreach(var actor in GetComponentsInChildren<TruckTaxiPassengerActor>(true))
                if(IsLegacyOccupant(actor)) output.Add(new TruckTaxiOccupantSlotSnapshot("legacy.visual."+actor.GetInstanceID(),
                    TruckTaxiCabOccupantKind.Passenger,"<unbound>",actor.Lifecycle==TruckTaxiActorLifecycle.Boarding ?
                        TruckTaxiOccupantSlotState.Reserved : TruckTaxiOccupantSlotState.Occupied));
        }
        private bool IsLegacyOccupant(TruckTaxiPassengerActor actor) => actor!=null &&
            !placements.ContainsKey(actor.transform) && !reservations.ContainsKey(actor.transform) &&
            !releasedVisuals.Contains(actor.transform) &&
            (actor.Lifecycle==TruckTaxiActorLifecycle.Seated || actor.Lifecycle==TruckTaxiActorLifecycle.Boarding);
        public bool IsOccupied(string slotId)
        {
            Prune();
            foreach (var placement in placements.Values)
                if (placement.anchor.StableSlotId == slotId) return true;
            return false;
        }
        public bool TryReserveSecondary(Transform actor,TruckTaxiSeatType type,TruckTaxiCabOccupantKind kind,string stableId)
        {
            if(actor==null || string.IsNullOrWhiteSpace(stableId)) return false;
            Prune();
            if(placements.ContainsKey(actor)) return false;
            if(reservations.TryGetValue(actor,out var existing))
                return existing.kind==kind && existing.stableId==stableId;
            var anchor=SelectSecondary(type,actor,kind);
            if(anchor==null) return false;
            reservations.Add(actor,new Placement { anchor=anchor,kind=kind,stableId=stableId });
            return true;
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
            if (!isActiveAndEnabled || sleeperOnly && !HasSleeperCapability || actor == null || PrivateTransitionActive) return false;
            Prune();
            Anchor anchor = Select(kind, preferred, seatType, actor, sleeperOnly, false, null);
            if (anchor == null) return false;
            Release(actor);
            Place(actor, anchor, worldScale, kind);
            return true;
        }

        public bool HasSeat(Transform actor, TruckTaxiCabOccupantKind kind, TruckTaxiCabAnchorId preferred,
            TruckTaxiSeatType seatType)
        {
            if (!isActiveAndEnabled || actor == null || PrivateTransitionActive) return false;
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
            Place(first.transform, a, first.StandingWorldScale, TruckTaxiCabOccupantKind.Passenger);
            Place(second.transform, b, second.StandingWorldScale, TruckTaxiCabOccupantKind.Passenger);
            first.CompleteSeating();
            second.CompleteSeating();
            return true;
        }

        private bool SelectPassengerPair(Transform first, TruckTaxiSeatProfile firstProfile,
            Transform second, TruckTaxiSeatProfile secondProfile, out Anchor a, out Anchor b)
        {
            a = b = null;
            if (!isActiveAndEnabled || PrivateTransitionActive || first == null || second == null || first == second)
                return false;
            Prune();
            if(UsedCapacity+(placements.ContainsKey(first) || reservations.ContainsKey(first) ? 0 : 1)+
                (placements.ContainsKey(second) || reservations.ContainsKey(second) ? 0 : 1)>MaximumOccupants)
                return false;
            var preferred = firstProfile != null ? firstProfile.primaryCabAnchor : TruckTaxiCabAnchorId.FrontPassengerSeat;
            foreach (var candidate in anchors)
            {
                if (!Usable(candidate, TruckTaxiCabOccupantKind.Passenger,
                    firstProfile != null ? firstProfile.seatType : TruckTaxiSeatType.StandardPassenger, first, false, false) ||
                    candidate.secondaryOccupantSeat && !HasTravelClearance(candidate, first)) continue;
                foreach (var extra in anchors)
                {
                    if (extra == null || extra == candidate || !extra.secondaryOccupantSeat ||
                        extra.id == TruckTaxiCabAnchorId.DriverSeat ||
                        extra.id == TruckTaxiCabAnchorId.FrontPassengerSeat && !extra.sharedPrimarySeat ||
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
            Place(actor, anchor, worldScale, kind);
            return true;
        }

        private Anchor SelectSecondary(TruckTaxiSeatType type, Transform actor, TruckTaxiCabOccupantKind kind)
        {
            if (!isActiveAndEnabled || PrivateTransitionActive) return null;
            Anchor best = null;
            foreach (var anchor in anchors)
            {
                if (anchor == null || !anchor.secondaryOccupantSeat ||
                    anchor.id == TruckTaxiCabAnchorId.DriverSeat ||
                    anchor.id == TruckTaxiCabAnchorId.FrontPassengerSeat && !anchor.sharedPrimarySeat ||
                    !Usable(anchor, kind, type, actor, false, false)) continue;
                if (!HasTravelClearance(anchor, actor)) continue;
                // Keep shared seats available for fares when this companion has an
                // authored exclusive travel seat. All compatibility/lease checks still apply.
                bool exclusive = kind == TruckTaxiCabOccupantKind.Companion &&
                    anchor.allowedOccupants == TruckTaxiCabOccupantKind.Companion;
                bool bestExclusive = kind == TruckTaxiCabOccupantKind.Companion && best != null &&
                    best.allowedOccupants == TruckTaxiCabOccupantKind.Companion;
                if (best == null || exclusive && !bestExclusive ||
                    exclusive == bestExclusive && anchor.priority > best.priority) best = anchor;
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
            foreach(var entry in reservations)
                if(entry.Key!=actor && entry.Key!=null && Vector3.Distance(position,AnchorPosition(entry.Value.anchor))<
                    radius+Radius(entry.Value.anchor,entry.Key)) return false;
            foreach (var passenger in GetComponentsInChildren<TruckTaxiPassengerActor>(true))
                if (!HasLeftSeat(passenger) && !releasedVisuals.Contains(passenger.transform) &&
                    passenger.transform != actor && !placements.ContainsKey(passenger.transform) &&
                    Vector3.Distance(position, passenger.transform.position) <
                    radius + SeatedRadius(passenger)) return false;
            var driver = GetComponent<TruckTaxiDriverPresentation>()?.Occupant;
            foreach(var authored in anchors)
                if(ValidAnchor(authored) && authored.id==TruckTaxiCabAnchorId.DriverSeat) return true;
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
            Place(actor, destination, worldScale, state.originalPlacement?.kind ??
                (driverTransition == null ? TruckTaxiCabOccupantKind.Driver : TruckTaxiCabOccupantKind.Companion));
            placements[actor].stableId = state.originalPlacement?.stableId;
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
            if(kind!=TruckTaxiCabOccupantKind.Driver &&
                (actor==null || !placements.ContainsKey(actor) && !reservations.ContainsKey(actor)) && UsedCapacity>=MaximumOccupants)
                return false;
            if (!ValidAnchor(anchor) || anchor.id==TruckTaxiCabAnchorId.DriverSeat && kind!=TruckTaxiCabOccupantKind.Driver ||
                (anchor.allowedOccupants & kind) != kind || kind == 0 ||
                sleeperOnly && !IsSleeper(anchor.id) || privateOnly && !anchor.privateEventEligible ||
                !string.IsNullOrWhiteSpace(anchor.mountPath) && transform.Find(anchor.mountPath) == null ||
                anchor.allowedSeatTypes != null && anchor.allowedSeatTypes.Length > 0 &&
                Array.IndexOf(anchor.allowedSeatTypes, type) < 0) return false;
            foreach (var entry in placements)
                if (entry.Key != actor && entry.Key != null && entry.Key.IsChildOf(transform) &&
                    entry.Value.anchor.StableSlotId == anchor.StableSlotId) return false;
            foreach(var entry in reservations)
                if(entry.Key!=actor && entry.Value.anchor.StableSlotId==anchor.StableSlotId) return false;
            if(actor!=null && reservations.TryGetValue(actor,out var reservation) && reservation.anchor!=anchor) return false;
            return true;
        }

        private void Place(Transform actor, Anchor anchor, Vector3 worldScale, TruckTaxiCabOccupantKind kind)
        {
            releasedVisuals.Remove(actor);
            Transform mount = string.IsNullOrWhiteSpace(anchor.mountPath) ? transform : transform.Find(anchor.mountPath);
            var state = new Placement { anchor = anchor, kind = kind, worldScale = worldScale,
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
            if(!ReferenceEquals(actor,null)) reservations.Remove(actor);
            if (ReferenceEquals(actor, null) || !placements.TryGetValue(actor, out var state)) return;
            if(actor!=null) releasedVisuals.Add(actor);
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
        private static bool HasLeftSeat(TruckTaxiPassengerActor actor) => actor != null &&
            (actor.IsTumbling || actor.Lifecycle == TruckTaxiActorLifecycle.Exiting ||
             actor.Lifecycle == TruckTaxiActorLifecycle.WalkingToContact ||
             actor.Lifecycle == TruckTaxiActorLifecycle.SketchyInteraction ||
             actor.Lifecycle == TruckTaxiActorLifecycle.Returning);
        private void Prune()
        {
            stale.Clear();
            foreach(var entry in reservations)
                if(entry.Key==null || !entry.Key.gameObject.activeSelf ||
                    HasLeftSeat(entry.Key.GetComponent<TruckTaxiPassengerActor>())) stale.Add(entry.Key);
            foreach(var actor in stale) reservations.Remove(actor);
            stale.Clear();
            foreach (var entry in placements)
                if (entry.Key == null || !entry.Key.IsChildOf(transform) ||
                    HasLeftSeat(entry.Key.GetComponent<TruckTaxiPassengerActor>()))
                    stale.Add(entry.Key);
            foreach (var actor in stale) Release(actor);
            releasedVisuals.RemoveWhere(IsDetachedVisual);
        }
        private bool IsDetachedVisual(Transform actor) => actor==null || !actor.IsChildOf(transform);
        private void ClearPlacements()
        {
            reservations.Clear();
            stale.Clear();
            foreach (var actor in placements.Keys) stale.Add(actor);
            foreach (var actor in stale) Release(actor);
            releasedVisuals.Clear();
        }
        private void LateUpdate() => Prune();
        private void OnDisable() { EndPrivateTransition(); reservations.Clear(); }
        private void OnDestroy()
        {
            EndPrivateTransition();
            ClearPlacements();
            foreach (var entry in cameraMasks) if (entry.Key != null) entry.Key.cullingMask = entry.Value;
        }
    }
}
