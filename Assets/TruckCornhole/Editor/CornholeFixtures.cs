using System;
using UnityEngine;
using LWS.TruckBowling;
namespace LWS.TruckCornhole.Editor {
public static class CornholeFixtures {
 public static void Run(){int n=0;Action<bool,string> check=(value,name)=>{if(!value)throw new Exception("Cornhole rule fixture "+name);n++;};check(CornholeRules.Points(false,false,true,true)==1,"stable board");check(CornholeRules.Points(true,false,false,false)==3,"hole");check(CornholeRules.Points(false,true,true,true)==0,"board plus ground");check(CornholeRules.Points(false,false,false,true)==0,"hover/side face");check(CornholeRules.Points(false,false,true,false)==0,"moving board");check(CornholeRules.Points(false,false,true,true,true)==0,"resolution failure");check(CornholeRules.Points(false,false,true,true)+CornholeRules.Points(false,false,false,true)==1,"knock off removes point");check(CornholeRules.Points(true,false,false,true)==3,"push through becomes three");check(CornholeRules.Winners(new[]{9,12,12,6},4).Length==2,"tie");check(CornholeRules.Winners(new[]{9,12,11,6},4)[0]==1,"winner");var setup=new BowlingSetup{count=4,mode=BowlingMode.Simultaneous};check(setup.Validate(out _),"four default independent sources");setup.players[1].source=BowlingSource.Keyboard;setup.players[1].forward=setup.players[0].forward;check(!setup.Validate(out _),"overlapping keyboard rejected");for(int count=1;count<=4;count++){float area=0;for(int i=0;i<count;i++){var r=CornholeMatch.ViewRect(count,i);area+=r.width*r.height;}check(Mathf.Abs(area-1)<.001f,"viewport coverage "+count);}Debug.Log("CORNHOLE_RULE_FIXTURES_PASS "+n);}
}
}
