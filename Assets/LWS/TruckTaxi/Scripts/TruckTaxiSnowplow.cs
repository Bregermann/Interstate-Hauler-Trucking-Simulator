using LWS.InterstateHauler;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Dispatch is Taxi-owned; steering, pathing, collision and motion remain UTS-owned.
    public sealed class TruckTaxiSnowplow : MonoBehaviour
    {
        public string VehicleId { get; private set; }
        public int ClearedCellCount { get; private set; }
        public string Diagnostic { get; private set; } = "Not initialized";
        private TruckTaxiBootstrap owner;
        private TruckTaxiSnowRegion region;
        private TruckTaxiEnvironmentSettings settings;
        private TruckTaxiSnowSurface surface;
        private Transform blade;
        private Vector3 previousBladePosition;
        private float nextDispatch;
        private Material bladeMaterial;
        private readonly List<Collider> clearedChassis = new List<Collider>();

        public void Initialize(TruckTaxiBootstrap taxi, TruckTaxiSnowRegion snow, TruckTaxiEnvironmentSettings configuration, TruckTaxiSnowSurface presentation)
        {
            owner = taxi; region = snow; settings = configuration; surface = presentation;
            Diagnostic = "Waiting for heavy snow and an available UTS lane.";
        }

        public void Tick(string weatherId)
        {
            if (owner == null || region == null || owner.traffic == null) return;
            bool dispatchedWeather = weatherId == LwsWeatherPresetCatalog.HeavySnowId || weatherId == TruckTaxiSnow.BlizzardId;
            if (!dispatchedWeather)
            {
                Release();
                return;
            }
            if (blade == null || !owner.traffic.TryResolveVehicle(VehicleId, out _))
            {
                Release();
                if (Time.time < nextDispatch || owner.Player == null) return;
                nextDispatch = Time.time + 10;
                if (!owner.traffic.TrySpawnDedicatedVehicle(owner.Player.transform.position, out _, out string id))
                {
                    Diagnostic = "UTS has no safe dedicated spawn at this position or is at capacity.";
                    return;
                }
                VehicleId = id;
                if (!owner.traffic.TryGetVehicleAi(id, out Component ai) || ai == null)
                {
                    Diagnostic = "Dedicated UTS vehicle has no CarAIController.";
                    Release();
                    return;
                }
                CreateBlade(ai.transform);
                // A dispatched plow starts on a cleared vehicle-sized patch, not embedded in 0.6 m snow.
                region.ClearSweep(ai.transform.position - ai.transform.forward * 3,
                    blade.position, 4.5f, settings.plowResidualDepthMeters);
                surface.MarkDirty(); surface.Refresh();
                // The cutting chassis must not hit the old depth mesh while its swept geometry awaits the next rebuild.
                // Wheels still use the real surface; buildings, vehicles and ordinary road collisions are unchanged.
                foreach (var collider in ai.GetComponentsInChildren<Collider>())
                    if (!collider.isTrigger && !(collider is WheelCollider) && surface.DepthCollider != null)
                    {
                        Physics.IgnoreCollision(collider, surface.DepthCollider, true);
                        clearedChassis.Add(collider);
                    }
                previousBladePosition = blade.position;
                Diagnostic = "UTS snowplow dispatched.";
                return;
            }
            Vector3 current = blade.position;
            float distance = Vector3.Distance(previousBladePosition, current);
            if (distance >= .1f && distance <= 12f)
            {
                int count = region.ClearSweep(previousBladePosition, current, 4.5f, settings.plowResidualDepthMeters);
                if (count > 0) { ClearedCellCount += count; surface.MarkDirty(); }
            }
            previousBladePosition = current;
        }

        private void CreateBlade(Transform vehicle)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "UTS snowplow blade";
            marker.transform.SetParent(vehicle, false);
            marker.transform.localPosition = new Vector3(0, .42f, 2.6f);
            marker.transform.localRotation = Quaternion.Euler(0, -15, 0);
            marker.transform.localScale = new Vector3(4.5f, .7f, .16f);
            var collider = marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            bladeMaterial = LwsWeatheradeMaterialFactory.CreateFallbackLitMaterial("Taxi plow steel", new Color(.35f, .49f, .55f));
            marker.GetComponent<Renderer>().sharedMaterial = bladeMaterial;
            blade = marker.transform;
        }

        private void Release()
        {
            if (surface != null && surface.DepthCollider != null)
                foreach (var collider in clearedChassis)
                    if (collider != null) Physics.IgnoreCollision(collider, surface.DepthCollider, false);
            clearedChassis.Clear();
            if (!string.IsNullOrEmpty(VehicleId) && owner?.traffic != null) owner.traffic.ReleaseDedicatedVehicle(VehicleId);
            VehicleId = null;
            if (blade != null) Destroy(blade.gameObject);
            blade = null;
            if (bladeMaterial != null) Destroy(bladeMaterial);
            bladeMaterial = null;
        }
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }
    }
}
