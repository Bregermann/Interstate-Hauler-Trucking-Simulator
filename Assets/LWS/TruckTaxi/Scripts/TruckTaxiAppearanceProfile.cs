using System;
using UnityEngine;

namespace LWS.TruckTaxi
{
    public enum TruckTaxiCharacterType { ModularHuman, GeneratedHumanoid, GeneratedCreature, Robot, SmallCompanion, DashboardPassenger, OversizedPassenger, Custom }
    public enum TruckTaxiHumanBuild { ThinMale, AverageMale, HeavyMale, HugeMale, ThinFemale, AverageFemale, HeavyFemale, OlderMale, OlderFemale }
    public enum TruckTaxiRigType { Humanoid, Generic, Static }
    public enum TruckTaxiModelBackend { ManualDropFolder, Placeholder, ConfiguredExternal }
    [CreateAssetMenu(menuName = "Truck Taxi/Passengers/Appearance")]
    public sealed class TruckTaxiAppearanceProfile : ScriptableObject
    {
        public TruckTaxiCharacterType characterType;
        public TruckTaxiHumanBuild humanBuild;
        public string raceEthnicity, skinTone, bodyDescription, faceDescription;
        [Min(.1f)] public float heightMeters = 1.75f;
        public Vector3 bodyScale = Vector3.one;
        [Range(.5f,2)] public float headScale = 1, shoulderWidth = 1, bodyWidth = 1;
        public Color fallbackColor = new Color(.1f,.65f,.7f);
        public GameObject head, hair, facialHair, eyes, glasses, hat, upperClothing, lowerClothing, shoes;
        public GameObject[] accessories = Array.Empty<GameObject>();
        public Material[] skinMaterials = Array.Empty<Material>();
        [TextArea] public string distinctiveFeatures;
        public TruckTaxiRigType rigType = TruckTaxiRigType.Humanoid;
        public bool buildRagdoll = true;
        [Range(0,1)] public float lodReduction = .5f;
        [Min(100)] public int targetPolygons = 12000;
        [Min(64)] public int textureResolution = 1024;
#if UNITY_EDITOR
        public TruckTaxiModelBackend generationBackend;
        [TextArea(3,10)] public string generationPrompt, avoidPrompt;
        public Texture2D[] referenceImages = Array.Empty<Texture2D>();
        public string sourceModelPath, processedModelPath, generationJobId, generationStatus, generationHash;
#endif
    }
}
