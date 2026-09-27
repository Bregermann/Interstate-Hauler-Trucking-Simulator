using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Editor
{
    // Called by regional authoring while the persistent and three relevant chunks are loaded additively.
    public static class TruckTaxiTransitAuthoring
    {
        public const string BusPrefabPath = "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Big_Bus.prefab";

        public static void Build(Transform railRoot, Transform town1Root, Transform town2Root,
            Transform persistentMetadata, TruckTaxiTrafficAdapter traffic)
        {
            if (railRoot == null || town1Root == null || town2Root == null || persistentMetadata == null || traffic == null)
                throw new ArgumentNullException("Transit authoring requires all three streamed roots, persistent metadata, and traffic.");
            var existing = persistentMetadata.Find("Truck Taxi Transit");
            if (existing != null)
            {
                foreach (var stop in existing.GetComponentsInChildren<TruckTaxiRideLocation>(true))
                    if (stop.locationId != null && stop.locationId.StartsWith("taxi.bus.", StringComparison.Ordinal))
                    { stop.district = "Downtown"; EditorUtility.SetDirty(stop); }
                return;
            }
            var busPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BusPrefabPath);
            if (busPrefab == null) throw new InvalidOperationException("Installed UTS Big_Bus prefab was not found.");

            var metadata = Child("Truck Taxi Transit", persistentMetadata);
            var regional = UnityEngine.Object.FindFirstObjectByType<TruckTaxiRegionalWorld>();
            var route = Child("Intercity Rail Spline", metadata).gameObject.AddComponent<TruckTaxiRailRoute>();
            route.Configure(new[] {
                Point(0, 200), Point(0, 280), Point(0, 355), Point(0, 500), Point(250, 500),
                Point(1460, 500), Point(1780, 500, 8), Point(2220, 500, 8), Point(2540, 500),
                Point(3750, 500), Point(4000, 500), Point(4000, 355), Point(4000, 280), Point(4000, 200) });
            var train = Child("Scheduled Intercity Train", metadata).gameObject.AddComponent<TruckTaxiRailTrain>();
            train.route = route; train.world = regional;

            var buses = Child("UTS Local Bus Service", metadata).gameObject.AddComponent<TruckTaxiBusService>();
            buses.busPrefab = busPrefab; buses.world = regional;
            buses.routes = new[] { LocalBusRoute("taxi.bus.town01", 0), LocalBusRoute("taxi.bus.town02", 4000) };

            var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/LWS/TruckTaxi/Regional/Materials/Regional Steel.mat");
            var concrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/LWS/TruckTaxi/Regional/Materials/Regional Concrete.mat");
            var red = AssetDatabase.LoadAssetAtPath<Material>("Assets/UTS_FullPack/Models/Traffic light/Materials/Red.mat");
            BuildTrack(railRoot, route, steel, concrete);
            BuildCrossing(metadata, train, buses, traffic, 0, steel, red);
            BuildCrossing(metadata, train, buses, traffic, 4000, steel, red);
            BuildBusStops(metadata, town1Root, buses.routes[0], "Town 01", steel);
            BuildBusStops(metadata, town2Root, buses.routes[1], "Town 02", steel);
        }

        private static Vector3 Point(float x, float z, float y = .25f) => new Vector3(x, y, z);

        private static TruckTaxiBusRoute LocalBusRoute(string id, float offset)
        {
            Vector3 P(float x, float z) => Point(offset + x, z, .3f);
            return new TruckTaxiBusRoute {
                id = id,
                points = new[] {
                    P(-145,-160),P(-85,-160),P(-80,-166),P(-75,-160),P(0,-160),P(80,-160),
                    P(145,-160),P(160,-145),P(160,-80),P(166,-75),P(160,-70),P(160,0),
                    P(160,80),P(160,145),P(145,160),P(80,160),P(75,166),P(70,160),
                    P(0,160),P(-80,160),P(-145,160),P(-160,145),P(-160,80),P(-166,75),
                    P(-160,70),P(-160,0),P(-160,-80),P(-160,-145) },
                stops = new[] { P(-80,-166),P(166,-75),P(75,166),P(-166,75) }
            };
        }

        private static void BuildTrack(Transform root, TruckTaxiRailRoute route, Material steel, Material concrete)
        {
            var track = Child("Intercity Rail Track", root);
            for (float d = 0; d < route.Length; d += 16)
            {
                float next = Mathf.Min(d + 16, route.Length);
                Vector3 start = route.Position(d), end = route.Position(next), forward = (end - start).normalized;
                if (forward.sqrMagnitude < .01f) continue;
                Vector3 side = Vector3.Cross(Vector3.up, forward);
                Vector3 center = (start + end) * .5f;
                foreach (float offset in new[] { -1.2f, 1.2f })
                {
                    var rail = Box("Steel running rail", track, center + side * offset + Vector3.up * .13f,
                        new Vector3(.18f, .22f, Vector3.Distance(start, end) + .1f), steel, false);
                    rail.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
                var tie = Box("Rail sleeper", track, start, new Vector3(3.6f, .22f, .45f), concrete, false);
                tie.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            Box("Rail bridge deck", track, new Vector3(2000, 7.35f, 500), new Vector3(440, 1, 9), concrete, true);
            foreach (float x in new[] { 1820f, 1920f, 2020f, 2120f, 2200f })
                Box("Rail bridge pier", track, new Vector3(x, 3, 500), new Vector3(4, 8, 7), concrete, true);
            foreach (float x in new[] { 0f, 4000f })
                Box("Station platform", track, new Vector3(x + 9, .35f, 355), new Vector3(12, .65f, 80), concrete, true);
        }

        private static void BuildCrossing(Transform metadata, TruckTaxiRailTrain train, TruckTaxiBusService buses,
            TruckTaxiTrafficAdapter traffic, float x, Material steel, Material red)
        {
            var root = Child(x < 1000 ? "Town 01 Rail Crossing" : "Town 02 Rail Crossing", metadata);
            root.position = Point(x, 320);
            var crossing = root.gameObject.AddComponent<TruckTaxiTransitCrossing>();
            crossing.train = train; crossing.traffic = traffic; crossing.buses = buses;
            crossing.gateArms = new Transform[2]; crossing.warningLamps = new Renderer[2];
            for (int side = 0; side < 2; side++)
            {
                int direction = side == 0 ? 1 : -1;
                var post = Box("Crossing post", root, root.position + Vector3.right * (-direction * 16) + Vector3.up * 1.4f,
                    new Vector3(.3f, 2.8f, .3f), steel, true);
                var arm = Child("Gate arm", root);
                arm.position = post.transform.position + Vector3.up * 1.7f;
                arm.localRotation = Quaternion.Euler(0, 0, direction * 80);
                var barrier = Box("Striped barrier", arm, arm.position,
                    new Vector3(9, .22f, .24f), steel, false);
                barrier.transform.localPosition = new Vector3(direction * 4.5f, 0, 0);
                crossing.gateArms[side] = arm;
                var lamp = Box("Rail warning lamp", root,
                    post.transform.position + Vector3.up * .9f, new Vector3(.4f, .4f, .4f), red, false);
                crossing.warningLamps[side] = lamp.GetComponent<Renderer>();
                crossing.warningLamps[side].enabled = false;
            }
        }

        private static void BuildBusStops(Transform metadata, Transform scenery, TruckTaxiBusRoute route,
            string town, Material signMaterial)
        {
            for (int i = 0; i < route.stops.Length; i++)
            {
                Vector3 position = route.stops[i];
                var stop = Child(town + " Bus Stop " + (i + 1), metadata).gameObject;
                stop.transform.position = position;
                var taxi = stop.AddComponent<TruckTaxiRideLocation>();
                taxi.locationId = route.id + ".stop." + i;
                taxi.locationName = stop.name; taxi.district = "Downtown"; taxi.locationType = TaxiLocationType.Transit;
                taxi.truckStopPoint = Child("Truck stop point", stop.transform);
                taxi.passengerSpawnPoint = Child("Passenger spawn", stop.transform);
                Vector3 sidewalk = Mathf.Abs(position.z) > 165 ?
                    Vector3.forward * Mathf.Sign(position.z) * 9 : Vector3.right * Mathf.Sign(position.x - (town == "Town 02" ? 4000 : 0)) * 9;
                taxi.passengerSpawnPoint.position = position + sidewalk;
                Box("Bus stop marker", scenery, position + sidewalk + Vector3.up * 2,
                    new Vector3(.35f, 4, .35f), signMaterial, false);
            }
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size,
            Material material, bool collision)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            SceneManager.MoveGameObjectToScene(box, parent.gameObject.scene);
            box.transform.SetParent(parent, false);
            box.transform.position = position; box.transform.localScale = size;
            if (material != null) box.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            SceneManager.MoveGameObjectToScene(child, parent.gameObject.scene);
            child.transform.SetParent(parent, false);
            return child.transform;
        }
    }
}
