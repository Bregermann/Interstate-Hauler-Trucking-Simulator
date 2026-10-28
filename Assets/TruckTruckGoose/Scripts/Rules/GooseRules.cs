using System;
namespace LWS.TruckTruckGoose
{
    // No input, transforms or AI outcomes: only observed physical progress can claim a bay.
    public sealed class GooseRules
    {
        public enum Stage { Picking, Racing, RoundResult, Complete }
        public readonly int[] Scores;
        public int Picker { get; private set; }
        public int Goose { get; private set; } = -1;
        public int Winner { get; private set; } = -1;
        public int Round { get; private set; } = 1;
        public int Rounds { get; }
        public Stage Phase { get; private set; }
        readonly double[] previous, progress;
        public GooseRules(int count, int rounds) { if(count<8||count>12||rounds<1)throw new ArgumentOutOfRangeException();Scores=new int[count];previous=new double[count];progress=new double[count];Rounds=rounds; }
        public double Progress(int slot) => progress[slot];
        public int Direction(int slot) => slot==Picker?1:slot==Goose?-1:0;
        public bool Select(int candidate, double pickerAngle, double gooseAngle, bool physicallyNearby)
        {
            if(Phase!=Stage.Picking||!physicallyNearby||candidate<0||candidate>=Scores.Length||candidate==Picker)return false;
            Goose=candidate;Winner=-1;Array.Clear(progress,0,progress.Length);previous[Picker]=pickerAngle;previous[Goose]=gooseAngle;Phase=Stage.Racing;return true;
        }
        public void Observe(int slot,double angle,bool inLegalAnnulus)
        {
            if(Phase!=Stage.Racing||Direction(slot)==0)return;
            double delta=Wrap(angle-previous[slot]);previous[slot]=angle;
            // Reject teleports, central cuts and wrong-way progress. Reverse travel undoes earned progress.
            if(!inLegalAnnulus||!double.IsFinite(delta)||Math.Abs(delta)>12){progress[slot]=0;return;}
            progress[slot]=Math.Max(0,progress[slot]+delta*Direction(slot));
        }
        public void Recover(int slot,double angle) { previous[slot]=angle;progress[slot]=Math.Max(0,progress[slot]-12); }
        public bool Claim(int slot,bool inBay,double speed,bool upright)
        {
            if(Phase!=Stage.Racing||Direction(slot)==0||progress[slot]<350||!inBay||!double.IsFinite(speed)||speed<0||speed>4||!upright)return false;
            Winner=slot;Scores[slot]++;Phase=Stage.RoundResult;return true;
        }
        public bool NextRound()
        {
            if(Phase!=Stage.RoundResult)return false;
            if(Round>=Rounds){Phase=Stage.Complete;return false;}
            Picker=Winner==Picker?Goose:Picker;Goose=-1;Winner=-1;Round++;Phase=Stage.Picking;return true;
        }
        public static double Wrap(double degrees) { degrees%=360;if(degrees>180)degrees-=360;if(degrees< -180)degrees+=360;return degrees; }
    }
}
