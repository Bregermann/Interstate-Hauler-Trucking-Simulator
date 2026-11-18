#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HarvestPhysicalRitualPlayModeTests
{
    static object Field(object owner,string name)=>owner.GetType().GetField(name).GetValue(owner);
    static object Property(object owner,string name)=>owner.GetType().GetProperty(name).GetValue(owner);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethod(name).Invoke(owner,args);
    [UnityTest]
    public IEnumerator GateOpeningChecksActualSavedRigBeyondOldCenterRadiusWithoutMovingIt()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/ScarecrowSeason/Scenes/ScarecrowSeason.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);
        var match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var truck=((Array)Field(match,"trucks")).GetValue(0);var shape=(BoxCollider)Field(truck,"shape");
        Vector3 originalPosition=shape.transform.position;Quaternion originalRotation=shape.transform.rotation;
        var fixture=new GameObject("Physical harvest gate clearance regression");
        try
        {
            var leaf=fixture.AddComponent<BoxCollider>();leaf.center=new Vector3(10,1.5f,0);leaf.size=new Vector3(20,3,.5f);
            Vector3 forward=shape.transform.forward,right=shape.transform.right;
            Vector3 extents=shape.bounds.extents;
            float forwardHalf=Mathf.Abs(forward.x)*extents.x+Mathf.Abs(forward.y)*extents.y+Mathf.Abs(forward.z)*extents.z;
            fixture.transform.SetPositionAndRotation(shape.bounds.center-right*19-forward*(forwardHalf+.3f)-Vector3.up*1.5f,originalRotation);
            var query=Field(match,"harvestObjects").GetType().GetMethod("WouldGateHitParticipant",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.That(query,Is.Not.Null);
            bool Hit(float angle)=>(bool)query.Invoke(null,new object[]{leaf,originalRotation*Quaternion.Euler(0,angle,0),shape});
            Assert.That(Vector3.Distance(shape.bounds.center,fixture.transform.TransformPoint(leaf.center)),Is.GreaterThan(8),"Exercise the leaf end missed by the old center-radius check.");
            Assert.That(Hit(0),Is.False,"The original saved rig starts genuinely clear of this closed leaf.");
            Assert.That(Hit(-5),Is.True,"The proposed real solid-leaf pose would penetrate the existing rig and must be held.");
            Assert.That(Hit(5),Is.False,"Opening away from the rig remains clear; unrelated venue props do not stall this check.");
            Assert.That(shape.transform.position,Is.EqualTo(originalPosition));Assert.That(Quaternion.Angle(shape.transform.rotation,originalRotation),Is.LessThan(.001f));
        }
        finally{UnityEngine.Object.Destroy(fixture);}
    }
    [UnityTest,Timeout(150000)]
    public IEnumerator NaturalRitualCarriesEffigiesUnlocksPhysicalGateAndEscapesThenResets()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10HarvestPhysicalRitual"}))
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/ScarecrowSeason/Scenes/ScarecrowSeason.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);
            var match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
            match.GetType().GetField("option").SetValue(match,1);Call(match,"AllBots");
            var props=Field(match,"harvestObjects");Assert.That(props,Is.Not.Null);
            var hinge=(Transform)Field(props,"gateHinge");Quaternion closed=hinge.localRotation;
            var effigies=(GameObject[])Field(props,"carriedEffigies");
            Assert.That((bool)Call(match,"StartMatch"),Is.True);
            try
            {
                Time.timeScale=3;bool carried=false,opened=false;float deadline=Time.realtimeSinceStartup+120;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    carried|=effigies.Any(g=>g!=null&&g.activeInHierarchy);
                    opened|=(bool)Property(props,"Unlocked")&&Quaternion.Angle(closed,hinge.localRotation)>80;
                    yield return null;
                }
                Assert.That((bool)Property(match,"Finished"),Is.True,"The actual Ritual Hunt must resolve through ordinary rules.");
                Assert.That(carried,Is.True,"A real collected relic must appear on its owning truck before returning to the altar.");
                Assert.That(opened,Is.True,"Completing the actual key objective must open the physical route gate.");
                int count=(int)Field(Field(match,"setup"),"count");bool escaped=false;
                for(int seat=0;seat<count;seat++)escaped|=(bool)Call(match,"Escaped",seat);
                Assert.That(escaped,Is.True,"At least one original AI driver must complete the ritual and physically escape; timer expiry is insufficient.");
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Assert.That((bool)Property(props,"Unlocked"),Is.False);Assert.That(Quaternion.Angle(closed,hinge.localRotation),Is.LessThan(.01f));
                Assert.That(effigies.Any(g=>g!=null&&g.activeInHierarchy),Is.False);
                Debug.Log("HH10_HARVEST_RITUAL_PASS original carried relic, actual key receipt, physical gate, escape and fresh rematch.");
            }
            finally{Time.timeScale=1;Call(match,"SetupScreen");}
        }
    }
}
#endif
