using System;
namespace LWS.TruckMagnetMayhem
{
 // Sport-specific force law; Unity/NWH remain the physics authorities.
 public static class MagnetForce
 {
  public static float Newtons(int sourcePole,int targetPole,float distance,float range,float sourceMass,float targetMass,float maximumForce,float maximumAcceleration)
  {
   if(sourcePole==0||sourcePole< -1||sourcePole>1||targetPole< -1||targetPole>1||distance<.4f||range<=.4f||distance>=range||sourceMass<=0||targetMass<=0||maximumForce<=0||maximumAcceleration<=0)return 0;
   if(!Finite(distance)||!Finite(range)||!Finite(sourceMass)||!Finite(targetMass)||!Finite(maximumForce)||!Finite(maximumAcceleration))return 0;
   float falloff=1-distance/range;float force=Math.Min(maximumForce,Math.Min(sourceMass,targetMass)*maximumAcceleration)*falloff*falloff;
   return sourcePole==targetPole?-force:force;
  }
  static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 }
}
