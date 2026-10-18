using UnityEngine;
namespace LWS.TruckTerrierRacing {
// Only a locked official winner collides with the dessert; collision, never proximity, confirms a catch.
public sealed class TerrierDessert:MonoBehaviour {
 public TerrierMatch match;public Transform basin,cream;public Collider winnerGate;public ParticleSystem[] splashes;public AudioClip squish;public AudioSource sound;public float radiusX=98,radiusZ=155;public int Squishes{get;private set;}
 readonly bool[] caught=new bool[12];Collider[] surfaces;
 public Vector3 Local(Vector3 p)=>basin.InverseTransformPoint(p);
 public bool Inside(Vector3 p){p=Local(p);return p.x*p.x/(radiusX*radiusX)+p.z*p.z/(radiusZ*radiusZ)<.96f;}
 public static float Profile(float x,float z){float h=1.2f+1.1f*Mathf.Sin(x*.065f)*Mathf.Cos(z*.045f);for(int i=0;i<6;i++){float cx=(i%3-1)*48,cz=(i/3==0?-1:1)*65;float dx=x-cx,dz=z-cz;h+=10*Mathf.Exp(-(dx*dx+dz*dz)/400);}return h;}
 public float Height(Vector3 p){p=Local(p);return Profile(p.x,p.z);}
 public void Align(){basin.SetPositionAndRotation(match.course.World(new Vector3(0,-38,match.course.Length+85)),match.course.Heading);if(winnerGate!=null)winnerGate.transform.localPosition=new Vector3(0,3,match.course.Length+1.4f);}
 public void ResetHeat(){System.Array.Clear(caught,0,12);Squishes=0;if(sound!=null)sound.Stop();foreach(var fx in splashes)fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);AuthorizeWinner(-1);}
 public void AuthorizeWinner(int winner){if(winnerGate!=null){winnerGate.enabled=winner>=0;var renderer=winnerGate.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=winner>=0;}if(surfaces==null)surfaces=basin.GetComponentsInChildren<Collider>(true);for(int i=0;i<12;i++)foreach(var shape in match.racers[i].truck.GetComponentsInChildren<Collider>(true)){foreach(var surface in surfaces)Physics.IgnoreCollision(shape,surface,i!=winner);if(winnerGate!=null)Physics.IgnoreCollision(shape,winnerGate,i==winner);}if(match.wrecks!=null)foreach(var piece in match.wrecks.pieces)IgnoreDebris(piece.shape);}
 public void IgnoreDebris(Collider shape){if(surfaces==null)surfaces=basin.GetComponentsInChildren<Collider>(true);foreach(var surface in surfaces)Physics.IgnoreCollision(shape,surface,true);}
 public void Contact(TerrierRacer racer,Collision collision){int slot=racer.slot;if(slot!=match.HeatWinner||!racer.Finished||!racer.Alive||caught[slot])return;caught[slot]=true;match.CreamLanding(slot);Squishes++;var fx=splashes[slot%splashes.Length];fx.transform.position=collision.contactCount>0?collision.GetContact(0).point:racer.truck.body.position;fx.Play();if(sound!=null&&squish!=null&&Application.isFocused){sound.volume=match.settings.sfx*.55f;sound.PlayOneShot(squish);}match.Pulse(slot,.25f,.12f);}
 public void Step(float dt){int slot=match.HeatWinner;if(slot<0)return;var racer=match.racers[slot];if(!racer.Finished||!racer.Alive)return;var b=racer.truck.body;if(b.isKinematic)return;if(caught[slot]){b.AddForce(Vector3.ClampMagnitude(-b.linearVelocity*3.2f,48),ForceMode.Acceleration);b.AddTorque(Vector3.ClampMagnitude(-b.angularVelocity*2,16),ForceMode.Acceleration);}else{var velocity=Vector3.ProjectOnPlane(b.linearVelocity,Vector3.up);b.AddForce(Vector3.ClampMagnitude(-velocity*.14f,6),ForceMode.Acceleration);}}
 public void Pause(bool paused){if(sound==null)return;if(paused)sound.Pause();else sound.UnPause();}public void StopAudio(){if(sound!=null)sound.Stop();}public void Stop(){if(sound!=null)sound.Stop();foreach(var fx in splashes)fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
}
}