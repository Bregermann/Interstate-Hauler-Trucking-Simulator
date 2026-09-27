using System.Collections;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DefaultExecutionOrder(400)]
    public sealed class TruckTaxiBootstrap : MonoBehaviour
    {
        public static TruckTaxiBootstrap Instance { get; private set; }
        public TruckTaxiConfiguration configuration;
        public LwsPlayerTruckSpawner spawner;
        public LwsRoadGraphProvider roadGraph;
        public TruckTaxiTrafficAdapter traffic;
        public TruckTaxiHud hud;
        public Transform playerSpawn;
        public TruckTaxiPedestrianPopulation pedestrians;
        public TruckTaxiSession Session { get; private set; }
        public TruckTaxiConfiguration Configuration => configuration;
        public LwsPlayerTruck Player { get; private set; }
        public TruckTaxiGPSAdapter GPS { get; private set; }
        public TruckTaxiRouteDistanceService RouteDistances { get; private set; }
        public TruckTaxiVehicleHandlingOverride Handling { get; private set; }
        public TruckTaxiPassengerRuntime Passengers { get; private set; }
        public TruckTaxiPickupZoneVisualizer PickupZone { get; private set; }
        public TruckTaxiOptionalStops OptionalStops { get; private set; }
        public TruckTaxiAudioController Audio { get; private set; }
        public TruckTaxiSteeringWheelVisual SteeringVisual { get; private set; }
        public TruckTaxiEnvironmentCoordinator Environment { get; private set; }
        public TruckTaxiDriverNeedsCoordinator DriverNeeds { get; private set; }
        public TruckTaxiFuelController Fuel { get; private set; }
        public TruckTaxiRoadsideAssistance Roadside { get; private set; }
        public TruckTaxiRoadsideCompanionLoop Companions { get; private set; }
        public TruckTaxiVehicleObjectiveCoordinator VehicleObjectives { get; private set; }
        public bool Ready { get; private set; }
        public bool Paused { get; private set; }
        private ILwsGameplayStateService gameplay;
        private Rigidbody body;
        private AudioSource audioSource;
        private TruckTaxiRideLocation[] locations;
        private TruckTaxiImpactTarget[] resetTargets;
        private TruckTaxiState observedState = (TruckTaxiState)(-1);
        private float lastSpeed;
        private readonly RaycastHit[] patienceObstacles=new RaycastHit[16];
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private IEnumerator Start()
        {
            if (configuration == null || spawner == null || roadGraph == null || LwsApplicationBootstrap.Instance == null)
            { Debug.LogError("Truck Taxi scene configuration incomplete.",this); yield break; }
            if (LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsSaveService careerSave))
            { Debug.LogError("Truck Taxi requires the drivingSandbox bootstrap to protect career saves.",this); yield break; }
            LwsApplicationBootstrap.Instance.Registry.TryGet(out gameplay);
            var regional = GetComponent<TruckTaxiRegionalWorld>();
            if (regional != null)
            {
                yield return regional.PrepareInitialWorld(playerSpawn.position);
                if (!regional.InitialWorldReady) yield break;
            }
            Player = spawner.SpawnValidationRig();
            if (Player == null) yield break;
            body = Player.GetComponent<Rigidbody>();
            regional?.BindPlayer(Player.transform);
            locations = FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None);
            System.Array.Sort(locations,(a,b)=>string.CompareOrdinal(a.locationId,b.locationId));
            resetTargets = FindObjectsByType<TruckTaxiImpactTarget>(FindObjectsSortMode.None);
            RouteDistances = new TruckTaxiRouteDistanceService(roadGraph.Graph);
            Session = new TruckTaxiSession(configuration,locations,Random.Range(1,int.MaxValue),RouteDistances,()=>body.position);
            if (regional != null) Session.RegionResolver = regional.ResolveRegion;
            if (regional != null)
            {
                traffic.SetRegionAvailability(regional.IsPositionAvailable);
                pedestrians.SetRegionAvailability(regional.IsPositionAvailable);
                pedestrians.RegisterLogicalRegion("taxi.town01", new Bounds(Vector3.zero,new Vector3(700,20,700)));
                pedestrians.RegisterLogicalRegion("taxi.town02", new Bounds(new Vector3(4000,0,0),new Vector3(700,20,700)));
                regional.AvailabilityChanged += OnRegionalAvailabilityChanged;
            }
            GPS = gameObject.AddComponent<TruckTaxiGPSAdapter>(); GPS.Initialize(Player.transform,roadGraph);
            var sensor = Player.GetComponent<TruckTaxiCollisionObserver>() ?? Player.gameObject.AddComponent<TruckTaxiCollisionObserver>();
            sensor.Initialize(this);
            Audio=gameObject.AddComponent<TruckTaxiAudioController>(); Audio.Initialize(configuration.audio);
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
            Audio.Route(audioSource,TruckTaxiAudioCategory.UI);
            Session.RequestResolved += OnRequestResolved;
            Session.RequestCreated += OnRequestCreated;
            Session.DrivingEvent += OnDrivingEvent;
            Session.Changed += OnSessionChanged;
            Session.BehaviorReaction+=OnBehaviorReaction;
            Session.DiversionOffered+=OnDiversionOffered;
            Session.DestinationChanged+=OnDesiredDestinationChanged;
            Session.IsLegitimateTrafficOrServiceStop=IsLegitimateWait;
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(System.Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="-truck-taxi-baseline-population"))
            {
                traffic.ConfigureBaselineForValidation(traffic.densityProfile);
                pedestrians.ConfigureBaselineForValidation(pedestrians.densityProfile);
            }
            #endif
            traffic.Initialize();
            pedestrians.Initialize(configuration.pedestrianImpact);
            // The existing development HUD auto-creates in Editor builds, even without its service.
            // Hide only that instance in this isolated scene; the taxi HUD owns these surfaces.
            foreach (var root in FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsSortMode.None)) root.gameObject.SetActive(false);
            yield return null;
            Handling = Player.gameObject.AddComponent<TruckTaxiVehicleHandlingOverride>();
            Handling.Initialize(Player.GetComponent<NWH.VehiclePhysics2.VehicleController>());
            SteeringVisual=Player.GetComponent<TruckTaxiSteeringWheelVisual>() ?? Player.gameObject.AddComponent<TruckTaxiSteeringWheelVisual>();
            SteeringVisual.Initialize(Player.GetComponent<NWH.VehiclePhysics2.VehicleController>());
            Audio.RouteVehicle(Player.GetComponent<NWH.VehiclePhysics2.VehicleController>());
            Fuel=GetComponent<TruckTaxiFuelController>() ?? gameObject.AddComponent<TruckTaxiFuelController>();
            Fuel.Initialize(this);
            GPS.ConfigureDemoPresentation();
            regional?.RegisterMapMetadata(GPS.MapMarkers);
            Passengers=gameObject.AddComponent<TruckTaxiPassengerRuntime>(); Passengers.Initialize(this);
            PickupZone=gameObject.AddComponent<TruckTaxiPickupZoneVisualizer>(); PickupZone.Initialize(this);
            OptionalStops=gameObject.AddComponent<TruckTaxiOptionalStops>(); OptionalStops.Initialize(this);
            RebuildObjectiveCapabilities();
            Session.RefreshObjectiveSupport=RefreshDynamicObjectiveSupport;
            VehicleObjectives=GetComponent<TruckTaxiVehicleObjectiveCoordinator>() ?? gameObject.AddComponent<TruckTaxiVehicleObjectiveCoordinator>();
            VehicleObjectives.Initialize(this);
            Ready = true;
            hud.Initialize(this);
            Environment=GetComponent<TruckTaxiEnvironmentCoordinator>();
            if(Environment!=null && Environment.Initialize(this))
            {
                Environment.WeatherAudioRootAvailable+=Audio.RouteWorldTree;
                if(Environment.WeatherAudioRoot!=null) Audio.RouteWorldTree(Environment.WeatherAudioRoot);
                DriverNeeds=GetComponent<TruckTaxiDriverNeedsCoordinator>() ?? gameObject.AddComponent<TruckTaxiDriverNeedsCoordinator>();
                if(DriverNeeds.Initialize(this,Environment)) hud.InitializeEnvironment(Environment,DriverNeeds);
            }
            Roadside = GetComponent<TruckTaxiRoadsideAssistance>() ?? gameObject.AddComponent<TruckTaxiRoadsideAssistance>();
            Roadside.Initialize(this);
            if(DriverNeeds?.State!=null)
            {
                Companions=GetComponent<TruckTaxiRoadsideCompanionLoop>() ?? gameObject.AddComponent<TruckTaxiRoadsideCompanionLoop>();
                var adult=System.Array.Find(configuration.passengerDatabase.passengers,
                    p=>p!=null && p.explicitlyAdult && p.minimumAdultAge>=21 && p.adultFemalePresentation && p.seatProfile!=null);
                Companions.Initialize(this,Player.transform,adult!=null ? adult.seatProfile : null);
            }
            hud.InitializeIntegratedControls();
            SetPaused(true);
            OnSessionChanged();
        }
        private void Update()
        {
            if (!Ready || body == null) return;
            if(Fuel?.IsRescuing==true || Roadside?.IsRecovering==true) { lastSpeed=0; return; }
            if(Environment?.Clock!=null)
            {
                var clock=Environment.Clock.CurrentSnapshot;
                Session.SetWorldConditions(clock.totalGameSeconds/60d,clock.timeOfDayHours,DemandWeather(Environment.CurrentTaxiWeatherId));
            }
            if(Paused)
            {
                // Offers keep their existing expiry while the modal suppresses driving/simulation.
                if(Session.State==TruckTaxiState.RideOffered) Session.Tick(Time.unscaledDeltaTime,body.position,0,0,true);
                return;
            }
            float speed = body.linearVelocity.magnitude;
            float acceleration = Time.deltaTime > 0 ? (speed-lastSpeed)/Time.deltaTime : 0;
            lastSpeed = speed;
            TruckTaxiSurface.TrySample(body.position,Player.transform,out bool onRoad);
            Session.Tick(Time.deltaTime,body.position,speed,acceleration,onRoad);
        }
        private static TruckTaxiDemandWeather DemandWeather(string id)
        {
            if(id==TruckTaxiSnow.BlizzardId) return TruckTaxiDemandWeather.Blizzard;
            if(id==LwsWeatherPresetCatalog.HeavySnowId) return TruckTaxiDemandWeather.HeavySnow;
            if(id==LwsWeatherPresetCatalog.LightSnowId) return TruckTaxiDemandWeather.Snow;
            if(id==LwsWeatherPresetCatalog.ThunderstormId) return TruckTaxiDemandWeather.Storm;
            if(id==LwsWeatherPresetCatalog.LightRainId || id==LwsWeatherPresetCatalog.HeavyRainId) return TruckTaxiDemandWeather.Rain;
            return TruckTaxiDemandWeather.Neutral;
        }
        private void OnSessionChanged()
        {
            if (Session.State == observedState) return;
            observedState = Session.State;
            switch (Session.State)
            {
                case TruckTaxiState.RideOffered:
                    if(Session.Offer?.TryClaimNotification()==true) Play(configuration.offerSound);
                    SetPaused(true); break;
                case TruckTaxiState.AppreciationOffer:
                case TruckTaxiState.AppreciationSequence: SetPaused(true); break;
                case TruckTaxiState.DrivingToPickup: GPS.SetPickupDestination(Session.Pickup); Play(configuration.acceptSound); SetPaused(false); break;
                case TruckTaxiState.PassengerBoarding: Play(configuration.boardingSound); break;
                case TruckTaxiState.DrivingToDestination:
                    GPS.SetRideDestination(Session.CurrentDesiredDestination);
                    break;
                case TruckTaxiState.PassengerExiting: Play(configuration.exitSound); break;
                case TruckTaxiState.RideComplete: GPS.ClearRideDestination(); Play(configuration.fareSound); SetPaused(true); break;
                case TruckTaxiState.RideFailed: GPS.ClearRideDestination(); SetPaused(true); break;
                case TruckTaxiState.PassengerEjected: GPS.ClearRideDestination(); SetPaused(false); break;
                case TruckTaxiState.Inactive:
                case TruckTaxiState.Available:
                    GPS.ClearRideDestination();
                    SetPaused(Session.State == TruckTaxiState.Inactive); break;
            }
        }
        private void OnRequestResolved(TaxiRequestProgress r)
        {
            if (r.State != TaxiRequestState.Succeeded) { Play(configuration.requestFailureSound); return; }
            if(!r.Definition.IsStop || DriverNeeds?.State==null) return;
            var d=r.Definition;
            if(d.rewardItemCount>0) DriverNeeds.State.AddItem(d.rewardItem,d.rewardItemCount);
            if(d.secondRewardItemCount>0) DriverNeeds.State.AddItem(d.secondRewardItem,d.secondRewardItemCount);
            if(d.rewardThirstRelief && Session.Passenger.explicitlyAdult && Session.Passenger.minimumAdultAge>=21)
                DriverNeeds.State.SatisfyThirst();
        }
        private void OnBehaviorReaction(TruckTaxiDialogueCategory category) =>
            Passengers?.Dialogue?.Speak(Session.Passenger,category,Session);
        private void OnDiversionOffered(TaxiRequestProgress request)
        {
            Play(configuration.requestSound);
            OnBehaviorReaction(TruckTaxiDialogueCategory.DiversionOffered);
            SetPaused(true);
        }
        private void OnDesiredDestinationChanged()
        {
            if(Session.HasPassenger && Session.ActiveStop==null) GPS.SetRideDestination(Session.CurrentDesiredDestination);
        }
        private bool IsLegitimateWait()
        {
            if(body==null || Player==null) return false;
            if(Fuel?.IsRescuing==true || Roadside?.IsRecovering==true) return true;
            if(body.linearVelocity.sqrMagnitude>9) return false;
            foreach(var service in TruckTaxiServicePoint.Points)
                if(service!=null && service.CanUse(body.position,body.linearVelocity.magnitude,.44704f)) return true;
            Vector3 front=Player.transform.position+Vector3.up+Player.transform.forward*3;
            if(traffic!=null && traffic.SignalStoppingDistance(front,Player.transform.forward,35)<35) return true;
            int count=Physics.SphereCastNonAlloc(front,2,Player.transform.forward,patienceObstacles,22,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=patienceObstacles[i].collider;
                if(hit==null || hit.transform.IsChildOf(Player.transform)) continue;
                if(hit.GetComponentInParent<TruckTaxiTrafficBehaviour>()!=null || hit.GetComponentInParent<TruckTaxiPedestrian>()!=null) return true;
            }
            return false;
        }
        private void OnRequestCreated(TaxiRequestProgress r) => Play(configuration.requestSound);
        private void OnDrivingEvent(TaxiEventType _) => Play(configuration.collisionSound);
        public void Play(AudioClip clip) { if(clip!=null && audioSource!=null) audioSource.PlayOneShot(clip); }
        public void StartShift() { Session?.StartShift(); SetPaused(false); }
        public void ReturnToMainMenu()
        {
            if(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid() ||
                !Application.CanStreamedLevelBeLoaded(TruckTaxiMainMenu.SceneName)) return;
            Session.EndShift();
            int goals=0,chaos=0;
            foreach(var ride in Session.RideHistory)
            { chaos+=ride.ChaosScore; foreach(var goal in ride.Goals) if(goal.State==TaxiRequestState.Succeeded) goals++; }
            TruckTaxiMainMenu.PublishSessionStats(new TruckTaxiMainMenuStats {
                rides=Session.CompletedRides, earningsCents=Session.ShiftEarnings, averageStars=(float)Session.DriverAverageRating,
                chaos=chaos, pedestriansHit=Session.PedestriansHit,trafficCollisions=Session.TrafficHits,
                passengersEjected=Session.EjectedRides,jugEvents=Session.JugsSucceeded+Session.JugsSpilled,
                bottlesThrown=Session.ContainersThrown,specialEvents=Session.SpecialStopsCompleted,objectivesCompleted=goals });
            SetPaused(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene(TruckTaxiMainMenu.SceneName);
        }
        public void SetPaused(bool value)
        {
            Paused = value;
            Environment?.SetSessionPaused(value);
            if (value) gameplay?.Pause("Truck Taxi menu"); else if(gameplay?.CurrentState == LwsGameplayState.Paused) gameplay.Resume("Truck Taxi driving");
            // Taxi pause is local presentation policy, not a second global state authority.
            Time.timeScale = value ? 0 : 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void TeleportNear(TruckTaxiRideLocation location)
        {
            if(Player==null || body==null || location==null) return;
            Teleport(location.StopPosition + Vector3.up*1.6f, location.truckStopPoint != null ? location.truckStopPoint.rotation : Quaternion.identity);
        }
        private void Teleport(Vector3 position, Quaternion rotation)
        {
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.position = position; body.rotation = rotation;
            Player.transform.SetPositionAndRotation(position,rotation);
            Physics.SyncTransforms(); Session?.DiscardTeleportDistance(); lastSpeed = 0;
        }
        public void ResetCity()
        {
            if(!Ready) return;
            Session.EndShift();
            traffic.ResetTraffic();
            pedestrians.ResetPopulation();
            foreach(var target in resetTargets) if(target!=null) target.ResetTarget();
            Teleport(playerSpawn.position,playerSpawn.rotation);
            Player.GetComponent<TruckTaxiCollisionObserver>().ResetTracking();
            Session.StartShift(); SetPaused(false);
        }
        public void SpawnPedestrian()
        {
            pedestrians.SpawnOne();
        }
        public void RebuildObjectiveCapabilities()
        {
            var caps=Session.Capabilities;
            caps.Register(TruckTaxiObjectiveCapability.Traffic,traffic.Ready ? traffic.ActiveCount : 0);
            caps.Register(TruckTaxiObjectiveCapability.Pedestrians,AvailablePedestrians());
            caps.Register(TruckTaxiObjectiveCapability.PedestrianHitDetection,Player.GetComponent<TruckTaxiCollisionObserver>()!=null ? 1 : 0);
            caps.Register(TruckTaxiObjectiveCapability.NearMiss,traffic.Ready && Player.GetComponent<TruckTaxiCollisionObserver>()!=null ? 1 : 0);
            int props=0,shortcuts=0,road=0,offroad=0;
            foreach(var target in FindObjectsByType<TruckTaxiImpactTarget>(FindObjectsSortMode.None))
                if(target.isActiveAndEnabled && target.kind==TaxiImpactKind.Property && !target.Damaged && target.GetComponent<Collider>()?.enabled==true) props++;
            foreach(var shortcut in FindObjectsByType<ShortcutTrigger>(FindObjectsSortMode.None))
                if(shortcut.isActiveAndEnabled && !shortcut.scenicPoint && shortcut.GetComponent<Collider>()?.enabled==true && shortcut.GetComponent<Collider>().isTrigger) shortcuts++;
            foreach(var surface in FindObjectsByType<TruckTaxiSurface>(FindObjectsSortMode.None))
                if(surface.GetComponentInChildren<Collider>()!=null) { if(surface.isRoad) road++; else offroad++; }
            caps.Register(TruckTaxiObjectiveCapability.DestructibleProps,props);
            caps.Register(TruckTaxiObjectiveCapability.Shortcuts,shortcuts);
            caps.Register(TruckTaxiObjectiveCapability.Offroad,road>0 ? offroad : 0);
            caps.Register(TruckTaxiObjectiveCapability.DestinationChange,locations.Length);
            caps.Stops.Clear(); int scenic=0,illicit=0,privateStops=0,food=0,racetracks=0;
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var point in FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None))
            {
                if(string.IsNullOrWhiteSpace(point.stableId) || !ids.Add(point.stableId) || point.radius<6 ||
                    !RouteDistances.Measure(Vector3.zero,point.Position).Navigable) continue;
                caps.Stops.Add(point);
                if(point.category==TruckTaxiStopCategory.Scenic) scenic++;
                if(point.category==TruckTaxiStopCategory.IllicitPickup) illicit++;
                if(point.category==TruckTaxiStopCategory.PrivateMeeting) privateStops++;
                if(point.category==TruckTaxiStopCategory.FoodStop) food++;
                if(point.category==TruckTaxiStopCategory.Racetrack) racetracks++;
            }
            caps.Stops.Sort((a,b)=>string.CompareOrdinal(a.stableId,b.stableId));
            caps.Register(TruckTaxiObjectiveCapability.ScenicStops,scenic);
            caps.Register(TruckTaxiObjectiveCapability.IllicitStops,illicit);
            caps.Register(TruckTaxiObjectiveCapability.PrivateStops,privateStops);
            caps.Register(TruckTaxiObjectiveCapability.FoodStops,food);
            caps.Register(TruckTaxiObjectiveCapability.Racetrack,racetracks);
        }
        private void RefreshDynamicObjectiveSupport()
        {
            var caps=Session.Capabilities;
            caps.Register(TruckTaxiObjectiveCapability.Traffic,traffic.Ready ? traffic.ActiveCount : 0);
            caps.Register(TruckTaxiObjectiveCapability.Pedestrians,AvailablePedestrians());
            int props=0;
            foreach(var target in resetTargets)
                if(target!=null && target.isActiveAndEnabled && target.kind==TaxiImpactKind.Property && !target.Damaged) props++;
            caps.Register(TruckTaxiObjectiveCapability.DestructibleProps,props);
        }
        private void OnRegionalAvailabilityChanged()
        {
            pedestrians.NotifyRegionChanged();
            if (!Ready) return;
            resetTargets = FindObjectsByType<TruckTaxiImpactTarget>(FindObjectsSortMode.None);
            RebuildObjectiveCapabilities();
            GetComponent<TruckTaxiSnowSurface>()?.RefreshStreamedSurfaces();
            GPS?.MapMarkers?.RefreshStreamedContent();
        }
        private int AvailablePedestrians()
        {
            int count=0;
            foreach(var pedestrian in pedestrians.People)
                if(pedestrian!=null && pedestrian.isActiveAndEnabled && !pedestrian.IsRagdoll && !pedestrian.HitEventSent) count++;
            return count;
        }
        private void OnDestroy()
        {
            if(Instance != this) return;
            var regional = GetComponent<TruckTaxiRegionalWorld>();
            if (regional != null) regional.AvailabilityChanged -= OnRegionalAvailabilityChanged;
            Time.timeScale = 1;
            if(Session!=null) { Session.Changed-=OnSessionChanged; Session.RequestResolved-=OnRequestResolved; Session.RequestCreated-=OnRequestCreated; Session.DrivingEvent-=OnDrivingEvent; }
            if(Session!=null) { Session.BehaviorReaction-=OnBehaviorReaction; Session.DiversionOffered-=OnDiversionOffered;
                Session.DestinationChanged-=OnDesiredDestinationChanged; Session.IsLegitimateTrafficOrServiceStop=null; }
            Instance = null;
        }
    }
}
