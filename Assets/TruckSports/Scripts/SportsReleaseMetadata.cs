using System;
using UnityEngine;

namespace LWS.TruckSports
{
    // The Sports build reads this owned resource. Host project identity/settings
    // remain scoped by the existing SportsBuilder restoration boundary.
    [Serializable]public sealed class SportsReleaseMetadata
    {
        public const string ResourcePath="SportsReleaseMetadata";
        public string version,releaseName,stage;
        public void Validate()
        {
            if(string.IsNullOrEmpty(version)||version.Split('.').Length!=3||!Version.TryParse(version,out var parsed)||parsed.ToString(3)!=version)throw new ArgumentException("Sports release version must be a canonical numeric major.minor.patch value.");
            if(string.IsNullOrWhiteSpace(releaseName)||releaseName.Trim()!=releaseName||ContainsControl(releaseName))throw new ArgumentException("Sports release name must be nonempty single-line text.");
            if(string.IsNullOrWhiteSpace(stage)||stage.Trim()!=stage||ContainsControl(stage))throw new ArgumentException("Sports release stage must be nonempty single-line text.");
        }
        static bool ContainsControl(string value){foreach(char character in value)if(char.IsControl(character))return true;return false;}
        public static SportsReleaseMetadata Load()
        {
            var resource=Resources.Load<TextAsset>(ResourcePath);if(resource==null)throw new InvalidOperationException("The owned Sports release metadata resource is missing.");var metadata=JsonUtility.FromJson<SportsReleaseMetadata>(resource.text);if(metadata==null)throw new InvalidOperationException("Sports release metadata is empty.");metadata.Validate();return metadata;
        }
    }
}
