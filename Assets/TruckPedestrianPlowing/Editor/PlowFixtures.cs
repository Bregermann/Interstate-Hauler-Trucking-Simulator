using System;
using UnityEngine;
namespace LWS.TruckPedestrianPlowing.Editor {
 public static class PlowFixtures {
  static int passed;static void Check(bool condition,string label){if(!condition)throw new Exception("PLOW RULE FAIL "+label);passed++;}
  public static void Run(){passed=0;var r=new PlowRound(new[]{PlowTargetKind.Standard,PlowTargetKind.BonusRunner,PlowTargetKind.PenaltyMarshal},2);r.Advance(1);Check(!r.TryClaim(0,0,0,9,1),"stationary overlap");Check(!r.TryClaim(0,0,3,0,1),"stationary truck");Check(!r.TryClaim(0,0,1.99f,9,1),"slow impact");Check(r.TryClaim(0,0,4,6,1)&&r.Score(0)==10,"standard");Check(!r.TryClaim(0,1,12,12,1)&&r.Owner(0)==0,"atomic owner");Check(r.TryClaim(2,0,4,6,1)&&r.Score(0)==-90,"negative penalty");Check(r.TryClaim(1,1,4,6,1)&&r.Score(1)==100&&r.ShouldEnd,"bonus and exhaustion");Check(r.StandardHits(0)==1&&r.Penalties(0)==1&&r.BonusHits(1)==1,"breakdown");r.Finish();Check(!r.TryClaim(0,0,8,8,1),"sealed");
   r=new PlowRound(new[]{PlowTargetKind.Standard,PlowTargetKind.PenaltyMarshal},3);r.Advance(1);Check(r.TryClaim(0,0,5,5,1)&&r.ShouldEnd&&r.Owner(1)==-1,"penalties do not block ending");Check(!r.TryClaim(1,1,5,5,1),"no penalty after final positive contact");
   r=new PlowRound(new[]{PlowTargetKind.Standard,PlowTargetKind.Standard,PlowTargetKind.PenaltyMarshal},3);r.Advance(180);Check(!r.TryClaim(0,0,5,5,180),"exact expiry");Check(!r.TryClaim(0,0,5,5,181),"late impact");Check(r.TryClaim(0,0,5,5,179.99f),"prior physics contact resolves before sealing");Check(r.TryClaim(1,1,5,5,179.9f)&&r.Place(0)==1&&r.Place(1)==1&&r.Place(2)==3,"shared placing");Check(!r.TryClaim(2,2,float.NaN,5,1)&&!r.TryClaim(2,2,5,float.PositiveInfinity,1),"finite inputs");Debug.Log("PLOW_RULE_FIXTURES_PASS "+passed);
  }
 }
}
