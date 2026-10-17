using UnityEngine;using LWS.TruckTaxi;
namespace LWS.TruckSports {
// Reuses Taxi's signed-angle, rate-limited steering presentation with this sport's resolved NWH road-wheel angle.
[DefaultExecutionOrder(180)]public sealed class SportsCabSteering:MonoBehaviour {
 public Transform wheel;public float lockToLockDegrees=900;public SportsEventTruck Truck{get;private set;}public float CurrentAngle=>motion.Angle;public float TargetAngle{get;private set;}
 Quaternion neutral;TruckTaxiSteeringVisualMotion motion;bool initialized;System.Func<float> readAngle;System.Func<bool> paused;float sourceMax;
 void Awake(){Initialize();}
 void Initialize(){if(initialized||wheel==null)return;neutral=wheel.localRotation;motion=new TruckTaxiSteeringVisualMotion(0);initialized=true;}
 public void BindSource(System.Func<float> source,float maxAngle,System.Func<bool> isPaused){Initialize();Truck=null;readAngle=source;sourceMax=maxAngle;paused=isPaused;motion=new TruckTaxiSteeringVisualMotion(0);TargetAngle=0;if(wheel!=null)wheel.localRotation=neutral;}
 public void Bind(SportsEventTruck truck){readAngle=null;paused=null;Initialize();if(Truck==truck)return;Truck=truck;motion=new TruckTaxiSteeringVisualMotion(0);TargetAngle=0;if(wheel!=null)wheel.localRotation=neutral;}
 void LateUpdate(){if(!initialized||Truck==null&&readAngle==null||wheel==null||Time.deltaTime<=0||Truck!=null&&Truck.session.Paused||paused!=null&&paused())return;float angle=readAngle!=null?readAngle():(Truck.wheels[0].SteerAngle+Truck.wheels[1].SteerAngle)*.5f;TargetAngle=TruckTaxiSteeringWheelVisual.ResolveTargetAngle(angle,readAngle!=null?sourceMax:Truck.steerAngle,lockToLockDegrees);bool returning=Mathf.Abs(TargetAngle)<Mathf.Abs(motion.Angle)&&(Mathf.Sign(TargetAngle)==Mathf.Sign(motion.Angle)||Mathf.Abs(TargetAngle)<.01f);motion.Step(TargetAngle,Time.deltaTime,returning?.23f:.16f,1,900);wheel.localRotation=neutral*Quaternion.AngleAxis(motion.Angle,Vector3.back);}
}
}
