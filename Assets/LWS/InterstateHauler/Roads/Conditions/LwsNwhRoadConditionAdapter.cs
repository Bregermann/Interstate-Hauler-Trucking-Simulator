using System;
using System.Collections.Generic;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsNwhRoadConditionAdapter : MonoBehaviour, ILwsRoadConditionPhysicsAdapter
    {
        [SerializeField] private bool attachOnEnable = true;
        [SerializeField] private bool includeAttachedTrailer = true;
        [SerializeField] private float rebindIntervalSeconds = 1f;
        [SerializeField] private float maintenanceApplyIntervalSeconds = 0.5f;

        private readonly List<WheelBinding> _bindings = new List<WheelBinding>();
        private ILwsRoadConditionService _roadConditionService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private LwsPlayerTruck _boundTruck;
        private string _boundTrailerId = string.Empty;
        private float _nextRebindTime;
        private float _nextMaintenanceApplyTime;
        private LwsRoadConditionSnapshot _lastApplied;
        private bool _hasAppliedSnapshot;

        public string AdapterId => "nwh.road.condition.physics";
        public bool IsAvailable => _bindings.Count > 0 || ResolveActiveTruck() != null;
        public string Status { get; private set; } = "NWH road condition adapter not initialized.";
        public int BoundWheelCount => _bindings.Count;

        private void OnEnable()
        {
            ResolveServices();
            if (attachOnEnable)
            {
                _roadConditionService?.AttachPhysicsAdapter(this);
            }
        }

        private void OnDisable()
        {
            _roadConditionService?.DetachPhysicsAdapter(this);
            RestoreDryBaseline();
        }

        private void Update()
        {
            if (_roadConditionService == null)
            {
                ResolveServices();
                _roadConditionService?.AttachPhysicsAdapter(this);
            }

            if (_roadConditionService == null)
            {
                return;
            }

            if (Time.time >= _nextRebindTime)
            {
                ResolveWheelBindings(false);
            }

            if (_hasAppliedSnapshot && Time.time >= _nextMaintenanceApplyTime)
            {
                ApplyToCachedWheels(_lastApplied);
                _nextMaintenanceApplyTime = Time.time + Mathf.Max(0.1f, maintenanceApplyIntervalSeconds);
            }
        }

        public void ApplyRoadCondition(LwsRoadConditionSnapshot snapshot)
        {
            ResolveWheelBindings(false);
            _lastApplied = snapshot;
            _hasAppliedSnapshot = true;

            if (_bindings.Count == 0)
            {
                Status = "No NWH player-truck wheels are currently bound.";
                return;
            }

            ApplyToCachedWheels(snapshot);
            Status = $"Applied {snapshot.condition} road condition to {_bindings.Count} NWH wheel(s).";
        }

        public void RestoreDryBaseline()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Restore();
            }

            _hasAppliedSnapshot = false;
            Status = $"Restored dry NWH baseline for {_bindings.Count} wheel(s).";
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _roadConditionService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
        }

        private void ResolveWheelBindings(bool force)
        {
            if (!force && Time.time < _nextRebindTime && _bindings.Count > 0)
            {
                return;
            }

            _nextRebindTime = Time.time + Mathf.Max(0.1f, rebindIntervalSeconds);
            LwsPlayerTruck activeTruck = ResolveActiveTruck();
            string trailerId = ResolveAttachedTrailerId(activeTruck);
            if (!force && activeTruck == _boundTruck && string.Equals(trailerId, _boundTrailerId, StringComparison.OrdinalIgnoreCase) && _bindings.Count > 0)
            {
                return;
            }

            RestoreDryBaseline();
            _bindings.Clear();
            _boundTruck = activeTruck;
            _boundTrailerId = trailerId;

            if (activeTruck == null || activeTruck.NwhAdapter == null || activeTruck.NwhAdapter.VehicleController == null)
            {
                Status = "Active player truck or NWH VehicleController is not available.";
                return;
            }

            AddVehicleWheels(activeTruck.NwhAdapter.VehicleController, "tractor");

            if (includeAttachedTrailer && !string.IsNullOrWhiteSpace(trailerId))
            {
                VehicleController trailerController = ResolveTrailerVehicleController(trailerId);
                if (trailerController != null)
                {
                    AddVehicleWheels(trailerController, "trailer");
                }
            }

            Status = $"Bound {_bindings.Count} NWH road-condition wheel(s).";
        }

        private LwsPlayerTruck ResolveActiveTruck()
        {
            if (_playerVehicleService != null && _playerVehicleService.ActiveTruck != null)
            {
                return _playerVehicleService.ActiveTruck;
            }

            return FindFirstObjectByType<LwsPlayerTruck>();
        }

        private static string ResolveAttachedTrailerId(LwsPlayerTruck truck)
        {
            if (truck == null || truck.CouplingAdapter == null)
            {
                return string.Empty;
            }

            LwsTrailerAttachmentState state = truck.CouplingAdapter.CurrentState;
            return state.attached ? state.trailerId : string.Empty;
        }

        private static VehicleController ResolveTrailerVehicleController(string trailerId)
        {
            if (string.IsNullOrWhiteSpace(trailerId))
            {
                return null;
            }

            LwsVehicleIdentity[] identities = FindObjectsByType<LwsVehicleIdentity>(FindObjectsSortMode.None);
            for (int i = 0; i < identities.Length; i++)
            {
                LwsVehicleIdentity identity = identities[i];
                if (identity == null || identity.Role != LwsVehicleRole.Trailer ||
                    !string.Equals(identity.VehicleId, trailerId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                LwsNwhVehicleAdapter adapter = identity.GetComponent<LwsNwhVehicleAdapter>();
                if (adapter != null && adapter.VehicleController != null)
                {
                    return adapter.VehicleController;
                }

                return identity.GetComponent<VehicleController>();
            }

            return null;
        }

        private void AddVehicleWheels(VehicleController controller, string owner)
        {
            if (controller == null || controller.powertrain == null || controller.powertrain.wheels == null)
            {
                return;
            }

            List<WheelComponent> wheels = controller.powertrain.wheels;
            for (int i = 0; i < wheels.Count; i++)
            {
                WheelUAPI wheel = wheels[i]?.wheelUAPI;
                if (wheel == null)
                {
                    continue;
                }

                _bindings.Add(new WheelBinding(
                    wheel,
                    $"{owner}:{controller.name}:{i}",
                    wheel.LongitudinalFrictionGrip,
                    wheel.LateralFrictionGrip,
                    wheel.RollingResistanceTorque));
            }
        }

        private void ApplyToCachedWheels(LwsRoadConditionSnapshot snapshot)
        {
            float longitudinalGrip = Mathf.Clamp(snapshot.longitudinalGripMultiplier01, 0.01f, 1.25f);
            float lateralGrip = Mathf.Clamp(snapshot.lateralGripMultiplier01, 0.01f, 1.25f);
            float brakingGrip = Mathf.Clamp(snapshot.brakingGripMultiplier01, 0.01f, 1.25f);
            float finalLongitudinal = Mathf.Min(longitudinalGrip, brakingGrip);
            float rollingResistance = Mathf.Clamp(snapshot.rollingResistanceMultiplier, 0.5f, 3f);

            for (int i = _bindings.Count - 1; i >= 0; i--)
            {
                if (!_bindings[i].IsValid)
                {
                    _bindings.RemoveAt(i);
                    continue;
                }

                _bindings[i].Apply(finalLongitudinal, lateralGrip, rollingResistance);
            }
        }

        private readonly struct WheelBinding
        {
            private readonly WheelUAPI _wheel;
            private readonly string _id;
            private readonly float _baselineLongitudinalGrip;
            private readonly float _baselineLateralGrip;
            private readonly float _baselineRollingResistance;

            public WheelBinding(
                WheelUAPI wheel,
                string id,
                float baselineLongitudinalGrip,
                float baselineLateralGrip,
                float baselineRollingResistance)
            {
                _wheel = wheel;
                _id = id;
                _baselineLongitudinalGrip = Mathf.Max(0.01f, baselineLongitudinalGrip);
                _baselineLateralGrip = Mathf.Max(0.01f, baselineLateralGrip);
                _baselineRollingResistance = Mathf.Max(0f, baselineRollingResistance);
            }

            public bool IsValid => _wheel != null;

            public void Apply(float longitudinalMultiplier, float lateralMultiplier, float rollingResistanceMultiplier)
            {
                if (_wheel == null)
                {
                    return;
                }

                _wheel.LongitudinalFrictionGrip = _baselineLongitudinalGrip * longitudinalMultiplier;
                _wheel.LateralFrictionGrip = _baselineLateralGrip * lateralMultiplier;
                _wheel.RollingResistanceTorque = _baselineRollingResistance * rollingResistanceMultiplier;
            }

            public void Restore()
            {
                if (_wheel == null)
                {
                    return;
                }

                _wheel.LongitudinalFrictionGrip = _baselineLongitudinalGrip;
                _wheel.LateralFrictionGrip = _baselineLateralGrip;
                _wheel.RollingResistanceTorque = _baselineRollingResistance;
            }
        }
    }
}
