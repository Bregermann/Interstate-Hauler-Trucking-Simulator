using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.InterstateHauler;

namespace LWS.TruckTaxi
{
    // Development Scene-view diagnostics; no gameplay collider is mutated.
    public sealed class TruckTaxiColliderDiagnostics : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private bool passengers,buses,traffic,world;
        private readonly List<Collider> colliders=new List<Collider>();
        public void Initialize(TruckTaxiBootstrap value) => host=value;
        public void TogglePassengers() {passengers=!passengers; Rebuild();}
        public void ToggleBuses() {buses=!buses; Rebuild();}
        public void ToggleTraffic() {traffic=!traffic; Rebuild();}
        public void ToggleSolidWorld() {world=!world; Rebuild();}
        public static string WorldSceneSnapshot()
        {
            var text = new StringBuilder("WORLD SCENE SNAPSHOT (observation only)\n");
            var seen = new HashSet<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                string key = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
                text.AppendLine($"{scene.handle}: {key}; loaded={scene.isLoaded}; roots={scene.rootCount}" +
                    (scene.isLoaded && !seen.Add(key) ? " DUPLICATE LOADED SCENE" : ""));
            }
            foreach (var adapter in FindObjectsByType<LwsSceneStreamerAdapter>(FindObjectsSortMode.None))
                text.AppendLine($"Streamer {TruckTaxiWorldColliderReview.Identity(adapter.transform)}: " +
                    $"pending={adapter.PendingOperationCount}; {adapter.Status}");
            foreach (var regional in FindObjectsByType<TruckTaxiRegionalWorld>(FindObjectsSortMode.None))
            {
                var registered = new HashSet<string>();
                foreach (var region in regional.regions ?? System.Array.Empty<TruckTaxiRegion>())
                    if (region != null)
                        text.AppendLine($"Registry {region.id}: {region.sceneName}; loaded={regional.Loaded(region)}" +
                            (!registered.Add(region.sceneName ?? "") ? " DUPLICATE REGISTRATION" : ""));
            }
            text.AppendLine("SceneManager excludes DontDestroyOnLoad. Use the loaded-renderer editor audit for persistent geometry.");
            return text.ToString();
        }
        private void Rebuild()
        {
            colliders.Clear();
            if(host==null) return;
            if(world) foreach(var collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if(TruckTaxiWorldColliderReview.IsSolid(collider) &&
                    (collider.attachedRigidbody==null || collider.attachedRigidbody.isKinematic)) colliders.Add(collider);
            if(passengers)
            {
                if(host.Passengers.PrimaryActor!=null) colliders.AddRange(host.Passengers.PrimaryActor.GetComponentsInChildren<Collider>());
                if(host.Passengers.SecondaryActor!=null) colliders.AddRange(host.Passengers.SecondaryActor.GetComponentsInChildren<Collider>());
            }
            if(traffic) foreach(var car in host.traffic.Vehicles) if(car!=null) colliders.AddRange(car.GetComponentsInChildren<Collider>());
            if(buses) foreach(var service in FindObjectsByType<TruckTaxiBusService>(FindObjectsSortMode.None))
                for(int i=0;i<service.RouteCount;i++) {var bus=service.GetLiveBus(i); if(bus!=null) colliders.AddRange(bus.GetComponentsInChildren<Collider>());}
            Debug.Log($"Taxi collider gizmos: {colliders.Count} cached colliders. Scene view Gizmos must be enabled.");
        }
        private void OnDrawGizmos()
        {
            foreach(var collider in colliders) if(collider!=null && collider.enabled)
            {
                Gizmos.color=collider.isTrigger ? Color.yellow : Color.cyan;
                var previous = Gizmos.matrix;
                if (collider is BoxCollider box)
                {
                    Gizmos.matrix = box.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else { Gizmos.matrix = Matrix4x4.identity; Gizmos.DrawWireCube(collider.bounds.center,collider.bounds.size); }
                Gizmos.matrix = previous;
            }
        }
        public static string ObjectiveTimers(TruckTaxiSession session)
        {
            var text=new StringBuilder("TAXI OBJECTIVE TIMERS\n");
            foreach(var request in session.Requests) text.AppendLine($"{request.Description}: {request.State} / "+
                (TruckTaxiRequestPolicy.IsTimed(request.Definition) ? $"{request.Remaining:0.0}s" : "UNTIMED"));
            return text.ToString();
        }
    }
}
