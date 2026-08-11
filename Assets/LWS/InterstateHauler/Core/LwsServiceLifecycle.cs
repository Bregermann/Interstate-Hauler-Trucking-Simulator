using System;
using System.Collections.Generic;
using System.Linq;

namespace LWS.InterstateHauler
{
    public enum LwsServiceState
    {
        Uninitialized,
        Initializing,
        Ready,
        ShuttingDown,
        Shutdown,
        Failed
    }

    public readonly struct LwsServiceResult
    {
        public bool Succeeded { get; }
        public string Message { get; }

        private LwsServiceResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public static LwsServiceResult Success(string message = "")
        {
            return new LwsServiceResult(true, message);
        }

        public static LwsServiceResult Failure(string message)
        {
            return new LwsServiceResult(false, string.IsNullOrWhiteSpace(message) ? "Service operation failed." : message);
        }
    }

    public sealed class LwsServiceContext
    {
        public LwsServiceContext(LwsServiceRegistry registry)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public LwsServiceRegistry Registry { get; }
    }

    public interface ILwsService
    {
        string ServiceId { get; }
        LwsServiceResult Initialize(LwsServiceContext context);
        LwsServiceResult Shutdown(LwsServiceContext context);
    }

    public sealed class LwsServiceDiagnostic
    {
        public LwsServiceDiagnostic(string serviceId, LwsServiceState state, string message)
        {
            ServiceId = serviceId;
            State = state;
            Message = message ?? string.Empty;
        }

        public string ServiceId { get; }
        public LwsServiceState State { get; }
        public string Message { get; }
    }

    public sealed class LwsServiceRegistration
    {
        private readonly List<Type> _dependencies;

        internal LwsServiceRegistration(Type serviceType, ILwsService service, IEnumerable<Type> dependencies)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Service = service ?? throw new ArgumentNullException(nameof(service));
            State = LwsServiceState.Uninitialized;
            _dependencies = dependencies?.Distinct().ToList() ?? new List<Type>();
        }

        public Type ServiceType { get; }
        public ILwsService Service { get; }
        public string ServiceId => Service.ServiceId;
        public LwsServiceState State { get; internal set; }
        public IReadOnlyList<Type> Dependencies => _dependencies;
    }

    public sealed class LwsServiceRegistry
    {
        private readonly List<LwsServiceRegistration> _registrations = new List<LwsServiceRegistration>();
        private readonly Dictionary<Type, LwsServiceRegistration> _byType = new Dictionary<Type, LwsServiceRegistration>();
        private readonly Dictionary<string, LwsServiceRegistration> _byId = new Dictionary<string, LwsServiceRegistration>(StringComparer.Ordinal);
        private readonly List<LwsServiceDiagnostic> _diagnostics = new List<LwsServiceDiagnostic>();

        public IReadOnlyList<LwsServiceRegistration> Registrations => _registrations;
        public IReadOnlyList<LwsServiceDiagnostic> Diagnostics => _diagnostics;

        public T Register<T>(T service, params Type[] dependencies) where T : class, ILwsService
        {
            Register(typeof(T), service, dependencies);
            return service;
        }

        public void Register(Type serviceType, ILwsService service, params Type[] dependencies)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (!typeof(ILwsService).IsAssignableFrom(serviceType))
            {
                throw new ArgumentException($"{serviceType.Name} must implement {nameof(ILwsService)}.", nameof(serviceType));
            }

            if (string.IsNullOrWhiteSpace(service.ServiceId))
            {
                throw new ArgumentException("LWS service IDs must be stable, non-empty strings.", nameof(service));
            }

            if (_byType.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"Service type already registered: {serviceType.FullName}");
            }

            if (_byId.ContainsKey(service.ServiceId))
            {
                throw new InvalidOperationException($"Service ID already registered: {service.ServiceId}");
            }

            var registration = new LwsServiceRegistration(serviceType, service, dependencies);
            _registrations.Add(registration);
            _byType.Add(serviceType, registration);
            _byId.Add(service.ServiceId, registration);
        }

        public bool TryGet<T>(out T service) where T : class, ILwsService
        {
            if (_byType.TryGetValue(typeof(T), out LwsServiceRegistration registration))
            {
                service = registration.Service as T;
                return service != null;
            }

            service = null;
            return false;
        }

        public T GetRequired<T>() where T : class, ILwsService
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"Required service is missing: {typeof(T).FullName}");
        }

        public IReadOnlyList<LwsServiceDiagnostic> ValidateRegistration()
        {
            _diagnostics.Clear();

            foreach (LwsServiceRegistration registration in _registrations)
            {
                foreach (Type dependency in registration.Dependencies)
                {
                    if (!_byType.ContainsKey(dependency))
                    {
                        _diagnostics.Add(new LwsServiceDiagnostic(
                            registration.ServiceId,
                            registration.State,
                            $"Missing dependency: {dependency.FullName}"));
                    }
                }
            }

            return _diagnostics;
        }

        public LwsServiceResult InitializeAll()
        {
            ValidateRegistration();
            if (_diagnostics.Count > 0)
            {
                return LwsServiceResult.Failure(string.Join("; ", _diagnostics.Select(d => d.Message)));
            }

            var context = new LwsServiceContext(this);
            var initialized = new HashSet<Type>();

            foreach (LwsServiceRegistration registration in _registrations)
            {
                foreach (Type dependency in registration.Dependencies)
                {
                    if (!initialized.Contains(dependency))
                    {
                        registration.State = LwsServiceState.Failed;
                        string message = $"Service {registration.ServiceId} initialized before dependency {dependency.FullName}.";
                        _diagnostics.Add(new LwsServiceDiagnostic(registration.ServiceId, registration.State, message));
                        return LwsServiceResult.Failure(message);
                    }
                }

                registration.State = LwsServiceState.Initializing;
                LwsServiceResult result = registration.Service.Initialize(context);
                if (!result.Succeeded)
                {
                    registration.State = LwsServiceState.Failed;
                    _diagnostics.Add(new LwsServiceDiagnostic(registration.ServiceId, registration.State, result.Message));
                    return result;
                }

                registration.State = LwsServiceState.Ready;
                initialized.Add(registration.ServiceType);
            }

            return LwsServiceResult.Success("All LWS services initialized.");
        }

        public LwsServiceResult ShutdownAll()
        {
            var context = new LwsServiceContext(this);
            for (int i = _registrations.Count - 1; i >= 0; i--)
            {
                LwsServiceRegistration registration = _registrations[i];
                if (registration.State != LwsServiceState.Ready && registration.State != LwsServiceState.Failed)
                {
                    continue;
                }

                registration.State = LwsServiceState.ShuttingDown;
                LwsServiceResult result = registration.Service.Shutdown(context);
                registration.State = result.Succeeded ? LwsServiceState.Shutdown : LwsServiceState.Failed;

                if (!result.Succeeded)
                {
                    _diagnostics.Add(new LwsServiceDiagnostic(registration.ServiceId, registration.State, result.Message));
                    return result;
                }
            }

            return LwsServiceResult.Success("All LWS services shut down.");
        }

        public bool AreAllReady()
        {
            return _registrations.Count > 0 && _registrations.All(r => r.State == LwsServiceState.Ready);
        }
    }
}
