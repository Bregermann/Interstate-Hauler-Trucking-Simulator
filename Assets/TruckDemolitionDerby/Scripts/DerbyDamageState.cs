using System;
namespace LWS.TruckDerby {
public enum DerbyZone{Front,Rear,Left,Right,Engine,Cab,Roof}
// Sport-specific impact attribution over NWH's real deformation and performance-damage APIs.
public sealed class DerbyDamageState {
 public readonly float[] zones=new float[7],wheels=new float[4];public float Structure{get;private set;}public float Engine=>zones[(int)DerbyZone.Engine];public bool Destroyed=>Structure>=1;public int Hits{get;private set;}
 public void Reset(){Array.Clear(zones,0,zones.Length);Array.Clear(wheels,0,wheels.Length);Structure=0;Hits=0;}
 public static float Impact(float normalSpeed,float mass,float otherMass,float impulse){if(!float.IsFinite(normalSpeed)||!float.IsFinite(mass)||!float.IsFinite(otherMass)||!float.IsFinite(impulse)||normalSpeed<2.3f||mass<=0)return 0;float effective=otherMass>0?mass*otherMass/(mass+otherMass):mass;float speed=Math.Min(25,normalSpeed);float energy=.5f*effective*speed*speed;if(energy<800)return 0;float confidence=Math.Max(0,Math.Min(1,impulse/Math.Max(1,effective*speed)));if(confidence<.08f)return 0;return Math.Min(.65f,energy/1000000f)*(.5f+.5f*confidence);}
 // Collision-health tuning only. Keep the original severe-contact ceiling and zone protection.
 public static float ScaleContact(float raw,float multiplier){if(!float.IsFinite(raw)||raw<=0)return 0;if(!float.IsFinite(multiplier))multiplier=2;return Math.Min(.65f,raw*Math.Max(.25f,Math.Min(4,multiplier)));}
 public void Apply(DerbyZone zone,float intensity,int wheel=-1){if(intensity<=0||Destroyed)return;float protection=zone==DerbyZone.Rear?.6f:zone==DerbyZone.Cab?1.2f:zone==DerbyZone.Roof?1.1f:1;float amount=Math.Min(.8f,intensity*protection);zones[(int)zone]=Math.Min(1,zones[(int)zone]+amount);if(zone==DerbyZone.Front)zones[(int)DerbyZone.Engine]=Math.Min(1,Engine+amount*.38f);if(zone==DerbyZone.Cab)zones[(int)DerbyZone.Engine]=Math.Min(1,Engine+amount*.12f);if(wheel>=0&&wheel<4)wheels[wheel]=Math.Min(1,wheels[wheel]+amount*.95f);Structure=Math.Min(1,Structure+amount*(zone==DerbyZone.Cab?.55f:zone==DerbyZone.Roof?.45f:.35f));Hits++;}
}
}
