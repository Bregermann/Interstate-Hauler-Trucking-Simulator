using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [CreateAssetMenu(menuName="Truck Taxi/Regional Map Tiles")]
    public sealed class TruckTaxiMapTileCatalog : ScriptableObject
    {
        public const string ResourcePath="TruckTaxi/RegionalMapTiles";
        [Tooltip("Explicit material dependency keeps the map shader available in player builds.")]
        public Material tileMaterial;
        [Serializable]
        public sealed class Tile
        {
            public string sceneName;
            public Texture2D texture;
            public Vector2 center;
            public Vector2 size;
        }
        public Tile[] tiles=Array.Empty<Tile>();
    }
}
