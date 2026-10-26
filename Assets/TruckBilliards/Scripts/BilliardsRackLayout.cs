using System;
using UnityEngine;

namespace LWS.TruckBilliards
{
 // Placement only. PhysX supplies every contact and all scatter after the break.
 public static class BilliardsRackLayout
 {
  public const float MaximumImperfection=.008f;
  static float Clearance(Vector3[] positions,float[] radii)
  {
   float gap=float.PositiveInfinity;
   for(int i=1;i<positions.Length;i++)for(int j=i+1;j<positions.Length;j++)gap=Mathf.Min(gap,Vector2.Distance(new Vector2(positions[i].x,positions[i].z),new Vector2(positions[j].x,positions[j].z))-radii[i]-radii[j]);
   return gap;
  }
  public static Vector3[] Generate(Vector3[] authored,float[] radii,int seed,float imperfection)
  {
   if(authored==null||radii==null||authored.Length!=radii.Length)throw new ArgumentException("Rack positions and physical radii must match.");
   var positions=(Vector3[])authored.Clone();float budget=Mathf.Clamp(imperfection,0,MaximumImperfection);if(budget<=0||positions.Length<3)return positions;
   float gap=Clearance(positions,radii),jitter=budget;
   if(gap<budget)
   {
    // A touching rack needs a tiny safe opening, not a zero jitter multiplier.
    // Common dilation increases every pair distance without rearranging balls.
    Vector3 center=Vector3.zero;for(int i=1;i<positions.Length;i++)center+=positions[i];center/=(positions.Length-1);
    float reach=0;for(int i=1;i<positions.Length;i++)reach=Mathf.Max(reach,Vector3.ProjectOnPlane(positions[i]-center,Vector3.up).magnitude);
    if(reach<=.0001f)throw new InvalidOperationException("Authored rack has coincident ball centers.");
    for(int i=1;i<positions.Length;i++)positions[i]+=Vector3.ProjectOnPlane(positions[i]-center,Vector3.up)*(budget*.75f/reach);
    gap=Clearance(positions,radii);jitter=budget*.25f;
   }
   if(gap<=0)throw new InvalidOperationException("Authored rack overlaps beyond the tiny placement allowance.");
   jitter=Mathf.Min(jitter,gap*.45f);var random=new System.Random(seed);
   for(int i=1;i<positions.Length;i++){float angle=(float)random.NextDouble()*Mathf.PI*2,radius=Mathf.Sqrt((float)random.NextDouble())*jitter;positions[i]+=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);}
   return positions;
  }
 }
}
