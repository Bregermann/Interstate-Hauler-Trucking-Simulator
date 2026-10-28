using System;
namespace LWS.TruckBallet
{
    public enum BalletMove { Pirouette, SuspensionPlie, WheelieArabesque, SideLean, ControlledSpin, SmallHop, Balance, FinalFlourish }
    [Serializable]public struct BalletPose
    {
        public float pitch,lean,lift,turnRate,heading;
        public BalletPose(float pitch,float lean,float lift,float turnRate=0,float heading=0){this.pitch=pitch;this.lean=lean;this.lift=lift;this.turnRate=turnRate;this.heading=heading;}
        public bool Finite=>float.IsFinite(pitch)&&float.IsFinite(lean)&&float.IsFinite(lift)&&float.IsFinite(turnRate)&&float.IsFinite(heading);
    }
    public static class BalletMoves
    {
        public const int Count=8;
        public static readonly string[] Names={"PIROUETTE","SUSPENSION PLIE","WHEELIE ARABESQUE","SIDE-LEAN POSE","CONTROLLED SPIN","SMALL HOP","BALANCE POSE","FINAL FLOURISH"};
        public static BalletPose Goal(BalletMove move,bool rose=false)
        {
            switch(move)
            {
                case BalletMove.Pirouette:return new BalletPose(0,0,.42f,100);
                case BalletMove.SuspensionPlie:return new BalletPose(0,0,-.18f);
                case BalletMove.WheelieArabesque:return new BalletPose(-28,0,.35f);
                case BalletMove.SideLean:return new BalletPose(0,23,.12f);
                case BalletMove.ControlledSpin:return new BalletPose(0,0,.2f,52);
                case BalletMove.SmallHop:return new BalletPose(0,0,rose?0:1.2f);
                case BalletMove.Balance:return new BalletPose(-8,-20,.35f);
                default:return new BalletPose(-8,-12,.45f,35);
            }
        }
        public static float RequiredTurn(BalletMove move)=>move==BalletMove.Pirouette?360:move==BalletMove.ControlledSpin?180:move==BalletMove.FinalFlourish?90:0;
        public static float Accuracy(BalletPose actual,BalletPose goal)
        {
            if(!actual.Finite||!goal.Finite)return 0;
            float error=Math.Max(Math.Abs(actual.pitch-goal.pitch)/35,Math.Max(Math.Abs(actual.lean-goal.lean)/30,Math.Max(Math.Abs(actual.lift-goal.lift)/.3f,Math.Abs(actual.turnRate-goal.turnRate)/100)));
            return Math.Clamp(1-error,0,1);
        }
        public static float Stability(BalletPose now,BalletPose prior,float dt)
        {
            if(!now.Finite||!prior.Finite||!float.IsFinite(dt)||dt<=0||dt>.25f)return 0;
            float change=Math.Max(Math.Abs(now.pitch-prior.pitch)/45,Math.Max(Math.Abs(now.lean-prior.lean)/40,Math.Max(Math.Abs(now.lift-prior.lift)/1.1f,Math.Abs(now.turnRate-prior.turnRate)/190)))/dt;
            return Math.Clamp(1-change,0,1);
        }
    }
    // Score only observed body poses/turns, not a timer-driven demonstration.
    // Choreography windows bound a match; they cannot manufacture a completion.
    public sealed class BalletRules
    {
        public readonly float[] score=new float[4],accuracy=new float[4],stability=new float[4],hold=new float[4],turn=new float[4];
        public readonly int[] completed=new int[4];
        public readonly bool[] rose=new bool[4];
        readonly bool[] awarded=new bool[4],entered=new bool[4];
        readonly float[] entryHeading=new float[4];
        public int Count{get;private set;}
        public BalletMove Move{get;private set;}
        public float MoveSeconds{get;private set;}
        public bool Finished{get;private set;}
        public const float HoldRequired=1.8f,MoveLimit=18;
        public void Begin(int count)
        {
            if(count<1||count>4)throw new ArgumentOutOfRangeException(nameof(count));Count=count;Move=BalletMove.Pirouette;Finished=false;
            Array.Clear(score,0,4);Array.Clear(completed,0,4);ResetMove();
        }
        void ResetMove(){MoveSeconds=0;Array.Clear(accuracy,0,4);Array.Clear(stability,0,4);Array.Clear(hold,0,4);Array.Clear(turn,0,4);Array.Clear(rose,0,4);Array.Clear(awarded,0,4);Array.Clear(entered,0,4);}
        public BalletPose Goal(int slot)=>BalletMoves.Goal(Move,rose[slot]);
        public bool Observe(int slot,BalletPose actual,BalletPose previous,float dt)
        {
            if(Finished||slot<0||slot>=Count||!actual.Finite||!previous.Finite||!float.IsFinite(dt)||dt<=0||dt>.25f)return false;
            if(!entered[slot]){entered[slot]=true;entryHeading[slot]=actual.heading;}
            turn[slot]=actual.heading-entryHeading[slot];
            if(Move==BalletMove.SmallHop&&actual.lift>=1.05f)rose[slot]=true;
            accuracy[slot]=BalletMoves.Accuracy(actual,Goal(slot));stability[slot]=BalletMoves.Stability(actual,previous,dt);
            bool qualified=accuracy[slot]>=.9f&&stability[slot]>=.6f&&turn[slot]>=BalletMoves.RequiredTurn(Move)&&(Move!=BalletMove.SmallHop||rose[slot]);
            hold[slot]=qualified?hold[slot]+dt:0;
            if(accuracy[slot]>=.8f)score[slot]+=dt*8*accuracy[slot]*stability[slot];
            if(awarded[slot]||hold[slot]<HoldRequired)return false;
            awarded[slot]=true;completed[slot]++;score[slot]+=25+25*Math.Clamp(1-MoveSeconds/MoveLimit,0,1);return true;
        }
        public bool Tick(float dt)
        {
            if(Finished||!float.IsFinite(dt)||dt<=0||dt>.25f)return false;
            MoveSeconds+=dt;bool all=true;for(int i=0;i<Count;i++)all&=awarded[i];if(!all&&MoveSeconds<MoveLimit)return false;
            if((int)Move==BalletMoves.Count-1){Finished=true;return true;}Move++;ResetMove();return true;
        }
        public int Place(int slot){int rank=1;for(int i=0;i<Count;i++)if(score[i]>score[slot]+.01f)rank++;return rank;}
    }
}
