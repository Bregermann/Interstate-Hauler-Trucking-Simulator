using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsInterstateCorridorSceneMarker : MonoBehaviour
    {
        [SerializeField] private string easyRoadsVersion = "3.2.4f5";
        [SerializeField] private string corridorId = "IH_TEST_I000";
        [SerializeField] private float targetLengthMeters = 3350f;
        [SerializeField] private string validationNotes =
            "Prompt 009 EasyRoads interstate corridor. Uses LWS truck spawn stack and runtime EasyRoads public API generation.";

        public string EasyRoadsVersion => easyRoadsVersion;
        public string CorridorId => corridorId;
        public float TargetLengthMeters => targetLengthMeters;
        public string ValidationNotes => validationNotes;
    }
}
