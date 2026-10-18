using System;
using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckSports.Editor {
public static class SportsCameraFixtures {
 public static void Run(){bool had=SportsPrefs.HasKey(SportsCameraOptions.PreferenceKey);int original=SportsPrefs.GetInt(SportsCameraOptions.PreferenceKey);int checks=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("AI camera fixture: "+label);checks++;};
 try{SportsPrefs.DeleteKey(SportsCameraOptions.PreferenceKey);check(!SportsCameraOptions.ShowAI,"new preference defaults OFF");
 for(int enabled=0;enabled<2;enabled++){SportsCameraOptions.Set(enabled!=0);check(SportsCameraOptions.ShowAI==(enabled!=0),"preference round trip");
 for(int total=1;total<=12;total++)for(int humans=0;humans<=Math.Min(4,total);humans++){
 var setup=new BowlingSetup{count=total,players=new BowlingPlayer[12]};for(int i=0;i<12;i++)setup.players[i]=new BowlingPlayer{source=i>=total-humans&&i<total?BowlingSource.Keyboard:BowlingSource.AI};var owners=new int[4];int count=SportsCameraOptions.Select(setup,owners);check(count==(enabled!=0?Math.Min(4,total):humans),"view count");
 for(int v=0;v<count;v++){check(owners[v]>=0&&owners[v]<total,"valid owner");if(v<humans)check(owners[v]==total-humans+v,"noncontiguous humans have priority");else check(enabled!=0&&setup.players[owners[v]].source==BowlingSource.AI,"remaining views only explicit AI");for(int k=0;k<v;k++)check(owners[k]!=owners[v],"no duplicate owners");}
 for(int i=0;i<total;i++)check((setup.players[i].source==BowlingSource.AI)==(i<total-humans),"source unchanged");int fallback=SportsCameraOptions.SelectOrSpectate(setup,owners);check(fallback>0&&fallback<=4,"all AI fallback never blank");
 }
 var paired=new BowlingSetup{count=8};for(int i=0;i<8;i++)paired.players[i].source=i==7?BowlingSource.Keyboard:BowlingSource.AI;var map=new[]{0,1,2,3,0,1,2,3};var selected=new int[4];int n=SportsCameraOptions.Select(paired,selected,map);check(selected[0]==3,"human passenger's team first");check(n==(enabled!=0?4:1),"paired view capacity");for(int i=0;i<n;i++)for(int j=0;j<i;j++)check(selected[i]!=selected[j],"paired teams not duplicated");
 }
 }finally{if(had)SportsPrefs.SetInt(SportsCameraOptions.PreferenceKey,original);else SportsPrefs.DeleteKey(SportsCameraOptions.PreferenceKey);SportsPrefs.Save();}
 Debug.Log("TS_CAMERA_FIXTURES_PASS checks="+checks);
 }
}
}
