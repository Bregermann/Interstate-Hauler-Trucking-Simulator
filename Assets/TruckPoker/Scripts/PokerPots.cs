using System;
using System.Collections.Generic;
namespace LWS.TruckPoker {
public sealed class PokerPotAward {
 public int Amount; public int[] Winners; public int[] Chips;
}
public static class PokerPots {
 // Pure settlement. Folded money funds pots but can never win one. Uncalled excess is returned first.
 public static int[] Settle(int[] contributions,bool[] folded,long[] ranks,int button,out int[] returned,out PokerPotAward[] awards){
  int n=contributions.Length;if(n<2||n>8||folded.Length!=n||ranks.Length!=n||button<0||button>=n)throw new ArgumentException("Invalid table");
  int[] paid=new int[n],work=(int[])contributions.Clone();returned=new int[n];int largest=0,second=0,owner=-1;for(int i=0;i<n;i++){if(work[i]<0)throw new ArgumentException("Negative contribution");if(work[i]>largest){second=largest;largest=work[i];owner=i;}else second=Math.Max(second,work[i]);}
  if(owner>=0&&largest>second){returned[owner]=largest-second;work[owner]=second;}
  var levels=new SortedSet<int>();foreach(int value in work)if(value>0)levels.Add(value);var result=new List<PokerPotAward>();int previous=0;
  foreach(int level in levels){int contributors=0;long best=-1;var winners=new List<int>();for(int i=0;i<n;i++)if(work[i]>=level){contributors++;if(!folded[i]){if(ranks[i]>best){best=ranks[i];winners.Clear();}if(ranks[i]==best)winners.Add(i);}}
   int pot=checked((level-previous)*contributors);previous=level;if(winners.Count==0)throw new InvalidOperationException("Pot has no eligible live hand");int each=pot/winners.Count,remainder=pot%winners.Count;var chips=new int[n];foreach(int i in winners){paid[i]+=each;chips[i]=each;}
   for(int step=1;step<=n&&remainder>0;step++){int i=(button+step)%n;if(winners.Contains(i)){paid[i]++;chips[i]++;remainder--;}}
   result.Add(new PokerPotAward{Amount=pot,Winners=winners.ToArray(),Chips=chips});
  }
  for(int i=0;i<n;i++)paid[i]+=returned[i];awards=result.ToArray();int before=0,after=0;foreach(int v in contributions)before=checked(before+v);foreach(int v in paid)after=checked(after+v);if(before!=after)throw new InvalidOperationException("Chip conservation failed");return paid;
 }
}
}
