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
    [Test]public void ActualLoosePieceParkingWithinOriginalStopRadiusIsInsideOriginalArmReachForAllHeadings()
    {
        var method=Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true).GetMethod("AcquisitionParkingTarget");
        foreach(float yaw in new[]{0f,45f,90f,180f,281.63864f,359f})
        {
            var rotation=Quaternion.Euler(0,yaw,0);var forward=rotation*Vector3.forward;var right=rotation*Vector3.right;var piece=new Vector3(-33,.18f,-40);
            var park=(Vector3)method.Invoke(null,new object[]{piece,forward,.08746f});
            foreach(var offset in new[]{Vector3.zero,forward*3.99f,-forward*3.99f,right*3.99f,-right*3.99f})
            {
                var actualStoppedBody=park+offset;var local=Quaternion.Inverse(rotation)*(piece-actualStoppedBody);
                Assert.That(Mathf.Abs(local.x),Is.LessThan(23));Assert.That(local.z,Is.InRange(2.001f,29.999f));Assert.That(Vector3.Distance(piece,actualStoppedBody),Is.LessThan(32));
            }
        }
        // Exact V6 stall: old fixed stand reached but selected material is behind actor.
        var observedPosition=new Vector3(-44.5720367f,.08745688f,-54.7031746f);var observedRotation=Quaternion.Euler(0,281.63864f,0);
        Assert.That(Vector3.ProjectOnPlane(new Vector3(-48,0,-54)-observedPosition,Vector3.up).magnitude,Is.LessThan(4));
        Assert.That((Quaternion.Inverse(observedRotation)*(new Vector3(-32.99998f,.18f,-40)-observedPosition)).z,Is.LessThan(2));
        var repaired=(Vector3)method.Invoke(null,new object[]{new Vector3(-32.99998f,.18f,-40),observedRotation*Vector3.forward,observedPosition.y});
        Assert.That(Vector3.ProjectOnPlane(repaired-observedPosition,Vector3.up).magnitude,Is.GreaterThan(4),"Actual original NWH navigation must receive a real reachable destination rather than the stale-stop position.");
    }
    [Test]public void ActualWorkApproachCannotStopOutsideUnchangedThreePointFiveMetrePlacementGate()
    {
        var method=Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true).GetMethod("NavigationDesiredSpeed");
        float actualGap=Vector2.Distance(new Vector2(12.3238621f,6.7851400f),new Vector2(9,8));
        Assert.That(actualGap,Is.InRange(3.53f,3.55f));Assert.That(actualGap,Is.GreaterThanOrEqualTo(3.5f),"Exact actual V8 carrier remains outside original placement acceptance.");
        Assert.That((float)method.Invoke(null,new object[]{actualGap,0f,4f}),Is.Zero,"Original default acquisition stop reproduces the work-approach deadlock.");
        foreach(float angle in new[]{0f,30f,90f})Assert.That((float)method.Invoke(null,new object[]{actualGap,angle,3f}),Is.GreaterThan(0),"Work approach must continue actual NWH drive toward its unchanged3.5m gate.");
        Assert.That((float)method.Invoke(null,new object[]{2.99f,0f,3f}),Is.Zero);Assert.That(3f,Is.LessThan(3.5f));
    }
    [Test]public void SelectedMaterialWorkspaceUsesOriginalHandClampsAndRejectsEscapedOrNonfiniteBodies()
    {
        var method=Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true).GetMethod("WithinHandWorkspace");
        foreach(var p in new[]{new Vector3(0,0,14),new Vector3(-24,-2,2),new Vector3(24,15,33)})Assert.That((bool)method.Invoke(null,new object[]{p}),Is.True);
        foreach(var p in new[]{new Vector3(24.01f,0,14),new Vector3(0,-2.01f,14),new Vector3(0,15.01f,14),new Vector3(0,0,1.99f),new Vector3(0,0,33.01f),new Vector3(1000,-26000,800),new Vector3(float.NaN,0,14)})Assert.That((bool)method.Invoke(null,new object[]{p}),Is.False);
    }
    [Test]public void AiSupplyPlanningRequiresActualNonSelfPhysicalSupportAndCannotFollowAnEscapedBody()
    {
        var builderType=Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true);var pieceType=Type.GetType("LWS.TruckNestBuilding.NestMaterial, Assembly-CSharp",true);
        var builderRoot=new GameObject("Synthetic read-only material planning");var item=new GameObject("Synthetic actual loose source");var floor=new GameObject("Synthetic physical source support");
        try
        {
            var builder=builderRoot.AddComponent(builderType);var piece=item.AddComponent(pieceType);item.transform.position=new Vector3(10000,.2f,10000);
            var body=item.AddComponent<Rigidbody>();body.isKinematic=true;item.AddComponent<BoxCollider>().size=Vector3.one*.4f;pieceType.GetField("body").SetValue(piece,body);
            floor.transform.position=new Vector3(10000,-.1f,10000);floor.AddComponent<BoxCollider>().size=new Vector3(3,.2f,3);UnityEngine.Physics.SyncTransforms();
            Assert.That((bool)Call(builder,"CanApproachLooseMaterial",piece),Is.True,"Real separate physical floor qualifies.");
            floor.SetActive(false);UnityEngine.Physics.SyncTransforms();Assert.That((bool)Call(builder,"CanApproachLooseMaterial",piece),Is.False,"Own collider alone never supplies physical support.");
            floor.SetActive(true);body.position=new Vector3(10000,-20,10000);UnityEngine.Physics.SyncTransforms();Assert.That((bool)Call(builder,"CanApproachLooseMaterial",piece),Is.False,"A below-world body remains untouched and cannot become a navigation destination.");
        }
        finally{UnityEngine.Object.DestroyImmediate(builderRoot);UnityEngine.Object.DestroyImmediate(item);UnityEngine.Object.DestroyImmediate(floor);}
    }
    [UnityTest,Timeout(70000)]public IEnumerator OrdinaryFourAiRetrieveActualLoosePiecesWithoutOrphanReservationsAndRootAllTenCoreSupports()
    {
        using(var scope=new Scope("actual-loose-retrieval-v7"))
        {
            yield return scope.Load();var match=scope.match;Set(Get(match,"setup"),"count",4);Set(match,"option",0);Call(match,"AllBots");
            var players=(Array)Get(Get(match,"setup"),"players");string[] bodies={"novelty.hotdog","truck.monster","truck.fire","novelty.icecream"};
            for(int i=0;i<4;i++){string id=(string)Get(players.GetValue(i),"participantId");var draft=Static("SportsParticipantRegistry","GetAppearance",id);Set(draft,"bodyId",bodies[i]);Static("SportsParticipantRegistry","TrySetAppearance",id,draft);}
            Assert.That((bool)Call(match,"StartMatch"),Is.True);using(var energy=NestMaterialEnergyProbe.Begin(match,Path.ChangeExtension(scope.progress,".energy.jsonl")))
            {
            float fixedStep=Time.fixedDeltaTime;Time.timeScale=12;float deadline=Time.realtimeSinceStartup+45,next=0,nextObservation=0;int previous=-1,samples=0,futureCollectionSamples=0;string previousCarrierState="";
            var orientationOffsets=new Dictionary<string,Quaternion>();var tiltedGrasps=new HashSet<string>();var correctedGrasps=new HashSet<string>();
            var brokenCarrySince=new Dictionary<string,float>();int transientBrokenSamples=0;float maximumBrokenCarryAge=0;var seenSupportedCargo=new HashSet<string>();var orphanSince=new Dictionary<string,float>();var unreachableSince=new Dictionary<string,float>();float maximumOrphanAge=0;string path=Path.ChangeExtension(scope.progress,".retrieval.jsonl");
            var site=Components(Get(match,"sites"))[0];
            while(!(bool)Get(site,"CoreReady")&&Time.realtimeSinceStartup<deadline)
            {
                if(Time.realtimeSinceStartup<nextObservation){yield return null;continue;}nextObservation=Time.realtimeSinceStartup+.15f;VerifyActualPlacementGraspTargets(match,orientationOffsets,tiltedGrasps,correctedGrasps);
                var row=ObserveLaterStage(match,"ordinary-original-ai-retrieval");VerifyActualPlacementGraspTargets(match,orientationOffsets,tiltedGrasps,correctedGrasps);float clock=row.clock;
                foreach(var piece in row.materials)
                {
                    bool orphan=piece.reserved>=0&&!row.builders.Any(b=>b.slot==piece.reserved&&(b.selected==piece.id||b.carried==piece.id));
                    if(orphan){if(!orphanSince.ContainsKey(piece.id))orphanSince[piece.id]=clock;float age=clock-orphanSince[piece.id];maximumOrphanAge=Mathf.Max(maximumOrphanAge,age);Assert.That(age,Is.LessThanOrEqualTo(.5f),"A broken finite grip cannot reserve an unowned material indefinitely: "+piece.id);}else orphanSince.Remove(piece.id);
                }
                foreach(var builder in row.builders)
                {
                    var selected=row.materials.FirstOrDefault(p=>p.id==builder.selected);
                    bool unreachable=selected!=null&&!(bool)Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true).GetMethod("WithinHandWorkspace").Invoke(null,new object[]{Quaternion.Inverse(Quaternion.Euler(builder.bodyEuler))*(selected.position-builder.body)});
                    string owner=builder.slot.ToString();if(unreachable){if(!unreachableSince.ContainsKey(owner))unreachableSince[owner]=clock;Assert.That(clock-unreachableSince[owner],Is.LessThanOrEqualTo(.5f),"Already-selected escaped body must release within the next ordinary simulation tick, not stall actual NWH forever.");}else unreachableSince.Remove(owner);
                }
                // PhysX breaks a finite joint after Session.FixedUpdate. The
                // observed Carried cache can legitimately survive until the next
                // session tick. A broken cache is not an actually held load.
                var brokenThisSample=new HashSet<string>();var actualBuilders=Components(Get(match,"builders"));
                foreach(var builder in row.builders)
                {
                    if(string.IsNullOrEmpty(builder.carried)||builder.grip)continue;
                    string generation=builder.slot+":"+builder.carried+":"+builder.grasps;brokenThisSample.Add(generation);
                    if(!brokenCarrySince.ContainsKey(generation))brokenCarrySince[generation]=clock;
                    float age=clock-brokenCarrySince[generation];maximumBrokenCarryAge=Mathf.Max(maximumBrokenCarryAge,age);transientBrokenSamples++;
                    File.AppendAllText(path,JsonUtility.ToJson(row)+"\n");
                    Assert.That(age,Is.LessThanOrEqualTo(.5f),"A genuinely broken finite-grip Carried cache must clear on the next ordinary session tick; generation="+generation);
                    var actualPiece=(Component)Get(actualBuilders[builder.slot],"Carried");var looseBody=(Rigidbody)Get(actualPiece,"body");
                    Assert.That(looseBody.isKinematic,Is.False);Assert.That(looseBody.detectCollisions,Is.True,"A broken load remains an actual colliding dynamic body; no attachment immunity.");
                    TestContext.WriteLine("NEST_TRANSIENT_FINITE_BREAK clock="+clock.ToString("R")+" generation="+generation+" observedAge="+age.ToString("R"));
                }
                foreach(string key in brokenCarrySince.Keys.Where(key=>!brokenThisSample.Contains(key)).ToArray())brokenCarrySince.Remove(key);
                foreach(var builder in row.builders)
                {
                    if(builder.socket<0||builder.socket>=10||string.IsNullOrEmpty(builder.carried)||!builder.grip)continue;
                    if((bool)Call(site,"Prerequisites",builder.socket)){seenSupportedCargo.Add(builder.carried);continue;}
                    if(seenSupportedCargo.Contains(builder.carried))continue; // Existing placement pauses after a later genuine support loss.
                    futureCollectionSamples++;
                    Assert.That(builder.mode,Is.EqualTo("Drive"),"An early collected material must remain in finite physical carry; placement cannot begin before actual rooted prerequisites.");
                    var actualBuilder=actualBuilders[builder.slot];var actualCargo=Get(actualBuilder,"Carried");var actualGrip=(FixedJoint)Get(actualCargo,"Grip");
                    Assert.That(actualGrip,Is.Not.Null);Assert.That(actualGrip.connectedBody,Is.SameAs(Get(actualBuilder,"hand")),"Future collection evidence requires the actual finite joint connected to this achieved hand, not a reservation/cache.");
                    Assert.That(actualGrip.breakForce,Is.EqualTo(2500));Assert.That(actualGrip.breakTorque,Is.EqualTo(1800));
                    Assert.That(((Rigidbody)Get(actualCargo,"body")).isKinematic,Is.False,"Actual early collection retains original dynamic cargo physics.");
                }
                int rooted=row.sites[0].rooted;
                string carrierState=string.Join("/",row.builders.Select(b=>b.mode+":"+b.socket+":"+b.grasps+":"+b.drops+":"+b.placementStage+":"+b.carried+":"+b.selected));
                if(rooted!=previous||carrierState!=previousCarrierState||Time.realtimeSinceStartup>=next)
                {
                    File.AppendAllText(path,JsonUtility.ToJson(row)+"\n");TestContext.WriteLine($"NEST_RETRIEVAL clock={clock:R} rooted={rooted} slots={string.Join(",",row.sites[0].slots)} maxOrphanAge={maximumOrphanAge:R}");previous=rooted;previousCarrierState=carrierState;next=Time.realtimeSinceStartup+2;samples++;
                }
                yield return null;
            }
            var final=ObserveLaterStage(match,"final-original-ai-retrieval");File.AppendAllText(path,JsonUtility.ToJson(final)+"\n");
            Assert.That(Time.fixedDeltaTime,Is.EqualTo(fixedStep));Assert.That(samples,Is.GreaterThan(2));Assert.That(futureCollectionSamples,Is.GreaterThan(0),"Exercise actual advance collection while prior physical supports remain incomplete.");Assert.That((bool)Get(site,"CoreReady"),Is.True,"Actual finite-force pickup, ordinary driving, drop, collision contact and physical rooting must create all ten core pieces; no pose/score/support injection.");
            for(int i=0;i<10;i++){var piece=Components(Get(site,"slots"))[i];Assert.That(piece,Is.Not.Null);Assert.That((bool)Call(site,"Rooted",piece),Is.True);}
            Assert.That(Components(Get(match,"builders")).All(b=>(int)Get(b,"PhysicalDrops")>=2),Is.True,"All four original AI share actual physical work.");
            TestContext.WriteLine("NEST_RETRIEVAL_ORIENTATION observed="+orientationOffsets.Count+" tilted="+tiltedGrasps.Count+" physicallyAligned="+correctedGrasps.Count);
            TestContext.WriteLine($"NEST_RETRIEVAL_PASS transientBrokenSamples={transientBrokenSamples} maximumBrokenCarryAge={maximumBrokenCarryAge:R} futureCollectionSamples={futureCollectionSamples} clock={final.clock:R} maximumTransientOrphanSeconds={maximumOrphanAge:R}; actual rules/forces/reach/fixedDelta unchanged; full egg/family completion remains separate original acceptance.");
            }
        }
    }
}
#endif
