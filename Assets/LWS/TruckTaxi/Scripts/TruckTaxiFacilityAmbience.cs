using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiAmbientRole { Panhandler, RoadPepStranger }

    // Binds optional interactions to existing pooled UTS pedestrians near active facilities.
    public sealed class TruckTaxiAmbientCharacter : TruckTaxiWorldInteractable
    {
        public TruckTaxiAmbientRole Role { get; private set; }
        public TruckTaxiServicePoint Facility { get; private set; }
        private TruckTaxiPedestrian pedestrian;
        public override string Prompt => Role == TruckTaxiAmbientRole.Panhandler ?
            "TALK - SOMEONE ASKS FOR SPARE CHANGE" : "TALK - ROADSIDE STRANGER";
        public void Configure(TruckTaxiPedestrian person, TruckTaxiServicePoint facility, TruckTaxiAmbientRole role)
        {
            pedestrian = person; Facility = facility; Role = role;
            SetStableId(facility.stableId + ".ambient." + role);
            interactionRange = 2.5f;
            enabled = true;
        }
        public override bool CanUse(TruckTaxiWorldLifeController actor) => base.CanUse(actor) &&
            pedestrian != null && pedestrian.isActiveAndEnabled && !pedestrian.IsRagdoll &&
            Facility != null && Facility.isActiveAndEnabled &&
            (transform.position - Facility.Position).sqrMagnitude <= 25f * 25f;
        public override void Use(TruckTaxiWorldLifeController actor) => actor.OpenAmbientChoice(this);
    }

    [DisallowMultipleComponent]
    public sealed class TruckTaxiFacilityAmbience : MonoBehaviour
    {
        [Tooltip("At most this many existing pedestrians receive interactive facility roles.")]
        [Range(0, 2)] public int maximumInteractiveActors = 2;
        public bool enablePanhandler = true;
        public bool enableRoadPepStranger = true;
        private readonly List<TruckTaxiAmbientCharacter> bindings = new List<TruckTaxiAmbientCharacter>();
        private TruckTaxiBootstrap host;
        private float nextRefresh;
        public int ActiveCount { get; private set; }
        public void Initialize(TruckTaxiBootstrap owner) => host = owner;
        private void Update()
        {
            if (host?.Ready != true || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .8f;
            Refresh();
        }
        private void Refresh()
        {
            ActiveCount = 0;
            var player = host.WorldLife?.FootTransform;
            if (player == null || !host.WorldLife.IsOnFoot || host.pedestrians == null)
            { foreach (var binding in bindings) if (binding != null) binding.enabled = false; return; }
            TruckTaxiServicePoint facility = null;
            float nearest = 30f * 30f;
            foreach (var point in TruckTaxiServicePoint.Points)
            {
                if (point == null || point.gameObject.scene != gameObject.scene ||
                    !(point.Supports(TruckTaxiServiceCapability.Store) ||
                      point.Supports(TruckTaxiServiceCapability.Sleep) ||
                      point.Supports(TruckTaxiServiceCapability.Food))) continue;
                float distance = (point.Position - player.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; facility = point;
            }
            foreach (var binding in bindings) if (binding != null) binding.enabled = false;
            if (facility == null) return;
            int limit = Mathf.Min(2, maximumInteractiveActors);
            foreach (var pedestrian in host.pedestrians.People)
            {
                if (ActiveCount >= limit) break;
                if (pedestrian == null || !pedestrian.isActiveAndEnabled || pedestrian.IsRagdoll ||
                    (pedestrian.transform.position - facility.Position).sqrMagnitude > 22f * 22f) continue;
                var role = ActiveCount == 0 ? TruckTaxiAmbientRole.Panhandler : TruckTaxiAmbientRole.RoadPepStranger;
                if (role == TruckTaxiAmbientRole.Panhandler && !enablePanhandler) continue;
                if (role == TruckTaxiAmbientRole.RoadPepStranger && !enableRoadPepStranger) continue;
                var binding = pedestrian.GetComponent<TruckTaxiAmbientCharacter>();
                if (binding == null)
                {
                    binding = pedestrian.gameObject.AddComponent<TruckTaxiAmbientCharacter>();
                    bindings.Add(binding);
                }
                binding.Configure(pedestrian, facility, role);
                host.pedestrians.PinForInteraction(pedestrian, 2f);
                ActiveCount++;
            }
        }
        private void OnDestroy()
        { foreach (var binding in bindings) if (binding != null) Destroy(binding); }
    }
}
