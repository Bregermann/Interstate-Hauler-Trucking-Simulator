#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using LWS.TruckFashionShow;
using LWS.TruckDanceOff;
public sealed class FashionPlayModeTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings original,temporary;readonly GameObject isolation;public Behaviour match;
        public Array Trucks=>(Array)Get(match,"trucks");public Array Walkers=>(Array)Get(match,"walkers");public FashionCompetition Competition=>(FashionCompetition)Get(match,"Competition");
        public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"Fashion07Native"});original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;isolation=new GameObject("Fashion original input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
        public IEnumerator Load(){Assert.That(File.Exists("Assets/TruckFashionShow/Scenes/TruckFashionShow.unity"),Is.True,"Lead must run explicit NEW scene author and bind existing actual four appearance hosts.");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFashionShow/Scenes/TruckFashionShow.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckFashionShow.FashionMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;}
        public Rigidbody Body(int i)=>(Rigidbody)Get(Trucks.GetValue(i),"body");
        public void Dispose(){Time.timeScale=1;if(match!=null){if((bool)Get(match,"WardrobeOpen"))Call(Get(match,"wardrobe"),"Close",false);match.enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(temporary);prefs.Dispose();}
    }
    static IEnumerator Until(Fixture f,Func<bool> test,float seconds,string label){float end=Time.realtimeSinceStartup+seconds;while(!test()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(test(),Is.True,label+" phase="+Get(f.match,"Phase")+" clock="+Get(f.match,"Clock")+" paused="+Get(f.match,"Paused"));}
    static IEnumerator Sim(Fixture f,float seconds){float target=(float)Get(f.match,"Clock")+seconds;yield return Until(f,()=>(float)Get(f.match,"Clock")>=target,Mathf.Max(6,seconds*3),"Original fixed-step Fashion clock must progress");}
    static void Capture(Fixture f,string name){string dir=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07Fashion");Directory.CreateDirectory(dir);var canvas=((Component)Get(f.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(f.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);foreach(int w in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,w,w==1280?720:1080,Path.Combine(dir,name+"-"+w+"-controlled.png"),null});}
    static void Keys(Keyboard keyboard,params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    static void Owners(object match,int humans){var players=(Array)Get(Get(match,"setup"),"players");Key[] forward={Key.W,Key.UpArrow,Key.T,Key.I},left={Key.A,Key.LeftArrow,Key.F,Key.J},back={Key.S,Key.DownArrow,Key.G,Key.K},right={Key.D,Key.RightArrow,Key.H,Key.L};for(int i=0;i<4;i++){var p=players.GetValue(i);var source=p.GetType().GetField("source");source.SetValue(p,Enum.Parse(source.FieldType,i<humans?"Keyboard":"AI"));p.GetType().GetField("forward").SetValue(p,forward[i]);p.GetType().GetField("left").SetValue(p,left[i]);p.GetType().GetField("back").SetValue(p,back[i]);p.GetType().GetField("right").SetValue(p,right[i]);}match.GetType().GetField("actionKeys").SetValue(match,new[]{Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp});}
    [UnityTest,Timeout(540000)]public IEnumerator NaturalFourAiShopEquipRealItemsWalkRunwayPerformQtesAndJudgeOnce()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var ids=Enumerable.Range(0,4).Select(i=>f.Body(i).GetInstanceID()).ToArray();float deadline=Time.realtimeSinceStartup+450,next=0;bool captured=false;
            while(!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.realtimeSinceStartup>=next){Debug.Log("FASHION_NATURAL clock="+Get(f.match,"Clock")+" phase="+Get(f.match,"Phase")+" active="+Get(f.match,"ActiveOwner")+" ready="+string.Join(",",(bool[])Get(f.match,"OutfitReady"))+" positions="+string.Join(";",Enumerable.Range(0,4).Select(i=>f.Body(i).position.ToString("F4")))+" judged="+f.Competition.Completed);next=Time.realtimeSinceStartup+10;}
                if(!captured&&Get(f.match,"Phase").ToString()=="Performance"&&((DanceRules)Get(f.match,"Timing")).Resolved>=2){Capture(f,"natural-real-outfit-QTE-runway");captured=true;}yield return null;
            }
            Assert.That((bool)Get(f.match,"Finished"),Is.True,"Original AI must complete all four genuine shopping/entry/performance/judge/exit turns.");Assert.That(f.Competition.Complete,Is.True);Assert.That((int)Get(f.match,"NaturalRunwayEntries"),Is.EqualTo(4));Assert.That((int)Get(f.match,"CompletedWalks"),Is.EqualTo(4));Assert.That(f.Competition.Grades.All(g=>g.qte>0&&g.poses>0&&g.completion==100),Is.True);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(4));CollectionAssert.AreEqual(ids,Enumerable.Range(0,4).Select(i=>f.Body(i).GetInstanceID()).ToArray());
            for(int i=0;i<4;i++){Assert.That(((Array)Call(f.match,"ActualOutfit",i)).Length,Is.GreaterThanOrEqualTo(5));Assert.That((float)Get(f.Walkers.GetValue(i),"Distance"),Is.GreaterThan(50));Assert.That((int)Get(f.Walkers.GetValue(i),"PoseChanges"),Is.GreaterThan(0));}
            var people=Get(f.match,"people");Assert.That((int)Get(people,"Reactions"),Is.GreaterThan(12));var judges=(Animator[])Get(people,"judges");Assert.That(judges,Has.Length.EqualTo(3));Assert.That(judges.All(a=>a.avatar!=null&&a.avatar.isHuman&&a.GetCurrentAnimatorClipInfo(0).Any(c=>c.weight>0&&c.clip.humanMotion)),Is.True,"All real judges must evaluate actual owned Human clips.");Capture(f,"natural-all-four-transparent-score-results");
        }
    }
    [UnityTest,Timeout(150000)]public IEnumerator OneThroughFourOwnersDriveOriginalWholeAssemblyWithActualBounceAndIndependentSources()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
        {
            var originalIds=Enumerable.Range(0,4).Select(i=>f.Body(i).GetInstanceID()).ToArray();
            for(int n=1;n<=4;n++)
            {
                Keys(keyboard);Call(f.match,"SetupScreen");Owners(f.match,n);Assert.That((bool)Call(f.match,"StartMatch"),Is.True,"Disjoint actual configured keyboard clusters validate");yield return null;yield return null;var sourceAssembly=((Component)f.Trucks.GetValue(0)).GetComponentsInChildren<Rigidbody>(true);var cab=sourceAssembly.Single(b=>b!=f.Body(0));Vector3 relativeCab=Quaternion.Inverse(f.Body(0).rotation)*(cab.position-f.Body(0).position);Vector3 start=f.Body(0).position;float yMin=start.y,yMax=start.y;Keys(keyboard,Key.W);float clock=(float)Get(f.match,"Clock")+2,deadline=Time.realtimeSinceStartup+7;
                while((float)Get(f.match,"Clock")<clock&&Time.realtimeSinceStartup<deadline){yMin=Mathf.Min(yMin,f.Body(0).position.y);yMax=Mathf.Max(yMax,f.Body(0).position.y);yield return null;}Keys(keyboard);Assert.That((float)Get(f.match,"Clock"),Is.GreaterThanOrEqualTo(clock));Assert.That(f.Body(0).position.z-start.z,Is.GreaterThan(3));Assert.That(yMax-yMin,Is.GreaterThan(.04f),"Actual native root must bounce, not only a text counter.");
                var assembly=((Component)f.Trucks.GetValue(0)).GetComponentsInChildren<Rigidbody>(true);Assert.That(assembly,Has.Length.EqualTo(2));Assert.That(assembly.All(b=>b.isKinematic),Is.True,"Fashion's deliberate choreography owns each original body; no dynamic Cab lag.");Assert.That(Vector3.Distance(relativeCab,Quaternion.Inverse(f.Body(0).rotation)*(cab.position-f.Body(0).position)),Is.LessThan(1e-4f),"Original complete Cab/chassis relative physical pose remains intact during strut.");CollectionAssert.AreEqual(originalIds,Enumerable.Range(0,4).Select(i=>f.Body(i).GetInstanceID()).ToArray());Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Capture(f,"assigned-"+n+"-whole-original-rig-strut");
            }
        }finally{InputSystem.RemoveDevice(keyboard);}}
    }
    [UnityTest,Timeout(180000)]public IEnumerator ActualFreeCustomizerPreviewCancelSavePersistsOnlyNamedOwnerAndReturnsToHub()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
        {
            Call(f.match,"SetupScreen");Owners(f.match,1);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return null;yield return null;Keys(keyboard,Key.A);yield return Sim(f,.46f);Keys(keyboard,Key.W);
            var shops=(Transform[])Get(f.match,"shops");yield return Until(f,()=>Vector3.ProjectOnPlane((Vector3)Get(f.Walkers.GetValue(0),"Position")-shops[0].position,Vector3.up).magnitude<6.5f,15,"Actual assigned input drives to the real hat boutique");Keys(keyboard);yield return null;
            var players=(Array)Get(Get(f.match,"setup"),"players");string[] owner=Enumerable.Range(0,4).Select(i=>(string)Get(players.GetValue(i),"participantId")).ToArray();var registry=Runtime("LWS.TruckSports.SportsParticipantRegistry");Func<string,string> saved=id=>JsonUtility.ToJson(registry.GetMethod("GetAppearance").Invoke(null,new object[]{id}));string[] before=owner.Select(saved).ToArray();Assert.That((bool)Call(f.match,"OpenDressing",0),Is.True);yield return null;var wardrobe=Get(f.match,"wardrobe");Assert.That(f.match.enabled,Is.False);Assert.That(((Behaviour)Get(f.match,"menus")).enabled,Is.False);var categories=(Array)Get(wardrobe,"categories");Call(Get(categories.GetValue(1),"onClick"),"Invoke");var cards=(Array)Get(wardrobe,"cards");Assert.That(cards.Cast<object>().All(c=>!(bool)Get(c,"enablePrice")&&!(bool)Get(c,"useModalWindow")),Is.True,"Existing free test cards display no currency or payment prerequisites.");Call(Get(cards.GetValue(0),"onClick"),"Invoke");yield return null;yield return null;Assert.That(Get(Get(wardrobe,"previewImage"),"texture"),Is.Not.Null,"Existing actual RT preview must show catalog composition");var preview=Get(wardrobe,"preview");var previewCamera=(Camera)Get(preview,"camera");Vector3 eye=previewCamera.transform.position;Call(Get(Get(wardrobe,"rotateLeft"),"onClick"),"Invoke");Assert.That(Vector3.Distance(eye,previewCamera.transform.position),Is.GreaterThan(.1f),"Existing dressing preview really rotates its camera.");var rt=(RenderTexture)Get(Get(wardrobe,"previewImage"),"texture");var oldTarget=RenderTexture.active;var pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);try{RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();var data=pixels.GetPixels();float lo=data.Min(c=>c.grayscale),hi=data.Max(c=>c.grayscale);Assert.That(hi-lo,Is.GreaterThan(.02f),"Actual existing customizer GPU target contains visible composed content, not a blank RT.");}finally{RenderTexture.active=oldTarget;UnityEngine.Object.DestroyImmediate(pixels);}Call(wardrobe,"Close",false);yield return null;Assert.That(saved(owner[0]),Is.EqualTo(before[0]),"Cancel never persists a preview.");Assert.That(f.match.enabled,Is.True);
            Assert.That((bool)Call(f.match,"OpenDressing",0),Is.True);yield return null;wardrobe=Get(f.match,"wardrobe");categories=(Array)Get(wardrobe,"categories");Call(Get(categories.GetValue(1),"onClick"),"Invoke");cards=(Array)Get(wardrobe,"cards");Call(Get(cards.GetValue(0),"onClick"),"Invoke");Call(wardrobe,"Close",true);yield return null;registry.GetMethod("ForgetCache").Invoke(null,null);Assert.That(saved(owner[0]),Is.Not.EqualTo(before[0]),"Real free equipped hat persists through original registry reload.");for(int i=1;i<4;i++)Assert.That(saved(owner[i]),Is.EqualTo(before[i]),"Another stable participant's outfit must not be overwritten by this shop.");Assert.That((int)Get(f.match,"WardrobeOwner"),Is.EqualTo(-1));Assert.That((bool)Get(f.match,"WardrobeOpen"),Is.False);Assert.That(f.match.enabled,Is.True);Assert.That(((Behaviour)Get(f.match,"menus")).enabled,Is.True);Assert.That(Get(f.match,"Phase").ToString(),Is.EqualTo("Hub"));Capture(f,"actual-free-hat-saved-return-to-hub");
        }finally{InputSystem.RemoveDevice(keyboard);}}
    }
    [UnityTest,Timeout(150000)]public IEnumerator AssignedHumanActuallyEntersRunwayAndTimesOriginalDanceInputWithoutAwardingWatchers()
    {
        using(var f=new Fixture()){yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();try
        {
            Call(f.match,"SetupScreen");Owners(f.match,1);Assert.That((bool)Call(f.match,"StartMatch"),Is.True);yield return null;yield return null;Assert.That((bool)Call(f.match,"ReadyOutfit",0),Is.True,"Same actual ready interaction enters the competition without injecting score.");
            yield return Until(f,()=>Get(f.match,"Phase").ToString()=="Entrance",75,"Original AI shop and actual lineup must finish before the human's own turn");yield return null;yield return null;Keys(keyboard,Key.D);yield return Sim(f,.47f);Keys(keyboard,Key.W);yield return Until(f,()=>Get(f.match,"Phase").ToString()=="Performance",18,"Actual assigned keyboard walks the full original truck into the real runway entrance");Keys(keyboard);yield return Sim(f,.2f);
            yield return Until(f,()=>(float)Get(f.match,"PerformanceTime")>=1.97f,5,"Actual original first pose timestamp");Keys(keyboard,Key.A);yield return Sim(f,.1f);Keys(keyboard);yield return Sim(f,.1f);
            var timing=(DanceRules)Get(f.match,"Timing");Assert.That(timing.Perfect+timing.Great,Is.GreaterThanOrEqualTo(1),"Real configured left-key InputAction must be judged by original timestamp-based DanceRules.");Assert.That(timing.Score,Is.GreaterThanOrEqualTo(700));Assert.That((int)Get(f.Walkers.GetValue(0),"PoseChanges"),Is.GreaterThan(0));Assert.That(Enumerable.Range(1,3).All(i=>(int)Get(f.Walkers.GetValue(i),"PoseChanges")==0),Is.True,"Only the actual current performer owns this input and pose.");Assert.That(f.Competition.Completed,Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero,"A single pose does not fabricate a finished runway grade.");Capture(f,"actual-human-original-router-successful-pose");
        }finally{InputSystem.RemoveDevice(keyboard);}}
    }
    [UnityTest,Timeout(150000)]public IEnumerator ActualRunwayPauseFreezesQteClockPosesAndRematchRetainsSavedOutfits()
    {
        using(var f=new Fixture()){yield return f.Load();yield return Until(f,()=>Get(f.match,"Phase").ToString()=="Performance"&&(float)Get(f.match,"PerformanceTime")>5,100,"Original AI must reach an actual judged performance before pausing");Call(f.match,"Pause",0,false);yield return null;var body=Enumerable.Range(0,4).Select(i=>f.Body(i).position).ToArray();float time=(float)Get(f.match,"PerformanceTime");int score=((DanceRules)Get(f.match,"Timing")).Score;yield return new WaitForSecondsRealtime(.2f);CollectionAssert.AreEqual(body,Enumerable.Range(0,4).Select(i=>f.Body(i).position).ToArray());Assert.That((float)Get(f.match,"PerformanceTime"),Is.EqualTo(time));Assert.That(((DanceRules)Get(f.match,"Timing")).Score,Is.EqualTo(score));Call(f.match,"Resume");yield return Until(f,()=>(float)Get(f.match,"PerformanceTime")>time+2,6,"Actual same runway resumes after neutral count-in");Call(f.match,"SetupScreen");Call(f.match,"AllBots");Assert.That((bool)Call(f.match,"StartMatch"),Is.True);Assert.That(f.Competition.Completed,Is.Zero);Assert.That(f.Competition.Scores,Is.All.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);Assert.That(Get(f.match,"Phase").ToString(),Is.EqualTo("Hub"));Call(f.match,"Pause",0,false);for(int i=0;i<4;i++){var walker=f.Walkers.GetValue(i);var bodies=((Component)f.Trucks.GetValue(i)).GetComponentsInChildren<Rigidbody>(true);var original=(bool[])Get(walker,"kinematic");Call(walker,"RestoreOriginal");CollectionAssert.AreEqual(original,bodies.Select(b=>b.isKinematic).ToArray(),"Exit restores every original body's physical simulation state.");}}
    }
}
#endif
