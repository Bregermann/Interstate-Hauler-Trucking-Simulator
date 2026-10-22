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

public sealed class DockSpectatorLifeTests
{
    static object Read(object o,string n)=>o.GetType().GetField(n)?.GetValue(o)??o.GetType().GetProperty(n)?.GetValue(o);
    static void Private(object o,string n,object value)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    [UnityTest]public IEnumerator ActualCompatibleCrowdPosesAnimatePhotoGestureStaysBoundedAndPauseStopsIt()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"DockSpectatorLife"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckDockDiving/Scenes/TruckDockDiving.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                var type=Type.GetType("LWS.TruckDockDiving.DockSpectatorLife, Assembly-CSharp",true);
                var lives=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Cast<Component>().ToArray();Assert.That(lives.Length,Is.EqualTo(24));
                foreach(var life in lives)
                {
                    var actor=(Animator)Read(life,"actor");Assert.That(actor.cullingMode,Is.EqualTo(AnimatorCullingMode.AlwaysAnimate),"Nearby procedural gestures require a fresh base pose offscreen.");foreach(string state in (string[])Read(life,"activities"))Assert.That(actor.HasState(0,Animator.StringToHash(state)),Is.True,state);
                    Assert.That(((Transform)Read(life,"phone")).GetComponentsInChildren<MeshRenderer>(true).Length,Is.GreaterThan(0));
                }
                var selected=lives.First(l=>(int)Read(l,"variety")==0);var anchor=(Transform)Read(selected,"anchor");var home=anchor.position;
                var crowd=UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckDockDiving.DockAudiencePresentation, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                var batches=(Array)Read(Read(crowd,"crowd"),"batches");var mesh=(Mesh)Read(batches.GetValue(0),"idle");var before=mesh.vertices;
                yield return new WaitForSeconds(.5f);Assert.That(before.Zip(mesh.vertices,(a,b)=>(a-b).sqrMagnitude).Max(),Is.GreaterThan(.0001f),"Distant shared character mesh must actually animate rather than only switch static poses.");
                Private(selected,"choice",5);Private(selected,"next",0f);yield return null;yield return new WaitForSeconds(.4f);
                Assert.That((string)Read(selected,"Activity"),Is.EqualTo("Photo"));var phone=(Transform)Read(selected,"phone");Assert.That(phone.gameObject.activeSelf,Is.True);
                var actorRoot=((Animator)Read(selected,"actor")).transform;var head=actorRoot.GetComponentsInChildren<Transform>().Single(t=>t.name=="Bip01 Head");Assert.That(Vector3.Distance(phone.position,head.position),Is.LessThan(.5f));
                var right=actorRoot.GetComponentsInChildren<Transform>().Single(t=>t.name=="Bip01 R Hand");Assert.That(Vector3.Distance(phone.position,right.position),Is.LessThan(.18f));
                Private(selected,"choice",7);Private(selected,"next",0f);yield return null;yield return new WaitForSeconds(1.1f);Assert.That((string)Read(selected,"Activity"),Is.EqualTo("Walk"));Assert.That(Vector3.Distance(anchor.position,home),Is.LessThan(.7f));
                Time.timeScale=0;var stopped=anchor.position;yield return new WaitForSecondsRealtime(.15f);Assert.That(anchor.position,Is.EqualTo(stopped));Time.timeScale=1;
                yield return new WaitForSeconds(1.5f);Assert.That(Vector3.Distance(anchor.position,home),Is.LessThan(.01f));
            }
            finally{Time.timeScale=1;}
        }
    }
}
#endif
