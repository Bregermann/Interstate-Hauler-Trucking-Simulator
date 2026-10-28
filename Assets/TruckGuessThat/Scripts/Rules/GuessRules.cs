using System;
using System.Collections.Generic;
using System.Linq;
namespace LWS.TruckGuessThat
{
    [Serializable] public sealed class GuessIdentity
    {
        public string id,name,body,hat,eyes,mouth,mirrors,wheels;
        public string bodyLabel,hatLabel,eyesLabel,mouthLabel,mirrorsLabel,wheelsLabel;
        public string TraitLabel(int family){string[] labels={bodyLabel,hatLabel,eyesLabel,mouthLabel,mirrorsLabel,wheelsLabel};return string.IsNullOrEmpty(labels[family])?Trait(family):labels[family];}
        public string Trait(int family) { switch(family){case 0:return body;case 1:return hat;case 2:return eyes;case 3:return mouth;case 4:return mirrors;case 5:return wheels;default:throw new ArgumentOutOfRangeException();} }
    }
    [Serializable] public sealed class GuessQuestion
    {
        public int family;public string value,label;
        public bool Matches(GuessIdentity identity)=>identity.Trait(family)==value;
    }
    public readonly struct GuessAnswer
    {
        public readonly int asker,target,question;public readonly bool yes;public readonly int remaining;
        public GuessAnswer(int a,int t,int q,bool y,int r){asker=a;target=t;question=q;yes=y;remaining=r;}
    }
    public sealed class GuessRules
    {
        public readonly GuessIdentity[] Identities;
        public readonly GuessQuestion[] Questions;
        readonly int[] secrets; readonly bool[][] candidates; readonly HashSet<int>[] asked;
        public int Players=>secrets.Length;
        public int Turn { get; private set; }
        public int Target=> (Turn+1)%Players;
        public int Winner { get; private set; }=-1;
        public int Turns { get; private set; }
        public bool AwaitingNext { get; private set; }
        public GuessRules(GuessIdentity[] roster,int players,int seed)
        {
            if(roster==null||roster.Length!=24||players<2||players>4||roster.Select(i=>i.id).Distinct().Count()!=24)throw new ArgumentException("24 unique trucks and 2–4 seats are required.");
            Identities=roster;var list=new List<GuessQuestion>();
            string[] stems={"BODY: ","HAT: ","EYES: ","MOUTH: ","MIRRORS: ","WHEELS: "};
            for(int f=0;f<6;f++)foreach(string v in roster.Select(i=>i.Trait(f)).Distinct())list.Add(new GuessQuestion{family=f,value=v,label=stems[f]+roster.First(i=>i.Trait(f)==v).TraitLabel(f)});
            Questions=list.ToArray();secrets=new int[players];candidates=new bool[players][];asked=new HashSet<int>[players];var random=new Random(seed);
            for(int p=0;p<players;p++){secrets[p]=random.Next(24);candidates[p]=Enumerable.Repeat(true,24).ToArray();asked[p]=new HashSet<int>();}
        }
        public bool Candidate(int seat,int identity)=>candidates[seat][identity];
        public int[] PublicCandidates(int seat)=>Enumerable.Range(0,24).Where(i=>candidates[seat][i]).ToArray();
        public bool CanAsk(int seat,int question)
        {
            if(seat!=Turn||Winner>=0||AwaitingNext||question<0||question>=Questions.Length||asked[seat].Contains(question))return false;
            int[] remaining=PublicCandidates(seat);int yes=remaining.Count(i=>Questions[question].Matches(Identities[i]));return yes>0&&yes<remaining.Length;
        }
        public bool Ask(int seat,int question,out GuessAnswer answer)
        {
            answer=default;if(!CanAsk(seat,question))return false;bool yes=Questions[question].Matches(Identities[secrets[Target]]);asked[seat].Add(question);
            for(int i=0;i<24;i++)if(candidates[seat][i]&&Questions[question].Matches(Identities[i])!=yes)candidates[seat][i]=false;
            answer=new GuessAnswer(seat,Target,question,yes,PublicCandidates(seat).Length);AwaitingNext=true;Turns++;return true;
        }
        public bool Guess(int seat,int identity,out bool correct)
        {
            correct=false;if(seat!=Turn||Winner>=0||AwaitingNext||identity<0||identity>=24||!candidates[seat][identity])return false;
            correct=identity==secrets[Target];if(correct)Winner=seat;else candidates[seat][identity]=false;AwaitingNext=true;Turns++;return true;
        }
        public void Next(){if(!AwaitingNext||Winner>=0)return;Turn=(Turn+1)%Players;AwaitingNext=false;}
        // This owner-only value is for the private reveal panel. Deduction AI is deliberately a separate public-data function.
        public int OwnerSecret(int seat)=>secrets[seat];
    }
    public static class GuessDeduction
    {
        // No GuessRules, secret, opponent or RNG argument: decisions can only depend on public evidence.
        public static int BestQuestion(GuessIdentity[] roster,GuessQuestion[] questions,int[] candidates,Func<int,bool> allowed)
        {
            int best=-1,spread=int.MaxValue;for(int q=0;q<questions.Length;q++){if(!allowed(q))continue;int yes=candidates.Count(i=>questions[q].Matches(roster[i]));int d=Math.Abs(candidates.Length-2*yes);if(yes>0&&yes<candidates.Length&&d<spread){best=q;spread=d;}}return best;
        }
    }
}
