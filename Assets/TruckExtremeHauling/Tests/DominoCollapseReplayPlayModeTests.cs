#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class DominoCollapseReplayPlayModeTests
    {
        static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
        static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
        [UnityTest,Timeout(150000)]
        public IEnumerator FourActualChainsReplayWithoutPhysicsOrScoreChangesThenReset()
        {
            var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
            var type=Type.GetType("LWS.TruckExtremeHauling.DominoDerbyMatch, Assembly-CSharp",true);
            UnityEngine.Object match=null;float previousScale=Time.timeScale;
            using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10DominoCollapseReplay"}))
            {
                try
                {
                    EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/DominoDerby.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                    match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);Call(match,"NormalizeSetup");Call(match,"AllBots");
                    Assert.That((int)Field(setup,"count"),Is.EqualTo(4));var replay=Field(match,"collapseReplay");Assert.That(replay,Is.Not.Null,"Apply the explicit Domino replay scene upgrade.");
                    Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;float deadline=Time.realtimeSinceStartup+95;
                    while(!(bool)Property(match,"ReplayControls") && !(bool)Property(match,"Finished") && Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
                    Assert.That((bool)Property(match,"ReplayControls"),Is.True,"A naturally completed physical round must enter its recorded collapse replay before results.");
                    var lanes=(Array)Field(replay,"lanes");Assert.That(lanes.Length,Is.EqualTo(4));var trailers=(Array)Field(match,"trailers");
                    var bodies=new Rigidbody[trailers.Length];var positions=new Vector3[trailers.Length];var rotations=new Quaternion[trailers.Length];
                    var scores=new double[4];var objectives=new int[4];
                    for(int slot=0;slot<4;slot++)
                    {
                        scores[slot]=(double)Call(match,"ResultScore",slot);objectives[slot]=(int)Call(match,"ObjectivesCompleted",slot);
                        Assert.That(objectives[slot],Is.GreaterThan(0),"Every actual AI lane must produce contact-derived topples before its replay.");
                        var lane=lanes.GetValue(slot);Assert.That(((Array)Field(lane,"sources")).Length,Is.EqualTo(12));Assert.That((float)Property(lane,"Duration"),Is.InRange(.05f,12.05f),"Only the bounded collapse, never the full setup, is recorded.");
                        foreach(var ghost in (Transform[])Field(lane,"ghosts"))
                        {Assert.That(ghost.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(ghost.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(ghost.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);}
                    }
                    Assert.That((int)Property(replay,"VisibleGhosts"),Is.EqualTo(48),"Four twelve-bit masks must display all forty-eight original trailers.");
                    for(int i=0;i<trailers.Length;i++){bodies[i]=(Rigidbody)Field(trailers.GetValue(i),"Body");positions[i]=bodies[i].position;rotations[i]=bodies[i].rotation;Assert.That(bodies[i].isKinematic,Is.True);}
                    var firstGhost=((Transform[])Field(lanes.GetValue(0),"ghosts"))[0];Vector3 ghostStart=firstGhost.position;Quaternion ghostRotation=firstGhost.rotation;
                    for(int frame=0;frame<60;frame++)yield return new WaitForFixedUpdate();
                    Assert.That(Vector3.Distance(firstGhost.position,ghostStart)+Quaternion.Angle(firstGhost.rotation,ghostRotation)*.01f,Is.GreaterThan(.02f),"The ghost must play the actual falling trajectory.");
                    float cursor=(float)Property(replay,"Cursor");Call(match,"ExtraAction","replayrewind");Assert.That((float)Property(replay,"Cursor"),Is.LessThan(cursor));Call(match,"ExtraAction","replayspeed");Assert.That((float)Property(replay,"Speed"),Is.EqualTo(.5f));Call(match,"ExtraAction","replayangle");Assert.That((int)Property(replay,"Angle"),Is.EqualTo(1));
                    for(int i=0;i<bodies.Length;i++){Assert.That(bodies[i].position,Is.EqualTo(positions[i]));Assert.That(Quaternion.Angle(bodies[i].rotation,rotations[i]),Is.LessThan(.001f));Assert.That(bodies[i].isKinematic,Is.True);}
                    for(int slot=0;slot<4;slot++){Assert.That((double)Call(match,"ResultScore",slot),Is.EqualTo(scores[slot]));Assert.That((int)Call(match,"ObjectivesCompleted",slot),Is.EqualTo(objectives[slot]));}
                    Call(match,"ExtraAction","replayskip");yield return new WaitForFixedUpdate();Assert.That((bool)Property(match,"Finished"),Is.True);Assert.That((bool)Property(replay,"Playing"),Is.False);Assert.That((int)Property(replay,"VisibleGhosts"),Is.Zero);
                    Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();Assert.That((bool)Property(match,"ReplayControls"),Is.False);Assert.That((float)Property(lanes.GetValue(0),"Duration"),Is.Zero);
                    for(int slot=0;slot<4;slot++){Assert.That((double)Call(match,"ResultScore",slot),Is.Zero);Assert.That((int)Call(match,"ObjectivesCompleted",slot),Is.Zero);}
                    Debug.Log("DOMINO_COLLAPSE_REPLAY_PASS actual four-lane contact scores, forty-eight collider-free ghosts, bounded records, stopped live physics, existing rewind/speed/angle/skip and normal rematch.");Call(match,"SetupScreen");
                }
                finally{try{if(match!=null && (bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=previousScale;}}
            }
        }
    }
}
#endif
