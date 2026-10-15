using System;
namespace LWS.TruckCornhole {
// The round is authoritative; a provisional board landing is never banked per attempt.
public static class CornholeRules {
 public const int Rounds=5,Deliveries=4;
 public static int Points(bool holed,bool ground,bool supported,bool stable,bool excluded=false)=>holed?3:!excluded&&!ground&&supported&&stable?1:0;
 public static int[] Winners(int[] totals,int count){int best=int.MinValue,n=0;for(int i=0;i<count;i++){if(totals[i]>best){best=totals[i];n=1;}else if(totals[i]==best)n++;}var result=new int[n];n=0;for(int i=0;i<count;i++)if(totals[i]==best)result[n++]=i;return result;}
}
public enum CornholePhase {Approach,Flight,Settling,AttemptResult,Waiting}
public enum CornholeSurfaceKind {Approach,Trampoline,Board,Ground,Barrier}
}
