using UnityEngine;
namespace LWS.TruckSports {
// A designer-owned reference to installed horn audio, not a replacement audio framework.
[CreateAssetMenu(menuName="Truck Sports/Shared horn audio")]
public sealed class SportsHornAssets:ScriptableObject { public AudioClip defaultHorn; [Range(0,1)]public float gain=.5f; }
}
