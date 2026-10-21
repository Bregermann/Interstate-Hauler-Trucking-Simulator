using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckHideAndSeek
{
 public sealed class HideEngine:MonoBehaviour
 {
  public SportsEventTruck truck;public float restartDelay=1.5f;public bool Lights{get;private set;}public bool Restarting=>restartAt>=0;public bool Running=>truck.tractorMotor.vehicle.powertrain.engine.IsRunning;
  float restartAt=-1,heldFor;bool wasHeld,heldConsumed;
  public void ResetStealth(bool lights){restartAt=-1;heldFor=0;wasHeld=heldConsumed=false;var engine=truck.tractorMotor.vehicle.powertrain.engine;engine.autoStartOnThrottle=false;engine.flyingStartEnabled=true;engine.StartEngine();SetLights(lights);}
  public void SetLights(bool value){Lights=value;truck.tractorMotor.vehicle.effectsManager.lightsManager.lowBeamLights.SetState(value);}
  public void ToggleEngine(float now){if(Running){truck.tractorMotor.vehicle.powertrain.engine.StopEngine();restartAt=-1;}else if(!Restarting)restartAt=now+restartDelay;}
  public void RequestEngine(bool running,float now){if(running&&!Running&&!Restarting)restartAt=now+restartDelay;else if(!running){restartAt=-1;if(Running)truck.tractorMotor.vehicle.powertrain.engine.StopEngine();}}
  public void Step(bool held,float dt,float now,bool permitted)
  {
   if(restartAt>=0&&now>=restartAt){truck.tractorMotor.vehicle.powertrain.engine.StartEngine();restartAt=-1;}
   if(!permitted){wasHeld=held;heldFor=0;heldConsumed=held;return;}
   if(held){heldFor+=dt;if(heldFor>=.75f&&!heldConsumed){ToggleEngine(now);heldConsumed=true;}}
   else{if(wasHeld&&!heldConsumed)SetLights(!Lights);heldFor=0;heldConsumed=false;}wasHeld=held;
  }
 }
}
