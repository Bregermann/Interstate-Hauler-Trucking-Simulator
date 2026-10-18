#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckSports {
public static class SportsParticipantChecks {
 public static int Run(){
  int checks=0;void Check(bool value,string message){if(!value)throw new InvalidOperationException("Participant fixture: "+message);checks++;}
  string production=PlayerPrefs.GetString(SportsParticipantRegistry.Key);
  using(SportsPrefs.IsolateValidation("IdentityFixtures")){
   foreach(var key in new[]{SportsParticipantRegistry.Key,SportsParticipantRegistry.Key+".Backup","TruckBowling.Setup.v1"})SportsPrefs.DeleteKey(key);SportsParticipantRegistry.ForgetCache();
   string a="{\"players\":[{\"name\":\"Old captain\",\"color\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1},\"source\":1}],\"unknownFutureField\":777}";
   string b="{\"setup\":{\"players\":[{\"name\":\"Other captain\",\"source\":0},{\"name\":\"Twin\",\"source\":0}]}}";
   var sources=new[]{new SportsLegacySetup{key="A",json=a},new SportsLegacySetup{key="B",json=b}};
   var migrated=SportsParticipantRegistry.Migrate(sources);var reversed=SportsParticipantRegistry.Migrate(sources.Reverse());
   Check(migrated.participants.All(p=>p.color.a>0),"missing legacy color cannot create invisible profile text");SportsPrefs.SetString("Validation.AliasCheck","same");Check(SportsPrefs.GetString("AliasCheck")=="same","one validation namespace for explicit and automatic prefixes");SportsPrefs.DeleteKey("AliasCheck");
   Check(migrated.participants.Find(p=>p.id==migrated.roster[0]).displayName=="Old captain","deterministic first custom name");
   Check(reversed.participants.Find(p=>p.id==reversed.roster[0]).displayName=="Old captain","input enumeration order does not alter precedence");
   Check(migrated.participants.Any(p=>p.displayName=="Other captain"),"conflicting name remains selectable");Check(migrated.legacy[0].json==a,"unknown legacy fields preserved verbatim");Check(migrated.roster.Count==12,"full roster independent of current mode capacity");
   SportsPrefs.SetString("TruckBowling.Setup.v1",a);SportsParticipantRegistry.ForgetCache();var first=SportsParticipantRegistry.Default(0);Check(first.displayName=="Old captain","real preference migration");
   var setup=new BowlingSetup{players=new BowlingPlayer[12],count=12};for(int i=0;i<12;i++)setup.players[i]=new BowlingPlayer{source=i<4?BowlingSource.Keyboard:BowlingSource.AI};
   var binding=new SportsParticipantBinding(setup.players);var ids=setup.players.Select(p=>p.participantId).ToArray();Check(ids.Distinct().Count()==12,"unique stable IDs for twelve participants");
   for(int count=1;count<=12;count++){int active=count;string summary=SportsParticipantRegistry.Totals(setup.players,active,i=>i+10);Check(summary==string.Join("; ",setup.players.Take(active).Select((p,i)=>p.name+"="+(i+10))),"result summary contains only active participants "+active);}
   var bowling=new BowlingSetup{count=4};for(int i=0;i<bowling.players.Length;i++){bowling.players[i].name=(i<4?"Active ":"Inactive ")+i;bowling.players[i].score=new BowlingScore();if(i<4)for(int roll=0;roll<20;roll++)bowling.players[i].score.Record(0);}
   Check(bowling.players.Take(4).All(p=>p.score.Finished),"active real Bowling score fixtures finish ten frames");bool pending=false;try{_ = bowling.players[4].score.FinalTotal;}catch(InvalidOperationException){pending=true;}Check(pending,"inactive Bowling score is deliberately unresolved");string completed=SportsParticipantRegistry.Totals(bowling.players,bowling.count,i=>bowling.players[i].score.FinalTotal);Check(!completed.Contains("Inactive")&&completed.Split(';').Length==4,"Bowling receipt never evaluates inactive pending bonuses");
   setup.players[0].name="Big Daddy";setup.players[0].color=Color.magenta;binding.Refresh(true);Check(SportsParticipantRegistry.Find(ids[0]).displayName=="Big Daddy","legacy name field updates global identity");
   for(int count=1;count<=16;count++){
    var roster=new BowlingPlayer[count];for(int i=0;i<count;i++)roster[i]=new BowlingPlayer{source=i%2==0?BowlingSource.Keyboard:BowlingSource.AI};var copy=new SportsParticipantBinding(roster);
    for(int i=0;i<count;i++){Check(roster[i].participantId==SportsParticipantRegistry.Default(i).id,"stable identity count "+count+" seat "+i);Check(roster[i].source==(i%2==0?BowlingSource.Keyboard:BowlingSource.AI),"ownership unchanged count "+count+" seat "+i);}
    Check(roster[0].name=="Big Daddy"&&roster[0].color==Color.magenta,"name/color follows across capacities "+count);
   }
   setup.players[1].name="Big Daddy";binding.Refresh(true);Check(setup.players[1].participantId!=ids[0],"duplicate display names retain separate identities");
   setup.players[0].source=BowlingSource.AI;binding.Refresh(true);Check(setup.players[0].participantId==ids[0],"source change never reassigns identity");
   SportsParticipantRegistry.ForgetCache();Check(SportsParticipantRegistry.Default(0).id==ids[0]&&SportsParticipantRegistry.Default(0).displayName=="Big Daddy","cache/domain reload retains ID and rename");
   string token=Guid.NewGuid().ToString("N");Check(SportsParticipantRegistry.Record(token,"Fixture","Observed tie 2:2",setup.players,4),"one completed result contract");Check(!SportsParticipantRegistry.Record(token,"Fixture","duplicate",setup.players,4),"duplicate completion ignored");var receipt=SportsParticipantRegistry.Current.results.Last();Check(receipt.participantIds.SequenceEqual(ids.Take(4))&&receipt.summary=="Observed tie 2:2","result preserves actual participants and supplied outcome");
   string replacement=SportsParticipantRegistry.Create("Imported alternative").id;Check(SportsParticipantRegistry.UseInSeat(0,replacement),"archived/new profile can become active");var next=new BowlingPlayer[4];for(int i=0;i<4;i++)next[i]=new BowlingPlayer();new SportsParticipantBinding(next);Check(next[0].participantId==replacement,"global selection overrides stale per-mode roster");Check(SportsParticipantRegistry.UseInSeat(1,replacement)&&SportsParticipantRegistry.Default(0).id!=SportsParticipantRegistry.Default(1).id,"occupied profile swaps, never duplicates");
   SportsParticipantRegistry.Rename(ids[0],"Backup version",Color.blue,false);SportsParticipantRegistry.Rename(ids[0],"Newer version",Color.green,false);SportsPrefs.SetString(SportsParticipantRegistry.Key,"broken json");SportsParticipantRegistry.ForgetCache();Check(SportsParticipantRegistry.Find(ids[0]).displayName=="Backup version","corrupt primary recovers validated prior copy");
   string newer=JsonUtility.ToJson(new SportsParticipantData{version=999});SportsPrefs.SetString(SportsParticipantRegistry.Key,newer);SportsParticipantRegistry.ForgetCache();var temporary=SportsParticipantRegistry.Default(0);SportsParticipantRegistry.Rename(temporary.id,"Temporary",Color.yellow,false);Check(SportsPrefs.GetString(SportsParticipantRegistry.Key)==newer,"older binary never overwrites newer schema");
   foreach(var key in new[]{SportsParticipantRegistry.Key,SportsParticipantRegistry.Key+".Backup","TruckBowling.Setup.v1"})SportsPrefs.DeleteKey(key);SportsPrefs.Save();
  }
  SportsParticipantRegistry.ForgetCache();Check(PlayerPrefs.GetString(SportsParticipantRegistry.Key)==production,"production preferences untouched by fixtures");Debug.Log("TS_PARTICIPANT_FIXTURES_PASS checks="+checks);return checks;
 }
}
}
#endif
