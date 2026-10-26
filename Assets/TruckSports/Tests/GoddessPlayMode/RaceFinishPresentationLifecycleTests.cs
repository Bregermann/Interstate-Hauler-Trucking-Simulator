#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RaceFinishPresentationLifecycleTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object At(object owner,string name,int slot)=>((Array)Get(owner,name)).GetValue(slot);
    static IList Rows(Component source){var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Runtime("LWS.TruckSports.SportsRaceFinishReceipt")));Call(source,"Read",list);return list;}
    static object Truck(Component match,string mode,int slot)=>mode=="Wife"?Get(At(match,"pairs",slot),"driver"):mode=="Mattress"||mode=="Bog"||mode=="Terrier"||mode=="Mine"?Get(At(match,"racers",slot),"truck"):At(match,"trucks",slot);
    static Rigidbody Body(object truck)=>(Rigidbody)Get(truck,"body");
    static void Seed(object truck,Vector3 position,Quaternion rotation,Vector3 velocity,bool powered=true)
    {
        Call(truck,"Prepare",position,rotation,powered);var body=Body(truck);body.linearVelocity=velocity;body.angularVelocity=Vector3.zero;
    }
    static bool ClearPairBox(object truck,Vector3 position,Quaternion rotation,Collider[] own)
    {
        var body=Body(truck);var shape=(BoxCollider)Get(truck,"shape");var scale=shape.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));var localPosition=Quaternion.Inverse(body.rotation)*(shape.transform.position-body.position);var localRotation=Quaternion.Inverse(body.rotation)*shape.transform.rotation;var center=position+rotation*(localPosition+localRotation*Vector3.Scale(shape.center,scale));var half=Vector3.Scale(shape.size*.5f,scale)+Vector3.one*.3f;
        return !Physics.OverlapBox(center,half,rotation*localRotation,~0,QueryTriggerInteraction.Ignore).Any(collider=>!own.Contains(collider));
    }
    static Vector3 ClearCarriedApproach(object pair,Vector3 ground,Quaternion rotation)
    {
        var driver=Get(pair,"driver");var passenger=Get(pair,"passenger");var own=((Component)driver).GetComponentsInChildren<Collider>(true).Concat(((Component)passenger).GetComponentsInChildren<Collider>(true)).ToArray();var anchor=(Vector3)Get(pair,"roofAnchor");
        for(float lift=8;lift<=28;lift+=4){var point=ground+Vector3.up*lift;if(ClearPairBox(driver,point,rotation,own)&&ClearPairBox(passenger,point+rotation*anchor,rotation,own))return point;}
        Assert.Fail("No bounded non-overlapping controlled carried approach exists above "+ground+". Keep native scenery collisions and the actual roof joint intact.");return ground;
    }
    static IEnumerator Until(Func<bool> ready,float seconds,string label)
    {
        float until=Time.realtimeSinceStartup+seconds;while(!ready()&&Time.realtimeSinceStartup<until)yield return null;Assert.That(ready(),Is.True,label);
    }
    static string Phase(Component match,string mode)=>Get(mode=="Frog"||mode=="Avalanche"||mode=="RedLight"?Get(match,"rules"):(object)match,"Phase").ToString();
    static IEnumerator NativeFinish(Component match,string mode,int slot)
    {
        var truck=Truck(match,mode,slot);
        if(mode=="Mattress"||mode=="Bog")
        {
            var course=Get(match,"course");var racer=At(match,"racers",slot);var gates=(float[])Get(course,"checkpoints");
            for(int gate=0;gate<gates.Length;gate++)
            {
                float z=gates[gate]-4;float x=mode=="Mattress"?(float)Call(course,"Center",z):Mathf.Sin(z*.025f)*8;
                // Bog's actual center is a pure source method, not a projected shortcut.
                if(mode=="Bog")x=(float)course.GetType().GetMethod("Center").Invoke(null,new object[]{z});
                var point=(Vector3)Call(course,"Surface",new Vector3(x,0,z));var rotation=mode=="Mattress"?(Quaternion)Call(course,"Rotation",z):Quaternion.identity;
                Seed(truck,point+Vector3.up*.7f,rotation,Vector3.forward*22,mode!="Mattress");
                int expected=gate+1;yield return Until(()=>(int)Get(racer,"Checkpoint")>=expected||Get(racer,mode=="Mattress"?"Phase":"State").ToString()=="Finished",3,"Native ordered gate "+gate+" "+mode);
            }
            if(Get(racer,mode=="Mattress"?"Phase":"State").ToString()!="Finished")
            {
                float finish=mode=="Mattress"?650:(float)Get(course,"FinishDistance");float z=finish-4;float x=mode=="Mattress"?(float)Call(course,"Center",z):(float)course.GetType().GetMethod("Center").Invoke(null,new object[]{z});
                Seed(truck,(Vector3)Call(course,"Surface",new Vector3(x,0,z))+Vector3.up*.7f,Quaternion.identity,Vector3.forward*22,mode!="Mattress");
            }
            yield return Until(()=>Get(racer,mode=="Mattress"?"Phase":"State").ToString()=="Finished",4,"Native accepted finish "+mode);yield break;
        }
        if(mode=="Wife")
        {
            var pair=At(match,"pairs",slot);var course=Get(match,"course");var passenger=Get(pair,"passenger");
            float until=Time.realtimeSinceStartup+20;
            while(Get(pair,"State").ToString()!="Finished"&&Time.realtimeSinceStartup<until)
            {
                Assert.That(Get(pair,"State").ToString(),Is.EqualTo("Racing"),"A legitimate native drop cannot be silently converted into a carried finish.");Assert.That(Get(pair,"Carried"),Is.True);float progress=(float)Get(pair,"Progress"),next=progress+4;var rotation=(Quaternion)Call(course,"Rotation",next);var point=ClearCarriedApproach(pair,(Vector3)Call(course,"Spawn",slot,next),rotation);
                Seed(truck,point,rotation,rotation*Vector3.forward*4);var roof=(Vector3)Call(pair,"RoofPosition");Seed(passenger,roof,rotation,rotation*Vector3.forward*4,false);yield return new WaitForFixedUpdate();yield return null;
            }
            Assert.That(Get(pair,"State").ToString(),Is.EqualTo("Finished"),"Controlled physical carried-pair progress must satisfy the original loop/gates before the real finish.");Assert.That(Get(pair,"Carried"),Is.True);TestContext.WriteLine("WIFE_FINISH_FIXTURE pair="+slot+"; bounded non-overlapping airborne initial poses, original live roof joint, native ordered4m gate progress/finish; no scenery collision changes and no full-track driving claim.");yield break;
        }
        if(mode=="Frog")
        {
            var world=Get(match,"world");var rules=Get(match,"rules");int active=(int)Get(rules,"Active");Assert.That(active,Is.EqualTo(slot));int courseIndex=(int)Get(rules,"Course");var course=At(world,"courses",courseIndex);var gates=(Vector3[])Get(course,"gates");var start=(Vector3)Get(course,"start");
            for(int gate=0;gate<gates.Length;gate++)
            {
                float plane=(float)Call(world,"CrossingEnd",gate);Seed(truck,new Vector3(start.x,.65f,plane-6),Quaternion.identity,Vector3.forward*22);
                int expected=gate+1;yield return Until(()=>(int)Get(match,"gate")>=expected||Get(rules,"Phase").ToString()!="Driving",3,"Native whole-body crossing gate "+gate);
                Assert.That(Get(rules,"Phase").ToString(),Is.EqualTo("Driving"),"A real solid impact cannot be converted into a finish.");
            }
            var finish=(Vector3)Get(course,"finish");Seed(truck,finish+new Vector3(0,.65f,-7),Quaternion.identity,Vector3.forward*22);
            yield return Until(()=>Get(rules,"Phase").ToString()=="TurnResult",4,"Native final whole-rear crossing");var history=(IEnumerable)Get(rules,"History");var last=history.Cast<object>().Last();Assert.That(Get(last,"success"),Is.True);yield break;
        }
        if(mode=="Terrier")
        {
            var course=Get(match,"course");var point=(Vector3)Call(course,"World",new Vector3(0,.48f,(float)Get(course,"Length")-8));var rotation=(Quaternion)Get(course,"DrivingRotation");
            Seed(truck,point,rotation,(Vector3)Get(course,"Forward")*18);yield return Until(()=>(bool)Get(At(match,"racers",slot),"Finished"),5,"Native complete tunnel passage");yield break;
        }
        Vector3 pose;Quaternion heading=Quaternion.identity;
        if(mode=="Avalanche"){var world=Get(match,"world");var finish=(Transform)Get(world,"finish");pose=finish.position+new Vector3(slot*6,1,-10);}
        else if(mode=="RedLight")
        {
            yield return Until(()=>Get(Get(match,"rules"),"Phase").ToString()=="Green",10,"Native GREEN phase before controlled finish approach");pose=new Vector3(slot*8,(float)Get(match,"startY"),(float)Get(match,"finishZ")-8);
        }
        else{float mile=(float)Runtime("LWS.TruckMinefield.MinefieldRules").GetField("Mile").GetRawConstantValue();pose=(Vector3)Call(match,"Surface",new Vector3(0,0,mile-8))+Vector3.up*.7f;}
        Seed(truck,pose,heading,Vector3.forward*24);
        yield return Until(()=>mode=="Avalanche"?((bool[])Get(Get(match,"rules"),"finished"))[slot]:mode=="RedLight"?((Array)Get(Get(match,"rules"),"outcomes")).GetValue(slot).ToString()=="Finished":Get(At(match,"racers",slot),"Phase").ToString()=="DrivenFinish",5,"Native swept finish "+mode);
    }
    static string Official(Component match,string mode,Component source)
    {
        object authority=mode=="Mattress"||mode=="Wife"||mode=="Bog"?Get(match,"series"):mode=="Terrier"||mode=="Mine"?(object)match:Get(match,"rules");
        var rows=Rows(source).Cast<object>().Select(row=>Get(row,"id")+":"+((float)Get(row,"seconds")).ToString("R")+":"+Get(row,"place")+":"+Get(row,"winner"));
        return JsonUtility.ToJson(authority)+"|"+string.Join(";",rows)+"|receipts="+Get(Get(match,"ScoreJournal"),"Count");
    }
    static void Capture(Component match,Component presentation,Component source,string mode)
    {
        int gate=(int)Get(source,"GateIndex");var data=At(source,"gates",gate);var point=((Transform)Get(data,"root")).position;var old=((Camera[])Get(match,"cameras")).First(camera=>camera.enabled);var go=new GameObject("Controlled finish-art diagnostic camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(old);camera.enabled=true;camera.rect=new Rect(0,0,1,1);var delta=new Vector3(21,17,-35);camera.transform.SetPositionAndRotation(point+delta,Quaternion.LookRotation(point+Vector3.up*6-(point+delta)));
        try{var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{((Component)Get(match,"menus")).GetComponent<Canvas>(),new[]{camera},size.x,size.y,"Logs/TruckSports/PlaytestRepair/RaceFinish/"+mode+"-controlled-showcase-"+size.y+".png",null});}
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [UnityTest,Timeout(180000)]public IEnumerator MattressNativeOrderedFinishThenIsolatedJugsNeverChangeOfficialState()=>Inspect("TruckMattressRacing","Mattress");
    [UnityTest,Timeout(180000)]public IEnumerator WifeNativeCarriedLoopFinishThenPostFinishPropIsolation()=>Inspect("TruckWifeCarrying","Wife");
    [UnityTest,Timeout(180000)]public IEnumerator BogNativeGatesAndFinishUseLockedTimesForCelebration()=>Inspect("TruckBogSnorkelling","Bog");
    [UnityTest,Timeout(180000)]public IEnumerator TerrierNativeTunnelWinnerNeverInventsSubsequentFinishes()=>Inspect("TruckTerrierRacing","Terrier");
    [UnityTest,Timeout(180000)]public IEnumerator AvalancheNativeWholeRearFinishPreservesDefaultSceneBodies()=>Inspect("TruckAvalancheEscape","Avalanche");
    [UnityTest,Timeout(180000)]public IEnumerator RedLightNativeLegalFinishKeepsRecordedPlacesAndTimes()=>Inspect("TruckRedLightGreenLight","RedLight");
    [UnityTest,Timeout(180000)]public IEnumerator MinefieldNativeDrivingMileWinnerHasNoInventedLosingFinishes()=>Inspect("TruckMinefield","Mine");
    [UnityTest,Timeout(180000)]public IEnumerator FrogNativeSequentialClearsWaitForOfficialComparison()=>Inspect("TruckFrogger","Frog");
    static IEnumerator Inspect(string sceneName,string mode)
    {
        var prefs=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"RaceFinish"+Guid.NewGuid().ToString("N")});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);Runtime("LWS.TruckSports.SportsPrefs").GetMethod("SetString").Invoke(null,new object[]{"TruckSports.Weather.Preset.v1","9aad5ff5dfa09ce468205f676714f891"});
        var isolation=new GameObject("Explicit finish-approach native input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));var pads=new[]{InputSystem.AddDevice<Gamepad>(),InputSystem.AddDevice<Gamepad>()};Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+sceneName+"/Scenes/"+sceneName+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var presentation=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckSports.SportsRaceFinishPresentation"));Assert.That(presentation,Is.Not.Null,"Run the explicit eight-scene finish authoring first.");var source=(Component)Get(presentation,"source");match=(Component)Get(source,"session");
            int count=mode=="Frog"?2:(int)Get(match,"MaximumPlayers");Set(Get(match,"setup"),"count",count);if(mode=="Frog")Set(match,"option",1);Call(match,"AllBots");
            for(int slot=0;slot<2;slot++){var player=At(Get(match,"setup"),"players",slot);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"WholeController"));Set(player,"pad",pads[slot]);}
            Assert.That(Call(match,"StartMatch"),Is.True);yield return Until(()=>Phase(match,mode)!="Countdown"&&Phase(match,mode)!="CountIn"&&Phase(match,mode)!="Setup",8,"Native race/countdown release");
            var idleJugs=(Component)Get(presentation,"jugs");var idleMode=Physics.simulationMode;var idleControl=new GameObject("Unqueued finish presenter must not advance this default-world control body");var idleBody=idleControl.AddComponent<Rigidbody>();idleBody.position=new Vector3(12345,60000,12345);idleBody.linearVelocity=new Vector3(3,2,1);var idleBodies=match.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity)).ToArray();int idleWarnings=0;Application.LogCallback idleLog=(message,stack,type)=>{if(message.Contains("PhysicsScene.Simulate"))idleWarnings++;};Application.logMessageReceived+=idleLog;
            try
            {
                // Test only: Script mode makes an accidental default-world step
                // actually advance the moving control body instead of only warning.
                Physics.simulationMode=SimulationMode.Script;for(int idle=0;idle<5;idle++)Call(idleJugs,"Tick",.1f,false);Assert.That(Physics.simulationMode,Is.EqualTo(SimulationMode.Script));Assert.That(idleWarnings,Is.Zero,"Before any loser allocates a local prop scene, its default PhysicsScene handle must never simulate the default world.");Assert.That(Get(idleJugs,"Isolated"),Is.False);foreach(var body in idleBodies){Assert.That(body.body.position,Is.EqualTo(body.position));Assert.That(body.body.rotation,Is.EqualTo(body.rotation));Assert.That(body.body.linearVelocity,Is.EqualTo(body.linearVelocity));Assert.That(body.body.angularVelocity,Is.EqualTo(body.angularVelocity));}
            }
            finally{Physics.simulationMode=idleMode;Application.logMessageReceived-=idleLog;UnityEngine.Object.DestroyImmediate(idleControl);}
            Assert.That(Physics.simulationMode,Is.EqualTo(idleMode));
            yield return NativeFinish(match,mode,0);
            if(mode=="Frog")
            {
                Assert.That(Get(presentation,"VictoryBursts"),Is.Zero,"The first attempted crossing is not a championship winner.");yield return Until(()=>Get(Get(match,"rules"),"Phase").ToString()=="Driving"&&(int)Get(Get(match,"rules"),"Active")==1,8,"Second original sequential attempt");yield return new WaitForSeconds(1f);yield return NativeFinish(match,mode,1);
                var frogRules=Get(match,"rules");for(int retry=0;retry<8&&!(bool)Get(frogRules,"Complete");retry++)
                {
                    TestContext.WriteLine("FROG_FINISH_COMPARISON phase="+Get(frogRules,"Phase")+" active="+Get(frogRules,"Active")+" history="+string.Join(";",((IEnumerable)Get(frogRules,"History")).Cast<object>().Select(attempt=>Get(attempt,"participant")+":"+((float)Get(attempt,"seconds")).ToString("R")+":"+Get(attempt,"success"))));
                    yield return Until(()=>(bool)Get(frogRules,"Complete")||Get(frogRules,"Phase").ToString()=="Driving",8,"Native comparison or requested physical tiebreak");if((bool)Get(frogRules,"Complete"))break;int actor=(int)Get(frogRules,"Active");yield return new WaitForSeconds(.4f+actor*.8f);yield return NativeFinish(match,mode,actor);
                }
                yield return Until(()=>(bool)Get(frogRules,"Complete"),5,"Original official sequential comparison");Assert.That((int)Get(frogRules,"Winner"),Is.GreaterThanOrEqualTo(0),"A real comparison must confirm a unique native champion before a winner ceremony.");
            }
            else
            {
                yield return new WaitForSecondsRealtime(.15f);Assert.That(Get(presentation,"VictoryBursts"),Is.EqualTo(1));Capture(match,presentation,source,mode);
                if(mode!="Terrier"&&mode!="Mine")yield return NativeFinish(match,mode,1);
            }
            Time.timeScale=0;((Behaviour)match).enabled=false;yield return null;yield return null;yield return new WaitForSecondsRealtime(.15f);Call(presentation,"Present",.1f);
            Assert.That(Get(presentation,"VictoryBursts"),Is.EqualTo(1));var nativeRows=Rows(source).Cast<object>().ToArray();var expectedWinners=nativeRows.Where(row=>(bool)Get(row,"winner")).Select(row=>(string)Get(row,"id")).OrderBy(id=>id).ToArray();var shown=((string)Get(presentation,"LastWinnerIds")).Split(',').OrderBy(id=>id).ToArray();CollectionAssert.AreEqual(expectedWinners,shown);
            string official=Official(match,mode,source);var allBodies=match.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Where(body=>body.GetComponent(Runtime("LWS.TruckSports.SportsEventTruck"))!=null).ToArray();var bodyState=allBodies.Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity,body.constraints,body.detectCollisions)).ToArray();
            var shapes=allBodies.Select(body=>(Collider)Get(body.GetComponent(Runtime("LWS.TruckSports.SportsEventTruck")),"shape")).ToArray();var pairs=new List<bool>();for(int a=0;a<shapes.Length;a++)for(int b=0;b<a;b++)pairs.Add(Physics.GetIgnoreCollision(shapes[a],shapes[b]));var simulation=Physics.simulationMode;
            Func<uint[]> matrix=()=>Enumerable.Range(0,32).Select(layer=>{uint mask=0;for(int other=0;other<32;other++)if(Physics.GetIgnoreLayerCollision(layer,other))mask|=1u<<other;return mask;}).ToArray();var originalMatrix=matrix();
            var jugs=(Component)Get(presentation,"jugs");Assert.That(((Array)Get(jugs,"slots")).Length,Is.EqualTo(3));
            int queued=(int)Get(jugs,"Queued");var fake=Activator.CreateInstance(Runtime("LWS.TruckSports.SportsRaceFinishReceipt"));Set(fake,"matchId",Get(match,"MatchId"));Set(fake,"scope",Get(source,"Scope"));Set(fake,"id","never-confirmed-finish");Set(fake,"gag",true);Set(fake,"seconds",1f);Set(fake,"truck",Truck(match,mode,0));Call(jugs,"Enqueue",fake);Assert.That(Get(jugs,"Queued"),Is.EqualTo(queued),"A fabricated DTO cannot make an unfinished/winning truck a losing finisher.");
            var realLoser=nativeRows.FirstOrDefault(row=>!(bool)Get(row,"winner"));if(realLoser!=null){var spoof=Activator.CreateInstance(realLoser.GetType());foreach(var field in realLoser.GetType().GetFields())field.SetValue(spoof,field.GetValue(realLoser));Set(spoof,"truck",nativeRows.Where(row=>(bool)Get(row,"winner")).Select(row=>Get(row,"truck")).First());Assert.That(Call(source,"IsConfirmedLoser",spoof),Is.False,"A genuine losing receipt key cannot be reassigned to another physical truck.");}
            if(mode=="Terrier"||mode=="Mine"){Assert.That(Get(presentation,"GagRequests"),Is.Zero);Assert.That(Get(jugs,"Throws"),Is.Zero);}
            else
            {
                yield return Until(()=>(int)Get(jugs,"HullContacts")>0,8,"Native isolated jug must actually strike the confirmed finished hull proxy");Assert.That(Get(jugs,"Isolated"),Is.True);Assert.That(Get(jugs,"Throws"),Is.GreaterThan(0));Assert.That(Get(jugs,"LiveJugs"),Is.LessThanOrEqualTo(3));Assert.That(Get(jugs,"Approaches"),Is.GreaterThan(0));
                if(!(bool)Get(match,"Finished"))
                {
                    Call(match,"Pause",0,false);Call(presentation,"Present",0f);var slots=(Array)Get(jugs,"slots");var pausedPositions=slots.Cast<object>().Select(slot=>((Rigidbody)Get(slot,"jug")).position).ToArray();yield return new WaitForSecondsRealtime(.25f);for(int i=0;i<slots.Length;i++)Assert.That(Vector3.Distance(((Rigidbody)Get(slots.GetValue(i),"jug")).position,pausedPositions[i]),Is.LessThan(.00001f));Call(match,"Resume");Time.timeScale=0;Call(presentation,"Present",0f);
                }
                Capture(match,presentation,source,mode+"-native-jug-contact");
            }
            for(int i=0;i<12;i++)Call(presentation,"Present",.1f);Assert.That(Get(presentation,"VictoryBursts"),Is.EqualTo(1));Assert.That(Official(match,mode,source),Is.EqualTo(official));Assert.That(Physics.simulationMode,Is.EqualTo(simulation));CollectionAssert.AreEqual(originalMatrix,matrix());
            foreach(var state in bodyState){Assert.That(state.body.position,Is.EqualTo(state.position));Assert.That(state.body.rotation,Is.EqualTo(state.rotation));Assert.That(state.body.linearVelocity,Is.EqualTo(state.linearVelocity));Assert.That(state.body.angularVelocity,Is.EqualTo(state.angularVelocity));Assert.That(state.body.constraints,Is.EqualTo(state.constraints));Assert.That(state.body.detectCollisions,Is.EqualTo(state.detectCollisions));}
            int pair=0;for(int a=0;a<shapes.Length;a++)for(int b=0;b<a;b++)Assert.That(Physics.GetIgnoreCollision(shapes[a],shapes[b]),Is.EqualTo(pairs[pair++]));
            string old=(string)Get(match,"MatchId");Call(match,"SetupScreen");Call(presentation,"Present",0f);Assert.That(Get(presentation,"VictoryBursts"),Is.Zero);Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Call(presentation,"Present",0f);Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(old));Assert.That(Get(presentation,"VictoryBursts"),Is.Zero);Assert.That(Get(jugs,"LiveJugs"),Is.Zero);
            string localScene=((Transform)Get(jugs,"poolRoot")).gameObject.scene.name;Call(match,"SetupScreen");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;match=null;if(localScene.StartsWith("Race finish prop isolation"))yield return Until(()=>!SceneManager.GetSceneByName(localScene).isLoaded,5,"Owned local physics scene must unload on sport exit");
            TestContext.WriteLine("RACE_FINISH_NATIVE "+mode+"; controlled initial body poses/velocities at ordered native gate/finish approaches, no Finish/Record/award callback injection; actual native acceptance and official comparison before once-only presentation; isolated native jug contacts with0resultsTimeScale, default bodies/collision pairs/scores/lockedtimes unchanged, bounded3pool, pause/rematch/sceneexit. Controlled showcase720/1080 captures are not full-track, human/device/listening/art approval.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    }
}
#endif
