using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckDerby {
// Per-owner budget over the existing GR3D/NWH physical mirror camera placements and glass.
// No overlay approximation, shared render texture, or recursively rendered mirror surface.
public sealed class DerbyMirrors:MonoBehaviour {
 public Camera[] mirrors;public Renderer[] glass;public DerbyFighter owner;RenderTexture[] targets;Material[] materials;float next;bool showing;
 void Awake(){targets=new RenderTexture[mirrors.Length];materials=new Material[glass.Length];for(int i=0;i<mirrors.Length;i++){mirrors[i].enabled=false;mirrors[i].cullingMask&=~((1<<5)|(1<<8));mirrors[i].nearClipPlane=.15f;mirrors[i].farClipPlane=150;mirrors[i].allowHDR=false;mirrors[i].allowMSAA=false;}for(int i=0;i<glass.Length;i++){glass[i].gameObject.layer=8;materials[i]=new Material(Shader.Find("Universal Render Pipeline/Unlit"));glass[i].material=materials[i];}}
 public void Show(bool value){showing=value;if(!value)Release();}
 void LateUpdate(){if(!showing||owner==null||!owner.match.Live||!Application.isFocused&&!LWS.TruckSports.SportsProbeInputIsolation.Active){Release();return;}if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;for(int i=0;i<mirrors.Length;i++){if(targets[i]==null){targets[i]=new RenderTexture(160,96,16,RenderTextureFormat.ARGB32){name="Owned Derby rear mirror "+owner.slot+" "+i};targets[i].Create();mirrors[i].targetTexture=targets[i];materials[Mathf.Min(i,materials.Length-1)].SetTexture("_BaseMap",targets[i]);}mirrors[i].Render();}}
 public void Release(){if(targets==null)return;for(int i=0;i<targets.Length;i++)if(targets[i]!=null){mirrors[i].targetTexture=null;targets[i].Release();Destroy(targets[i]);targets[i]=null;}}
 void OnDisable(){Release();}void OnDestroy(){Release();if(materials!=null)foreach(var m in materials)if(m!=null)Destroy(m);}
}}
