using System.Collections;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiEdenboroughSceneBinding : MonoBehaviour
    {
        public TruckTaxiBootstrap host;
        public TruckTaxiEdenboroughPalette palette;
        public TruckTaxiGarage garage;
        public BoxCollider garageVolume;
        private IEnumerator Start()
        {
            if (host == null || palette == null || garage == null || garageVolume == null)
            { Debug.LogError("Edenborough garage authoring references are incomplete.", this); yield break; }
            while (!host.StartupComplete) yield return null;
            if (!host.Ready) yield break;
            garage.Configure(host, host.hud, garageVolume, palette.truckPaintMaterials);
        }
    }
}
