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
        }

        public TruckTaxiRaceSettings settings = new TruckTaxiRaceSettings();

        [Tooltip("Use installed UTS Truck_1/Truck_2 prefabs, never an NWH player tractor.")]
        public GameObject[] utsTruckPrefabs = Array.Empty<GameObject>();

        public bool IsActive { get; private set; }
        public string BlockTravelReason => IsActive || MenuOpen ? "Leave the Thunder Bowl session before fast travel." : null;
        public bool MenuOpen => FocusRoot != null;
        public Transform FocusRoot => results != null && results.gameObject.activeSelf ? results :
            menu != null && menu.gameObject.activeSelf ? menu : null;
        public Transform MenuRoot => FocusRoot;
        public TruckTaxiRaceMode Mode { get; private set; }
        public int Position { get; private set; } = 1;
        public int OpponentCount => opponents.Count;
        public bool EventRaceAvailable => EventActive();
        public string Feedback { get; private set; } = "";

        private readonly LwsUtsTrafficApi uts = new LwsUtsTrafficApi();

        private readonly LwsTrafficSpawnPolicy spawnPolicy = new LwsTrafficSpawnPolicy
        {
            densityTier = LwsTrafficDensityTier.Off,
            targetCruiseSpeedScale = .9f,
            maximumTrafficSpeedMetersPerSecond = 27,
            autoResolveEditorPrefabs = false
        };

        private readonly List<Opponent> opponents = new List<Opponent>(7);
        private readonly List<GameObject> pathRoots = new List<GameObject>(2);
        private readonly List<LwsTrafficLaneDefinition> lanes = new List<LwsTrafficLaneDefinition>(2);
        private readonly List<Component> paths = new List<Component>(2);
        private readonly Vector3[] gates = TruckTaxiSpeedwayLayout.LapCheckpoints();
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
            playerBody = owner.Player.GetComponent<Rigidbody>();
            if (playerBody == null) throw new InvalidOperationException("NWH player tractor Rigidbody is missing.");
            centerline = TruckTaxiSpeedwayLayout.TrackCenterline().ToArray();
            playerProgress = new TruckTaxiRaceProgress(gates, settings);
            BuildUi(owner.hud);
            initialized = true;
        }

        private void BuildUi(TruckTaxiHud view)
        {
            registration = view.Button(view.Root, "RACE REGISTRATION", new Vector2(.73f, .34f),
                new Vector2(.99f, .40f), OpenRegistration);
            menu = view.Panel(view.Root, "Thunder Bowl Racing", new Vector2(.28f, .09f), new Vector2(.72f, .91f));
            var title = view.Text(menu, "Race title", new Vector2(.05f, .89f), new Vector2(.95f, .98f), 32);
            title.text = "THUNDER BOWL RACING";
            view.Button(menu, "CLOSE", new Vector2(.76f, .82f), new Vector2(.96f, .89f), CloseRegistration);
            string[] labels = { "OPEN PRACTICE", "TIME TRIAL", "TRUCK RACE", "ELIMINATION", "CHAOS RACE", "EVENT RACE" };
            for (int i = 0; i < labels.Length; i++)
            {
                int selected = i;
                float top = .79f - i * .105f;
                view.Button(menu, labels[i], new Vector2(.06f, top - .075f), new Vector2(.94f, top),
                    () => BeginRace((TruckTaxiRaceMode)selected));
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
                    registration.SetActive(nearby && !MenuOpen && !host.Paused && !host.hud.NormalHudSuppressed &&
                        host.Session.State == TruckTaxiState.Available && host.Companions?.IsBusy != true);
                }
                return;
            }
            if (host.Session.State != TruckTaxiState.Available || host.Player == null)
            { CancelRace("Race interrupted by taxi session state."); return; }
            if (Mode == TruckTaxiRaceMode.EventRace && Time.time >= nextUiRefresh && !EventActive())
            { CancelRace("Scheduled event interrupted. Race cancelled safely."); return; }
            if (host.Paused) return;
            if (countdown)
            {
                int remaining = Mathf.CeilToInt(countdownEnd - Time.time);
                raceStatus.text = remaining > 0 ? "THUNDER BOWL\n" + remaining : "THUNDER BOWL\nGO";
                if (Time.time < countdownEnd) return;
                ReleaseCountdown();
                host.Session.CommitRaceEntry();
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
            if (DistanceToTrack(playerPosition) > TruckTaxiSpeedwayLayout.TrackWidth * .5f + 5f)
                playerProgress.Invalidate(playerPosition, now);
            bool completed = playerProgress.Tick(playerPosition, now);
            for (int i = 0; i < opponents.Count; i++)
            {
                var opponent = opponents[i];
                if (opponent.Body == null || opponent.EliminatedAtLap > 0) continue;
                Vector3 position = opponent.Body.position;
                opponent.Progress.Tick(position, now);
                if ((position - opponent.LastPosition).sqrMagnitude > 16)
                { opponent.LastPosition = position; opponent.LastMovedAt = now; }
                else if (now - opponent.LastMovedAt > 12 && opponent.Recoveries < 2)
                    RecoverOpponent(opponent, now);
            }
            if (Mode == TruckTaxiRaceMode.Elimination && completed)
                EliminateLast();
            if (!IsActive) return;
            Position = 1;
            float playerScore = Score(playerProgress, playerPosition);
            foreach (var opponent in opponents)
                if (opponent.EliminatedAtLap == 0 && opponent.Body != null &&
                    Score(opponent.Progress, opponent.Body.position) > playerScore) Position++;
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

        public bool BeginRace(TruckTaxiRaceMode mode)
        {
            if (!initialized || !isActiveAndEnabled || IsActive || host.Session.State != TruckTaxiState.Available)
            { Feedback = "FINISH CURRENT RIDE BEFORE RACING"; RefreshMenu(); return false; }
            if (mode == TruckTaxiRaceMode.EventRace && !EventActive())
            { Feedback = "EVENT RACE IS NOT ON THE CALENDAR NOW"; RefreshMenu(); return false; }
            var hitch = host.Player.GetComponentInChildren<TrailerHitchModuleWrapper>(true);
            if (hitch != null && hitch.module.attached)
            { Feedback = "DETACH TRAILER BEFORE RACING"; RefreshMenu(); return false; }
            if (host.Fuel?.IsRescuing == true || host.Roadside?.IsRecovering == true || host.FastTravel?.IsTravelling == true)
            { Feedback = "TRUCK RECOVERY IS IN PROGRESS"; RefreshMenu(); return false; }
            if (host.GetComponent<TruckTaxiRegionalWorld>() is TruckTaxiRegionalWorld world &&
                !world.IsPositionAvailable(gates[0]))
            { Feedback = "THUNDER BOWL TRACK IS STILL STREAMING"; RefreshMenu(); return false; }
            if (DistanceToTrack(host.Player.transform.position) > 250)
            { Feedback = "REGISTER AT THUNDER BOWL PIT OFFICE"; RefreshMenu(); return false; }
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
                { Feedback = "NO SAFE UTS GRID; CHECK TRUCKS AND TRACK CLEARANCE"; return false; }
                playerCountdownLock = new TruckTaxiRaceCountdownLock(playerBody, host.Player.GetComponent<VehicleController>()?.input);
                long fee = needsAi ? Math.Max(0, settings.entryFeeCents) : 0;
                if (!host.Session.TryEnterRace(fee))
                { Feedback = "NOT ENOUGH CASH FOR ENTRY"; return false; }
                entered = true;
                host.Session.AcquireOfferSuppression(this);
                Mode = mode; targetLaps = settings.Laps(mode, timeTrialLaps);
                penaltySeconds = 0; Position = 1; IsActive = true;
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
                    ReleaseCountdown(); CleanupOpponents(); before.Restore(); Physics.SyncTransforms();
                    host.Session.DiscardTeleportDistance();
                    if (entered) host.Session.RefundRaceEntry();
                    host.Session.ReleaseOfferSuppression(this);
                    IsActive = countdown = false;
                    raceHud.gameObject.SetActive(false);
                    if (hadMenu && !MenuOpen) { OpenModal(menu); menuWasPaused = modalPreviousPause; }
                    host.SetPaused(wasPaused);
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
            if (!uts.IsAvailable || utsTruckPrefabs == null || utsTruckPrefabs.Length == 0) return false;
            foreach (var prefab in utsTruckPrefabs) if (!uts.PrefabLooksLikeUtsVehicle(prefab)) return false;
            int count = mode == TruckTaxiRaceMode.EventRace ? settings.eventOpponents : settings.normalOpponents;
            count = Mathf.Clamp(count, 3, 7);
            if (SystemInfo.processorCount <= 4) count = 3;
            for (int laneIndex = 0; laneIndex < 2; laneIndex++)
            {
                var points = new Vector3[centerline.Length];
                for (int i = 0; i < points.Length; i++)
                    points[i] = TruckTaxiRaceGrid.LanePoint(centerline, i, laneIndex);
                var lane = new LwsTrafficLaneDefinition
                {
                    laneId = "taxi.speedway.race." + laneIndex,
                    roadId = "taxi.speedway.track",
                    segmentId = "oval",
                    laneIndex = laneIndex,
                    centerline = points,
                    laneWidthMeters = 4,
                    speedLimitMph = 60,
                    spawnEnabled = false
                };
                var root = new GameObject("Thunder Bowl UTS race lane " + laneIndex);
                root.transform.SetParent(transform, false);
                var path = uts.CreatePath(root, lane, utsTruckPrefabs, spawnPolicy, out string message);
                if (path == null) { Debug.LogWarning(message, this); Destroy(root); CleanupOpponents(); return false; }
                lanes.Add(lane); paths.Add(path); pathRoots.Add(root);
            }
            for (int i = 0; i < count; i++)
            {
                int laneIndex = TruckTaxiRaceGrid.Lane(i + 1);
                int index = TruckTaxiRaceGrid.Index(centerline.Length, i + 1);
                GameObject prefab = utsTruckPrefabs[i % utsTruckPrefabs.Length];
                var personality = TruckTaxiRaceSettings.Personality(i);
                var driverPolicy = new LwsTrafficSpawnPolicy
                {
                    densityTier = LwsTrafficDensityTier.Off,
                    targetCruiseSpeedScale = TruckTaxiRaceSettings.SpeedScale(personality),
                    maximumTrafficSpeedMetersPerSecond = spawnPolicy.maximumTrafficSpeedMetersPerSecond,
                    autoResolveEditorPrefabs = false
                };
                GameObject actor = uts.SpawnVehicle(prefab, paths[laneIndex], lanes[laneIndex], index, transform, driverPolicy, out string message);
                if (actor == null) { Debug.LogWarning(message, this); CleanupOpponents(); return false; }
                Behaviour driver = null;
                foreach (var behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour.GetType().Name == "CarAIController") { driver = behaviour; break; }
                var body = driver != null ? driver.GetComponent<Rigidbody>() : null;
                if (body == null) { actor.SetActive(false); Destroy(actor); CleanupOpponents(); return false; }
                driver.enabled = false;
                Physics.SyncTransforms();
                Quaternion rotation = TrackFacing(index);
                if (!TruckTaxiRecoverySpace.TryGroundedPose(body, lanes[laneIndex].centerline[index], rotation,
                    .5f, body, null, out var destination))
                { actor.SetActive(false); Destroy(actor); CleanupOpponents(); return false; }
                TruckTaxiRecoverySpace.Move(body, destination, rotation);
                Physics.SyncTransforms();
                var target = actor.GetComponent<TruckTaxiImpactTarget>() ?? actor.AddComponent<TruckTaxiImpactTarget>();
                target.kind = TaxiImpactKind.Traffic;
                target.targetId = "race:thunder-bowl:" + i;
                opponents.Add(new Opponent
                {
                    Actor = actor,
                    Driver = driver,
                    Body = body,
                    CountdownLock = new TruckTaxiRaceCountdownLock(body),
                    LaneIndex = laneIndex,
                    LastPosition = body.position,
                    LastMovedAt = Time.time,
                    Progress = new TruckTaxiRaceProgress(gates, settings)
                });
            }
            return true;
        }

        private void RecoverOpponent(Opponent opponent, float now)
        {
            if ((opponent.Body.position - playerBody.position).sqrMagnitude < 25 * 25) return;
            var lane = lanes[opponent.LaneIndex];
            int nearest = 1; float best = float.PositiveInfinity;
            for (int i = 1; i < lane.centerline.Length - 1; i++)
            {
                float distance = (lane.centerline[i] - opponent.Body.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = i; }
            }
            var body = opponent.Body;
            if (body == null) return;
            Quaternion facing = TrackFacing(nearest);
            if (!TruckTaxiRecoverySpace.TryGroundedPose(body, lane.centerline[nearest], facing, .5f,
                body, null, out var target) || (target - playerBody.position).sqrMagnitude < 25 * 25) return;
            var before = new TruckTaxiRecoverySpace.BodySnapshot(body);
            try
            {
                foreach (var component in opponent.Actor.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component.GetType().Name == "MovePath")
                    {
                        component.GetType().GetMethod("InitStartPosition")?.Invoke(component, new object[] { 0, nearest, true, true });
                        component.GetType().GetMethod("SetLookPosition")?.Invoke(component, null);
                        break;
                    }
            }
            catch (Exception error)
            {
                Debug.LogWarning("UTS race path recovery failed: " + error.Message, this);
                before.Restore(); Physics.SyncTransforms();
                opponent.Recoveries = 2;
                return;
            }
            TruckTaxiRecoverySpace.Move(body, target, facing);
            Physics.SyncTransforms();
            opponent.Progress.Invalidate(body.position, now);
            opponent.Recoveries++;
            opponent.LastPosition = body.position; opponent.LastMovedAt = now;
        }

        private Vector3 FindGridPoint(int slot) => TruckTaxiRaceGrid.LanePoint(centerline,
            TruckTaxiRaceGrid.Index(centerline.Length, slot), TruckTaxiRaceGrid.Lane(slot));

        private Quaternion TrackFacing(int index)
        {
            Vector3 forward = centerline[(index + 1) % centerline.Length] - centerline[index];
            forward.y = 0;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private bool PlacePlayerAt(Vector3 center, bool grid)
        {
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
            if (!IsActive || countdown || host.Paused) return false;
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
            host.Session.RecordRaceResult(Position, playerProgress.BestLapSeconds, prize, chaos);
            resultText.text = "THUNDER BOWL RESULTS\n" +
                $"POSITION  {Position}/{opponents.Count + 1}\nTOTAL  {Format(total)}\nBEST LAP  {Format(playerProgress.BestLapSeconds)}\n" +
                $"PRIZE  {TruckTaxiHud.Money(prize)}\nCHAOS  +{chaos}\nDAMAGE REPAIR EST.  {TruckTaxiHud.Money(damageCost)}\n" +
                $"MEDAL  {(Mode == TruckTaxiRaceMode.TimeTrial ? settings.Medal(playerProgress.BestLapSeconds) : "--")}\n" +
                $"RECORD  {host.Session.RaceWins} WINS / {host.Session.RacePodiums} PODIUMS";
            Publish("race-result", "Thunder Bowl: P" + Position + "  " + TruckTaxiHud.Money(prize), TruckTaxiAlertPriority.Normal, 7);
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
            ReleaseCountdown();
            if (countdown) host?.Session?.RefundRaceEntry();
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
                if (opponent.Actor != null) { opponent.Actor.SetActive(false); Destroy(opponent.Actor); }
            }
            opponents.Clear();
            foreach (var root in pathRoots) if (root != null) Destroy(root);
            pathRoots.Clear(); lanes.Clear(); paths.Clear();
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
