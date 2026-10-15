using UnityEngine;
namespace LWS.TruckHockey {
// Measurements come from the preceding physics step; responses replace the solver's planar
// outcome rather than adding another full-strength collision impulse on top of it.
[DefaultExecutionOrder(-150)]public sealed class HockeyKnockback:MonoBehaviour {
 public HockeyMatch match;public float stoppedThreshold=.35f,minimumClosing=2.2f,maximumLaunchSpeed=25,protectionSeconds=.18f;
 struct Hit {public bool pending;public int a,b;public Vector3 av,bv,normal,point;public float closing;}
 readonly Hit[] hits=new Hit[36];readonly bool[] touching=new bool[36];readonly Vector3[] proposed=new Vector3[6];readonly int[] proposals=new int[6];readonly float[] protectedUntil=new float[6];
 public int StrongHits{get;private set;}public int Counters{get;private set;}
 static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
 public static bool Response(Vector3 av,Vector3 bv,Vector3 normal,float stopped,float minimum,float cap,out Vector3 a,out Vector3 b,out bool counter){
  av=Flat(av);bv=Flat(bv);normal=Flat(normal).normalized;a=av;b=bv;counter=false;float closing=Vector3.Dot(av-bv,normal);if(closing<minimum)return false;float sa=av.magnitude,sb=bv.magnitude;
  if(sa<=stopped||sb<=stopped){counter=true;if(sb<=stopped){a=av-normal*(Vector3.Dot(av,normal)+closing*1.35f);b=Vector3.ClampMagnitude(bv,.22f);}else{b=bv+normal*(-Vector3.Dot(bv,normal)+closing*1.35f);a=Vector3.ClampMagnitude(av,.22f);}}
  else if(Mathf.Abs(sa-sb)<1.1f||Mathf.Max(sa,sb)<Mathf.Min(sa,sb)*1.25f){a=av-normal*closing*1.05f;b=bv+normal*closing*1.05f;}
  else if(sa>sb){float kick=Mathf.Clamp(closing*.85f+(sa-sb)*1.25f,0,cap);b=bv+normal*kick;a=av-normal*closing*.38f;}
  else{float kick=Mathf.Clamp(closing*.85f+(sb-sa)*1.25f,0,cap);a=av-normal*kick;b=bv+normal*closing*.38f;}
  a=Vector3.ClampMagnitude(a,cap);b=Vector3.ClampMagnitude(b,cap);return true;
 }
 public void Record(HockeyTruck one,HockeyTruck two,Vector3 point,Vector3 surfaceNormal){if(one==null||two==null||one==two||one.match!=match||two.match!=match)return;int a=Mathf.Min(one.side,two.side),b=Mathf.Max(one.side,two.side),key=a*6+b;if(touching[key])return;touching[key]=true;if(Time.time<protectedUntil[a]||Time.time<protectedUntil[b])return;var ta=match.trucks[a];var tb=match.trucks[b];var n=Flat(one.side==a?-surfaceNormal:surfaceNormal).normalized;if(n.sqrMagnitude<.5f)return;float closing=Vector3.Dot(ta.IncomingVelocity-tb.IncomingVelocity,n);hits[key]=new Hit{pending=true,a=a,b=b,av=ta.IncomingVelocity,bv=tb.IncomingVelocity,normal=n,point=point,closing=closing};}
 public void Separate(int a,int b){touching[Mathf.Min(a,b)*6+Mathf.Max(a,b)]=false;}
 public void Forget(int slot){for(int i=0;i<6;i++){int key=Mathf.Min(i,slot)*6+Mathf.Max(i,slot);touching[key]=false;hits[key]=default;}protectedUntil[slot]=0;}
 void FixedUpdate(){if(!match.InMatch||!match.rules.Live||match.MenuOpen)return;System.Array.Clear(proposals,0,6);System.Array.Clear(proposed,0,6);
  for(int i=0;i<hits.Length;i++){var h=hits[i];if(!h.pending)continue;hits[i]=default;if(!Response(h.av,h.bv,h.normal,stoppedThreshold,minimumClosing,maximumLaunchSpeed,out var a,out var b,out bool counter))continue;proposed[h.a]+=a;proposed[h.b]+=b;proposals[h.a]++;proposals[h.b]++;StrongHits++;if(counter)Counters++;match.contacts?.Impact(h.point,h.closing*1.5f,Color.white);match.feedback.Play(3,h.closing);match.feedback.Pulse(match.players[h.a].controller?match.players[h.a].pad:null,Mathf.Clamp01(h.closing*.045f),.14f);match.feedback.Pulse(match.players[h.b].controller?match.players[h.b].pad:null,Mathf.Clamp01(h.closing*.045f),.14f);}
  for(int i=0;i<6;i++)if(proposals[i]>0){var t=match.trucks[i];var v=Vector3.ClampMagnitude(proposed[i]/proposals[i],maximumLaunchSpeed);v.y=t.body.linearVelocity.y;t.body.linearVelocity=v;t.KnockbackUntil=Time.time+1.15f;protectedUntil[i]=Time.time+protectionSeconds;}
 }
}
}
