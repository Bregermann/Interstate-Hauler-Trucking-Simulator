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
        public bool Ready { get; private set; }
        public bool Paused { get; private set; }
        private ILwsGameplayStateService gameplay;
        private Rigidbody body;
        private AudioSource audioSource;
        private TruckTaxiRideLocation[] locations;
        private TruckTaxiImpactTarget[] resetTargets;
        private TruckTaxiState observedState = (TruckTaxiState)(-1);
        private float lastSpeed;
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
            Player = spawner.SpawnValidationRig();
            if (Player == null) yield break;
            body = Player.GetComponent<Rigidbody>();
            locations = FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None);
            System.Array.Sort(locations,(a,b)=>string.CompareOrdinal(a.locationId,b.locationId));
            resetTargets = FindObjectsByType<TruckTaxiImpactTarget>(FindObjectsSortMode.None);
            RouteDistances = new TruckTaxiRouteDistanceService(roadGraph.Graph);
            Session = new TruckTaxiSession(configuration,locations,Random.Range(1,int.MaxValue),RouteDistances,()=>body.position);
            GPS = gameObject.AddComponent<TruckTaxiGPSAdapter>(); GPS.Initialize(Player.transform,roadGraph);
            var sensor = Player.GetComponent<TruckTaxiCollisionObserver>() ?? Player.gameObject.AddComponent<TruckTaxiCollisionObserver>();
            sensor.Initialize(this);
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
            Session.RequestResolved += OnRequestResolved;
            Session.RequestCreated += OnRequestCreated;
            Session.DrivingEvent += OnDrivingEvent;
            Session.Changed += OnSessionChanged;
            traffic.Initialize();
            pedestrians.Initialize(configuration.pedestrianImpact);
            // The existing development HUD auto-creates in Editor builds, even without its service.
            // Hide only that instance in this isolated scene; the taxi HUD owns these surfaces.
            foreach (var root in FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsSortMode.None)) root.gameObject.SetActive(false);
            yield return null;
            Handling = Player.gameObject.AddComponent<TruckTaxiVehicleHandlingOverride>();
            Handling.Initialize(Player.GetComponent<NWH.VehiclePhysics2.VehicleController>());
            GPS.ConfigureDemoPresentation();
            Passengers=gameObject.AddComponent<TruckTaxiPassengerRuntime>(); Passengers.Initialize(this);
            PickupZone=gameObject.AddComponent<TruckTaxiPickupZoneVisualizer>(); PickupZone.Initialize(this);
            OptionalStops=gameObject.AddComponent<TruckTaxiOptionalStops>(); OptionalStops.Initialize(this);
            RebuildObjectiveCapabilities();
            Session.RefreshObjectiveSupport=RefreshDynamicObjectiveSupport;
            Ready = true;
            hud.Initialize(this);
            SetPaused(true);
            OnSessionChanged();
        }
        private void Update()
        {
            if (!Ready || body == null) return;
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
        private void OnSessionChanged()
        {
            if (Session.State == observedState) return;
            observedState = Session.State;
            switch (Session.State)
            {
                case TruckTaxiState.RideOffered: Play(configuration.offerSound); SetPaused(true); break;
                case TruckTaxiState.AppreciationOffer:
                case TruckTaxiState.AppreciationSequence: SetPaused(true); break;
                case TruckTaxiState.DrivingToPickup: GPS.SetPickupDestination(Session.Pickup); Play(configuration.acceptSound); SetPaused(false); break;
                case TruckTaxiState.PassengerBoarding: Play(configuration.boardingSound); break;
                case TruckTaxiState.DrivingToDestination:
                    GPS.SetRideDestination(Session.Destination);
                    break;
                case TruckTaxiState.PassengerExiting: Play(configuration.exitSound); break;
                case TruckTaxiState.RideComplete: GPS.ClearDestination(); Play(configuration.fareSound); SetPaused(true); break;
                case TruckTaxiState.RideFailed: GPS.ClearDestination(); SetPaused(true); break;
                case TruckTaxiState.PassengerEjected: GPS.ClearDestination(); SetPaused(false); break;
                case TruckTaxiState.Inactive:
                case TruckTaxiState.Available:
                    GPS.ClearDestination();
                    SetPaused(Session.State == TruckTaxiState.Inactive); break;
            }
        }
        private void OnRequestResolved(TaxiRequestProgress r) => Play(r.State == TaxiRequestState.Succeeded ? configuration.requestSuccessSound : configuration.requestFailureSound);
        private void OnRequestCreated(TaxiRequestProgress r) => Play(configuration.requestSound);
        private void OnDrivingEvent(TaxiEventType _) => Play(configuration.collisionSound);
        public void Play(AudioClip clip) { if(clip!=null && audioSource!=null) audioSource.PlayOneShot(clip); }
        public void StartShift() { Session?.StartShift(); SetPaused(false); }
        public void SetPaused(bool value)
        {
            Paused = value;
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
            caps.Stops.Clear(); int scenic=0,illicit=0,privateStops=0;
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var point in FindObjectsByType<TruckTaxiStopObjectivePoint>(FindObjectsSortMode.None))
            {
                if(string.IsNullOrWhiteSpace(point.stableId) || !ids.Add(point.stableId) || point.radius<6 ||
                    !RouteDistances.Measure(Vector3.zero,point.Position).Navigable) continue;
                caps.Stops.Add(point);
                if(point.category==TruckTaxiStopCategory.Scenic) scenic++;
                if(point.category==TruckTaxiStopCategory.IllicitPickup) illicit++;
                if(point.category==TruckTaxiStopCategory.PrivateMeeting) privateStops++;
            }
            caps.Stops.Sort((a,b)=>string.CompareOrdinal(a.stableId,b.stableId));
            caps.Register(TruckTaxiObjectiveCapability.ScenicStops,scenic);
            caps.Register(TruckTaxiObjectiveCapability.IllicitStops,illicit);
            caps.Register(TruckTaxiObjectiveCapability.PrivateStops,privateStops);
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
            Time.timeScale = 1;
            if(Session!=null) { Session.Changed-=OnSessionChanged; Session.RequestResolved-=OnRequestResolved; Session.RequestCreated-=OnRequestCreated; Session.DrivingEvent-=OnDrivingEvent; }
            Instance = null;
        }
    }
}
