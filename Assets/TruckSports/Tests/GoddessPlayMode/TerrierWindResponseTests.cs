#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TerrierWindResponseTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    [UnityTest]
    public IEnumerator ActualAirborneWindResponseIsSeventyPercentWithoutChangingWeatherOrTruckTuning()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"TerrierWindResponse"}))
        {
            Component match=null;bool gravity=true;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckTerrierRacing.TerrierMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;
                float until=Time.realtimeSinceStartup+8;while(match.GetType().GetProperty("Phase").GetValue(match).ToString()!="Race"&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That(match.GetType().GetProperty("Phase").GetValue(match).ToString(),Is.EqualTo("Race"));((Behaviour)match).enabled=false;
                var course=Field(match,"course");var setting=course.GetType().GetField("windForceMultiplier");Assert.That(setting,Is.Not.Null);Assert.That((float)setting.GetValue(course),Is.EqualTo(.7f).Within(.0001f));
                var racers=(Array)Field(match,"racers");Assert.That(racers.Length,Is.EqualTo(12));
                foreach(var competitor in racers)Assert.That(Field(Field(competitor,"match"),"course"),Is.SameAs(course),"Humans and AI must share one mode-specific multiplier.");
                var racer=racers.GetValue(0);var truck=Field(racer,"truck");var body=(Rigidbody)Field(truck,"body");gravity=body.useGravity;body.useGravity=false;
                var weather=Field(course,"weather");var flow=weather.GetType().GetProperty("Flow");var wind=new Vector3(10,0,0);
                var at=(Vector3)Call(course,"World",new Vector3(0,35,18));var rotation=(Quaternion)course.GetType().GetProperty("DrivingRotation").GetValue(course);
                // A calibrated wind-tunnel fixture: native NWH/PhysX body and
                // actual racer Step apply the force. Gravity/collisions are
                // isolated; no claim of complete-race or storm visual acceptance.
                var speeds=new float[3];float[] levels={1,.7f,0};
                for(int trial=0;trial<levels.Length;trial++)
                {
                    Call(truck,"Prepare",at,rotation,true);body.useGravity=false;Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();body.linearVelocity=body.angularVelocity=Vector3.zero;
                    setting.SetValue(course,levels[trial]);flow.GetSetMethod(true).Invoke(weather,new object[]{wind});
                    Call(racer,"Step",Vector2.zero,false,Time.fixedDeltaTime);yield return new WaitForFixedUpdate();
                    Assert.That((bool)truck.GetType().GetProperty("Grounded").GetValue(truck),Is.False);
                    speeds[trial]=Vector3.Dot(body.linearVelocity,wind.normalized);
                    Assert.That((Vector3)flow.GetValue(weather),Is.EqualTo(wind),"Vehicle response must not rewrite the authoritative weather wind.");
                }
                TestContext.WriteLine("Actual wind-tunnel velocities old response="+speeds[0]+" default="+speeds[1]+" disabled="+speeds[2]);
                Assert.That(speeds[0],Is.GreaterThan(.01f));Assert.That(speeds[1]/speeds[0],Is.EqualTo(.7f).Within(.02f));Assert.That(Mathf.Abs(speeds[2]),Is.LessThan(.002f));
                setting.SetValue(course,.7f);body.useGravity=gravity;
            }
            finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}}
        }
    }
}
#endif
