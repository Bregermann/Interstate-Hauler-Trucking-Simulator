using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static partial class TruckTaxiEdenboroughAuthoring
    {
        private static TruckTaxiRideLocation[] CreateDestinations(Transform parent, TruckTaxiEdenboroughPalette palette, Material asphalt)
        {
            var result = new List<TruckTaxiRideLocation>(); var approaches = new List<Vector3[]>();
            foreach (var district in TruckTaxiEdenboroughLayout.Districts)
            {
                string key = district.Id.Substring("taxi.edenborough.".Length);
                var roads = TruckTaxiEdenboroughLayout.Roads.Where(r => TruckTaxiEdenboroughLayout.RegionAt(r.Points[r.Points.Length / 2]) == district.Id && r.Kind != "Highway").ToArray();
                if (roads.Length == 0) throw new InvalidOperationException("District has no accessible local road: " + key);
                string[] names = key == "gogar" ? new[] { "Gogar Truck Stop", "Gogar Repair Yard", "Gogar Freight Depot" } :
                    key == "leith" ? new[] { "Leith Waterfront Parking", "Leith Harbor Depot", "Leith Quay Diner" } :
                    key == "hillend" ? new[] { "Hillend Scenic Pull-Off", "Hillend Trailhead Parking" } :
                    key == "central" ? new[] { "Central Transit", "Central Market", "Central Diner", "Central Public Parking" } :
                    new[] { district.Name + " Market", district.Name + " Diner", district.Name + " Motel" };
                for (int i = 0; i < names.Length; i++)
                {
                    var road = roads[Math.Min(i, roads.Length - 1)]; int mid = road.Points.Length / 2;
                    var direction = Vector3.ProjectOnPlane(road.Points[mid + 1] - road.Points[mid], Vector3.up).normalized;
                    var right = Vector3.Cross(Vector3.up, direction);
                    Vector3 edge = road.Points[mid]; Vector3 stop = edge + right * (road.Width / 2 + 13);
                    stop.y = edge.y + .08f;
                    var go = New(names[i], parent, stop); go.transform.rotation = Quaternion.LookRotation(direction);
                    var location = go.AddComponent<TruckTaxiRideLocation>();
                    location.locationId = district.Id + ".stop." + i; location.locationName = names[i];
                    location.district = key == "gogar" ? "Industrial" : key == "leith" ? "Harbor" : key == "central" ? "Downtown" : "Residential";
                    location.tags = new[] { key, "Edenborough", location.district };
                    location.locationType = names[i].Contains("Diner") ? TaxiLocationType.Restaurant : names[i].Contains("Motel") ? TaxiLocationType.Hotel :
                        names[i].Contains("Market") ? TaxiLocationType.Store : names[i].Contains("Truck Stop") ? TaxiLocationType.GasStation :
                        names[i].Contains("Depot") || names[i].Contains("Repair") ? TaxiLocationType.Industrial : TaxiLocationType.Transit;
                    location.truckStopPoint = New("Truck Parking / Approach", go.transform, stop).transform;
                    location.truckStopPoint.rotation = go.transform.rotation;
                    location.passengerSpawnPoint = New("Passenger Safe Sidewalk", go.transform, stop + right * 4).transform;
                    approaches.Add(new[] { edge, edge + right * 6, stop, stop + direction * 16 });
                    result.Add(location);
                    var service = go.AddComponent<TruckTaxiServicePoint>(); service.stableId = location.locationId + ".service";
                    service.displayName = names[i]; service.location = location; service.radius = 18;
                    service.recoveryAnchor = location.truckStopPoint; service.generateValidationFacility = false;
                    var binding = go.AddComponent<TruckTaxiEdenboroughFacility>(); binding.service = service;
                    var interactions = new List<TruckTaxiEdenboroughFacility.Binding>();
                    GameObject[] buildings = location.locationType == TaxiLocationType.Hotel ? palette.motelModules :
                        location.locationType == TaxiLocationType.Restaurant ? palette.dinerModules :
                        location.locationType == TaxiLocationType.Store ? palette.storeModules :
                        location.locationType == TaxiLocationType.Industrial ? palette.warehouseBuildings : palette.urbanBuildings;
                    GameObject building = key != "hillend" ? PlaceArt(buildings[i % buildings.Length], go.transform,
                        stop + right * 34, Quaternion.LookRotation(right)) : null;
                    if (location.locationType == TaxiLocationType.GasStation)
                    {
                        var station = go.AddComponent<TruckTaxiGasStationPoint>(); station.stableId = location.locationId + ".diesel";
                        station.displayName = names[i]; station.recoveryAnchor = location.truckStopPoint; binding.station = station;
                        service.capabilities |= TruckTaxiServiceCapability.Refuel | TruckTaxiServiceCapability.Store;
                        var pump = PlaceArt(palette.pumps[0], go.transform, stop + right * 4, go.transform.rotation);
                        interactions.Add(new TruckTaxiEdenboroughFacility.Binding { art = pump, role = TruckTaxiEdenboroughFacility.Role.DieselPump });
                    }
                    if (location.locationType == TaxiLocationType.Store || location.locationType == TaxiLocationType.GasStation)
                    {
                        var store = go.AddComponent<TruckTaxiStorePoint>(); store.stableId = location.locationId + ".store";
                        store.location = location; store.displayName = names[i]; binding.store = store;
                        service.capabilities |= TruckTaxiServiceCapability.Store | TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink;
                        var counter = FindArt(building, "Store.Checkout.Register") ??
                            PlaceArt(palette.shopCounters[0], go.transform, stop + right * 19, go.transform.rotation);
                        interactions.Add(new TruckTaxiEdenboroughFacility.Binding { art = counter, role = TruckTaxiEdenboroughFacility.Role.Checkout });
                        var products = TruckTaxiNeedsItems.Store;
                        for (int product = 0; product < products.Length; product++)
                        {
                            Vector3 productPosition = building != null && FindArt(building, "Store.Shelf.Right") != null ?
                                building.transform.TransformPoint(new Vector3(3.05f, 1.0f + product / 5 * .35f, -1.6f + product % 5 * .75f)) :
                                stop + right * (19 + product % 5 * 1.4f) + direction * (3 + product / 5 * 1.8f);
                            var art = PlaceArt(palette.shopProducts[product % palette.shopProducts.Length], go.transform,
                                productPosition, building != null ? building.transform.rotation : go.transform.rotation);
                            interactions.Add(new TruckTaxiEdenboroughFacility.Binding { art = art, role = TruckTaxiEdenboroughFacility.Role.ProductShelf, product = products[product].Item });
                        }
                    }
                    if (location.locationType == TaxiLocationType.Restaurant)
                    {
                        service.capabilities |= TruckTaxiServiceCapability.Food | TruckTaxiServiceCapability.Drink;
                        var seat = FindArt(building, "Diner.Seat.Left.South") ??
                            PlaceArt(palette.dinerSeats[0], go.transform, stop + right * 19, go.transform.rotation);
                        interactions.Add(new TruckTaxiEdenboroughFacility.Binding { art = seat, role = TruckTaxiEdenboroughFacility.Role.DinerSeat });
                    }
                    if (location.locationType == TaxiLocationType.Hotel)
                    {
                        service.capabilities |= TruckTaxiServiceCapability.Sleep; service.sleepCostPerHourCents = 900;
                        var desk = FindArt(building, "Motel.Desk") ?? PlaceArt(palette.shopCounters[0], go.transform, stop + right * 19, go.transform.rotation);
                        var bed = FindArt(building, "Motel.Bed.0") ?? PlaceArt(palette.motelBeds[0], go.transform, stop + right * 23 + direction * 5, go.transform.rotation);
                        interactions.Add(new TruckTaxiEdenboroughFacility.Binding { art = desk, role = TruckTaxiEdenboroughFacility.Role.MotelDesk });
                        interactions.Add(new TruckTaxiEdenboroughFacility.Binding { art = bed, role = TruckTaxiEdenboroughFacility.Role.MotelBed });
                    }
                    if (key != "hillend")
                    {
                        service.capabilities |= TruckTaxiServiceCapability.Restroom | TruckTaxiServiceCapability.DisposeWaste | TruckTaxiServiceCapability.CleanCab;
                        GameObject room = palette.restroomModules.Length > 0 ? PlaceArt(palette.restroomModules[0], go.transform,
                            stop + right * 25 - direction * 12, Quaternion.LookRotation(right)) : null;
                        var toilet = FindArt(room, "Restroom.Toilet") ??
                            PlaceArt(palette.bathroomFixtures[0], go.transform, stop + right * 20 - direction * 7, go.transform.rotation);
                        interactions.Add(new TruckTaxiEdenboroughFacility.Binding { art = toilet, role = TruckTaxiEdenboroughFacility.Role.Toilet });
                    }
                    if (names[i].Contains("Repair")) service.capabilities |= TruckTaxiServiceCapability.RepairEngine | TruckTaxiServiceCapability.RepairTransmission |
                        TruckTaxiServiceCapability.RepairGeneralDamage | TruckTaxiServiceCapability.RepairTires | TruckTaxiServiceCapability.RecoverVehicle;
                    binding.interactions = interactions.ToArray();
                    PlaceArt(palette.signs[i % palette.signs.Length], go.transform, stop + right * 7 - direction * 6, go.transform.rotation);
                }
            }
            TruckTaxiDemoBuilder.BakeRegionalRoads(parent, approaches, 6.2f, "Edenborough_ServiceApproaches", asphalt, Folder + "/RoadMeshes");
            return result.ToArray();
        }
        private static TruckTaxiGarage CreateGarage(Transform parent, TruckTaxiEdenboroughPalette palette, Material asphalt, out BoxCollider volume)
        {
            var go = New("Edenborough Drive-In Customization Garage", parent, TruckTaxiEdenboroughLayout.Garage);
            var art = (GameObject)PrefabUtility.InstantiatePrefab(palette.garageModules[0], go.transform);
            art.transform.localPosition = palette.garageLocalPosition;
            art.transform.localRotation = Quaternion.Euler(palette.garageLocalEuler);
            art.transform.localScale = palette.garageLocalScale;
            var zone = New("Stopped Truck Garage Bay", go.transform, go.transform.position + Vector3.up * 2.5f);
            volume = zone.AddComponent<BoxCollider>(); volume.isTrigger = true; volume.size = new Vector3(7,5,24);
            var bay = new[] { go.transform.position - Vector3.forward * 22, go.transform.position, go.transform.position + Vector3.forward * 22 };
            TruckTaxiDemoBuilder.BakeRegionalRoads(parent, new List<Vector3[]> { bay }, 7, "Edenborough_GarageBay", asphalt, Folder + "/RoadMeshes");
            return go.AddComponent<TruckTaxiGarage>();
        }
        private static GameObject PlaceArt(GameObject prefab, Transform parent, Vector3 ground, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(ground, rotation);
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Reviewed art has no visible renderers: " + AssetDatabase.GetAssetPath(prefab));
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            go.transform.position += Vector3.up * (ground.y - bounds.min.y);
            foreach (var renderer in renderers)
                if (renderer.sharedMaterials.Any(m => m == null || m.shader == null || UnityEditor.ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Unresolved material/shader on reviewed art: " + AssetDatabase.GetAssetPath(prefab));
            return go;
        }
        private static GameObject FindArt(GameObject building, string name) => building == null ? null :
            building.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)?.gameObject;
    }
}
