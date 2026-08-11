using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsNavigationRoutePresenter
    {
        string PresenterId { get; }
        bool PresentRoute(LwsRouteResult route);
        void ClearRoute();
    }

    public interface ILwsNavigationService : ILwsService
    {
        LwsRouteResult CurrentRoute { get; }
        LwsRouteResult RequestRoute(LwsRouteRequest request, LwsRoadGraph graph);
        void SetPresenter(ILwsNavigationRoutePresenter presenter);
        bool PresentCurrentRoute();
        void ClearRoute();
    }

    public sealed class LwsNavigationService : ILwsNavigationService
    {
        private ILwsNavigationRoutePresenter _presenter;

        public string ServiceId => "lws.navigation";
        public LwsRouteResult CurrentRoute { get; private set; }

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            CurrentRoute = null;
            return LwsServiceResult.Success("LWS navigation service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ClearRoute();
            _presenter = null;
            return LwsServiceResult.Success("LWS navigation service shut down.");
        }

        public void SetPresenter(ILwsNavigationRoutePresenter presenter)
        {
            _presenter = presenter;
        }

        public LwsRouteResult RequestRoute(LwsRouteRequest request, LwsRoadGraph graph)
        {
            if (request == null)
            {
                CurrentRoute = new LwsRouteResult { succeeded = false, message = "Route request is null." };
                return CurrentRoute;
            }

            if (graph == null || !graph.Validate().IsValid)
            {
                CurrentRoute = new LwsRouteResult { routeId = request.requestId, succeeded = false, message = "Road graph is missing or invalid." };
                return CurrentRoute;
            }

            // This shell only validates ownership and data flow. Production routing
            // belongs to a later prompt.
            CurrentRoute = new LwsRouteResult
            {
                routeId = request.requestId,
                succeeded = false,
                message = "Route solving is deferred; LWS road graph owns the future computation."
            };
            return CurrentRoute;
        }

        public bool PresentCurrentRoute()
        {
            return CurrentRoute != null && _presenter != null && _presenter.PresentRoute(CurrentRoute);
        }

        public void ClearRoute()
        {
            CurrentRoute = null;
            _presenter?.ClearRoute();
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsCompassRoutePresenter : MonoBehaviour, ILwsNavigationRoutePresenter
    {
        [SerializeField] private MonoBehaviour compassBehaviour;
        [SerializeField] private string presenterId = "compass.navigator";

        public string PresenterId => presenterId;

        public bool PresentRoute(LwsRouteResult route)
        {
            if (route == null || route.waypoints == null || route.waypoints.Count == 0)
            {
                return false;
            }

            object compass = compassBehaviour != null ? compassBehaviour : ResolveCompassInstance();
            if (compass == null)
            {
                return false;
            }

            MethodInfo setRoute = compass.GetType().GetMethod("SetRoute", BindingFlags.Instance | BindingFlags.Public);
            if (setRoute == null)
            {
                return false;
            }

            setRoute.Invoke(compass, new object[] { route.waypoints });
            return true;
        }

        public void ClearRoute()
        {
            object compass = compassBehaviour != null ? compassBehaviour : ResolveCompassInstance();
            MethodInfo clearRoute = compass?.GetType().GetMethod("ClearRoute", BindingFlags.Instance | BindingFlags.Public);
            clearRoute?.Invoke(compass, null);
        }

        private static object ResolveCompassInstance()
        {
            Type compassType = Type.GetType("CompassNavigatorPro.CompassPro, Assembly-CSharp-firstpass")
                ?? Type.GetType("CompassNavigatorPro.CompassPro, Assembly-CSharp");
            PropertyInfo instanceProperty = compassType?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
            return instanceProperty?.GetValue(null);
        }
    }
}
