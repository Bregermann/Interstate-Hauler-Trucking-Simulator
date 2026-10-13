using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using sc.splines.spawner.runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace LWS.TruckTaxi.Editor
{
    public static partial class TruckTaxiEdenboroughAuthoring
    {
        private static void CreateDressing(Transform parent, LwsRoadGraphProvider graph,
            TruckTaxiRideLocation[] stops, BoxCollider garage, TruckTaxiEdenboroughPalette palette)
        {
            Directory.CreateDirectory(Folder + "/Dressing"); AssetDatabase.Refresh();
            var profiles = new Dictionary<TruckTaxiDressingContext, TruckTaxiDressingProfile>();
            foreach (TruckTaxiDressingContext context in Enum.GetValues(typeof(TruckTaxiDressingContext)))
            {
                var prefabs = context == TruckTaxiDressingContext.Forest || context == TruckTaxiDressingContext.Suburb || context == TruckTaxiDressingContext.Highway ? palette.trees :
                    context == TruckTaxiDressingContext.Industrial ? palette.industrial : context == TruckTaxiDressingContext.Construction ? palette.construction : palette.roadside;
                string path = Folder + "/Dressing/" + context + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<TruckTaxiDressingProfile>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<TruckTaxiDressingProfile>(); profile.context = context;
                    profile.spacingMeters = context == TruckTaxiDressingContext.Forest ? 14 : context == TruckTaxiDressingContext.Highway ? 38 : 26;
                    profile.clearanceMeters = 3; profile.seed = 21620 + (int)context;
                    foreach (var prefab in prefabs) profile.prefabs.Add(new SplineSpawner.SpawnableObject(prefab, 100f / prefabs.Length));
                    AssetDatabase.CreateAsset(profile, path);
                }
                if (!profile.Validate(out var reason)) throw new InvalidOperationException(context + ": " + reason);
                profiles.Add(context, profile);
            }
            var roads = TruckTaxiEdenboroughLayout.Roads;
            for (int index = 0; index < roads.Length; index++)
            {
                var road = roads[index];
                string region = TruckTaxiEdenboroughLayout.RegionAt(road.Points[road.Points.Length / 2]);
                var district = TruckTaxiEdenboroughLayout.Districts.First(d => d.Id == region);
                var context = road.Kind == "Highway" ? TruckTaxiDressingContext.Highway :
                    (TruckTaxiDressingContext)Enum.Parse(typeof(TruckTaxiDressingContext), district.Dressing);
                if (road.Id.EndsWith("gogar.x.-1.-1", StringComparison.Ordinal)) context = TruckTaxiDressingContext.Construction;
                for (int side = -1; side <= 1; side += 2)
                {
                    var go = New("Edenborough " + context + " Dressing " + index + "/" + side, parent, Vector3.zero);
                    var container = go.AddComponent<SplineContainer>(); var spline = new Spline();
                    for (int p = 0; p < road.Points.Length; p += 3)
                    {
                        var direction = p + 1 < road.Points.Length ? road.Points[p + 1] - road.Points[p] : road.Points[p] - road.Points[p - 1];
                        Vector3 point = road.Points[p] + Vector3.Cross(Vector3.up, direction.normalized) * (road.Width / 2 + 14) * side;
                        point.y = TruckTaxiEdenboroughLayout.Height(point.x, point.z);
                        spline.Add(new BezierKnot(point), TangentMode.Linear);
                    }
                    if (spline.Count < 2) { UnityEngine.Object.DestroyImmediate(go); continue; }
                    container.Spline = spline;
                    var spawner = go.AddComponent<SplineSpawner>(); spawner.SplineContainer = container; spawner.root = go.transform;
                    var binding = go.AddComponent<TruckTaxiDressingBinding>();
                    binding.stableId = road.Id + ".dressing." + side; binding.spawner = spawner; binding.profile = profiles[context];
                    binding.roadGraph = graph; binding.parkingBays = stops; binding.entranceApproaches = stops.Select(s => s.truckStopPoint).ToArray();
                    binding.protectedVolumes = new[] { garage }; binding.exclusionScopeReviewed = true;
                    if (!binding.GenerateExplicitly(out var reason)) throw new InvalidOperationException("Dressing rejected: " + binding.stableId + ": " + reason);
                }
            }
            foreach (var district in TruckTaxiEdenboroughLayout.Districts)
            {
                if (district.Dressing == "Forest") continue;
                var pool = district.Dressing == "Industrial" ? palette.warehouseBuildings :
                    district.Dressing == "Urban" ? palette.urbanBuildings : palette.residentialBuildings;
                int count = district.Dressing == "Urban" ? 24 : 12;
                var roadsInDistrict = roads.Where(r => r.Kind != "Highway" && TruckTaxiEdenboroughLayout.RegionAt(r.Points[r.Points.Length / 2]) == district.Id).ToArray();
                for (int i = 0; i < count; i++)
                {
                    var road = roadsInDistrict[i % roadsInDistrict.Length];
                    int sample = Math.Min(road.Points.Length - 2, road.Points.Length / 4 + (i / roadsInDistrict.Length) * road.Points.Length / 3);
                    var direction = Vector3.ProjectOnPlane(road.Points[sample + 1] - road.Points[sample], Vector3.up).normalized;
                    var prefab = pool[i % pool.Length];
                    if (!TruckTaxiDressingProfile.TryPrefabBounds(prefab, out var bounds)) throw new InvalidOperationException("Building bounds missing.");
                    var point = road.Points[sample] + Vector3.Cross(Vector3.up, direction) * (road.Width / 2 + 18 + bounds.extents.magnitude) * (i % 2 == 0 ? 1 : -1);
                    point.y = TruckTaxiEdenboroughLayout.Height(point.x, point.z);
                    if (stops.Any(s => Vector3.Distance(s.StopPosition, point) < 75) || Vector3.Distance(garage.transform.position, point) < 75) continue;
                    bool overlapsRoad = graph.Graph.edges.Any(e => e.samples.Any(s => Vector3.Distance(s.position, point) < bounds.extents.magnitude + road.Width / 2 + 4));
                    if (!overlapsRoad) PlaceArt(prefab, parent, point, Quaternion.LookRotation(-direction));
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
