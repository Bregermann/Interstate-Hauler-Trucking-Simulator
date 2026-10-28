#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using LWS.TruckPetPals;
using static WildernessNativeScope;
public sealed class PetPalsNativeTests
{
    static PetRecord Record(Component pet)=>(PetRecord)Get(pet,"Record");
    static Component Panel(Component match)=>match.GetComponent(Runtime("LWS.TruckPetPals.PetAdoptionPanel"))??((Component)Get(match,"menus")).GetComponent(Runtime("LWS.TruckPetPals.PetAdoptionPanel"));
    [UnityTest,Timeout(90000)]public IEnumerator ActualDetachedHeatAdoptionAndFourOwnedCompanionsReuseTheExactCampingPrefab()
    {
        using(var s=new WildernessNativeScope("pet-adoption-views"))
        {
            yield return s.Load("TruckPetPals","LWS.TruckPetPals.PetPalsMatch");var m=s.match;var repo=Get(m,"petProfiles");Assert.That((bool)Get(repo,"Loaded"),Is.True);s.Configure(4);
            var players=(Array)Get(Get(m,"setup"),"players");for(int i=0;i<4;i++)Call(repo,"ForProfile",Get(players.GetValue(i),"participantId"),"semi.standard");
            string before=JsonUtility.ToJson(Get(repo,"Archive"));var panel=Panel(m);Call(panel,"Open",0);Assert.That((bool)Get(panel,"IsOpen"),Is.True);var camera=(Camera)Get(panel,"camera");Assert.That(camera.targetTexture,Is.Not.Null);Assert.That(camera.cullingMask,Is.EqualTo(1<<24));Call(panel,"CycleType");Call(panel,"CycleAccessory");Call(panel,"Close");yield return null;yield return null;Assert.That(JsonUtility.ToJson(Get(repo,"Archive")),Is.EqualTo(before),"Cancel discards detached pet identity/body/accessory draft.");Assert.That(Get(panel,"camera"),Is.Null);
            Call(panel,"Open",0);Call(panel,"CycleType");Set(Get(panel,"petName"),"text","Clover");Call(panel,"Apply");yield return null;yield return null;
            var archive=(PetArchive)Get(repo,"Archive");var chosen=archive.profiles.Single(p=>p.profileId==(string)Get(players.GetValue(0),"participantId"));Assert.That(chosen.pets.Single(p=>p.id==chosen.selectedPetId).name,Is.EqualTo("Clover"));
            for(int count=1;count<=4;count++)
            {
                s.Configure(count);Assert.That((bool)Call(m,"StartMatch"),Is.True);Call(Get(m,"drivingViews"),"Select",1);yield return s.Advance(1);
                var pets=Components(Get(m,"companions"));Assert.That(pets.Count(p=>p.gameObject.activeInHierarchy),Is.EqualTo(count));Assert.That(pets.Take(count).Select(p=>Record(p).profileId).Distinct().Count(),Is.EqualTo(count));var rally=Get(Get(m,"drivingViews"),"rally");Assert.That((int)Get(rally,"Humans"),Is.EqualTo(count));
                for(int i=0;i<count;i++)Assert.That(Record(pets[i]).profileId,Is.EqualTo(Get(players.GetValue(i),"participantId")));s.Capture("pet-real-owned-companions-"+count,1920,1080);
            }
            var world=(Component)Get(m,"world");WildernessSharedWorldProof.AssertActualSharedAssetBindings(world);Assert.That(((Terrain[])Get(world,"terrains")).Length,Is.EqualTo(4));
            Call(m,"Pause",0,false);Call(panel,"Open",0);s.Capture("pet-actual-Heat-adoption-RT",1920,1080);Call(panel,"Close");Assert.That(Get(panel,"camera"),Is.Null);Call(m,"ExtraAction","camp-finish");Assert.That(Get(m,"Results").ToString(),Does.Contain("NO WINNER OR POINTS"));
        }
    }
    [UnityTest,Timeout(240000)]public IEnumerator EveryRealPetRigPerformsSevenTricksLegLiftGrassKicksInteractiveBathAndRealTerrainDig()
    {
        using(var s=new WildernessNativeScope("pet-three-rigs-real-care"))
        {
            yield return s.Load("TruckPetPals","LWS.TruckPetPals.PetPalsMatch");s.Configure(1);Assert.That((bool)Call(s.match,"StartMatch"),Is.True);Time.timeScale=4;yield return s.Advance(1);
            var m=s.match;var explorer=Components(Get(m,"explorers"))[0];var pet=Components(Get(m,"companions"))[0];var truck=(Component)Get(explorer,"truck");string physics=PhysicsIdentity(truck);
            var patches=Components(Get(m,"digPatches"));var initialData=((Terrain[])Get(Get(m,"world"),"terrains")).Select(t=>t.terrainData).ToArray();
            yield return s.DriveTo(explorer,patches[0].transform.position-Vector3.forward*7);
            foreach(PetKind kind in Enum.GetValues(typeof(PetKind)))
            {
                Call(m,"Pause",0,false);Call(m,"Adopt",0,kind,"Little "+kind,"truck.monster","bandana.rose");Call(m,"Resume");Time.timeScale=4;yield return s.Advance(2);
                Assert.That(Record(pet).kind,Is.EqualTo(kind));var visual=Get(pet,"Visual");Assert.That(((GameObject)Get(visual,"root")).activeInHierarchy,Is.True);var art=(Component)(Get(visual,"living")??Get(visual,"miniature"));Assert.That(art,Is.Not.Null);
                foreach(PetTrick trick in Enum.GetValues(typeof(PetTrick)))
                {
                    if(trick==PetTrick.Come)
                    {
                        var player=((Array)Get(Get(m,"setup"),"players")).GetValue(0);s.Keys((UnityEngine.InputSystem.Key)Get(player,"forward"));yield return s.Advance(2);s.Keys();yield return s.Advance(.5f);
                    }
                    int before=Record(pet).familiarity[(int)trick];float trainingDeadline=Time.realtimeSinceStartup+16;Assert.That((bool)Call(pet,"BeginTraining",trick),Is.True,"Actual eligible trick starts: "+kind+"/"+trick);
                    while(!(bool)Get(pet,"TrainingCue")&&Get(pet,"Mood").ToString()=="Training"&&Time.realtimeSinceStartup<trainingDeadline)yield return null;
                    var actualPatch=(Component)Get(pet,"patch");string cueState=kind+"/"+trick+" mood="+Get(pet,"Mood")+" elapsed="+((float)Get(pet,"elapsed")).ToString("R")+" patchDistance="+(actualPatch==null?"NONE":Vector3.Distance(pet.transform.position,actualPatch.transform.position).ToString("R"));
                    s.Trace("actual-training-cue-"+kind+"-"+trick);TestContext.WriteLine("PET_ACTUAL_CUE "+cueState);
                    Assert.That((bool)Get(pet,"TrainingCue"),Is.True,"Wait for the original achieved approach/cue, never assume a walk-to-dig interaction is ready after fixed1.3s: "+cueState);yield return s.Tap();
                    float deadline=trainingDeadline;while(Get(pet,"Mood").ToString()=="Training"&&Time.realtimeSinceStartup<deadline)yield return null;
                    s.Trace(kind+"-actual-"+trick);Assert.That(Record(pet).familiarity[(int)trick],Is.GreaterThan(before),"Learning requires the real achieved animated pose/movement and actual keyboard cue: "+kind+"/"+trick);
                    Assert.That((bool)Get(explorer,"ContextOpen"),Is.False,"One care interaction owns ACTION; it cannot also open the camp menu.");s.Capture("pet-"+kind+"-actual-"+trick);
                }
                Assert.That((int)Get(pet,"PerformedTricksMask"),Is.EqualTo(127));Call(pet,"CallBack");Assert.That((bool)Call(pet,"RequestPee"),Is.True);bool stream=false,kicks=false;float peeEnd=Time.realtimeSinceStartup+16;
                while(Get(pet,"Mood").ToString()=="Peeing"&&Time.realtimeSinceStartup<peeEnd){stream|=((ParticleSystem)Get(pet,"peeStream")).particleCount>0;kicks|=((ParticleSystem)Get(pet,"grassKicks")).particleCount>0;yield return null;}
                Assert.That((bool)Get(pet,"PeeLegObserved")&&(bool)Get(pet,"GrassKickObserved")&&stream&&kicks,Is.True,"Every actual rig lifts its own limb/wheel before emission and scrapes grass afterwards.");Assert.That(Record(pet).pees,Is.GreaterThan(0));
                var stations=(Transform[])Get(m,"washStations");var body=(Rigidbody)Get(truck,"body");var station=stations.OrderBy(t=>(t.position-body.position).sqrMagnitude).First();yield return s.DriveTo(explorer,station.position-Vector3.forward*5);
                Assert.That((bool)Call(pet,"BeginBath",station),Is.True);float approachEnd=Time.realtimeSinceStartup+12;while(Vector3.Distance(pet.transform.position,station.position)>1.2f&&Time.realtimeSinceStartup<approachEnd)yield return null;Assert.That(Vector3.Distance(pet.transform.position,station.position),Is.LessThan(1.3f));
                var bath=(PetBathRules)Get(pet,"Bath");int baths=Record(pet).baths;
                for(int phase=0;phase<5;phase++)
                {
                    Assert.That((int)bath.Step,Is.EqualTo(phase));
                    if(bath.Step==PetBathStep.Scrub){var player=((Array)Get(Get(m,"setup"),"players")).GetValue(0);for(int stroke=0;stroke<8;stroke++){s.Keys((UnityEngine.InputSystem.Key)Get(player,stroke%2==0?"left":"right"));yield return s.Advance(.25f);}s.Keys();}
                    else{s.Keys(s.Action);yield return s.Advance(2.3f);s.Keys();yield return s.Advance(.2f);}
                    Assert.That(bath.Progress,Is.EqualTo(1));yield return s.Tap();
                }
                Assert.That(bath.Complete,Is.True);Assert.That(Record(pet).baths,Is.EqualTo(baths+1));Assert.That(Record(pet).cleanliness,Is.EqualTo(100));Assert.That((bool)Get(explorer,"ContextOpen"),Is.False);Assert.That(PhysicsIdentity(truck),Is.EqualTo(physics));s.Capture("pet-"+kind+"-actual-washed-and-trained",1920,1080);
            }
            Assert.That(patches.Count(p=>(bool)Get(p,"Discovered")),Is.GreaterThanOrEqualTo(3),"Each pet really digs a separate hole; no text-only completion.");Assert.That(patches.Where(p=>(bool)Get(p,"Discovered")).All(p=>((MeshCollider)Get(p,"collision")).sharedMesh!=null),Is.True);
            var terrains=(Terrain[])Get(Get(m,"world"),"terrains");Assert.That(terrains.Where((t,i)=>t.terrainData!=initialData[i]).Any(),Is.True,"Digging modifies ONLY a runtime terrain clone.");
            foreach(var ground in Components(Get(m,"digGrounds")))Call(ground,"ResetGround");CollectionAssert.AreEqual(initialData,terrains.Select(t=>t.terrainData).ToArray(),"Original saved terrain data references are restored exactly.");
        }
    }
    [UnityTest,Timeout(105000)]public IEnumerator OrdinaryAIOwnersWalkRealPetsNaturalGrassBreaksAndRetrievePhysicalToysWithoutForcedWinner()
    {
        using(var s=new WildernessNativeScope("pet-natural-ai"))
        {
            yield return s.Load("TruckPetPals","LWS.TruckPetPals.PetPalsMatch");s.Configure(4,true);Assert.That((bool)Call(s.match,"StartMatch"),Is.True);Time.timeScale=4;
            var pets=Components(Get(s.match,"companions"));var starts=pets.Select(p=>p.transform.position).ToArray();float end=Time.realtimeSinceStartup+75,next=0;bool pee=false,fetch=false;
            while(Time.realtimeSinceStartup<end&&(!pee||!fetch))
            {
                pee|=pets.Any(p=>(int)Get(p,"NaturalPees")>0);fetch|=Components(Get(s.match,"petToys")).Any(t=>(int)Get(t,"PhysicalReturns")>0);if(Time.realtimeSinceStartup>=next){s.Trace("ordinary-ai-pet-care");next=Time.realtimeSinceStartup+4;}yield return null;
            }
            Assert.That(pets.All(p=>Record(p).walked>8),Is.True);Assert.That(pets.Select((p,i)=>Vector3.Distance(p.transform.position,starts[i])).Max(),Is.GreaterThan(10));Assert.That(pee,Is.True,"At least one unprompted ordinary walk includes real leg-lift/grass kicks.");Assert.That(fetch,Is.True,"Actual original dynamic toy force, proximity retrieval and finite joint must produce a return.");Assert.That((bool)Get(s.match,"Finished"),Is.False);
            foreach(var pet in pets)Assert.That((bool)Get(Get(pet,"motor"),"isOnNavMesh"),Is.True);s.Capture("pet-ordinary-ai-walk-care-and-toys",1920,1080);
            Call(s.match,"Pause",0,false);yield return null;yield return null;var positions=pets.Select(p=>p.transform.position).ToArray();yield return null;yield return null;CollectionAssert.AreEqual(positions,pets.Select(p=>p.transform.position).ToArray());Call(s.match,"SetupScreen");Assert.That(pets.All(p=>!((LineRenderer)Get(p,"leash")).enabled),Is.True);
        }
    }
}
#endif
