using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckSkyTow
{
 // Presentation only: bounded pools reuse the owned Weatherade chip material.
 // The authored scoring frame/root never moves, scales or changes its collider.
 public sealed class SkyTowRingEffects:MonoBehaviour
 {
  sealed class Gate {public Transform root;public ParticleSystem particles;public LineRenderer line;public SkyTowRing ring;}
  readonly Gate[] upcoming=new Gate[12],success=new Gate[4];
  SkyTowMatch match;Material energy;Tween pulseTween;float pulse;bool running;
  public int PoolCount{get;private set;}public int SuccessfulBursts{get;private set;}
  public bool Running=>running;
  Gate CreateGate(bool burst)
  {
   var root=new GameObject(burst?"Pooled successful ring energy":"Pooled upcoming ring energy").transform;root.SetParent(transform,false);var particles=root.gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=particles.main;main.playOnAwake=false;main.loop=!burst;main.duration=1;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.startLifetime=burst?.8f:1.4f;main.startSpeed=burst?.5f:0;main.startSize=new ParticleSystem.MinMaxCurve(.055f,.12f);main.maxParticles=burst?96:48;main.gravityModifier=0;
   var emission=particles.emission;emission.rateOverTime=burst?0:16;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radiusThickness=0;
   var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;velocity.orbitalZ=new ParticleSystem.MinMaxCurve(burst?1.1f:.6f);
   var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.12f),new GradientAlphaKey(0,1)});var colors=particles.colorOverLifetime;colors.enabled=true;colors.color=fade;
   var trails=particles.trails;trails.enabled=true;trails.ratio=.5f;trails.lifetime=.3f;trails.minVertexDistance=.12f;trails.worldSpace=false;trails.dieWithParticles=true;
   var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=energy;renderer.trailMaterial=energy;renderer.maxParticleSize=.025f;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   LineRenderer line=null;if(!burst){var visual=new GameObject("Pulsating circular energy");visual.transform.SetParent(root,false);line=visual.AddComponent<LineRenderer>();line.sharedMaterial=energy;line.loop=true;line.useWorldSpace=false;line.positionCount=64;line.widthMultiplier=.06f;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;}
   root.gameObject.SetActive(false);PoolCount++;return new Gate{root=root,particles=particles,line=line};
  }
  public void Begin(SkyTowMatch owner)
  {
   Clear();match=owner;SuccessfulBursts=0;
   if(energy==null){Material source=null;if(owner.gateFeedback!=null)foreach(var renderer in owner.gateFeedback.GetComponentsInChildren<ParticleSystemRenderer>(true))if(renderer.sharedMaterial!=null){source=renderer.sharedMaterial;break;}if(source==null)return;energy=new Material(source){name="Sky Tow owned circular energy"};energy.SetColor("_BaseColor",new Color(1.15f,1.15f,1.15f,.8f));for(int i=0;i<upcoming.Length;i++)upcoming[i]=CreateGate(false);for(int i=0;i<success.Length;i++)success[i]=CreateGate(true);}
   foreach(var ring in owner.course.rings)
   {
    // Snow owns the frame's live material lifecycle. Its runtime clones can
    // expire on a forecast/rematch refresh; never cache or replace them here.
    // Ring MPBs supply outcome cues; the owned line/particle pools supply glow.
    ring.effects=this;
   }
   running=true;pulseTween=DOVirtual.Float(0,1,1.3f,value=>pulse=value).SetEase(Ease.InOutSine).SetLoops(-1,LoopType.Yoyo).SetLink(gameObject);Tick();
  }
  static Color ColorFor(SkyTowRing ring)=>ring.bonus?new Color(1,.62f,.12f):new Color(.18f,.85f,1);
  void Bind(Gate visual,SkyTowRing ring)
  {
   if(visual.ring==ring&&visual.root.gameObject.activeSelf)return;
   visual.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);visual.ring=ring;visual.root.gameObject.SetActive(true);visual.root.SetPositionAndRotation(ring.transform.position,ring.transform.rotation);visual.root.localScale=Vector3.one;var shape=visual.particles.shape;shape.radius=ring.radius+.35f;var main=visual.particles.main;main.startColor=ColorFor(ring);
   if(visual.line!=null){for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;visual.line.SetPosition(i,new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*(ring.radius+.35f));}visual.line.startColor=visual.line.endColor=ColorFor(ring);visual.particles.Play();}
  }
  public void Tick()
  {
   if(!running||match==null)return;
   foreach(var ring in match.course.rings)if(ring.gameObject.activeInHierarchy)ring.Animate(pulse);
   for(int slot=0;slot<4;slot++){int count=0;if(slot<match.setup.count&&!match.rules.finished[slot])for(int index=0;index<36&&count<3;index++)if(match.rules.outcomes[slot,index]==0){var gate=upcoming[slot*3+count++];Bind(gate,match.course.Ring(slot,index));gate.line.widthMultiplier=Mathf.Lerp(.04f,.1f,pulse);}while(count<3){var gate=upcoming[slot*3+count++];gate.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);gate.root.gameObject.SetActive(false);gate.ring=null;}}
  }
  public void Resolved(SkyTowRing ring,bool hit)
  {
   if(!running||!hit||ring.slot<0||ring.slot>=4)return;var visual=success[ring.slot];Bind(visual,ring);visual.root.DOKill();visual.root.localScale=Vector3.one;visual.root.DOPunchScale(Vector3.one*.08f,.55f,2,.4f).SetLink(visual.root.gameObject);visual.particles.Play();visual.particles.Emit(64);SuccessfulBursts++;
  }
  public void Clear()
  {
   running=false;pulseTween?.Kill();pulseTween=null;pulse=0;
   foreach(var visual in upcoming)if(visual!=null&&visual.root!=null&&visual.particles!=null){visual.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);visual.root.gameObject.SetActive(false);visual.ring=null;}
   foreach(var visual in success)if(visual!=null&&visual.root!=null&&visual.particles!=null){visual.root.DOKill();visual.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);visual.root.gameObject.SetActive(false);visual.ring=null;}
  }
  void OnDisable()=>Clear();
  void OnDestroy(){Clear();if(energy!=null)Destroy(energy);}
 }
}
