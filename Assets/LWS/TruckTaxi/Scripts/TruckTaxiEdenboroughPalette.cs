using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName="Truck Taxi/Edenborough/Reviewed Art Palette")]
    public sealed class TruckTaxiEdenboroughPalette : ScriptableObject
    {
        [Tooltip("Exact inspected prefab references. Scene builders never substitute guessed Asset Store names.")]
        public GameObject[] trees=Array.Empty<GameObject>();
        public GameObject[] roadside=Array.Empty<GameObject>();
        public GameObject[] industrial=Array.Empty<GameObject>();
        public GameObject[] construction=Array.Empty<GameObject>();
        public GameObject[] urbanBuildings=Array.Empty<GameObject>();
        public GameObject[] residentialBuildings=Array.Empty<GameObject>();
        public GameObject[] warehouseBuildings=Array.Empty<GameObject>();
        public GameObject[] dinerModules=Array.Empty<GameObject>();
        public GameObject[] motelModules=Array.Empty<GameObject>();
        public GameObject[] storeModules=Array.Empty<GameObject>();
        public GameObject[] garageModules=Array.Empty<GameObject>();
        public GameObject[] bathroomFixtures=Array.Empty<GameObject>();
        public GameObject[] pumps=Array.Empty<GameObject>();
        public GameObject[] signs=Array.Empty<GameObject>();
        public GameObject[] dinerSeats=Array.Empty<GameObject>();
        public GameObject[] motelBeds=Array.Empty<GameObject>();
        public GameObject[] shopCounters=Array.Empty<GameObject>();
        public GameObject[] shopProducts=Array.Empty<GameObject>();
        [Header("Reviewed garage module alignment relative to the drive-through bay")]
        public Vector3 garageLocalPosition;
        public Vector3 garageLocalEuler;
        public Vector3 garageLocalScale=Vector3.one;
        public Material[] truckPaintMaterials=Array.Empty<Material>();
        [TextArea] public string inspectionManifest;
        public bool HasCoreContent => trees.Length>0 && roadside.Length>0 && industrial.Length>0 &&
            urbanBuildings.Length>0 && garageModules.Length>0 && truckPaintMaterials.Length>0;
    }
}
