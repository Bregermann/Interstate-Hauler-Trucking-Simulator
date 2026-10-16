using System;
namespace LWS.TruckPoker {
public readonly struct PokerDecision {public readonly PokerAction Action;public readonly int Total;public PokerDecision(PokerAction action,int total=0){Action=action;Total=total;}}
// No reference to PokerTable, a dealer, a deck source, or any other player's private cards.
public static class PokerPolicy {
 public static PokerDecision Choose(PokerView view,int difficulty,Random personalRandom){
  if(view.Seat!=view.Actor)throw new ArgumentException("Policy can act only for its own seat");int opponents=0;for(int i=0;i<view.Stacks.Length;i++)if(i!=view.Seat&&view.InHand[i]&&!view.Folded[i])opponents++;int samples=difficulty==0?12:difficulty==1?32:80;double equity=Estimate(view.OwnA,view.OwnB,view.Board,opponents,samples,personalRandom);double personality=.82+(view.Seat*7%11)*.035;double error=(personalRandom.NextDouble()-.5)*(difficulty==0?.36:difficulty==1?.16:.055);equity=Math.Max(0,Math.Min(1,equity+error));double odds=view.Call==0?0:(double)view.Call/(view.Pot+view.Call);bool bluff=personalRandom.NextDouble()<(difficulty==0?.1:.055)*personality;
  if(view.Call>0&&equity+.035<odds&&!bluff)return new PokerDecision(PokerAction.Fold);
  double threshold=opponents>2?.54:.61;if(view.CanRaise&&(equity>threshold||bluff)&&personalRandom.NextDouble()<(equity> .82?.9:.5)*personality){int desired=view.Bets[view.Seat]+view.Call+Math.Max(view.BigBlind,(int)((view.Pot+view.Call)*(equity>.78?.85:.5)));if(equity>.88&&view.Stacks[view.Seat]<view.Pot*2)desired=view.MaximumRaiseTo;int total=Math.Min(view.MaximumRaiseTo,Math.Max(view.MinimumRaiseTo,desired));if(total>view.Bets[view.Seat]+view.Call)return new PokerDecision(PokerAction.RaiseTo,total);}
  return new PokerDecision(PokerAction.CheckCall);
 }
 static double Estimate(int ownA,int ownB,int[] community,int opponents,int samples,Random random){if(opponents<1)return 1;bool[] known=new bool[52];known[ownA]=known[ownB]=true;foreach(int card in community)known[card]=true;var available=new int[52-2-community.Length];int n=0;for(int card=0;card<52;card++)if(!known[card])available[n++]=card;var pool=new int[n];var hero=new int[7];var rival=new int[7];hero[0]=ownA;hero[1]=ownB;for(int i=0;i<community.Length;i++)hero[2+i]=community[i];double wins=0;
  for(int sample=0;sample<samples;sample++){Array.Copy(available,pool,n);int cursor=0;for(int i=community.Length;i<5;i++)hero[2+i]=Draw(pool,ref cursor,random);Array.Copy(hero,2,rival,2,5);long best=PokerHand.Evaluate(hero);bool lose=false;int tied=1;for(int enemy=0;enemy<opponents;enemy++){rival[0]=Draw(pool,ref cursor,random);rival[1]=Draw(pool,ref cursor,random);long rank=PokerHand.Evaluate(rival);if(rank>best)lose=true;else if(rank==best)tied++;}if(!lose)wins+=1.0/tied;}return wins/samples;
 }
 static int Draw(int[] pool,ref int cursor,Random random){int i=random.Next(cursor,pool.Length);int result=pool[i];pool[i]=pool[cursor];pool[cursor++]=result;return result;}
}
}
