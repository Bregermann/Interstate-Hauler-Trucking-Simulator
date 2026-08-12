using System.Collections.Generic;
using NWH.VehiclePhysics2.Input;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(-110)]
    [DisallowMultipleComponent]
    public sealed class LwsWheelInputBootstrap : MonoBehaviour
    {
        [SerializeField] private LwsWheelInputSource wheelInputSource;
        [SerializeField] private LwsNwhVehicleInputProvider nwhVehicleInputProvider;
        [SerializeField] private bool preferWheelWhenDetected = true;
        [SerializeField] private bool disableStockNwhVehicleProvidersWhenWheelOwns = true;
        [SerializeField] private bool restoreFallbackProvidersOnRelease = true;

        private readonly List<VehicleInputProviderBase> _disabledFallbackProviders = new List<VehicleInputProviderBase>();
        private ILwsVehicleInputService _inputService;
        private bool _wheelOwnsInput;

        public bool WheelOwnsInput => _wheelOwnsInput;
        public int DisabledFallbackProviderCount => _disabledFallbackProviders.Count;

        private void Reset()
        {
            ResolveLocalReferences();
        }

        private void Awake()
        {
            ResolveLocalReferences();
        }

        private void Start()
        {
            ResolveServices();
            if (nwhVehicleInputProvider != null)
            {
                nwhVehicleInputProvider.SetInputSource(wheelInputSource);
            }

            if (preferWheelWhenDetected && wheelInputSource != null && wheelInputSource.SelectPreferredDevice())
            {
                ActivateWheelOwner();
            }
        }

        private void Update()
        {
            if (wheelInputSource == null)
            {
                return;
            }

            if (preferWheelWhenDetected && !_wheelOwnsInput && wheelInputSource.HasConnectedDevice)
            {
                ActivateWheelOwner();
            }
            else if (_wheelOwnsInput && !wheelInputSource.HasConnectedDevice)
            {
                ReleaseWheelOwner();
            }
        }

        public LwsServiceResult ActivateWheelOwner()
        {
            ResolveLocalReferences();
            ResolveServices();

            if (wheelInputSource == null || nwhVehicleInputProvider == null)
            {
                return LwsServiceResult.Failure("Wheel input bootstrap is missing its wheel source or NWH bridge provider.");
            }

            nwhVehicleInputProvider.SetInputSource(wheelInputSource);
            nwhVehicleInputProvider.enabled = true;

            if (disableStockNwhVehicleProvidersWhenWheelOwns)
            {
                DisableFallbackVehicleProviders();
            }

            LwsServiceResult result = _inputService != null
                ? _inputService.SetInputSource(wheelInputSource, LwsVehicleInputOwner.Wheel, true)
                : LwsServiceResult.Success("No LWS input service available; wheel provider still feeds NWH directly.");
            _wheelOwnsInput = result.Succeeded;
            return result;
        }

        public LwsServiceResult ReleaseWheelOwner()
        {
            if (_wheelOwnsInput && _inputService != null)
            {
                _inputService.ReleaseInputSource(wheelInputSource);
            }

            wheelInputSource?.NeutralizeForDisconnect("Wheel ownership released; continuous vehicle input neutralized.");
            _wheelOwnsInput = false;
            if (restoreFallbackProvidersOnRelease)
            {
                RestoreFallbackVehicleProviders();
            }

            return LwsServiceResult.Success("Wheel input ownership released.");
        }

        private void ResolveLocalReferences()
        {
            if (wheelInputSource == null)
            {
                wheelInputSource = GetComponent<LwsWheelInputSource>();
            }

            if (wheelInputSource == null)
            {
                wheelInputSource = gameObject.AddComponent<LwsWheelInputSource>();
            }

            if (nwhVehicleInputProvider == null)
            {
                nwhVehicleInputProvider = GetComponent<LwsNwhVehicleInputProvider>();
            }

            if (nwhVehicleInputProvider == null)
            {
                nwhVehicleInputProvider = gameObject.AddComponent<LwsNwhVehicleInputProvider>();
            }
        }

        private void ResolveServices()
        {
            if (_inputService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
        }

        private void DisableFallbackVehicleProviders()
        {
            _disabledFallbackProviders.Clear();
            VehicleInputProviderBase[] providers = FindObjectsByType<VehicleInputProviderBase>(FindObjectsSortMode.None);
            foreach (VehicleInputProviderBase provider in providers)
            {
                if (provider == null || provider == nwhVehicleInputProvider || !provider.enabled)
                {
                    continue;
                }

                provider.enabled = false;
                _disabledFallbackProviders.Add(provider);
            }
        }

        private void RestoreFallbackVehicleProviders()
        {
            foreach (VehicleInputProviderBase provider in _disabledFallbackProviders)
            {
                if (provider != null)
                {
                    provider.enabled = true;
                }
            }

            _disabledFallbackProviders.Clear();
        }
    }
}
