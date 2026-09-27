using System;
using System.Collections;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiBusRoute
    {
        public string id;
        public Vector3[] points;
        public Vector3[] stops;
        public float cruiseMetersPerSecond = 8;
        public int capacity = 20;
        public Color fleetColor = Color.white;
    }

    // A two-actor maximum UTS bus pool. Routes and paths remain in the persistent scene.
    [DefaultExecutionOrder(500), DisallowMultipleComponent]
    public sealed class TruckTaxiBusService : MonoBehaviour
    {
        public GameObject busPrefab;
        public TruckTaxiBusRoute[] routes = Array.Empty<TruckTaxiBusRoute>();
        public TruckTaxiRegionalWorld world;
        public Transform player;
        public float spawnRadius = 340;
        public float despawnRadius = 500;
        public float stopSeconds = 5;
        public int LiveBuses { get; private set; }
        public int RouteCount => states?.Length ?? 0;
        public int PassengerCount(int index) => index >= 0 && index < RouteCount && states[index] != null ? states[index].Passengers : 0;
        public float RouteProgressMeters(int index) => index >= 0 && index < RouteCount && states[index] != null ? states[index].Progress : 0;
        public GameObject GetLiveBus(int index) => index >= 0 && index < RouteCount && states[index]?.Actor != null &&
            states[index].Actor.activeInHierarchy ? states[index].Actor : null;
        private readonly LwsUtsTrafficApi api = new LwsUtsTrafficApi();
        private readonly LwsTrafficSpawnPolicy policy = new LwsTrafficSpawnPolicy {
            densityTier = LwsTrafficDensityTier.Off, targetCruiseSpeedScale = .65f, maximumTrafficSpeedMetersPerSecond = 11 };
        private RouteState[] states;
        private Func<Vector3,float> venueFlow;
        public void SetVenueFlow(Func<Vector3,float> sampler) => venueFlow=sampler;
        private int PassengerExchange(Vector3 stop,int normal)
        { float flow=venueFlow?.Invoke(stop) ?? 0; return flow>.1f ? -Mathf.CeilToInt(flow*3) : flow<-.1f ? Mathf.CeilToInt(-flow*3) : normal; }

        private sealed class RouteState
        {
            public LwsTrafficLaneDefinition Lane;
            public Component Path;
            public GameObject Actor;
            public Component Ai;
            public Behaviour AiBehaviour;
            public FieldInfo StopField;
            public WheelCollider[] Wheels;
            public Rigidbody Body;
            public float DwellUntil;
            public int LastStop = -1;
            public int Generation;
            public float Progress;
            public float Length;
            public int Passengers;
            public float NextStopUntil;
            public GameObject BoardingVisual;
        }

        private void Start()
        {
            if (player == null) player = TruckTaxiBootstrap.Instance?.Player?.transform;
            if (world == null) world = GetComponentInParent<TruckTaxiRegionalWorld>();
            states = new RouteState[routes.Length];
            if (busPrefab == null || !api.PrefabLooksLikeUtsVehicle(busPrefab))
            { Debug.LogError("Town bus requires the installed UTS Big_Bus prefab and CarAI components.", this); return; }
            for (int i = 0; i < routes.Length; i++)
            {
                var route = routes[i];
                if (route == null || route.points == null || route.points.Length < 5) continue;
                var lane = new LwsTrafficLaneDefinition { laneId = route.id, roadId = route.id,
                    centerline = route.points, laneWidthMeters = 3.5f, speedLimitMph = 25,
                    lengthMeters = RouteLength(route.points), spawnEnabled = false };
                var owner = new GameObject(route.id + " UTS path"); owner.transform.SetParent(transform, false);
                var path = api.CreatePath(owner, lane, new[] { busPrefab }, policy, out string message);
                if (path == null) { Debug.LogError(message, this); Destroy(owner); continue; }
                states[i] = new RouteState { Lane = lane, Path = path, Length = lane.lengthMeters,
                    Passengers = Mathf.Min(6, Mathf.Max(0, route.capacity)) };
            }
        }

        private void Update()
        {
            if (player == null) player = TruckTaxiBootstrap.Instance?.Player?.transform;
            if (states == null || player == null) return;
            LiveBuses = 0;
            foreach (var state in states)
                if (state?.Actor != null && state.Actor.activeSelf) LiveBuses++;
            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state == null) continue;
                var route = routes[i];
                bool live = state.Actor != null && state.Actor.activeSelf;
                if (live) state.Progress = ProjectDistance(route.points, state.Actor.transform.position);
                else AdvanceLogical(state, route, Time.deltaTime);
                Vector3 position = Sample(route.points, state.Progress);
                float distance = Vector3.Distance(player.position, position);
                bool available = world == null || world.IsPositionAvailable(position);
                if (live && (!available || distance > despawnRadius)) { Pool(state); LiveBuses--; live = false; }
                else if (!live && available && distance < spawnRadius && LiveBuses < 2)
                { Activate(state, route); if (state.Actor != null && state.Actor.activeSelf) LiveBuses++; }
            }
        }

        private void FixedUpdate()
        {
            if (states == null) return;
            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state?.Actor == null || !state.Actor.activeSelf || state.Ai == null) continue;
                var stops = routes[i].stops;
                if (stops == null) continue;
                if (state.DwellUntil <= 0)
                {
                    for (int s = 0; s < stops.Length; s++)
                    {
                        if (s == state.LastStop || (state.Actor.transform.position - stops[s]).sqrMagnitude > 10 * 10) continue;
                        state.LastStop = s; state.DwellUntil = Time.time + stopSeconds;
                        ExchangePassengers(state, routes[i]); break;
                    }
                    if (state.LastStop >= 0 && (state.Actor.transform.position - stops[state.LastStop]).sqrMagnitude > 30 * 30)
                        state.LastStop = -1;
                }
                bool waiting = state.DwellUntil > Time.time;
                if (!waiting) state.DwellUntil = 0;
                if (!waiting && state.BoardingVisual != null) { Destroy(state.BoardingVisual); state.BoardingVisual = null; }
                SetStop(state, waiting);
                if (waiting) foreach (var wheel in state.Wheels)
                    wheel.brakeTorque = Mathf.Max(wheel.brakeTorque, 18000);
                else if (state.DwellUntil == 0) foreach (var wheel in state.Wheels)
                    if (Mathf.Approximately(wheel.brakeTorque, 18000)) wheel.brakeTorque = 0;
            }
        }

        private void Activate(RouteState state, TruckTaxiBusRoute route)
        {
            if (state.Actor == null)
            {
                state.Actor = api.SpawnVehicle(busPrefab, state.Path, state.Lane, 1, transform, policy, out string message);
                if (state.Actor == null) { Debug.LogWarning(message, this); return; }
                state.Ai = FindUts(state.Actor, "CarAIController");
                state.Body = state.Ai != null ? state.Ai.GetComponent<Rigidbody>() : null;
                if (state.Ai == null || state.Body == null) { Debug.LogError("UTS bus spawned without CarAIController/Rigidbody.", this); Destroy(state.Actor); state.Actor = null; return; }
                state.AiBehaviour = state.Ai as Behaviour;
                state.StopField = state.Ai.GetType().GetField("tempStop", BindingFlags.Public | BindingFlags.Instance);
                state.Wheels = state.Ai.GetComponentsInChildren<WheelCollider>(true);
                FitPhysicalBody(state.Body);
                var target=state.Actor.GetComponent<TruckTaxiImpactTarget>() ?? state.Actor.AddComponent<TruckTaxiImpactTarget>();
                target.kind=TaxiImpactKind.Traffic;
                target.targetId="bus:"+route.id;
            }
            else
            {
                if (state.Body != null)
                { state.Body.isKinematic = false; state.Body.linearVelocity = Vector3.zero; state.Body.angularVelocity = Vector3.zero; }
                state.Actor.SetActive(true);
            }
            state.Actor.transform.position = Sample(route.points, state.Progress);
            var movePath = FindUts(state.Actor, "MovePath");
            movePath?.GetType().GetMethod("InitStartPosition")?.Invoke(movePath,
                new object[] { 0, SegmentAt(route.points, state.Progress), true, true });
            movePath?.GetType().GetMethod("SetLookPosition")?.Invoke(movePath, null);
            state.DwellUntil = 0; state.LastStop = -1;
            ApplyFleetColor(state.Actor, route.fleetColor);
            foreach (var sound in state.Actor.GetComponentsInChildren<AudioSource>(true))
            {
                sound.spatialBlend = 1; sound.rolloffMode = AudioRolloffMode.Linear;
                sound.minDistance = Mathf.Min(sound.minDistance, 7); sound.maxDistance = Mathf.Min(sound.maxDistance, 65);
                TruckTaxiAudioController.Instance?.Route(sound, TruckTaxiAudioCategory.World);
            }
            SetStop(state, false);
            state.AiBehaviour.enabled = false;
            StartCoroutine(EnableInitializedAi(state, ++state.Generation));
        }

        private static IEnumerator EnableInitializedAi(RouteState state, int generation)
        {
            yield return null;
            if (state.Actor != null && state.Actor.activeInHierarchy && state.Generation == generation)
                state.AiBehaviour.enabled = true;
        }

        private static void Pool(RouteState state)
        {
            state.Generation++;
            if (state.AiBehaviour != null) state.AiBehaviour.enabled = false;
            SetStop(state, false);
            if (state.Wheels != null) foreach (var wheel in state.Wheels)
                if (wheel != null && Mathf.Approximately(wheel.brakeTorque, 18000)) wheel.brakeTorque = 0;
            if (state.Body != null)
            { state.Body.linearVelocity = Vector3.zero; state.Body.angularVelocity = Vector3.zero; state.Body.isKinematic = true; }
            state.Actor.SetActive(false);
            state.DwellUntil = 0;
            if (state.BoardingVisual != null) { Destroy(state.BoardingVisual); state.BoardingVisual = null; }
        }

        private static void FitPhysicalBody(Rigidbody body)
        {
            if(body==null) return;
            Vector3 low=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
            Vector3 high=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
            bool found=false;
            foreach(var renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                if(!renderer.gameObject.activeInHierarchy || renderer.name.Contains("Wheel") ||
                    renderer.name.Contains("Mirror") || renderer.name.Contains("Antenna")) continue;
                var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
                Bounds bounds=mesh!=null ? mesh.bounds : renderer is SkinnedMeshRenderer skinned ?
                    skinned.localBounds : renderer.bounds;
                Transform source=mesh!=null || renderer is SkinnedMeshRenderer ? renderer.transform : null;
                Vector3 min=bounds.min, max=bounds.max;
                for(int x=0;x<2;x++) for(int y=0;y<2;y++) for(int z=0;z<2;z++)
                {
                    Vector3 corner=new Vector3(x==0 ? min.x : max.x,y==0 ? min.y : max.y,z==0 ? min.z : max.z);
                    Vector3 point=body.transform.InverseTransformPoint(source!=null ? source.TransformPoint(corner) : corner);
                    low=Vector3.Min(low,point); high=Vector3.Max(high,point);
                }
                found=true;
            }
            if(!found) { Debug.LogWarning("UTS bus has no visible body renderer for collider fitting.",body); return; }
            var collider=body.gameObject.AddComponent<BoxCollider>();
            collider.center=(low+high)*.5f;
            collider.size=new Vector3((high.x-low.x)*.96f,(high.y-low.y)*.94f,(high.z-low.z)*.97f);
            collider.isTrigger=false;
        }

        private void OnDisable()
        {
            if (states == null) return;
            foreach (var state in states) if (state?.Actor != null && state.Actor.activeSelf) Pool(state);
        }

        private static void SetStop(RouteState state, bool stop) => state.StopField?.SetValue(state.Ai, stop);

        private static Component FindUts(GameObject owner, string name)
        {
            foreach (var component in owner.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.GetType().Name == name) return component;
            return null;
        }

        private static float RouteLength(Vector3[] points)
        {
            float length = 0;
            for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]);
            return length + Vector3.Distance(points[points.Length - 1], points[0]);
        }

        private void AdvanceLogical(RouteState state, TruckTaxiBusRoute route, float seconds)
        {
            if (Time.time < state.NextStopUntil) return;
            float previous = state.Progress;
            state.Progress = Mathf.Repeat(previous + Mathf.Max(1, route.cruiseMetersPerSecond) * seconds, state.Length);
            if (route.stops == null) return;
            for (int i = 0; i < route.stops.Length; i++)
            {
                float stop = ProjectDistance(route.points, route.stops[i]);
                if (!(previous <= state.Progress ? stop > previous && stop <= state.Progress : stop > previous || stop <= state.Progress)) continue;
                state.Progress = stop; state.NextStopUntil = Time.time + stopSeconds;
                state.Passengers = Mathf.Clamp(state.Passengers + PassengerExchange(route.stops[i],(i & 1) == 0 ? 2 : -1), 0, route.capacity);
                break;
            }
        }

        private void ExchangePassengers(RouteState state, TruckTaxiBusRoute route)
        {
            int exchanged=PassengerExchange(route.stops[state.LastStop],(state.LastStop & 1) == 0 ? 2 : -1);
            state.Passengers = Mathf.Clamp(state.Passengers + exchanged, 0, route.capacity);
            if (state.BoardingVisual != null) Destroy(state.BoardingVisual);
            state.BoardingVisual = new GameObject("Taxi bus boarding passenger");
            var renderer = state.Actor.GetComponentInChildren<Renderer>();
            Material material = renderer != null ? renderer.sharedMaterial : null;
            TruckTaxiWobbleVisual.Create(state.BoardingVisual.transform, 1.5f, 1, material, material);
            Vector3 curb = route.stops[state.LastStop] + state.Actor.transform.right * 3;
            Vector3 door = state.Actor.transform.position + state.Actor.transform.right * 1.2f;
            StartCoroutine(MoveBoarder(state.BoardingVisual, exchanged>0 ? curb : door,exchanged>0 ? door : curb));
        }

        private static IEnumerator MoveBoarder(GameObject actor, Vector3 from, Vector3 to)
        {
            float elapsed = 0;
            while (actor != null && elapsed < 2)
            {
                elapsed += Time.deltaTime;
                actor.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / 2));
                yield return null;
            }
        }

        private static void ApplyFleetColor(GameObject actor, Color color)
        {
            if (color == Color.white) return;
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) renderer.SetPropertyBlock(block);
        }

        private static int SegmentAt(Vector3[] points, float distance)
        {
            for (int i = 0; i < points.Length; i++)
            {
                float length = Vector3.Distance(points[i], points[(i + 1) % points.Length]);
                if (distance < length) return i;
                distance -= length;
            }
            return 1;
        }

        public static Vector3 Sample(Vector3[] points, float distance)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = points[i], b = points[(i + 1) % points.Length];
                float length = Vector3.Distance(a, b);
                if (distance <= length) return Vector3.Lerp(a, b, length > 0 ? distance / length : 0);
                distance -= length;
            }
            return points[0];
        }

        public static float ProjectDistance(Vector3[] points, Vector3 position)
        {
            float along = 0, best = 0, bestSqr = float.PositiveInfinity;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = points[i], b = points[(i + 1) % points.Length];
                Vector3 delta = b - a;
                float length = delta.magnitude;
                float t = length > .001f ? Mathf.Clamp01(Vector3.Dot(position - a, delta) / (length * length)) : 0;
                float sqr = (position - Vector3.Lerp(a, b, t)).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = along + t * length; }
                along += length;
            }
            return best;
        }
    }
}
