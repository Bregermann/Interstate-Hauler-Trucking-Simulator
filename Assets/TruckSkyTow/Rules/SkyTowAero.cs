using System;
namespace LWS.TruckSkyTow {
// A self-trimming ram-air canopy: wind still changes support, but ordinary
// tailwinds must not make full climb input incapable of opposing gravity.
// No target/ring/AI information enters this bounded force model.
public static class SkyTowAero {
 public static float Neutral(float apparentSpeed,float cruise,float gravity){if(!Finite(apparentSpeed)||!Finite(cruise)||!Finite(gravity)||apparentSpeed<0||cruise<=0||gravity<=0)throw new ArgumentException("Canopy requires finite nonnegative airspeed, positive cruise and gravity.");return gravity*(float)Math.Sqrt(apparentSpeed/cruise);}
 public static float Acceleration(float apparentSpeed,float cruise,float input,float gravity,float maximum,float controlRange=.9f){if(!Finite(input)||!Finite(maximum)||maximum<=0||!Finite(controlRange)||controlRange<=0||controlRange>1)throw new ArgumentException("Canopy control/cap must be finite and bounded.");float command=Math.Max(-1,Math.Min(1,input));return Math.Min(maximum,Math.Max(0,Neutral(apparentSpeed,cruise,gravity)*(1+command*controlRange)));}
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
