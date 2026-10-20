using System;
namespace LWS.TruckJousting
{
    // One score authority per pass. Actual lance contacts supply the hit, never proximity.
    [Serializable] public sealed class JoustRules
    {
        public int PassLimit{get;private set;} public int Pass{get;private set;}
        public bool Live{get;private set;} public bool PassClosed{get;private set;} public bool Complete{get;private set;}
        public float Clock{get;private set;} public int Winner{get;private set;}=-1;
        public readonly int[] points=new int[2],passPoints=new int[2],hits=new int[2],centralHits=new int[2],knockdowns=new int[2];
        readonly bool[] struck=new bool[2],down=new bool[2];
        public float MinimumImpact=6;public int ArmorPoints=1,CentralPoints=2,KnockdownPoints=3;
        public void Reset(int passes)
        {
            if(passes!=3&&passes!=5)throw new ArgumentOutOfRangeException(nameof(passes));
            PassLimit=passes;Pass=1;Winner=-1;Complete=false;
            foreach(var array in new[]{points,passPoints,hits,centralHits,knockdowns})Array.Clear(array,0,2);
            ClearPass();
        }
        void ClearPass(){Live=PassClosed=false;Clock=0;Array.Clear(passPoints,0,2);Array.Clear(struck,0,2);Array.Clear(down,0,2);}
        public void Begin(){if(PassLimit==0)throw new InvalidOperationException("Configure the tournament first.");if(!Complete&&!PassClosed)Live=true;}
        public void Tick(float dt){if(Live&&dt>0&&!float.IsNaN(dt)&&!float.IsInfinity(dt))Clock+=dt;}
        public bool Hit(int attacker,bool central,float incomingNormalSpeed)
        {
            if(attacker<0||attacker>1||!Live||Complete||struck[attacker]||float.IsNaN(incomingNormalSpeed)||float.IsInfinity(incomingNormalSpeed)||incomingNormalSpeed<MinimumImpact)return false;
            struck[attacker]=true;hits[attacker]++;if(central)centralHits[attacker]++;
            passPoints[attacker]=central?CentralPoints:ArmorPoints;points[attacker]+=passPoints[attacker];return true;
        }
        public bool Knockdown(int victim)
        {
            if(victim<0||victim>1||!Live||Complete||down[victim]||!struck[1-victim])return false;
            down[victim]=true;int attacker=1-victim;int delta=Math.Max(0,KnockdownPoints-passPoints[attacker]);points[attacker]+=delta;passPoints[attacker]+=delta;knockdowns[attacker]++;return true;
        }
        public bool EndPass()
        {
            if(!Live||PassClosed||Complete)return false;Live=false;PassClosed=true;
            if(Pass>=PassLimit){Complete=true;Winner=points[0]==points[1]?-1:points[0]>points[1]?0:1;}return true;
        }
        public bool NextPass(){if(!PassClosed||Complete)return false;Pass++;ClearPass();return true;}
    }
}
