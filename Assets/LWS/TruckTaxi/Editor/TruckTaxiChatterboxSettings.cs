using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    [FilePath("UserSettings/TruckTaxiChatterbox.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class TruckTaxiChatterboxSettings : ScriptableSingleton<TruckTaxiChatterboxSettings>
    {
        public string workHere = "F:/Codexprojects/MyVoiceForRecording/WorkHere";
        public bool offline = true;
        [HideInInspector] public string lastJob;
        public void Persist() => Save(true);
    }
}
