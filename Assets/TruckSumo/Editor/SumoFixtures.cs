using System;using UnityEngine;
namespace LWS.TruckSumo.Editor {
public static class SumoFixtures {
 public static void Run(){int checks=0;Action<bool,string> check=(value,name)=>{if(!value)throw new Exception("Sumo fixture: "+name);checks++;};var rules=new SumoRules();rules.Reset(4);rules.Crossed(0);check(!rules.FinishStep()&&rules.alive[1],"one eliminated body does not end four-truck heat");rules.Crossed(1);rules.Crossed(2);rules.Crossed(3);check(rules.FinishStep()&&rules.HeatWinner==-1&&Array.TrueForAll(rules.wins,n=>n==0),"entire final same-step batch gives draw, no accidental survival award");check(!rules.Crossed(0)&&!rules.FinishStep(),"duplicate callbacks cannot refinalize heat");rules.Reset(2);for(int i=0;i<5;i++){rules.Crossed(1);check(rules.FinishStep()&&rules.HeatWinner==0&&rules.wins[0]==i+1&&rules.points[0]==3*(i+1),"one heat win and three points per sole-survivor heat");if(i<4)rules.NextHeat();}check(rules.Complete&&rules.Winner==0&&rules.Heat==5,"first to five heat wins still finishes match");rules.Reset(3);rules.TimeoutDraw();check(rules.Resolved&&rules.HeatWinner==-1&&Array.TrueForAll(rules.wins,n=>n==0),"bounded unresolved heat has no survival award");check(!SumoRules.Outside(12,0,12)&&SumoRules.Outside(12.001f,0,12),"body-center boundary has inclusive visible edge, no wheel footprint test");check(!SumoRules.Outside(0,0,5),"airborne body inside legal XZ is not ring-out");
  rules.Reset(4);rules.Crossed(1,0);rules.Crossed(2,0);rules.Crossed(3,0);check(rules.FinishStep()&&rules.points[0]==6&&rules.ringouts[0]==3&&rules.wins[0]==1,"three legitimate ring-outs plus survival yields six points");check(!rules.Crossed(3,0)&&!rules.FinishStep()&&rules.points[0]==6,"repeated crossing/finalization never awards twice");rules.NextHeat();check(rules.points[0]==6&&rules.ringouts[0]==3&&rules.wins[0]==1,"next heat retains cumulative awards");
  rules.Reset(2);rules.Crossed(0,1);rules.Crossed(1,0);check(rules.FinishStep()&&rules.HeatWinner<0&&rules.points[0]==1&&rules.points[1]==1&&rules.wins[0]==0&&rules.wins[1]==0,"mutual same-step falls retain each causal point without a survival winner");
  rules.Reset(2);rules.Crossed(1,0);rules.Crossed(0,1);rules.FinishStep();check(rules.points[0]==1&&rules.points[1]==1,"reversed crossing callback order gives identical mutual awards");
  rules.Reset(4);rules.Crossed(0,0);rules.Crossed(1,9);check(Array.TrueForAll(rules.points,n=>n==0),"self and invalid attacker cannot earn points");rules.Crossed(2,3);rules.TimeoutDraw();check(rules.points[3]==1&&rules.wins[3]==0,"timeout preserves earned ring-out but adds no survival bonus");rules.Reset(3);check(Array.TrueForAll(rules.points,n=>n==0)&&Array.TrueForAll(rules.ringouts,n=>n==0),"rematch clears all point and attribution totals");
  Attribution(check);Debug.Log("SUMO_RULE_FIXTURES_PASS "+checks);
 }
 static void Attribution(Action<bool,string> check){
  var history=new SumoAttribution();var origin=Vector3.forward*8;var outside=Vector3.forward*12.2f;var movement=Vector3.forward*3;
  Action fresh=()=>history.Reset(4,1);Func<int,float,float,bool> hit=(attacker,time,strength)=>history.Record(1,attacker,time,origin,Vector3.forward,strength,5,5,false);
  fresh();check(hit(0,1,2)&&history.TakeCredit(1,2,outside,movement)==0,"recent actual outward push can credit a delayed fall");check(history.TakeCredit(1,2,outside,movement)<0,"taking credit consumes the contact record once");
  fresh();hit(0,1,2);check(history.TakeCredit(1,5.01f,outside,movement)<0,"ancient contact expires at the bounded window");
  fresh();check(!history.Record(1,0,1,origin,Vector3.forward,.1f,5,.5f,false)&&history.TakeCredit(1,2,outside,movement)<0,"incidental gentle contact cannot claim a later fall");
  fresh();check(!history.Record(1,0,1,origin,Vector3.forward,3,0,5,false),"a stationary blocking opponent is not credited as an aggressor");
  fresh();hit(0,1,2);history.Observe(1,1.7f,.7f,origin,Vector3.zero);check(history.TakeCredit(1,2,outside,movement)<0,"settled recovery clears contact before a later self-driven fall");
  fresh();hit(0,1,2);history.Observe(1,1.5f,.02f,origin-Vector3.forward,Vector3.back*2);check(history.TakeCredit(1,2,outside,movement)<0,"inward recovery clears a stale outward push");
  fresh();hit(0,1,2);check(history.TakeCredit(1,2,outside,Vector3.zero)<0,"stationary shrink-only elimination gives no collision credit");
  fresh();hit(0,1,2);check(history.TakeCredit(1,2,Vector3.right*12.2f,Vector3.right*3)<0,"an unrelated sideways self-inflicted departure is uncredited");
  fresh();hit(0,1,2);hit(2,1.5f,5);check(history.TakeCredit(1,2,outside,movement)==2,"stronger recent causal opponent wins attribution");
  fresh();hit(0,1,2);hit(2,1,2);int first=history.TakeCredit(1,2,outside,movement);fresh();hit(2,1,2);hit(0,1,2);check(history.TakeCredit(1,2,outside,movement)==first,"equal simultaneous contact priority is independent of callback order");history.Reset(4,3);hit(0,1,2);hit(2,1,2);check(history.TakeCredit(1,2,outside,movement)!=first,"exact shared-impact tie priority rotates between heats");
  fresh();check(history.Record(1,0,1,origin,Vector3.forward,.03f,1,.1f,true),"meaningful driven sustained pressure can start a push record");history.Record(1,0,1.1f,origin+Vector3.forward*.1f,Vector3.forward,.03f,1,.1f,true);check(history.TakeCredit(1,2,outside,movement)==0,"sustained push preserves its original displacement evidence");
  fresh();history.Record(1,0,1,Vector3.forward*11.99f,Vector3.forward,2,5,5,false);check(history.TakeCredit(1,1.02f,Vector3.forward*12.01f,movement)==0,"small real motion across the rim can score an immediate pre-step impact");
  fresh();hit(0,1,2);history.Reset(4,2);check(history.TakeCredit(1,2,outside,movement)<0,"heat reset clears all prior contact histories");
 }
}
}
