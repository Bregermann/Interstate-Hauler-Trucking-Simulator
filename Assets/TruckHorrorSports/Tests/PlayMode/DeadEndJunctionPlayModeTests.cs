#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class DeadEndJunctionPlayModeTests
{
    static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static object Property(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method).Invoke(o,args);
    [UnityTest,Timeout(180000)]
    public IEnumerator NaturalPuzzleDrivesActualBranchesObservesLightCluesAndReachesAnEnding()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        UnityEngine.Object match=null;float previousScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10DeadEndJunction"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/DeadEnd/Scenes/DeadEnd.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var puzzle=Field(match,"junctionPuzzle");Assert.That(puzzle,Is.Not.Null,"Apply the explicit branching-sector scene upgrade before acceptance.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,2);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
                float deadline=Time.realtimeSinceStartup+115;Time.timeScale=3;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
                var body=(Rigidbody)Field(((Array)Field(match,"trucks")).GetValue(0),"body");
                string state="clock="+Property(match,"Age")+" position="+body.position+" entered="+Call(puzzle,"Entered",0)+" ending="+Call(puzzle,"ExitKind",0)+" tasks="+Call(match,"Objectives",0)+" wrongRoutes="+Call(puzzle,"WrongTurns",0)+" "+Call(puzzle,"Diagnostic",0);
                Assert.That((bool)Property(match,"Finished"),Is.True,state);Assert.That((bool)Call(puzzle,"Entered",0),Is.True,state);
                Assert.That((int)Call(match,"Objectives",0),Is.EqualTo(4),"Only the existing four actual route receipts may complete the puzzle. "+state);
                for(int gate=0;gate<4;gate++)Assert.That((bool)Call(puzzle,"SawClue",0,gate),Is.True,"The appropriate physically visible headlight/dark clue must be observed. "+state);
                Assert.That((bool)Call(match,"Escaped",0),Is.True,state);Assert.That(Call(puzzle,"ExitKind",0).ToString(),Is.Not.EqualTo("None"),state);
                Assert.That((string)Call(puzzle,"EndingCaption",0),Is.Not.Empty,state);
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();
                Assert.That((bool)Call(puzzle,"Entered",0),Is.False);Assert.That(Call(puzzle,"ExitKind",0).ToString(),Is.EqualTo("None"));
                for(int gate=0;gate<4;gate++)Assert.That((bool)Call(puzzle,"SawClue",0,gate),Is.False);
                Assert.That((int)Call(match,"Objectives",0),Is.Zero);Debug.Log("ALPHA10_DEAD_END_BRANCH_PASS actual roads, visible clues, existing receipts, physical ending and reset: "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=previousScale;}}
        }
    }
}
#endif
