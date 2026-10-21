using System;
namespace LWS.TruckHideAndSeek
{
 // Perception semantics only. PhysX supplies actual occlusion; CNP displays the
 // resulting broad sector without publishing exact hidden vehicle coordinates.
 public static class HideSignalRules
 {
  public static float VisualRange(bool running,bool targetLights,bool moving,bool seekerLights,float visibility){float range=targetLights?220:moving?150:running?80:45;if(seekerLights)range+=35;return range*Math.Max(.3f,Math.Min(1,float.IsFinite(visibility)?visibility:.3f));}
  public static bool CanSee(float distance,float facing,bool solidWall,bool running,bool targetLights,bool moving,bool seekerLights,float visibility)=>!solidWall&&distance>=0&&distance<=VisualRange(running,targetLights,moving,seekerLights,visibility)&&facing>=.3f;
  public static float AudibleRange(bool running,float speed,bool solidWall){float range=running?110:speed>2?45:0;return solidWall?range*.3f:range;}
  public static int Sector(float bearingDegrees){float angle=(bearingDegrees%360+360)%360;return (int)Math.Floor((angle+22.5f)/45)%8;}
  public static string Band(float distance)=>distance<50?"NEAR":distance<140?"MID-RANGE":"DISTANT";
 }
}
