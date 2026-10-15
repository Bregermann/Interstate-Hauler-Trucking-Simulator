using UnityEngine;
namespace LWS.TruckTerrierRacing {
// One bounded catch basin for actual off-course bodies. Its trigger never decides a race winner.
public sealed class TerrierDessert:MonoBehaviour {
 public TerrierMatch match;public Transform basin,cream;public ParticleSystem[] splashes;public AudioClip squish;public AudioSource sound;public float radiusX=98,radiusZ=155;public int Squishes{get;private set;}
 readonly bool[] caught=new bool[12];readonly float[] ages=new float[12];
 public Vector3 Local(Vector3 p)=>basin.InverseTransformPoint(p);
 public bool Inside(Vector3 p){p=Local(p);return p.x*p.x/(radiusX*radiusX)+p.z*p.z/(radiusZ*radiusZ)<.96f;}
 public static float Profile(float x,float z){float h=1.2f+1.1f*Mathf.Sin(x*.065f)*Mathf.Cos(z*.045f);for(int i=0;i<6;i++){float cx=(i%3-1)*48,cz=(i/3==0?-1:1)*65;float dx=x-cx,dz=z-cz;h+=10*Mathf.Exp(-(dx*dx+dz*dz)/400);}return h;}public float Height(Vector3 p){p=Local(p);return Profile(p.x,p.z);}
 public void ResetHeat(){System.Array.Clear(caught,0,12);System.Array.Clear(ages,0,12);Squishes=0;if(sound!=null)sound.Stop();foreach(var fx in splashes)fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
 public void Step(float dt){for(int i=0;i<12;i++){var r=match.racers[i];if(r.Finished)continue;var b=r.truck.body;var p=Local(b.worldCenterOfMass);bool inside=Inside(b.position);float top=Height(b.position);if(!caught[i]&&r.Alive&&r.OffCourse&&inside&&p.y<top+1){caught[i]=true;match.CreamLanding(i);Squishes++;ages[i]=0;var fx=splashes[i%splashes.Length];fx.transform.position=basin.TransformPoint(new Vector3(p.x,top,p.z));fx.Play();if(sound!=null&&squish!=null&&Application.isFocused){sound.volume=match.settings.sfx*.55f;sound.PlayOneShot(squish);}match.Pulse(i,.25f,.12f);}if(caught[i]){ages[i]+=dt;if(!b.isKinematic&&inside&&p.y<top+2){var damping=Vector3.ClampMagnitude(-b.linearVelocity*3.2f,48);b.AddForce(damping,ForceMode.Acceleration);b.AddTorque(Vector3.ClampMagnitude(-b.angularVelocity*2,16),ForceMode.Acceleration);}}}}
 public void Pause(bool paused){if(sound==null)return;if(paused)sound.Pause();else sound.UnPause();}public void StopAudio(){if(sound!=null)sound.Stop();}public void Stop(){if(sound!=null)sound.Stop();foreach(var fx in splashes)fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
}
}
