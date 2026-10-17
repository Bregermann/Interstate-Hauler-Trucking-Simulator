using UnityEngine;using NWH.VehiclePhysics2;using NWH.Common.Vehicles;
namespace LWS.TruckSports {
// Reusable, explicitly installed bridge. NWH alone simulates powertrain, tires and steering.
// No existing sport receives this component; Derby opts into its localized performance policy.
public sealed class SportsNwhDamageAdapter:MonoBehaviour {
 public VehicleController vehicle;public bool gameplayEffects;WheelUAPI[] wheels;float[] longitudinal,lateral;float steeringBias;bool initialized;
 void Awake(){Initialize();}
 void Initialize(){if(initialized)return;int n=vehicle.powertrain.wheels.Count;wheels=new WheelUAPI[n];longitudinal=new float[n];lateral=new float[n];for(int i=0;i<n;i++){wheels[i]=vehicle.powertrain.wheels[i].wheelUAPI;longitudinal[i]=wheels[i].LongitudinalFrictionGrip;lateral[i]=wheels[i].LateralFrictionGrip;}steeringBias=vehicle.steering.externallyAddedAngle;initialized=true;}
 public void Apply(float engine,float transmission,float[] wheelDamage){Initialize();if(!gameplayEffects)return;vehicle.powertrain.engine.Damage=Mathf.Clamp01(engine);vehicle.powertrain.transmission.Damage=Mathf.Clamp01(transmission);for(int i=0;i<wheels.Length;i++){float damage=i<wheelDamage.Length?Mathf.Clamp01(wheelDamage[i]):0;wheels[i].Damage=damage;wheels[i].LongitudinalFrictionGrip=longitudinal[i]*Mathf.Lerp(1,.3f,damage);wheels[i].LateralFrictionGrip=lateral[i]*Mathf.Lerp(1,.45f,damage);}vehicle.steering.externallyAddedAngle=steeringBias+(wheelDamage.Length>=2?(wheelDamage[0]-wheelDamage[1])*3:0);}
 public void Repair(){Initialize();vehicle.powertrain.Repair();for(int i=0;i<wheels.Length;i++){wheels[i].Damage=0;wheels[i].LongitudinalFrictionGrip=longitudinal[i];wheels[i].LateralFrictionGrip=lateral[i];}vehicle.steering.externallyAddedAngle=steeringBias;}
}
}
