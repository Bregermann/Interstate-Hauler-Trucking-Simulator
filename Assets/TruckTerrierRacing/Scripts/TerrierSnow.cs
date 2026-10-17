using UnityEngine;
using LWS.TruckTaxi;
using LWS.InterstateHauler;
using LWS.TruckSports;
namespace LWS.TruckTerrierRacing {
// Reuses Taxi's Weatherade accumulation/trace renderer; only supplies sport weather and real NWH contacts.
public sealed class TerrierSnow:MonoBehaviour {
 public TerrierMatch match;public TruckTaxiSnowSurface surface;public LwsWeatheradeAdapter adapter;public Texture2D snowTexture,detailTexture,sparkleTexture;public Shader[] retainedShaders;
 public float Coverage{get;private set;}readonly Vector3[] prior=new Vector3[48];readonly bool[] contact=new bool[48];float nextRefresh;bool initialized;
 void Start(){surface.ConfigureVendorTextures(snowTexture,detailTexture,sparkleTexture);surface.ConfigureDepthRenderer(1);surface.Initialize(new TruckTaxiSnowRegion(.3f),adapter);initialized=true;}
 public void ResetHeat(){var kind=match.course.weather.Kind;Coverage=kind==SportsWeatherKind.Snow||kind==SportsWeatherKind.Blizzard?.68f:0;System.Array.Clear(contact,0,contact.Length);if(initialized){surface.RefreshStreamedSurfaces();surface.SetCoverage(Coverage);}nextRefresh=0;}
 public void Step(float dt){if(!initialized)return;var kind=match.course.weather.Kind;bool falling=kind==SportsWeatherKind.Snow||kind==SportsWeatherKind.Blizzard;bool active=match.Phase==TerrierPhase.Countdown||match.Phase==TerrierPhase.Race;if(active)Coverage=Mathf.Clamp01(Coverage+dt*(falling?(kind==SportsWeatherKind.Blizzard?.075f:.043f):-.03f));surface.SetCoverage(Coverage);if(Time.time>=nextRefresh){surface.Refresh();nextRefresh=Time.time+.25f;}if(!active)StopTraces();}
 void LateUpdate(){if(!initialized||!match.Live||match.Phase!=TerrierPhase.Race||Coverage<.06f){StopTraces();return;}for(int i=0;i<12;i++)for(int w=0;w<4;w++){int index=i*4+w;var wheel=match.racers[i].truck.wheels[w];if(!match.racers[i].Alive||!wheel.IsGrounded||wheel.HitCollider==null||wheel.HitNormal.y<.25f){surface.StopWheelTrace(index);contact[index]=false;continue;}var point=wheel.HitPoint;if(!contact[index]||(point-prior[index]).sqrMagnitude>=9){prior[index]=point;contact[index]=true;surface.StopWheelTrace(index);continue;}if((point-prior[index]).sqrMagnitude<.0025f){surface.StopWheelTrace(index);continue;}surface.TraceWheel(index,prior[index],point,Mathf.Max(.16f,wheel.wheel.width),Coverage*.3f,true);prior[index]=point;}}
 public void StopTraces(){if(surface==null)return;for(int i=0;i<48;i++)surface.StopWheelTrace(i);}
 void OnDisable(){StopTraces();}
}
}
