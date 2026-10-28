using System;
namespace LWS.TruckSnowmanBuilding
{
    public sealed class SnowmanRules
    {
        public int Placed { get; private set; }public int Decorations { get; private set; }public bool Complete { get; private set; }public int Score { get; private set; }
        public bool Place(int role,bool actualRestContact){if(role!=Placed||role<0||role>2||!actualRestContact||Complete)return false;Placed++;return true;}
        public bool Decorate(int family,bool actualDelivered){if(Placed!=3||family<0||family>5||!actualDelivered||Complete)return false;Decorations|=1<<family;return true;}
        public bool Finish(bool physicallyStable,double height,double baseRadius,double middleRadius,double headRadius,double stability,double seconds){if(Complete||Placed!=3||Decorations!=63||!physicallyStable||!double.IsFinite(height)||!double.IsFinite(seconds)||height<=0||seconds<0||baseRadius<=0||middleRadius<=0||headRadius<=0||!double.IsFinite(baseRadius+middleRadius+headRadius+stability))return false;Score=Grade(height,baseRadius,middleRadius,headRadius,stability,seconds);Complete=true;return true;}
        public static int Grade(double height,double a,double b,double c,double stability,double time){if(!double.IsFinite(a+b+c+stability+time)||a<=0||b<=0||c<=0)return 0;double Clamp(double x)=>Math.Max(0,Math.Min(1,x));double proportions=(Clamp(1-Math.Abs(b/a-.72)/.72)+Clamp(1-Math.Abs(c/b-.7)/.7))*50;return (int)Math.Round(Math.Min(180,height*12)+proportions+Clamp(stability)*100+90+Math.Max(0,120-time*.15));}
        public static double Grow(double radius,double distance,double angleRadians,double depth,double available,double maximum,out double consumed){consumed=0;if(!double.IsFinite(radius+distance+angleRadians+depth+available+maximum)||radius<=0||distance<=0||distance>3||angleRadians*radius<distance*.3||depth<=0||available<=0)return radius;double desired=Math.Min(available,distance*2*radius*depth);double volume=4*Math.PI*radius*radius*radius/3;double cap=4*Math.PI*maximum*maximum*maximum/3;consumed=Math.Max(0,Math.Min(desired,cap-volume));return Math.Pow((volume+consumed)*3/(4*Math.PI),1d/3);}
        public void Restore(int placed,int decorations,bool complete,int score){if(placed<0||placed>3||decorations<0||decorations>63||complete&&(placed!=3||decorations!=63||score<=0))throw new ArgumentException();Placed=placed;Decorations=decorations;Complete=complete;Score=score;}
    }
}
