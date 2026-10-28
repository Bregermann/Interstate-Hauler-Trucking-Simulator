#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public sealed class GuessPlayModeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings originalSettings,temporarySettings;readonly GameObject isolate;public Behaviour match;
        public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"Guess07Native"});originalSettings=InputSystem.settings;temporarySettings=UnityEngine.Object.Instantiate(originalSettings);temporarySettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporarySettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporarySettings;isolate=new GameObject("Guess original input isolation");UnityEngine.Object.DontDestroyOnLoad(isolate);isolate.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
        public IEnumerator Load(){Assert.That(File.Exists("Assets/TruckGuessThat/Scenes/TruckGuessThat.unity"),Is.True,"Run the explicit NEW scene author first.");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckGuessThat/Scenes/TruckGuessThat.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckGuessThat.GuessMatch")).Cast<Behaviour>().Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;}
        public void Dispose(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolate);InputSystem.settings=originalSettings;UnityEngine.Object.DestroyImmediate(temporarySettings);prefs.Dispose();}
    }

    static void KeyboardOwners(object match,int humans,int total){var setup=Get(match,"setup");setup.GetType().GetField("count").SetValue(setup,total);var players=(Array)Get(setup,"players");Key[] forwards={Key.W,Key.UpArrow,Key.I,Key.T},lefts={Key.A,Key.LeftArrow,Key.J,Key.F},backs={Key.S,Key.DownArrow,Key.K,Key.G},rights={Key.D,Key.RightArrow,Key.L,Key.H};for(int i=0;i<total;i++){var p=players.GetValue(i);var source=p.GetType().GetField("source");source.SetValue(p,Enum.Parse(source.FieldType,i<humans?"Keyboard":"AI"));if(i<humans){p.GetType().GetField("forward").SetValue(p,forwards[i]);p.GetType().GetField("left").SetValue(p,lefts[i]);p.GetType().GetField("back").SetValue(p,backs[i]);p.GetType().GetField("right").SetValue(p,rights[i]);}}match.GetType().GetField("actionKeys").SetValue(match,new[]{Key.Space,Key.RightShift,Key.R,Key.U,Key.B,Key.Numpad0,Key.Digit0,Key.PageUp,Key.Space,Key.RightShift,Key.R,Key.U});}
    static void Keys(Keyboard keyboard,params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    static string SourceState(object match){var args=new object[]{null};bool valid=(bool)match.GetType().GetMethod("ValidateSources").Invoke(match,args);var setup=Get(match,"setup");int count=(int)Get(setup,"count");string ready=string.Join(",",Enumerable.Range(0,count).Select(i=>i+":"+Call(match,"Ready",i)));return "valid="+valid+" reason="+args[0]+" ready="+ready+" keyboard="+(Keyboard.current!=null?Keyboard.current.deviceId+"/enabled="+Keyboard.current.enabled:"null")+" paused="+Get(match,"Paused")+" scale="+Time.timeScale;}
    static void StartChecked(Fixture fixture,string label){string before=SourceState(fixture.match);bool started=(bool)Call(fixture.match,"StartMatch");TestContext.WriteLine("GUESS_V2_START "+label+" "+before+" started="+started);Assert.That(started,Is.True,label+" must start through actual source validation: "+before);}
    static IEnumerator Bounded(Fixture fixture,Func<bool> predicate,string label,float seconds=3){float until=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<until)yield return null;Assert.That(predicate(),Is.True,label+" | "+SourceState(fixture.match));}
    static IEnumerator FixedSteps(Fixture fixture,int count,string label){float target=Time.fixedTime+Time.fixedDeltaTime*count;yield return Bounded(fixture,()=>Time.fixedTime>=target,label);}
    static void Capture(Fixture fixture,string name){string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/Impossible07Guess");Directory.CreateDirectory(folder);var canvas=((Component)Get(fixture.match,"menus")).GetComponent<Canvas>();var cameras=(Camera[])Get(fixture.match,"cameras");var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);foreach(int width in new[]{1280,1920})save.Invoke(null,new object[]{canvas,cameras,width,width==1280?720:1080,Path.Combine(folder,name+"-"+width+"-controlled.png"),null});}
    [UnityTest,Timeout(90000)]public IEnumerator ActualAiQuestionsFoldRealCatalogTilesAndReachSingleCorrectResult()
    {
        using(var f=new Fixture()){yield return f.Load();var rules=(LWS.TruckGuessThat.GuessRules)Get(f.match,"Rules");var tiles=((Array)Get(f.match,"tiles")).Cast<Component>().ToArray();Assert.That(tiles.Length,Is.EqualTo(96));Assert.That(rules.Identities.Select(i=>i.body+"/"+i.hat).Distinct().Count(),Is.EqualTo(24));float deadline=Time.realtimeSinceStartup+65;bool folded=false,moving=false,captured=false;var initial=tiles.Select(t=>((Transform)Get(t,"hinge")).localRotation).ToArray();while(!(bool)Get(f.match,"Finished")&&Time.realtimeSinceStartup<deadline){if(!captured&&rules.Turns>=2){Capture(f,"natural-ai-deduction");captured=true;}for(int i=0;i<tiles.Length;i++){folded|=(bool)Get(tiles[i],"Folded");moving|=Quaternion.Angle(initial[i],((Transform)Get(tiles[i],"hinge")).localRotation)>20;}yield return null;}TestContext.WriteLine("GUESS_NATURAL turns="+rules.Turns+" winner="+rules.Winner+" folded="+folded+" realMotion="+moving);Assert.That((bool)Get(f.match,"Finished"),Is.True);Assert.That(rules.Winner,Is.GreaterThanOrEqualTo(0));Assert.That(folded&&moving,Is.True);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(1));Assert.That(tiles.All(t=>t.GetComponentsInChildren<Rigidbody>(true).Length==0&&t.GetComponentsInChildren<Collider>(true).Length==0),Is.True);}
    }
    [UnityTest,Timeout(60000)]public IEnumerator RealFoldTweenFreezesOnPauseAndRematchRestoresAll24PerOwner()
    {
        using(var f=new Fixture()){
            yield return f.Load();var rules=(LWS.TruckGuessThat.GuessRules)Get(f.match,"Rules");yield return Bounded(f,()=>rules.Turns>0,"An original AI question must resolve",8);var tiles=((Array)Get(f.match,"tiles")).Cast<Component>().ToArray();Assert.That(tiles.Length,Is.EqualTo(96),"All four original24-tile boards remain authored.");yield return new WaitForSeconds(.1f);Call(f.match,"Pause",0,false);yield return null;
            var rotations=tiles.Select(t=>((Transform)Get(t,"hinge")).localRotation).ToArray();yield return new WaitForSecondsRealtime(.25f);CollectionAssert.AreEqual(rotations,tiles.Select(t=>((Transform)Get(t,"hinge")).localRotation).ToArray(),"Actual hinge transforms must remain exact throughout pause.");Call(f.match,"Resume");yield return FixedSteps(f,3,"Resume must advance genuine fixed steps");Call(f.match,"SetupScreen");Call(f.match,"AllBots");StartChecked(f,"AI rematch");rules=(LWS.TruckGuessThat.GuessRules)Get(f.match,"Rules");
            Assert.That(rules.Turns,Is.Zero,"Rematch starts before another AI question.");for(int seat=0;seat<rules.Players;seat++)Assert.That(rules.PublicCandidates(seat).Length,Is.EqualTo(24),"Every participating owner's full candidate set returns.");
            foreach(var tile in tiles){int owner=(int)Get(tile,"owner");Assert.That((bool)Get(tile,"Folded"),Is.EqualTo(owner>=rules.Players),"Rematch tile owner="+owner+" id="+Get(tile,"identity")+": participating boards unfold; unused boards remain folded for privacy.");}
            Assert.That((GameObject)Get(f.match,"privacyCurtain"),Is.Not.Null,"The real privacy curtain remains authored.");
        }
    }
    [UnityTest,Timeout(60000)]public IEnumerator FourAssignedKeyboardOwnersRespectPrivateHandoffAndRealQuestionAction()
    {
        using(var f=new Fixture()){
            yield return f.Load();var keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();
            try{
                Keys(keyboard);Call(f.match,"SetupScreen");KeyboardOwners(f.match,4,4);StartChecked(f,"Four assigned keyboards");Keys(keyboard);
                var players=(Array)Get(Get(f.match,"setup"),"players");yield return Bounded(f,()=>Enumerable.Range(0,4).All(i=>(bool)Get(players.GetValue(i),"released")),"All four assigned owners must pass the original neutral gate");Assert.That((bool)Get(f.match,"Handoff"),Is.True,"The incoming owner begins behind the privacy handoff.");
                Keys(keyboard,Key.RightShift);yield return FixedSteps(f,2,"Wrong-owner action must actually reach two simulation steps");Assert.That((bool)Get(f.match,"Handoff"),Is.True,"Seat one cannot accept seat zero's private handoff.");
                Keys(keyboard);yield return null;Keys(keyboard,Key.Space);yield return Bounded(f,()=>!(bool)Get(f.match,"Handoff"),"The actual owner action must accept its handoff");yield return Bounded(f,()=>((Transform[])Get(f.match,"boardFocus")).Select((board,i)=>board.gameObject.activeSelf==(i==0)).All(value=>value),"Presentation must open only owner zero's actual board after handoff");Assert.That((bool)Get(f.match,"BroadcastFramingApplicable"),Is.False,"Tractor framing must never expose another owner's private board.");
                Keys(keyboard);yield return null;int before=(int)Get(f.match,"SelectedQuestion");Keys(keyboard,Key.D);yield return Bounded(f,()=>(int)Get(f.match,"SelectedQuestion")!=before,"The assigned navigation key must select another legal question");Keys(keyboard);yield return null;Keys(keyboard,Key.Space);var rules=(LWS.TruckGuessThat.GuessRules)Get(f.match,"Rules");yield return Bounded(f,()=>rules.Turns==1,"The assigned confirm key must resolve exactly one truthful question");Assert.That(rules.PublicCandidates(0).Length,Is.InRange(1,23),"The real answer must remove incompatible candidates.");Assert.That((string)Get(f.match,"Header"),Does.Not.Contain(rules.Identities[rules.OwnerSecret(1)].name),"Broadcast text must not expose the rival secret.");Capture(f,"four-owner-assigned-keyboard-truthful-answer");
            }finally{InputSystem.RemoveDevice(keyboard);}
        }
    }
}
#endif
