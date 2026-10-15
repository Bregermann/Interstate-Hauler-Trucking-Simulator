using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsTrailerRegistryService : ILwsTrailerRegistryService
    {
        private readonly Dictionary<string, LwsTrailerGameplayIdentity> _trailersById = new Dictionary<string, LwsTrailerGameplayIdentity>(StringComparer.Ordinal);
        private readonly List<LwsTrailerGameplayIdentity> _registeredTrailers = new List<LwsTrailerGameplayIdentity>();

        public string ServiceId => "lws.trailers.registry";
        public IReadOnlyList<LwsTrailerGameplayIdentity> RegisteredTrailers => _registeredTrailers;
        public event Action<LwsTrailerGameplayIdentity> TrailerRegistered;
        public event Action<string> TrailerUnregistered;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            RegisterExistingSceneTrailers();
            return LwsServiceResult.Success($"LWS trailer registry initialized with {_registeredTrailers.Count} trailer(s).");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            _trailersById.Clear();
            _registeredTrailers.Clear();
            TrailerRegistered = null;
            TrailerUnregistered = null;
            return LwsServiceResult.Success("LWS trailer registry shut down.");
        }

        public LwsServiceResult RegisterTrailer(LwsTrailerGameplayIdentity trailer)
        {
            if (trailer == null)
            {
                return LwsServiceResult.Failure("Cannot register a null trailer identity.");
            }

            if (!trailer.Validate(out string validationMessage))
            {
                return LwsServiceResult.Failure(validationMessage);
            }

            string id = trailer.StableTrailerId;
            if (_trailersById.TryGetValue(id, out LwsTrailerGameplayIdentity existing) && existing != null && existing != trailer)
            {
                return LwsServiceResult.Failure($"Duplicate trailer identity '{id}' on {existing.name} and {trailer.name}.");
            }

            _trailersById[id] = trailer;
            if (!_registeredTrailers.Contains(trailer))
            {
                _registeredTrailers.Add(trailer);
            }

            TrailerRegistered?.Invoke(trailer);
            return LwsServiceResult.Success($"Registered trailer {id}.");
        }

        public LwsServiceResult UnregisterTrailer(LwsTrailerGameplayIdentity trailer)
        {
            if (trailer == null)
            {
                return LwsServiceResult.Success("No trailer identity to unregister.");
            }

            string id = trailer.StableTrailerId;
            _registeredTrailers.Remove(trailer);
            if (_trailersById.TryGetValue(id, out LwsTrailerGameplayIdentity existing) && existing == trailer)
            {
                _trailersById.Remove(id);
                TrailerUnregistered?.Invoke(id);
            }

            return LwsServiceResult.Success($"Unregistered trailer {id}.");
        }

        public bool TryResolveTrailer(string stableTrailerId, out LwsTrailerGameplayIdentity trailer)
        {
            PruneDestroyedTrailers();
            return _trailersById.TryGetValue(Normalize(stableTrailerId), out trailer) && trailer != null;
        }

        public IReadOnlyList<LwsTrailerGameplayIdentity> FindAssignableTrailers(string requiredTrailerTypeId, string preferredOriginDepotId, string activeJobId)
        {
            PruneDestroyedTrailers();
            string normalizedOrigin = Normalize(preferredOriginDepotId);
            return _registeredTrailers
                .Where(t => t != null && t.CanAssignTo(activeJobId, requiredTrailerTypeId))
                .OrderByDescending(t => string.Equals(Normalize(t.OriginDepotId), normalizedOrigin, StringComparison.Ordinal))
                .ThenBy(t => t.StableTrailerId, StringComparer.Ordinal)
                .ToList();
        }

        private void RegisterExistingSceneTrailers()
        {
            foreach (LwsTrailerGameplayIdentity trailer in UnityEngine.Object.FindObjectsByType<LwsTrailerGameplayIdentity>())
            {
                RegisterTrailer(trailer);
            }
        }

        private void PruneDestroyedTrailers()
        {
            for (int i = _registeredTrailers.Count - 1; i >= 0; i--)
            {
                LwsTrailerGameplayIdentity trailer = _registeredTrailers[i];
                if (trailer != null)
                {
                    continue;
                }

                _registeredTrailers.RemoveAt(i);
            }

            foreach (string key in _trailersById.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList())
            {
                _trailersById.Remove(key);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
