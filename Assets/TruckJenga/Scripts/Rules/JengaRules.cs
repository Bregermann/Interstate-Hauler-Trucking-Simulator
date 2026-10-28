using System;
using System.Linq;
namespace LWS.TruckJenga
{
    // Rules accept physical observations; they never move bodies or manufacture a collapse.
    public sealed class JengaRules
    {
        public enum Stage { Preparing, Selecting, Pulling, Crane, Settling, Complete }
        public Stage Phase { get; private set; }=Stage.Preparing;
        public int Owner { get; private set; }
        public int Responsible { get; private set; }=-1;
        public int Selected { get; private set; }=-1;
        public int Loser { get; private set; }=-1;
        public int Turn { get; private set; }=1;
        public readonly int[] Stacks;
        readonly int[] rows,columns;
        public int Count=>rows.Length;
        public int Row(int piece)=>rows[piece];public int Column(int piece)=>columns[piece];
        public int Highest=>rows.Max();
        public int HighestComplete { get { for(int r=Highest;r>=0;r--)if(rows.Count(value=>value==r)==3)return r;return -1; } }
        public JengaRules(int participants,int layers=6){if(participants<2||participants>4||layers<4||layers>10)throw new ArgumentOutOfRangeException();Stacks=new int[participants];rows=new int[layers*3];columns=new int[rows.Length];for(int i=0;i<rows.Length;i++){rows[i]=i/3;columns[i]=i%3;}}
        public void FoundationReady(){if(Phase==Stage.Preparing)Phase=Stage.Selecting;}
        public bool Legal(int piece)=>piece>=0&&piece<Count&&rows[piece]<HighestComplete;
        public int[] LegalPieces()=>Enumerable.Range(0,Count).Where(Legal).ToArray();
        public bool BeginPull(int owner,int piece){if(Phase!=Stage.Selecting||owner!=Owner||!Legal(piece))return false;Selected=piece;Responsible=owner;Phase=Stage.Pulling;return true;}
        public bool Extracted(int piece,bool entireBodyClear){if(Phase!=Stage.Pulling||piece!=Selected||!entireBodyClear)return false;Phase=Stage.Crane;return true;}
        public void NextSlot(out int row,out int column){int top=Highest;int count=rows.Count(value=>value==top);if(count==3){row=top+1;column=0;}else{row=top;column=Enumerable.Range(0,3).First(c=>!Enumerable.Range(0,Count).Any(i=>rows[i]==top&&columns[i]==c));}}
        public bool Restacked(int piece,bool releasedAndActuallyResting){if(Phase!=Stage.Crane||piece!=Selected||!releasedAndActuallyResting)return false;NextSlot(out int row,out int column);rows[piece]=row;columns[piece]=column;Stacks[Owner]++;Phase=Stage.Settling;return true;}
        public bool NextTurn(bool towerActuallySettled){if(Phase!=Stage.Settling||!towerActuallySettled)return false;Owner=(Owner+1)%Stacks.Length;Selected=-1;Turn++;Phase=Stage.Selecting;return true;}
        public bool Collapse(bool physicalCollapse){if(!physicalCollapse||Responsible<0||Phase==Stage.Complete)return false;Loser=Responsible;Phase=Stage.Complete;return true;}
    }
    public static class JengaSelection
    {
        // Public tower occupancy only. Prefer a supported middle piece, and spread removals among layers.
        public static int Careful(JengaRules rules)
        {
            int best=-1;float score=float.MaxValue;foreach(int p in rules.LegalPieces()){int row=rules.Row(p);int remaining=Enumerable.Range(0,rules.Count).Count(i=>rules.Row(i)==row);float risk=(3-remaining)*12+(rules.Column(p)==1?0:5)+(row==0?4:0)+row*.15f;if(risk<score){score=risk;best=p;}}return best;
        }
    }
}
