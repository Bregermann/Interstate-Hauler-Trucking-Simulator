using UnityEngine;

namespace LWS.TruckTaxi
{
    public sealed class TruckTaxiCabLookSettings
    {
        public const string PreferencePrefix="TruckTaxi.CabLook.";
        public float MouseSensitivity { get; private set; }=0.12f;
        public float GamepadSensitivity { get; private set; }=105f;
        public float Smoothing { get; private set; }=0.06f;
        public bool InvertY { get; private set; }

        public static TruckTaxiCabLookSettings Load()
        {
            var value=new TruckTaxiCabLookSettings();
            value.SetMouseSensitivity(PlayerPrefs.GetFloat(PreferencePrefix+"Mouse",value.MouseSensitivity),false);
            value.SetGamepadSensitivity(PlayerPrefs.GetFloat(PreferencePrefix+"Gamepad",value.GamepadSensitivity),false);
            value.SetSmoothing(PlayerPrefs.GetFloat(PreferencePrefix+"Smoothing",value.Smoothing),false);
            value.SetInvertY(PlayerPrefs.GetInt(PreferencePrefix+"InvertY",0)!=0,false);
            return value;
        }
        public void SetMouseSensitivity(float value,bool save=true)
        { MouseSensitivity=Mathf.Clamp(!float.IsNaN(value) && !float.IsInfinity(value)?value:0.12f,0.02f,0.5f); if(save) Save(); }
        public void SetGamepadSensitivity(float value,bool save=true)
        { GamepadSensitivity=Mathf.Clamp(!float.IsNaN(value) && !float.IsInfinity(value)?value:105f,30f,240f); if(save) Save(); }
        public void SetSmoothing(float value,bool save=true)
        { Smoothing=Mathf.Clamp(!float.IsNaN(value) && !float.IsInfinity(value)?value:0.06f,0f,0.25f); if(save) Save(); }
        public void SetInvertY(bool value,bool save=true) { InvertY=value; if(save) Save(); }
        public void ResetDefaults()
        { MouseSensitivity=0.12f; GamepadSensitivity=105f; Smoothing=0.06f; InvertY=false; Save(); }
        public void Save()
        {
            PlayerPrefs.SetFloat(PreferencePrefix+"Mouse",MouseSensitivity);
            PlayerPrefs.SetFloat(PreferencePrefix+"Gamepad",GamepadSensitivity);
            PlayerPrefs.SetFloat(PreferencePrefix+"Smoothing",Smoothing);
            PlayerPrefs.SetInt(PreferencePrefix+"InvertY",InvertY?1:0);
        }
    }
}
