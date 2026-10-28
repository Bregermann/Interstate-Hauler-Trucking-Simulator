#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LWS.TruckBallet;
public sealed class BalletNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string n)=>Type.GetType("LWS.TruckSports."+n+", Assembly-CSharp",true);
    static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Any).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length&&m.GetParameters().Select((p,i)=>a[i]==null||p.ParameterType.IsInstanceOfType(a[i])).All(x=>x)).Invoke(o,a);
    static object Static(string t,string n,params object[] a)=>Sports(t).GetMethod(n,BindingFlags.Public|BindingFlags.Static).Invoke(null,a);
    static Component[] Parts(object array)=>((IEnumerable)array).Cast<Component>().ToArray();
    sealed class Scope:IDisposable
    {
        public Component match;public Keyboard keyboard;public readonly string path;readonly IDisposable prefs;readonly InputSettings original,fixture;readonly GameObject isolation;
        public Scope(string name)
        {
            original=InputSystem.settings;fixture=UnityEngine.Object.Instantiate(original);fixture.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;fixture.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;fixture.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=fixture;
            prefs=(IDisposable)Static("SportsPrefs","IsolateValidation","ImpossibleBallet"+Guid.NewGuid().ToString("N"));Static("SportsParticipantRegistry","ForgetCache");isolation=new GameObject("Owned Ballet native input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));keyboard=InputSystem.AddDevice<Keyboard>();path=Path.GetFullPath("Logs/TruckSports/Impossible07/BalletLifecycle/"+name+"-"+Guid.NewGuid().ToString("N")+".jsonl");Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
        public IEnumerator Load(){EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBallet/Scenes/TruckBallet.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckBallet.BalletMatch, Assembly-CSharp",true));Assert.That(match,Is.Not.Null);Trace("loaded");}
        public void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
        [Serializable]sealed class Row{public string phase,screen,source;public bool ready,live,paused;public float real,time,scale,arrival,moveSeconds;public int move;public int[] banks,steps,completed;public float[] scores,accuracy,stability,hold,turn;public BalletPose[] actual,intent,goal;}
        public void Trace(string phase){if(match==null)return;var rules=(BalletRules)Get(match,"rules");var rigs=Parts(Get(match,"rigs"));var row=new Row{phase=phase,screen=Get(Get(match,"menus"),"Screen").ToString(),source=Get(((Array)Get(Get(match,"setup"),"players")).GetValue(0),"source").ToString(),ready=(bool)Get(match,"DevicesReady"),live=(bool)Get(match,"Live"),paused=(bool)Get(match,"Paused"),real=Time.realtimeSinceStartup,time=Time.time,scale=Time.timeScale,arrival=(float)Get(match,"ArrivalSeconds"),moveSeconds=rules.MoveSeconds,move=(int)rules.Move,banks=rigs.Select(r=>(int)Get(r,"Bank")).ToArray(),steps=rigs.Select(r=>(int)Get(r,"PoseSteps")).ToArray(),completed=rules.completed,scores=rules.score,accuracy=rules.accuracy,stability=rules.stability,hold=rules.hold,turn=rules.turn,goal=Enumerable.Range(0,rules.Count).Select(rules.Goal).ToArray(),actual=rigs.Select(r=>(BalletPose)Get(r,"Achieved")).ToArray(),intent=rigs.Select(r=>(BalletPose)Get(r,"Intent")).ToArray()};string json=JsonUtility.ToJson(row);File.AppendAllText(path,json+"\n");TestContext.WriteLine("BALLET_NATIVE "+json);}
        public IEnumerator Advance(float seconds,float realLimit=8){float start=Time.time,end=Time.realtimeSinceStartup+realLimit;while(Time.time-start<seconds&&Time.realtimeSinceStartup<end)yield return null;Trace("advance-"+seconds);Assert.That(Time.time-start,Is.GreaterThanOrEqualTo(seconds),"Actual original fixed loop must advance at a bounded real-time boundary.");}
        public void Capture(string label,int w=1280,int h=720)=>Sports("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5).Invoke(null,new object[]{match,w,h,Path.GetFullPath("Logs/TruckSports/Impossible07/BalletControlled/"+label+".png"),null});
        public void Dispose(){Time.timeScale=1;Keys();if(match!=null)Call(match,"SetupScreen");if(keyboard.added)InputSystem.RemoveDevice(keyboard);UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(fixture);Static("SportsParticipantRegistry","ForgetCache");prefs.Dispose();Static("SportsParticipantRegistry","ForgetCache");}
    }
    static string PhysicalConfiguration(Component rig)
    {
        var truck=(Component)Get(rig,"truck");var body=(Rigidbody)Get(truck,"body");var shape=(BoxCollider)Get(truck,"shape");return body.GetInstanceID()+":"+body.mass.ToString("R")+":"+shape.GetInstanceID()+":"+shape.center.ToString("R")+":"+shape.size.ToString("R")+string.Join("/",Parts(Get(truck,"wheels")).Select(w=>w.GetInstanceID()+":"+Get(Get(w,"wheel"),"radius")+":"+Get(Get(w,"spring"),"maxLength")))+string.Join("/",((Rigidbody[])Get(rig,"attachedBodies")).Select(b=>b.GetInstanceID()+":"+b.mass.ToString("R")));
    }
    [UnityTest,Timeout(90000)]public IEnumerator ActualKeyboardSelectsControlBankHoldsRealPirouetteAndPauseFreezesTheWholeAssembly()
    {
        using(var s=new Scope("human-pirouette"))
        {
            yield return s.Load();var m=s.match;Set(Get(m,"setup"),"count",1);Call(m,"AllBots");var player=((Array)Get(Get(m,"setup"),"players")).GetValue(0);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"Keyboard"));s.Keys();Assert.That((bool)Call(m,"StartMatch"),Is.True);yield return s.Advance(5);
            var rig=Parts(Get(m,"rigs"))[0];var rules=(BalletRules)Get(m,"rules");Assert.That(rules.completed[0],Is.Zero);Assert.That(rules.score[0],Is.Zero,"An elapsed cue cannot award an automatic demonstration.");string physical=PhysicalConfiguration(rig);
            var action=(Key)((Array)Get(m,"actionKeys")).GetValue(0);s.Keys(action);float bankEnd=Time.realtimeSinceStartup+2;while((int)Get(rig,"Bank")!=1&&Time.realtimeSinceStartup<bankEnd)yield return null;s.Keys();Assert.That((int)Get(rig,"Bank"),Is.EqualTo(1));yield return s.Advance(.2f);
            float adjustEnd=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<adjustEnd)
            {
                var intent=(BalletPose)Get(rig,"Intent");if(Mathf.Abs(intent.turnRate-100)<2.1f&&Mathf.Abs(intent.lift-.42f)<.015f)break;
                var keys=new System.Collections.Generic.List<Key>();if(intent.turnRate<98)keys.Add((Key)Get(player,"right"));else if(intent.turnRate>102)keys.Add((Key)Get(player,"left"));if(intent.lift<.408f)keys.Add((Key)Get(player,"forward"));else if(intent.lift>.432f)keys.Add((Key)Get(player,"back"));s.Keys(keys.ToArray());yield return null;
            }
            s.Keys();float finish=Time.realtimeSinceStartup+10;while(rules.completed[0]==0&&Time.realtimeSinceStartup<finish)yield return null;s.Trace("actual-human-pirouette");Assert.That(rules.completed[0],Is.EqualTo(1));Assert.That(((BalletPose)Get(rig,"Achieved")).heading,Is.GreaterThanOrEqualTo(360));Assert.That(rules.score[0],Is.GreaterThan(25));s.Capture("human-actual-held-full-pirouette",1920,1080);
            Call(m,"Pause",0,false);yield return null;yield return null;var rigs=Parts(Get(m,"rigs"));var positions=rigs.Select(r=>((Rigidbody)Get(Get(r,"truck"),"body")).position).ToArray();var rotations=rigs.Select(r=>((Rigidbody)Get(Get(r,"truck"),"body")).rotation).ToArray();float clock=rules.MoveSeconds;var score=(float[])rules.score.Clone();yield return null;yield return null;CollectionAssert.AreEqual(positions,rigs.Select(r=>((Rigidbody)Get(Get(r,"truck"),"body")).position).ToArray());CollectionAssert.AreEqual(rotations,rigs.Select(r=>((Rigidbody)Get(Get(r,"truck"),"body")).rotation).ToArray());CollectionAssert.AreEqual(score,rules.score);Assert.That(rules.MoveSeconds,Is.EqualTo(clock));Assert.That(PhysicalConfiguration(rig),Is.EqualTo(physical));Call(m,"Resume");yield return s.Advance(.5f);Assert.That((bool)Get(m,"Live"),Is.True);
        }
    }
    [UnityTest,Timeout(210000)]public IEnumerator FourOrdinaryAIPerformAllEightActualMovesAcrossSevenSelectedBodiesWithCoherentCabWheelsTutusAndRematch()
    {
        using(var s=new Scope("four-ai-seven-bodies"))
        {
            yield return s.Load();var m=s.match;Set(Get(m,"setup"),"count",4);Call(m,"AllBots");var rigs=Parts(Get(m,"rigs"));var players=(Array)Get(Get(m,"setup"),"players");var catalog=Resources.Load("SportsTruckAppearanceCatalog");var bodies=(Array)Get(catalog,"bodies");Assert.That(bodies.Length,Is.EqualTo(7));string[] baseline=null;int lateSamples=0;var observer=m.gameObject.AddComponent<BalletLateObserver>();
            var sourceTutus=rigs.Select(r=>(Mesh)Get(Get(r,"costume"),"original")).ToArray();var sourceVertices=sourceTutus.Select(mesh=>mesh.vertices).ToArray();
            for(int batch=0;batch<2;batch++)
            {
                Call(m,"SetupScreen");var expectedIds=new string[4];for(int i=0;i<4;i++){expectedIds[i]=(string)Get(bodies.GetValue((batch*4+i)%7),"id");string profile=(string)Get(players.GetValue(i),"participantId");var draft=Static("SportsParticipantRegistry","GetAppearance",profile);Set(draft,"bodyId",expectedIds[i]);Static("SportsParticipantRegistry","TrySetAppearance",profile,draft);}
                Assert.That((bool)Call(m,"StartMatch"),Is.True);Time.timeScale=4;yield return s.Advance(1);if(baseline==null)baseline=rigs.Select(PhysicalConfiguration).ToArray();
                var originalOffsets=rigs.Select(r=>{var truck=(Component)Get(r,"truck");var body=(Rigidbody)Get(truck,"body");return ((Rigidbody[])Get(r,"attachedBodies")).Select(b=>Quaternion.Inverse(body.rotation)*(b.position-body.position)).ToArray();}).ToArray();
                var wheelLocals=rigs.Select(r=>Parts(Get(Get(r,"truck"),"wheels")).Select(w=>w.transform.localPosition).ToArray()).ToArray();var musicians=UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBallet.BalletOrchestraPlayer, Assembly-CSharp",true),FindObjectsSortMode.None).Cast<Component>().ToArray();Assert.That(musicians.Length,Is.EqualTo(8));var arms=musicians.Select(p=>(Transform)Get(p,"rightUpper")).ToArray();var before=arms.Select(t=>t.localRotation).ToArray();bool bonesChanged=false;
                float maximumCabError=0,minimumHemGap=float.PositiveInfinity;bool physicalAuthority=true,rollingTransforms=true,identities=true;
                observer.sample=()=>
                {
                    if(!(bool)Get(m,"Live"))return;lateSamples++;
                    for(int i=0;i<rigs.Length;i++)
                    {
                        var truck=(Component)Get(rigs[i],"truck");var body=(Rigidbody)Get(truck,"body");physicalAuthority&=body.isKinematic&&!(bool)Get(truck,"Powered");var cab=(Rigidbody[])Get(rigs[i],"attachedBodies");for(int j=0;j<cab.Length;j++)maximumCabError=Mathf.Max(maximumCabError,Vector3.Distance(Quaternion.Inverse(body.rotation)*(cab[j].position-body.position),originalOffsets[i][j]));
                        rollingTransforms&=wheelLocals[i].SequenceEqual(Parts(Get(truck,"wheels")).Select(w=>w.transform.localPosition));var host=Get(truck,"appearanceHost");identities&=host!=null&&(bool)Get(host,"Applied")&&(string)Get(host,"ProfileId")== (string)Get(players.GetValue(i),"participantId")&&(string)Get(host,"EffectiveBodyId")==expectedIds[i]&&(int)Get(rigs[i],"BoundAppearanceRevision")== (int)Get(host,"AppearanceRevision");
                        var cloth=(MeshFilter)Get(Get(rigs[i],"costume"),"cloth");float floor=(float)Get(Get(rigs[i],"costume"),"stageFloor");minimumHemGap=Mathf.Min(minimumHemGap,cloth.sharedMesh.vertices.Select(v=>cloth.transform.TransformPoint(v).y).Min()-floor);
                    }
                    for(int i=0;i<arms.Length;i++)bonesChanged|=Quaternion.Angle(before[i],arms[i].localRotation)>5;
                };
                var rules=(BalletRules)Get(m,"rules");float deadline=Time.realtimeSinceStartup+70,next=0;int lastMove=-1,lastCompletions=-1;
                while(!(bool)Get(m,"Finished")&&Time.realtimeSinceStartup<deadline){int completions=rules.completed.Sum();if((int)rules.Move!=lastMove||completions!=lastCompletions||Time.realtimeSinceStartup>=next){s.Trace("ordinary-AI-choreography");lastMove=(int)rules.Move;lastCompletions=completions;next=Time.realtimeSinceStartup+3;}yield return null;}
                observer.sample=null;s.Trace("ordinary-AI-end");Assert.That((bool)Get(m,"Finished"),Is.True);Assert.That(rules.completed.All(c=>c==8),Is.True,"Every actual AI must achieve all8moves, not time out through a canned routine.");Assert.That(rules.score.All(v=>v>200),Is.True);Assert.That(bonesChanged,Is.True);Assert.That(lateSamples,Is.GreaterThan(20));Assert.That(physicalAuthority&&rollingTransforms&&identities,Is.True);Assert.That(maximumCabError,Is.LessThan(.001f));Assert.That(minimumHemGap,Is.GreaterThanOrEqualTo(.025f));CollectionAssert.AreEqual(baseline,rigs.Select(PhysicalConfiguration).ToArray());s.Capture("actual-all-eight-AI-gala-bodies-"+batch,1920,1080);TestContext.WriteLine($"BALLET_LATE batch={batch} samples={lateSamples} maximumCabErrorMetres={maximumCabError:R} minimumVisibleHemGapMetres={minimumHemGap:R} originalNWHSettingsAndLocalWheelTransformsExact=true");
            }
            for(int i=0;i<sourceTutus.Length;i++)CollectionAssert.AreEqual(sourceVertices[i],sourceTutus[i].vertices);Assert.That((bool)Call(m,"StartMatch"),Is.True);Assert.That(((BalletRules)Get(m,"rules")).completed.All(v=>v==0),Is.True);Assert.That(rigs.All(r=>(int)Get(r,"PoseSteps")==0),Is.True);
            TestContext.WriteLine("BALLET_NATIVE ordinary AI/vendor input and original kinematic exhibition assembly: all8observed choreography goals, 7catalog bodies across4 stable owners, original physical refs/settings/Cab offsets/wheel transforms, late-stage cloth clearance and genuine orchestra bones, pause and rematch. Controlled native only; no Windows/human/device/listening/performance claim.");
        }
    }
}
#endif
