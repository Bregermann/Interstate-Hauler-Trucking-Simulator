using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    // Explicit one-shot entry point. No scene or asset mutation occurs until invoked in Unity.
    public static class TruckTaxiMegaWorldAuthoring
    {
        private const string Root = TruckTaxiRegionalAuthoring.Root;
        private const string CatalogPath = "Assets/LWS/TruckTaxi/Resources/TruckTaxi/TruckTaxiVenueCatalog.asset";
        private const string MeshFolder = Root + "/Meshes/MegaWorld";
        private const string MaterialFolder = Root + "/Materials";
        public static readonly string[] NewScenePaths = {
            Root + "/Scenes/" + TruckTaxiVenueLayout.StadiumScene + ".unity",
            Root + "/Scenes/" + TruckTaxiVenueLayout.ConcertScene + ".unity",
            Root + "/Scenes/" + TruckTaxiVenueLayout.MountainScene + ".unity"
        };

        [MenuItem("Truck Taxi/Regional/Author Stadium, Concert and Mountain Pass")]
        public static void AuthorMegaWorld()
        {
            RequireCleanScenes();
            var persistent = EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath);
            var host = Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            var world = host != null ? host.GetComponent<TruckTaxiRegionalWorld>() : null;
            if (world == null || host.roadGraph == null || world.streamingManifest == null ||
                host.traffic == null || host.pedestrians == null)
                throw new InvalidOperationException("The persistent Truck Taxi regional world is required.");
            var graph = host.roadGraph.Graph;
            if (graph == null || !graph.Validate().IsValid ||
                !graph.nodes.Exists(n => n.nodeId == "taxi.regional.1080") ||
                !graph.nodes.Exists(n => n.nodeId == "taxi.regional.3640"))
                throw new InvalidOperationException("The existing regional road graph/junctions are incomplete.");
            var manifest = world.streamingManifest;
            foreach (string neighbor in new[] { "taxi.highway", "taxi.town02outskirts", TruckTaxiSpeedwayLayout.RegionId })
                if (!manifest.chunks.Any(c => c.chunkId == neighbor))
                    throw new InvalidOperationException("Required stream neighbor missing: " + neighbor);
            bool any = NewScenePaths.Any(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null) ||
                       world.regions.Any(r => r.id == TruckTaxiVenueLayout.StadiumId ||
                                              r.id == TruckTaxiVenueLayout.ConcertId ||
                                              r.id == TruckTaxiVenueLayout.MountainId);
            bool all = NewScenePaths.All(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null) &&
                       new[] { TruckTaxiVenueLayout.StadiumId, TruckTaxiVenueLayout.ConcertId, TruckTaxiVenueLayout.MountainId }
                           .All(id => world.regions.Any(r => r.id == id) && manifest.chunks.Any(c => c.chunkId == id)) &&
                       AssetDatabase.LoadAssetAtPath<TruckTaxiVenueCatalog>(CatalogPath) != null &&
                       host.GetComponent<TruckTaxiVenueRuntime>() != null &&
                       graph.nodes.Exists(n => n.nodeId == TruckTaxiVenueLayout.MountainId + ".crossing");
            if (any)
            {
                if (!all) throw new InvalidOperationException("Partial Mega World authoring detected; inspect before retrying.");
                Debug.Log("Truck Taxi Mega World already authored; no changes made."); return;
            }
            if (AssetDatabase.LoadAssetAtPath<TruckTaxiVenueCatalog>(CatalogPath) != null)
                throw new InvalidOperationException("Venue catalog already exists; inspect it before authoring.");
            Folder(MeshFolder); Folder("Assets/LWS/TruckTaxi/Resources/TruckTaxi");
            var asphalt = Material("Mega World Asphalt", new Color(.15f, .17f, .18f));
            var concrete = Material("Mega World Concrete", new Color(.58f, .63f, .65f));
            var turf = Material("Stadium Turf", new Color(.11f, .4f, .24f));
            var seats = Material("Stadium Seats", new Color(.13f, .35f, .47f));
            var concert = Material("Concert Cladding", new Color(.22f, .21f, .29f));
            var accent = Material("Concert Accent", new Color(.86f, .21f, .35f));
            var earth = Material("Mountain Earth", new Color(.34f, .39f, .34f));
            var rock = Material("Mountain Rock", new Color(.48f, .51f, .5f));
            var metadata = new GameObject("Truck Taxi Mega World Metadata");
            SceneManager.MoveGameObjectToScene(metadata, persistent);
            foreach (var stop in TruckTaxiVenueLayout.StadiumStops) AddStop(metadata.transform, stop, Icon("SportsStadium", TruckTaxiMapMarkerType.SpecialEvent));
            foreach (var stop in TruckTaxiVenueLayout.ConcertStops) AddStop(metadata.transform, stop, Icon("ConcertVenue", TruckTaxiMapMarkerType.SpecialEvent));
            foreach (var stop in TruckTaxiVenueLayout.MountainStops) AddStop(metadata.transform, stop, TruckTaxiMapMarkerType.ScenicStop);
            AddPoi(metadata.transform, TruckTaxiVenueLayout.StadiumPoiId, "Union Field Stadium",
                TruckTaxiVenueLayout.StadiumCenter, Icon("SportsStadium", TruckTaxiMapMarkerType.SpecialEvent));
            AddPoi(metadata.transform, TruckTaxiVenueLayout.ConcertPoiId, "Neon Yard Concert Venue",
                TruckTaxiVenueLayout.ConcertCenter, Icon("ConcertVenue", TruckTaxiMapMarkerType.SpecialEvent));
            AddPoi(metadata.transform, TruckTaxiVenueLayout.MountainPoiId, "Pinecrest Mountain Pass",
                TruckTaxiVenueLayout.MountainCenter, Icon("Hazard", TruckTaxiMapMarkerType.Danger));
            BuildChunk(0, root => BuildStadium(root, asphalt, concrete, turf, seats));
            BuildChunk(1, root => BuildConcert(root, asphalt, concrete, concert, accent));
            BuildChunk(2, root => BuildMountain(root, asphalt, earth, rock));
            SceneManager.SetActiveScene(persistent);
            TruckTaxiVenueLayout.AddToGraph(graph);
            host.roadGraph.SetGraph(graph, false);
            AddRegion(world, manifest, TruckTaxiVenueLayout.StadiumId, "Union Field Stadium",
                TruckTaxiVenueLayout.StadiumScene, TruckTaxiVenueLayout.StadiumBounds, NewScenePaths[0], "taxi.highway");
            AddRegion(world, manifest, TruckTaxiVenueLayout.ConcertId, "Neon Yard Concert Venue",
                TruckTaxiVenueLayout.ConcertScene, TruckTaxiVenueLayout.ConcertBounds, NewScenePaths[1], "taxi.town02outskirts");
            AddRegion(world, manifest, TruckTaxiVenueLayout.MountainId, "Pinecrest Mountain Pass",
                TruckTaxiVenueLayout.MountainScene, TruckTaxiVenueLayout.MountainBounds, NewScenePaths[2],
                TruckTaxiVenueLayout.StadiumId);
            AuthorPopulationAccess(host, metadata.transform);
            var catalog = CreateCatalog();
            var runtime = host.GetComponent<TruckTaxiVenueRuntime>() ?? host.gameObject.AddComponent<TruckTaxiVenueRuntime>();
            runtime.catalog = catalog;
            var validation = manifest.ValidateManifest();
            if (!validation.IsValid) throw new InvalidOperationException(validation.Summary);
            EditorUtility.SetDirty(host.roadGraph); EditorUtility.SetDirty(world);
            EditorUtility.SetDirty(host.traffic); EditorUtility.SetDirty(host.pedestrians);
            EditorUtility.SetDirty(manifest); EditorUtility.SetDirty(runtime);
            EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent);
            var builds = EditorBuildSettings.scenes.ToList();
            foreach (string path in NewScenePaths)
                if (!builds.Any(s => s.path == path)) builds.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Truck Taxi Mega World authored: Stadium, Concert Venue, Mountain Pass; Thunder Bowl reused.");
        }

        private static TruckTaxiVenueCatalog CreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TruckTaxiVenueCatalog>(CatalogPath);
            if (catalog != null) throw new InvalidOperationException("Existing venue catalog found before authoring; inspect it first.");
            catalog = ScriptableObject.CreateInstance<TruckTaxiVenueCatalog>();
            catalog.venues = new[] {
                Venue(TruckTaxiVenueLayout.StadiumId, "Union Field Stadium", TruckTaxiVenueType.Stadium,
                    "Town 01", "Stadium", TruckTaxiVenueLayout.StadiumScene, TruckTaxiVenueLayout.StadiumCenter, 32000,
                    TruckTaxiVenueLayout.StadiumStops, "taxi.event.union-field-game"),
                Venue(TruckTaxiVenueLayout.ConcertId, "Neon Yard Concert Venue", TruckTaxiVenueType.ConcertArena,
                    "Town 02", "Concert", TruckTaxiVenueLayout.ConcertScene, TruckTaxiVenueLayout.ConcertCenter, 9000,
                    TruckTaxiVenueLayout.ConcertStops, "taxi.event.neon-yard-concert"),
                Venue(TruckTaxiSpeedwayLayout.RegionId, TruckTaxiSpeedwayLayout.DisplayName, TruckTaxiVenueType.Racetrack,
                    "Regional", "Speedway", TruckTaxiSpeedwayLayout.SceneName, TruckTaxiSpeedwayLayout.Center, 18000,
                    TruckTaxiSpeedwayLayout.Stops.Select(s => s.Position).ToArray(), "taxi.event.thunder-bowl-race")
            };
            catalog.events = new[] {
                Event("taxi.event.union-field-game", TruckTaxiVenueLayout.StadiumId, "Union Field Saturday Game",
                    TruckTaxiVenueEventType.Football, DayOfWeek.Saturday, 15, 18, 1.6f, 1.8f, 2.1f),
                Event("taxi.event.neon-yard-concert", TruckTaxiVenueLayout.ConcertId, "Neon Yard Live",
                    TruckTaxiVenueEventType.Concert, DayOfWeek.Friday, 20, 23, 1.75f, 1.6f, 2f),
                Event("taxi.event.thunder-bowl-race", TruckTaxiSpeedwayLayout.RegionId, "Thunder Bowl Stock Car Night",
                    TruckTaxiVenueEventType.StockCarRace, DayOfWeek.Sunday, 17, 20, 1.5f, 1.7f, 1.7f)
            };
            catalog.Validate(); AssetDatabase.CreateAsset(catalog, CatalogPath); return catalog;
        }
        private static TruckTaxiVenueDefinition Venue(string id, string name, TruckTaxiVenueType type,
            string town, string district, string scene, Vector3 position, int capacity,
            TruckTaxiVenueLayout.Stop[] stops, string eventId)
        {
            return Venue(id, name, type, town, district, scene, position, capacity,
                stops.Select(s => s.Position).ToArray(), eventId);
        }
        private static TruckTaxiVenueDefinition Venue(string id, string name, TruckTaxiVenueType type,
            string town, string district, string scene, Vector3 position, int capacity,
            Vector3[] stopPositions, string eventId)
        {
            return new TruckTaxiVenueDefinition {
                id = id, displayName = name, type = type, town = town, district = district,
                regionId = id, sceneName = scene, worldPosition = position, mapPosition = position, capacity = capacity,
                influenceRadiusMeters = 420, parkingZones = stopPositions.Skip(1).Take(1).ToArray(),
                crowdZones = new[] { position + Vector3.forward * 95, position + Vector3.back * 95 },
                taxiPickupZones = stopPositions, taxiDropoffZones = stopPositions,
                eventIds = new[] { eventId }
            };
        }
        private static TruckTaxiVenueEventDefinition Event(string id, string venue, string name,
            TruckTaxiVenueEventType type, DayOfWeek day, int start, int end,
            float fare, float traffic, float crowd)
        {
            return new TruckTaxiVenueEventDefinition {
                id = id, venueId = venue, displayName = name, type = type,
                recurrence = TruckTaxiVenueRecurrence.SpecificWeekday, weekday = day,
                firstDate = "2020-01-01", lastDate = "2099-12-31",
                startHour = start, endHour = end, arrivalLeadMinutes = 120, departureMinutes = 120,
                announcementLeadMinutes = 30, attendanceScale = 1,
                fareMultiplier = fare, trafficMultiplier = traffic, crowdMultiplier = crowd,
                rideFrequencyMultiplier = 1.7f
            };
        }
        private static void AddRegion(TruckTaxiRegionalWorld world, LwsWorldStreamingManifest manifest,
            string id, string name, string scene, Bounds bounds, string path, string neighbor)
        {
            var regions = world.regions.ToList();
            regions.Add(new TruckTaxiRegion { id = id, displayName = name, sceneName = scene, bounds = bounds, population = true });
            world.regions = regions.ToArray();
            var chunk = new LwsWorldChunkDefinition {
                chunkId = id, displayName = name, sceneName = scene, scenePath = path,
                boundsCenter = bounds.center, boundsSize = bounds.size,
                containsRoadGeometry = true, containsTrafficPresentation = false,
                neighborChunkIds = new List<string> { neighbor }
            };
            manifest.chunks.Add(chunk);
            var other = manifest.chunks.Find(c => c.chunkId == neighbor);
            if (!other.neighborChunkIds.Contains(id)) other.neighborChunkIds.Add(id);
        }
        private static void AuthorPopulationAccess(TruckTaxiBootstrap host, Transform metadata)
        {
            var lanes = (host.traffic.cityLanes ?? Array.Empty<LwsTrafficLaneDefinition>()).ToList();
            lanes.Add(CreateAccessLane("taxi.stadium.access.loop", TruckTaxiVenueLayout.StadiumRoad));
            lanes.Add(CreateAccessLane("taxi.concert.access.loop", TruckTaxiVenueLayout.ConcertRoad));
            lanes.Add(CreateAccessLane("taxi.mountainpass.access.loop", TruckTaxiVenueLayout.MountainRoad));
            host.traffic.cityLanes = lanes.ToArray();
            host.pedestrians.roadLanes = lanes.ToArray();

            var sample = (host.pedestrians.peoplePaths ?? Array.Empty<Component>()).FirstOrDefault(p => p != null);
            if (sample == null) throw new InvalidOperationException("An existing UTS PeopleWalkPath is needed for venue crowd authoring.");
            var paths = (host.pedestrians.peoplePaths ?? Array.Empty<Component>()).ToList();
            var areas = (host.pedestrians.pedestrianAreas ?? Array.Empty<TruckTaxiPedestrianArea>()).ToList();
            AddWalkingLoop(metadata, sample, paths, areas, "Union Field west concourse",
                new Vector3(970, .2f, -632),
                new[] { new Vector3(945,.2f,-605), new Vector3(995,.2f,-605),
                    new Vector3(995,.2f,-665), new Vector3(945,.2f,-665) });
            AddWalkingLoop(metadata, sample, paths, areas, "Neon Yard west plaza",
                new Vector3(3595, .2f, -632),
                new[] { new Vector3(3570,.2f,-605), new Vector3(3620,.2f,-605),
                    new Vector3(3620,.2f,-665), new Vector3(3570,.2f,-665) });
            host.pedestrians.peoplePaths = paths.ToArray();
            host.pedestrians.pedestrianAreas = areas.ToArray();

            var buses = Object.FindFirstObjectByType<TruckTaxiBusService>();
            if (buses == null || buses.gameObject.scene != host.gameObject.scene) return;
            var routes = (buses.routes ?? Array.Empty<TruckTaxiBusRoute>()).ToList();
            routes.Add(new TruckTaxiBusRoute {
                id = "taxi.bus.union-field-shuttle", capacity = 28, cruiseMetersPerSecond = 7,
                fleetColor = new Color(.22f,.55f,.62f),
                points = new[] {
                    new Vector3(1080,.18f,-330), TruckTaxiVenueLayout.StadiumStops[0].Position,
                    TruckTaxiVenueLayout.StadiumStops[2].Position, new Vector3(1275,.2f,-620),
                    new Vector3(1275,.2f,-560), new Vector3(1210,.2f,-560),
                    TruckTaxiVenueLayout.StadiumStops[2].Position,
                    TruckTaxiVenueLayout.StadiumStops[0].Position, new Vector3(1080,.18f,-330) },
                stops = new[] { TruckTaxiVenueLayout.StadiumStops[0].Position, TruckTaxiVenueLayout.StadiumStops[2].Position }
            });
            routes.Add(new TruckTaxiBusRoute {
                id = "taxi.bus.neon-yard-shuttle", capacity = 24, cruiseMetersPerSecond = 7,
                fleetColor = new Color(.73f,.27f,.38f),
                points = new[] {
                    new Vector3(3640,.18f,-320), TruckTaxiVenueLayout.ConcertStops[0].Position,
                    TruckTaxiVenueLayout.ConcertStops[2].Position, new Vector3(3915,.2f,-700),
                    new Vector3(3915,.2f,-610), new Vector3(3830,.2f,-610),
                    TruckTaxiVenueLayout.ConcertStops[2].Position,
                    TruckTaxiVenueLayout.ConcertStops[0].Position, new Vector3(3640,.18f,-320) },
                stops = new[] { TruckTaxiVenueLayout.ConcertStops[0].Position, TruckTaxiVenueLayout.ConcertStops[2].Position }
            });
            buses.routes = routes.ToArray();
            EditorUtility.SetDirty(buses);
        }
        public static LwsTrafficLaneDefinition CreateAccessLane(string id, Vector3[] road)
        {
            if (string.IsNullOrWhiteSpace(id) || road == null || road.Length < 3)
                throw new ArgumentException("A venue traffic lane needs an ID and at least three road points.");
            var points = new List<Vector3>(road.Length * 2 + 1);
            for (int i = 0; i < road.Length; i++)
            {
                Vector3 tangent = (road[Mathf.Min(i + 1, road.Length - 1)] -
                    road[Mathf.Max(0, i - 1)]).normalized;
                points.Add(road[i] + new Vector3(-tangent.z, 0, tangent.x) * 2.3f);
            }
            for (int i = road.Length - 1; i >= 0; i--)
            {
                Vector3 tangent = (road[Mathf.Min(i + 1, road.Length - 1)] -
                    road[Mathf.Max(0, i - 1)]).normalized;
                points.Add(road[i] - new Vector3(-tangent.z, 0, tangent.x) * 2.3f);
            }
            points.Add(points[0]);
            float length = 0;
            for (int i = 1; i < points.Count; i++) length += Vector3.Distance(points[i - 1], points[i]);
            return new LwsTrafficLaneDefinition {
                laneId = id, roadId = id, segmentId = id, edgeId = id,
                laneIndex = 0, roadClass = LwsRoadClass.LocalRoad,
                direction = LwsRoadDirection.Bidirectional, laneWidthMeters = 3.5f,
                speedLimitMph = 18, lengthMeters = length, spawnEnabled = true,
                centerline = points.ToArray()
            };
        }
        private static void AddWalkingLoop(Transform metadata, Component sample, List<Component> paths,
            List<TruckTaxiPedestrianArea> areas, string name, Vector3 center, Vector3[] points)
        {
            var areaObject = new GameObject(name + " crowd area");
            areaObject.transform.SetParent(metadata, false); areaObject.transform.position = center;
            var area = areaObject.AddComponent<TruckTaxiPedestrianArea>();
            area.Configure("taxi.crowd." + name.ToLowerInvariant().Replace(' ', '-'), new Vector2(115, 130), 5);
            var pathObject = new GameObject(name + " UTS walking loop");
            pathObject.transform.SetParent(areaObject.transform, false);
            var type = sample.GetType();
            var path = pathObject.AddComponent(type);
            CopyField(type, sample, path, "walkingPrefabs");
            SetField(type, path, "numberOfWays", 1);
            SetField(type, path, "loopPath", true);
            SetField(type, path, "Density", .04f);
            SetField(type, path, "_minimalObjectLength", 3f);
            SetField(type, path, "disableLineDraw", true);
            SetField(type, path, "_ignorePeople", false);
            SetField(type, path, "_ignoreCar", false);
            SetField(type, path, "_ignoreBicycle", false);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var transforms = type.GetField("pathPointTransform", flags)?.GetValue(path) as List<GameObject>;
            var positions = type.GetField("pathPoint", flags)?.GetValue(path) as List<Vector3>;
            if (transforms == null || positions == null)
                throw new InvalidOperationException("The installed UTS PeopleWalkPath point API changed.");
            foreach (var point in points)
            {
                var node = new GameObject("Crowd waypoint");
                node.transform.SetParent(pathObject.transform, false); node.transform.position = point;
                transforms.Add(node); positions.Add(point);
            }
            area.walkingPaths = new Component[] { path };
            areas.Add(area); paths.Add(path);
        }
        private static void CopyField(Type type, object source, object target, string name)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("The installed UTS PeopleWalkPath field is missing: " + name);
            field.SetValue(target, field.GetValue(source));
        }
        private static void SetField(Type type, object target, string name, object value)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("The installed UTS PeopleWalkPath field is missing: " + name);
            field.SetValue(target, value);
        }
        private static void AddStop(Transform parent, TruckTaxiVenueLayout.Stop spec, TruckTaxiMapMarkerType icon)
        {
            var go = new GameObject(spec.Name); go.transform.SetParent(parent, false); go.transform.position = spec.Position;
            var stop = go.AddComponent<TruckTaxiRideLocation>();
            stop.locationId = spec.Id; stop.locationName = spec.Name; stop.district = spec.District;
            stop.locationType = TaxiLocationType.Transit; stop.detectionRadius = 18;
            stop.truckStopPoint = new GameObject("Truck Stop").transform;
            stop.truckStopPoint.SetParent(go.transform, false);
            stop.passengerSpawnPoint = new GameObject("Passenger Spawn").transform;
            stop.passengerSpawnPoint.SetParent(go.transform, false);
            stop.passengerSpawnPoint.localPosition = Vector3.right * 7;
            var marker = go.AddComponent<TruckTaxiMapMarker>();
            marker.stableId = spec.Id; marker.label = spec.Name; marker.markerType = icon;
        }
        private static void AddPoi(Transform parent, string id, string name, Vector3 position, TruckTaxiMapMarkerType icon)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position;
            var marker = go.AddComponent<TruckTaxiMapMarker>();
            marker.stableId = id; marker.label = name; marker.markerType = icon;
            marker.state = TruckTaxiMapMarkerState.Known;
        }
        private static TruckTaxiMapMarkerType Icon(string name, TruckTaxiMapMarkerType fallback)
        {
            return Enum.TryParse(name, out TruckTaxiMapMarkerType icon) ? icon : fallback;
        }
        private static void BuildChunk(int index, Action<Transform> build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject(new[] { TruckTaxiVenueLayout.StadiumScene,
                TruckTaxiVenueLayout.ConcertScene, TruckTaxiVenueLayout.MountainScene }[index]).transform;
            build(root);
            EditorSceneManager.SaveScene(scene, NewScenePaths[index]);
            EditorSceneManager.CloseScene(scene, true);
        }
        private static void BuildStadium(Transform root, Material asphalt, Material concrete, Material turf, Material seats)
        {
            Vector3 c = TruckTaxiVenueLayout.StadiumCenter;
            Box("Stadium campus", root, c + Vector3.down * .55f, new Vector3(570, 1, 700), turf);
            Bake(root, TruckTaxiVenueLayout.StadiumRoad, "UnionField_Access", asphalt);
            Bake(root, new[] { TruckTaxiVenueLayout.StadiumStops[0].Position,
                TruckTaxiVenueLayout.StadiumStops[2].Position }, "UnionField_ParkingRoad", asphalt);
            Box("West truck parking", root, new Vector3(1195, .02f, -610), new Vector3(190, .12f, 190), asphalt);
            Box("Playing field", root, c + Vector3.down * .15f, new Vector3(180, .3f, 95), turf);
            foreach (int side in new[] { -1, 1 })
            {
                for (int tier = 0; tier < 4; tier++)
                {
                    var stand = Box("Grandstand tier", root, c + new Vector3(0, 3 + tier * 3, side * (65 + tier * 17)),
                        new Vector3(230 + tier * 12, 3, 14), seats);
                    stand.transform.rotation = Quaternion.Euler(side * -8, 0, 0);
                }
                Box("End-zone concourse", root, c + new Vector3(side * 112, 5, 0), new Vector3(18, 10, 130), concrete);
            }
            Box("Press box", root, c + new Vector3(0, 23, 144), new Vector3(105, 14, 15), concrete);
            Sign(root, "UNION FIELD", c + new Vector3(0, 30, 155), 9);
        }
        private static void BuildConcert(Transform root, Material asphalt, Material concrete, Material cladding, Material accent)
        {
            Vector3 c = TruckTaxiVenueLayout.ConcertCenter;
            Box("Concert campus", root, c + Vector3.down * .55f, new Vector3(610, 1, 700), concrete);
            Bake(root, TruckTaxiVenueLayout.ConcertRoad, "NeonYard_Access", asphalt);
            Bake(root, new[] { TruckTaxiVenueLayout.ConcertStops[0].Position,
                TruckTaxiVenueLayout.ConcertStops[2].Position }, "NeonYard_ParkingRoad", asphalt);
            Box("East parking", root, c + new Vector3(170, .02f, 40), new Vector3(195, .12f, 220), asphalt);
            Box("Long industrial concert hall", root, c + new Vector3(-15, 19, -35),
                new Vector3(170, 38, 110), cladding);
            Box("Sawtooth roof A", root, c + new Vector3(-58, 40, -35), new Vector3(80, 5, 120), concrete)
                .transform.rotation = Quaternion.Euler(0, 0, 12);
            Box("Sawtooth roof B", root, c + new Vector3(28, 40, -35), new Vector3(80, 5, 120), concrete)
                .transform.rotation = Quaternion.Euler(0, 0, 12);
            Box("Stage wing", root, c + new Vector3(-114, 15, -45), new Vector3(42, 30, 130), accent);
            Box("Entry canopy", root, c + new Vector3(0, 8, 85), new Vector3(135, 3, 38), accent);
            Sign(root, "NEON YARD", c + new Vector3(0, 18, 107), 8);
        }
        private static void BuildMountain(Transform root, Material asphalt, Material earth, Material rock)
        {
            Vector3 c = TruckTaxiVenueLayout.MountainCenter;
            Box("Pass foothills", root, c + new Vector3(0, -1, 0), new Vector3(670, 2, 790), earth);
            Bake(root, TruckTaxiVenueLayout.MountainRoad, "PinecrestPass_Road", asphalt);
            for (int side = -1; side <= 1; side += 2)
            {
                var slope = Box("Authored slide slope " + side, root,
                    TruckTaxiVenueLayout.MountainSlopeCrossing + new Vector3(side * 90, 20, 0),
                    new Vector3(170, 9, 205), rock);
                slope.transform.rotation = Quaternion.Euler(0, 0, side * -13);
            }
            Box("Lower pass embankment", root, new Vector3(1180, 1.2f, -1310), new Vector3(70, 2.4f, 140), earth);
            Box("Slide crossing embankment", root, new Vector3(1180, 4, -1540), new Vector3(75, 8, 105), rock);
            Box("Summit embankment", root, new Vector3(1180, 8, -1660), new Vector3(80, 16, 135), rock);
            Box("Overlook apron", root, TruckTaxiVenueLayout.MountainStops[0].Position + Vector3.down * .12f,
                new Vector3(85, .2f, 85), asphalt);
            Box("Summit turnout", root, TruckTaxiVenueLayout.MountainStops[1].Position + Vector3.down * .12f,
                new Vector3(90, .2f, 85), asphalt);
            Sign(root, "PINECREST PASS", TruckTaxiVenueLayout.MountainStops[0].Position + new Vector3(30, 7, 0), 5);
        }
        private static void Bake(Transform root, Vector3[] points, string key, Material material)
        {
            TruckTaxiDemoBuilder.BakeRegionalRoads(root, new List<Vector3[]> { points }, 10, key, material, MeshFolder);
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        private static void Sign(Transform parent, string text, Vector3 position, float size)
        {
            var sign = new GameObject(text).AddComponent<TextMeshPro>();
            sign.transform.SetParent(parent, false); sign.transform.position = position;
            sign.text = text; sign.fontSize = size; sign.rectTransform.sizeDelta = new Vector2(80, 12);
            sign.alignment = TextAlignmentOptions.Center;
        }
        private static Material Material(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/'); Folder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
        private static void RequireCleanScenes()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before authoring.");
        }
    }
}
