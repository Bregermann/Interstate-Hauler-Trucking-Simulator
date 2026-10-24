#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FishingScoreViewportLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly List<object> receipts=new List<object>();
    void Record(object receipt)=>receipts.Add(receipt);
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);
    static Rect ScreenRect(RectTransform rect){var points=new Vector3[4];rect.GetWorldCorners(points);var a=RectTransformUtility.WorldToScreenPoint(null,points[0]);var b=RectTransformUtility.WorldToScreenPoint(null,points[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}

    [UnityTest,Timeout(360000)]
    public IEnumerator ActualCatchesStayInsideOwnedViewsAcrossHumansAISpectatorsAndRematches()
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"FishingOwnedCatchFeedback"});
        var isolation=new GameObject("Fishing real net-input score ownership acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var original=InputSystem.settings;var input=UnityEngine.Object.Instantiate(original);input.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;input.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=input;
        Keyboard keyboard=null;Component match=null;EventInfo scoreEvent=null;Delegate handler=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFishing/Scenes/TruckFishing.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            scoreEvent=match.GetType().GetEvent("ScoreConfirmed");handler=Delegate.CreateDelegate(scoreEvent.EventHandlerType,this,GetType().GetMethod("Record",Any));scoreEvent.AddEventHandler(match,handler);
            var presenters=((Component)Get(match,"menus")).GetComponentsInChildren(Runtime("SportsScorePresentation"),true).Cast<Component>().OrderBy(p=>(int)Get(p,"viewport")).ToArray();Assert.That(presenters.Length,Is.EqualTo(4));
            string previous=null;
            for(int pass=0;pass<7;pass++)
            {
                int humans=pass<2?0:pass==6?1:pass-1;bool aiViews=pass==1||pass==6;
                Time.timeScale=1;((Behaviour)match).enabled=true;Call(match,"SetupScreen");var setup=Get(match,"setup");Set(setup,"count",4);Call(match,"AllBots");
                Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{aiViews});var players=(Array)Get(setup,"players");
                for(int slot=0;slot<4;slot++){var player=players.GetValue(slot);Set(player,"name","Same River Driver");Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Hard"));if(slot<humans)Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"Keyboard"));}
                receipts.Clear();InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Assert.That(Call(match,"StartMatch"),Is.True);yield return null;
                Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(previous));previous=(string)Get(match,"MatchId");Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero);
                Assert.That(((Camera[])Get(match,"cameras")).Length,Is.EqualTo(aiViews?4:Math.Max(1,humans)));
                Assert.That(presenters.All(p=>Get(p,"LastPresented")==null),Is.True,"Rematch clears every old participant notification.");
                Time.timeScale=4;float deadline=Time.realtimeSinceStartup+35;
                // The original AI only chooses left/right software key input for
                // human fixtures. Nets, scheduled fish and catches remain real.
                while((double)Get(match,"Clock")<23&&!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    var keys=new List<Key>();for(int slot=0;slot<humans;slot++){float direction=(float)Call(match,"AI",slot);if(Mathf.Abs(direction)>.12f)keys.Add((Key)Get(players.GetValue(slot),direction>0?"right":"left"));}
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));yield return null;
                }
                Assert.That((double)Get(match,"Clock"),Is.GreaterThanOrEqualTo(23));Assert.That(receipts.Count,Is.GreaterThan(0));Time.timeScale=1;
                // Capture a fresh real notification, rather than requiring a
                // transient catch from seconds earlier to stay on screen.
                var prior=presenters.Select(p=>(int)Get(p,"Presented")).ToArray();int fresh=-1;deadline=Time.realtimeSinceStartup+24;
                while(fresh<0&&Time.realtimeSinceStartup<deadline)
                {
                    var keys=new List<Key>();for(int slot=0;slot<humans;slot++){float direction=(float)Call(match,"AI",slot);if(Mathf.Abs(direction)>.12f)keys.Add((Key)Get(players.GetValue(slot),direction>0?"right":"left"));}InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));yield return null;
                    for(int view=0;view<presenters.Length;view++)if((int)Get(presenters[view],"Presented")>prior[view]){fresh=view;break;}
                }
                Assert.That(fresh,Is.GreaterThanOrEqualTo(0),"Fresh actual catch must be observed before its transient UI capture.");((Behaviour)match).enabled=false;InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                var totals=new double[4];var ids=new HashSet<string>();foreach(var receipt in receipts){int slot=(int)Get(receipt,"Slot");double delta=(double)Get(receipt,"Delta");Assert.That(delta==1||delta==5,Is.True);Assert.That(Get(receipt,"Unit"),Is.EqualTo("LB"));Assert.That(ids.Add((string)Get(receipt,"EventId")),Is.True);totals[slot]+=delta;Assert.That(Get(receipt,"Total"),Is.EqualTo(totals[slot]));Assert.That(Get(receipt,"MatchId"),Is.EqualTo(previous));}
                var caught=(int[])Get(Get(match,"rules"),"caught");Assert.That(totals,Is.EqualTo(caught.Select(x=>(double)x).ToArray()),"Only actual accepted fish weight can create a receipt.");
                var cameras=(Camera[])Get(match,"cameras");deadline=Time.realtimeSinceStartup+18;
                while(presenters.Take(cameras.Length).Any(p=>Get(p,"LastPresented")==null)&&Time.realtimeSinceStartup<deadline)yield return null;
                yield return new WaitForSeconds(.87f);Canvas.ForceUpdateCanvases();
                for(int view=0;view<4;view++)
                {
                    var p=presenters[view];int owner=view<cameras.Length?(int)Get(p,"ViewportOwner"):-1;
                    if(view>=cameras.Length){Assert.That(((CanvasGroup)Get(p,"visibility")).alpha,Is.Zero);Assert.That(Get(p,"LastPresented"),Is.Null);continue;}
                    int expected=humans==0&&!aiViews?(int)Get(match,"Spectator"):((int[])Get(match,"Owners"))[view];Assert.That(owner,Is.EqualTo(expected));var receipt=Get(p,"LastPresented");Assert.That(receipt,Is.Not.Null,"A real owned catch must reach each visible viewport.");Assert.That(Get(receipt,"Slot"),Is.EqualTo(owner));Assert.That(Get(receipt,"MatchId"),Is.EqualTo(previous));
                    if(view==fresh)Assert.That(((Component)Get(p,"notification")).gameObject.activeInHierarchy,Is.True);var texts=((Component)Get(p,"notification")).GetComponentsInChildren(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true),true);foreach(var text in texts){Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False);}
                    var rect=ScreenRect((RectTransform)p.transform);var camera=cameras[view].rect;Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(camera.xMin*Screen.width-1));Assert.That(rect.xMax,Is.LessThanOrEqualTo(camera.xMax*Screen.width+1));Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(camera.yMin*Screen.height-1));Assert.That(rect.yMax,Is.LessThanOrEqualTo(camera.yMax*Screen.height+1));
                    Assert.That(((Component)((Array)Get(Get(match,"menus"),"viewTexts")).GetValue(view)).gameObject.activeInHierarchy,Is.True,"Preserve the original complete net/survival HUD.");
                }
                if(pass==0)Assert.That((int)Get(match,"Spectator"),Is.GreaterThan(0),"Real clock changes the AI-only broadcast owner; old participant feedback must not leak.");
                Time.timeScale=0;var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/FishingOwnedAwards/pass-"+pass+"-"+size.y+".png",null});
                Call(match,"Pause",0,false);yield return null;Assert.That(presenters.All(p=>((CanvasGroup)Get(p,"visibility")).alpha==0),Is.True);Call(match,"Resume");
                TestContext.WriteLine("ACTUAL_FISHING_OWNED_AWARDS humans="+humans+" AIViews="+aiViews+" caught="+string.Join(",",caught)+" receipts="+receipts.Count+" actual scheduled/swept catches, owned Heat, original HUD, duplicate names and fresh rematch.");
            }
        }
        finally{Time.timeScale=1;if(match!=null){if(handler!=null)scoreEvent.RemoveEventHandler(match,handler);((Behaviour)match).enabled=true;Call(match,"SetupScreen");}if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(input);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
}
#endif
