using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public readonly struct LwsDepotPresenceChangedEvent
    {
        public LwsDepotPresenceChangedEvent(LwsDepotDefinition depot, LwsDepotRuntime runtime, LwsPlayerTruck playerTruck, bool entered, string reason)
        {
            Depot = depot;
            Runtime = runtime;
            PlayerTruck = playerTruck;
            Entered = entered;
            Reason = reason ?? string.Empty;
        }

        public LwsDepotDefinition Depot { get; }
        public LwsDepotRuntime Runtime { get; }
        public LwsPlayerTruck PlayerTruck { get; }
        public bool Entered { get; }
        public string Reason { get; }
        public string DepotId => Depot != null ? Depot.StableDepotId : string.Empty;
    }

    public interface ILwsDepotService : ILwsService
    {
        IReadOnlyList<LwsDepotDefinition> DepotDefinitions { get; }
        IReadOnlyList<LwsDepotRuntime> RuntimeDepots { get; }
        bool IsPlayerAtDepot { get; }
        string CurrentDepotId { get; }
        LwsDepotDefinition CurrentDepotDefinition { get; }
        LwsDepotRuntime CurrentDepotRuntime { get; }
        event Action<LwsDepotPresenceChangedEvent> PlayerEnteredDepot;
        event Action<LwsDepotPresenceChangedEvent> PlayerExitedDepot;
        event Action<LwsDepotPresenceChangedEvent> CurrentDepotChanged;
        LwsServiceResult RegisterDepotDefinition(LwsDepotDefinition definition);
        LwsServiceResult UnregisterDepotDefinition(LwsDepotDefinition definition);
        LwsServiceResult RegisterRuntimeDepot(LwsDepotRuntime runtime);
        LwsServiceResult UnregisterRuntimeDepot(LwsDepotRuntime runtime);
        bool TryResolveDepot(string stableDepotId, out LwsDepotDefinition definition);
        void NotifyPlayerEnteredDepot(LwsDepotRuntime runtime, LwsPlayerTruck playerTruck);
        void NotifyPlayerExitedDepot(LwsDepotRuntime runtime, LwsPlayerTruck playerTruck);
        void ClearPresence(string reason);
    }

    public sealed class LwsDepotService : ILwsDepotService
    {
        private readonly Dictionary<string, LwsDepotDefinition> _definitions = new Dictionary<string, LwsDepotDefinition>(StringComparer.Ordinal);
        private readonly List<LwsDepotRuntime> _runtimeDepots = new List<LwsDepotRuntime>();
        private ILwsGameplayStateService _gameplayStateService;
        private LwsDepotDefinition _currentDepotDefinition;
        private LwsDepotRuntime _currentDepotRuntime;
        private LwsPlayerTruck _currentPlayerTruck;

        public string ServiceId => "lws.depot";
        public IReadOnlyList<LwsDepotDefinition> DepotDefinitions => _definitions.Values.OrderBy(d => d.StableDepotId, StringComparer.Ordinal).ToList();
        public IReadOnlyList<LwsDepotRuntime> RuntimeDepots => _runtimeDepots;
        public bool IsPlayerAtDepot => _currentDepotDefinition != null;
        public string CurrentDepotId => _currentDepotDefinition != null ? _currentDepotDefinition.StableDepotId : string.Empty;
        public LwsDepotDefinition CurrentDepotDefinition => _currentDepotDefinition;
        public LwsDepotRuntime CurrentDepotRuntime => _currentDepotRuntime;

        public event Action<LwsDepotPresenceChangedEvent> PlayerEnteredDepot;
        public event Action<LwsDepotPresenceChangedEvent> PlayerExitedDepot;
        public event Action<LwsDepotPresenceChangedEvent> CurrentDepotChanged;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            context.Registry.TryGet(out _gameplayStateService);
            return LwsServiceResult.Success("LWS depot presence service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _definitions.Clear();
            _runtimeDepots.Clear();
            _currentDepotDefinition = null;
            _currentDepotRuntime = null;
            _currentPlayerTruck = null;
            _gameplayStateService = null;
            PlayerEnteredDepot = null;
            PlayerExitedDepot = null;
            CurrentDepotChanged = null;
            return LwsServiceResult.Success("LWS depot presence service shut down.");
        }

        public LwsServiceResult RegisterDepotDefinition(LwsDepotDefinition definition)
        {
            if (definition == null)
            {
                return LwsServiceResult.Failure("Cannot register a null depot definition.");
            }

            if (!definition.Validate(out string message))
            {
                return LwsServiceResult.Failure(message);
            }

            if (_definitions.TryGetValue(definition.StableDepotId, out LwsDepotDefinition existing) && existing != definition)
            {
                return LwsServiceResult.Failure($"Duplicate depot ID registered: {definition.StableDepotId}");
            }

            _definitions[definition.StableDepotId] = definition;
            return LwsServiceResult.Success($"Registered depot definition {definition.StableDepotId}.");
        }

        public LwsServiceResult UnregisterDepotDefinition(LwsDepotDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.StableDepotId))
            {
                return LwsServiceResult.Success("No depot definition to unregister.");
            }

            _definitions.Remove(definition.StableDepotId);
            if (_currentDepotDefinition == definition)
            {
                ClearPresence("Depot definition unregistered.");
            }

            return LwsServiceResult.Success($"Unregistered depot definition {definition.StableDepotId}.");
        }

        public LwsServiceResult RegisterRuntimeDepot(LwsDepotRuntime runtime)
        {
            if (runtime == null)
            {
                return LwsServiceResult.Failure("Cannot register a null depot runtime.");
            }

            if (!_runtimeDepots.Contains(runtime))
            {
                _runtimeDepots.Add(runtime);
            }

            if (runtime.Definition != null)
            {
                LwsServiceResult result = RegisterDepotDefinition(runtime.Definition);
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            return LwsServiceResult.Success($"Registered depot runtime {runtime.name}.");
        }

        public LwsServiceResult UnregisterRuntimeDepot(LwsDepotRuntime runtime)
        {
            if (runtime == null)
            {
                return LwsServiceResult.Success("No depot runtime to unregister.");
            }

            _runtimeDepots.Remove(runtime);
            if (_currentDepotRuntime == runtime)
            {
                ClearPresence("Depot runtime unregistered.");
            }

            return LwsServiceResult.Success($"Unregistered depot runtime {runtime.name}.");
        }

        public bool TryResolveDepot(string stableDepotId, out LwsDepotDefinition definition)
        {
            return _definitions.TryGetValue(stableDepotId ?? string.Empty, out definition);
        }

        public void NotifyPlayerEnteredDepot(LwsDepotRuntime runtime, LwsPlayerTruck playerTruck)
        {
            if (runtime == null || playerTruck == null || runtime.Definition == null || !runtime.Definition.Enabled)
            {
                return;
            }

            if (_currentDepotRuntime == runtime && _currentPlayerTruck == playerTruck)
            {
                return;
            }

            _currentDepotDefinition = runtime.Definition;
            _currentDepotRuntime = runtime;
            _currentPlayerTruck = playerTruck;

            var evt = new LwsDepotPresenceChangedEvent(runtime.Definition, runtime, playerTruck, true, "Canonical player tractor entered depot zone.");
            PlayerEnteredDepot?.Invoke(evt);
            CurrentDepotChanged?.Invoke(evt);
            TryEnterAtDepotState(runtime.Definition);
        }

        public void NotifyPlayerExitedDepot(LwsDepotRuntime runtime, LwsPlayerTruck playerTruck)
        {
            if (runtime == null || _currentDepotRuntime != runtime)
            {
                return;
            }

            LwsDepotDefinition exited = _currentDepotDefinition;
            var evt = new LwsDepotPresenceChangedEvent(exited, runtime, playerTruck, false, "Canonical player tractor exited depot zone.");
            _currentDepotDefinition = null;
            _currentDepotRuntime = null;
            _currentPlayerTruck = null;

            PlayerExitedDepot?.Invoke(evt);
            CurrentDepotChanged?.Invoke(evt);
            TryExitAtDepotState();
        }

        public void ClearPresence(string reason)
        {
            if (_currentDepotDefinition == null)
            {
                return;
            }

            var evt = new LwsDepotPresenceChangedEvent(_currentDepotDefinition, _currentDepotRuntime, _currentPlayerTruck, false, reason);
            _currentDepotDefinition = null;
            _currentDepotRuntime = null;
            _currentPlayerTruck = null;
            PlayerExitedDepot?.Invoke(evt);
            CurrentDepotChanged?.Invoke(evt);
            TryExitAtDepotState();
        }

        private void TryEnterAtDepotState(LwsDepotDefinition depot)
        {
            if (_gameplayStateService == null || _gameplayStateService.CurrentState != LwsGameplayState.FreeDrive)
            {
                return;
            }

            _gameplayStateService.TryTransitionTo(LwsGameplayState.AtDepot, $"Player entered depot {depot.StableDepotId}.");
        }

        private void TryExitAtDepotState()
        {
            if (_gameplayStateService == null || _gameplayStateService.CurrentState != LwsGameplayState.AtDepot)
            {
                return;
            }

            _gameplayStateService.TryTransitionTo(LwsGameplayState.FreeDrive, "Player exited current depot.");
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsDepotRuntime : MonoBehaviour
    {
        [SerializeField] private LwsDepotDefinition definition;
        [SerializeField] private Collider zoneCollider;
        [SerializeField] private Transform jobBoardInteractionPoint;
        [SerializeField] private float jobBoardInteractionRadiusMeters = 8f;

        private readonly HashSet<Collider> _playerTractorColliders = new HashSet<Collider>();
        private ILwsDepotService _depotService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private LwsPlayerTruck _insideTruck;

        public LwsDepotDefinition Definition => definition;
        public Transform JobBoardInteractionPoint => jobBoardInteractionPoint != null ? jobBoardInteractionPoint : transform;
        public float JobBoardInteractionRadiusMeters => jobBoardInteractionRadiusMeters;
        public bool PlayerTractorInside => _playerTractorColliders.Count > 0;

        public void Configure(LwsDepotDefinition depotDefinition, Collider trigger, Transform interactionPoint, float interactionRadiusMeters)
        {
            definition = depotDefinition;
            zoneCollider = trigger;
            jobBoardInteractionPoint = interactionPoint;
            jobBoardInteractionRadiusMeters = Mathf.Max(0.5f, interactionRadiusMeters);
            EnsureTriggerCollider();
            if (isActiveAndEnabled)
            {
                ResolveServices();
                _depotService?.RegisterRuntimeDepot(this);
            }
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnEnable()
        {
            ResolveServices();
            _depotService?.RegisterRuntimeDepot(this);
        }

        private void OnDisable()
        {
            if (_insideTruck != null)
            {
                _depotService?.NotifyPlayerExitedDepot(this, _insideTruck);
            }

            _playerTractorColliders.Clear();
            _insideTruck = null;
            _depotService?.UnregisterRuntimeDepot(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryResolveCanonicalPlayerTruck(other, out LwsPlayerTruck truck))
            {
                return;
            }

            bool firstCollider = _playerTractorColliders.Count == 0;
            _playerTractorColliders.Add(other);
            _insideTruck = truck;
            if (firstCollider)
            {
                _depotService?.NotifyPlayerEnteredDepot(this, truck);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_playerTractorColliders.Remove(other) || _playerTractorColliders.Count > 0)
            {
                return;
            }

            LwsPlayerTruck truck = _insideTruck;
            _insideTruck = null;
            _depotService?.NotifyPlayerExitedDepot(this, truck);
        }

        private void EnsureTriggerCollider()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<Collider>();
            }

            if (zoneCollider == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                zoneCollider = box;
            }

            zoneCollider.isTrigger = true;
        }

        private bool TryResolveCanonicalPlayerTruck(Collider candidate, out LwsPlayerTruck truck)
        {
            truck = candidate != null ? candidate.GetComponentInParent<LwsPlayerTruck>() : null;
            if (truck == null)
            {
                return false;
            }

            ResolveServices();
            if (_playerVehicleService != null && _playerVehicleService.ActiveTruck != null && _playerVehicleService.ActiveTruck != truck)
            {
                return false;
            }

            return true;
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            if (_depotService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _depotService);
            }

            if (_playerVehicleService == null)
            {
                LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
            }
        }
    }
}
