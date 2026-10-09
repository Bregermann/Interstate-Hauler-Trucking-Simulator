using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.Trailer;
using NWH.VehiclePhysics2.Damage;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Bootstrap-owned sidecar. Call Initialize after the taxi HUD and venue runtime are ready.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRaceRuntime : MonoBehaviour
    {
        private sealed class Opponent
        {
            public GameObject Actor;
            public Behaviour Driver;
            public Rigidbody Body;
            public TruckTaxiRaceCountdownLock CountdownLock;
            public TruckTaxiRaceProgress Progress;
            public int EliminatedAtLap;
            public int LaneIndex, Recoveries;
            public float LastMovedAt;
            public Vector3 LastPosition;
            public Component MovePath;
            public float FinishedAt, NextRecoveryAt, WaitingSince;
            public TruckTaxiRaceCountdownLock StreamingLock;
        }

        public TruckTaxiRaceSettings settings = new TruckTaxiRaceSettings();

        [Tooltip("Use installed UTS Truck_1/Truck_2 prefabs, never an NWH player tractor.")]
        public GameObject[] utsTruckPrefabs = Array.Empty<GameObject>();

        public bool IsActive { get; private set; }
        public string BlockTravelReason => IsActive || MenuOpen ? "Leave the race session before fast travel." : null;
        public bool MenuOpen => FocusRoot != null;
        public Transform FocusRoot => results != null && results.gameObject.activeSelf ? results :
            menu != null && menu.gameObject.activeSelf ? menu : null;
        public Transform MenuRoot => FocusRoot;
        public TruckTaxiRaceMode Mode { get; private set; }
        public int Position { get; private set; } = 1;
        public int OpponentCount => opponents.Count;
        public bool EventRaceAvailable => EventActive();
        public string Feedback { get; private set; } = "";
        public bool IsFlagship => Mode == TruckTaxiRaceMode.FlagshipRace;
        public string FlagshipUnavailableReason { get; private set; }
        public int NextCheckpoint => playerProgress?.NextCheckpoint ?? 0;
        public int CheckpointCount => gates.Length;
        public bool DebugModified { get; private set; }
        public IReadOnlyList<Vector3> FlagshipGlobalCheckpoints => flagshipRoute?.Gates ?? Array.Empty<Vector3>();

        private readonly LwsUtsTrafficApi uts = new LwsUtsTrafficApi();

        private readonly LwsTrafficSpawnPolicy spawnPolicy = new LwsTrafficSpawnPolicy
        {
            densityTier = LwsTrafficDensityTier.Off,
            targetCruiseSpeedScale = .9f,
            maximumTrafficSpeedMetersPerSecond = 27,
            autoResolveEditorPrefabs = false
        };

        private readonly List<Opponent> opponents = new List<Opponent>(20);
        private readonly List<GameObject> pathRoots = new List<GameObject>(2);
        private readonly List<LwsTrafficLaneDefinition> lanes = new List<LwsTrafficLaneDefinition>(2);
        private readonly List<Component> paths = new List<Component>(2);
        private Vector3[] gates = TruckTaxiSpeedwayLayout.LapCheckpoints();
        private TruckTaxiFlagshipRoute flagshipRoute;
        private TruckTaxiRaceSettings courseSettings;
        private TruckTaxiRegionalWorld regionalWorld;
        private ILwsWorldOriginService worldOrigin;
        private bool originSubscribed, debugStartRequested, entryReserved;
        private int debugRivalCount = 20;
        private float nextRecoveryWork;
        private TruckTaxiBootstrap host;
        private Rigidbody playerBody;
        private TruckTaxiRaceProgress playerProgress;
        private Vector3[] centerline;
        private RectTransform menu, results, raceHud;
        private GameObject registration;
        private TextMeshProUGUI menuFeedback, lapSelection, raceStatus, resultText;
        private int timeTrialLaps = 3;
        private int targetLaps;
        private float startTime, countdownEnd, nextUiRefresh, penaltySeconds;
        private float startDamage;
        private int startTrafficHits;
        private bool countdown, menuWasPaused, initialized;
        private readonly object modalSuppression = new object();
        private TruckTaxiRaceCountdownLock playerCountdownLock;

        public void Initialize(TruckTaxiBootstrap owner)
        {
            if (initialized) return;
            if (owner == null || owner.Player == null || owner.Session == null || owner.hud == null)
                throw new ArgumentException("Race sidecar requires a ready Truck Taxi host.", nameof(owner));
            host = owner;
            regionalWorld = owner.GetComponent<TruckTaxiRegionalWorld>();
            LwsApplicationBootstrap.Instance?.Registry?.TryGet(out worldOrigin);
            playerBody = owner.Player.NwhAdapter?.VehicleController?.vehicleRigidbody;
            centerline = TruckTaxiSpeedwayLayout.TrackCenterline().ToArray();
            playerProgress = new TruckTaxiRaceProgress(gates, settings);
            RefreshFlagshipRoute();
            BuildUi(owner.hud);
            initialized = true;
        }

        private void BuildUi(TruckTaxiHud view)
        {
            registration = view.Button(view.Root, "RACE REGISTRATION", new Vector2(.73f, .34f),
                new Vector2(.99f, .40f), OpenRegistration);
            menu = view.Panel(view.Root, "Thunder Bowl Racing", new Vector2(.28f, .09f), new Vector2(.72f, .91f));
            var title = view.Text(menu, "Race title", new Vector2(.05f, .89f), new Vector2(.95f, .98f), 32);
            title.text = "ROAD RACING";
            view.Button(menu, "CLOSE", new Vector2(.76f, .82f), new Vector2(.96f, .89f), CloseRegistration);
            string[] labels = { "HIGHWAY / CITY / HIGHWAY", "OVAL PRACTICE", "OVAL TIME TRIAL", "OVAL TRUCK RACE", "OVAL ELIMINATION", "OVAL CHAOS RACE", "OVAL EVENT RACE" };
            for (int i = 0; i < labels.Length; i++)
            {
                var selected = i == 0 ? TruckTaxiRaceMode.FlagshipRace : (TruckTaxiRaceMode)(i - 1);
                float top = .79f - i * .087f;
                view.Button(menu, labels[i], new Vector2(.06f, top - .066f), new Vector2(.94f, top),
                    () => BeginRace(selected));
            }
            lapSelection = view.Text(menu, "Trial laps", new Vector2(.07f, .10f), new Vector2(.52f, .17f), 22);
            view.Button(menu, "1 / 3 / 5 LAPS", new Vector2(.54f, .10f), new Vector2(.94f, .17f), CycleTrialLaps);
            menuFeedback = view.Text(menu, "Race feedback", new Vector2(.06f, .015f), new Vector2(.94f, .09f), 20);
            menu.gameObject.SetActive(false);

            raceHud = view.Panel(view.Root, "Race timing", new Vector2(.73f, .43f), new Vector2(.99f, .70f));
            raceStatus = view.Text(raceHud, "Timing", new Vector2(.05f, .22f), new Vector2(.95f, .97f), 23);
            view.Button(raceHud, "RESET TO TRACK", new Vector2(.04f, .04f), new Vector2(.52f, .20f), () => ResetToTrack());
            view.Button(raceHud, "LEAVE", new Vector2(.56f, .04f), new Vector2(.96f, .20f), LeaveRace);
            raceHud.gameObject.SetActive(false);

            results = view.Panel(view.Root, "Race results", new Vector2(.26f, .25f), new Vector2(.74f, .75f));
            resultText = view.Text(results, "Result", new Vector2(.06f, .19f), new Vector2(.94f, .94f), 28);
            view.Button(results, "CLOSE", new Vector2(.66f, .04f), new Vector2(.95f, .16f), CloseRegistration);
            results.gameObject.SetActive(false);
            registration.SetActive(false);
            RefreshMenu();
        }

        private void Update()
        {
            if (!initialized) return;
            if (!IsActive)
            {
                if (registration != null)
                {
                    Vector3 pit = TruckTaxiSpeedwayLayout.Stops[1].Position;
                    bool nearby = host.Player != null && (host.Player.transform.position - pit).sqrMagnitude <= 40 * 40;
                    nearby |= flagshipRoute != null && host.Player != null &&
                        (host.Player.transform.position - ToLocal(flagshipRoute.Gates[0])).sqrMagnitude <= 100 * 100;
                    registration.SetActive(nearby && !MenuOpen && !host.Paused && !host.hud.NormalHudSuppressed &&
                        host.Session.State == TruckTaxiState.Available && host.Companions?.IsBusy != true);
                }
                return;
            }
            if (host.Session.State != TruckTaxiState.Available || host.Player == null)
            { CancelRace("Race interrupted by taxi session state."); return; }
            if (host.WorldLife != null && host.WorldLife.Mode != TruckTaxiPlayerControlMode.Driving)
            { CancelRace("Race ended when the driver left truck control."); return; }
            if (!RequirePlayerEligibility()) return;
            if (Mode == TruckTaxiRaceMode.EventRace && Time.time >= nextUiRefresh && !EventActive())
            { CancelRace("Scheduled event interrupted. Race cancelled safely."); return; }
            if (host.Paused) return;
            if (IsFlagship && regionalWorld != null && !regionalWorld.IsPositionAvailable(playerBody.position))
            { CancelRace("Flagship road region became unavailable."); return; }
            if (countdown)
            {
                int remaining = Mathf.CeilToInt(countdownEnd - Time.time);
                raceStatus.text = (IsFlagship ? "HIGHWAY / CITY / HIGHWAY\n" : "THUNDER BOWL\n") + (remaining > 0 ? remaining.ToString() : "GO");
                if (Time.time < countdownEnd) return;
                foreach (var opponent in opponents)
                    if (opponent.Body == null || opponent.Driver == null)
                    { CancelRace("Rival unavailable during countdown; entry refunded."); return; }
                ReleaseCountdown();
                if (entryReserved) { host.Session.CommitRaceEntry(); entryReserved = false; }
                countdown = false; startTime = Time.time;
                playerProgress.Begin(playerBody.position, startTime);
                foreach (var opponent in opponents)
                {
                    opponent.Progress.Begin(opponent.Body.position, startTime);
                    opponent.LastMovedAt = startTime;
                    if (opponent.Driver != null) opponent.Driver.enabled = true;
                }
            }
            float now = Time.time;
            Vector3 playerPosition = playerBody.position;
            if (DistanceToTrack(playerPosition) > (IsFlagship ? 10 : TruckTaxiSpeedwayLayout.TrackWidth * .5f + 5f))
                playerProgress.Invalidate(playerPosition, now);
            bool completed = playerProgress.Tick(playerPosition, now);
            for (int i = 0; i < opponents.Count; i++)
            {
                var opponent = opponents[i];
                if (opponent.Body == null)
                { opponent.EliminatedAtLap = 1; if (opponent.Actor != null) opponent.Actor.SetActive(false); continue; }
                if (opponent.EliminatedAtLap > 0 || opponent.FinishedAt > 0) continue;
                if (IsFlagship && !RivalRoadReady(opponent, now)) continue;
                Vector3 position = opponent.Body.position;
                bool rivalCompleted = opponent.Progress.Tick(position, now);
                if (rivalCompleted && targetLaps > 0 && opponent.Progress.CompletedLaps >= targetLaps)
                { opponent.FinishedAt = now; opponent.Driver.enabled = false; opponent.Actor.SetActive(false); continue; }
                if ((position - opponent.LastPosition).sqrMagnitude > 16)
                { opponent.LastPosition = position; opponent.LastMovedAt = now; }
                if ((now - opponent.LastMovedAt > 12 || IsFlagship && (MissedGate(opponent) || opponent.Body.transform.up.y < .4f)) &&
                    now >= opponent.NextRecoveryAt && now >= nextRecoveryWork)
                {
                    if (opponent.Recoveries >= (IsFlagship ? 6 : 2))
                    { opponent.EliminatedAtLap = 1; opponent.Actor.SetActive(false); continue; }
                    nextRecoveryWork = now + .5f; opponent.NextRecoveryAt = now + 3;
                    RecoverOpponent(opponent, now);
                }
            }
            if (Mode == TruckTaxiRaceMode.Elimination && completed)
                EliminateLast();
            if (!IsActive) return;
            Position = 1;
            float playerScore = Score(playerProgress, playerPosition);
            foreach (var opponent in opponents)
                if (opponent.EliminatedAtLap == 0 && opponent.Body != null &&
                    (opponent.FinishedAt > 0 || Score(opponent.Progress, opponent.Body.position) > playerScore)) Position++;
            if (Time.time >= nextUiRefresh)
            {
                nextUiRefresh = Time.time + .1f;
                raceStatus.text = $"{Mode.ToString().ToUpperInvariant()}  P{Position}/{opponents.Count + 1}\n" +
                    $"LAP {playerProgress.CompletedLaps + 1}/{(targetLaps == 0 ? "OPEN" : targetLaps.ToString())}  " +
                    $"CP {playerProgress.NextCheckpoint + 1}/{gates.Length}\n" +
                    $"CURRENT {Format(playerProgress.CurrentLapSeconds(now))}  BEST {Format(playerProgress.BestLapSeconds)}";
            }
            if (completed && targetLaps > 0 && playerProgress.CompletedLaps >= targetLaps) FinishRace();
        }

        private void FixedUpdate() => HoldCountdown();
        private void LateUpdate() => HoldCountdown();
        private void HoldCountdown()
        {
            if (!countdown) return;
            if (!RequirePlayerEligibility()) return;
            playerCountdownLock?.Hold();
            foreach (var opponent in opponents) opponent.CountdownLock?.Hold();
        }

        private void ReleaseCountdown()
        {
            playerCountdownLock?.Dispose(); playerCountdownLock = null;
            foreach (var opponent in opponents)
            { opponent.CountdownLock?.Dispose(); opponent.CountdownLock = null; }
        }

        private float Score(TruckTaxiRaceProgress progress, Vector3 position)
        {
            int previous = (progress.NextCheckpoint + gates.Length - 1) % gates.Length;
            Vector3 a = gates[previous], b = gates[progress.NextCheckpoint];
            Vector3 span = b - a;
            float fraction = span.sqrMagnitude > 1 ? Mathf.Clamp01(Vector3.Dot(position - a, span) / span.sqrMagnitude) : 0;
            return progress.PassedCheckpoints + fraction;
        }

        private float DistanceToTrack(Vector3 position)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < centerline.Length; i++)
            {
                Vector2 a = new Vector2(centerline[i].x, centerline[i].z);
                Vector2 b = new Vector2(centerline[(i + 1) % centerline.Length].x, centerline[(i + 1) % centerline.Length].z);
                Vector2 delta = b - a;
                float t = delta.sqrMagnitude < .01f ? 0 : Mathf.Clamp01(Vector2.Dot(new Vector2(position.x, position.z) - a, delta) / delta.sqrMagnitude);
                nearest = Mathf.Min(nearest, Vector2.Distance(new Vector2(position.x, position.z), a + delta * t));
            }
            return nearest;
        }

        public void OpenRegistration()
        {
            if (!initialized || !isActiveAndEnabled || IsActive || MenuOpen || host.Paused || host.hud.NormalHudSuppressed) return;
            if (host.Session.State != TruckTaxiState.Available)
            { Publish("race-ride", "FINISH CURRENT RIDE BEFORE RACING", TruckTaxiAlertPriority.High, 5); return; }
            OpenModal(menu);
            RefreshMenu();
        }

        private void OpenModal(RectTransform root)
        {
            menuWasPaused = host.Paused;
            host.Session.AcquireOfferSuppression(modalSuppression);
            host.SetPaused(true);
            registration.SetActive(false);
            root.gameObject.SetActive(true); root.SetAsLastSibling();
            host.hud.UIInput?.Focus(root);
        }

        public void CloseRegistration()
        {
            if (!MenuOpen) return;
            menu.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            host?.Session?.ReleaseOfferSuppression(modalSuppression);
            if (host != null) host.SetPaused(menuWasPaused);
            host?.hud?.UIInput?.Focus(null);
        }

        private void CycleTrialLaps()
        { timeTrialLaps = timeTrialLaps == 1 ? 3 : timeTrialLaps == 3 ? 5 : 1; RefreshMenu(); }

        private void RefreshMenu()
        {
            if (lapSelection != null) lapSelection.text = "TIME TRIAL  " + timeTrialLaps + " LAPS";
            if (menuFeedback != null) menuFeedback.text = Feedback;
        }

        public bool RefreshFlagshipRoute()
        {
            if (IsActive) return flagshipRoute != null;
            var traffic = host != null ? host.traffic : null;
            bool valid = TruckTaxiFlagshipRoute.TryCreate(host?.roadGraph?.Graph, traffic?.cityLanes,
                out flagshipRoute, out string reason);
            FlagshipUnavailableReason = reason;
            return valid;
        }

        private Vector3 ToLocal(Vector3 global) => worldOrigin != null ?
            worldOrigin.GlobalToLocal(LwsWorldPositionD.FromVector3(global)) : global;

        private TruckTaxiRaceProgress NewProgress() => new TruckTaxiRaceProgress(gates,
            courseSettings ?? settings, IsFlagship ? flagshipRoute.GateForwards : null, IsFlagship);

        private bool SelectCourse(TruckTaxiRaceMode mode)
        {
            if (mode == TruckTaxiRaceMode.FlagshipRace)
            {
                if (!RefreshFlagshipRoute()) { Feedback = FlagshipUnavailableReason; return false; }
                centerline = Array.ConvertAll(flagshipRoute.Points, ToLocal);
                gates = Array.ConvertAll(flagshipRoute.Gates, ToLocal);
                courseSettings = new TruckTaxiRaceSettings {
                    maximumStepMeters = settings.maximumStepMeters,
                    checkpointHalfWidthMeters = Mathf.Min(5, flagshipRoute.LaneWidthMeters + 1)
                };
            }
            else
            {
                centerline = TruckTaxiSpeedwayLayout.TrackCenterline().ToArray();
                gates = TruckTaxiSpeedwayLayout.LapCheckpoints(); courseSettings = settings;
            }
            Mode = mode; playerProgress = NewProgress();
            return true;
        }

        public bool BeginFlagshipRace() => BeginRace(TruckTaxiRaceMode.FlagshipRace);

        private string PlayerBlockReason()
        {
            var player = host != null ? host.Player : null;
            if (player == null) return "NWH PLAYER TRUCK IS UNAVAILABLE";
            if (host.WorldLife != null && host.WorldLife.Mode != TruckTaxiPlayerControlMode.Driving)
                return "RETURN TO TRUCK CONTROL BEFORE RACING";
            var hitch = player.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            if (player.CouplingAdapter?.IsTrailerAttached == true || hitch != null && hitch.module != null && hitch.module.attached)
                return "DETACH TRAILER BEFORE RACING";
            if (host.Kaiju?.IsCapturingPlayer == true || host.GetComponent<TruckTaxiKaijuPlayerRecovery>()?.Captured == true)
                return "PLAYER CAPTURE IS IN PROGRESS";
            if (host.Fuel?.IsRescuing == true || host.Roadside?.IsRecovering == true || host.FastTravel?.IsTravelling == true)
                return "TRUCK RECOVERY IS IN PROGRESS";
            var vehicle = player.NwhAdapter?.VehicleController;
            if (!player.IsReady || !player.isActiveAndEnabled || vehicle == null || !vehicle.isActiveAndEnabled ||
                !vehicle.IsInitialized || vehicle.input == null)
                return "NWH DRIVER IS NOT READY";
            var canonicalBody = vehicle.vehicleRigidbody;
            if (canonicalBody == null || canonicalBody.isKinematic || !canonicalBody.gameObject.activeInHierarchy ||
                vehicle.GetComponent<Rigidbody>() != canonicalBody || vehicle.transform != player.transform ||
                playerBody != null && playerBody != canonicalBody)
                return "NWH CANONICAL DYNAMIC BODY IS UNAVAILABLE";
            // Bootstrap can initialize this sidecar before NWH Start finishes.
            if (playerBody == null && !IsActive) playerBody = canonicalBody;
            return playerBody == canonicalBody ? null : "NWH PLAYER BODY CHANGED";
        }

        private bool RequirePlayerEligibility()
        {
            string reason = PlayerBlockReason();
            if (reason == null) return true;
            Feedback = reason;
            if (IsActive) CancelRace(reason);
            RefreshMenu();
            return false;
        }

        private void OriginShiftStarting(LwsOriginShiftEvent shift) => CancelRace("Race cancelled before world-origin shift; restart on the relocated road.");

        public bool BeginRace(TruckTaxiRaceMode mode)
        {
            if (!initialized || !isActiveAndEnabled || IsActive || host.Session.State != TruckTaxiState.Available)
            { Feedback = "FINISH CURRENT RIDE BEFORE RACING"; RefreshMenu(); return false; }
            if (host.WorldLife != null && host.WorldLife.Mode != TruckTaxiPlayerControlMode.Driving || host.Companions?.IsBusy == true)
            { Feedback = "RETURN TO TRUCK AND FINISH THE ACTIVE COMPANION TASK BEFORE RACING"; RefreshMenu(); return false; }
            if (!SelectCourse(mode)) { RefreshMenu(); return false; }
            if (mode == TruckTaxiRaceMode.EventRace && !EventActive())
            { Feedback = "EVENT RACE IS NOT ON THE CALENDAR NOW"; RefreshMenu(); return false; }
            if (!RequirePlayerEligibility()) return false;
            if (host.GetComponent<TruckTaxiRegionalWorld>() is TruckTaxiRegionalWorld world &&
                !world.IsPositionAvailable(gates[0]))
            { Feedback = "RACE START ROAD IS STILL STREAMING"; RefreshMenu(); return false; }
            if (!debugStartRequested && (IsFlagship ? Vector3.Distance(host.Player.transform.position, gates[0]) : DistanceToTrack(host.Player.transform.position)) > 250)
            { Feedback = IsFlagship ? "REGISTER NEAR THE HIGHWAY START" : "REGISTER AT THUNDER BOWL PIT OFFICE"; RefreshMenu(); return false; }
            bool needsAi = mode != TruckTaxiRaceMode.Practice && mode != TruckTaxiRaceMode.TimeTrial;
            var before = new TruckTaxiRecoverySpace.BodySnapshot(playerBody);
            bool entered = false, started = false, wasPaused = host.Paused, hadMenu = MenuOpen;
            bool modalPreviousPause = menuWasPaused;
            try
            {
                // Reserve the player's lane first, so every opponent clearance query sees it.
                if (!PlacePlayerAt(FindGridPoint(0), true))
                { Feedback = "NO CLEAR GROUNDED PLAYER GRID SLOT"; return false; }
                if (needsAi && !PrepareOpponents(mode))
                { if (string.IsNullOrEmpty(Feedback)) Feedback = "NO SAFE UTS GRID; CHECK TRUCKS AND ROAD CLEARANCE"; return false; }
                if (!RequirePlayerEligibility()) return false;
                playerCountdownLock = new TruckTaxiRaceCountdownLock(playerBody, host.Player.NwhAdapter.VehicleController.input);
                long fee = needsAi && !debugStartRequested ? Math.Max(0, settings.entryFeeCents) : 0;
                if (!debugStartRequested && !host.Session.TryEnterRace(fee))
                { Feedback = "NOT ENOUGH CASH FOR ENTRY"; return false; }
                entered = entryReserved = !debugStartRequested;
                if (!RequirePlayerEligibility()) return false;
                host.Session.AcquireOfferSuppression(this);
                Mode = mode; targetLaps = settings.Laps(mode, timeTrialLaps);
                DebugModified = debugStartRequested;
                if (worldOrigin != null) { worldOrigin.OriginShiftStarting += OriginShiftStarting; originSubscribed = true; }
                penaltySeconds = 0; nextRecoveryWork = 0; Position = 1; IsActive = true;
                startTrafficHits = host.Session.TrafficHits;
                var damage = host.Player.GetComponent<DamageHandler>();
                startDamage = damage != null ? damage.Damage : 0;
                countdown = true; countdownEnd = Time.time + 3;
                menu.gameObject.SetActive(false); results.gameObject.SetActive(false); registration.SetActive(false);
                host.Session.ReleaseOfferSuppression(modalSuppression);
                raceHud.gameObject.SetActive(true); raceHud.SetAsLastSibling();
                host.hud.UIInput?.Focus(null);
                host.SetPaused(false);
                Feedback = ""; started = true;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Thunder Bowl start failed: " + error.Message, this);
                Feedback = "RACE START FAILED; ENTRY REFUNDED";
                return false;
            }
            finally
            {
                if (!started)
                {
                    ReleaseCountdown(); CleanupOpponents();
                    // Never move a newly coupled/captured truck or override another recovery authority.
                    bool retainsControl = PlayerBlockReason() == null;
                    if (retainsControl) { before.Restore(); Physics.SyncTransforms(); }
                    UnsubscribeOrigin();
                    host.Session.DiscardTeleportDistance();
                    if (entered) host.Session.RefundRaceEntry();
                    entryReserved = false;
                    host.Session.ReleaseOfferSuppression(this);
                    IsActive = countdown = false;
                    raceHud.gameObject.SetActive(false);
                    if (retainsControl)
                    {
                        if (hadMenu && !MenuOpen) { OpenModal(menu); menuWasPaused = modalPreviousPause; }
                        host.SetPaused(wasPaused);
                    }
                    RefreshMenu();
                }
            }
        }

        private bool EventActive()
        {
            var venue = host?.Venues;
            if (venue == null) return false;
            foreach (var item in venue.EventsSnapshots)
                if (item.VenueId == TruckTaxiSpeedwayLayout.RegionId && item.Status == TruckTaxiVenueEventStatus.InProgress)
                    return true;
            return false;
        }

        private bool PrepareOpponents(TruckTaxiRaceMode mode)
        {
            CleanupOpponents();
            Feedback = "";
            if (!uts.IsAvailable || utsTruckPrefabs == null || utsTruckPrefabs.Length == 0) return false;
            foreach (var prefab in utsTruckPrefabs) if (!uts.PrefabLooksLikeUtsVehicle(prefab)) return false;
            int count = IsFlagship ? (debugStartRequested ? debugRivalCount : TruckTaxiFlagshipRoute.MaximumRivals) :
                Mathf.Clamp(mode == TruckTaxiRaceMode.EventRace ? settings.eventOpponents : settings.normalOpponents, 3, 7);
            if (!IsFlagship && SystemInfo.processorCount <= 4) count = 3;
            for (int laneIndex = 0; laneIndex < (IsFlagship ? 1 : 2); laneIndex++)
            {
                var points = new Vector3[centerline.Length];
                for (int i = 0; i < points.Length; i++)
                    points[i] = IsFlagship ? centerline[i] : TruckTaxiRaceGrid.LanePoint(centerline, i, laneIndex);
                var lane = new LwsTrafficLaneDefinition
                {
                    laneId = IsFlagship ? "taxi.flagship.race" : "taxi.speedway.race." + laneIndex,
                    roadId = IsFlagship ? flagshipRoute.SourceLaneId : "taxi.speedway.track",
                    segmentId = IsFlagship ? "highway-city-highway" : "oval",
                    laneIndex = laneIndex,
                    centerline = points,
                    laneWidthMeters = IsFlagship ? flagshipRoute.LaneWidthMeters : 4,
                    speedLimitMph = IsFlagship ? 50 : 60,
                    spawnEnabled = false
                };
                var root = new GameObject("Thunder Bowl UTS race lane " + laneIndex);
                root.transform.SetParent(transform, false);
                pathRoots.Add(root);
                var path = uts.CreatePath(root, lane, utsTruckPrefabs, spawnPolicy, out string message);
                if (path == null) { Debug.LogWarning(message, this); CleanupOpponents(); return false; }
                lanes.Add(lane); paths.Add(path);
            }
            for (int i = 0; i < count; i++)
            {
                int laneIndex = IsFlagship ? 0 : TruckTaxiRaceGrid.Lane(i + 1);
                int index = IsFlagship ? flagshipRoute.GridIndex(i + 1) : TruckTaxiRaceGrid.Index(centerline.Length, i + 1);
                if (IsFlagship && regionalWorld != null && !regionalWorld.IsPositionAvailable(centerline[index]))
                { Feedback = "RIVAL GRID ROAD IS STILL STREAMING (SLOT " + (i + 1) + ")"; CleanupOpponents(); return false; }
                GameObject prefab = utsTruckPrefabs[i % utsTruckPrefabs.Length];
                var personality = TruckTaxiRaceSettings.Personality(i);
                var driverPolicy = new LwsTrafficSpawnPolicy
                {
                    densityTier = LwsTrafficDensityTier.Off,
                    targetCruiseSpeedScale = TruckTaxiRaceSettings.SpeedScale(personality),
                    maximumTrafficSpeedMetersPerSecond = spawnPolicy.maximumTrafficSpeedMetersPerSecond,
                    autoResolveEditorPrefabs = false
                };
                GameObject actor = uts.SpawnVehicle(prefab, paths[laneIndex], lanes[laneIndex], index, pathRoots[laneIndex].transform, driverPolicy, out string message);
                if (actor == null) { Debug.LogWarning(message, this); CleanupOpponents(); return false; }
                // Take ownership before any further vendor/clearance operation can throw.
                var opponent = new Opponent { Actor = actor };
                opponents.Add(opponent);
                Behaviour driver = null;
                foreach (var behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour.GetType().Name == "CarAIController") driver = behaviour;
                    // Same instance-only cab configuration as the existing taxi rival adapter.
                    if (behaviour.GetType().Name == "AddTrailer") { behaviour.enabled = false; Destroy(behaviour); }
                }
                var body = driver != null ? driver.GetComponent<Rigidbody>() : null;
                if (body == null) { CleanupOpponents(); return false; }
                driver.enabled = false;
                opponent.Driver = driver; opponent.Body = body;
                foreach (var component in actor.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component.GetType().Name == "MovePath") { opponent.MovePath = component; break; }
                if (opponent.MovePath == null) { CleanupOpponents(); return false; }
                float following = personality == TruckTaxiRacePersonality.Reckless ? 4 :
                    personality == TruckTaxiRacePersonality.Aggressive ? 8 : personality == TruckTaxiRacePersonality.Cautious ? 28 : 16;
                driver.GetType().GetProperty("TO_CAR")?.SetValue(driver, following);
                Physics.SyncTransforms();
                Quaternion rotation = TrackFacing(index);
                if (!TruckTaxiRecoverySpace.TryGroundedPose(body, lanes[laneIndex].centerline[index], rotation,
                    .5f, body, null, out var destination))
                { Feedback = "NO CLEAR GROUNDED RIVAL GRID SLOT " + (i + 1); CleanupOpponents(); return false; }
                TruckTaxiRecoverySpace.Move(body, destination, rotation);
                Physics.SyncTransforms();
                var target = actor.GetComponent<TruckTaxiImpactTarget>() ?? actor.AddComponent<TruckTaxiImpactTarget>();
                target.kind = TaxiImpactKind.Traffic;
                target.targetId = (IsFlagship ? "race:flagship:" : "race:thunder-bowl:") + i;
                opponent.CountdownLock = new TruckTaxiRaceCountdownLock(body);
                opponent.LaneIndex = laneIndex; opponent.LastPosition = body.position;
                opponent.LastMovedAt = Time.time; opponent.Progress = NewProgress();
            }
            return true;
        }

        private void RecoverOpponent(Opponent opponent, float now)
        {
            if ((opponent.Body.position - playerBody.position).sqrMagnitude < 25 * 25) return;
            var lane = lanes[opponent.LaneIndex];
            int nearest = 1; float best = float.PositiveInfinity;
            for (int i = 1; !IsFlagship && i < lane.centerline.Length - 1; i++)
            {
                float distance = (lane.centerline[i] - opponent.Body.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = i; }
            }
            if (IsFlagship) nearest = flagshipRoute.RecoveryIndex(opponent.Progress.NextCheckpoint);
            var body = opponent.Body;
            if (body == null) return;
            opponent.Recoveries++;
            if (regionalWorld != null && IsFlagship && !regionalWorld.IsPositionAvailable(lane.centerline[nearest])) return;
            Quaternion facing = TrackFacing(nearest);
            if (!TruckTaxiRecoverySpace.TryGroundedPose(body, lane.centerline[nearest], facing, .5f,
                body, null, out var target) || (target - playerBody.position).sqrMagnitude < 25 * 25) return;
            var before = new TruckTaxiRecoverySpace.BodySnapshot(body);
            try
            {
                var component = opponent.MovePath;
                var initialize = component != null ? component.GetType().GetMethod("InitStartPosition") : null;
                var look = component != null ? component.GetType().GetMethod("SetLookPosition") : null;
                if (initialize == null || look == null) throw new InvalidOperationException("UTS recovery API unavailable.");
                initialize.Invoke(component, new object[] { 0, nearest, true, true });
                look.Invoke(component, null);
            }
            catch (Exception error)
            {
                Debug.LogWarning("UTS race path recovery failed: " + error.Message, this);
                before.Restore(); Physics.SyncTransforms();
                opponent.EliminatedAtLap = 1; opponent.Actor.SetActive(false);
                return;
            }
            TruckTaxiRecoverySpace.Move(body, target, facing);
            Physics.SyncTransforms();
            opponent.Progress.Invalidate(body.position, now);
            opponent.LastPosition = body.position; opponent.LastMovedAt = now;
        }

        private bool MissedGate(Opponent opponent)
        {
            int gate = opponent.Progress.NextCheckpoint;
            Vector3 offset = opponent.Body.position - gates[gate];
            return offset.sqrMagnitude < 200 * 200 && Vector3.Dot(offset, flagshipRoute.GateForwards[gate]) > 20;
        }

        private bool RivalRoadReady(Opponent opponent, float now)
        {
            if (regionalWorld == null) return true;
            bool ready = regionalWorld.IsPositionAvailable(opponent.Body.position) &&
                regionalWorld.IsPositionAvailable(gates[opponent.Progress.NextCheckpoint]);
            if (!ready)
            {
                if (opponent.StreamingLock == null)
                {
                    opponent.Driver.enabled = false;
                    opponent.StreamingLock = new TruckTaxiRaceCountdownLock(opponent.Body);
                    opponent.WaitingSince = now;
                }
                opponent.StreamingLock.Hold();
                if (now - opponent.WaitingSince >= 30)
                {
                    opponent.StreamingLock.Dispose(); opponent.StreamingLock = null;
                    opponent.EliminatedAtLap = 1; opponent.Actor.SetActive(false);
                }
                return false;
            }
            if (opponent.StreamingLock != null)
            {
                opponent.StreamingLock.Dispose(); opponent.StreamingLock = null;
                opponent.Progress.Reanchor(opponent.Body.position);
                opponent.LastMovedAt = now; opponent.Driver.enabled = true;
            }
            return true;
        }

        private Vector3 FindGridPoint(int slot) => IsFlagship ? centerline[flagshipRoute.GridIndex(slot)] : TruckTaxiRaceGrid.LanePoint(centerline,
            TruckTaxiRaceGrid.Index(centerline.Length, slot), TruckTaxiRaceGrid.Lane(slot));

        private Quaternion TrackFacing(int index)
        {
            Vector3 forward = centerline[(index + 1) % centerline.Length] - centerline[index];
            forward.y = 0;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private bool PlacePlayerAt(Vector3 center, bool grid)
        {
            if (!RequirePlayerEligibility()) return false;
            if (IsFlagship && regionalWorld != null && !regionalWorld.IsPositionAvailable(center)) return false;
            Physics.SyncTransforms();
            int index = 0; float nearest = float.PositiveInfinity;
            for (int i = 0; i < centerline.Length; i++)
            {
                float distance = (centerline[i] - center).sqrMagnitude;
                if (distance < nearest) { nearest = distance; index = i; }
            }
            Quaternion rotation = TrackFacing(index);
            if (!TruckTaxiRecoverySpace.TryGroundedPose(playerBody, center, rotation, 1.6f,
                playerBody, null, out var destination)) return false;
            TruckTaxiRecoverySpace.Move(playerBody, destination, rotation);
            Physics.SyncTransforms(); host.Session.DiscardTeleportDistance();
            if (!grid) playerProgress.Invalidate(destination, Time.time);
            return true;
        }

        public bool ResetToTrack()
        {
            if (IsActive && !RequirePlayerEligibility()) return false;
            if (!IsActive || countdown || host.Paused) return false;
            if (IsFlagship)
            {
                if (!PlacePlayerAt(centerline[flagshipRoute.RecoveryIndex(playerProgress.NextCheckpoint)], false))
                { Feedback = "NEXT CHECKPOINT HAS NO CLEAR LOADED ROAD FOR RECOVERY"; return false; }
                penaltySeconds += settings.resetPenaltySeconds;
                Publish("race-reset", "Checkpoint retained. Recovery +" + settings.resetPenaltySeconds.ToString("0") + "s", TruckTaxiAlertPriority.Normal, 4);
                return true;
            }
            int[] authoredIndices = { 0, 9, 18, 27, 36, 45 };
            var candidates = new List<Vector3>(12);
            foreach (int gate in authoredIndices)
                for (int lane = 0; lane < 2; lane++)
                    candidates.Add(TruckTaxiRaceGrid.LanePoint(centerline, (gate + centerline.Length - 1) % centerline.Length, lane));
            candidates.Sort((a, b) => (a - playerBody.position).sqrMagnitude.CompareTo((b - playerBody.position).sqrMagnitude));
            bool placed = false;
            foreach (var candidate in candidates)
                if (PlacePlayerAt(candidate, false)) { placed = true; break; }
            if (!placed)
            { Publish("race-reset", "No safe track surface found for reset.", TruckTaxiAlertPriority.High, 4); return false; }
            penaltySeconds += settings.resetPenaltySeconds;
            Publish("race-reset", "Lap invalidated. Reset penalty +" + settings.resetPenaltySeconds.ToString("0") + "s", TruckTaxiAlertPriority.Normal, 4);
            return true;
        }

        private void EliminateLast()
        {
            Opponent last = null; float lowest = float.PositiveInfinity;
            foreach (var opponent in opponents)
            {
                if (opponent.EliminatedAtLap > 0 || opponent.Body == null) continue;
                float score = Score(opponent.Progress, opponent.Body.position);
                if (score < lowest) { lowest = score; last = opponent; }
            }
            if (last == null) return;
            if (Score(playerProgress, playerBody.position) < lowest)
            { CancelRace("Eliminated from Thunder Bowl race."); return; }
            last.EliminatedAtLap = playerProgress.CompletedLaps;
            if (last.Driver != null) last.Driver.enabled = false;
            last.Actor.SetActive(false);
        }

        private void FinishRace()
        {
            float total = Time.time - startTime + penaltySeconds;
            int impacts = Mathf.Max(0, host.Session.TrafficHits - startTrafficHits);
            int chaos = Mode == TruckTaxiRaceMode.ChaosRace ? impacts * 25 : 0;
            long prize = settings.Prize(Mode, Position, playerProgress.BestLapSeconds) + chaos * 20;
            var damage = host.Player.GetComponent<DamageHandler>();
            long damageCost = damage != null ? (long)Math.Ceiling(Mathf.Max(0, damage.Damage - startDamage) * 5000) : 0;
            if (DebugModified) prize = 0;
            // Session currently stores an oval-specific best lap; do not overwrite it with endurance timing.
            else host.Session.RecordRaceResult(Position, IsFlagship ? 0 : playerProgress.BestLapSeconds, prize, chaos);
            resultText.text = (DebugModified ? "DEBUG RACE - NO RESULT RECORDED\n" : IsFlagship ? "HIGHWAY / CITY / HIGHWAY RESULTS\n" : "THUNDER BOWL RESULTS\n") +
                $"POSITION  {Position}/{opponents.Count + 1}\nTOTAL  {Format(total)}\nBEST LAP  {Format(playerProgress.BestLapSeconds)}\n" +
                $"PRIZE  {TruckTaxiHud.Money(prize)}\nCHAOS  +{chaos}\nDAMAGE REPAIR EST.  {TruckTaxiHud.Money(damageCost)}\n" +
                $"MEDAL  {(Mode == TruckTaxiRaceMode.TimeTrial ? settings.Medal(playerProgress.BestLapSeconds) : "--")}\n" +
                $"RECORD  {host.Session.RaceWins} WINS / {host.Session.RacePodiums} PODIUMS";
            Publish("race-result", (IsFlagship ? "Flagship: P" : "Thunder Bowl: P") + Position + "  " + TruckTaxiHud.Money(prize), TruckTaxiAlertPriority.Normal, 7);
            EndRace();
            OpenModal(results);
        }

        public void LeaveRace()
        { if (IsActive) CancelRace("Thunder Bowl session ended."); }

        public void CancelRace(string reason)
        {
            if (!IsActive) return;
            Publish("race-ended", reason, TruckTaxiAlertPriority.High, 5);
            EndRace();
        }

        private void EndRace()
        {
            UnsubscribeOrigin();
            ReleaseCountdown();
            if (entryReserved) host?.Session?.RefundRaceEntry();
            entryReserved = false;
            countdown = false; IsActive = false;
            host?.Session?.ReleaseOfferSuppression(this);
            host?.Session?.DiscardTeleportDistance();
            if (raceHud != null) raceHud.gameObject.SetActive(false);
            CleanupOpponents();
        }

        private void CleanupOpponents()
        {
            foreach (var opponent in opponents)
            {
                opponent.CountdownLock?.Dispose();
                opponent.StreamingLock?.Dispose();
                if (opponent.Actor != null) { opponent.Actor.SetActive(false); Destroy(opponent.Actor); }
            }
            opponents.Clear();
            foreach (var root in pathRoots) if (root != null) { root.SetActive(false); Destroy(root); }
            pathRoots.Clear(); lanes.Clear(); paths.Clear();
        }

        private void UnsubscribeOrigin()
        {
            if (originSubscribed && worldOrigin != null) worldOrigin.OriginShiftStarting -= OriginShiftStarting;
            originSubscribed = false;
        }

        // Parent's central developer menu owns presentation. Mutating hooks never award a race result.
        public bool DebugStartFlagship()
        {
            if (!Debug.isDebugBuild || IsActive) return false;
            debugStartRequested = true;
            try { return BeginFlagshipRace(); }
            finally { debugStartRequested = false; }
        }

        public bool DebugRestartFlagship()
        {
            if (!Debug.isDebugBuild) return false;
            CancelRace("Developer race restart."); CloseRegistration();
            return DebugStartFlagship();
        }

        public bool DebugResetRace()
        {
            if (!Debug.isDebugBuild) return false;
            CancelRace("Developer race reset."); CloseRegistration(); return true;
        }

        public bool DebugSetRivalCount(int count)
        {
            if (!Debug.isDebugBuild || count < 0 || count > TruckTaxiFlagshipRoute.MaximumRivals) return false;
            debugRivalCount = count; return true;
        }

        public bool DebugTeleportToCheckpoint(int index)
        {
            if (IsActive && !RequirePlayerEligibility()) return false;
            if (!Debug.isDebugBuild || !IsActive || !IsFlagship || countdown || index < 0 || index >= gates.Length) return false;
            if (!PlacePlayerAt(centerline[flagshipRoute.RecoveryIndex(index)], false)) return false;
            DebugModified = true;
            playerProgress.DebugSelectCheckpoint(index, playerBody.position, Time.time);
            return true;
        }

        public bool DebugAdvanceCheckpoint() => DebugTeleportToCheckpoint((NextCheckpoint + 1) % gates.Length);
        public bool DebugBackCheckpoint() => DebugTeleportToCheckpoint((NextCheckpoint + gates.Length - 1) % gates.Length);

        public bool DebugRemoveRivals()
        {
            if (!Debug.isDebugBuild || !IsActive) return false;
            DebugModified = true; CleanupOpponents(); return true;
        }

        public bool DebugSpawnRivals()
        {
            if (!Debug.isDebugBuild || !IsActive || !IsFlagship) return false;
            if (!RequirePlayerEligibility()) return false;
            DebugModified = true; debugStartRequested = true;
            try
            {
                if (!PrepareOpponents(Mode)) return false;
                if (!countdown)
                    foreach (var rival in opponents)
                    {
                        rival.CountdownLock.Dispose(); rival.CountdownLock = null;
                        rival.Progress.Begin(rival.Body.position, Time.time); rival.Driver.enabled = true;
                    }
                return true;
            }
            catch (Exception error) { Feedback = "RIVAL SPAWN FAILED: " + error.Message; CleanupOpponents(); return false; }
            finally { debugStartRequested = false; }
        }

        public bool DebugCrashRival(int index)
        {
            if (!Debug.isDebugBuild || !IsActive || countdown || index < 0 || index >= opponents.Count) return false;
            var rival = opponents[index];
            if (rival.Body == null || !rival.Actor.activeInHierarchy || rival.StreamingLock != null) return false;
            DebugModified = true;
            rival.Body.AddTorque(rival.Body.transform.forward * 3, ForceMode.VelocityChange);
            return true;
        }

        public bool DebugForceFinish()
        {
            if (!Debug.isDebugBuild || !IsActive || countdown) return false;
            DebugModified = true; FinishRace(); return true;
        }

        public string RivalCheckpointDiagnostics()
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < opponents.Count; i++)
            {
                var rival = opponents[i];
                text.Append(i + 1).Append(": CP ").Append((rival.Progress?.NextCheckpoint ?? 0) + 1)
                    .Append('/').Append(gates.Length).Append(" recovery ").Append(rival.Recoveries)
                    .Append(rival.FinishedAt > 0 ? " FINISHED" : rival.EliminatedAtLap > 0 ? " DNF" :
                        rival.StreamingLock != null ? " WAITING FOR ROAD" : " RACING").AppendLine();
            }
            return text.ToString();
        }

        private void Publish(string key, string message, TruckTaxiAlertPriority priority, float seconds)
        { host?.hud?.Alerts?.Publish(key, message, priority, seconds); }

        private static string Format(float seconds) => seconds <= 0 ? "--:--.--" :
            TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.ff");

        private void OnDestroy()
        {
            CloseRegistration(); EndRace();
            if (registration != null) Destroy(registration);
            if (menu != null) Destroy(menu.gameObject);
            if (results != null) Destroy(results.gameObject);
            if (raceHud != null) Destroy(raceHud.gameObject);
        }

        private void OnDisable()
        {
            CloseRegistration(); EndRace();
            if (registration != null) registration.SetActive(false);
        }
    }
}
