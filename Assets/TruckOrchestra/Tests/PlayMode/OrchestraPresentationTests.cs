#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LWS.TruckOrchestra.Tests
{
    public sealed class OrchestraPresentationTests
    {
        const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static Type Runtime(string n)=>Type.GetType(n+", Assembly-CSharp",true);
        static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
        sealed class Fixture:IDisposable
        {
            readonly IDisposable prefs;readonly InputSettings original,scoped;readonly GameObject isolation;public Component match;
            public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"OrchestraPresentationV2"});original=InputSystem.settings;scoped=UnityEngine.Object.Instantiate(original);scoped.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;scoped.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=scoped;isolation=new GameObject("Orchestra presentation input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
            public IEnumerator Load(){EditorSceneManager.LoadSceneInPlayMode("Assets/TruckOrchestra/Scenes/TruckOrchestra.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckOrchestra.OrchestraMatch"));Assert.That(match,Is.Not.Null);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;}
            public void Dispose(){Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(scoped);prefs.Dispose();}
        }
        static IEnumerator Until(Func<bool> predicate,float seconds,string label){float end=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(predicate(),Is.True,label);}
        static Bounds BoundsOf(Renderer[] renderers){Assert.That(renderers.Length,Is.GreaterThan(0));var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
        static Rect Project(Camera camera,Bounds b,string label)
        {
            Vector2 lo=Vector2.one*float.MaxValue,hi=Vector2.one*float.MinValue;
            for(int i=0;i<8;i++){Vector3 p=camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));Assert.That(p.z,Is.GreaterThan(camera.nearClipPlane),label+" behind camera");lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p);}
            Assert.That(lo.x,Is.GreaterThanOrEqualTo(.005f),label+" left clipped");Assert.That(lo.y,Is.GreaterThanOrEqualTo(.005f),label+" bottom clipped");Assert.That(hi.x,Is.LessThanOrEqualTo(.995f),label+" right clipped");Assert.That(hi.y,Is.LessThanOrEqualTo(.995f),label+" top clipped");return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
        }
        static float Bend(Component c,string side)
        {
            var shoulder=(Transform)Get(c,side+"Arm");var elbow=(Transform)Get(c,side+"Forearm");var hand=(Transform)Get(c,side+"Hand");
            return 180-Vector3.Angle(shoulder.position-elbow.position,hand.position-elbow.position);
        }
        static Color32[] RenderPixels(Camera camera,int width,int height,Action inspect)
        {
            var old=camera.targetTexture;float aspect=camera.aspect;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);Texture2D pixels=null;
            try{camera.targetTexture=rt;camera.aspect=width/(float)height;camera.Render();inspect?.Invoke();RenderTexture.active=rt;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();return pixels.GetPixels32();}
            finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(pixels!=null)UnityEngine.Object.Destroy(pixels);}
        }
        [UnityTest,Timeout(120000)]public IEnumerator OriginalAiShowsRealSkinnedBentElbowsVisibleConductorAndFourSupportedStationsAt720And1080()
        {
            using(var f=new Fixture())
            {
                yield return f.Load();yield return Until(()=>(int)Get(f.match,"AudibleNotes")>8,25,"Original AI must sound real accepted notes before visual QA");
                var c=(Component)Get(f.match,"conductor");var skin=c.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(s=>s.sharedMesh.vertexCount).First();var mesh=new Mesh();
                try
                {
                    skin.BakeMesh(mesh);var first=mesh.vertices;double time=(double)Get(f.match,"SongTime");yield return Until(()=>(double)Get(f.match,"SongTime")>time+.35,3,"Real DSP clock must advance");skin.BakeMesh(mesh);var next=mesh.vertices;float max=first.Zip(next,(a,b)=>Vector3.Distance(a,b)).Max();Assert.That(max,Is.GreaterThan(.01f),"Actual skinned vertices must move, not only a beat or bone counter");
                }
                finally{UnityEngine.Object.Destroy(mesh);}
                for(int frame=0;frame<12;frame++){Assert.That(Bend(c,"left"),Is.InRange(35f,130f),"Actual left elbow must be visibly bent");Assert.That(Bend(c,"right"),Is.InRange(35f,130f),"Actual right elbow must be visibly bent");yield return null;}
                // Controlled still freezes only the mode update; keep the real Play HUD.
                // Opening the pause menu would conceal the very action we must inspect.
                ((Behaviour)f.match).enabled=false;yield return null;yield return null;yield return new WaitForSecondsRealtime(.18f);
                var camera=(Camera)Get(f.match,"stageCamera");var renderers=c.GetComponentsInChildren<SkinnedMeshRenderer>(true);var states=renderers.Select(r=>r.enabled).ToArray();var stations=((Array)Get(f.match,"instruments")).Cast<Component>().ToArray();
                var title=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="Concert venue original enamel title");
                foreach(int width in new[]{1280,1920})
                {
                    int height=width==1280?720:1080;Rect conductorRect=default;
                    var visible=RenderPixels(camera,width,height,()=>{conductorRect=Project(camera,BoundsOf(renderers),"Actual conductor");for(int i=0;i<4;i++)Project(camera,BoundsOf(stations[i].GetComponentsInChildren<Renderer>(true)),"Actual station "+i);var tr=Project(camera,title.GetComponent<Renderer>().bounds,"Original world venue title");Assert.That(tr.height*height,Is.LessThanOrEqualTo(36.1f));});
                    Color32[] hidden;
                    try{foreach(var renderer in renderers)renderer.enabled=false;hidden=RenderPixels(camera,width,height,null);}
                    finally{for(int i=0;i<renderers.Length;i++)renderers[i].enabled=states[i];}
                    int changed=0;for(int y=Mathf.Max(0,(int)(conductorRect.yMin*height));y<Mathf.Min(height,Mathf.CeilToInt(conductorRect.yMax*height));y++)for(int x=Mathf.Max(0,(int)(conductorRect.xMin*width));x<Mathf.Min(width,Mathf.CeilToInt(conductorRect.xMax*width));x++){int i=y*width+x;if(Mathf.Abs(visible[i].r-hidden[i].r)+Mathf.Abs(visible[i].g-hidden[i].g)+Mathf.Abs(visible[i].b-hidden[i].b)>18)changed++;}
                    Assert.That(changed,Is.GreaterThan(80),"Actual conductor must contribute visible GPU pixels at "+width);
                    string path=Path.GetFullPath("Logs/TruckSports/Impossible07/OrchestraPresentationV2/concert-bent-visible-"+width+"-controlled.png");
                    Action<Camera> inspect=ui=>{var strips=(Array)Get(Get(f.match,"rhythmHud"),"strips");foreach(var strip in strips){var rect=(RectTransform)Get(strip,"root");if(!rect.gameObject.activeInHierarchy)continue;var corners=new Vector3[4];rect.GetWorldCorners(corners);float top=corners.Max(p=>ui.WorldToViewportPoint(p).y);Assert.That(conductorRect.yMin,Is.GreaterThan(top+.005f),"Conductor body must remain above actual rendered timing panels");}};
                    Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5).Invoke(null,new object[]{f.match,width,height,path,inspect});
                    TestContext.WriteLine("ORCHESTRA_RENDERED width="+width+" conductorPixels="+changed+" rect="+conductorRect+" leftBend="+Bend(c,"left")+" rightBend="+Bend(c,"right"));
                }
            }
        }
        [UnityTest,Timeout(90000)]public IEnumerator OriginalAiStrikersApproachFixedSkinsAndBarsWithConnectedSupportsThenPauseAndRematchReset()
        {
            using(var f=new Fixture())
            {
                yield return f.Load();var instruments=((Array)Get(f.match,"instruments")).Cast<Component>().ToArray();var tips=new Transform[8];var targets=new Transform[8];
                for(int group=0;group<2;group++)for(int lane=0;lane<4;lane++){int i=group*4+lane;tips[i]=((Transform[])Get(instruments[group==0?1:3],"strikeTips"))[lane];targets[i]=((Transform[])Get(instruments[group==0?1:3],"strikeTargets"))[lane];Assert.That(tips[i],Is.Not.Null);Assert.That(targets[i],Is.Not.Null);Assert.That(Vector3.Distance(tips[i].position,targets[i].position),Is.GreaterThan(.3f),"Original reset mallet must be clear of its surface");}
                var fixedPoints=targets.Select(t=>t.position).ToArray();var closest=Enumerable.Repeat(float.MaxValue,8).ToArray();float end=Time.realtimeSinceStartup+35;
                while(Time.realtimeSinceStartup<end&&closest.Any(d=>d>.2f)){for(int i=0;i<8;i++){closest[i]=Mathf.Min(closest[i],Vector3.Distance(tips[i].position,targets[i].position));Assert.That(Vector3.Distance(fixedPoints[i],targets[i].position),Is.LessThan(1e-5f),"Drum skins/marimba bars stay fixed while mallets strike independently");}yield return null;}
                Assert.That(closest.All(d=>d<.2f),Is.True,"All actual note-driven mallets must approach their fixed playing surfaces: "+string.Join(",",closest));Assert.That((int)Get(f.match,"AudibleNotes"),Is.GreaterThan(8));
                Call(f.match,"Pause",0,false);yield return null;var roots=instruments.SelectMany(i=>(Transform[])Get(i,"movingParts")).ToArray();var poses=roots.Select(t=>t.localRotation).ToArray();var positions=roots.Select(t=>t.localPosition).ToArray();double before=(double)Get(f.match,"SongTime");yield return new WaitForSecondsRealtime(.25f);Assert.That((double)Get(f.match,"SongTime"),Is.EqualTo(before));CollectionAssert.AreEqual(poses,roots.Select(t=>t.localRotation).ToArray());CollectionAssert.AreEqual(positions,roots.Select(t=>t.localPosition).ToArray());
                Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Assert.That((int)Get(f.match,"AudibleNotes"),Is.Zero);for(int i=0;i<8;i++)Assert.That(Vector3.Distance(tips[i].position,targets[i].position),Is.GreaterThan(.3f));
                TestContext.WriteLine("ORCHESTRA_RELATIVE_STRIKERS closest="+string.Join(",",closest)+"; original sound/timing authority, no note or score injection; pause/rematch exact");
            }
        }
    }
}
#endif
