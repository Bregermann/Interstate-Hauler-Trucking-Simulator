using System;
using UnityEngine;
using UnityEngine.AI;
namespace LWS.TruckCamping
{
    public enum CampPlaceKind { Campsite, Lake, Waterfall, Cabin, Overlook, Cave, Meadow, Bridge, DiggingGlade, RestClearing }
    public sealed class CampingWilderness : MonoBehaviour
    {
        // Both scenes serialize the very same prefab. State is mode-owned;
        // geometry, navigation, water, wildlife and terrain have one definition.
        public const string SharedPrefab = "Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab";
        public Terrain[] terrains;
        public CampLandmark[] landmarks;
        public CampResource[] resources;
        public CampSite[] campsites;
        public CampHammock[] hammocks;
        public Transform[] route;
        public CampWildlife[] animals;
        public Vector2 min = new Vector2(-980, -980), max = new Vector2(980, 980);
        public CampingMatch session;
        void Start() { CampLandmarkRockPresentation.TryInstall(this); }
        public float Height(Vector3 p)
        {
            foreach (var terrain in terrains)
            {
                var local = p - terrain.transform.position;
                if (local.x >= 0 && local.z >= 0 && local.x <= terrain.terrainData.size.x && local.z <= terrain.terrainData.size.z) return terrain.SampleHeight(p) + terrain.transform.position.y;
            }
            return 0;
        }
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.z >= min.y && p.z <= max.y;
        public bool WalkingPoint(Vector3 desired, out Vector3 safe)
        {
            safe = desired;
            if (!Contains(desired) || !NavMesh.SamplePosition(desired, out var hit, 8, NavMesh.AllAreas)) return false;
            safe = hit.position; return true;
        }
        public CampLandmark Nearest(Vector3 p)
        {
            CampLandmark best = null; float distance = float.PositiveInfinity;
            foreach (var point in landmarks) { float d = (point.transform.position - p).sqrMagnitude; if (d < distance) { best = point; distance = d; } }
            return best;
        }
        public void ResetWorkshop()
        {
            foreach (var resource in resources) resource.Restore(0);
            foreach (var camp in campsites) camp.ResetSite();
        }
        public void Tick(float dt)
        {
            foreach (var resource in resources) resource.Tick(dt);
            foreach (var camp in campsites) camp.Tick(dt);
            foreach (var hammock in hammocks) hammock.Tick(dt);
        }
    }
}
