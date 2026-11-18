#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class CursedConvoyRoadPlayModeTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    [UnityTest,Timeout(170000)]
    public IEnumerator FourOriginalLoadsPhysicallyContainAlongHauntedRoadsDeliverTogetherAndReset()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);UnityEngine.Object match=null;float previousScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10HauntedConvoyRoad"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/CursedCargo/Scenes/CursedCargo.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var route=Field(match,"convoyRoad");Assert.That(route,Is.Not.Null,"Apply the explicit option1 haunted-road upgrade before acceptance.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);match.GetType().GetField("option").SetValue(match,1);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((int)Field(setup,"count"),Is.EqualTo(4));
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((bool)Property(route,"Active"),Is.True);Assert.That((bool)Property(match,"Cooperative"),Is.True);
                var cargo=(Array)Field(match,"cargo");var loads=new object[4];for(int i=0;i<4;i++)loads[i]=Field(cargo.GetValue(i),"trailer");
                Time.timeScale=3;float deadline=Time.realtimeSinceStartup+125;float next=45;int observations=0;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    if(observations<4&&(float)Property(match,"Age")>=next){observations++;next+=45;Debug.Log("HH10_CONVOY_ROAD clock="+Property(match,"Age")+" "+string.Join(" | ",Enumerable.Range(0,4).Select(i=>(string)Call(route,"Diagnostic",i))));}
                    yield return new WaitForFixedUpdate();
                }
                string state="clock="+Property(match,"Age")+" "+string.Join(" | ",Enumerable.Range(0,4).Select(i=>(string)Call(route,"Diagnostic",i)));
                Assert.That((bool)Property(match,"Finished"),Is.True,state);Assert.That((bool)Property(match,"Successful"),Is.True,"Timeout or a partial convoy is not success. "+state);
                for(int i=0;i<4;i++)
                {
                    Assert.That((int)Call(match,"Objectives",i),Is.EqualTo(3),"Every original load must physically stop at every ward. "+state);Assert.That((bool)Call(match,"Escaped",i),Is.True,state);Assert.That((bool)Call(match,"ResultWinner",i),Is.False,"Cooperative delivery must not manufacture an individual Game Night winner.");
                    Assert.That((bool)Property(cargo.GetValue(i),"Ready"),Is.True,state);Assert.That((float)Property(cargo.GetValue(i),"Condition"),Is.GreaterThan(0),state);Assert.That(Field(cargo.GetValue(i),"trailer"),Is.SameAs(loads[i]),"No replacement cargo may substitute for the original load.");
                    var coupler=Field(cargo.GetValue(i),"coupler");Assert.That((bool)Property(coupler,"PhysicalConstraintPresent"),Is.True,state);Assert.That(Property(coupler,"Attached"),Is.SameAs(loads[i]),state);
                    var load=(Rigidbody)Property(cargo.GetValue(i),"Body");Vector3 exit=(Vector3)Call(route,"ExitPosition",i);Assert.That(Vector3.Distance(load.position,exit),Is.LessThan(16),state);Assert.That((float)Call(route,"DistanceTravelled",i),Is.GreaterThan(200),state);Assert.That((int)Call(route,"EncountersSeen",i),Is.GreaterThan(0),state);
                }
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();for(int i=0;i<4;i++){Assert.That((int)Call(match,"Objectives",i),Is.Zero);Assert.That((int)Call(route,"EncountersSeen",i),Is.Zero);Assert.That((float)Call(route,"DistanceTravelled",i),Is.LessThan(1));}
                Call(match,"SetupScreen");yield return null;match.GetType().GetField("option").SetValue(match,0);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();Assert.That((bool)Property(route,"Active"),Is.False);Assert.That(((GameObject)Field(route,"geometry")).activeSelf,Is.False,"Ordinary delivery must keep its original geometry/receivers.");Assert.That((bool)Property(route,"DefaultGeometryRestored"),Is.True,"Restore the original court and every designer-authored tree active state.");
                Debug.Log("CURSED_CONVOY_ROAD_PASS four original native loads, actual wards/route/extraction, cooperative result, rematch and default0 geometry preservation. "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=previousScale;}}
        }
    }
}
#endif
