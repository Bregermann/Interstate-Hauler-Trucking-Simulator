using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsRenderValidationSceneMarker : MonoBehaviour
    {
        public LwsRenderingSettings renderingSettings;
        public string[] representativeSystems =
        {
            "NWH",
            "EasyRoads",
            "Vista",
            "Weather Maker",
            "Weatherade",
            "Compass",
            "Heat",
            "Pixel Crushers",
            "River Modeler",
            "VFX Graph",
            "UTS"
        };
        public string notes = "Prompt 003 render-validation scene. Vendor assets are referenced or documented without copying vendor content.";
    }
}
