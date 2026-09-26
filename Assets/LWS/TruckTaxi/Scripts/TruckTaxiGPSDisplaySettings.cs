using UnityEngine;

namespace LWS.TruckTaxi
{
    // Local display preferences only, like the existing LWS GPS voice preference.
    // No routes, ride progress, or physical GPS transforms are saved here.
    public sealed class TruckTaxiGPSDisplaySettings
    {
        public const string PreferencePrefix = "ih.truckTaxi.gps.";
        public bool showHud = true;
        public bool northUp;
        public bool showPois = true;
        public float hudSize = .3f;
        public float hudRangeMeters = 700;
        public float cabRangeMeters = 700;
        public float routeWidth = 12;
        public int routeColorIndex;
        public static readonly Color[] RouteColors = {
            new Color(0,.85f,1), new Color(1,.8f,.05f), new Color(1,.2f,.7f), new Color(.3f,1,.35f)
        };
        public Color RouteColor => RouteColors[Mathf.Clamp(routeColorIndex,0,RouteColors.Length-1)];
        public void Validate()
        {
            hudSize=Clamp(hudSize,.18f,.36f,.3f);
            hudRangeMeters=Clamp(hudRangeMeters,150,1600,700);
            cabRangeMeters=Clamp(cabRangeMeters,150,1600,700);
            routeWidth=Clamp(routeWidth,4,24,12);
            routeColorIndex=Mathf.Clamp(routeColorIndex,0,RouteColors.Length-1);
        }
        private static float Clamp(float value,float min,float max,float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value,min,max);
        public static TruckTaxiGPSDisplaySettings Load(string prefix = PreferencePrefix)
        {
            var s=new TruckTaxiGPSDisplaySettings {
                showHud=PlayerPrefs.GetInt(prefix+"hud",1)!=0,
                northUp=PlayerPrefs.GetInt(prefix+"north",0)!=0,
                showPois=PlayerPrefs.GetInt(prefix+"pois",1)!=0,
                hudSize=PlayerPrefs.GetFloat(prefix+"size",.3f),
                hudRangeMeters=PlayerPrefs.GetFloat(prefix+"hudRange",700),
                cabRangeMeters=PlayerPrefs.GetFloat(prefix+"cabRange",700),
                routeWidth=PlayerPrefs.GetFloat(prefix+"width",12),
                routeColorIndex=PlayerPrefs.GetInt(prefix+"color",0)
            };
            s.Validate(); return s;
        }
        public void Save(string prefix = PreferencePrefix)
        {
            Validate();
            PlayerPrefs.SetInt(prefix+"hud",showHud ? 1 : 0);
            PlayerPrefs.SetInt(prefix+"north",northUp ? 1 : 0);
            PlayerPrefs.SetInt(prefix+"pois",showPois ? 1 : 0);
            PlayerPrefs.SetFloat(prefix+"size",hudSize);
            PlayerPrefs.SetFloat(prefix+"hudRange",hudRangeMeters);
            PlayerPrefs.SetFloat(prefix+"cabRange",cabRangeMeters);
            PlayerPrefs.SetFloat(prefix+"width",routeWidth);
            PlayerPrefs.SetInt(prefix+"color",routeColorIndex);
            // Flush on menu close/application quit, not on every slider sample.
        }
    }
}
