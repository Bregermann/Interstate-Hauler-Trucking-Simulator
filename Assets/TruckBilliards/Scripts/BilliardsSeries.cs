namespace LWS.TruckBilliards
{
 public enum BilliardsFormat { SingleRack, BestOf3, BestOf5 }
 public enum BilliardsDifficulty { Easy, Normal, Hard }
 // Rack winners come exclusively from BilliardsRules; this owns only series bookkeeping.
 public sealed class BilliardsSeries
 {
  public readonly int[] Wins=new int[2];
  public int Required{get;private set;}=1;
  public int RackNumber{get;private set;}=1;
  public int OriginalBreaker{get;private set;}
  public int Breaker=>(OriginalBreaker+RackNumber-1)%2;
  public bool RackFinished{get;private set;}
  public int Winner{get;private set;}=-1;
  public bool Finished=>Winner>=0;
  public void Start(BilliardsFormat format,int opener){Required=(int)format+1;OriginalBreaker=opener;RackNumber=1;Wins[0]=Wins[1]=0;RackFinished=false;Winner=-1;}
  public bool Award(int winner){if(RackFinished||Finished||winner<0||winner>1)return false;RackFinished=true;Wins[winner]++;if(Wins[winner]>=Required)Winner=winner;return true;}
  public bool Next(){if(!RackFinished||Finished)return false;RackNumber++;RackFinished=false;return true;}
 }
}
