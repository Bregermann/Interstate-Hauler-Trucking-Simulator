using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsTruckValidationSceneMarker : MonoBehaviour
    {
        [SerializeField] private LwsTruckDefinition selectedTruckDefinition;
        [SerializeField] private GameObject selectedTrailerPrefab;
        [SerializeField] private string nwhVersion = "13.6";
        [SerializeField] private string validationNotes =
            "Prompt 004 controlled NWH semi validation scene. Uses NWH physics/input and project-owned LWS adapters.";

        public LwsTruckDefinition SelectedTruckDefinition => selectedTruckDefinition;
        public GameObject SelectedTrailerPrefab => selectedTrailerPrefab;
        public string NwhVersion => nwhVersion;
        public string ValidationNotes => validationNotes;
    }
}
