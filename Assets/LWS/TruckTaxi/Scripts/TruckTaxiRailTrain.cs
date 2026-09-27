using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRailTrain : MonoBehaviour
    {
        public TruckTaxiRailRoute route;
        public TruckTaxiRegionalWorld world;
        public Transform player;
        [Min(1)] public float speed = 22;
        [Min(0)] public float stationWait = 12;
        [Min(50)] public float presentationRadius = 450;
        public float Progress { get; private set; } = 65;
        public int Direction { get; private set; } = 1;
        public bool IsDwell => dwell > 0;
        private const float CarSpacing = 19;
        private readonly GameObject[] cars = new GameObject[4];
        private float dwell;
        private int lastStation = -1;

        private void Update()
        {
            if (route == null || route.Length <= 0) return;
            if (player == null) player = TruckTaxiBootstrap.Instance?.Player?.transform;
            Advance(Time.deltaTime);
            RefreshPresentation();
        }

        public void Advance(float seconds)
        {
            if (route == null || route.Length <= 0 || seconds <= 0) return;
            if (dwell > 0) { dwell = Mathf.Max(0, dwell - seconds); return; }
            float before = Progress;
            Progress = Mathf.Clamp(Progress + Direction * speed * seconds, 65, route.Length - 65);
            for (int i = 0; i < route.stationDistances.Length; i++)
            {
                float station = route.stationDistances[i];
                if (i != lastStation && (before - station) * (Progress - station) <= 0 &&
                    Mathf.Abs(Progress - before) > .001f)
                { Progress = station; dwell = stationWait; lastStation = i; return; }
            }
            if (Progress <= 65 || Progress >= route.Length - 65)
            { Direction = -Direction; lastStation = -1; dwell = stationWait; }
            else if (lastStation >= 0 && Mathf.Abs(Progress - route.stationDistances[lastStation]) > 35) lastStation = -1;
        }

        public Vector3 CarPosition(int index) => route.Position(Mathf.Clamp(Progress - Direction * index * CarSpacing, 0, route.Length));

        private void RefreshPresentation()
        {
            if (player == null) return;
            float radiusSquared = presentationRadius * presentationRadius;
            bool railLoaded = world == null || SceneManager.GetSceneByName("Taxi_RailCorridor").isLoaded;
            for (int i = 0; i < cars.Length; i++)
            {
                Vector3 position = CarPosition(i);
                bool visible = railLoaded && (position - player.position).sqrMagnitude < radiusSquared &&
                    (world == null || world.IsPositionAvailable(position));
                if (visible && cars[i] == null) cars[i] = CreateCar(i);
                if (cars[i] == null) continue;
                cars[i].SetActive(visible);
                if (!visible) continue;
                float next = Mathf.Clamp(Progress - Direction * i * CarSpacing + Direction * 3, 0, route.Length);
                Vector3 tangent = route.Position(next) - position;
                if (tangent.sqrMagnitude > .01f) cars[i].transform.rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
                cars[i].transform.position = position;
            }
        }

        private GameObject CreateCar(int index)
        {
            var root = new GameObject(index == 0 ? "Rail locomotive" : "Rail carriage " + index);
            root.transform.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body"; body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * 2.6f;
            body.transform.localScale = new Vector3(3.2f, 3.8f, index == 0 ? 14 : 15);
            var collider = body.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            body.GetComponent<Renderer>().material.color = index == 0 ? new Color(.72f, .16f, .09f) : new Color(.18f, .39f, .48f);
            return root;
        }
    }
}
