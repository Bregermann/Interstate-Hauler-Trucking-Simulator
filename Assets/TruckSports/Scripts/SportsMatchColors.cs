using System;
using System.Collections.Generic;
using UnityEngine;
namespace LWS.TruckSports
{
 // Temporary FFA semantics only. Profiles, devices and appearance persistence
 // remain owned by their existing systems; this class never reads/writes prefs.
 public sealed class SportsMatchColors:IDisposable
 {
  public const int PaletteSize=12;
  public const float MinimumDistance=22;
  sealed class Assignment{public Color preferred,effective;}
  sealed class Suspension:IDisposable
  {
   SportsMatchColors owner;public Suspension(SportsMatchColors value){owner=value;}
   public void Dispose(){var value=owner;owner=null;value?.Resume();}
  }
  readonly ISportsParticipantSlot[] slots;readonly int count;readonly Dictionary<string,Assignment> assignments=new Dictionary<string,Assignment>(StringComparer.Ordinal);int depth;bool restored;
  // Okabe-Ito-inspired hues with additional distinct luminance/chroma choices.
  // Existing participant names/portraits remain secondary identity cues; this
  // palette does not claim universal color-vision or display certification.
  static readonly Color[] palette={Hex(0x0072B2),Hex(0xE69F00),Hex(0x009E73),Hex(0xD55E00),Hex(0xCC79A7),Hex(0x56B4E9),Hex(0xF0E442),Hex(0xF0F0F0),Hex(0x8866CC),Hex(0xAA3355),Hex(0x999933),Hex(0x44DDDD)};
  public bool Active=>!restored;
  public int Count=>count;
  public SportsMatchColors(ISportsParticipantSlot[] slots,int count)
  {
   if(slots==null||count<1||count>PaletteSize||count>slots.Length)throw new ArgumentOutOfRangeException(nameof(count));this.slots=slots;this.count=count;
   var ids=new List<string>(count);
   for(int i=0;i<count;i++){var slot=slots[i];if(slot==null||string.IsNullOrWhiteSpace(slot.ParticipantId)||assignments.ContainsKey(slot.ParticipantId))throw new ArgumentException("Match colors require distinct stable participant IDs.");assignments.Add(slot.ParticipantId,new Assignment{preferred=slot.ParticipantColor});ids.Add(slot.ParticipantId);}
   ids.Sort(StringComparer.Ordinal);var used=new List<Color>(count);bool complete=true;
   foreach(string id in ids)
   {
    var assignment=assignments[id];var preferred=Displayable(assignment.preferred);Color selected=preferred;float distance=Nearest(preferred,used);
    if(!Finite(assignment.preferred)||distance<MinimumDistance)selected=Farthest(used,out distance);
    assignment.effective=selected;used.Add(selected);if(distance<MinimumDistance)complete=false;
   }
   // Crowded arbitrary preferences can block two palette choices. A validated
   // complete palette is the bounded fallback, chosen before any slot is changed.
   if(!complete)for(int i=0;i<ids.Count;i++)assignments[ids[i]].effective=palette[i];ApplyEffective();
  }
  public Color PreferredColor(string id)=>assignments[id].preferred;
  public Color EffectiveColor(string id)=>assignments[id].effective;
  public static Color PaletteColor(int index)=>palette[index];
  public IDisposable Suspend()
  {
   if(restored)return new Suspension(null);if(depth++==0){CaptureExternalEdits();WritePreferred();}return new Suspension(this);
  }
  void Resume()
  {
   if(depth>0)depth--;if(restored||depth>0)return;
   foreach(var slot in slots)if(slot!=null&&slot.ParticipantId!=null&&assignments.TryGetValue(slot.ParticipantId,out var assignment))assignment.preferred=slot.ParticipantColor;
   // Profile refresh can reorder seats. Known identities keep exactly the same
   // effective color; a newly selected identity gets an unused active color.
   var used=new List<Color>(count);for(int i=0;i<count;i++)if(slots[i]!=null&&slots[i].ParticipantId!=null&&assignments.TryGetValue(slots[i].ParticipantId,out var known))used.Add(known.effective);
   for(int i=0;i<count;i++){var slot=slots[i];if(slot==null||string.IsNullOrWhiteSpace(slot.ParticipantId)||assignments.ContainsKey(slot.ParticipantId))continue;var color=Displayable(slot.ParticipantColor);if(!Finite(slot.ParticipantColor)||Nearest(color,used)<MinimumDistance)color=Farthest(used,out _);assignments.Add(slot.ParticipantId,new Assignment{preferred=slot.ParticipantColor,effective=color});used.Add(color);}
   ApplyEffective();
  }
  void CaptureExternalEdits(){foreach(var slot in slots)if(slot!=null&&slot.ParticipantId!=null&&assignments.TryGetValue(slot.ParticipantId,out var assignment)&&slot.ParticipantColor!=assignment.effective)assignment.preferred=slot.ParticipantColor;}
  void WritePreferred(){foreach(var slot in slots)if(slot!=null&&slot.ParticipantId!=null&&assignments.TryGetValue(slot.ParticipantId,out var assignment))slot.ParticipantColor=assignment.preferred;}
  void ApplyEffective(){for(int i=0;i<count;i++){var slot=slots[i];if(slot!=null&&slot.ParticipantId!=null&&assignments.TryGetValue(slot.ParticipantId,out var assignment))slot.ParticipantColor=assignment.effective;}}
  public void Restore(){if(restored)return;if(depth==0)CaptureExternalEdits();else foreach(var slot in slots)if(slot!=null&&slot.ParticipantId!=null&&assignments.TryGetValue(slot.ParticipantId,out var assignment))assignment.preferred=slot.ParticipantColor;WritePreferred();restored=true;}
  public void Dispose()=>Restore();
  static Color Hex(int rgb)=>new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,1);
  static bool Finite(Color color)=>float.IsFinite(color.r)&&float.IsFinite(color.g)&&float.IsFinite(color.b);
  static Color Displayable(Color color)=>Finite(color)?new Color(Mathf.Clamp01(color.r),Mathf.Clamp01(color.g),Mathf.Clamp01(color.b),1):Color.white;
  static Color Farthest(List<Color> used,out float distance){distance=-1;Color result=palette[0];foreach(var color in palette){float nearest=Nearest(color,used);if(nearest>distance){distance=nearest;result=color;}}return result;}
  static float Nearest(Color color,List<Color> used){float nearest=float.PositiveInfinity;foreach(var previous in used)nearest=Mathf.Min(nearest,Distance(color,previous));return nearest;}
  public static float Distance(Color a,Color b)=>Vector3.Distance(Lab(a),Lab(b));
  static float Linear(float value){value=Mathf.Clamp01(value);return value<=.04045f?value/12.92f:Mathf.Pow((value+.055f)/1.055f,2.4f);}
  static float Curve(float value)=>value>.008856f?Mathf.Pow(value,1f/3):7.787f*value+16f/116;
  static Vector3 Lab(Color color){float r=Linear(color.r),g=Linear(color.g),b=Linear(color.b);float x=Curve((r*.4124564f+g*.3575761f+b*.1804375f)/.95047f),y=Curve(r*.2126729f+g*.7151522f+b*.072175f),z=Curve((r*.0193339f+g*.119192f+b*.9503041f)/1.08883f);return new Vector3(116*y-16,500*(x-y),200*(y-z));}
 }
}
