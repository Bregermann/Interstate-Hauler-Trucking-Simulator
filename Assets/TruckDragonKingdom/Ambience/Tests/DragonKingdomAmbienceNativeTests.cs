#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Saved authored world, actual rigs and native Navigation/DOTween. The fixture
// changes only ordinary setup/start/pause/rematch choices, never actor poses,
// paths, clocks, bones, animation state or score.
public sealed class DragonKingdomAmbienceNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Component match,ambience;
    GameObject isolation;IDisposable preferences;string output;
    readonly StringBuilder evidence=new StringBuilder();
    static Type Runtime(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(assembly=>assembly.GetType(name,false)).First(type=>type!=null);
    static object Read(object instance,string name)=>instance.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(instance):instance.GetType().GetProperty(name,Any).GetValue(instance);
    static object Call(object instance,string name,params object[] args)=>instance.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(instance,args);
    static object[] Items(object instance,string name)=>((Array)Read(instance,name)).Cast<object>().ToArray();
    static string Argument(string name,string fallback){var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,name);return index>=0&&index+1<args.Length?args[index+1]:fallback;}
    void Log(string value){evidence.AppendLine(value);TestContext.WriteLine(value);File.WriteAllText(Path.Combine(output,"observations.txt"),evidence.ToString());}
    void Capture(string label)
    {
        var method=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(value=>value.Name=="Save"&&value.GetParameters().Length==5);
        string path=Path.Combine(output,label+".png");method.Invoke(null,new object[]{match,1280,720,path,null});Log("CONTROLLED_RENDER "+path+"; actual rendered quality requires inspection, no Windows/performance/human acceptance.");
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator SavedKingdomAmbientRigsNavigateOffroadPauseCullAndReuseAcrossOriginalRematch()
    {
        output=Path.GetFullPath(Path.Combine(Argument("-tsDragonOutput",Path.Combine(Application.persistentDataPath,"DragonNative")),"Ambience",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        string path=Argument("-tsDragonAmbienceScene","Assets/TruckDragonEscape/Scenes/TruckDragonEscape.unity");
        try
        {
            Assert.That(File.Exists(path),Is.True,"Explicitly author the accepted shared world and mode ambience before running this fixture.");
            preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DragonAmbienceNative"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
            isolation=new GameObject("Ambience actual-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckSports.SportsEventSession"));ambience=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonKingdom.Ambience.DragonKingdomAmbience"));Assert.That(match,Is.Not.Null);Assert.That(ambience,Is.Not.Null);
            Assert.That(Read(ambience,"session"),Is.SameAs(match),"Install must serialize actual session binding, not rely on an uncalled setup method.");Call(ambience,"ValidateBindings");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
            var walkers=Items(ambience,"walkers");var birds=Items(ambience,"birds");var sounds=Items(ambience,"sounds");var ids=walkers.Concat(birds).Cast<Component>().Select(component=>component.GetInstanceID()).ToArray();
            Assert.That(walkers.Length,Is.EqualTo(8));Assert.That(birds.Length,Is.EqualTo(7));Assert.That(sounds.Length,Is.EqualTo(9));Assert.That(walkers.Count(value=>(bool)Read(value,"villager")),Is.EqualTo(5));Assert.That((bool)Read(ambience,"Prepared"),Is.True);
            Assert.That(walkers.Concat(birds).Cast<Component>().Sum(component=>component.GetComponentsInChildren<Collider>(true).Length),Is.Zero,"Decorative actors cannot obstruct roads/trucks/objectives.");
            var authoring=Runtime("LWS.TruckDragonKingdom.Ambience.DragonKingdomAmbienceAuthoring");var world=Read(ambience,"world");
            foreach(var walker in walkers)
            {
                var agent=(NavMeshAgent)Read(walker,"agent");Assert.That(agent.isOnNavMesh,Is.True);Assert.That(agent.enabled,Is.True);
                var route=Items(walker,"route").Cast<Transform>().ToArray();var nav=new NavMeshPath();
                for(int index=0;index<route.Length;index++)
                {
                    Assert.That((bool)authoring.GetMethod("ClearOfRoadsAndObjectives").Invoke(null,new object[]{world,route[index].position,10f}),Is.True);
                    Assert.That(NavMesh.CalculatePath(route[index].position,route[(index+1)%route.Length].position,NavMesh.AllAreas,nav)&&nav.status==NavMeshPathStatus.PathComplete,Is.True);
                    Assert.That(Physics.Raycast(route[index].position+Vector3.up*.5f,Vector3.down,out var hit,1,~0,QueryTriggerInteraction.Ignore)&&hit.normal.y>.8f,Is.True,"Actual supported ground required.");
                }
            }
            foreach(var sound in sounds){var source=(AudioSource)Read(sound,"source");Assert.That(source.clip,Is.Not.Null);Assert.That(source.spatialBlend,Is.EqualTo(1));Assert.That(source.playOnAwake,Is.False);Assert.That(source.dopplerLevel,Is.Zero);}
            var transforms=walkers.Concat(birds).Cast<Component>().Select(value=>value.transform).ToArray();var initial=transforms.Select(value=>value.position).ToArray();
            var skins=walkers.Concat(birds).Cast<Component>().SelectMany(value=>value.GetComponentsInChildren<SkinnedMeshRenderer>(true)).ToArray();Assert.That(skins.Length,Is.GreaterThanOrEqualTo(15));
            var bones=skins.SelectMany(skin=>skin.bones).Distinct().Where(bone=>bone!=null).ToArray();var pose=bones.Select(bone=>bone.localRotation).ToArray();
            float until=Time.realtimeSinceStartup+35;bool movedWalker=false,movedBird=false,changedBone=false;
            while(Time.realtimeSinceStartup<until)
            {
                yield return null;
                for(int index=0;index<transforms.Length;index++)if(Vector3.Distance(initial[index],transforms[index].position)>.8f){if(index<walkers.Length)movedWalker=true;else movedBird=true;}
                for(int index=0;index<bones.Length;index++)if(Quaternion.Angle(pose[index],bones[index].localRotation)>4)changedBone=true;
                if(movedWalker&&movedBird&&changedBone&&(float)Read(match,"Intro")<=0)break;
            }
            Log("ACTUAL_MOVEMENT walker="+movedWalker+" bird="+movedBird+" changedRigBone="+changedBone+" activeWalkers="+walkers.Count(value=>(bool)Read(value,"Active"))+" activeBirds="+birds.Count(value=>(bool)Read(value,"Active")));
            Assert.That(movedWalker&&movedBird&&changedBone,Is.True,"At least one observed nearby real walker, flying bird and articulated rig must actually move in the saved starting region.");Capture("actual-nearby-life");
            foreach(var actor in walkers.Concat(birds))if(!(bool)Read(actor,"Active"))Assert.That(((Renderer[])Read(actor,"visuals")).All(renderer=>!renderer.enabled),Is.True,"Distant actors must be culled rather than animated across the complete kingdom.");
            var poseAtPause=bones.Select(value=>value.localRotation).ToArray();
            Call(match,"Pause",0,false);yield return null;yield return null;var paused=transforms.Select(value=>value.position).ToArray();var pausedBones=bones.Select(value=>value.localRotation).ToArray();var samples=sounds.Select(value=>((AudioSource)Read(value,"source")).timeSamples).ToArray();
            for(int index=0;index<bones.Length;index++)Assert.That(Quaternion.Angle(poseAtPause[index],pausedBones[index]),Is.LessThan(.01f),"Pause must preserve the observed rig pose, including across existing CampLivingArt.OnDisable.");
            yield return new WaitForSecondsRealtime(.4f);Assert.That((bool)Read(ambience,"Paused"),Is.True);
            for(int index=0;index<transforms.Length;index++)Assert.That(Vector3.Distance(paused[index],transforms[index].position),Is.LessThan(.001f));
            for(int index=0;index<bones.Length;index++)Assert.That(Quaternion.Angle(pausedBones[index],bones[index].localRotation),Is.LessThan(.01f));
            for(int index=0;index<sounds.Length;index++)Assert.That(((AudioSource)Read(sounds[index],"source")).timeSamples,Is.EqualTo(samples[index]));Capture("paused-life");
            Call(match,"Resume");yield return null;Assert.That((bool)Read(ambience,"Paused"),Is.False);
            int resets=(int)Read(ambience,"ResetCount");Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);
            Assert.That((int)Read(ambience,"ResetCount"),Is.EqualTo(resets+1));CollectionAssert.AreEqual(ids,walkers.Concat(birds).Cast<Component>().Select(component=>component.GetInstanceID()).ToArray());
            foreach(var actor in walkers.Concat(birds))Assert.That(Vector3.Distance(((Component)actor).transform.position,(Vector3)Read(actor,"authoredPosition")),Is.LessThan(.5f));
            Log("PASSED original scene ambience: actual supported offroad paths, real moving original rigs, spatial clips, distance culling, pause and same-population rematch. Normal-speed early fixture only; not complete matches/performance/listening/human acceptance.");
        }
        finally
        {
            Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        }
    }
}
#endif
