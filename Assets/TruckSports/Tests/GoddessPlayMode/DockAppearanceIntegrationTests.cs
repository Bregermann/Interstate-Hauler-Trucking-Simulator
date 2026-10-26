#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class DockAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly GameObject isolation;public Behaviour match;public string[] ids=new string[8];
        public Component Truck=>(Component)Get(match,"truck");public Rigidbody Body=>(Rigidbody)Get(Truck,"body");public object Presenter=>Get(match,"appearanceSplit");public object Replay=>Get(match,"replay");public object Host=>Get(Truck,"appearanceHost");
        public Fixture(){prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DockSelectedBody"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Dock original AI input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));}
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckDockDiving/Scenes/TruckDockDiving.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Behaviour)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckDockDiving.DockMatch, Assembly-CSharp",true));Assert.That(Presenter,Is.Not.Null,"Run explicit Dock author after private Y recipes and save first.");
            for(int i=0;i<8;i++){ids[i]=(string)Get(Registry("Create","Same Dock Driver"),"id");Assert.That((bool)Registry("UseInSeat",i,ids[i]),Is.True);Save(i,i%2==0?"truck.monster":"novelty.icecream");}Call(match,"RefreshParticipants",false);
            var weather=UnityEngine.Object.FindObjectsByType(Sports("SportsChaosWeather"),FindObjectsInactive.Include,FindObjectsSortMode.None).Cast<Component>().FirstOrDefault(item=>item.gameObject.scene==match.gameObject.scene);if(weather!=null){var presets=Get(weather,"presets");Assert.That(presets,Is.Not.Null);var clear=((Array)Get(presets,"entries")).Cast<object>().First(entry=>(string)Get(entry,"displayName")=="Clear");Call(presets,"Select",Get(clear,"id"));}
        }
        public void Save(int slot,string body){var draft=Registry("GetAppearance",ids[slot]);Set(draft,"bodyId",body);((IList)Get(draft,"selections")).Clear();foreach(var pair in new[]{new[]{"hat","hat.crown"},new[]{"wheels","wheels.gold"},new[]{"rear.ornament","rear.chrome"}}){var item=Activator.CreateInstance(Sports("SportsTruckCosmeticSelection"));Set(item,"slotId",pair[0]);Set(item,"itemId",pair[1]);((IList)Get(draft,"selections")).Add(item);}Registry("TrySetAppearance",ids[slot],draft);object[] args={draft,false};var normalized=Sports("SportsTruckAppearanceData").GetMethod("Normalize",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Invoke(null,args);Assert.That((bool)Call(Registry("GetAppearance",ids[slot]),"SameAs",normalized),Is.True);}
        public void Start(int count){match.enabled=true;Call(match,"SetupScreen");Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);}
        public void Dispose(){Time.timeScale=1;if(match!=null){match.enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");prefs.Dispose();}
    }
    static string Physics(Component truck)
    {
        var rb=(Rigidbody)Get(truck,"body");string value=rb.GetInstanceID()+":"+rb.mass.ToString("R",CultureInfo.InvariantCulture)+":"+rb.centerOfMass+":"+rb.linearDamping+":"+rb.angularDamping+":"+truck.transform.localScale;
        foreach(var shape in truck.GetComponentsInChildren<Collider>(true)){value+="/"+shape.GetInstanceID()+":"+shape.isTrigger;if(shape is BoxCollider box)value+=":"+box.center+":"+box.size;}
        foreach(var wheel in (IEnumerable)Get(truck,"wheels")){var data=Get(wheel,"wheel");var spring=Get(wheel,"spring");value+="/"+((Component)wheel).GetInstanceID()+":"+Get(data,"radius")+":"+Get(data,"width")+":"+Get(spring,"maxLength")+":"+Get(spring,"maxForce");}return value;
    }
    static void Metadata(object replay)
    {
        var sources=((IEnumerable)Get(replay,"snapshotSources")).Cast<Renderer>().ToArray();var targets=((IEnumerable)Get(replay,"snapshotTargets")).Cast<Renderer>().ToArray();Assert.That(sources.Length,Is.GreaterThan(12));Assert.That(targets.Length,Is.EqualTo(sources.Length));
        for(int i=0;i<sources.Length;i++){CollectionAssert.AreEqual(sources[i].sharedMaterials,targets[i].sharedMaterials);Assert.That(targets[i].GetComponent<MeshFilter>().sharedMesh,Is.SameAs(sources[i].GetComponent<MeshFilter>().sharedMesh));var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();sources[i].GetPropertyBlock(a);targets[i].GetPropertyBlock(b);Assert.That(a.GetColor("_BaseColor"),Is.EqualTo(b.GetColor("_BaseColor")));for(int material=0;material<sources[i].sharedMaterials.Length;material++){a.Clear();b.Clear();sources[i].GetPropertyBlock(a,material);targets[i].GetPropertyBlock(b,material);Assert.That(a.GetColor("_BaseColor"),Is.EqualTo(b.GetColor("_BaseColor")));Assert.That(a.GetColor("_Color"),Is.EqualTo(b.GetColor("_Color")));}}
    }
    [UnityTest,Timeout(180000)]public IEnumerator SevenBodiesNineOriginalActorsKeepPhysicalConfigurationAndExactOriginalLocalSeparation()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var frames=Get(f.Replay,"frames");Assert.That(((Array)frames).Length,Is.EqualTo(1000));int ghost=((Transform)Get(f.Replay,"ghost")).GetInstanceID();
            foreach(string body in new[]{"truck.monster","truck.lifted","semi.lowrider","truck.fire","novelty.hotdog","novelty.icecream","semi.standard"})
            {
                f.Save(0,body);Call(f.match,"RefreshParticipants",false);f.Start(8);f.match.enabled=false;Time.timeScale=0;
                Assert.That((bool)Get(f.Presenter,"Applied"),Is.True,(string)Get(f.Presenter,"Notice"));Assert.That(Get(f.Host,"EffectiveBodyId"),Is.EqualTo(body));Assert.That(Get(f.Host,"ProfileId"),Is.EqualTo(f.ids[0]));
                var staged=((Array)Get(f.match,"staged")).Cast<Component>().ToArray();Assert.That(staged.Length,Is.EqualTo(8));for(int i=0;i<8;i++)Assert.That(Get(Get(staged[i],"appearanceHost"),"ProfileId"),Is.EqualTo(f.ids[i]));Assert.That(((Array)Get(f.match,"appearanceHosts")).Length,Is.EqualTo(9));
                string physical=Physics(f.Truck);var position=f.Body.position;var rotation=f.Body.rotation;var velocity=f.Body.linearVelocity;var angular=f.Body.angularVelocity;var split=Get(f.match,"split");var upper=(Transform)Get(f.Presenter,"UpperRoot");var lower=upper.parent.Find("Dock selected lower body");var upperStart=upper.position;var lowerStart=lower.position;var model=(Transform)Get(f.Truck,"visuals");
                Call(split,"Apply",.6f);Assert.That(Vector3.Distance(upper.position-upperStart,model.TransformVector(Vector3.up*.6f)),Is.LessThan(1e-6f));Assert.That(lower.position,Is.EqualTo(lowerStart));Assert.That(f.Body.position,Is.EqualTo(position));Assert.That(f.Body.rotation,Is.EqualTo(rotation));Assert.That(f.Body.linearVelocity,Is.EqualTo(velocity));Assert.That(f.Body.angularVelocity,Is.EqualTo(angular));Assert.That(Physics(f.Truck),Is.EqualTo(physical));Call(split,"Apply",0f);
                Assert.That(upper.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(upper.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(lower.GetComponentsInChildren<Collider>(true),Is.Empty);Metadata(f.Replay);Assert.That(Get(f.Replay,"frames"),Is.SameAs(frames));Assert.That(((Transform)Get(f.Replay,"ghost")).GetInstanceID(),Is.EqualTo(ghost));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
                yield return null;
            }
            TestContext.WriteLine("DOCK_SELECTED_SPLIT all7 bodies, original9actors/1000frames/physical configuration/local split scale and per-owner identities; controlled rendering fixture only.");
        }
    }
    [UnityTest,Timeout(120000)]public IEnumerator OriginalAiLaunchWaterLockFourRecordedAnglesAndNextActualOwnerRetainSelectedRendering()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();f.Start(2);var frames=Get(f.Replay,"frames");int snapshots=(int)Get(f.Replay,"AppearanceSnapshots");bool airborne=false;float maxSplit=0;float deadline=Time.realtimeSinceStartup+65;
            while(Get(f.match,"Phase").ToString()!="Replay"&&Time.realtimeSinceStartup<deadline){airborne|=Get(f.match,"Phase").ToString()=="Flight";maxSplit=Mathf.Max(maxSplit,(float)Get(Get(f.match,"split"),"Separation"));yield return new WaitForFixedUpdate();}
            Assert.That(airborne,Is.True);Assert.That(maxSplit,Is.GreaterThan(.4f));Assert.That(Get(f.match,"Phase").ToString(),Is.EqualTo("Replay"));Assert.That((int)Get(f.match,"Launches"),Is.EqualTo(1));Assert.That((int)Get(f.match,"Locks"),Is.EqualTo(1));Assert.That((int)Get(f.match,"CompletedAttempts"),Is.EqualTo(1));Assert.That((int)Get(f.match,"LastInches"),Is.GreaterThan(0),"Actual original AI water contact must produce the original valid distance.");
            float pausedClock=(float)Get(f.Replay,"Clock");int pausedCount=(int)Get(f.Replay,"Count");var pausedPosition=f.Body.position;Call(f.match,"Pause",0,false);yield return new WaitForSecondsRealtime(.08f);Assert.That((float)Get(f.Replay,"Clock"),Is.EqualTo(pausedClock));Assert.That((int)Get(f.Replay,"Count"),Is.EqualTo(pausedCount));Assert.That(f.Body.position,Is.EqualTo(pausedPosition));Assert.That((int)Get(f.Replay,"AppearanceSnapshots"),Is.EqualTo(snapshots));Call(f.match,"Resume");f.match.enabled=false;Time.timeScale=0;Metadata(f.Replay);var ghost=(Transform)Get(f.Replay,"ghost");Assert.That(ghost.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(ghost.GetComponentsInChildren<Rigidbody>(true),Is.Empty);int locked=(int)Get(f.match,"Locks");int count=(int)Get(f.Replay,"Count");
            var seenAngles=new System.Collections.Generic.HashSet<int>();Call(f.Replay,"NextAngle");for(int angle=0;angle<4;angle++){Call(f.Replay,"Rewind");seenAngles.Add((int)Get(f.Replay,"Angle"));Assert.That((int)Get(f.Replay,"Angle"),Is.InRange(0,3));Call(f.Replay,"Tick",.1f,false);Call(f.Replay,"NextAngle");Assert.That((int)Get(f.Replay,"Count"),Is.EqualTo(count));Assert.That((int)Get(f.match,"Locks"),Is.EqualTo(locked));Metadata(f.Replay);}
            CollectionAssert.AreEquivalent(new[]{0,1,2,3},seenAngles);Call(f.Replay,"Rewind");var rewind=ghost.position;Call(f.Replay,"Tick",.25f,false);Call(f.Replay,"Rewind");Assert.That(Vector3.Distance(ghost.position,rewind),Is.LessThan(1e-5f));Assert.That(Get(f.Replay,"frames"),Is.SameAs(frames));
            f.match.enabled=true;Time.timeScale=1;deadline=Time.realtimeSinceStartup+3;while((float)Get(f.match,"timer")<=1&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Get(f.match,"timer"),Is.GreaterThan(1));Call(f.match,"ExtraAction","replayskip");deadline=Time.realtimeSinceStartup+30;while((int)Get(f.match,"Active")==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That((int)Get(f.match,"Active"),Is.EqualTo(1));Assert.That(Get(f.Host,"ProfileId"),Is.EqualTo(f.ids[1]));Assert.That(Get(f.Host,"EffectiveBodyId"),Is.EqualTo("novelty.icecream"));Assert.That((int)Get(f.Replay,"AppearanceSnapshots"),Is.EqualTo(snapshots+1));f.Start(1);Assert.That(Get(f.Host,"ProfileId"),Is.EqualTo(f.ids[0]));Assert.That(Get(f.Replay,"frames"),Is.SameAs(frames));Assert.That((int)Get(f.match,"Locks"),Is.Zero);
        }
    }
}
#endif
