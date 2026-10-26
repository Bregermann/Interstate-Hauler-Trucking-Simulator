using UnityEngine;

namespace LWS.TruckBaseJumping
{
 // Six bounded emitters reuse the repaired owned NWH smoke atlas/material.
 // They have no colliders, bodies, lights, cameras or force/scoring callbacks.
 public sealed class BaseGustSweep
 {
  readonly ParticleSystem[] emitters=new ParticleSystem[6];readonly bool[] playing=new bool[6];
  public int PoolCount{get;private set;}
  public ParticleSystem Emitter(int index)=>emitters[index];
  public BaseGustSweep(BaseJumpWeather owner)
  {
   ParticleSystem source=null;if(owner.match!=null&&owner.match.skyField!=null&&owner.match.skyField.bodies!=null)foreach(var item in owner.match.skyField.bodies)if(item!=null&&item.kind==2&&item.smoke!=null){source=item.smoke;break;}
   if(source==null){Debug.LogWarning("BASE_GUST_VISUAL_SOURCE_MISSING: existing repaired NWH flare smoke is required.");return;}
   for(int i=0;i<emitters.Length;i++)
   {
    var effect=Object.Instantiate(source,owner.transform,false);effect.name="Pooled NWH sweeping air gust "+i;effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    var main=effect.main;main.playOnAwake=false;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(1.2f,1.7f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(6,10);main.gravityModifier=0;main.maxParticles=48;
    var emission=effect.emission;emission.enabled=true;emission.rateOverTime=10;var shape=effect.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(45,35,18);
    var collision=effect.collision;collision.enabled=false;var inherit=effect.inheritVelocity;inherit.enabled=false;var force=effect.forceOverLifetime;force.enabled=false;var velocity=effect.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
    effect.gameObject.SetActive(false);emitters[i]=effect;PoolCount++;
   }
  }
  public void Tick(BaseWindGusts gust)
  {
   for(int i=0;i<emitters.Length;i++)
   {
    var effect=emitters[i];if(effect==null)continue;
    if(!gust.Active){if(effect.isPlaying)effect.Stop(true,ParticleSystemStopBehavior.StopEmitting);if(effect.particleCount==0)effect.gameObject.SetActive(false);continue;}
    var tangent=Vector3.Cross(Vector3.up,gust.Direction);var center=gust.Center+tangent*((i%3-1)*80)+Vector3.up*(i<3?65:165);effect.transform.SetPositionAndRotation(center,Quaternion.LookRotation(gust.Direction));
    var main=effect.main;main.startColor=new Color(.78f,.84f,.85f,.2f*gust.Envelope);var velocity=effect.velocityOverLifetime;velocity.x=gust.Direction.x*15;velocity.y=0;velocity.z=gust.Direction.z*15;
    if(!effect.gameObject.activeSelf)effect.gameObject.SetActive(true);if(!effect.isPlaying)effect.Play();
   }
  }
  public void Pause(bool pause){for(int i=0;i<emitters.Length;i++)if(emitters[i]!=null){if(pause){playing[i]=emitters[i].isPlaying;emitters[i].Pause();}else if(playing[i])emitters[i].Play();}}
  public void Stop(){foreach(var effect in emitters)if(effect!=null){effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);effect.gameObject.SetActive(false);}}
  public void Dispose(){Stop();foreach(var effect in emitters)if(effect!=null)Object.Destroy(effect.gameObject);}
 }
}
