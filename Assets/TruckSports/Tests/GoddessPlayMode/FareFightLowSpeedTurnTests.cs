#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FareFightLowSpeedTurnTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
    [UnityTest]
    public IEnumerator ColdNwhSlotOneCanTurnTowardTheRoadFromTheObservedHealthyStallPose()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"FareObservedStallTurn"}))
        {
            Component match=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFareFight/Scenes/TruckFareFight.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckFareFight.FareFightMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();var catalog=Field(match,"presets");foreach(var entry in (IEnumerable)Field(catalog,"entries"))if((string)Field(entry,"displayName")=="Clear"){Call(catalog,"Select",Field(entry,"id"));break;}
                Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;float until=Time.realtimeSinceStartup+5;while((float)Property(match,"Intro")>0&&Time.realtimeSinceStartup<until)yield return null;Assert.That((float)Property(match,"Intro"),Is.LessThanOrEqualTo(0));((Behaviour)match).enabled=false;
                var trucks=(Array)Field(match,"trucks");for(int i=0;i<4;i++)if(i!=1)Call(trucks.GetValue(i),"Park",new Vector3(0,-100-i*20,0));
                var truck=trucks.GetValue(1);var body=(Rigidbody)Field(truck,"body");var motor=Field(truck,"tractorMotor");var vehicle=Field(motor,"vehicle");var powertrain=Field(vehicle,"powertrain");var engine=Field(powertrain,"engine");var transmission=Field(powertrain,"transmission");var at=new Vector3(158.8f,.25f,-193.5f);var facing=Quaternion.Euler(0,140,0);
                Call(truck,"Prepare",at,facing,true);Physics.SyncTransforms();var route=((Array)Field(match,"routes")).GetValue(1);Call(route,"Reset");Call(route,"DirectGoal",at,new Vector3(160,.15f,-180));float began=Time.time,next=0,moved=0;
                while(Time.time-began<20&&moved<10){var drive=(Vector2)Call(route,"Drive",body,((Component)truck).transform,1,Time.fixedDeltaTime,1f,null,null);Call(truck,"TickDrive",drive);yield return new WaitForFixedUpdate();moved=Mathf.Max(moved,Vector3.Distance(body.position,at));if(Time.time>=next){next=Time.time+2;TestContext.WriteLine("TURN t="+(Time.time-began)+" p="+body.position+" speed="+body.linearVelocity.magnitude+" drive="+drive+" sleep="+body.IsSleeping()+" constraints="+body.constraints+" enabled="+((Behaviour)vehicle).enabled+" powertrain="+Property(powertrain,"IsActive")+" rpm="+Property(engine,"OutputRPM")+" power="+Field(engine,"generatedPower")+" gear="+Property(transmission,"Gear"));}}
                foreach(var wheel in (IEnumerable)Field(powertrain,"wheels")){var api=Field(wheel,"wheelUAPI");TestContext.WriteLine("NATIVE WHEEL "+((Component)api).name+" motor="+Property(api,"MotorTorque")+" brake="+Property(api,"BrakeTorque")+" damage="+Property(api,"Damage"));}
                Call(truck,"Prepare",at,facing,true);Physics.SyncTransforms();began=Time.time;while(Time.time-began<4){Call(truck,"TickDrive",Vector2.up);yield return new WaitForFixedUpdate();}
                TestContext.WriteLine("Straight control displacement="+Vector3.Distance(body.position,at)+" speed="+body.linearVelocity.magnitude+" rpm="+Property(engine,"OutputRPM"));Assert.That(Vector3.Distance(body.position,at),Is.GreaterThan(2),"The observed road point must allow genuine native propulsion.");
                Assert.That(moved,Is.GreaterThan(8),"A healthy cold truck on clear road must make turning progress, rather than indefinitely alternating full-lock forward and reverse.");
            }
            finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}}
        }
    }
}
#endif
