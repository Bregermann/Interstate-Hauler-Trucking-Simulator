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
        public TruckTaxiCustomContentRunner CustomContent { get; private set; }
        public TruckTaxiRadio Radio { get; private set; }
        public TruckTaxiRaceRuntime Races { get; private set; }
        public TruckTaxiKaijuDirector Kaiju { get; private set; }
        public TruckTaxiSteeringWheelVisual SteeringVisual { get; private set; }
        public TruckTaxiEnvironmentCoordinator Environment { get; private set; }
        public TruckTaxiCalendarWeatherService CalendarWeather => Environment?.CalendarWeather;
        public TruckTaxiWorldCoordinator WorldCoordinator { get; private set; }
        public TruckTaxiVenueRuntime Venues { get; private set; }
        public TruckTaxiExtremeEventDirector Extremes { get; private set; }
        public TruckTaxiRivalPopulation Rivals { get; private set; }
        public TruckTaxiRivalTheft RivalTheft { get; private set; }
        public TruckTaxiDriverNeedsCoordinator DriverNeeds { get; private set; }
        public TruckTaxiPassengerBathroomNeed PassengerBathroom { get; private set; }
        public TruckTaxiCompanionPickupRequest CompanionPickup { get; private set; }
        public TruckTaxiFuelController Fuel { get; private set; }
        public TruckTaxiRoadsideAssistance Roadside { get; private set; }
        public TruckTaxiFastTravel FastTravel { get; private set; }
        public TruckTaxiRoadsideCompanionLoop Companions { get; private set; }
        public TruckTaxiVehicleObjectiveCoordinator VehicleObjectives { get; private set; }
        public TruckTaxiVibinPersistence Persistence { get; private set; }
        public TruckTaxiWorldLifeController WorldLife { get; private set; }
        public TruckTaxiPhysicalFacilities PhysicalFacilities { get; private set; }
        public TruckTaxiFacilityAmbience FacilityAmbience { get; private set; }
        public TruckTaxiComponentDamage ComponentDamage { get; private set; }
        public bool Ready { get; private set; }
        public TruckTaxiCareerEquipment CareerEquipment { get; private set; }
        public TruckTaxiMoralityWorldController Morality { get; private set; }
        public ITruckTaxiPlatformCapabilities PlatformCapabilities { get; private set; } = new TruckTaxiOfflinePlatformCapabilities();
        public void BindPlatformCapabilities(ITruckTaxiPlatformCapabilities capabilities) =>
            PlatformCapabilities = capabilities ?? new TruckTaxiOfflinePlatformCapabilities();
        public bool StartupComplete => !startupPending;
        public bool Paused { get; private set; }
        public bool CanResume => Ready && !startupPending && Persistence != null && !Persistence.ResumeBlocked;
        public string ResumeBlockReason => !Ready || startupPending ? "Vibin startup is not ready." :
            Persistence != null ? Persistence.ResumeBlockReason : "Vibin persistence is unavailable.";
        private bool startupPending = true;
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
            SetPaused(true);
            if (configuration == null || spawner == null || roadGraph == null || LwsApplicationBootstrap.Instance == null)
            { Debug.LogError("Truck Taxi scene configuration incomplete.",this); yield break; }
            if (LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsSaveService careerSave) &&
                !TruckTaxiVibinPersistence.IsVibinScope(careerSave))
            { Debug.LogError("Truck Taxi rejects non-Vibin save scopes to protect career saves.",this); yield break; }
            LwsApplicationBootstrap.Instance.Registry.TryGet(out gameplay);
            var regional = GetComponent<TruckTaxiRegionalWorld>();
            if (regional != null)
            {
                yield return regional.PrepareInitialWorld(playerSpawn.position);
                if (!regional.InitialWorldReady) yield break;
            }
            Player = spawner.SpawnValidationRig();
            if (Player == null) yield break;
            if(!TruckTaxiEightOccupantLayout.TryApplyToConfiguredStarter(Player,out string layoutReason))
                Debug.LogWarning("Truck Taxi starter occupant layout not applied: "+layoutReason,Player);
            body = Player.GetComponent<Rigidbody>();
            regional?.BindPlayer(Player.transform);
            locations = FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None);
            System.Array.Sort(locations,(a,b)=>string.CompareOrdinal(a.locationId,b.locationId));
            resetTargets = FindObjectsByType<TruckTaxiImpactTarget>(FindObjectsSortMode.None);
            ConfigureBreakawayProps();
            RouteDistances = new TruckTaxiRouteDistanceService(roadGraph.Graph);
            Session = new TruckTaxiSession(configuration,locations,Random.Range(1,int.MaxValue),RouteDistances,()=>body.position);
            if (regional != null) Session.RegionResolver = regional.ResolveRideTown;
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
            TruckTaxiSignificantCollisionDiagnostic.Attach(Player.gameObject);
            Audio=gameObject.AddComponent<TruckTaxiAudioController>(); Audio.Initialize(configuration.audio);
            CustomContent=GetComponent<TruckTaxiCustomContentRunner>() ?? gameObject.AddComponent<TruckTaxiCustomContentRunner>();
            CustomContent.Initialize(this);
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
            Passengers.ConfigureCompanionReactions(Configuration.companionReactions);
            Passengers.CompanionReactionRequested += reaction =>
                Passengers.Dialogue.SpeakAuthoredText(reaction.passenger.passengerName, reaction.subtitle);
            PickupZone=gameObject.AddComponent<TruckTaxiPickupZoneVisualizer>(); PickupZone.Initialize(this);
            OptionalStops=gameObject.AddComponent<TruckTaxiOptionalStops>(); OptionalStops.Initialize(this);
            RebuildObjectiveCapabilities();
            Session.RefreshObjectiveSupport=RefreshDynamicObjectiveSupport;
            VehicleObjectives=GetComponent<TruckTaxiVehicleObjectiveCoordinator>() ?? gameObject.AddComponent<TruckTaxiVehicleObjectiveCoordinator>();
            VehicleObjectives.Initialize(this);
            hud.Initialize(this);
            Radio=GetComponent<TruckTaxiRadio>() ?? gameObject.AddComponent<TruckTaxiRadio>(); Radio.Initialize(this);
            Environment=GetComponent<TruckTaxiEnvironmentCoordinator>();
            if(Environment!=null && Environment.Initialize(this))
            {
                Environment.WeatherAudioRootAvailable+=Audio.RouteWorldTree;
                if(Environment.WeatherAudioRoot!=null) Audio.RouteWorldTree(Environment.WeatherAudioRoot);
                DriverNeeds=GetComponent<TruckTaxiDriverNeedsCoordinator>() ?? gameObject.AddComponent<TruckTaxiDriverNeedsCoordinator>();
                if(DriverNeeds.Initialize(this,Environment))
                { hud.InitializeEnvironment(Environment,DriverNeeds); OptionalStops.ConfigureScenicThirstRelief(DriverNeeds.State.RelieveThirst); }
            }
            ComponentDamage = Player.GetComponent<TruckTaxiComponentDamage>() ?? Player.gameObject.AddComponent<TruckTaxiComponentDamage>();
            ComponentDamage.Initialize(this);
            TruckTaxiVideoSettings.ApplyStartup(this);
            Roadside = GetComponent<TruckTaxiRoadsideAssistance>() ?? gameObject.AddComponent<TruckTaxiRoadsideAssistance>();
            Roadside.Initialize(this);
            FastTravel = GetComponent<TruckTaxiFastTravel>() ?? gameObject.AddComponent<TruckTaxiFastTravel>();
            FastTravel.Initialize(this);
            if(DriverNeeds?.State!=null)
            {
                Companions=GetComponent<TruckTaxiRoadsideCompanionLoop>() ?? gameObject.AddComponent<TruckTaxiRoadsideCompanionLoop>();
                var adult=System.Array.Find(configuration.passengerDatabase.passengers,
                    p=>p!=null && p.explicitlyAdult && p.minimumAdultAge>=21 && p.adultFemalePresentation && p.seatProfile!=null);
                Companions.Initialize(this,Player.transform,adult!=null ? adult.seatProfile : null);
                PassengerBathroom=GetComponent<TruckTaxiPassengerBathroomNeed>() ?? gameObject.AddComponent<TruckTaxiPassengerBathroomNeed>();
                PassengerBathroom.Initialize(this);
                CompanionPickup=GetComponent<TruckTaxiCompanionPickupRequest>() ?? gameObject.AddComponent<TruckTaxiCompanionPickupRequest>();
                CompanionPickup.Initialize(this);
                Session.JugGoalContextProvider=CaptureJugGoalContext;
            }
            hud.InitializeIntegratedControls();
            WorldLife = GetComponent<TruckTaxiWorldLifeController>() ?? gameObject.AddComponent<TruckTaxiWorldLifeController>();
            WorldLife.Initialize(this, hud);
            PhysicalFacilities = GetComponent<TruckTaxiPhysicalFacilities>() ?? gameObject.AddComponent<TruckTaxiPhysicalFacilities>();
            PhysicalFacilities.Initialize(this);
            FacilityAmbience = GetComponent<TruckTaxiFacilityAmbience>() ?? gameObject.AddComponent<TruckTaxiFacilityAmbience>();
            FacilityAmbience.Initialize(this);
            if(CalendarWeather!=null)
            {
                Venues=GetComponent<TruckTaxiVenueRuntime>() ?? gameObject.AddComponent<TruckTaxiVenueRuntime>();
                Extremes=GetComponent<TruckTaxiExtremeEventDirector>() ?? gameObject.AddComponent<TruckTaxiExtremeEventDirector>();
                Rivals=traffic.GetComponent<TruckTaxiRivalPopulation>() ?? traffic.gameObject.AddComponent<TruckTaxiRivalPopulation>();
                WorldCoordinator=GetComponent<TruckTaxiWorldCoordinator>() ?? gameObject.AddComponent<TruckTaxiWorldCoordinator>();
                WorldCoordinator.Initialize(this);
                RivalTheft=GetComponent<TruckTaxiRivalTheft>() ?? gameObject.AddComponent<TruckTaxiRivalTheft>();
                RivalTheft.Initialize(this,Rivals);
                Races=GetComponent<TruckTaxiRaceRuntime>() ?? gameObject.AddComponent<TruckTaxiRaceRuntime>();
                Races.Initialize(this);
                Kaiju=GetComponent<TruckTaxiKaijuDirector>() ?? gameObject.AddComponent<TruckTaxiKaijuDirector>();
                Kaiju.Initialize(this);
                WorldCoordinator.BindKaiju(Kaiju);
                FastTravel.AdditionalBlockReason=()=>Kaiju.IsCapturingPlayer ? "Wait until vehicle recovery completes." : Races.BlockTravelReason;
            }
            SetPaused(true);
            Ready = true;
            CareerEquipment = GetComponent<TruckTaxiCareerEquipment>() ?? gameObject.AddComponent<TruckTaxiCareerEquipment>();
            CareerEquipment.Initialize(this);
            Morality = GetComponent<TruckTaxiMoralityWorldController>() ?? gameObject.AddComponent<TruckTaxiMoralityWorldController>();
            if (!Morality.Initialize(this, hud)) Debug.LogWarning("Truck Taxi authored trailer events could not bind to the existing session/HUD.", this);
            var analytics = GetComponent<TruckTaxiAnalytics>() ?? gameObject.AddComponent<TruckTaxiAnalytics>();
            analytics.Initialize(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var spikes=GetComponent<TruckTaxiFrameSpikeDiagnostics>() ?? gameObject.AddComponent<TruckTaxiFrameSpikeDiagnostics>();
            spikes.Initialize(this);
#endif
            Persistence = GetComponent<TruckTaxiVibinPersistence>() ?? gameObject.AddComponent<TruckTaxiVibinPersistence>();
            var initialized = Persistence.Initialize(this);
            bool continuing = TruckTaxiVibinPersistence.HasContinueRequest;
            var startup = initialized.Succeeded
                ? continuing ? Persistence.ConsumeContinueRequest() : Persistence.NewGame()
                : initialized;
            startupPending = false;
            if (!startup.Succeeded) Debug.LogWarning("Truck Taxi startup blocked: " + startup.Message, this);
            OnSessionChanged();
            // Async loads own their pause until completion; an already completed load must stay drivable.
            SetPaused(!startup.Succeeded || Persistence.IsBusy);
            if (!continuing && startup.Succeeded) hud.Career?.OpenFirstDay();
        }
        private void Update()
        {
            if (!Ready || body == null) return;
            if (Persistence == null || Persistence.ResumeBlocked) { lastSpeed = 0; return; }
            if(Fuel?.IsRescuing==true || Roadside?.IsRecovering==true || FastTravel?.IsTravelling==true) { lastSpeed=0; return; }
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
            RivalTheft?.Tick(Time.deltaTime);
            Session.Tick(Time.deltaTime,body.position,speed,acceleration,onRoad);
            PassengerBathroom?.Tick(PassengerBathroom.CanOfferRequest);
            CompanionPickup?.Tick();
        }
        private TruckTaxiJugGoalContext CaptureJugGoalContext()
        {
            bool runtime = DriverNeeds != null && DriverNeeds.isActiveAndEnabled && DriverNeeds.CanInteract &&
                DriverNeeds.JugHoldAction?.enabled == true && DriverNeeds.JugCueAction?.enabled == true;
            bool safe = runtime && body != null && Vector3.Dot(Player.transform.up,Vector3.up) > .7f &&
                TruckTaxiSurface.TrySample(body.position,Player.transform,out _) &&
                PassengerBathroom?.HasAcceptedDiversion != true && CompanionPickup?.IsActive != true &&
                Fuel?.IsRescuing != true && Roadside?.IsRecovering != true && FastTravel?.IsTravelling != true &&
                Kaiju?.IsCapturingPlayer != true && CustomContent?.IsPlaying != true && Companions?.IsBusy != true;
            var clock = Environment?.Clock;
            float rate = clock != null && !Paused && !clock.CurrentSnapshot.paused ? clock.CurrentSnapshot.timeScale : 0;
            return TruckTaxiJugGoalPolicy.Capture(DriverNeeds?.State,Environment?.settings,rate,runtime,safe);
        }

        public bool CanEndPassengerDiversions(string reason)
        {
            if (PassengerBathroom != null && !PassengerBathroom.Cancel(reason)) return false;
            CompanionPickup?.Cancel();
            RivalTheft?.Cancel();
            return true;
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
            if(d.requestType!=TaxiRequestType.ScenicRoute && d.rewardThirstRelief && Session.Passenger.explicitlyAdult && Session.Passenger.minimumAdultAge>=21)
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
            if(PassengerBathroom?.IsLegitimateServiceDiversion==true || CompanionPickup?.IsLegitimateDiversion==true ||
                RivalTheft?.IsLegitimateDiversion==true) return true;
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
        public void StartShift()
        {
            if (!CanResume) { SetPaused(true); return; }
            Session?.StartShift(); SetPaused(false);
        }
        public void RetryStartup()
        {
            if (!Ready || startupPending || Persistence == null || Persistence.IsBusy) return;
            if (TruckTaxiVibinPersistence.HasContinueRequest) Persistence.RetryContinueRequest();
            else if (!Persistence.HasActiveRun) Persistence.NewGame();
            else if (CanResume) StartShift();
            else hud?.SaveLoad?.Open();
        }
        public void ReturnToMainMenu()
        {
            if(Persistence?.IsBusy==true || !UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid() ||
                !Application.CanStreamedLevelBeLoaded(TruckTaxiMainMenu.SceneName)) return;
            if (Persistence?.HasActiveRun == true && !Persistence.ResumeBlocked && !Persistence.SaveBeforeLeaving().Succeeded)
            {
                hud?.SaveLoad?.Open();
                return;
            }
            Session.EndShift();
            TruckTaxiMainMenu.PublishSessionStats(Session.CaptureMainMenuStats());
            SetPaused(true);
            UnityEngine.SceneManagement.SceneManager.LoadScene(TruckTaxiMainMenu.SceneName);
        }
        public void SetPaused(bool value)
        {
            if (!value && !CanResume) value = true;
            Paused = value;
            Environment?.SetSessionPaused(value);
            if (value) gameplay?.Pause("Truck Taxi menu"); else if(gameplay?.CurrentState == LwsGameplayState.Paused) gameplay.Resume("Truck Taxi driving");
            // Taxi pause is local presentation policy, not a second global state authority.
            Time.timeScale = value ? 0 : 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void TeleportNear(TruckTaxiRideLocation location)
        {
            if (!CanResume) return;
            if(Player==null || body==null || location==null) return;
            Teleport(location.StopPosition + Vector3.up*1.6f, location.truckStopPoint != null ? location.truckStopPoint.rotation : Quaternion.identity);
        }
        private void Teleport(Vector3 position, Quaternion rotation)
        {
            if(!CanEndPassengerDiversions("Player teleport requested.")) return;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.position = position; body.rotation = rotation;
            Player.transform.SetPositionAndRotation(position,rotation);
            Physics.SyncTransforms(); Session?.DiscardTeleportDistance(); lastSpeed = 0;
        }
        public void ResetCity()
        {
            if(!CanResume) return;
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
            CustomContent?.RefreshObjectiveCapabilities();
            var caps=Session.Capabilities;
            caps.Register(TruckTaxiObjectiveCapability.JugQte,DriverNeeds?.isActiveAndEnabled==true &&
                DriverNeeds.JugHoldAction?.enabled==true && DriverNeeds.JugCueAction?.enabled==true ? 1 : 0);
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
            ConfigureBreakawayProps();
            RebuildObjectiveCapabilities();
            GetComponent<TruckTaxiSnowSurface>()?.RefreshStreamedSurfaces();
            GPS?.MapMarkers?.RefreshStreamedContent();
            PhysicalFacilities?.Refresh();
        }
        private void ConfigureBreakawayProps()
        {
            if (resetTargets == null) return;
            foreach (var target in resetTargets)
            {
                if (target == null || target.kind != TaxiImpactKind.Property ||
                    target.GetComponent<TruckTaxiBreakawayProp>() != null) continue;
                var collider = target.GetComponent<Collider>();
                if (collider == null || collider.bounds.size.magnitude > 7f) continue;
                string label = target.name.ToLowerInvariant();
                if (label.Contains("concrete") || label.Contains("building") || label.Contains("wall") ||
                    label.Contains("large tree") || label.Contains("guardrail")) continue;
                var prop = target.gameObject.AddComponent<TruckTaxiBreakawayProp>();
                var category = label.Contains("cone") || label.Contains("trash") || label.Contains("mailbox") ?
                    TruckTaxiPropPhysicsCategory.Lightweight : label.Contains("sapling") || label.Contains("fence") ?
                    TruckTaxiPropPhysicsCategory.Destructible : TruckTaxiPropPhysicsCategory.Breakaway;
                prop.Configure(category, category == TruckTaxiPropPhysicsCategory.Lightweight ? 6f : 35f);
            }
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
                Session.DestinationChanged-=OnDesiredDestinationChanged; Session.IsLegitimateTrafficOrServiceStop=null; Session.JugGoalContextProvider=null; }
            Instance = null;
        }
    }
}
