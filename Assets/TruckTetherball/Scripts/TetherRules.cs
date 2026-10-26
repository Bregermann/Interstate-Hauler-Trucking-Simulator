using System;
namespace LWS.TruckTetherball
{
    // Sport semantics only: the runtime feeds actual ball angles from its
    // Rigidbody. Touching the ball is deliberately not a scoring operation.
    [Serializable]public sealed class TetherRules
    {
        public int BestOf {get;private set;}
        public int TargetTurns {get;private set;}
        public int Round {get;private set;}
        public int RoundWinner {get;private set;}=-1;
        public int Winner {get;private set;}=-1;
        public bool Live {get;private set;}
        public bool Complete {get;private set;}
        public float NetDegrees {get;private set;}
        public float Clock {get;private set;}
        public readonly int[] wins=new int[2];
        bool sampled;float previous;
        public void Reset(int bestOf,int targetTurns)
        {
            if(bestOf!=3&&bestOf!=5||targetTurns<1||targetTurns>8)throw new ArgumentOutOfRangeException();
            BestOf=bestOf;TargetTurns=targetTurns;Round=0;Winner=-1;Complete=false;wins[0]=wins[1]=0;ResetRound();
        }
        void ResetRound(){RoundWinner=-1;NetDegrees=Clock=0;Live=sampled=false;previous=0;}
        public void Begin(){if(BestOf==0)throw new InvalidOperationException("Configure the match first.");if(!Complete&&RoundWinner<0)Live=true;}
        public bool NextRound(){if(Complete||RoundWinner<0)return false;Round++;ResetRound();return true;}
        public float Progress(int slot)=>Math.Max(0,Math.Min(1,NetDegrees*(slot==0?1:-1)/(TargetTurns*360f)));
        public void ObserveUnstruckBall(float angleDegrees,float radialMetres,float dt)
        {
            if(!Live||Complete||RoundWinner>=0||dt<=0||!Finite(dt)||!Finite(angleDegrees)||!Finite(radialMetres)||radialMetres<4)return;
            Clock+=dt;sampled=true;previous=angleDegrees;NetDegrees=0;
        }
        public void Sample(float angleDegrees,float radialMetres,float dt)
        {
            if(!Live||Complete||RoundWinner>=0||dt<=0||!Finite(dt)||!Finite(angleDegrees)||!Finite(radialMetres)||radialMetres<4)return;
            Clock+=dt;if(!sampled){sampled=true;previous=angleDegrees;return;}
            float delta=(float)Math.IEEERemainder((double)angleDegrees-previous,360);previous=angleDegrees;
            // Reject discontinuous resets/teleports, not ordinary fast orbits.
            if(Math.Abs(delta)>Math.Min(170,900*dt+5))return;
            NetDegrees+=delta;
            if(Math.Abs(NetDegrees)+.001f<TargetTurns*360)return;
            RoundWinner=NetDegrees>0?0:1;wins[RoundWinner]++;Live=false;
            if(wins[RoundWinner]>BestOf/2){Complete=true;Winner=RoundWinner;}
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
