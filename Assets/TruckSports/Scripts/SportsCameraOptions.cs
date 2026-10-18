using System;
using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckSports {
// Presentation preference only. Never changes participants, input sources or simulation.
public static class SportsCameraOptions {
 public const string PreferenceKey="TruckSports.Cameras.ShowAI";
 public static bool ShowAI=>SportsPrefs.GetInt(PreferenceKey,0)!=0;
 public static void Set(bool enabled){SportsPrefs.SetInt(PreferenceKey,enabled?1:0);SportsPrefs.Save();}
 // Humans first, then AI in roster order, capped at the supported view capacity.
 // Paired participants share a single existing team camera.
 public static int Select(BowlingSetup setup,int[] owners,int[] groups=null){
  if(setup==null||setup.players==null||owners==null)throw new ArgumentNullException();
  Array.Fill(owners,-1);int count=0;
  for(int pass=0;pass<(ShowAI?2:1);pass++)for(int slot=0;slot<setup.count&&count<owners.Length;slot++){
   bool ai=setup.players[slot].source==BowlingSource.AI;if(ai!=(pass==1))continue;
   int owner=groups==null?slot:groups[slot];bool duplicate=false;
   for(int view=0;view<count;view++)if(owners[view]==owner){duplicate=true;break;}
   if(!duplicate)owners[count++]=owner;
  }
  return count;
 }
 public static int SelectOrSpectate(BowlingSetup setup,int[] owners){int count=Select(setup,owners);if(count==0){owners[0]=0;return 1;}return count;}
}
}
