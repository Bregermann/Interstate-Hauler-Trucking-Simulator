using System;
using UnityEngine;
namespace LWS.TruckBilliards.Editor
{
 public static class BilliardsFeatureFixtures
 {
  static int checks;
  static void Check(bool value,string message){checks++;if(!value)throw new Exception("TB_FEATURE_FIXTURE_FAILED "+message);}
  public static void Run(){checks=0;
   int[][] outcomes={new[]{0},new[]{0,0},new[]{0,1,0},new[]{1,1,1},new[]{1,0,1,0,1}};
   for(int test=0;test<outcomes.Length;test++){
    var series=new BilliardsSeries();var format=test==0?BilliardsFormat.SingleRack:test<3?BilliardsFormat.BestOf3:BilliardsFormat.BestOf5;series.Start(format,1);
    for(int rack=0;rack<outcomes[test].Length;rack++){
     Check(series.Breaker==(1+rack)%2,"breaker alternation");Check(series.Award(outcomes[test][rack]),"authoritative rack award");
     int score=series.Wins[outcomes[test][rack]];Check(!series.Award(outcomes[test][rack])&&series.Wins[outcomes[test][rack]]==score,"exactly once scoring");
     if(rack<outcomes[test].Length-1)Check(series.Next(),"next rack");
    }
    Check(series.Finished&&series.Wins[series.Winner]==series.Required,"expected deciding rack");Check(!series.Next()&&!series.Award(0),"deciding rack stops series");
    int opener=series.OriginalBreaker;series.Start(format,1-opener);Check(series.Wins[0]+series.Wins[1]==0&&series.Breaker==1-opener,"rematch series reset");
   }
   var rules=new BilliardsRules();rules.Reset(0,true);Check(rules.BeginShot()&&!rules.BeginShot(),"practice one strike");rules.Contact(8);rules.Pocket(8,1);rules.Resolve();
   Check(rules.Phase==MatchPhase.Aiming&&rules.Winner==-1&&rules.PlayerGroup==0&&rules.Turn==0&&rules.Pocketed.Contains(8),"practice eight remains pocketed without competitive outcome");
   Check(rules.PracticePlacement(),"practice manual placement");rules.CompletePlacement();rules.BeginShot();rules.Pocket(0,2);rules.Resolve();Check(rules.Phase==MatchPhase.Placement&&rules.Turn==0,"practice scratch placement");rules.CompletePlacement();Check(!rules.Pocketed.Contains(0),"practice cue registry restore");
   rules.Reset();Check(!rules.PracticePlacement(),"competitive manual placement refused");
   Debug.Log("TB_FEATURE_FIXTURES_PASS "+checks+" assertions");
  }
 }
}
