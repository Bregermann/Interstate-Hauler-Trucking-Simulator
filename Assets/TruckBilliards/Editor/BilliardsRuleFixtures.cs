using System;
using UnityEngine;
namespace LWS.TruckBilliards.Editor {
public static class BilliardsRuleFixtures {
 static int checks;
 static void Check(bool b,string name){checks++;if(!b)throw new Exception("TB_FIXTURE_FAILED "+name);}
 static BilliardsRules New(){var r=new BilliardsRules();r.Reset();return r;}
 static void Shot(BilliardsRules r,int first,params int[] pockets){Check(r.BeginShot(),"begin");r.Contact(first);foreach(int n in pockets)r.Pocket(n,1);r.Resolve();}
 public static void Run(){
  checks=0;
  var r=New();Shot(r,1,1);Check(r.PlayerGroup==1&&r.Turn==0,"legal assignment retains");Shot(r,2,9);Check(r.Turn==1&&r.Pocketed.Contains(9),"opponent pot stays");
  r=New();r.BeginShot();r.Contact(9);r.Pocket(9,3);r.Pocket(1,3);r.Resolve();Check(r.Phase==MatchPhase.GroupSelection&&r.PlayerGroup==0&&r.Turn==0,"mixed pots wait for shooter choice");Check(!r.ChooseGroup(1,1),"opponent cannot choose");Check(r.ChooseGroup(0,2)&&r.PlayerGroup==2&&r.Turn==0&&r.Pocketed.Contains(1)&&r.Pocketed.Contains(9),"choice retains turn and both pots");
  r=New();r.BeginShot();r.Contact(9);r.Pocket(9,2);r.Pocket(1,3);r.Resolve();Check(r.Phase==MatchPhase.GroupSelection&&r.ChooseGroup(0,1)&&r.PlayerGroup==1,"mixed pots choice independent of callback ordering");
  r=New();r.BeginShot();r.Contact(1);r.Pocket(1,1);r.Pocket(9,2);r.Pocket(0,3);r.Resolve();Check(r.PlayerGroup==0&&r.Phase==MatchPhase.Placement&&r.Turn==1,"mixed pots scratch never assigns or chooses");
  r=New();r.BeginShot();r.Contact(8);r.Pocket(1,1);r.Pocket(9,2);r.Resolve();Check(r.PlayerGroup==0&&r.Phase==MatchPhase.Aiming&&r.Turn==1,"illegal mixed pots never choose");
  r=New();r.BeginShot();r.Contact(1);r.Pocket(1,1);r.Pocket(9,2);r.Pocket(8,3);r.Resolve();Check(r.Phase==MatchPhase.GroupSelection&&r.RespotEight&&!r.Pocketed.Contains(8)&&r.RemainingGroup(1)==6&&r.RemainingGroup(2)==6,"opening mixed groups preserve break-eight respot and counts");Check(!r.ChooseGroup(0,0)&&r.ChooseGroup(0,1),"choice rejects invalid group");
  r=New();Shot(r,8,1);Check(r.PlayerGroup==0&&r.Turn==1&&r.Phase==MatchPhase.Aiming,"foul no assign and no placement");
  r=New();Shot(r,-1);Check(r.Turn==1&&r.Phase==MatchPhase.Aiming,"miss no placement");
  r=New();Shot(r,1,0,1);Check(r.PlayerGroup==0&&r.Turn==1&&r.Phase==MatchPhase.Placement,"human scratch");r.CompletePlacement();Check(!r.Pocketed.Contains(0)&&r.Phase==MatchPhase.Aiming,"cue registry restoration");Shot(r,2,0);Check(r.Turn==0&&r.Phase==MatchPhase.Placement,"AI scratch incoming human");
  r=New();r.BeginShot();r.Contact(1);Check(r.Pocket(1,1),"first pocket recorded");Check(!r.Pocket(1,1),"duplicate pocket refused");r.Resolve();int turn=r.Turn;r.Resolve();Check(r.Turn==turn,"resolution idempotent");
  r=New();Shot(r,1,8);Check(r.Phase==MatchPhase.Aiming&&r.RespotEight&&!r.Pocketed.Contains(8),"break eight respot");
  r=New();Shot(r,1,8,0);Check(r.RespotEight&&r.Phase==MatchPhase.Placement&&r.Turn==1,"break eight scratch");
  r=New();Shot(r,1,1);Shot(r,2,8);Check(r.Phase==MatchPhase.Ended&&r.Winner==1,"early eight loses");
  r=New();Shot(r,1,1,2,3,4,5,6);Shot(r,7,7,8);Check(r.Winner==1,"last group plus eight loses");
  r=New();Shot(r,1,1,2,3,4,5,6,7);Shot(r,8,8);Check(r.Winner==0&&r.Phase==MatchPhase.Ended,"eligible eight win");
  r=New();Shot(r,1,1,2,3,4,5,6,7);Shot(r,8,8,0);Check(r.Winner==1&&r.Phase==MatchPhase.Ended,"eight scratch loses no placement");
  r=New();Shot(r,1,1,2,3,4,5,6,7);Shot(r,9,8);Check(r.Winner==1,"illegal eight loses");
  r=New();Shot(r,1,1);Shot(r,9);Check(r.Turn==1&&r.Phase==MatchPhase.Aiming,"assigned wrong contact no placement");
  r.Reset();Check(r.Pocketed.Count==0&&r.PlayerGroup==0&&r.Turn==0&&r.ShotId==0&&r.Opening&&r.Winner==-1,"full reset");
  r=New();r.Reset(1);Check(r.Turn==1&&r.Opening,"selected Player 2 break");Shot(r,1,1);Check(r.Turn==1&&r.Group(1)==1&&r.PlayerGroup==2,"Player 2 legal assignment retains");
  Debug.Log("TB_RULE_FIXTURES_PASS "+checks+" assertions");
 }
}}

