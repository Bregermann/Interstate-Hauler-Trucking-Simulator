#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class KeepawayArenaRepairTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    static Component Match()=>(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckKeepaway.KeepawayMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
    static IEnumerator Load(bool cab)
    {EditorSceneManager.LoadSceneInPlayMode("Assets/TruckKeepaway/Scenes/TruckKeepaway"+(cab?"FirstPerson":"")+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;}
    static float Area(Mesh mesh)
    {float result=0;var v=mesh.vertices;var t=mesh.triangles;for(int i=0;i<t.Length;i+=3)result+=Mathf.Abs(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).y)*.5f;return result;}

    [UnityTest]
    public IEnumerator BothSavedVariantsHaveThreeTimesPhysicalAreaAndCategoryCompleteDestruction()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"KeepawayArenaGeometry"}))
        foreach(bool cab in new[]{false,true})
        {
            yield return Load(cab);var match=Match();var field=Field(match,"chaosField");
            Assert.That((int)Field(field,"arenaRepairVersion"),Is.EqualTo(1));
            var ground=Field(field,"ground");var surface=(MeshRenderer)Field(ground,"surface");var mesh=surface.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(Area(mesh)/(float)Field(field,"baselinePlayableArea"),Is.EqualTo(3).Within(.002f));
            Assert.That(surface.GetComponent<MeshCollider>().sharedMesh,Is.SameAs(mesh),"Visible and physical expanded surfaces must agree.");
            Assert.That(mesh.bounds.size.x,Is.GreaterThan(110));Assert.That(mesh.bounds.size.z,Is.GreaterThan(62));
            var props=(Array)Field(field,"breakableProps");Assert.That(props.Length,Is.EqualTo((int)Field(field,"baselineObstacleCount")));
            foreach(var prop in props)
            {
                var collapse=Field(prop,"breakable");Assert.That(collapse,Is.Not.Null);Assert.That(((Array)Field(collapse,"fragments")).Length,Is.EqualTo(2));
                foreach(Component fragment in (Array)Field(collapse,"fragments"))Assert.That(fragment.GetComponentsInChildren<MeshFilter>(true).Any(v=>v.sharedMesh!=null),Is.True,"Actual model fragments must exist before gameplay.");
            }
            Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();
            var trucks=(Array)Field(match,"trucks");for(int i=0;i<4;i++)Assert.That((bool)Call(field,"Reachable",((Rigidbody)Field(trucks.GetValue(i),"body")).position),Is.True,"All expanded starting poses must be connected.");
            foreach(var prop in props)Assert.That(((Collider)Field(prop,"intactShape")).enabled,Is.True);
            Call(match,"SetupScreen");
        }
    }

    [UnityTest]
    public IEnumerator PhysicalTruckImpactBreaksTheActualModelAndRematchRestoresIt()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"KeepawayPhysicalFracture"}))
        {
            Component match=null;Keyboard keyboard=null;GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;isolation=new GameObject("Keepaway fracture fixture physical-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();
                yield return Load(false);match=Match();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;
                var trucks=(Array)Field(match,"trucks");var players=(Array)Field(Field(match,"setup"),"players");
                for(int i=0;i<4;i++){var source=players.GetValue(i).GetType().GetField("source");source.SetValue(players.GetValue(i),Enum.Parse(source.FieldType,"Keyboard"));Call(trucks.GetValue(i),"Prepare",new Vector3(-40+i*5,.48f,20),Quaternion.identity,true);}
                var field=Field(match,"chaosField");var target=(Component)((Array)Field(field,"breakableProps")).GetValue(0);var collapse=Field(target,"breakable");
                var targetBody=(Rigidbody)Field(collapse,"intactBody");targetBody.position=new Vector3(0,.6f,0);targetBody.rotation=Quaternion.identity;
                Call(trucks.GetValue(0),"Prepare",new Vector3(0,.48f,-6),Quaternion.identity,true);
                var truckBody=(Rigidbody)Field(trucks.GetValue(0),"body");truckBody.linearVelocity=Vector3.forward*10;((Rigidbody)Field(match,"ball")).position=new Vector3(20,.85f,10);Physics.SyncTransforms();
                float deadline=Time.realtimeSinceStartup+4;
                while(!(bool)collapse.GetType().GetProperty("HasCollapsed").GetValue(collapse)&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Assert.That((bool)collapse.GetType().GetProperty("HasCollapsed").GetValue(collapse),Is.True,"Real truck contact, not a direct damage API call, must break the obstacle.");
                Assert.That(((Collider)Field(target,"intactShape")).enabled,Is.False);
                Assert.That(((Transform)Field(collapse,"model")).gameObject.activeSelf,Is.False);
                foreach(Rigidbody fragment in (Array)Field(collapse,"fragments")){Assert.That(fragment.gameObject.activeSelf,Is.True);Assert.That(fragment.isKinematic,Is.False);}
                Call(match,"SetupScreen");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;
                Assert.That((bool)collapse.GetType().GetProperty("HasCollapsed").GetValue(collapse),Is.False);
                Assert.That(((Collider)Field(target,"intactShape")).enabled,Is.True);
                Assert.That(((Transform)Field(collapse,"model")).gameObject.activeSelf,Is.True);
                foreach(Rigidbody fragment in (Array)Field(collapse,"fragments"))Assert.That(fragment.gameObject.activeSelf,Is.False);
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }

    [UnityTest]
    public IEnumerator SharedPooledDamageAdapterStillResetsInBothOriginalPlowingScenes()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"KeepawaySharedCollapsePreservation"}))
        foreach(bool cab in new[]{false,true})
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckPedestrianPlowing/Scenes/TruckPedestrianPlowing"+(cab?"FirstPerson":"")+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var type=Type.GetType("LWS.TruckPedestrianPlowing.PlowBreakable, Assembly-CSharp",true);
            var target=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).First();
            Call(target,"ReceiveAllDamage");yield return new WaitForFixedUpdate();
            Assert.That((bool)type.GetProperty("HasCollapsed").GetValue(target),Is.True);
            foreach(Rigidbody fragment in (Array)Field(target,"fragments"))Assert.That(fragment.gameObject.activeSelf,Is.True);
            Call(target,"ResetPiece");
            Assert.That((bool)type.GetProperty("HasCollapsed").GetValue(target),Is.False);
            Assert.That(((Transform)Field(target,"model")).gameObject.activeSelf,Is.True);
            foreach(Rigidbody fragment in (Array)Field(target,"fragments"))Assert.That(fragment.gameObject.activeSelf,Is.False);
        }
    }
}
#endif
