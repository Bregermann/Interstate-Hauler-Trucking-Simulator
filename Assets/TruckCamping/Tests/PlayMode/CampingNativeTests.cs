#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using static WildernessNativeScope;
using LWS.TruckCamping;
public sealed class CampingNativeTests
{
    [UnityTest,Timeout(150000)]public IEnumerator SharedActualVistaWorldOwnedOneToFourViewsRealKeyboardGatherAndAtomicPausedExpedition()
    {
        using(var s=new WildernessNativeScope("camp-human-world"))
        {
            yield return s.Load("TruckCamping","LWS.TruckCamping.CampingMatch");var m=s.match;var world=(Component)Get(m,"world");WildernessSharedWorldProof.AssertActualSharedAssetBindings(world);
            var terrain=(Terrain[])Get(world,"terrains");Assert.That(terrain.Length,Is.EqualTo(4));Assert.That(terrain.All(t=>t.terrainData.size.x==1000&&t.terrainData.size.z==1000&&t.terrainData.treeInstanceCount>100),Is.True);Assert.That(Components(Get(world,"landmarks")).Length,Is.EqualTo(12));
            Assert.That(Components(Get(world,"hammocks")).All(h=>((Rigidbody)Get(h,"platform")).GetComponents<SpringJoint>().Length==4),Is.True);
            for(int count=1;count<=4;count++)
            {
                s.Configure(count);Assert.That((bool)Call(m,"StartMatch"),Is.True);Call(Get(m,"drivingViews"),"Select",1);yield return s.Advance(.8f);var rally=Get(Get(m,"drivingViews"),"rally");Assert.That((int)Get(rally,"Humans"),Is.EqualTo(count));Assert.That(((Camera[])Get(m,"cameras")).Length,Is.EqualTo(count));
                for(int i=0;i<count;i++)Assert.That(((Camera)Get(Components(Get(rally,"views"))[i],"view")).enabled,Is.True);s.Capture("camp-real-owned-views-"+count);if(count==1||count==4)s.Capture("camp-real-owned-views-"+count+"-1080",1920,1080);
            }
            s.Configure(1);Assert.That((bool)Call(m,"StartMatch"),Is.True);Time.timeScale=3;yield return s.Advance(2);var explorer=Components(Get(m,"explorers"))[0];var truck=(Component)Get(explorer,"truck");string original=PhysicsIdentity(truck);var body=(Rigidbody)Get(truck,"body");
            var resources=Components(Get(world,"resources"));var nearest=resources.Where(r=>(bool)Get(r,"Available")).OrderBy(r=>(r.transform.position-body.position).sqrMagnitude).First();var before=body.position;yield return WildernessDriveObservation.Run(s,explorer,((Rigidbody)Get(nearest,"body")).position,30,"actual-first-resource");Assert.That(Vector3.Distance(before,body.position),Is.GreaterThan(8));
            yield return s.Tap();Assert.That((bool)Get(explorer,"ContextOpen"),Is.True);yield return s.Tap();yield return s.Advance(2);Assert.That((int)Get(explorer,"ActualCollections"),Is.GreaterThan(0),"ACTION menu collection requires actual low-speed proximity to a real physical pickup.");
            var hammock=Components(Get(world,"hammocks")).OrderBy(h=>(h.transform.position-body.position).sqrMagnitude).First();yield return WildernessDriveObservation.Run(s,explorer,((Transform)Get(hammock,"approach")).position,35,"actual-hammock");float settle=Time.realtimeSinceStartup+6;while(!(bool)Call(hammock,"Supports",truck)&&Time.realtimeSinceStartup<settle)yield return null;
            Assert.That((bool)Call(hammock,"Supports",truck),Is.True,"At least two genuine NWH wheels must contact the spring-supported hammock deck at rest; no synthetic contact.");Call(m,"Interact",0,7);Assert.That((bool)Get(explorer,"Sleeping"),Is.True);yield return s.Advance(2);Assert.That(((CampJournal)Get(explorer,"Journal")).slept,Is.GreaterThan(1.5f));s.Capture("camp-real-physical-hammock-nap",1920,1080);Call(m,"Interact",0,8);
            Call(m,"Pause",0,false);yield return null;yield return null;string save=(string)Call(m,"CaptureWorkshopJson");var poses=resources.Select(r=>((Rigidbody)Get(r,"body")).position).ToArray();
            foreach(string bad in new[]{save.Replace("\"version\":1","\"version\":999"),save.Replace("camp.resource.","unknown.resource."),"{broken"})
            {var args=new object[]{bad,null};Assert.That((bool)Call(m,"TryRestoreWorkshopJson",args),Is.False);Assert.That(args[1],Is.Not.Empty);Assert.That((string)Call(m,"CaptureWorkshopJson"),Is.EqualTo(save));CollectionAssert.AreEqual(poses,resources.Select(r=>((Rigidbody)Get(r,"body")).position).ToArray());}
            var restore=new object[]{save,null};Assert.That((bool)Call(m,"TryRestoreWorkshopJson",restore),Is.True,restore[1] as string);Assert.That(PhysicsIdentity(truck),Is.EqualTo(original));Assert.That((bool)Call(Get(m,"persistence"),"SaveWorkshop"),Is.True);Assert.That((bool)Call(Get(m,"persistence"),"LoadWorkshop"),Is.True);
            s.Capture("camp-actual-paused-expedition-save",1920,1080);Call(m,"Resume");yield return s.Advance(.5f);Call(m,"BeginPhoto",0);Assert.That((bool)Get(m,"PhotoActive"),Is.True);s.Capture("camp-actual-photo-camera",1920,1080);Call(m,"EndPhoto");Call(m,"ExtraAction","camp-finish");Assert.That((bool)Get(m,"Finished"),Is.True);Assert.That(Get(m,"Results").ToString(),Does.Contain("NO WINNER OR POINTS"));
            TestContext.WriteLine("CAMPING_NATIVE genuine keyboard/software-pad ownership1–4, actual NWH approach and material gathering, exact rejected-snapshot atomicity, existing PixelCrushers isolated disk roundtrip, physical spring hammocks and shared scenic photo authority. No hardware/Windows/listening/performance certification.");
        }
    }
    [UnityTest,Timeout(110000)]public IEnumerator OrdinaryAIActuallyExploresCollectsBuildsFireAndCooksWhileAllWildlifeRigsMoveAndPauseFreezes()
    {
        using(var s=new WildernessNativeScope("camp-natural-ai"))
        {
            yield return s.Load("TruckCamping","LWS.TruckCamping.CampingMatch");s.Configure(4,true);Assert.That((bool)Call(s.match,"StartMatch"),Is.True);Time.timeScale=4;
            var world=(Component)Get(s.match,"world");var explorers=Components(Get(s.match,"explorers"));var starts=explorers.Select(e=>((Rigidbody)Get(Get(e,"truck"),"body")).position).ToArray();var sites=Components(Get(world,"campsites"));
            var skins=world.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.That(skins.Length,Is.GreaterThanOrEqualTo(12));var bones=skins.SelectMany(r=>r.bones).Distinct().ToArray();var initial=bones.Select(b=>b.localRotation).ToArray();bool animated=false,built=false,cooked=false;float end=Time.realtimeSinceStartup+80,next=0;
            while(Time.realtimeSinceStartup<end&&!cooked)
            {
                for(int i=0;i<bones.Length;i++)animated|=Quaternion.Angle(initial[i],bones[i].localRotation)>8;
                built|=sites.Any(c=>((CampfireState)Get(c,"state")).built);cooked|=explorers.Any(e=>((CampJournal)Get(e,"Journal")).mealsCooked>0);
                if(Time.realtimeSinceStartup>=next){s.Trace("ordinary-ai-camp");next=Time.realtimeSinceStartup+4;}yield return null;
            }
            Assert.That(animated,Is.True,"Measure actual skin bones, not actor orbit transforms.");Assert.That(explorers.All(e=>(int)Get(e,"ActualCollections")>0),Is.True,"Every original AI must collect actual nearby physical resources; counts="+string.Join(",",explorers.Select(e=>Get(e,"ActualCollections"))));Assert.That(built&&cooked,Is.True,"No fake AI inventory, automatic cooking credit or remote resource pickup.");
            Assert.That(explorers.Select((e,i)=>Vector3.Distance(((Rigidbody)Get(Get(e,"truck"),"body")).position,starts[i])).Min(),Is.GreaterThan(10));Assert.That((bool)Get(s.match,"Finished"),Is.False,"Cooperation never forces a winner or auto-ends the shared world.");
            Call(s.match,"Pause",0,false);yield return null;yield return null;var positions=explorers.Select(e=>((Rigidbody)Get(Get(e,"truck"),"body")).position).ToArray();float clock=(float)Get(s.match,"WildernessSeconds");yield return null;yield return null;CollectionAssert.AreEqual(positions,explorers.Select(e=>((Rigidbody)Get(Get(e,"truck"),"body")).position).ToArray());Assert.That((float)Get(s.match,"WildernessSeconds"),Is.EqualTo(clock));
            s.Capture("camp-ordinary-ai-real-campfire-cooking",1920,1080);Call(s.match,"Resume");Assert.That((bool)Call(s.match,"StartMatch"),Is.True);Assert.That(sites.All(c=>!((CampfireState)Get(c,"state")).built),Is.True);
        }
    }
}
#endif
