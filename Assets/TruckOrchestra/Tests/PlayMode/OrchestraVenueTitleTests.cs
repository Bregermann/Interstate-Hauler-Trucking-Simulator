#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace LWS.TruckOrchestra.Tests
{
 public sealed class OrchestraVenueTitleTests
 {
  const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  static object Get(object x,string n)=>x.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(x):x.GetType().GetProperty(n,Any).GetValue(x);
  static object[] Style(TextMesh t)=>new object[]{t.text,t.font,t.characterSize,t.fontSize,t.fontStyle,t.anchor,t.alignment,t.lineSpacing,t.tabSize,t.richText,t.color,t.offsetZ};
  static Rect WorldRect(Camera camera,Bounds bounds)
  {
   Vector2 lo=Vector2.one*float.MaxValue,hi=Vector2.one*float.MinValue;for(int i=0;i<8;i++){Vector3 p=camera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));Assert.That(p.z,Is.GreaterThan(camera.nearClipPlane));lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p);}Assert.That(lo.x,Is.GreaterThan(.005f));Assert.That(lo.y,Is.GreaterThan(.005f));Assert.That(hi.x,Is.LessThan(.995f));Assert.That(hi.y,Is.LessThan(.995f));return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
  }
  static Rect UiRect(Camera ui,RectTransform rect)
  {
   var corners=new Vector3[4];rect.GetWorldCorners(corners);var points=corners.Select(p=>ui.WorldToViewportPoint(p)).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
  }
  static Color32[] Render(Camera camera,int width,int height,Action inspect)
  {
   var old=camera.targetTexture;float aspect=camera.aspect;var active=RenderTexture.active;var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);Texture2D pixels=null;
   try{camera.targetTexture=target;camera.aspect=width/(float)height;camera.Render();if(inspect!=null)inspect();RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();return pixels.GetPixels32();}
   finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
  }
  [UnityTest,Timeout(90000)]public IEnumerator OriginalAiVenueTitleIsVisibleInCurtainAndClearOfActualSongRoundAndProgressAt720And1080()
  {
   // Same original actual scene/input/prefs/AI start as the unchanged strict
   // conductor/all-four-station/audio tests. No song/pose/score injection.
   var type=typeof(OrchestraPresentationTests).GetNestedType("Fixture",BindingFlags.NonPublic);var fixture=(IDisposable)Activator.CreateInstance(type,true);
   using(fixture)
   {
    yield return (IEnumerator)type.GetMethod("Load",Any).Invoke(fixture,null);var match=(Component)Get(fixture,"match");float end=Time.realtimeSinceStartup+25;while((int)Get(match,"AudibleNotes")<=8&&Time.realtimeSinceStartup<end)yield return null;Assert.That((int)Get(match,"AudibleNotes"),Is.GreaterThan(8),"Original AI must sound actual accepted notes before title/HUD inspection");Assert.That(Get(match,"Phase").ToString(),Is.EqualTo("Song"));
    ((Behaviour)match).enabled=false;yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);
    var camera=(Camera)Get(match,"stageCamera");var title=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="Concert venue original enamel title");var renderer=title.GetComponent<MeshRenderer>();var style=Style(title);var material=renderer.sharedMaterial;Assert.That(title.text,Is.EqualTo("THE SIX-TON SYMPHONY"));Assert.That(material.shader.name,Is.Not.EqualTo("GUI/Text Shader"),"Existing actual depth-tested font shader stays authoritative");
    var hud=Get(match,"rhythmHud");var protectedUi=new[]{(RectTransform)((Component)Get(hud,"song")).transform,(RectTransform)((Component)Get(hud,"time")).transform,(RectTransform)Get(hud,"progress")};string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/OrchestraVenueTitleV5/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);
    foreach(int width in new[]{1280,1920})
    {
     int height=width==1280?720:1080;Rect titleRect=default;var shown=Render(camera,width,height,()=>titleRect=WorldRect(camera,renderer.bounds));Color32[] hidden;try{renderer.enabled=false;hidden=Render(camera,width,height,null);}finally{renderer.enabled=true;}
     int changed=0;for(int y=Mathf.Max(0,(int)(titleRect.yMin*height));y<Mathf.Min(height,Mathf.CeilToInt(titleRect.yMax*height));y++)for(int x=Mathf.Max(0,(int)(titleRect.xMin*width));x<Mathf.Min(width,Mathf.CeilToInt(titleRect.xMax*width));x++){int i=y*width+x;if(Mathf.Abs(shown[i].r-hidden[i].r)+Mathf.Abs(shown[i].g-hidden[i].g)+Mathf.Abs(shown[i].b-hidden[i].b)>18)changed++;}
     Assert.That(changed,Is.GreaterThan(80),"Existing actual venue glyphs must visibly contribute GPU pixels in the curtain");Assert.That(1-titleRect.center.y,Is.InRange(.22f,.29f),"First natural concert wide shot should place world title in blank curtain around250-300px at1080, proportionally at720");Assert.That(titleRect.height*height,Is.LessThanOrEqualTo(36.1f));
     Action<Camera> inspect=ui=>{foreach(var rect in protectedUi){Assert.That(rect.gameObject.activeInHierarchy,Is.True);Rect area=UiRect(ui,rect);area=Rect.MinMaxRect(area.xMin-8f/width,area.yMin-8f/height,area.xMax+8f/width,area.yMax+8f/height);Assert.That(area.Overlaps(titleRect),Is.False,"Actual world title must clear original rendered "+rect.name+" by8capturepixels");TestContext.WriteLine("ORCHESTRA_TITLE_HUD width="+width+" title="+titleRect+" protected="+rect.name+" expandedRect="+area);}};
     string path=Path.Combine(folder,"venue-title-curtain-"+width+"-controlled.png");Type.GetType("LWS.TruckSports.SportsControlledCapture, Assembly-CSharp",true).GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5).Invoke(null,new object[]{match,width,height,path,inspect});Assert.That(File.Exists(path),Is.True);TestContext.WriteLine("ORCHESTRA_TITLE_VISIBLE width="+width+" glyphPixels="+changed+" titleRect="+titleRect+" path="+path+"; native controlled rendering, not Windows/human/listening acceptance");
    }
    CollectionAssert.AreEqual(style,Style(title));Assert.That(renderer.sharedMaterial,Is.EqualTo(material),"Existing real font material remains untouched");
   }
  }
 }
}
#endif
