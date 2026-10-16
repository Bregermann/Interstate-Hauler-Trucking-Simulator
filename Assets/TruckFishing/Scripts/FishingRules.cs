using System;
namespace LWS.TruckFishing {
public enum FishKind{Small,Large,Poison}
public sealed class FishingRules {
 public readonly int[] caught=new int[4],missed=new int[4];public readonly bool[] alive=new bool[4];public readonly string[] reason=new string[4];public int Count{get;private set;}public int Winner{get;private set;}=-1;public bool Complete{get;private set;}readonly long[] newest={-1,-1,-1,-1};readonly ulong[] resolved=new ulong[4];
 public void Reset(int count){Count=Math.Max(1,Math.Min(4,count));Winner=-1;Complete=false;for(int i=0;i<4;i++){caught[i]=missed[i]=0;alive[i]=i<Count;reason[i]="";newest[i]=-1;resolved[i]=0;}}
 public bool Resolve(int slot,long id,FishKind kind,bool catchFish){if(Complete||slot<0||slot>=Count||!alive[slot]||id<0)return false;long delta=id-newest[slot];if(delta>0){resolved[slot]=delta>=64?0:resolved[slot]<<(int)delta;newest[slot]=id;}long age=newest[slot]-id;if(age>=64)return false;ulong mask=1UL<<(int)age;if((resolved[slot]&mask)!=0)return false;resolved[slot]|=mask;if(kind==FishKind.Poison){if(catchFish){alive[slot]=false;reason[slot]="POISON CAUGHT";}}else{int pounds=kind==FishKind.Large?5:1;if(catchFish)caught[slot]+=pounds;else{missed[slot]+=pounds;if(missed[slot]>=20){alive[slot]=false;reason[slot]="20 LB MISSED";}}}return true;}
 // Called once AFTER the entire shared simulation-step event batch, never inside Resolve.
 public void FinishStep(){if(Complete)return;int living=0;Winner=-1;for(int i=0;i<Count;i++)if(alive[i]){living++;Winner=i;}if(Count==1){if(living==0)Complete=true;return;}if(living<=1)Complete=true;else Winner=-1;}
 public int Remaining{get{int n=0;for(int i=0;i<Count;i++)if(alive[i])n++;return n;}}
 public static bool SweptCatch(float previousZ,float currentZ,float previousX,float currentX,float oldNet,float newNet,float radius,out float fraction){fraction=0;if(previousZ<0||currentZ>0||previousZ==currentZ)return false;fraction=previousZ/(previousZ-currentZ);float fish=previousX+(currentX-previousX)*fraction;float net=oldNet+(newNet-oldNet)*fraction;return Math.Abs(fish-net)<=radius;}
}
public struct FishPlan {public long id;public FishKind kind;public double arrival;public float travel,x,curve,ramp;public double Spawn=>arrival-travel;}
public sealed class FishingDirector {
 readonly Random random;long sequence;double next=8,lastArrival;float lastX;FishKind previous;bool schoolNext;public float acceleration=.12f,minimumSpeed=2.8f,maximumSpeed=4.5f,density=1,maxNetSpeed=8.5f,openingSeconds=25,challengeSeconds=600,minTravel=4.5f,maxTravel=8.5f,poisonMaximum=.2f,burstSeconds=5,breakSeconds=3;
 public FishingDirector(int seed){random=new Random(seed);}
 float Random(float a,float b)=>(float)(a+(b-a)*random.NextDouble());
 public FishPlan Next(){float challenge=(float)Math.Max(0,Math.Min(1,next/challengeSeconds));float phase=(float)(next%26);bool opening=next<openingSeconds;bool burst=!opening&&phase<burstSeconds;bool lull=!opening&&phase>=17;bool school=schoolNext;schoolNext=false;float gap=opening?Random(2.4f,3):lull?Random(2.5f,3.5f):burst?Random(.85f,1.15f):Random(1.3f,2)*(1-challenge*.25f);if(!opening&&phase>26-breakSeconds){next+=26-phase;gap+=breakSeconds;}
 gap/=Math.Max(.7f,Math.Min(1.3f,density));float reach=Math.Min(school?.7f:4.8f,maxNetSpeed*Math.Max(.1f,(float)(next-lastArrival)-.3f)*.52f);float x=Math.Max(-5,Math.Min(5,lastX+Random(-reach,reach)));float poison=opening?0:Math.Min(poisonMaximum,.07f+challenge*.1f+(phase>10&&phase<14?.04f:0));FishKind kind=random.NextDouble()<poison?FishKind.Poison:random.NextDouble()<.24?FishKind.Large:FishKind.Small;
 if(school)kind=FishKind.Small;if(!opening&&!school&&phase>=6&&phase<10&&kind!=FishKind.Poison&&random.NextDouble()<.35){schoolNext=true;gap=.45f;}if(previous==FishKind.Poison&&kind!=FishKind.Poison&&Math.Abs(x-lastX)<2.4f)x=Math.Max(-5,Math.Min(5,lastX+(lastX>0?-2.6f:2.6f)));float speed=opening?2.5f:Random(minimumSpeed,maximumSpeed-.7f)+challenge*.7f+(burst?.35f:0);var result=new FishPlan{id=sequence++,kind=kind,arrival=next,travel=Math.Max(minTravel,Math.Min(maxTravel,20/speed)),x=x,curve=Random(-1.2f,1.2f),ramp=Math.Max(0,Math.Min(.2f,acceleration))};lastX=x;previous=kind;lastArrival=next;next+=gap;return result;}
}
}
