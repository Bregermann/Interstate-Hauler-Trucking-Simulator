using System;
using UnityEngine;
namespace LWS.TruckPedestrianPlowing {
 [CreateAssetMenu(menuName="Truck Sports/Pedestrian Plowing/Crowd Settings")]
 public sealed class PlowSettings:ScriptableObject {
  [Header("Round and finite crowd")][Min(15)]public float roundSeconds=180;
  [Range(16,384)]public int crowdSize=240;
  [Range(0,.4f)]public float bonusFraction=.1f;
  [Range(0,.4f)]public float penaltyFraction=.16f;
  public int randomSeed=17419;public bool varySeedEachMatch=true;
  [Header("Characters — independent of scoring role")]
  public PlowCharacter[] characters=Array.Empty<PlowCharacter>();
  [Header("Movement")][Min(.2f)]public float walkSpeed=1.4f;
  [Min(.5f)]public float panicSpeed=3;[Min(.5f)]public float bonusSpeed=4.1f;
  [Range(3,20)]public float threatRadius=12;
  [Header("Performance")][Range(8,64)]public int maximumActiveRagdolls=32;
  [Range(4,15)]public float crowdThinkRate=6;
  [Range(15,120)]public float animationDetailDistance=55;
  [Header("Impact presentation")][Range(1,60)]public float ragdollSeconds=24;
  public AudioClip[] reactionVoices=Array.Empty<AudioClip>();
  [TextArea]public string customizationNotes="Edit crowd count and role fractions here. Characters are weighted prefab entries; their appearances never determine points. In the scene, select Crowd Spawn Zones; move/resize zones and adjust weights. Navigation must be rebaked after changing solid arena geometry.";
 }
 [Serializable]public sealed class PlowCharacter {
  public string displayName;public PlowPedestrian prefab;public LWS.TruckTaxi.TruckTaxiDialogueSet reactions;[Min(0)]public float weight=1;
  [TextArea]public string calmReaction="What a parade!",panicReaction="Truck! Heads up!";
 }
}
