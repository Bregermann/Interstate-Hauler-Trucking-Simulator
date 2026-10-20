using System;
namespace LWS.TruckFloorIsLava
{
    public enum LavaPlatformState { Untouched, Warning, Falling }
    [Serializable] public sealed class LavaRules
    {
        public int Count { get; private set; }
        public int Winner { get; private set; }
        public int Collapses { get; private set; }
        public float Clock { get; private set; }
        public bool Started { get; private set; }
        public bool Complete { get; private set; }
        public readonly bool[] alive=new bool[8];
        public readonly int[] scores=new int[8],knockoffs=new int[8];
        public readonly float[] survived=new float[8];
        public LavaPlatformState[] platforms { get; private set; }=Array.Empty<LavaPlatformState>();
        float[] duration,deadline;readonly int[] hitter=new int[8];readonly float[] hitAt=new float[8];
        public void Reset(int count,float[] warningSeconds)
        {
            if(count<1||count>8||warningSeconds==null||warningSeconds.Length<8||warningSeconds.Length>64)throw new ArgumentException("Provide1–8trucks and8–64finite platform warning durations.");
            foreach(float seconds in warningSeconds)if(seconds<1||float.IsNaN(seconds)||float.IsInfinity(seconds))throw new ArgumentException("Every platform warning must be finite and at least one second.");
            Count=count;Clock=0;Winner=-1;Collapses=0;Started=Complete=false;duration=(float[])warningSeconds.Clone();platforms=new LavaPlatformState[duration.Length];deadline=new float[duration.Length];
            for(int i=0;i<8;i++){alive[i]=i<count;scores[i]=knockoffs[i]=0;survived[i]=0;hitter[i]=-1;hitAt[i]=-100;}
        }
        public void Begin(){if(Count==0)throw new InvalidOperationException("Configure the finite arena first.");if(!Complete)Started=true;}
        public bool Touch(int index)
        {
            if(!Started||Complete||index<0||index>=platforms.Length||platforms[index]!=LavaPlatformState.Untouched)return false;
            platforms[index]=LavaPlatformState.Warning;deadline[index]=Clock+duration[index];return true;
        }
        public float Remaining(int index)=>platforms[index]==LavaPlatformState.Untouched?duration[index]:Math.Max(0,deadline[index]-Clock);
        public void RecordPush(int attacker,int victim,float incomingNormalSpeed)
        {
            if(!Started||Complete||attacker<0||victim<0||attacker>=Count||victim>=Count||attacker==victim||!alive[attacker]||!alive[victim]||incomingNormalSpeed<3||float.IsNaN(incomingNormalSpeed)||float.IsInfinity(incomingNormalSpeed))return;
            hitter[victim]=attacker;hitAt[victim]=Clock;
        }
        public void Step(float dt,int genuineLavaMask)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));if(!Started||Complete)return;float before=Clock;Clock+=dt;
            for(int i=0;i<platforms.Length;i++)if(platforms[i]==LavaPlatformState.Warning&&Clock>=deadline[i]){platforms[i]=LavaPlatformState.Falling;Collapses++;}
            for(int i=0;i<Count;i++)if(alive[i])
            {
                survived[i]=Clock;scores[i]+=(int)(Clock/15)-(int)(before/15);
                if((genuineLavaMask&(1<<i))!=0){alive[i]=false;int credit=hitter[i];if(credit>=0&&Clock-hitAt[i]<=4){scores[credit]++;knockoffs[credit]++;}}
            }
            int remaining=0,last=-1;for(int i=0;i<Count;i++)if(alive[i]){remaining++;last=i;}
            // Resolve the whole completed physics sample before declaring a
            // survivor, so callback order cannot invent a winner in a tie.
            if(remaining==0||Count>1&&remaining==1){Complete=true;if(remaining==1){Winner=last;scores[last]+=5;}}
        }
        public int Place(int slot){int place=1;for(int i=0;i<Count;i++)if(survived[i]>survived[slot]+.0001f||i==Winner&&slot!=Winner&&Math.Abs(survived[i]-survived[slot])<=.0001f)place++;return place;}
        public int Available(){int count=0;foreach(var state in platforms)if(state!=LavaPlatformState.Falling)count++;return count;}
    }
}
