#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BungeeOwnedBuildingTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    static Type Type(string name)=>System.Type.GetType(name+", Assembly-CSharp",true);

    [UnityTest]
    public IEnumerator OwnedCityModelsRetainRulesAndBreakUnderActualTruckContact()
    {
        using((IDisposable)Type("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BungeeOwnedCityNative"}))
        {
            Component match=null;GameObject isolation=null;
            try
            {
                isolation=new GameObject("Bungee owned city fixture input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type("LWS.TruckSports.SportsProbeInputIsolation"));
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBungeeDemolition/Scenes/TruckBungeeDemolition.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type("LWS.TruckBungeeDemolition.BungeeMatch"),FindObjectsSortMode.None).Single();var bays=(Array)Field(match,"bays");
                foreach(var bay in bays)
                {
                    Assert.That((float)Field(bay,"pumpAcceleration"),Is.EqualTo(5.5f));Assert.That((float)Field(bay,"restLength"),Is.EqualTo(48f));
                    Assert.That(AssetDatabase.GetAssetPath((GameObject)Field(bay,"towerPrefab")),Does.EndWith("Owned hotel demolition tower v2.prefab"));Assert.That(AssetDatabase.GetAssetPath((GameObject)Field(bay,"expansionPrefab")),Does.EndWith("Owned city demolition annex v2.prefab"));
                }
                Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=4;float until=Time.realtimeSinceStartup+8;
                while(Property(match,"Phase").ToString()!="Demolition"&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That(Property(match,"Phase").ToString(),Is.EqualTo("Demolition"));((Behaviour)match).enabled=false;Time.timeScale=1;
                var first=bays.GetValue(0);var modules=(Array)Field(first,"modules");Assert.That(modules.Length,Is.EqualTo(85));
                var materialPaths=((Component)first).GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(AssetDatabase.GetAssetPath).ToArray();
                Assert.That(materialPaths.Any(p=>p.StartsWith("Assets/JC_LP_MegaCity/Materials/")),Is.True,"Actual city textures must be visible on the spawned structures.");
                var target=modules.Cast<Component>().First(m=>m.name.StartsWith("Building 1 structural")&&(int)Field(m,"level")==2);
                var truck=(Component)Field(first,"truck");var body=(Rigidbody)Field(truck,"body");var spring=(SpringJoint)Field(first,"spring");spring.spring=spring.damper=0;
                // Exercise the established pump/reel against real PhysX in clear
                // space; no new acceleration or button semantics are introduced.
                var anchor=(Transform)Field(first,"craneAnchor");Call(truck,"Prepare",anchor.position+new Vector3(45,-25,0),Quaternion.identity,false);body.useGravity=false;float shortest=48;
                for(int step=0;step<25;step++){Call(first,"Tick",Vector2.right,step==0,Time.fixedDeltaTime);shortest=Mathf.Min(shortest,spring.maxDistance);yield return new WaitForFixedUpdate();}
                float positive=body.linearVelocity.x;Assert.That(positive,Is.GreaterThan(1.5f));Assert.That(shortest,Is.LessThan(47.5f));
                for(int step=0;step<25;step++){Call(first,"Tick",Vector2.left,false,Time.fixedDeltaTime);yield return new WaitForFixedUpdate();}
                Debug.Log("BUNGEE_EXISTING_CONTROL_RESPONSE forward="+positive+" reversed="+body.linearVelocity.x+" shortestReel="+shortest);Assert.That(body.linearVelocity.x,Is.LessThan(positive-1.5f));
                var shape=target.GetComponent<Collider>();Call(truck,"Prepare",shape.bounds.center+Vector3.back*7,Quaternion.identity,false);body.useGravity=false;body.linearVelocity=Vector3.forward*23;
                until=Time.realtimeSinceStartup+3;while(target.gameObject.activeInHierarchy&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That((bool)Property(Field(target,"core"),"HasCollapsed"),Is.True,"A genuine incoming truck contact must break the real model, not only a score fixture.");
                Assert.That((int)Property(first,"Score"),Is.GreaterThan(0));Assert.That((int)Property(target,"DamageEntries"),Is.GreaterThan(0));
                var debris=((IEnumerable)Field(first,"debris")).Cast<Component>().ToArray();Assert.That(debris.Length,Is.GreaterThan(0));
                Assert.That(debris.Any(d=>d.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Any(m=>AssetDatabase.GetAssetPath(m).StartsWith("Assets/JC_LP_MegaCity/Materials/"))),Is.True,"Falling debris must retain the actual facade materials.");
                int score=(int)Property(first,"Score");Call(first,"Award",target);Assert.That((int)Property(first,"Score"),Is.EqualTo(score));
                var bodies=debris.SelectMany(d=>d.GetComponentsInChildren<Rigidbody>()).ToArray();var starts=bodies.Select(b=>b.position).ToArray();float fastest=0,downward=0,travel=0;
                until=Time.realtimeSinceStartup+3;
                while(Time.realtimeSinceStartup<until)
                {
                    for(int i=0;i<bodies.Length;i++){fastest=Mathf.Max(fastest,bodies[i].linearVelocity.magnitude);downward=Mathf.Max(downward,-bodies[i].linearVelocity.y);travel=Mathf.Max(travel,Vector3.Distance(starts[i],bodies[i].position));}yield return new WaitForFixedUpdate();
                }
                Debug.Log("BUNGEE_OWNED_DEBRIS speed="+fastest+" down="+downward+" travel="+travel+" bodies="+string.Join(";",bodies.Select(b=>b.name+" kinematic="+b.isKinematic+" gravity="+b.useGravity+" position="+b.position+" velocity="+b.linearVelocity)));
                Assert.That(downward,Is.GreaterThan(1),"Real building pieces must fall under PhysX.");
                Assert.That(travel,Is.GreaterThan(1),"Actual contact momentum must clear building debris from the impact area.");Assert.That((float)Property(first,"TransferredImpactImpulse"),Is.InRange(.01f,150f));
                body.useGravity=true;((Behaviour)match).enabled=true;Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;
                Assert.That((int)Property(first,"Score"),Is.Zero);Assert.That(((IEnumerable)Field(first,"debris")).Cast<object>(),Is.Empty);Assert.That(((Array)Field(first,"modules")).Length,Is.EqualTo(85));
                var release=((Array)Field(first,"modules")).GetValue(0);Call(Field(release,"core"),"ReceiveAllDamage");Assert.That((float)Property(first,"TransferredImpactImpulse"),Is.Zero,"Countdown/finale/support-only destruction cannot reuse a stale contact impulse.");
                Call(match,"SetupScreen");
            }
            finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}if(isolation!=null)UnityEngine.Object.Destroy(isolation);}
        }
    }
}
#endif
