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
using LWS.TruckSnowmanBuilding;
public sealed class SnowmanPackingDiagnostics
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string n)=>Type.GetType(n+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    [UnityTest,Timeout(140000)]public IEnumerator NaturalAiFirstPhysicalPackingGateRetainsActualContactAndReportsEveryCranePredicate()
    {
        var prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SnowmanV5PackingGate"});var original=InputSystem.settings;var scoped=UnityEngine.Object.Instantiate(original);scoped.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;scoped.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=scoped;var isolation=new GameObject("Snowman packing diagnostic input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));Component match=null;
        try
        {
            Assert.That(File.Exists("Assets/TruckSnowmanBuilding/Scenes/TruckSnowmanBuilding.unity"),Is.True);EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnowmanBuilding/Scenes/TruckSnowmanBuilding.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckSnowmanBuilding.SnowmanMatch"));Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
            var balls=((Array)Get(match,"balls")).Cast<Component>().ToArray();var ball=balls[0];var body=(Rigidbody)Get(ball,"body");var foundation=(Collider)Get(match,"foundation");var hoist=Get(match,"hoist");var probe=body.gameObject.AddComponent<SnowmanPackingContactProbe>();probe.target=foundation;var rack=Get(match,"collectionRack");
            float end=Time.realtimeSinceStartup+110,next=0;while(((SnowmanRules)Get(match,"Rules")).Placed<1&&(float)Get(match,"Clock")<90&&Time.realtimeSinceStartup<end)
            {
                float clock=(float)Get(match,"Clock");if(clock>=52&&clock>=next)
                {
                    next=clock+.5f;float radius=(float)Get(ball,"radius");Vector3 target=((Transform)Get(match,"buildCenter")).position+Vector3.up*radius;var surface=(Collider)Get(ball,"packSurface");var joint=body.GetComponent<FixedJoint>();
                    Debug.Log("SNOWMAN_PACKING_GATE clock="+clock.ToString("R")+" stage="+Get(hoist,"Stage")+" lift="+Get(match,"LiftRole")+" placed="+((SnowmanRules)Get(match,"Rules")).Placed+" position="+body.position.ToString("F7")+" target="+target.ToString("F7")+" offset="+Vector3.Distance(body.position,target).ToString("R")+" velocity="+body.linearVelocity.ToString("F7")+" angular="+body.angularVelocity.ToString("F7")+" angle="+Quaternion.Angle(body.rotation,Quaternion.identity).ToString("R")+" up="+Vector3.Dot(body.transform.up,Vector3.up).ToString("R")+" sleeping="+body.IsSleeping()+" joint="+(joint==null?"none":joint.GetInstanceID().ToString())+" released="+Get(hoist,"Released")+" atRest="+Call(hoist,"AtRest",target,radius*2)+" bodyDynamic="+!body.isKinematic+" packSurface="+(surface==null?"none":surface.name)+" surfaceIsFoundation="+(surface==foundation)+" supportAge="+(Time.fixedTime-(float)Get(ball,"packStamp")).ToString("R")+" rest="+Get(ball,"rest")+" pair="+probe.paired+" enter="+probe.enters+" stay="+probe.stays+" exit="+probe.exits+" pairAge="+(Time.fixedTime-probe.last).ToString("R")+" normalY="+probe.maxUp.ToString("R")+" roles="+string.Join(";",balls.Select(b=>"r"+Get(b,"role")+"@"+((Rigidbody)Get(b,"body")).position.ToString("F4")+" v="+((Rigidbody)Get(b,"body")).linearVelocity.ToString("F4")+" inLane="+Call(rack,"ContainsLane",b)))+" ai="+string.Join(",",(int[])Get(match,"AiDeliveryPhase")));
                }
                yield return null;
            }
            Assert.That(((SnowmanRules)Get(match,"Rules")).Placed,Is.GreaterThanOrEqualTo(1),"Original uninjected AI must finish real first foundation packing within90simulation seconds. Gate trace distinguishes orientation/joint/velocity/contact expiry before any production packing change.");Assert.That((bool)Get(ball,"Packed"),Is.True);Assert.That(body.isKinematic,Is.False);Assert.That(probe.enters,Is.GreaterThan(0));Assert.That(probe.maxUp,Is.GreaterThanOrEqualTo(.5f));
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(scoped);prefs.Dispose();}
    }
}
#endif
