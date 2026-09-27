using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LWS.TruckTaxi
{
    [DefaultExecutionOrder(75), DisallowMultipleComponent]
    public sealed class TruckTaxiDriverNeedsCoordinator : MonoBehaviour
    {
        public TruckTaxiDriverNeedsState State { get; private set; }
        public IReadOnlyList<TruckTaxiBathroomPoint> Bathrooms => bathrooms;
        public TruckTaxiBathroomPoint RoutedBathroom { get; private set; }
        public TruckTaxiBathroomPoint NearbyBathroom { get; private set; }
        public TruckTaxiStorePoint NearbyStore { get; private set; }
        public TruckTaxiServicePoint NearbyService { get; private set; }
        public TruckTaxiServicePoint NearbyRestaurant
        {
            get
            {
                var point = ServiceInReach(TruckTaxiServiceCapability.Food);
                return point != null && point.location != null &&
                    point.location.locationType == TaxiLocationType.Restaurant ? point : null;
            }
        }
        public TruckTaxiServicePoint NearbyRepair => ServiceInReach(TruckTaxiServiceCapability.RepairGeneralDamage);
        public InputAction ThrowContainerAction { get; private set; }
        public IReadOnlyList<TruckTaxiStorePoint> Stores => stores;
        public string Feedback { get; private set; } = "";
        public InputAction JugHoldAction { get; private set; }
        public InputAction JugCueAction { get; private set; }
        public bool JugActive => State?.JugActive == true;
        public bool LoopAudioActive => JugActive && host?.Paused != true && liquidLoop?.IsPlaying == true;
        public bool UsingGeneratedLiquidAudio => liquidLoop?.UsesGeneratedClip == true;
        public bool CanInteract => host != null && host.Ready && !host.Paused &&
            (host.Session.State == TruckTaxiState.Available || host.Session.State == TruckTaxiState.DrivingToPickup || host.Session.State == TruckTaxiState.DrivingToDestination);
        public bool IsMoving => body != null && body.linearVelocity.magnitude > settings.bathroomMaximumSpeed;
        public bool ProperlyStopped => ServiceInReach(TruckTaxiServiceCapability.Restroom) != null;
        public event Action<TruckTaxiDriverNeedEvent> DriverNeedsEvent;
        private TruckTaxiBootstrap host;
        private TruckTaxiEnvironmentCoordinator environment;
        private TruckTaxiEnvironmentSettings settings;
        private Rigidbody body;
        private LwsKeyboardGamepadTruckInputSource[] indicatorSources = Array.Empty<LwsKeyboardGamepadTruckInputSource>();
        private TruckTaxiBathroomPoint[] bathrooms = Array.Empty<TruckTaxiBathroomPoint>();
        private TruckTaxiStorePoint[] stores = Array.Empty<TruckTaxiStorePoint>();
        private TruckTaxiCabDecorations decorations;
        private Transform throwAnchor;
        private TruckTaxiJugLiquidLoop liquidLoop;
        private bool liquidWorldRouted;
        [SerializeField] private AudioClip jugLiquidClip;
        [SerializeField] private AudioClip containerImpactClip;
        public void ConfigureAudio(AudioClip liquid, AudioClip impact)
        { jugLiquidClip = liquid; containerImpactClip = impact; liquidLoop?.Configure(liquid); }
        private double lastGameSeconds;
        private float nextProximityCheck;
        private Vector3 previousVelocity;
        private readonly HashSet<string> scenicAwards = new HashSet<string>(StringComparer.Ordinal);
        private TruckTaxiState previousSessionState;

        public bool Initialize(TruckTaxiBootstrap owner, TruckTaxiEnvironmentCoordinator context)
        {
            if (State != null) return true;
            if (owner?.Player == null || context?.IsInitialized != true || owner.hud?.UIInput == null) return false;
            host = owner; environment = context; settings = context.settings;
            body = host.Player.GetComponent<Rigidbody>();
            if (body == null) return false;
            indicatorSources = owner.Player.GetComponentsInChildren<LwsKeyboardGamepadTruckInputSource>(true);
            State = new TruckTaxiDriverNeedsState(settings, 20925);
            host.Handling?.BindEffects(State.Effects);
            (GetComponent<TruckTaxiEffectPresentation>() ?? gameObject.AddComponent<TruckTaxiEffectPresentation>()).Initialize(State.Effects);
            TruckTaxiServicePoint.BindExistingPoints(owner.gameObject.scene);
            if (owner.gameObject.scene.name == TruckTaxiMainMenu.FreePlaySceneName) State.GrantFreePlayTestInventory();
            State.Event += OnJugInputEvent;
            State.Event += OnNeedEvent;
            var found = FindObjectsByType<TruckTaxiBathroomPoint>(FindObjectsSortMode.None);
            var local = new List<TruckTaxiBathroomPoint>();
            foreach (var point in found) if (point.gameObject.scene == owner.gameObject.scene) local.Add(point);
            local.Sort((a, b) => string.CompareOrdinal(a.stableId, b.stableId)); bathrooms = local.ToArray();
            var foundStores = FindObjectsByType<TruckTaxiStorePoint>(FindObjectsSortMode.None);
            var localStores = new List<TruckTaxiStorePoint>();
            foreach (var point in foundStores) if (point.gameObject.scene == owner.gameObject.scene) localStores.Add(point);
            localStores.Sort((a, b) => string.CompareOrdinal(a.stableId, b.stableId)); stores = localStores.ToArray();
            decorations = new TruckTaxiCabDecorations(host.Player.GetComponent<LwsCabAccessoryAnchorRegistry>());
            var anchorObject = new GameObject("TT_FilledContainerWindowAnchor");
            throwAnchor = anchorObject.transform;
            throwAnchor.SetParent(host.Player.transform, false);
            throwAnchor.localPosition = new Vector3(1.15f, 1.45f, .5f);
            var audioObject = new GameObject("TT_JugLiquidAudio");
            audioObject.transform.SetParent(host.Player.transform, false);
            var liquidSource = audioObject.AddComponent<AudioSource>();
            liquidLoop = new TruckTaxiJugLiquidLoop(liquidSource, jugLiquidClip);
            liquidWorldRouted = TruckTaxiAudioController.Instance?.Route(liquidSource, TruckTaxiAudioCategory.World) == true;
            lastGameSeconds = context.Clock.CurrentSnapshot.totalGameSeconds;
            context.Clock.ClockChanged += OnClockChanged;
            host.Session.RequestResolved += OnScenicStopResolved;
            host.Session.Changed += OnSessionChanged;
            previousSessionState = host.Session.State;
            var actions = owner.hud.UIInput.Actions;
            bool wasEnabled = actions.enabled; actions.Disable();
            JugHoldAction = actions.FindAction("DriverNeedsJugHold") ?? actions.AddAction("DriverNeedsJugHold", InputActionType.Button, "<Keyboard>/insert");
            JugCueAction = actions.FindAction("DriverNeedsJugCue") ?? actions.AddAction("DriverNeedsJugCue", InputActionType.Button, "<Keyboard>/delete");
            JugHoldAction.AddBinding("<Gamepad>/leftShoulder"); JugCueAction.AddBinding("<Gamepad>/rightShoulder");
            ThrowContainerAction = actions.FindAction("ThrowFilledContainer") ?? actions.AddAction("ThrowFilledContainer", InputActionType.Button, "<Keyboard>/home");
            ThrowContainerAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier", "<Gamepad>/select").With("Button", "<Gamepad>/rightShoulder");
            if (wasEnabled) actions.Enable();
            previousVelocity = body.linearVelocity;
            RefreshProximity();
            return true;
        }
        private void OnClockChanged(LwsGameClockSnapshot snapshot)
        {
            double elapsed = snapshot.totalGameSeconds - lastGameSeconds; lastGameSeconds = snapshot.totalGameSeconds;
            if (CanInteract && !snapshot.paused) State.AdvanceGameSeconds(elapsed);
        }
        private void Update()
        {
            if (State == null || body == null) return;
            bool throwChord = Gamepad.current != null && Gamepad.current.selectButton.isPressed && Gamepad.current.rightShoulder.isPressed;
            foreach (var source in indicatorSources)
                if (source != null) { if (throwChord) source.AcquireGamepadIndicatorSuppression(ThrowContainerAction); else source.ReleaseGamepadIndicatorSuppression(ThrowContainerAction); }
            if (!host.Paused) State.TickArcadeEffects(Time.deltaTime);
            if (Time.unscaledTime >= nextProximityCheck) { nextProximityCheck = Time.unscaledTime + .2f; RefreshProximity(); }
            if (State.JugActive) liquidLoop?.SetPaused(host.Paused);
            if (!CanInteract) { previousVelocity = body.linearVelocity; return; }
            if (ThrowContainerAction.WasPressedThisFrame()) ThrowFilledContainer();
            // Controller jug initiation is an explicit HUD command; normal indicator taps never start it.
            if (!JugActive && JugHoldAction.WasPressedThisFrame() && JugHoldAction.activeControl?.device is Keyboard) StartJug();
            float acceleration = Time.deltaTime > 0 ? Mathf.Abs(Vector3.Dot((body.linearVelocity - previousVelocity) / Time.deltaTime, host.Player.transform.right)) : 0;
            previousVelocity = body.linearVelocity;
            if (JugActive) State.TickJug(Time.deltaTime, JugHoldAction.IsPressed(), JugCueAction.WasPressedThisFrame(), IsMoving, acceleration);
        }
        public bool StartJug()
        {
            if (!CanInteract) return false;
            bool started = State.StartJug();
            if (!started) Feedback = !State.HasEmptyContainer ? "No empty container. Buy a jug or drink a bottle." : "No emergency needed yet.";
            return started;
        }
        public void RefreshProximity()
        {
            NearbyBathroom = null; NearbyStore = null;
            if (body == null) return;
            NearbyService = ServiceInReach(TruckTaxiServiceCapability.None);
            float distance = float.PositiveInfinity;
            foreach (var point in bathrooms)
            {
                if (point == null || !point.isActiveAndEnabled || !point.Contains(body.position)) continue;
                float candidate = (point.Position - body.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; NearbyBathroom = point; }
            }
            distance = float.PositiveInfinity;
            foreach (var point in stores)
            {
                if (point == null || !point.isActiveAndEnabled ||
                    !point.CanUse(body.position, body.linearVelocity.magnitude, settings.bathroomMaximumSpeed)) continue;
                float candidate = (point.Position - body.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; NearbyStore = point; }
            }
        }
        public bool BuyItem(TruckTaxiNeedsItem item)
        {
            string failure = PurchaseFailureReason(item);
            if (failure != null) { Feedback = failure; return false; }
            var entry = TruckTaxiNeedsItems.Find(item);
            if (!host.Session.TrySpend(entry.PriceCents))
            { Feedback = "Not Enough Cash"; return false; }
            State.AddItem(item);
            Feedback = "Bought " + entry.Name + ".";
            return true;
        }
        public string PurchaseFailureReason(TruckTaxiNeedsItem item)
        {
            if (host?.Session == null || State == null || !host.Ready ||
                host.Session.State == TruckTaxiState.Inactive) return "Service Unavailable";
            var entry = TruckTaxiNeedsItems.Find(item);
            if (entry == null) return "Item Unavailable";
            if (State.JugActive) return "Finish Current Action";
            var service = NearestServiceAtPosition(TruckTaxiServiceCapability.Store);
            if (service == null) return "Not In Store Area";
            if (!service.CanUse(body.position, body.linearVelocity.magnitude, settings.bathroomMaximumSpeed))
                return "Vehicle Must Stop";
            if (entry.HungerRelief > 0 && !service.Supports(TruckTaxiServiceCapability.Food) ||
                entry.ThirstRelief > 0 && !service.Supports(TruckTaxiServiceCapability.Drink) ||
                item == TruckTaxiNeedsItem.EmptyPissJug && !service.Supports(TruckTaxiServiceCapability.PissJugs))
                return "Item Not Sold Here";
            if (entry.Slot.HasValue && decorations != null)
            {
                bool full = item == TruckTaxiNeedsItem.AirFreshener ? decorations.IsMounted(TruckTaxiCabSlot.Mirror) :
                    decorations.IsMounted(TruckTaxiCabSlot.DashboardLeft) && decorations.IsMounted(TruckTaxiCabSlot.DashboardRight);
                if (full) return "No Slot Available";
                if (State.Count(item) > decorations.MountedCount(item)) return "Already Owned";
            }
            if (!State.CanAdd(item)) return "Inventory Full";
            if (host.Session.WalletBalanceCents < entry.PriceCents) return "Not Enough Cash";
            return null;
        }
        private TruckTaxiServicePoint NearestServiceAtPosition(TruckTaxiServiceCapability capability)
        {
            if (host == null || body == null) return null;
            TruckTaxiServicePoint best = null; float bestDistance = float.PositiveInfinity;
            foreach (var point in TruckTaxiServicePoint.Points)
            {
                if (point == null || point.gameObject.scene != host.gameObject.scene || !point.Supports(capability)) continue;
                Vector3 offset = body.position - point.Position;
                float planar = Vector3.ProjectOnPlane(offset, Vector3.up).sqrMagnitude;
                if (Mathf.Abs(offset.y) > point.verticalTolerance || planar > point.InteractionRadius * point.InteractionRadius || planar >= bestDistance) continue;
                best = point; bestDistance = planar;
            }
            return best;
        }
        public bool EatHere()
        {
            RefreshProximity();
            var restaurant = NearbyRestaurant;
            if (restaurant == null) { Feedback = "Not In Restaurant Area"; return false; }
            var meal = TruckTaxiNeedsItems.Find(TruckTaxiNeedsItem.Meal);
            if (host.Session.WalletBalanceCents < meal.PriceCents || !host.Session.TrySpend(meal.PriceCents))
            { Feedback = "Not Enough Cash"; return false; }
            State.EatMeal(meal.HungerRelief);
            Feedback = "Ate here for " + TruckTaxiHud.Money(meal.PriceCents) + ".";
            if (host.Session.HasPassenger && host.Session.Passenger != null)
                host.Passengers?.Dialogue?.Speak(host.Session.Passenger,
                    TruckTaxiDialogueCategory.RestaurantReaction, host.Session);
            RestoreRideRoute();
            return true;
        }
        public bool RouteToStore()
        {
            return RouteToService(TruckTaxiServiceCapability.Store);
        }
        public bool RouteToService(TruckTaxiServiceCapability capability)
        {
            if (host?.GPS == null || body == null) return false;
            if (host.Companions?.ActivePrivateStop != null)
            { Feedback = "Complete the private stop before changing service routes."; return false; }
            var point = TruckTaxiServicePoint.Nearest(body.position, capability, host.gameObject.scene,
                capability == (TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink));
            if (point == null) { Feedback = "No compatible service is available in this level."; return false; }
            host.GPS.SetServiceDestination(point.stableId, point.displayName, point.Position);
            Feedback = "Service route: " + point.displayName; return true;
        }
        public TruckTaxiServicePoint ServiceInReach(TruckTaxiServiceCapability capability)
        {
            var point = NearestServiceAtPosition(capability);
            return point != null && point.CanUse(body.position, body.linearVelocity.magnitude,
                settings.bathroomMaximumSpeed) ? point : null;
        }
        public bool ConsumeItem(TruckTaxiNeedsItem item)
        {
            if (State == null || State.JugActive || !State.Consume(item)) { Feedback = "Item unavailable."; return false; }
            Feedback = item == TruckTaxiNeedsItem.MysteryMushrooms ? "Everything looks unusually vivid." :
                item == TruckTaxiNeedsItem.HighOctaneSuppository ? "High-octane boost active." : "Used " + TruckTaxiNeedsItems.Find(item).Name + ".";
            if (item == TruckTaxiNeedsItem.MysteryMushrooms || item == TruckTaxiNeedsItem.HighOctaneSuppository)
                host.Session.ApplyMechanicReward(2, 0);
            host.Session.React(Feedback);
            return true;
        }
        public bool MountDecoration(TruckTaxiNeedsItem item, TruckTaxiCabSlot slot)
        {
            if (State == null || decorations == null || State.Count(item) <= decorations.MountedCount(item))
            { Feedback = "Item Not Owned"; return false; }
            if (decorations.IsMounted(slot)) { Feedback = "No Slot Available"; return false; }
            var visual = GameObject.CreatePrimitive(item == TruckTaxiNeedsItem.AirFreshener ? PrimitiveType.Cube : PrimitiveType.Sphere);
            visual.name = TruckTaxiNeedsItems.Find(item)?.Name ?? item.ToString();
            visual.transform.localScale = item == TruckTaxiNeedsItem.AirFreshener ? new Vector3(.07f, .12f, .018f) : Vector3.one * .11f;
            var renderer = visual.GetComponent<Renderer>();
            renderer.material.color = item == TruckTaxiNeedsItem.AirFreshener ? Color.green : item == TruckTaxiNeedsItem.HulaFigure ? Color.magenta : Color.yellow;
            if (!decorations.Mount(item, slot, visual)) { Destroy(visual); Feedback = "No Slot Available"; return false; }
            Feedback = "Mounted " + TruckTaxiNeedsItems.Find(item).Name + ".";
            return true;
        }
        public bool ThrowFilledContainer()
        {
            if (!CanInteract || State == null || State.JugActive || throwAnchor == null || body == null) return false;
            var item = State.Count(TruckTaxiNeedsItem.FilledJug) > 0 ? TruckTaxiNeedsItem.FilledJug : TruckTaxiNeedsItem.FilledBottle;
            if (!State.ThrowFilled(item)) { Feedback = "No filled container to throw."; return false; }
            StartCoroutine(ThrowContainer(item));
            Feedback = "Container thrown from the window.";
            host.Session.ApplyMechanicReward(6, -100);
            host.Session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.JugThrownFromWindow);
            DriverNeedsEvent?.Invoke(TruckTaxiDriverNeedEvent.JugThrownFromWindow);
            host.Session.React(Feedback);
            if (host.Session.HasPassenger && Enum.TryParse("JugThrownFromWindow", out TruckTaxiDialogueCategory category))
                host.Passengers?.Dialogue?.Speak(host.Session.Passenger, category, host.Session,
                    host.Session.Passenger.chaosAffinity > 0 ? "That was outrageous. I kind of respect it." :
                    "Please do not throw anything else out the window.", 40);
            return true;
        }
        private System.Collections.IEnumerator ThrowContainer(TruckTaxiNeedsItem item)
        {
            var visual = GameObject.CreatePrimitive(item == TruckTaxiNeedsItem.FilledJug ? PrimitiveType.Cylinder : PrimitiveType.Capsule);
            visual.name = item.ToString();
            visual.transform.SetPositionAndRotation(throwAnchor.position, throwAnchor.rotation);
            visual.transform.localScale = item == TruckTaxiNeedsItem.FilledJug ? new Vector3(.12f, .2f, .12f) : new Vector3(.09f, .14f, .09f);
            var collider = visual.GetComponent<Collider>(); collider.enabled = false;
            var projectile = visual.AddComponent<TruckTaxiFilledContainerProjectile>();
            projectile.Configure(containerImpactClip, host, body);
            var rigidbody = visual.GetComponent<Rigidbody>(); rigidbody.mass = .35f; rigidbody.isKinematic = true;
            float elapsed = 0;
            while (elapsed < .12f && visual != null && throwAnchor != null)
            {
                elapsed += Time.deltaTime;
                visual.transform.position = throwAnchor.position + throwAnchor.right * Mathf.Clamp01(elapsed / .12f) * .5f;
                yield return null;
            }
            if (visual == null) yield break;
            collider.enabled = true; rigidbody.isKinematic = false;
            rigidbody.linearVelocity = body != null ? body.linearVelocity : Vector3.zero;
            rigidbody.AddForce(throwAnchor.right * 4f + throwAnchor.up * 1.5f + throwAnchor.forward * 1.2f, ForceMode.Impulse);
        }
        public bool RouteToBathroom()
        {
            return RouteToService(TruckTaxiServiceCapability.Restroom);
        }
        public bool UseBathroom()
        {
            RefreshProximity();
            if (!State.UseBathroom(ProperlyStopped)) { Feedback = "Stop inside the restroom bay first."; return false; }
            RestoreRideRoute(); return true;
        }
        public bool DisposeJug()
        {
            RefreshProximity();
            if (State.DisposeJug(ServiceInReach(TruckTaxiServiceCapability.DisposeWaste) != null)) return true;
            Feedback = "Dispose at a waste service. Or throw it out the window."; return false;
        }
        public bool CleanCab()
        {
            RefreshProximity();
            if (State.CleanCab(ServiceInReach(TruckTaxiServiceCapability.CleanCab) != null))
            { host.Session.ChargeService(Mathf.Max(0, settings.spillCleanupCostCents)); return true; }
            Feedback = "Cab cleanup is available while stopped at a restroom."; return false;
        }
        public void RestoreRideRoute()
        {
            RoutedBathroom = null;
            var privateStop = host.Companions?.ActivePrivateStop;
            if (privateStop != null)
            {
                if (host.GPS.IsServiceDestination && host.GPS.TargetId == privateStop.stableId)
                {
                    if (!host.GPS.GuidanceSuppressed || host.GPS.RestoreServiceRoute()) return;
                }
                host.GPS.SetPrivateStopDestination(privateStop);
                return;
            }
            host.GPS.ClearDestination();
            if (host.Session.State == TruckTaxiState.DrivingToPickup) host.GPS.SetPickupDestination(host.Session.Pickup);
            else if (host.Session.State == TruckTaxiState.DrivingToDestination)
            {
                if (host.Session.ActiveStop?.StopPoint != null) host.GPS.SetStopDestination(host.Session.ActiveStop.StopPoint);
                else if(host.Session.SafeDropRequested) host.GPS.SetSafeDropDestination(host.Session.CurrentDesiredDestination);
                else host.GPS.SetRideDestination(host.Session.CurrentDesiredDestination);
            }
            else host.GPS.ClearDestination();
        }
        private void OnNeedEvent(TruckTaxiDriverNeedEvent kind)
        {
            if (kind == TruckTaxiDriverNeedEvent.JugCancelled) { DriverNeedsEvent?.Invoke(kind); return; }
            if (kind == TruckTaxiDriverNeedEvent.JugSucceeded || kind == TruckTaxiDriverNeedEvent.JugSpilled)
                host.Session.RecordNeedsEvent(kind);
            TruckTaxiDialogueCategory? category = null;
            switch (kind)
            {
                case TruckTaxiDriverNeedEvent.BladderUrgent: Feedback = "Restroom break needed soon."; category = TruckTaxiDialogueCategory.BladderUrgent; break;
                case TruckTaxiDriverNeedEvent.JugStarted: Feedback = "Steady hands. Three cues."; category = TruckTaxiDialogueCategory.JugStarted; break;
                case TruckTaxiDriverNeedEvent.JugSucceeded: Feedback = "No spill. Dispose at a service stop. Or throw it out the window."; category = TruckTaxiDialogueCategory.JugSucceeded; break;
                case TruckTaxiDriverNeedEvent.JugSpilled:
                case TruckTaxiDriverNeedEvent.CrisisAccident:
                    Feedback = kind == TruckTaxiDriverNeedEvent.CrisisAccident ? "An embarrassing incident. Shift continues; cab needs cleanup." : "That did not go to plan. Cab needs cleanup.";
                    host.Session.ApplyMechanicReward(5, 0);
                    category = TruckTaxiDialogueCategory.JugSpilled; break;
                case TruckTaxiDriverNeedEvent.BathroomStop: Feedback = "Restroom break complete."; category = TruckTaxiDialogueCategory.BathroomStop; break;
                case TruckTaxiDriverNeedEvent.JugDisposed: Feedback = "Filled jug disposed of. Emergency jug available again."; break;
                case TruckTaxiDriverNeedEvent.CabCleaned: Feedback = "Cab cleaned."; break;
            }
            DriverNeedsEvent?.Invoke(kind);
            host.Session.React(Feedback);
            if (category.HasValue && host.Session.HasPassenger)
                host.Passengers?.Dialogue?.Speak(host.Session.Passenger, category.Value, host.Session,
                    settings.ReactionFor(category.Value, host.Session.Passenger), 35);
        }
        private void OnScenicStopResolved(TaxiRequestProgress request)
        {
            if (request.State != TaxiRequestState.Succeeded || request.StopPoint == null || request.StopPoint.category != TruckTaxiStopCategory.Scenic) return;
            var bonus = request.StopPoint.GetComponent<TruckTaxiScenicEnvironmentBonus>();
            if (bonus == null || !bonus.Matches(environment.Period, environment.Weather.CurrentSnapshot) ||
                !scenicAwards.Add(host.Session.CurrentRideId + ":" + request.StopPoint.stableId)) return;
            host.Session.ApplyMechanicReward(0, Mathf.Max(0, bonus.bonusFareCents));
            Feedback = "Scenic conditions bonus: " + TruckTaxiHud.Money(bonus.bonusFareCents);
            host.Session.React(Feedback);
            host.Passengers?.Dialogue?.Speak(host.Session.Passenger, TruckTaxiDialogueCategory.UniqueMechanicReaction, host.Session, bonus.bonusDialogue, 40);
        }
        private void OnSessionChanged()
        {
            if (previousSessionState == host.Session.State) return;
            previousSessionState = host.Session.State;
            if (host.Session.State != TruckTaxiState.Available &&
                host.Session.State != TruckTaxiState.DrivingToPickup &&
                host.Session.State != TruckTaxiState.DrivingToDestination) State.CancelJug();
            if (host.Session.State == TruckTaxiState.Inactive) { scenicAwards.Clear(); RoutedBathroom = null; }
            if (host.Session.State == TruckTaxiState.RideOffered) RoutedBathroom = null;
        }
        public void DebugSetBladder(float value) { if (Debug.isDebugBuild) State?.SetPressure(value); }
        public void DebugFinishJug(bool success)
        {
            if (!Debug.isDebugBuild || State == null) return;
            if (!State.JugActive) { State.DisposeJug(true); State.SetPressure(.9f); State.StartJug(); }
            State.FinishJug(success);
        }
        public void DebugClearCabMess() { if (Debug.isDebugBuild && State != null) { State.CancelJug(); State.CleanCab(true); } }
        private void OnJugInputEvent(TruckTaxiDriverNeedEvent kind)
        {
            if (kind == TruckTaxiDriverNeedEvent.JugStarted)
            {
                SetJugIndicatorSuppression(true);
                liquidLoop?.Play();
            }
            else if (kind == TruckTaxiDriverNeedEvent.JugSucceeded || kind == TruckTaxiDriverNeedEvent.JugSpilled ||
                     kind == TruckTaxiDriverNeedEvent.JugCancelled || kind == TruckTaxiDriverNeedEvent.CrisisAccident)
            { SetJugIndicatorSuppression(false); liquidLoop?.Stop(); }
        }
        [ContextMenu("Report Jug Loop Audio")]
        private void ReportJugLoopAudio()
        {
            string report = "JUG AUDIO: qte=" + JugActive + " playing=" + LoopAudioActive +
                " generated=" + UsingGeneratedLiquidAudio +
                " worldBus=" + liquidWorldRouted;
            if (JugActive && host?.Paused != true && !LoopAudioActive) Debug.LogError(report, this);
            else Debug.Log(report, this);
        }
        private void OnDisable()
        {
            foreach (var source in indicatorSources) if (source != null) source.ReleaseGamepadIndicatorSuppression(ThrowContainerAction);
            if (State?.JugActive == true) State.CancelJug();
            liquidLoop?.Stop();
        }
        private void SetJugIndicatorSuppression(bool suppressed)
        {
            foreach (var source in indicatorSources)
            {
                if (source == null) continue;
                if (suppressed) source.AcquireGamepadIndicatorSuppression(this);
                else source.ReleaseGamepadIndicatorSuppression(this);
            }
        }
        private void OnDestroy()
        {
            SetJugIndicatorSuppression(false);
            liquidLoop?.Dispose();
            liquidLoop = null;
            if (State == null) return;
            State.Event -= OnNeedEvent; State.Event -= OnJugInputEvent; State.CancelJug();
            environment.Clock.ClockChanged -= OnClockChanged;
            if (host?.Session != null) { host.Session.RequestResolved -= OnScenicStopResolved; host.Session.Changed -= OnSessionChanged; }
        }
    }
}
