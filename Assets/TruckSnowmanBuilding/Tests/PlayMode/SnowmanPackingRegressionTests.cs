#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LWS.TruckSnowmanBuilding;
public sealed class SnowmanPackingRegressionTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string n)=>Type.GetType(n+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly InputSettings original,scoped;readonly GameObject isolate;public Behaviour match;
        public Component[] Balls=>((Array)Get(match,"balls")).Cast<Component>().ToArray();public SnowmanRules Rules=>(SnowmanRules)Get(match,"Rules");
        public Fixture(){prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SnowmanSleepingSupport"});original=InputSystem.settings;scoped=UnityEngine.Object.Instantiate(original);scoped.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;scoped.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=scoped;isolate=new GameObject("Snowman sleeping support input isolation");UnityEngine.Object.DontDestroyOnLoad(isolate);isolate.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));}
        public IEnumerator Load(){EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnowmanBuilding/Scenes/TruckSnowmanBuilding.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Behaviour)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckSnowmanBuilding.SnowmanMatch"));Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;}
        public void Dispose(){Time.timeScale=1;if(match!=null){match.enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolate);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(scoped);prefs.Dispose();}
    }
    static IEnumerator Until(Func<bool> predicate,float seconds,string label){float end=Time.realtimeSinceStartup+seconds;while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(predicate(),Is.True,label);}
    [UnityTest,Timeout(60000)]public IEnumerator ActualSleepingSupportKeepsOriginalHoldAndRejectsNoContactDisableSeparationAndReset()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();f.match.enabled=false;foreach(var truck in ((Array)Get(f.match,"trucks")).Cast<Component>()){Call(truck,"Power",false);((Rigidbody)Get(truck,"body")).isKinematic=true;}
            var ball=f.Balls[0];var body=(Rigidbody)Get(ball,"body");var foundation=(Collider)Get(f.match,"foundation");foreach(var other in f.Balls.Skip(1))((Rigidbody)Get(other,"body")).isKinematic=true;
            // Explicit controlled pose fixture, not natural gameplay progress.
            Vector3 target=((Transform)Get(f.match,"buildCenter")).position;target.y=foundation.bounds.max.y+.75f+.03f;Call(ball,"Restore",.75f,0f,-1,target,Quaternion.identity,Vector3.zero,Vector3.zero);
            for(int i=0;i<40;i++)Assert.That((bool)Call(ball,"RestOn",null,foundation,.02f),Is.False,"Geometry alone, without a genuine Enter/Stay pair, cannot accrue support");
            yield return Until(()=>body.IsSleeping()&&(Collider)Get(ball,"packSurface")==foundation,4,"Original dynamic ball must naturally sleep in actual physical foundation contact");yield return new WaitForSeconds(.2f);Assert.That(Time.fixedTime-(float)Get(ball,"packStamp"),Is.GreaterThan(.07f),"Reproduce actual sleeping callback expiry");
            for(int i=0;i<32;i++){Assert.That((bool)Call(ball,"RestOn",null,foundation,.02f),Is.False,"Original .65second hold must not shorten");Assert.That(body.IsSleeping(),Is.True);yield return new WaitForFixedUpdate();}
            Assert.That((bool)Call(ball,"RestOn",null,foundation,.02f),Is.True,"Genuine sleeping support must complete the original hold without wake/forces");Assert.That(body.IsSleeping(),Is.True);Assert.That(f.Rules.Placed,Is.Zero,"Fixture never awards controlled geometry points");
            bool wasEnabled=foundation.enabled;try{foundation.enabled=false;Assert.That((bool)Call(ball,"RestOn",null,foundation,.02f),Is.False,"Disabled support is invalid immediately, without relying on Exit timing");Assert.That((Collider)Get(ball,"packSurface"),Is.Null);}finally{foundation.enabled=wasEnabled;}
            body.WakeUp();yield return Until(()=>(Collider)Get(ball,"packSurface")==foundation,2,"A fresh actual Stay reacquires the existing real pair");
            body.position+=Vector3.up;Assert.That((bool)Call(ball,"RestOn",null,foundation,.02f),Is.False,"Actual geometric separation rejects stale paired support before next physics callback");Assert.That((Collider)Get(ball,"packSurface"),Is.Null);
            Call(ball,"Restore",.75f,0f,-1,target,Quaternion.identity,Vector3.zero,Vector3.zero);body.WakeUp();yield return Until(()=>(Collider)Get(ball,"packSurface")==foundation,2,"Actual contact is reacquired after explicit controlled restore");((Behaviour)ball).enabled=false;Assert.That((Collider)Get(ball,"packSurface"),Is.Null,"Disabling the observer clears support");((Behaviour)ball).enabled=true;
            Call(ball,"Track");for(int i=0;i<40;i++)Assert.That((bool)Call(ball,"RestOn",null,foundation,.02f),Is.False,"Reset cannot retain old contact credit");Assert.That(f.Rules.Placed,Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
        }
    }
    [UnityTest,Timeout(250000)]public IEnumerator NaturalAiPhysicallyPacksAllThreeInOrderAndHandsOffOriginalSingleHoist()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var balls=f.Balls;var roots=balls.Select(b=>(Rigidbody)Get(b,"body")).ToArray();var crane=Get(f.match,"hoist");int placed=0;float end=Time.realtimeSinceStartup+225,next=0;
            while(f.Rules.Placed<3&&(float)Get(f.match,"Clock")<210&&Time.realtimeSinceStartup<end)
            {
                if(Time.realtimeSinceStartup>=next||f.Rules.Placed!=placed)
                {
                    Debug.Log("SNOWMAN_ORDERED_HANDOFF clock="+Get(f.match,"Clock")+" placed="+f.Rules.Placed+" lift="+Get(f.match,"LiftRole")+" stage="+Get(crane,"Stage")+" pickups="+Get(crane,"Pickups")+" releases="+Get(crane,"Releases")+" balls="+string.Join(";",balls.Select(b=>Get(b,"role")+" packed="+Get(b,"Packed")+" p="+((Rigidbody)Get(b,"body")).position+" v="+((Rigidbody)Get(b,"body")).linearVelocity+" sleep="+((Rigidbody)Get(b,"body")).IsSleeping())));next=Time.realtimeSinceStartup+5;
                }
                Assert.That(f.Rules.Placed,Is.InRange(placed,placed+1));placed=f.Rules.Placed;for(int i=0;i<3;i++)Assert.That((bool)Get(balls[i],"Packed"),Is.EqualTo(i<placed),"Only actual ordered support/rest may pack each next sphere");yield return null;
            }
            Assert.That(f.Rules.Placed,Is.EqualTo(3),"Original real AI must finish all three physical packing handoffs before another full540s workshop run");Assert.That((int)Get(crane,"Pickups"),Is.EqualTo(3));Assert.That((int)Get(crane,"Releases"),Is.EqualTo(3));Assert.That((int)Get(f.match,"LiftRole"),Is.EqualTo(-1));Assert.That(Get(crane,"Stage").ToString(),Is.EqualTo("Idle"));
            for(int i=0;i<3;i++){Assert.That(roots[i].isKinematic,Is.False);var bond=roots[i].GetComponent<FixedJoint>();Assert.That(bond,Is.Not.Null);Assert.That(bond.connectedBody,Is.SameAs(i==0?(Rigidbody)Get(f.match,"foundationBody"):roots[i-1]));Assert.That((float)Get(balls[i],"Rolled"),Is.GreaterThanOrEqualTo(8));}
            Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero,"No workshop completion receipt from only packing");CollectionAssert.AreEqual(roots,f.Balls.Select(b=>(Rigidbody)Get(b,"body")).ToArray());
        }
    }
}
#endif
