#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class TerrierAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static void Set(object value,string name,object field)=>value.GetType().GetField(name,Any).SetValue(value,field);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(value,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    sealed class Fixture:IDisposable
    {
        readonly IDisposable prefs;readonly GameObject isolation;readonly InputSettings original,temporary;readonly UnityEngine.Random.State random;
        public readonly Gamepad[] pads=new Gamepad[4];public Behaviour match;public string[] ids=new string[12];
        public object Bridge=>Get(match,"appearanceBridge");public object Replay=>Get(match,"replay");public object Pool=>Get(match,"wrecks");public object[] Racers=>((Array)Get(match,"racers")).Cast<object>().ToArray();public Component Truck(int slot)=>(Component)Get(Racers[slot],"truck");public object Host(int slot)=>Get(Truck(slot),"appearanceHost");public Rigidbody Body(int slot)=>(Rigidbody)Get(Truck(slot),"body");
        public Fixture()
        {
            random=UnityEngine.Random.state;prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"TerrierSelected"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");
            original=InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
            isolation=new GameObject("Terrier original input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
        }
        public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Behaviour)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckTerrierRacing.TerrierMatch, Assembly-CSharp",true));Assert.That(Bridge,Is.Not.Null,"Run explicit Terrier appearance author and save the original scene first.");
            for(int i=0;i<12;i++){ids[i]=(string)Get(Registry("Create","Same Terrier Driver"),"id");Assert.That((bool)Registry("UseInSeat",i,ids[i]),Is.True);Save(i,i%2==0?"truck.monster":"novelty.icecream");}Call(match,"RefreshParticipants",false);SelectWeather(false);Sports("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
        }
        public void SelectWeather(bool snow){var presets=Get(Get(Get(match,"course"),"weather"),"presets");Assert.That(presets,Is.Not.Null);var entry=((Array)Get(presets,"entries")).Cast<object>().First(item=>snow?(bool)Get(item,"Snow"):(string)Get(item,"displayName")=="Clear");Call(presets,"Select",Get(entry,"id"));}
        public void Save(int slot,string body)
        {
            var draft=Registry("GetAppearance",ids[slot]);Set(draft,"bodyId",body);var list=(IList)Get(draft,"selections");list.Clear();foreach(var pair in new[]{new[]{"hat","hat.crown"},new[]{"mirrors","mirrors.chrome"},new[]{"wheels","wheels.gold"},new[]{"rear.ornament","rear.chrome"},new[]{"dashboard","dashboard.cat"}}){var item=Activator.CreateInstance(Sports("SportsTruckCosmeticSelection"));Set(item,"slotId",pair[0]);Set(item,"itemId",pair[1]);list.Add(item);}Registry("TrySetAppearance",ids[slot],draft);object[] args={draft,false};var normalized=Sports("SportsTruckAppearanceData").GetMethod("Normalize",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,args);Assert.That((bool)Call(Registry("GetAppearance",ids[slot]),"SameAs",normalized),Is.True);
        }
        public void Start(int humans=0)
        {
            match.enabled=true;Call(match,"SetupScreen");Call(match,"AllBots");Set(match,"option",humans>0?0:1);var players=(Array)Get(Get(match,"setup"),"players");
            for(int i=0;i<12;i++){var p=players.GetValue(i);Set(p,"difficulty",Enum.Parse(Get(p,"difficulty").GetType(),"Hard"));if(i<humans){Set(p,"source",Enum.Parse(Get(p,"source").GetType(),"WholeController"));Set(p,"pad",pads[i]);}}
            Assert.That((bool)Call(match,"StartMatch"),Is.True,"Original source validation with real independently assigned native test devices.");
        }
        public void Dispose(){Time.timeScale=1;UnityEngine.Random.state=random;if(match!=null){match.enabled=true;Call(match,"SetupScreen");}foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");prefs.Dispose();}
    }
    static object[] Pieces(Fixture f)=>((Array)Get(f.Pool,"pieces")).Cast<object>().ToArray();
    static object[] Graphics(Fixture f,int slot)=>((IEnumerable)Get(((Array)Get(f.Bridge,"states")).GetValue(slot),"graphics")).Cast<object>().ToArray();
    static string Physical(Component truck)
    {
        var body=(Rigidbody)Get(truck,"body");string value=body.GetInstanceID()+":"+body.mass.ToString("R",CultureInfo.InvariantCulture)+":"+body.centerOfMass+":"+body.linearDamping+":"+body.angularDamping+":"+truck.transform.localScale;
        foreach(var collider in truck.GetComponentsInChildren<Collider>(true)){value+="/"+collider.GetInstanceID()+":"+collider.isTrigger;if(collider is BoxCollider box)value+=":"+box.center+":"+box.size;}
        foreach(var wheel in (IEnumerable)Get(truck,"wheels")){var data=Get(wheel,"wheel");var spring=Get(wheel,"spring");value+="/"+((Component)wheel).GetInstanceID()+":"+Get(data,"radius")+":"+Get(data,"width")+":"+Get(spring,"maxLength")+":"+Get(spring,"maxForce");}return value;
    }
    static string PoolPhysics(object[] pieces)=>string.Join("/",pieces.Select(piece=>{var body=(Rigidbody)Get(piece,"body");var shape=(BoxCollider)Get(piece,"shape");return body.GetInstanceID()+":"+body.mass+":"+body.centerOfMass+":"+body.maxLinearVelocity+":"+body.maxAngularVelocity+":"+body.linearDamping+":"+body.angularDamping+":"+shape.GetInstanceID()+":"+shape.center+":"+shape.size;}));
    static void RendererMetadata(Renderer a,Renderer b)
    {
        CollectionAssert.AreEqual(a.sharedMaterials,b.sharedMaterials);var x=new MaterialPropertyBlock();var y=new MaterialPropertyBlock();a.GetPropertyBlock(x);b.GetPropertyBlock(y);Assert.That(x.GetColor("_BaseColor"),Is.EqualTo(y.GetColor("_BaseColor")));Assert.That(x.GetColor("_Color"),Is.EqualTo(y.GetColor("_Color")));for(int i=0;i<a.sharedMaterials.Length;i++){x.Clear();y.Clear();a.GetPropertyBlock(x,i);b.GetPropertyBlock(y,i);Assert.That(x.GetColor("_BaseColor"),Is.EqualTo(y.GetColor("_BaseColor")));Assert.That(x.GetColor("_Color"),Is.EqualTo(y.GetColor("_Color")));}
    }
    static void IntactMetadata(Fixture f)
    {
        foreach(var state in (Array)Get(f.Bridge,"states")){var a=((IEnumerable)Get(state,"sources")).Cast<Renderer>().ToArray();var b=((IEnumerable)Get(state,"targets")).Cast<Renderer>().ToArray();Assert.That(a.Length,Is.GreaterThan(12));Assert.That(b.Length,Is.EqualTo(a.Length));for(int i=0;i<a.Length;i++)RendererMetadata(a[i],b[i]);}
    }
    static void GraphicsAtEmit(Fixture f,int slot)
    {
        foreach(var graphic in Graphics(f,slot))
        {
            var source=(Renderer)Get(graphic,"source");var target=(Renderer)Get(graphic,"target");var ghost=(Renderer)Get(graphic,"ghost");var mesh=(Mesh)Get(graphic,"mesh");var input=(Mesh)Get(graphic,"input");Assert.That(mesh,Is.Not.SameAs(input));CollectionAssert.AreEqual(input.uv,mesh.uv);Assert.That(ghost.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(mesh));RendererMetadata(target,ghost);
            int material=(int)Get(graphic,"material");Assert.That(target.sharedMaterials[0],Is.SameAs(source.sharedMaterials[material<0?0:material]));if(material>=0&&mesh.subMeshCount>1)Assert.That(target.sharedMaterials[1],Is.SameAs(Get(graphic,"cap")));
            for(int i=0;i<input.vertexCount;i+=Mathf.Max(1,input.vertexCount/32))Assert.That(Vector3.Distance(target.transform.TransformPoint(mesh.vertices[i]),source.transform.TransformPoint(input.vertices[i])),Is.LessThan(.001f),"Selected surface coincides at the unchanged original physical Emit pose (render1mm bound).");
            Assert.That(target.GetComponents<Collider>(),Is.Empty);Assert.That(target.GetComponents<Rigidbody>(),Is.Empty);Assert.That(ghost.GetComponents<Collider>(),Is.Empty);Assert.That(ghost.GetComponents<Rigidbody>(),Is.Empty);Assert.That(ghost.transform.localPosition,Is.EqualTo(target.transform.localPosition));Assert.That(ghost.transform.localRotation,Is.EqualTo(target.transform.localRotation));Assert.That(ghost.transform.localScale,Is.EqualTo(target.transform.localScale));
        }
    }
    [UnityTest,Timeout(240000)]public IEnumerator SevenBodiesKeepTwelveOriginalOwnersSame144EmitBodiesAndFixed34PlusFourChannels()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();var pieces=Pieces(f);Assert.That(pieces.Length,Is.EqualTo(144));string poolPhysics=PoolPhysics(pieces);int[] bodyIds=pieces.Select(p=>((Rigidbody)Get(p,"body")).GetInstanceID()).ToArray();int[] ghosts=pieces.Select(p=>((Transform)Get(p,"ghost")).GetInstanceID()).ToArray();int[] ghostRenderers=pieces.Select(p=>((Renderer)Get(p,"ghostRenderer")).GetInstanceID()).ToArray();int[] intactRoots=((Transform[])Get(f.Replay,"ghosts")).Select(root=>root.GetInstanceID()).ToArray();object frameRoots=null,frameParts=null,times=null,masks=null;string[] truckPhysics=null;
            foreach(string body in new[]{"truck.monster","truck.lifted","semi.lowrider","truck.fire","novelty.hotdog","novelty.icecream","semi.standard"})
            {
                for(int slot=0;slot<12;slot++)f.Save(slot,slot%2==0?body:"novelty.icecream");Call(f.match,"RefreshParticipants",false);f.Start();f.match.enabled=false;Time.timeScale=0;
                if(truckPhysics==null)truckPhysics=Enumerable.Range(0,12).Select(slot=>Physical(f.Truck(slot))).ToArray();else CollectionAssert.AreEqual(truckPhysics,Enumerable.Range(0,12).Select(slot=>Physical(f.Truck(slot))).ToArray());Assert.That(Get(f.Bridge,"LastFallback"),Is.Empty);Assert.That((int)Get(f.Replay,"capacity"),Is.EqualTo(2200));Assert.That((int)Get(f.Replay,"partCount"),Is.EqualTo(12*38));Assert.That(((Array)Get(f.Replay,"parts")).Length,Is.EqualTo(2200*12*38));Assert.That(((Array)Get(f.Replay,"parts")).Length*28,Is.EqualTo(28089600));
                if(frameRoots==null){frameRoots=Get(f.Replay,"roots");frameParts=Get(f.Replay,"parts");times=Get(f.Replay,"times");masks=Get(f.Replay,"masks");}else{Assert.That(Get(f.Replay,"roots"),Is.SameAs(frameRoots));Assert.That(Get(f.Replay,"parts"),Is.SameAs(frameParts));Assert.That(Get(f.Replay,"times"),Is.SameAs(times));Assert.That(Get(f.Replay,"masks"),Is.SameAs(masks));}
                var bindings=((Array)Get(f.Bridge,"owners")).Cast<object>().ToArray();var channels=(Transform[][])Get(f.Replay,"sourceParts");var ghostChannels=(Transform[][])Get(f.Replay,"ghostParts");
                for(int slot=0;slot<12;slot++)
                {
                    Assert.That(Get(f.Host(slot),"ProfileId"),Is.EqualTo(f.ids[slot]));Assert.That(Get(f.Host(slot),"EffectiveBodyId"),Is.EqualTo(slot%2==0?body:"novelty.icecream"));var model=(Transform)Get(f.Host(slot),"AppearanceRoot");Assert.That(model.gameObject.activeInHierarchy,Is.True);Assert.That(model.lossyScale.sqrMagnitude,Is.GreaterThan(.00001f));Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);CollectionAssert.AreEqual((Transform[])Get(bindings[slot],"originalParts"),channels[slot].Take(34));CollectionAssert.AreEqual((Transform[])Get(bindings[slot],"originalGhostParts"),ghostChannels[slot].Take(34));Assert.That(channels[slot].Length,Is.EqualTo(38));Assert.That(channels[slot].Skip(34).All(t=>t.GetComponent(Sports("SportsTruckAppearanceMountTag"))!=null),Is.True,"Four actual selected wheel pivots are the only additional dynamic channels.");
                    string physics=Physical(f.Truck(slot));var position=f.Body(slot).position;var velocity=f.Body(slot).linearVelocity;Assert.That((bool)Call(f.Bridge,"BeforeEmit",slot),Is.True);Assert.That(Physical(f.Truck(slot)),Is.EqualTo(physics));Assert.That(f.Body(slot).position,Is.EqualTo(position));Assert.That(f.Body(slot).linearVelocity,Is.EqualTo(velocity));
                    var owned=pieces.Where(piece=>(int)Get(piece,"owner")==slot).ToArray();var expected=owned.Select(piece=>{var source=(Transform)Get(piece,"source");var center=source.TransformPoint(source.GetComponent<MeshFilter>().sharedMesh.bounds.center);var radial=center-f.Body(slot).worldCenterOfMass;if(radial.sqrMagnitude<.01f)radial=Vector3.up;var result=f.Body(slot).GetPointVelocity(center)*.65f+radial.normalized*5+Vector3.up*6;result.y=Mathf.Max(result.y,(float)Get(f.Pool,"minimumUpwardVelocity"));return result;}).ToArray();
                    Call(f.Pool,"Emit",slot);Call(f.Bridge,"AfterEmit",slot,true);for(int i=0;i<owned.Length;i++)Assert.That(Vector3.Distance(((Rigidbody)Get(owned[i],"body")).linearVelocity,expected[i]),Is.LessThan(1e-5f));GraphicsAtEmit(f,slot);
                }
                IntactMetadata(f);Assert.That((int)Get(f.Pool,"Emissions"),Is.EqualTo(12));Assert.That((int)Get(f.Bridge,"ActualEmissions"),Is.EqualTo(12));Assert.That(PoolPhysics(pieces),Is.EqualTo(poolPhysics));CollectionAssert.AreEqual(bodyIds,pieces.Select(p=>((Rigidbody)Get(p,"body")).GetInstanceID()).ToArray());CollectionAssert.AreEqual(ghosts,pieces.Select(p=>((Transform)Get(p,"ghost")).GetInstanceID()).ToArray());CollectionAssert.AreEqual(ghostRenderers,pieces.Select(p=>((Renderer)Get(p,"ghostRenderer")).GetInstanceID()).ToArray());CollectionAssert.AreEqual(intactRoots,((Transform[])Get(f.Replay,"ghosts")).Select(root=>root.GetInstanceID()).ToArray());Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);yield return null;yield return null;
            }
            TestContext.WriteLine("TERRIER_SELECTED_CONTROLLED all7bodies/12fixedowners/144unchangedEmit bodies and ghost references, exact34+4channels and fixed28,089,600byte part buffer; no natural gameplay result claim for this controlled fixture.");
            var ownedMeshes=Enumerable.Range(0,12).SelectMany(slot=>Graphics(f,slot)).Select(graphic=>(Mesh)Get(graphic,"mesh")).ToArray();Call(f.match,"SetupScreen");UnityEngine.Object.Destroy((Component)f.Bridge);yield return null;yield return null;Assert.That(ownedMeshes.All(mesh=>mesh==null),Is.True,"Mode-owned derived mesh clones are released on adapter teardown.");
        }
    }
    [UnityTest,Timeout(240000)]public IEnumerator GenuineTwelveAiFinishCreamLoserWreckReplayAndFreshHeatKeepSelectedBodies()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();UnityEngine.Random.InitState(104729);f.Start();Time.timeScale=3;int winner=-1;bool cream=false,loss=false;float deadline=Time.realtimeSinceStartup+150;
            while(Time.realtimeSinceStartup<deadline)
            {
                if((int)Get(f.match,"HeatWinner")>=0){winner=(int)Get(f.match,"HeatWinner");cream|=(bool)Get(f.Racers[winner],"Creamed");}loss|=(int)Get(f.Bridge,"ActualEmissions")>0;
                if(Get(f.match,"Phase").ToString()=="Replay"&&winner>=0)break;yield return null;
            }
            Assert.That(Get(f.match,"Phase").ToString(),Is.EqualTo("Replay"));Assert.That(winner,Is.GreaterThanOrEqualTo(0));Assert.That(cream,Is.True,"Official original legal winner must reach the actual icecream collider and squish.");Assert.That(loss,Is.True,"Original failed-racer callbacks must emit selected wrecks during natural twelve-AI flow.");Assert.That((int)Get(Get(f.match,"dessert"),"Squishes"),Is.EqualTo(1));Assert.That(((int[])Get(f.match,"wins"))[winner],Is.EqualTo(1));Assert.That((bool)Get(f.Racers[winner],"Finished"),Is.True);Assert.That((bool)Get(f.Racers[winner],"Alive"),Is.True);
            f.match.enabled=false;Time.timeScale=0;var roots=Get(f.Replay,"roots");var parts=Get(f.Replay,"parts");var times=Get(f.Replay,"times");var masks=Get(f.Replay,"masks");int locks=(int)Get(f.match,"Locks");int journal=(int)Get(Get(f.match,"ScoreJournal"),"Count");var scores=((int[])Get(f.match,"wins")).ToArray();var positions=Enumerable.Range(0,12).Select(i=>f.Body(i).position).ToArray();IntactMetadata(f);var ghostParts=(Transform[][])Get(f.Replay,"ghostParts");var recorded=(Array)Get(f.Replay,"parts");var frameTimes=(float[])Get(f.Replay,"times");int count=(int)Get(f.Replay,"count");float duration=(float)Get(f.Replay,"Duration");
            foreach(float time in new[]{0f,duration*.35f,duration*.7f,Mathf.Max(0,duration-.2f)})
            {
                Call(f.Replay,"Seek",time);int lo=0,hi=count-1;while(lo+1<hi){int mid=(lo+hi)/2;if(frameTimes[mid]<=time)lo=mid;else hi=mid;}float alpha=Mathf.InverseLerp(frameTimes[lo],frameTimes[hi],time);
                for(int slot=0;slot<12;slot++)for(int wheel=0;wheel<4;wheel++){int channel=slot*38+34+wheel;var a=recorded.GetValue(lo*12*38+channel);var b=recorded.GetValue(hi*12*38+channel);var ghost=ghostParts[slot][34+wheel];if(!((Transform[])Get(f.Replay,"ghosts"))[slot].gameObject.activeSelf)continue;Assert.That(Vector3.Distance(ghost.localPosition,Vector3.Lerp((Vector3)Get(a,"position"),(Vector3)Get(b,"position"),alpha)),Is.LessThan(1e-5f));Assert.That(Quaternion.Angle(ghost.localRotation,Quaternion.Slerp((Quaternion)Get(a,"rotation"),(Quaternion)Get(b,"rotation"),alpha)),Is.LessThan(.01f));}
                for(int angle=0;angle<4;angle++){Set(f.Replay,"Angle",angle);Set(f.Replay,"AutoAngles",false);Call(f.match,"Present",0f);Assert.That(Get(f.Replay,"Angle"),Is.EqualTo(angle));IntactMetadata(f);}
                CollectionAssert.AreEqual(positions,Enumerable.Range(0,12).Select(i=>f.Body(i).position).ToArray());CollectionAssert.AreEqual(scores,(int[])Get(f.match,"wins"));Assert.That((int)Get(f.match,"Locks"),Is.EqualTo(locks));Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.EqualTo(journal));
            }
            // Exercise the ORIGINAL 144-piece ghost recorder at an actual
            // recorded emission frame, rather than synthesizing replay poses.
            var wreckFrames=(Array)Get(f.Pool,"record");bool sawDebris=false;
            for(int frame=0;frame<count&&!sawDebris;frame++)for(int piece=0;piece<144&&!sawDebris;piece++)if((bool)Get(wreckFrames.GetValue(frame*144+piece),"active"))
            {
                Call(f.Replay,"Seek",frameTimes[frame]);var actual=Pieces(f)[piece];var ghost=(Transform)Get(actual,"ghost");Assert.That(ghost.gameObject.activeSelf,Is.True);Assert.That(ghost.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(ghost.GetComponentsInChildren<Rigidbody>(true),Is.Empty);var owner=(int)Get(actual,"owner");Assert.That(Graphics(f,owner).Any(graphic=>((Renderer)Get(graphic,"ghost")).transform.IsChildOf(ghost)),Is.True);sawDebris=true;
            }
            Assert.That(sawDebris,Is.True);int heat=(int)Get(f.match,"Heat");Call(f.Replay,"Seek",duration);Call(f.match,"Pause",0,false);float cursor=(float)Get(f.Replay,"TimeCursor");yield return new WaitForSecondsRealtime(.08f);Assert.That((float)Get(f.Replay,"TimeCursor"),Is.EqualTo(cursor));Assert.That(Get(f.Replay,"parts"),Is.SameAs(parts));Call(f.match,"Resume");f.match.enabled=true;Time.timeScale=3;deadline=Time.realtimeSinceStartup+12;while((int)Get(f.match,"Heat")==heat&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((int)Get(f.match,"Heat"),Is.EqualTo(heat+1));Assert.That(Get(f.Replay,"roots"),Is.SameAs(roots));Assert.That(Get(f.Replay,"parts"),Is.SameAs(parts));Assert.That(Get(f.Replay,"times"),Is.SameAs(times));Assert.That(Get(f.Replay,"masks"),Is.SameAs(masks));for(int slot=0;slot<12;slot++)Assert.That(Get(f.Host(slot),"ProfileId"),Is.EqualTo(f.ids[slot]));f.Start();Assert.That((int)Get(f.match,"Heat"),Is.EqualTo(1));Assert.That((int)Get(f.match,"Locks"),Is.Zero);Assert.That(((int[])Get(f.match,"wins")).Sum(),Is.Zero);
            TestContext.WriteLine("TERRIER_SELECTED_NATURAL actual12AI legalfinish/cream/loserEmit/recordedWheelPoses+debris/fourangles/pause/nextheat/rematch; no injected race poses or results.");
        }
    }
    [UnityTest,Timeout(150000)]public IEnumerator ClearAndSnowRefreshActualIndexedMetadataOnceWithoutChangingExistingPoseBuffersOrTimes()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();foreach(bool snow in new[]{false,true})
            {
                f.SelectWeather(snow);f.Start();f.match.enabled=false;var surface=Get(Get(f.match,"snow"),"surface");var roots=Get(f.Replay,"roots");var parts=Get(f.Replay,"parts");var times=Get(f.Replay,"times");var masks=Get(f.Replay,"masks");
                // Controlled recording-boundary fixture: actual native start
                // poses are sampled once while original countdown is held.
                // Weather batches run through the original TerrierSnow.Step.
                Call(f.Replay,"Record",0f,true);int samples=(int)Get(f.Replay,"count");var recorded=((Array)parts).Cast<object>().Take(samples*12*38).ToArray();var recordedTimes=((float[])times).ToArray();float duration=(float)Get(f.Replay,"Duration");float deadline=Time.realtimeSinceStartup+50;
                while((int)Get(f.Bridge,"WeatherMetadataRefreshes")==0&&Time.realtimeSinceStartup<deadline){Call(Get(f.match,"snow"),"Step",Time.deltaTime);Call(f.Replay,"Record",0f,false);yield return null;}
                Assert.That((bool)Get(surface,"EnvironmentBindingsReady"),Is.True);Assert.That((int)Get(f.Bridge,"WeatherMetadataRefreshes"),Is.EqualTo(1));IntactMetadata(f);Call(f.Bridge,"TryRefreshWeatherMetadata");Call(f.Bridge,"TryRefreshWeatherMetadata");Assert.That((int)Get(f.Bridge,"WeatherMetadataRefreshes"),Is.EqualTo(1));Assert.That((int)Get(f.Replay,"count"),Is.EqualTo(samples));Assert.That((float)Get(f.Replay,"Duration"),Is.EqualTo(duration));Assert.That(Get(f.Replay,"roots"),Is.SameAs(roots));Assert.That(Get(f.Replay,"parts"),Is.SameAs(parts));Assert.That(Get(f.Replay,"times"),Is.SameAs(times));Assert.That(Get(f.Replay,"masks"),Is.SameAs(masks));CollectionAssert.AreEqual(recorded,((Array)parts).Cast<object>().Take(samples*12*38).ToArray());CollectionAssert.AreEqual(recordedTimes,(float[])times);
                foreach(int slot in new[]{0,1,10,11}){Assert.That((bool)Call(f.Bridge,"BeforeEmit",slot),Is.True);foreach(var graphic in Graphics(f,slot))RendererMetadata((Renderer)Get(graphic,"target"),(Renderer)Get(graphic,"ghost"));}
            }
        }
    }
    [UnityTest,Timeout(120000)]public IEnumerator OneToFourActualAssignedCabOwnersKeepPrivateExteriorMasksDashboardOwnershipAndSpectatorCleanup()
    {
        using(var f=new Fixture())
        {
            yield return f.Load();for(int humans=1;humans<=4;humans++)
            {
                f.Start(humans);f.match.enabled=false;Time.timeScale=0;Call(f.match,"Present",.016f);Call(f.Bridge,"LateUpdate");yield return null;Assert.That((int)Get(f.match,"HumanViews"),Is.EqualTo(humans));var cameras=((Array)Get(f.match,"cabViews")).Cast<object>().ToArray();var bindings=((Array)Get(f.Bridge,"cabs")).Cast<object>().ToArray();var owners=(int[])Get(f.match,"viewSlots");
                for(int view=0;view<4;view++)
                {
                    var camera=(Camera)Get(cameras[view],"view");Assert.That(camera.enabled,Is.EqualTo(view<humans));if(view>=humans)continue;int slot=owners[view];Assert.That(Get(bindings[view],"owner"),Is.SameAs(f.Host(slot)));Assert.That(camera.cullingMask&(1<<(10+slot)),Is.Zero);for(int other=0;other<humans;other++)Assert.That((camera.cullingMask&(1<<(25+other)))!=0,Is.EqualTo(view==other));foreach(var renderer in ((Transform)Get(f.Host(slot),"AppearanceRoot")).GetComponentsInChildren<Renderer>(true))Assert.That(renderer.gameObject.layer,Is.EqualTo(10+slot));var graphic=(GameObject)Get(bindings[view],"graphic");Assert.That(graphic,Is.Not.Null);Assert.That(graphic.GetComponentsInChildren<Transform>(true).All(part=>part.gameObject.layer==25+view),Is.True);var expected=(Rect)Type.GetType("LWS.TruckCornhole.CornholeMatch, Assembly-CSharp",true).GetMethod("ViewRect").Invoke(null,new object[]{humans,view});Assert.That(camera.rect,Is.EqualTo(expected));
                }
                // Controlled original elimination callback, distinct from the
                // separate genuine twelve-AI race above. No score is injected.
                Call(f.Racers[owners[0]],"Wreck");Call(f.match,"Present",.016f);Call(f.Bridge,"LateUpdate");yield return null;Assert.That(Get(bindings[0],"owner"),Is.Null);Assert.That(((Camera)Get(cameras[0],"view")).cullingMask&(15<<25),Is.Zero);Assert.That((int)Get(Get(f.match,"ScoreJournal"),"Count"),Is.Zero);
            }
        }
    }
}
#endif
