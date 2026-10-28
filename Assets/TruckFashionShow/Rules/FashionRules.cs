using System;
using System.Collections.Generic;
using System.Linq;
namespace LWS.TruckFashionShow
{
    public enum FashionTheme { Royalty, FancyEvening, Spooky, WildlyRidiculous }
    [Flags]public enum FashionTag { None=0, Royal=1, Elegant=2, Spooky=4, Playful=8, Gold=16, Chrome=32, Ruby=64, Novelty=128 }
    public readonly struct FashionItem
    {
        public readonly string id,slot;public readonly FashionTag tags;
        public FashionItem(string stableId,string semanticSlot,FashionTag actualTags){id=stableId;slot=semanticSlot;tags=actualTags;}
    }
    public readonly struct FashionGrade
    {
        public readonly int theme,coordination,creativity,qte,poses,completion;
        public int Total=>theme+coordination+creativity+qte+poses+completion;
        public FashionGrade(int t,int c,int a,int q,int p,int f){theme=t;coordination=c;creativity=a;qte=q;poses=p;completion=f;}
    }
    // Explicit tags describe actual owned catalog visuals, never arbitrary names or hidden random judging.
    public static class FashionRules
    {
        public static FashionTag Tags(string id)
        {
            switch(id)
            {
                case "hat.crown":return FashionTag.Royal|FashionTag.Gold|FashionTag.Elegant;
                case "hat.wizard":case "hat.viking":return FashionTag.Spooky|FashionTag.Novelty;
                case "hat.party":case "hat.propeller":return FashionTag.Playful|FashionTag.Novelty;
                case "hat.pirate":return FashionTag.Spooky|FashionTag.Playful;
                case "hat.baseball":case "hat.hardhat":return FashionTag.Playful;
                case "lashes.long":case "mustache.handlebar":return FashionTag.Elegant;
                case "mouth.goofy":case "rear.party":return FashionTag.Playful|FashionTag.Novelty;
                case "mirrors.gold":case "wheels.gold":return FashionTag.Gold|FashionTag.Royal|FashionTag.Elegant;
                case "mirrors.chrome":case "wheels.chrome":case "rear.chrome":return FashionTag.Chrome|FashionTag.Elegant;
                case "mirrors.ruby":case "wheels.ruby":return FashionTag.Ruby|FashionTag.Playful;
                case "novelty.hotdog":case "novelty.icecream":return FashionTag.Playful|FashionTag.Novelty;
                case "semi.lowrider":return FashionTag.Chrome|FashionTag.Elegant;
                default:return FashionTag.None;
            }
        }
        public static FashionTag ThemeTag(FashionTheme theme)=>(FashionTag)(1<<(int)theme);
        public static FashionGrade Judge(FashionTheme theme,IEnumerable<FashionItem> actualAppliedItems,int successfulNotes,int totalNotes,int timingScore,int maximumTimingScore,bool reachedActualFinalMark)
        {
            if(!Enum.IsDefined(typeof(FashionTheme),theme)||actualAppliedItems==null||totalNotes<1||successfulNotes<0||successfulNotes>totalNotes||timingScore<0||maximumTimingScore<1)throw new ArgumentException("Known theme, actual applied items and bounded actual performance are required.");
            var items=actualAppliedItems.ToArray();if(items.Any(i=>string.IsNullOrWhiteSpace(i.id)||string.IsNullOrWhiteSpace(i.slot))||items.Select(i=>i.slot).Distinct().Count()!=items.Length)throw new ArgumentException("One real equipped item per stable semantic slot.");
            int relevant=items.Count(i=>(i.tags&ThemeTag(theme))!=0),matching=0;
            foreach(var finish in new[]{FashionTag.Gold,FashionTag.Chrome,FashionTag.Ruby})matching=Math.Max(matching,items.Count(i=>(i.tags&finish)!=0));
            int creative=items.Count(i=>i.slot!="body")*8+items.Count(i=>(i.tags&FashionTag.Novelty)!=0)*4;
            return new FashionGrade(Math.Min(120,relevant*40),Math.Min(60,Math.Max(0,matching-1)*30),Math.Min(80,creative),Math.Min(600,(int)Math.Round(Math.Min(timingScore,maximumTimingScore)*600d/maximumTimingScore)),successfulNotes*20,reachedActualFinalMark?100:0);
        }
    }
    public sealed class FashionCompetition
    {
        public readonly int[] Scores;public readonly FashionGrade[] Grades;public int Completed{get;private set;}
        public FashionCompetition(int contestants){if(contestants<1||contestants>4)throw new ArgumentOutOfRangeException();Scores=new int[contestants];Grades=new FashionGrade[contestants];}
        public bool Lock(int owner,FashionGrade actualGrade){if(owner!=Completed||owner<0||owner>=Scores.Length||actualGrade.theme<0||actualGrade.theme>120||actualGrade.coordination<0||actualGrade.coordination>60||actualGrade.creativity<0||actualGrade.creativity>80||actualGrade.qte<0||actualGrade.qte>600||actualGrade.poses<0||actualGrade.poses>240||(actualGrade.completion!=0&&actualGrade.completion!=100))return false;Grades[owner]=actualGrade;Scores[owner]=actualGrade.Total;Completed++;return true;}
        public bool Complete=>Completed==Scores.Length;
        public int[] Leaders(){int best=Scores.Max();return Enumerable.Range(0,Scores.Length).Where(i=>Scores[i]==best).ToArray();}
    }
}
