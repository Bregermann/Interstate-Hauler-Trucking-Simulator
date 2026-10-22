#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class HockeyPhysicalGoalTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);

    [UnityTest]
    public IEnumerator RealPuckScoresThroughCenterAndBothLegalMouthEdgesAtThreeSpeeds()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"HockeyMouthEdges"}))
        {
            Component match=null;GameObject isolation=null;
            try
            {
                isolation=new GameObject("Hockey physical-goal device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHockey/Scenes/TruckHockey.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckHockey.HockeyMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();var mode=match.GetType().GetField("mode");mode.SetValue(match,Enum.Parse(mode.FieldType,"AIVsAI"));Call(match,"Configure");
                var puck=(Rigidbody)Field(match,"puck");var shape=puck.GetComponent<Collider>();var rules=Field(match,"rules");float line=(float)Field(match,"goalLine");Time.timeScale=3;
                foreach(int sign in new[]{-1,1})foreach(float speed in new[]{2f,8f,16f})foreach(float side in new[]{-.98f,0,.98f})
                {
                    Call(match,"ChangeSetup");Assert.That((bool)Call(match,"StartMatch",false),Is.True);Time.timeScale=3;foreach(Behaviour agent in (Array)Field(match,"agents"))agent.enabled=false;
                    float until=Time.realtimeSinceStartup+5;while(!(bool)Property(rules,"Live")&&Time.realtimeSinceStartup<until)yield return null;Assert.That((bool)Property(rules,"Live"),Is.True);
                    int index=0;foreach(Component truck in (Array)Field(match,"trucks")){Call(truck,"ClearInput");((Behaviour)truck).enabled=false;var body=(Rigidbody)Field(truck,"body");body.isKinematic=true;body.position=new Vector3(-6+index++*2,body.position.y,-(float)Field(match,"halfWidth")+1.5f);}
                    var posts=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.name=="Goal post"&&c.bounds.center.x*sign>0).ToArray();Assert.That(posts.Length,Is.EqualTo(2));float inside=posts.Min(c=>Mathf.Abs(c.bounds.center.z)-c.bounds.extents.z);float radius=shape.bounds.extents.z;float z=side*(inside-radius-.08f);
                    puck.position=new Vector3(sign*(line-1.5f),puck.position.y,z);puck.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();puck.linearVelocity=Vector3.right*(sign*speed);
                    until=Time.realtimeSinceStartup+3;float deepest=float.NegativeInfinity;while((bool)Property(rules,"Live")&&Time.realtimeSinceStartup<until){deepest=Mathf.Max(deepest,puck.position.x*sign);yield return new WaitForFixedUpdate();}
                    int attacker=(int)Call(rules,"Attacker",sign);var score=(int[])Field(rules,"Score");
                    Debug.Log("HOCKEY_MOUTH sign="+sign+" speed="+speed+" lateral="+z+" physicalInner="+inside+" puckRadius="+radius+" deepest="+deepest+" goals="+Property(rules,"Goals"));
                    Assert.That((int)Property(rules,"Goals"),Is.EqualTo(1),"Legal real-puck passage must score at center and both clear frame edges.");Assert.That(score[attacker],Is.EqualTo(1));Assert.That(score[1-attacker],Is.Zero);Assert.That(Property(rules,"Phase").ToString(),Is.EqualTo("Goal"));Assert.That((string)Property(match,"Notice"),Does.Contain("GOAL"));
                    yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Assert.That((int)Property(rules,"Goals"),Is.EqualTo(1),"Delayed net contacts cannot duplicate a recorded goal.");
                    until=Time.realtimeSinceStartup+6;while(!(bool)Property(rules,"Live")&&Time.realtimeSinceStartup<until)yield return null;Assert.That((bool)Property(rules,"Live"),Is.True);Assert.That(puck.position.sqrMagnitude,Is.LessThan(1),"The next faceoff restores the actual puck.");
                }
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");if(isolation!=null)UnityEngine.Object.Destroy(isolation);}
        }
    }
}
#endif
