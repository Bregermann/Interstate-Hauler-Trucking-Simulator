#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed partial class NestNativeTests
{
    const string BroodSnapshotPath="Logs/TruckSports/Impossible07/NestBroodStackControlledV17/Input/actual-r37-paused-workshop.json";
    const string BroodSnapshotHash="38bae1ce70f9fe06b7a55e829b2d4973a59b09f04bb9ed089b26e566eb738e7c";
    [Serializable]sealed class ControlledBroodRecord
    {
        public string scope="CONTROLLED_PHYSICS_FROM_GENUINE_PAUSED_SNAPSHOT_NOT_NATURAL_COMPLETION",stage,sourceSha256,liningId,surplusId,runId;public CorePoseFact[] core;public bool extraPlaced,matchEnabled,paused;public float maximumCorePositionError,stableSeconds,looseSupportSlowSeconds;public int rooted;public EggContactFacts egg;
    }
    [Serializable]sealed class CorePoseFact{public string id,parent,actualJointParent;public Vector3 expected,body,transform,jointAnchor,connectedAnchor,expectedConnectedAnchor;public bool autoConfigureConnectedAnchor;public Quaternion bodyRotation,transformRotation;}
    static string BroodFileHash(string path){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    [UnityTest,Timeout(60000)]public IEnumerator ControlledActualPausedCoreEggContactsRootedLiningWithoutLooseMoss()
    {yield return ControlledBroodStack(false);}
    [UnityTest,Timeout(60000)]public IEnumerator ControlledMeasuredLooseMossStackInterceptsEggAndCannotGrantRootedLiningSupport()
    {yield return ControlledBroodStack(true);}
    [UnityTest,Timeout(90000)]public IEnumerator ControlledActualAIGripsAndClearsLooseMossSoTheUnchangedRootedLiningCanSupportTheEgg()
    {yield return ControlledBroodStack(true,true);}
    [Test]public void BroodStagingRequiresWholeCargoOutsideActualPlatformWithHalfMetreClearance()
    {
        var type=Type.GetType("LWS.TruckNestBuilding.NestBuilder, Assembly-CSharp",true);
        var predicate=type.GetMethod("OutsideBroodPlatform",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
        var deck=new Bounds(new Vector3(0,6,30),new Vector3(12,.5f,12));
        Func<Vector3,bool> clear=centre=>(bool)predicate.Invoke(null,new object[]{new Bounds(centre,new Vector3(3.8f,.2f,3.8f)),deck});
        Assert.That(clear(new Vector3(0,8.3f,30)),Is.False);
        Assert.That(clear(new Vector3(0,8.3f,22)),Is.False,"A clear egg footprint alone does not justify releasing cargo above the platform edge.");
        Assert.That(clear(new Vector3(0,8.3f,21.5f)),Is.True);
        Assert.That(clear(new Vector3(0,8.3f,38.5f)),Is.True);
        Assert.That(clear(new Vector3(-8.5f,8.3f,30)),Is.True);
        Assert.That(clear(new Vector3(8.5f,8.3f,30)),Is.True);
        var stage=type.GetMethod("BroodStagingTarget");
        foreach(var home in new[]{new Vector3(0,3,28),new Vector3(0,3,32),new Vector3(-2,3,30),new Vector3(2,3,30)})
        {
            var actual=(Vector3)stage.Invoke(null,new object[]{new Bounds(new Vector3(0,8.3f,30),new Vector3(5.2f,.2f,5.2f)),deck,home,8.3f});
            Assert.That((bool)predicate.Invoke(null,new object[]{new Bounds(actual,new Vector3(5.2f,.2f,5.2f)),deck}),Is.True);
            Assert.That(actual.y,Is.EqualTo(8.3f),"Level withdrawal avoids lifting the real egg or descending through the deck lip.");
        }
    }
    static IEnumerator ControlledBroodStack(bool withExtra,bool exerciseCleanup=false)
    {
        Assert.That(File.Exists(BroodSnapshotPath),Is.True,"Lead must intake the exact pinned genuine R37 snapshot; never skip or fabricate a replacement.");Assert.That(BroodFileHash(BroodSnapshotPath),Is.EqualTo(BroodSnapshotHash));
        using(var scope=new Scope(exerciseCleanup?"controlled-brood-real-clearance":withExtra?"controlled-brood-loose-stack":"controlled-brood-direct-lining"))
        {
            yield return scope.Load();var match=scope.match;Set(Get(match,"setup"),"count",4);Set(match,"option",0);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return Advance(scope,.4f);Call(match,"Pause",0,false);yield return null;yield return null;
            var actors=Components(Get(match,"builders"));var trucks=actors.Select(b=>(Component)Get(b,"truck")).ToArray();var originalPhysics=trucks.Select(Geometry).ToArray();
            var type=Type.GetType("LWS.TruckNestBuilding.NestMatch+Workshop, Assembly-CSharp",true);var draft=JsonUtility.FromJson(File.ReadAllText(BroodSnapshotPath,System.Text.Encoding.UTF8),type);var oldIds=(string[])Get(draft,"profiles");Assert.That(oldIds.Length,Is.EqualTo(4));Assert.That(oldIds.Distinct().Count(),Is.EqualTo(4));
            var players=(Array)Get(Get(match,"setup"),"players");var newIds=Enumerable.Range(0,4).Select(i=>(string)Get(players.GetValue(i),"participantId")).ToArray();
            var pieces=((Array)Get(draft,"pieces")).Cast<object>().ToArray();foreach(var piece in pieces){string owner=(string)Get(piece,"owner");if(string.IsNullOrEmpty(owner))continue;int seat=Array.IndexOf(oldIds,owner);Assert.That(seat,Is.InRange(0,3));Set(piece,"owner",newIds[seat]);}Set(draft,"profiles",newIds);
            var lining=pieces.Single(piece=>(int)Get(piece,"site")==0&&(int)Get(piece,"socket")==9);string liningId=(string)Get(lining,"id");var liningPosition=(Vector3)Get(Get(lining,"pose"),"position");
            var surplus=pieces.First(piece=>(int)Get(piece,"site")==-1&&((string)Get(piece,"id")).EndsWith(".piece9",StringComparison.Ordinal));string surplusId=(string)Get(surplus,"id");
            if(withExtra)
            {
                var pose=Get(surplus,"pose");Set(pose,"position",liningPosition+new Vector3(-.26022717f,.19704151f,-.058296204f));Set(pose,"rotation",Quaternion.Euler(.003213237f,28.705322f,.009135682f));Set(pose,"velocity",Vector3.zero);Set(pose,"angular",Vector3.zero);
            }
            var familyData=((Array)Get(draft,"families")).GetValue(0);Set(familyData,"received",true);Set(familyData,"hatched",false);Set(familyData,"incubation",0f);var eggPose=Get(familyData,"egg");Set(eggPose,"position",liningPosition+Vector3.up*.91f);Set(eggPose,"rotation",Quaternion.identity);Set(eggPose,"velocity",Vector3.zero);Set(eggPose,"angular",Vector3.zero);
            var args=new object[]{JsonUtility.ToJson(draft),null};Assert.That((bool)Call(match,"TryRestoreWorkshopJson",args),Is.True,args[1] as string);
            var site=Components(Get(match,"sites"))[0];var family=(Component)Get(site,"family");var materialById=Components(Get(match,"materials")).ToDictionary(p=>(string)Get(p,"pieceId"));var currentLining=materialById[liningId];var currentSurplus=materialById[surplusId];var egg=(Collider)Get(family,"eggCollider");
            var rules=Get(match,"rules");var quality=(int[])((int[])Get(rules,"quality")).Clone();var scoreBefore=Get(match,"ScoreJournal");
            string folder=Path.GetFullPath("Logs/TruckSports/Impossible07/NestBroodStackControlledV17/Output");Directory.CreateDirectory(folder);string runId=Guid.NewGuid().ToString("N"),output=Path.Combine(folder,(exerciseCleanup?"clear-":withExtra?"stack-":"direct-")+runId+".jsonl");Assert.That(File.Exists(output),Is.False);Assert.That(output.Length,Is.LessThan(240));
            bool wasEnabled=((Behaviour)match).enabled;float stableSeconds=0,looseSlowSeconds=0;bool observedLoose=false,observedRooted=false;float maxCoreError=0;
            Action<string> record=stage=>File.AppendAllText(output,JsonUtility.ToJson(new ControlledBroodRecord{stage=stage,runId=runId,core=pieces.Where(p=>(int)Get(p,"site")==0).Select(p=>{var actual=materialById[(string)Get(p,"id")];var b=(Rigidbody)Get(actual,"body");var joint=(FixedJoint)Get(actual,"Interlock");return new CorePoseFact{actualJointParent=joint==null||joint.connectedBody==null?"NONE":joint.connectedBody.name,jointAnchor=joint==null?Vector3.zero:joint.anchor,connectedAnchor=joint==null?Vector3.zero:joint.connectedAnchor,expectedConnectedAnchor=joint==null||joint.connectedBody==null?Vector3.zero:joint.connectedBody.transform.InverseTransformPoint(b.transform.TransformPoint(joint.anchor)),autoConfigureConnectedAnchor=joint!=null&&joint.autoConfigureConnectedAnchor,id=(string)Get(p,"id"),parent=(string)Get(p,"parent"),expected=(Vector3)Get(Get(p,"pose"),"position"),body=b.position,transform=b.transform.position,bodyRotation=b.rotation,transformRotation=b.transform.rotation};}).ToArray(),sourceSha256=BroodSnapshotHash,liningId=liningId,surplusId=surplusId,extraPlaced=withExtra,matchEnabled=((Behaviour)match).enabled,paused=(bool)Get(match,"Paused"),maximumCorePositionError=maxCoreError,stableSeconds=stableSeconds,looseSupportSlowSeconds=looseSlowSeconds,rooted=(int)Get(site,"RootedCount"),egg=ObserveEgg(site)})+"\n");
            try
            {
                // This isolated CONTROLLED physics test freezes only the match
                // coordinator. Original bodies/joints/gravity/callbacks run;
                // original support predicate is READ, never injected or scored.
                ((Behaviour)match).enabled=false;Time.timeScale=1;record("paused-official-restore-before-first-physics");yield return new WaitForFixedUpdate();
                foreach(var piece in pieces.Where(p=>(int)Get(p,"site")==0))
                {
                    var actual=materialById[(string)Get(piece,"id")];var body=(Rigidbody)Get(actual,"body");maxCoreError=Mathf.Max(maxCoreError,Vector3.Distance(body.position,(Vector3)Get(Get(piece,"pose"),"position")));
                }
                record("first-actual-physics-after-official-restore");Assert.That(maxCoreError,Is.LessThan(.05f),"Genuine restored core poses must survive the first actual physics step; never hide a stale joint-reference defect using fixture pose writes/SyncTransforms.");
                Assert.That((bool)Get(site,"CoreReady"),Is.True);Assert.That((bool)Get(family,"EggPresent"),Is.True);Assert.That(egg,Is.TypeOf<CapsuleCollider>());Assert.That(((CapsuleCollider)egg).height,Is.EqualTo(1.35f));Assert.That(((BoxCollider)Get(currentLining,"shape")).size,Is.EqualTo(new Vector3(3.8f,.2f,3.8f)));
                float end=Time.realtimeSinceStartup+4,next=0;
                while(Time.realtimeSinceStartup<end)
                {
                    yield return new WaitForFixedUpdate();var row=ObserveEgg(site);bool loose=row.contacts.Any(c=>c.materialId==surplusId&&!c.exactRootedLining),rooted=row.contacts.Any(c=>c.materialId==liningId&&c.exactRootedLining);observedLoose|=loose;observedRooted|=rooted;
                    if(row.stable)stableSeconds+=Time.fixedDeltaTime;
                    if(row.present&&row.core&&!row.kinematic&&loose&&row.velocity.sqrMagnitude<.025f&&row.angular.sqrMagnitude<.06f)looseSlowSeconds+=Time.fixedDeltaTime;
                    if(Time.realtimeSinceStartup>=next){record("actual-callback-physical-support");next=Time.realtimeSinceStartup+.25f;}
                }
                record("controlled-physics-end");
                Assert.That((bool)Get(family,"EggPresent"),Is.True,"A lost/disabled egg cannot falsely prove rejection of wrong support.");Assert.That(((Rigidbody)Get(family,"eggBody")).isKinematic,Is.False);Assert.That(egg.enabled,Is.True);Assert.That(((Rigidbody)Get(family,"eggBody")).detectCollisions,Is.True);
                Assert.That((bool)Get(site,"CoreReady"),Is.True,"Wrong-support control requires the original full rooted core to remain intact at the END, not only before the first step.");
                for(int socket=0;socket<10;socket++){var actual=Components(Get(site,"slots"))[socket];Assert.That(actual,Is.Not.Null);var b=(Rigidbody)Get(actual,"body");Assert.That((bool)Call(site,"Rooted",actual),Is.True);Assert.That((bool)Call(site,"Fits",actual,socket,b.position,b.rotation),Is.True);Assert.That(b.isKinematic,Is.False);}

                if(withExtra){Assert.That(observedLoose,Is.True,"Actual Collision.GetContacts must identify the measured unrooted duplicate Moss; geometry overlap alone is not proof.");Assert.That(looseSlowSeconds,Is.GreaterThan(1),"The egg must actually be slow on the loose material, distinguishing wrong support from velocity-only rejection.");Assert.That(stableSeconds,Is.Zero,"Unrooted duplicate support cannot grant original direct rooted-lining EggStable.");Assert.That(observedRooted,Is.False);Assert.That(Get(currentSurplus,"Site"),Is.Null);Assert.That((FixedJoint)Get(currentSurplus,"Grip"),Is.Null);Assert.That(((Rigidbody)Get(currentSurplus,"body")).isKinematic,Is.False);}
                else{Assert.That(observedRooted,Is.True);Assert.That(stableSeconds,Is.GreaterThan(1),"Same genuine restored core/egg must physically pass the unmodified direct-lining predicate without the duplicate.");Assert.That(observedLoose,Is.False);}
                CollectionAssert.AreEqual(quality,(int[])Get(rules,"quality"));Assert.That(Get(match,"ScoreJournal"),Is.SameAs(scoreBefore));Assert.That(((bool[])Get(rules,"hatched"))[0],Is.False);Assert.That(((float[])Get(rules,"incubation"))[0],Is.Zero,"Coordinator is disabled: contact experiment grants no incubation/hatching score.");CollectionAssert.AreEqual(originalPhysics,trucks.Select(Geometry).ToArray());
                if(exerciseCleanup)
                {
                    var builderType=actors[0].GetType();var obstruction=builderType.GetMethod("ObstructsBrood",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                    Assert.That((bool)obstruction.Invoke(null,new object[]{site,currentSurplus}),Is.True);
                    Assert.That((bool)obstruction.Invoke(null,new object[]{site,currentLining}),Is.False,"Actual rooted lining is never a cleanup candidate.");
                    var materialIds=materialById.Values.Select(p=>p.GetInstanceID()).OrderBy(id=>id).ToArray();
                    ((Behaviour)match).enabled=wasEnabled;Call(match,"Resume");Assert.That((bool)Get(match,"Live"),Is.True);
                    bool finiteGrasp=false,releaseOutside=false,stableAfter=false;float cleanupEnd=Time.realtimeSinceStartup+40,cleanupNext=0;
                    while(Time.realtimeSinceStartup<cleanupEnd)
                    {
                        foreach(var actor in actors)
                        {
                            if((Component)Get(actor,"Carried")!=currentSurplus)continue;
                            var grip=(FixedJoint)Get(currentSurplus,"Grip");Assert.That(grip,Is.Not.Null);Assert.That(grip.connectedBody,Is.SameAs((Rigidbody)Get(actor,"hand")));
                            Assert.That(grip.breakForce,Is.EqualTo(2500));Assert.That(grip.breakTorque,Is.EqualTo(1800));Assert.That(grip.enableCollision,Is.False);
                            Assert.That(((Rigidbody)Get(currentSurplus,"body")).isKinematic,Is.False);Assert.That(Get(currentSurplus,"Site"),Is.Null);Assert.That((FixedJoint)Get(currentSurplus,"Interlock"),Is.Null);Assert.That((bool)Get(actor,"ClearingBrood"),Is.True);finiteGrasp=true;
                        }
                        if(actors.Sum(a=>(int)Get(a,"BroodClearancesCompleted"))>0)
                        {
                            Assert.That((FixedJoint)Get(currentSurplus,"Grip"),Is.Null);Assert.That(Get(currentSurplus,"Site"),Is.Null);
                            Assert.That((bool)builderType.GetMethod("OutsideBroodPlatform").Invoke(null,new object[]{((Collider)Get(currentSurplus,"shape")).bounds,((Collider)Get(site,"deck")).bounds}),Is.True);
                            releaseOutside=true;
                        }
                        stableAfter|=(bool)Get(family,"EggStable");
                        if(Time.realtimeSinceStartup>=cleanupNext){scope.Trace("controlled-real-AI-brood-clearance");record("real-AI-finite-grip-level-clearance");cleanupNext=Time.realtimeSinceStartup+.5f;}
                        if(releaseOutside&&((bool)Get(family,"Hatched")||stableAfter))break;
                        yield return null;
                    }
                    record("controlled-real-AI-clearance-end");
                    Assert.That(finiteGrasp,Is.True,"Actual AI must reserve, reach and create the original finite grasp; no fixture cargo/hand/score writes after restore.");
                    Assert.That(releaseOutside,Is.True,"The ordinary hand must carry the entire surplus outside the actual platform before real release.");
                    Assert.That(stableAfter||(bool)Get(family,"Hatched"),Is.True,"Cleanup must restore actual direct lining support through original egg callbacks, not waive the predicate.");
                    Assert.That((bool)Get(site,"CoreReady"),Is.True);Assert.That((bool)obstruction.Invoke(null,new object[]{site,currentSurplus}),Is.False);
                    CollectionAssert.AreEqual(materialIds,Components(Get(match,"materials")).Select(p=>p.GetInstanceID()).OrderBy(id=>id).ToArray(),"Every actual material instance is conserved.");
                    Assert.That(((Collider)Get(currentSurplus,"shape")).enabled,Is.True);Assert.That(((Rigidbody)Get(currentSurplus,"body")).detectCollisions,Is.True);
                    CollectionAssert.AreEqual(originalPhysics,trucks.Select(Geometry).ToArray());
                    Call(match,"StartMatch");Assert.That(actors.All(a=>!(bool)Get(a,"ClearingBrood")&&(int)Get(a,"BroodClearancesCompleted")==0),Is.True,"Rematch clears job state and counters.");
                    TestContext.WriteLine("CONTROLLED_REAL_AI_CLEARANCE actualFiniteGrasp="+finiteGrasp+" releasedOutsideActualPlatform="+releaseOutside+" actualRootedLiningStable="+stableAfter+"; pinned snapshot setup only; no live egg/material/hand pose writes; NOT natural full-family acceptance.");
                }
                TestContext.WriteLine("CONTROLLED_BROOD_PHYSICS output="+output+" extra="+withExtra+" actualLooseContact="+observedLoose+" actualRootedContact="+observedRooted+" stableSeconds="+stableSeconds.ToString("R")+" slowLooseSupportSeconds="+looseSlowSeconds.ToString("R")+"; pausedsnapshotprofileIDsremappedonlyforfreshcrew; controlledpendingegg/optionalextraPOSESETUPONLYATPAUSE; originalgravity/collisions/rootedpredicate/physics; NOTordinaryAI/naturalcompletion.");
            }
            finally{Time.timeScale=0;((Behaviour)match).enabled=wasEnabled;}
        }
    }
}
#endif
