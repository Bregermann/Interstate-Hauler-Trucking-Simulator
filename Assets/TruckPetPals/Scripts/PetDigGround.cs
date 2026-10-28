using UnityEngine;
namespace LWS.TruckPetPals
{
    public sealed class PetDigGround : MonoBehaviour
    {
        public Terrain terrain;
        public TerrainCollider physicalTerrain;
        TerrainData original, owned;
        bool[,] holes;
        public void OpenPatch(Vector3 center, float radius)
        {
            if (owned == null)
            {
                original = terrain.terrainData; owned = Instantiate(original); owned.name = original.name + " · runtime pet digging only";
                terrain.terrainData = owned; physicalTerrain.terrainData = owned; holes = original.GetHoles(0, 0, original.holesResolution, original.holesResolution);
            }
            int resolution = owned.holesResolution; var size = owned.size; var local = center - terrain.transform.position;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((local.x - radius) / size.x * resolution), 0, resolution - 1), x1 = Mathf.Clamp(Mathf.CeilToInt((local.x + radius) / size.x * resolution), 0, resolution - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((local.z - radius) / size.z * resolution), 0, resolution - 1), z1 = Mathf.Clamp(Mathf.CeilToInt((local.z + radius) / size.z * resolution), 0, resolution - 1);
            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
            {
                float px = (x + .5f) / resolution * size.x - local.x, pz = (z + .5f) / resolution * size.z - local.z;
                if (px * px + pz * pz <= radius * radius) holes[z, x] = false;
            }
            owned.SetHoles(0, 0, holes);
        }
        public float Height(Vector3 point) => (original == null ? terrain.terrainData : original).GetInterpolatedHeight((point.x - terrain.transform.position.x) / terrain.terrainData.size.x, (point.z - terrain.transform.position.z) / terrain.terrainData.size.z) + terrain.transform.position.y;
        public void ResetGround()
        {
            if (owned == null) return;
            if (terrain != null && terrain.terrainData == owned) terrain.terrainData = original;
            if (physicalTerrain != null && physicalTerrain.terrainData == owned) physicalTerrain.terrainData = original;
            Destroy(owned); owned = null; holes = null;
        }
        void OnDestroy() { ResetGround(); }
    }
}
