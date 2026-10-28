#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LWS.TruckNestBuilding;

public sealed partial class NestNativeTests
{
    [Serializable] sealed class LaterRow
    {
        public string status="DIAGNOSTIC_ONLY_NOT_COMPLETION_ACCEPTANCE",checkpoint,menu;
        public float real,clock,timeScale,fixedDeltaTime,maximumDeltaTime;
        public bool live,paused,ready,finished;
        public BuilderFacts[] builders;
        public AcquisitionFacts[] acquisition;
        public SiteFacts[] sites;
        public MaterialFacts[] materials;
    }
    [Serializable] sealed class AcquisitionFacts
    {
        public int owner,socket;public string required,nearest;
        public bool fairClaimAllowed,available,withinOriginalReach,standDeadzoneButOutsideReach,vehicleEnabled,driveStateFieldsAvailable;
        public Vector3 itemPosition,fixedPickupStand,itemRelativeToTruck;
        public float distanceToFixedStand,itemToFixedStand,speed,throttle,brakes,handbrake,steering;
    }
    [Serializable] sealed class SiteFacts
    {
        public int index,rooted;public bool active,core,eggPresent,eggStable,hatched;
        public string[] slots,candidates;public int[] claims;public float[] resting;
        public Vector3 egg,eggVelocity;public float settled,incubation;
    }
    [Serializable] sealed class MaterialFacts
    {
        public string id,kind,site,support;public int reserved,lastOwner,socket;public bool available,sleeping,gripped,locked,colliderEnabled;
        public Vector3 position,velocity,angular,pickupStand,home,gripForce,gripTorque;
        public int[] fittingSlots,supportedSlots;
    }
    static string PieceId(Component material)=>material==null?"":(string)Get(material,"pieceId");
    static object Maybe(object owner,string name)
    {
        if(owner==null)return null;var field=owner.GetType().GetField(name,Any);if(field!=null)return field.GetValue(owner);
        return owner.GetType().GetProperty(name,Any)?.GetValue(owner);
    }
    static float StateValue(object state,string name)=>Maybe(state,name) is float value?value:0;

    static LaterRow ObserveLaterStage(Component match,string checkpoint)
    {
        var rules=(NestRules)Get(match,"rules");var sites=Components(Get(match,"sites"));var pieces=Components(Get(match,"materials"));var builders=Components(Get(match,"builders"));var site0=sites[0];
        var materials=pieces.Select(part=>
        {
            var body=(Rigidbody)Get(part,"body");var grip=(FixedJoint)Get(part,"Grip");var locked=(FixedJoint)Get(part,"Interlock");var site=(Component)Get(part,"Site");
            var fitting=new List<int>();var supported=new List<int>();
            if(Vector3.Distance(body.position,site0.transform.position)<18)
                for(int socket=0;socket<12;socket++)
                {
                    if((bool)Call(site0,"Fits",part,socket,body.position,body.rotation))fitting.Add(socket);
                    if((Rigidbody)Call(part,"ContactSupport",site0,socket)!=null)supported.Add(socket);
                }
            return new MaterialFacts{id=PieceId(part),kind=Get(part,"kind").ToString(),site=site==null?"":site.name,support=locked!=null&&locked.connectedBody!=null?locked.connectedBody.name:"",reserved=(int)Get(part,"ReservedBy"),lastOwner=(int)Get(part,"LastOwner"),socket=(int)Get(part,"Socket"),available=(bool)Get(part,"Available"),sleeping=body.IsSleeping(),gripped=grip!=null,locked=locked!=null,colliderEnabled=((Collider)Get(part,"shape")).enabled,position=body.position,velocity=body.linearVelocity,angular=body.angularVelocity,pickupStand=(Vector3)Get(part,"pickupStand"),home=(Vector3)Get(part,"home"),gripForce=grip==null?Vector3.zero:grip.currentForce,gripTorque=grip==null?Vector3.zero:grip.currentTorque,fittingSlots=fitting.ToArray(),supportedSlots=supported.ToArray()};
        }).ToArray();
        var acquisition=builders.Select(builder=>
        {
            int owner=(int)Get(builder,"slot"),socket=(int)Get(builder,"GoalSocket");var truck=(Component)Get(builder,"truck");var body=(Rigidbody)Get(truck,"body");
            var motor=Get(truck,"tractorMotor");var vehicle=Get(motor,"vehicle");var states=Maybe(Maybe(vehicle,"input"),"states");
            var result=new AcquisitionFacts{owner=owner,socket=socket,fairClaimAllowed=(bool)Call(match,"ClaimAvailableFor",builder),speed=body.linearVelocity.magnitude,vehicleEnabled=((Behaviour)vehicle).enabled,driveStateFieldsAvailable=new[]{"throttle","brakes","handbrake","steering"}.All(n=>Maybe(states,n) is float),throttle=StateValue(states,"throttle"),brakes=StateValue(states,"brakes"),handbrake=StateValue(states,"handbrake"),steering=StateValue(states,"steering")};
            if(socket<0)return result;
            string kind=(socket<4?NestMaterialKind.Branch:socket<8?NestMaterialKind.Stick:socket==8?NestMaterialKind.Leaves:socket==9?NestMaterialKind.Moss:NestMaterialKind.Decoration).ToString();result.required=kind;
            var nearest=pieces.Where(p=>(bool)Get(p,"Available")&&Get(p,"kind").ToString()==kind).OrderBy(p=>(((Rigidbody)Get(p,"body")).position-body.position).sqrMagnitude).FirstOrDefault();
            if(nearest==null)return result;
            result.nearest=PieceId(nearest);result.available=(bool)Get(nearest,"Available");result.itemPosition=((Rigidbody)Get(nearest,"body")).position;result.fixedPickupStand=(Vector3)Get(nearest,"pickupStand");result.itemRelativeToTruck=truck.transform.InverseTransformPoint(result.itemPosition);
            result.distanceToFixedStand=Vector3.ProjectOnPlane(result.fixedPickupStand-body.position,Vector3.up).magnitude;result.itemToFixedStand=Vector3.Distance(result.itemPosition,result.fixedPickupStand);
            result.withinOriginalReach=Mathf.Abs(result.itemRelativeToTruck.x)<23&&result.itemRelativeToTruck.z>2&&result.itemRelativeToTruck.z<30&&Vector3.Distance(result.itemPosition,body.position)<32;
            result.standDeadzoneButOutsideReach=result.distanceToFixedStand<4&&!result.withinOriginalReach;return result;
        }).ToArray();
        return new LaterRow{checkpoint=checkpoint,menu=Get(Get(match,"menus"),"Screen").ToString(),real=Time.realtimeSinceStartup,clock=(float)Get(match,"Clock"),timeScale=Time.timeScale,fixedDeltaTime=Time.fixedDeltaTime,maximumDeltaTime=Time.maximumDeltaTime,live=(bool)Get(match,"Live"),paused=(bool)Get(match,"Paused"),ready=(bool)Get(match,"DevicesReady"),finished=(bool)Get(match,"Finished"),builders=builders.Select(Observe).ToArray(),materials=materials,acquisition=acquisition,sites=sites.Select(site=>
        {
            int index=(int)Get(site,"index");var family=(Component)Get(site,"family");var egg=(Rigidbody)Get(family,"eggBody");return new SiteFacts{index=index,active=site.gameObject.activeInHierarchy,rooted=(int)Get(site,"RootedCount"),core=(bool)Get(site,"CoreReady"),slots=Components(Get(site,"slots")).Select(PieceId).ToArray(),candidates=Components(Get(site,"candidates")).Select(PieceId).ToArray(),claims=(int[])((int[])Get(site,"claims")).Clone(),resting=(float[])((float[])Get(site,"resting")).Clone(),eggPresent=(bool)Get(family,"EggPresent"),eggStable=(bool)Get(family,"EggStable"),hatched=(bool)Get(family,"Hatched"),egg=egg.position,eggVelocity=egg.isKinematic?Vector3.zero:egg.linearVelocity,settled=rules.settled[index],incubation=rules.incubation[index]};
        }).ToArray()};
    }

