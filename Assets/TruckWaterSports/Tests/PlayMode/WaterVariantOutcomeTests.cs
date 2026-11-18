#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    public sealed class WaterVariantOutcomeTests
    {
        static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
        static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);
        static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
        static object Vessel(object match,int slot)=>Call(match,"Vessel",slot);
        static Rigidbody Body(object match,int slot)=>(Rigidbody)Field(Vessel(match,slot),"body");
        static float Clock(object match)=>(float)Property(match,"Clock");
        static void ClearWeather()
        {
            var catalog=AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset",Runtime("LWS.TruckSports.SportsWeatherPresets"));
            foreach(var entry in (Array)Field(catalog,"entries"))if((string)Field(entry,"displayName")=="Clear"){Call(catalog,"Select",(string)Field(entry,"id"));return;}
            Assert.Fail("Use the installed Clear weather selection.");
        }
        static string Observed(object match,int count)
        {
            var text=new StringBuilder("clock=").Append(Clock(match));
            for(int i=0;i<count;i++)text.Append(" | ").Append(i).Append(" pos=").Append(Body(match,i).position.ToString("F1"))
                .Append(" gate=").Append(((int[])Field(match,"Gates"))[i]).Append(" capsize=").Append(Property(Vessel(match,i),"Capsizes"))
                .Append(" finish=").Append(((float[])Field(match,"FinishTimes"))[i]).Append(" score=").Append(((float[])Field(match,"Scores"))[i])
                .Append(" speed=").Append(Body(match,i).linearVelocity.magnitude.ToString("F1")).Append(" air=").Append(Property(Vessel(match,i),"AirborneSeconds"))
                .Append(" landings=").Append(Property(Vessel(match,i),"Landings")).Append(" landingQuality=").Append(Property(Vessel(match,i),"LastLandingQuality"));
            if(match.GetType().Name=="BlackwaterMatch")text.Append(" banked=").Append(Property(match,"BankedSupplies")).Append(" creatureEuler=").Append(((Rigidbody)Field(match,"entity")).rotation.eulerAngles.ToString("F1"));
            return text.ToString();
        }
        // Three read-only snapshots per natural run. Do not invoke Bot: it owns
        // healing reservations. The nav estimate below repeats only its pure
        // sensor calculation and is labelled separately from actual velocities.
        public static void ObserveBlackwaterNavigation(object match,int count,int option)
        {
            var text=new StringBuilder("BLACKWATER_NAV_OBSERVATION option=").Append(option).Append(" clock=").Append(Clock(match));
            var entity=(Rigidbody)Field(match,"entity");var creatureHull=entity.GetComponent<BoxCollider>();
            text.Append(" entity=").Append(entity.position.ToString("F2")).Append(" entityVelocity=").Append(entity.linearVelocity.ToString("F2"));
            if(creatureHull!=null)text.Append(" crown=").Append(creatureHull.bounds.max.y.ToString("F2"));
            var integrity=(float[])Field(match,"Integrity");var cargoOwner=(int[])Field(match,"cargoOwner");var boatCargo=(int[])Field(match,"boatCargo");
            var banked=(bool[])Field(match,"banked");var healing=(Light[])Field(match,"healingAt");var quiet=(float[])Field(match,"quietSeconds");
            var crates=(Rigidbody[])Field(match,"supplies");var dock=(Transform)Field(match,"dock");var course=Field(match,"course");var gates=(int[])Field(match,"Gates");
            for(int i=0;i<count;i++)
            {
                var body=Body(match,i);Vector3 goal=(Vector3)Call(course,"Point",gates[i]);Rigidbody contact=null;string objective="gate";
                int rescue=-1;float nearest=float.MaxValue;
                if(option==3)for(int other=0;other<count;other++)if(other!=i&&integrity[other]<=0){float d=(Body(match,other).position-body.position).sqrMagnitude;if(d<nearest){nearest=d;rescue=other;}}
                if(rescue>=0){contact=Body(match,rescue);goal=contact.position;objective="rescue "+rescue;}
                else if(boatCargo[i]>=0||option==3&&(int)Property(match,"BankedSupplies")>=(int)Property(match,"SharedSuppliesNeeded")){goal=dock.position;objective="dock";}
                else if(option==1||option==3)
                {
                    int cargo=-1;nearest=float.MaxValue;for(int c=0;c<count;c++)if(!banked[c]&&cargoOwner[c]<0){float d=(crates[c].position-body.position).sqrMagnitude;if(d<nearest){nearest=d;cargo=c;}}
                    goal=cargo>=0?crates[cargo].position:dock.position;contact=cargo>=0?crates[cargo]:null;objective=cargo>=0?"crate "+cargo:"dock";
                }
                if(healing[i]!=null&&rescue<0){goal=healing[i].transform.position;contact=null;objective="healing "+healing[i].name;}
                Vector3 delta=Vector3.ProjectOnPlane(goal-body.position,Vector3.up),best=delta.normalized;
                float reach=Mathf.Clamp(body.linearVelocity.magnitude*1.2f+7,9,20),bestScore=float.NegativeInfinity;string blocker="none";
                for(int sample=-3;sample<=3;sample++)
                {
                    Vector3 direction=Quaternion.AngleAxis(sample*25,Vector3.up)*delta.normalized;float clear=reach;string hitName="none";
                    foreach(var hit in Physics.SphereCastAll(body.position+Vector3.up*.45f,2.1f,direction,reach,~0,QueryTriggerInteraction.Ignore))if(hit.rigidbody!=body&&(contact==null||hit.rigidbody!=contact)&&hit.distance<clear){clear=hit.distance;hitName=hit.collider.name;}
                    float score=Vector3.Dot(direction,delta.normalized)*3+clear/reach*4;
                    if(score>bestScore){bestScore=score;best=direction;blocker=hitName+" @"+clear.ToString("F2");}
                }
                float yaw=Vector3.SignedAngle(Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up),best,Vector3.up);
                float speed=Vector3.Dot(body.linearVelocity,Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up).normalized);
                float predictedThrottle=Mathf.Clamp((5.5f*Mathf.Max(0,Mathf.Cos(yaw*Mathf.Deg2Rad))-speed)*.6f,-1,1);
                text.Append(" | slot=").Append(i).Append(" position=").Append(body.position.ToString("F2")).Append(" euler=").Append(body.rotation.eulerAngles.ToString("F1"))
                    .Append(" velocity=").Append(body.linearVelocity.ToString("F2")).Append(" angular=").Append(body.angularVelocity.ToString("F2"))
                    .Append(" hull=").Append(integrity[i].ToString("F1")).Append(" goal=").Append(objective).Append(goal.ToString("F2"))
                    .Append(" navYaw=").Append(yaw.ToString("F1")).Append(" navThrottleEstimate=").Append(predictedThrottle.ToString("F2"))
                    .Append(" bestProbeBlocker=").Append(blocker).Append(" quietSeconds=").Append(quiet[i].ToString("F1"))
                    .Append(" sheltered=").Append(match.GetType().GetMethod("LightShelter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(match,new object[]{body.position}));
                var ownHull=body.GetComponent<BoxCollider>();if(ownHull!=null)for(int other=0;other<count;other++)if(other!=i)
                {
                    var otherHull=Body(match,other).GetComponent<BoxCollider>();if(otherHull!=null&&Physics.ComputePenetration(ownHull,body.position,body.rotation,otherHull,Body(match,other).position,Body(match,other).rotation,out _,out float depth))text.Append(" overlappingHull=").Append(other).Append(" depth=").Append(depth.ToString("F3"));
                }
            }
            Debug.Log(text.ToString());
        }
        [UnityTest]public IEnumerator NaturalTeamPaddleChallengeAwardsMovingSynchronizationAndSharedResults()=>Run("PaddlePals","PaddlePalsMatch",3,4);
        [UnityTest]public IEnumerator NaturalWaterfallFreestyleBanksOnlyActualControlledLandings()=>Run("WhitewaterWipeout","WhitewaterWipeoutMatch",2,1);
        [UnityTest]public IEnumerator NaturalKayakSurvivalResolvesRealTimeCapsizesAndRematch()=>Run("WhitewaterWipeout","WhitewaterWipeoutMatch",3,1);
        [UnityTest]public IEnumerator NaturalBlackwaterSupplyRunPhysicallyCouplesReturnsAndBanksCargo()=>Run("Blackwater","BlackwaterMatch",1,2);
        [UnityTest]public IEnumerator NaturalBlackwaterCooperativeEscapeReturnsSuppliesAndAllLivingBoats()=>Run("Blackwater","BlackwaterMatch",3,2);

        static IEnumerator Run(string sceneName,string typeName,int option,int count)
        {
            var lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"WaterVariant"+typeName+option});
            float scale=Time.timeScale,fixedStep=Time.fixedDeltaTime;Component match=null;
            try
            {
                Time.timeScale=1;ClearWeather();EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/"+sceneName+".unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports."+typeName),FindObjectsSortMode.None)[0];
                match.GetType().GetField("option").SetValue(match,option);var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,count);Call(match,"NormalizeSetup");Call(match,"AllBots");
                float duration=(float)Field(match,"matchDuration");var origin=Body(match,0).position;
                Assert.That((bool)Call(match,"StartMatch"),Is.True);string first=(string)Property(match,"MatchId");
                float furthest=0,nextLog=sceneName=="Blackwater"?60:0,highestWave=0;int blackwaterRows=0;bool wet=false,physicalCargo=false,neededRescue=false;
                float startingWave=(float)Field(Field(Vessel(match,0),"flotation"),"waveAmplitude");
                Time.timeScale=4;float deadline=Time.realtimeSinceStartup+100;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline&&Clock(match)<=(float)Property(match,"RoundDeadline")+5)
                {
                    furthest=Mathf.Max(furthest,Vector3.Distance(Body(match,0).position,origin));
                    var flotation=Field(Vessel(match,0),"flotation");wet|=(uint)Property(flotation,"WetTriangles")>0;
                    highestWave=Mathf.Max(highestWave,(float)Field(flotation,"waveAmplitude"));
                    if(sceneName=="Blackwater")
                    {
                        foreach(var joint in (FixedJoint[])Field(match,"cargoJoints"))if(joint!=null&&joint.connectedBody!=null)physicalCargo=true;
                        for(int i=0;i<count;i++)neededRescue|=((float[])Field(match,"Integrity"))[i]<=0;
                    }
                    if(sceneName=="WhitewaterWipeout"&&option==2&& (int)Property(Vessel(match,0),"Landings")==0)
                        Assert.That((float)Call(match,"QualifiedFreestylePoints",0),Is.Zero,"Flat-water rolls and unlanded aerial attempts must not bank freestyle points.");
                    if(Clock(match)>=nextLog){if(sceneName=="Blackwater"){if(blackwaterRows<3){blackwaterRows++;nextLog=60*(blackwaterRows+1);ObserveBlackwaterNavigation(match,count,option);}}else{nextLog=Clock(match)+30;Debug.Log("WATER_VARIANT_ACTUAL scene="+sceneName+" option="+option+" "+Observed(match,count));}}
                    yield return null;
                }
                Time.timeScale=1;string observed=Observed(match,count);
                Assert.That((bool)Property(match,"Finished"),Is.True,"Natural default-duration variant did not resolve. "+observed);
                Assert.That(Property(Field(match,"menus"),"Screen").ToString(),Is.EqualTo("Result"));
                Assert.That((float)Field(match,"matchDuration"),Is.EqualTo(duration));Assert.That(wet&&furthest>25,Is.True,"Actual water physics and meaningful travel required. "+observed);
                if(sceneName=="PaddlePals")
                {
                    int totalPairs=0;foreach(int pairs in (int[])Field(match,"qualifiedSynchronization"))totalPairs+=pairs;
                    Assert.That(totalPairs,Is.GreaterThan(0),observed);
                    Assert.That((float[])Field(match,"Scores"),Has.Some.GreaterThan(0));
                    Assert.That(((float[])Field(match,"Scores"))[0],Is.EqualTo(((float[])Field(match,"Scores"))[1]));
                    Assert.That(((float[])Field(match,"Scores"))[2],Is.EqualTo(((float[])Field(match,"Scores"))[3]));
                }
                else if(sceneName=="WhitewaterWipeout"&&option==2)
                {
                    Assert.That((int)Call(match,"QualifiedFreestyleLandings",0),Is.GreaterThan(0),observed);
                    Assert.That((float)Call(match,"QualifiedFreestylePoints",0),Is.GreaterThan(0),observed);
                    Assert.That((bool)Call(match,"ResultHasOutcome",0),Is.True,observed);
                }
                else if(sceneName=="WhitewaterWipeout")
                {
                    Assert.That((int)Property(Vessel(match,0),"Strokes"),Is.GreaterThan(0));
                    Assert.That((double)Call(match,"ResultScore",0),Is.GreaterThan(3).And.LessThanOrEqualTo(duration+.1f));
                    Assert.That(highestWave,Is.GreaterThan(startingWave+.02f),"Survival pressure must grow through the existing wave authority.");
                }
                else
                {
                    Assert.That(physicalCargo,Is.True,"Actual native crate/boat coupling must precede delivery.");
                    Assert.That((int)Property(match,"BankedSupplies"),Is.GreaterThan(0),observed);
                    if(option==3)
                    {
                        Assert.That((bool)Property(match,"CooperativeEscaped"),Is.True,observed);
                        int rescues=0;foreach(int value in (int[])Field(match,"Rescues"))rescues+=value;
                        if(neededRescue)Assert.That(rescues,Is.GreaterThan(0),"Naturally disabled teammates must be genuinely rescued before escape.");
                        for(int i=0;i<count;i++)Assert.That((bool)Call(match,"ResultWinner",i),Is.False,"Cooperative completion never invents an individual competitive winner.");
                        TestContext.WriteLine("BLACKWATER_RESCUE_OBSERVATION needed="+neededRescue+" actual_rescues="+rescues+"; no failure state was injected.");
                    }
                }
                TestContext.WriteLine("WATER_VARIANT_NATURAL software_ai_only=true scene="+sceneName+" option="+option+" "+observed);
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(first));
                Assert.That((float[])Field(match,"Scores"),Is.All.EqualTo(0));
                if(sceneName=="Blackwater"){Assert.That((int)Property(match,"BankedSupplies"),Is.Zero);Assert.That((bool)Property(match,"CooperativeEscaped"),Is.False);}
                if(sceneName=="WhitewaterWipeout"&&option==2)Assert.That((float)Call(match,"QualifiedFreestylePoints",0),Is.Zero);
                Time.timeScale=4;yield return new WaitForSeconds(4);Time.timeScale=1;
                Assert.That(Clock(match),Is.GreaterThan(3));Assert.That(Time.fixedDeltaTime,Is.EqualTo(fixedStep));
            }
            finally{if(match!=null)Call(match,"SetupScreen");Time.timeScale=scale;lease.Dispose();}
        }

        [UnityTest]
        public IEnumerator TwoAssignedSoftwareControllersIndependentlyPowerBothCanoeStations()
        {
            var lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"PaddleTwoHumanAgency"});
            float scale=Time.timeScale;var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Gamepad first=null,second=null;Component match=null;
            try
            {
                Time.timeScale=1;ClearWeather();EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/PaddlePals.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.PaddlePalsMatch"),FindObjectsSortMode.None)[0];
                first=InputSystem.AddDevice<Gamepad>("Paddle station1 software");second=InputSystem.AddDevice<Gamepad>("Paddle station2 software");
                yield return null;yield return new WaitForFixedUpdate();
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,2);var players=(Array)Field(setup,"players");
                for(int i=0;i<2;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));}
                match.GetType().GetField("option").SetValue(match,3);Call(match,"NormalizeSetup");
                Assert.That((bool)Call(match,"Assign",0,first),Is.True);Assert.That((bool)Call(match,"Assign",1,second),Is.True);
                Assert.That((bool)Call(match,"StartMatch"),Is.True);
                InputSystem.QueueStateEvent(first,new GamepadState());InputSystem.QueueStateEvent(second,new GamepadState());
                yield return null;yield return new WaitForFixedUpdate();yield return null;
                var canoe=Vessel(match,0);Assert.That(Vessel(match,1),Is.SameAs(canoe));
                InputSystem.QueueStateEvent(first,new GamepadState{rightTrigger=1});
                yield return null;yield return new WaitForFixedUpdate();yield return new WaitForSeconds(1.1f);
                int left=(int)Call(canoe,"StrokesAtStation",0);Assert.That(left,Is.GreaterThan(0));Assert.That((int)Call(canoe,"StrokesAtStation",1),Is.Zero);
                Assert.That(Body(match,0).linearVelocity.magnitude,Is.GreaterThan(.5f),"Assigned input must operate the actual floating canoe.");
                InputSystem.QueueStateEvent(first,new GamepadState());InputSystem.QueueStateEvent(second,new GamepadState{rightTrigger=1});
                yield return null;yield return new WaitForFixedUpdate();yield return new WaitForSeconds(1.1f);
                Assert.That((int)Call(canoe,"StrokesAtStation",0),Is.EqualTo(left));Assert.That((int)Call(canoe,"StrokesAtStation",1),Is.GreaterThan(0));
                InputSystem.QueueStateEvent(first,new GamepadState());InputSystem.QueueStateEvent(second,new GamepadState());
                yield return null;yield return new WaitForFixedUpdate();yield return new WaitForSeconds(.5f);
                InputSystem.QueueStateEvent(first,new GamepadState{rightTrigger=1});InputSystem.QueueStateEvent(second,new GamepadState{rightTrigger=1});
                yield return null;yield return new WaitForFixedUpdate();yield return new WaitForSeconds(1);
                Assert.That((int)Property(canoe,"Synchronizations"),Is.GreaterThan(0));
                TestContext.WriteLine("PADDLE_TWO_STATION_AGENCY software_devices=true independently_assigned=true native_strokes_and_float_physics=true physical_gamepads=unverified");
            }
            finally
            {
                if(match!=null)Call(match,"SetupScreen");if(first!=null)InputSystem.RemoveDevice(first);if(second!=null)InputSystem.RemoveDevice(second);
                InputSystem.settings.backgroundBehavior=background;Time.timeScale=scale;lease.Dispose();
            }
        }
    }
}
#endif
