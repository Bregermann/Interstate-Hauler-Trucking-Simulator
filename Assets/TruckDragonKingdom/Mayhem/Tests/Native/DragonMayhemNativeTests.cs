#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed partial class DragonMayhemNativeTests
{
    Component director,match;IDisposable lease;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Field(object source,string name)=>source.GetType().GetField(name).GetValue(source);
    static object Property(object source,string name)=>source.GetType().GetProperty(name).GetValue(source);
    static object Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name).Invoke(source,args);
    void Trace(string reason)
    {
        if(director==null)return;
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
        object Private(string name)=>director.GetType().GetField(name,flags)?.GetValue(director);
        var actor=Property(director,"Actor");var motor=actor==null?null:Field(actor,"motor");
        var animal=motor==null?null:Field(motor,"animal");var state=animal==null?null:animal.GetType().GetProperty("ActiveState").GetValue(animal);var body=motor==null?null:(Rigidbody)Field(motor,"body");
        string observation="MAYHEM_NATIVE_OBSERVATION "+reason+" clock="+Private("clock")+" phase="+Private("phase")+" phaseAge="+Private("phaseAge")+" pass="+Private("pass")+" goal="+Private("goal")+" goldDrops="+Property(director,"PhysicalGoldDrops")+" grabs="+Property(director,"SheepGrabs")+" meals="+Property(director,"SheepMeals")+" actor="+(actor==null?"none":Property(actor,"Position"))+" flying="+(motor==null?"none":Property(motor,"IsFlying"))+" action="+(motor==null?"none":Property(motor,"CurrentAction"))+" actionPlaying="+(motor==null?"none":Property(motor,"ActionPlaying"))+" vendorState="+(state==null?"none":state.GetType().Name)+" prepared="+(actor==null?"none":Property(actor,"Prepared"))+" root="+(actor==null?"none":((Component)actor).transform.position.ToString("F3"))+" velocity="+(body==null?"none":body.linearVelocity.ToString("F3"))+" sheepLanding="+Private("sheepLandingRequested")+" sheepGrounded="+Private("sheepLanded");
        var meal=Private("meal") as Component;
        observation+=" actualDropClipLead="+Private("dropContactLead")+" predictedDropMouth="+Private("predictedDropMouth")+" predictedReleaseSupported="+Private("dropForecastSupported");
        observation+=" totalExplosiveReactions="+Property(director,"ExplosiveReactions")+" consumedExplosiveMeals="+Property(director,"ConsumedExplosiveMeals")+" consumedExplosiveReactions="+Property(director,"ConsumedExplosiveReactions")+" lastConsumedId="+Property(director,"LastConsumedSheepId")+" lastConsumedExplosionId="+Property(director,"LastConsumedExplosionSheepId")+" selectedSheepId="+Property(director,"CurrentSheepId")+" selectedAvailable="+(meal==null?"none":Property(meal,"Available"))+" selectedConsumed="+(meal==null?"none":Property(meal,"Consumed"))+" selectedDetonated="+(meal==null?"none":Property(meal,"Detonated"))+" sheepJournal=["+string.Join("; ",((IEnumerable)Property(director,"SheepEvents")).Cast<object>())+"]";
        TestContext.WriteLine(observation);
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
        if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"MayhemLiveObservations");Directory.CreateDirectory(folder);File.AppendAllText(Path.Combine(folder,TestContext.CurrentContext.Test.MethodName+".txt"),observation+Environment.NewLine);}
    }
    IEnumerator Begin(string folder,bool enabled)
    {
        Assert.That(Application.isPlaying,Is.True);
        lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"Mayhem"+Guid.NewGuid().ToString("N")});
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/"+folder+"/Scenes/"+folder+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
        director=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemDirector"));
        Assert.That(director,Is.Not.Null,"ROOT must first explicitly migrate the real saved original scene with the additive installer.");
        Assert.That((bool)Property(director,"EnabledForSport"),Is.False,"A fresh isolated preference scope defaults OFF.");
        Assert.That((bool)Call(director,"SetEnabled",enabled),Is.True);
        var adapter=Field(director,"adapter");
        match=new[]{"keepaway","balloons","cornhole","cheese"}.Select(n=>Field(adapter,n)).OfType<Component>().Single();
        var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);
        var all=match.GetType().GetMethod("AllBots")??match.GetType().GetMethod("AllAI");all.Invoke(match,null);
        if(folder=="TruckKeepaway")match.GetType().GetField("option").SetValue(match,2);
        Assert.That((bool)Call(match,"StartMatch"),Is.True);
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator OffRunsAllFourOriginalGamesWithoutVariantRuntimeActorsOrSideScore()
    {
        foreach(string mode in new[]{"TruckKeepaway","TruckBalloonBattle","TruckCornhole","TruckCheeseRolling"})
        {
            yield return Begin(mode,false);yield return new WaitForSeconds(12);
            Assert.That((int)Property(director,"RuntimeActorCount"),Is.Zero);
            foreach(int value in (int[])Property(director,"SideGold"))Assert.That(value,Is.Zero);
            foreach(string name in new[]{"DragonMayhemGold","DragonMayhemSheep"})Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckDragonKingdom.Mayhem."+name),FindObjectsInactive.Include),Is.Empty);
            var presentation=director.GetComponent(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemPresentation"));
            Assert.That(((GameObject)Field(presentation,"overlay")).activeSelf,Is.False);
            TestContext.WriteLine("OFF_BASELINE_OBSERVED "+mode+" actual original AI play12seconds; zero runtime actors/physical gold/sheep/side points; variant overlay hidden.");
            yield return Cleanup();
        }
    }
    [UnityTest,Timeout(1000000)]
    public IEnumerator OnRequiresObservedAnimatedDropAndRealGoldPhysicsInAllFourGames()
    {
        foreach(string mode in new[]{"TruckKeepaway","TruckBalloonBattle","TruckCornhole","TruckCheeseRolling"})
        {
            yield return Begin(mode,true);
            var masses=UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include).ToDictionary(b=>b,b=>b.mass);
            float limit=Time.realtimeSinceStartup+210,nextTrace=0;
            while((int)Property(director,"PhysicalGoldDrops")==0 && Time.realtimeSinceStartup<limit && !(bool)Property(match,"Finished"))
            {
                Assert.That((int)Property(director,"RuntimeActorCount"),Is.InRange(0,1));if(Time.realtimeSinceStartup>=nextTrace){Trace(mode+" real-drop wait");nextTrace=Time.realtimeSinceStartup+10;}yield return new WaitForSeconds(.2f);
            }
            Assert.That((int)Property(director,"PhysicalGoldDrops"),Is.GreaterThan(0),"No injected drop or animation contact: a real configured dragon must reach the pass and play Drop.");
            var coins=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemGold"),FindObjectsInactive.Include).Cast<Component>().ToArray();
            Assert.That(coins.Length,Is.InRange(6,32));
            Assert.That(coins.Any(c=>c.gameObject.activeSelf&&!(bool)((Rigidbody)Field(c,"body")).isKinematic),Is.True,"Released gold must fall and bounce as an actual Rigidbody.");
            foreach(var pair in masses)if(pair.Key!=null)Assert.That(pair.Key.mass,Is.EqualTo(pair.Value),"Variant cannot replace or rescale an original truck.");
            Assert.That((bool)Call(director,"SetEnabled",false),Is.False,"Setup toggle cannot change physics mid-match.");
            yield return Cleanup();
        }
    }
    [UnityTest,Timeout(420000)]
    public IEnumerator ActualKeepawayExplosiveSheepIsGrabbedEatenAndReactsExactlyOnce()
    {
        yield return Begin("TruckKeepaway",true);
        var geometry=Field(director,"configuration");
        Assert.That((string)Field(geometry,"groundGrabSpeciesId"),Is.EqualTo("dragon-inferno"));
        Assert.That((AnimationClip)Field(geometry,"groundGrabContactClip"),Is.Not.Null,"Run the explicit accepted GroundGrab contact calibration before this actual pickup acceptance fixture.");
        Assert.That((float)Field(geometry,"groundGrabContactNormalizedTime"),Is.InRange(.1f,.9f));
        var originalSheep=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckKeepaway.KeepawaySheep"),FindObjectsInactive.Include).Cast<Component>().ToDictionary(s=>s,s=>s.transform.lossyScale);
        Assert.That(originalSheep.Count,Is.GreaterThan(0),"The real Keepaway sheep must supply the explosive meal.");
        bool heldScaleObserved=false;
        float limit=Time.realtimeSinceStartup+360,nextTrace=0;
        while((int)Property(director,"ConsumedExplosiveReactions")==0 && Time.realtimeSinceStartup<limit && !(bool)Property(match,"Finished"))
        {
            foreach(var original in originalSheep)
            {
                if(original.Key==null)continue;
                var wrapper=original.Key.GetComponent(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemSheep"));
                if(wrapper==null || !(bool)Property(Field(wrapper,"item"),"IsPicked"))continue;
                heldScaleObserved=true;
                Assert.That(Vector3.Distance(original.Key.transform.lossyScale,original.Value),Is.LessThan(.01f),"Actual vendor-held original sheep must retain its original world scale at grip and mouth.");
            }
            if(Time.realtimeSinceStartup>=nextTrace){Trace("actual consumed explosive sheep wait (unrelated blasts do not end observation)");nextTrace=Time.realtimeSinceStartup+10;}
            yield return new WaitForSeconds(.2f);
        }
        Trace("actual consumed explosive sheep terminal");
        Assert.That((int)Property(director,"SheepGrabs"),Is.GreaterThan(0),"Actual grounded reach action contact must use Malbers Pickable; no direct simulated pickup.");
        Assert.That((int)Property(director,"SheepMeals"),Is.GreaterThan(0),"Actual mouth-range Eat contact is required.");
        Assert.That(heldScaleObserved,Is.True,"Observe the original sheep during real vendor attachment, not merely a resulting meal counter.");
        Assert.That((int)Property(director,"ConsumedExplosiveMeals"),Is.EqualTo(1),"The actual eaten original explosive sheep uses its original Queue.");
        Assert.That((int)Property(director,"ConsumedExplosiveReactions"),Is.EqualTo(1),"Wait for the consumed original's actual detonation; unrelated original truck blasts remain separately counted.");
        int consumedId=(int)Property(director,"LastConsumedSheepId");
        Assert.That(consumedId,Is.Not.Zero);
        Assert.That((int)Property(director,"LastConsumedExplosionSheepId"),Is.EqualTo(consumedId),"Eat and actual detonation must belong to the same original sheep identity.");
        yield return new WaitForSeconds(.5f);
        Assert.That((int)Property(director,"ConsumedExplosiveReactions"),Is.EqualTo(1));
        Assert.That((int)Property(director,"LastConsumedExplosionSheepId"),Is.EqualTo(consumedId));
    }
    [UnityTest,Timeout(260000)]
    public IEnumerator RealAnimatedDropThenPhysicalGoldContactCreditsOneUniquePickup()
    {
        yield return Begin("TruckKeepaway",true);
        float deadline=Time.realtimeSinceStartup+210,nextTrace=0;
        while((int)Property(director,"PhysicalGoldDrops")==0 && Time.realtimeSinceStartup<deadline && !(bool)Property(match,"Finished")){if(Time.realtimeSinceStartup>=nextTrace){Trace("unique pickup real-drop wait");nextTrace=Time.realtimeSinceStartup+10;}yield return new WaitForSeconds(.1f);}
        Assert.That((int)Property(director,"PhysicalGoldDrops"),Is.GreaterThan(0),"The real Drop animation must release the gold first.");
        yield return new WaitForSeconds(.8f);
        var coin=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemGold"),FindObjectsInactive.Include).Cast<Component>().FirstOrDefault(c=>c.gameObject.activeSelf);
        Assert.That(coin,Is.Not.Null);var coinBody=(Rigidbody)Field(coin,"body");
        foreach(var other in UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemGold"),FindObjectsInactive.Include).Cast<Component>())if(other!=coin && other.gameObject.activeSelf)Call(other,"Retire");
        var adapter=Field(director,"adapter");int target=-1;
        int count=(int)Property(adapter,"Count");for(int i=0;i<count;i++)if((bool)Call(adapter,"CanCollect",i)){target=i;break;}
        Assert.That(target,Is.GreaterThanOrEqualTo(0));var truck=(Rigidbody)Call(adapter,"Body",target);
        int before=((int[])Property(director,"SideGold")).Sum();
        // A physical impulse is the native collision fixture. It changes only
        // this genuinely dropped coin's trajectory, never a truck pose, contact
        // callback, lease, score, or animation event.
        const float flight=.35f;var aim=truck.worldCenterOfMass+truck.linearVelocity*flight;
        var velocity=(aim-coinBody.position)/flight-Physics.gravity*(flight*.5f);
        coinBody.AddForce(velocity-coinBody.linearVelocity,ForceMode.VelocityChange);
        deadline=Time.realtimeSinceStartup+3;
        while(!(bool)Property(coin,"Collected") && Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That((bool)Property(coin,"Collected"),Is.True,"Real PhysX trigger contact with an eligible original truck must collect the observed drop.");
        Assert.That(((int[])Property(director,"SideGold")).Sum(),Is.EqualTo(before+1));
        yield return new WaitForSeconds(.3f);
        Assert.That(coin.gameObject.activeSelf,Is.False,"The used lease is returned to the pool; lingering contacts cannot recollect it.");
        Assert.That(((int[])Property(director,"SideGold")).Sum(),Is.EqualTo(before+1));
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        if(match==null && director==null && lease==null)yield break;
        Time.timeScale=1;
        if(match!=null){var stop=match.GetType().GetMethod("SetupScreen")??match.GetType().GetMethod("ChangeSetup");stop?.Invoke(match,null);}
        if(director!=null)Call(director,"Cleanup");
        yield return null;
        var current=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Mayhem native cleanup "+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(empty);
        if(current.IsValid()&&current.isLoaded)yield return SceneManager.UnloadSceneAsync(current);
        lease?.Dispose();lease=null;director=match=null;
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
    }
}
#endif
