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

public sealed class PongCarnivalReplayCameraTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    [UnityTest,Timeout(180000)]
    public IEnumerator OriginalPhysicalShotReplayBeginsAboveTableAndKeepsEveryAngleOutsideItsGeometry()
    {
        var lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CarnivalPongReplayCamera"});
        var isolation=new GameObject("Private original Pong replay camera acceptance");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBeerPong/Scenes/TruckBeerPong.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckBeerPong.PongMatch, Assembly-CSharp",true)) as Component;
            Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=2;
            float deadline=Time.realtimeSinceStartup+60;
            while(Get(match,"Phase").ToString()!="Replay"&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Get(match,"Phase").ToString(),Is.EqualTo("Replay"),"An original physical AI shot must reach its real recorded replay.");
            var rules=Get(match,"rules");var shots=((int[])Get(rules,"shots")).ToArray();var hits=((int[])Get(rules,"hits")).ToArray();
            var replay=Get(match,"replay");var rig=Get(match,"cameraRig");var camera=(Camera)Get(rig,"view");var table=(Collider)Get(match,"table");
            Time.timeScale=0;yield return null;
            for(int angle=0;angle<3;angle++)
            {
                if(angle>0){Call(match,"ExtraAction","replayangle");yield return null;}
                Assert.That(Get(replay,"Angle"),Is.EqualTo(angle));Check("start-angle-"+angle);
                foreach(float fraction in new[]{.25f,.5f,.75f,1f})
                {Call(replay,"Seek",(float)Get(replay,"Duration")*fraction);yield return null;Check("record-"+fraction+"-angle-"+angle);}
                foreach(int width in new[]{1280,1920})Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,width,width==1280?720:1080,"Logs/TruckSports/PlaytestRepair/PongCarnivalReplayNative/Actual-original-shot-angle-"+angle+"-"+width+"-controlled.png",null});
            }
            CollectionAssert.AreEqual(shots,(int[])Get(rules,"shots"));CollectionAssert.AreEqual(hits,(int[])Get(rules,"hits"));
            Call(match,"ExtraAction","replayskip");Time.timeScale=1;yield return new WaitForFixedUpdate();
            Assert.That(Get(replay,"Playing"),Is.False);CollectionAssert.AreEqual(shots,(int[])Get(rules,"shots"));CollectionAssert.AreEqual(hits,(int[])Get(rules,"hits"));
            void Check(string label)
            {
                var position=camera.transform.position;var focus=(Vector3)Call(replay,"Position",0);var originalBounds=table.bounds;
                var truck=Get(match,"truck");var body=(Rigidbody)Get(truck,"body");var source=((Transform[])Get(replay,"sources"))[0];var ghost=((Transform[])Get(replay,"ghosts"))[0];
                var actualRecordedCenter=ghost.TransformPoint(source.InverseTransformPoint(body.worldCenterOfMass));var aim=(Transform)Get(rig,"aim");
                TestContext.WriteLine("PONG_REPLAY_CAMERA "+label+" camera="+position+" ghostPivot="+focus+" physicalCenter="+actualRecordedCenter+" cameraAim="+aim.position+" aimError="+Vector3.Distance(aim.position,actualRecordedCenter).ToString("R")+" tableTop="+originalBounds.max.y+" insideTable="+originalBounds.Contains(position));
                Assert.That(Vector3.Distance(aim.position,actualRecordedCenter),Is.LessThan(.15f),"The recorded NWH visual pivot is offset from the physical truck; replay must aim at the recorded physical center.");
                Assert.That(position.y,Is.GreaterThanOrEqualTo(originalBounds.max.y+.6f),"Every replay shot must begin and remain above the actual table, including a recorded floor miss.");
                Assert.That(originalBounds.Contains(position),Is.False,"Replay camera may not enter the table's actual collision volume.");
            }
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");UnityEngine.Object.DestroyImmediate(isolation);lease.Dispose();}
    }
}
#endif