    [UnityTest,Timeout(45000)]public IEnumerator DiagnosticOnlyLaterAiClaimsAcquisitionSupportAndEggStateWithinTwentyEightRealSeconds()
    {
        using(var scope=new Scope("later-stage-v6-measurement"))
        {
            yield return scope.Load();var match=scope.match;Set(Get(match,"setup"),"count",4);Set(match,"option",0);Call(match,"AllBots");
            var players=(Array)Get(Get(match,"setup"),"players");var bodies=new[]{"novelty.hotdog","truck.monster","truck.fire","novelty.icecream"};
            for(int i=0;i<4;i++){string id=(string)Get(players.GetValue(i),"participantId");var draft=Static("SportsParticipantRegistry","GetAppearance",id);Set(draft,"bodyId",bodies[i]);Static("SportsParticipantRegistry","TrySetAppearance",id,draft);}
            Assert.That((bool)Call(match,"StartMatch"),Is.True);float originalFixed=Time.fixedDeltaTime;Time.timeScale=12;
            string path=Path.ChangeExtension(scope.progress,".later-state.jsonl");float end=Time.realtimeSinceStartup+28,next=0;int samples=0;string last="";
            while(!(bool)Get(match,"Finished")&&Time.realtimeSinceStartup<end)
            {
                string state=string.Join("/",Components(Get(match,"sites")).Select(s=>Get(s,"RootedCount")))+"|"+string.Join("/",Components(Get(match,"builders")).Select(b=>Get(b,"Mode")+":"+Get(b,"GoalSocket")+":"+Get(b,"Grasps")+":"+Get(b,"PhysicalDrops")));
                if(state!=last||Time.realtimeSinceStartup>=next)
                {
                    var row=ObserveLaterStage(match,state==last?"ordinary-later-stage-interval":"ordinary-later-stage-transition");File.AppendAllText(path,JsonUtility.ToJson(row)+"\n");samples++;last=state;next=Time.realtimeSinceStartup+1.5f;
                    TestContext.WriteLine($"NEST_LATER_MEASUREMENT sample={samples} clock={row.clock:R} rooted={row.sites[0].rooted} claim={string.Join(",",row.sites[0].claims)} egg={row.sites[0].eggPresent}/{row.sites[0].eggStable} real={row.real:R}");
                }
                yield return null;
            }
            var final=ObserveLaterStage(match,"final-read-only-measurement");File.AppendAllText(path,JsonUtility.ToJson(final)+"\n");
            Assert.That(Time.fixedDeltaTime,Is.EqualTo(originalFixed));Assert.That(samples,Is.GreaterThan(2));Assert.That(final.clock,Is.GreaterThan(1));
            TestContext.WriteLine($"NEST_LATER_DIAGNOSTIC_ONLY records={samples+1} clock={final.clock:R} rooted={final.sites[0].rooted} sameFixedStep={originalFixed:R} scale12 window28realSeconds noRulesPoseForcesScoreOutcomeInjection=true path={path}");
        }
    }
}
#endif
