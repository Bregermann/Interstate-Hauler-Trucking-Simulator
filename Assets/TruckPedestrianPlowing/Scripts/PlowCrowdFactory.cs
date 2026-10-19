using System;
using UnityEngine;
using LWS.TruckTaxi;
namespace LWS.TruckPedestrianPlowing {
 public static class PlowCrowdFactory {
  public static PlowCharacter Choose(PlowCharacter[] roster,System.Random random){
   float total=0;foreach(var entry in roster)if(entry!=null&&entry.prefab!=null)total+=Mathf.Max(0,entry.weight);
   if(total<=0)throw new InvalidOperationException("Crowd needs an assigned pedestrian prefab with a positive spawn weight.");
   float pick=(float)random.NextDouble()*total;PlowCharacter last=null;
   foreach(var entry in roster){if(entry==null||entry.prefab==null||entry.weight<=0)continue;last=entry;if((pick-=entry.weight)<=0)return entry;}
   return last;
  }
  public static PlowPedestrian Create(PlowCharacter entry,Transform parent,PlowSettings settings){
   var pedestrian=UnityEngine.Object.Instantiate(entry.prefab,parent);
   pedestrian.characterName=entry.displayName;pedestrian.panicLine=entry.panicReaction;
   pedestrian.reactions=entry.reactions!=null?Array.FindAll(entry.reactions.lines,line=>line!=null&&!string.IsNullOrWhiteSpace(line.Subtitle)&&(line.category==TruckTaxiDialogueCategory.NearMissReaction||line.category==TruckTaxiDialogueCategory.EjectionReaction||line.category==TruckTaxiDialogueCategory.HardBrakeReaction||line.category==TruckTaxiDialogueCategory.AirTimeReaction)&&line.rideStateRequirements.Length==0&&line.requestRequirements.Length==0):Array.Empty<TruckTaxiDialogueLine>();
   // The instantiated prefab owns its serialized Taxi impact settings.
   // Never mutate the shared Taxi profile or its pedestrian source prefab.
   pedestrian.ragdoll.settings.ragdollLifetime=settings.ragdollSeconds;
   return pedestrian;
  }
 }
}
