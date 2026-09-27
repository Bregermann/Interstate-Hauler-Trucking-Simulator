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
        public GameObject GetLiveBus(int index) => index >= 0 && index < RouteCount && states[index]?.Actor != null &&
            states[index].Actor.activeInHierarchy ? states[index].Actor : null;
        private readonly LwsUtsTrafficApi api = new LwsUtsTrafficApi();
        private readonly LwsTrafficSpawnPolicy policy = new LwsTrafficSpawnPolicy {
            densityTier = LwsTrafficDensityTier.Off, targetCruiseSpeedScale = .65f, maximumTrafficSpeedMetersPerSecond = 11 };
        private RouteState[] states;

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
                states[i] = new RouteState { Lane = lane, Path = path };
            }
        }

        private void Update()
        {
            if (player == null) player = TruckTaxiBootstrap.Instance?.Player?.transform;
            if (states == null || player == null) return;
            LiveBuses = 0;
            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state == null) continue;
                Vector3 centre = routes[i].points[0];
                float distance = Vector3.Distance(player.position, centre);
                bool available = world == null || world.IsPositionAvailable(centre);
                if (state.Actor != null && state.Actor.activeSelf && (!available || distance > despawnRadius)) Pool(state);
                else if (available && distance < spawnRadius && (state.Actor == null || !state.Actor.activeSelf)) Activate(state);
                if (state.Actor != null && state.Actor.activeSelf) LiveBuses++;
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
                        state.LastStop = s; state.DwellUntil = Time.time + stopSeconds; break;
                    }
                    if (state.LastStop >= 0 && (state.Actor.transform.position - stops[state.LastStop]).sqrMagnitude > 30 * 30)
                        state.LastStop = -1;
                }
                bool waiting = state.DwellUntil > Time.time;
                if (!waiting) state.DwellUntil = 0;
                SetStop(state, waiting);
                if (waiting) foreach (var wheel in state.Wheels)
                    wheel.brakeTorque = Mathf.Max(wheel.brakeTorque, 18000);
                else if (state.DwellUntil == 0) foreach (var wheel in state.Wheels)
                    if (Mathf.Approximately(wheel.brakeTorque, 18000)) wheel.brakeTorque = 0;
            }
        }

        private void Activate(RouteState state)
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
            }
            else
            {
                state.Actor.transform.position = state.Lane.centerline[1];
                if (state.Body != null)
                { state.Body.isKinematic = false; state.Body.linearVelocity = Vector3.zero; state.Body.angularVelocity = Vector3.zero; }
                var movePath = FindUts(state.Actor, "MovePath");
                movePath?.GetType().GetMethod("InitStartPosition")?.Invoke(movePath, new object[] { 0, 1, true, true });
                movePath?.GetType().GetMethod("SetLookPosition")?.Invoke(movePath, null);
                state.Actor.SetActive(true);
            }
            state.DwellUntil = 0; state.LastStop = -1;
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
    }
}
