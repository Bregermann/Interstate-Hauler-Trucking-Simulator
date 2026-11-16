#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
public sealed class DragonRiderPromptOwnershipTests
{
    static Type Match=>Type.GetType("LWS.TruckDragonRiders.DragonRidersMatch, Assembly-CSharp",true);
    static object Phase(string value)=>Enum.Parse(Type.GetType("LWS.TruckDragonRiders.DragonRiderPhase, Assembly-CSharp",true),value);
    static string Call(string method,params object[] args)=>(string)Match.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    static string Prompt(string id,bool human,string phase,bool chute,string notice,string reason,string eventId,bool active,string announcement)=>Call("ResolveOwnedViewportPrompt",id,human,Phase(phase),chute,notice,reason,eventId,active,announcement);
    [Test]
    public void DuplicateDisplayNamesCannotBroadcastAnotherParticipantsAnnouncementIntoMountedHumanViewports()
    {
        const string eventText="Alex · HEAVY AERIAL COLLISION";
        Assert.That(Prompt("crew-a",true,"Mounted",false,"MOUNTED",null,"crew-ai",true,eventText),Is.Null);
        Assert.That(Prompt("crew-b",true,"Mounted",false,"MOUNTED",null,"crew-ai",true,eventText),Is.Null);
        Assert.That(Prompt("crew-a",true,"Mounted",false,"MOUNTED",null,"crew-b",true,eventText),Is.Null);
        Assert.That(Prompt("crew-b",true,"Mounted",false,"MOUNTED",null,"crew-b",true,eventText),Is.EqualTo(eventText));
        Assert.That(Prompt("crew-b",true,"Mounted",false,"MOUNTED",null,"crew-b",false,eventText),Is.Null);
    }
    [TestCase("Falling")]
    [TestCase("RescueApproach")]
    public void ActionableChutePromptBelongsOnlyToItsHumanOwnerUntilActuallyDeployed(string phase)
    {
        Assert.That(Prompt("crew-a",true,phase,false,"COLLISION · DEPLOY CHUTE","COLLISION","crew-ai",false,null),Does.Contain("DEPLOY CHUTE"));
        Assert.That(Prompt("crew-ai",false,phase,false,"COLLISION · DEPLOY CHUTE","COLLISION","crew-ai",true,"AI COLLISION"),Does.Not.Contain("DEPLOY"));
        string deployed=Prompt("crew-a",true,phase,true,"COLLISION · DEPLOY CHUTE","COLLISION",null,false,null);Assert.That(deployed,Does.Contain("CHUTE OPEN"));Assert.That(deployed,Does.Not.Contain("DEPLOY"));
    }
    [Test]
    public void SharedEventRetainsCollisionButRemovesOnlyItsOwnerSpecificControlInstruction()
    {
        Assert.That(Call("NeutralPublicAnnouncement","HEAVY AERIAL COLLISION · DEPLOY CHUTE","HEAVY AERIAL COLLISION"),Is.EqualTo("HEAVY AERIAL COLLISION"));
        Assert.That(Call("NeutralPublicAnnouncement","FIRE HIT","HEAVY AERIAL COLLISION"),Is.EqualTo("FIRE HIT"));
    }
    [Test]
    public void SharedArenaNamesTheActualHumanCrewAndNeverRequestsAiInput()
    {
        string first=Call("NamedSharedHumanPrompt",0,"Alex","crew-a",true,Phase("Falling"),false,"COLLISION · DEPLOY CHUTE","COLLISION");string second=Call("NamedSharedHumanPrompt",1,"Alex","crew-b",true,Phase("Falling"),false,"COLLISION · DEPLOY CHUTE","COLLISION");Assert.That(first,Does.Contain("CREW 1 · Alex").And.Contain("DEPLOY CHUTE"));Assert.That(second,Does.Contain("CREW 2 · Alex").And.Contain("DEPLOY CHUTE"));Assert.That(first,Is.Not.EqualTo(second));
        Assert.That(Call("NamedSharedHumanPrompt",2,"Alex","crew-ai",false,Phase("Falling"),false,"COLLISION · DEPLOY CHUTE","COLLISION"),Is.Null);
        Assert.That(Call("NamedSharedHumanPrompt",0,"Alex","crew-a",true,Phase("Mounted"),false,"MOUNTED","COLLISION"),Is.Null);
    }
    [Test]
    public void UnjoinedViewHasNoActionPromptAndRescueStateKeepsItsOwnNotice()
    {
        Assert.That(Prompt(null,true,"Falling",false,"COLLISION · DEPLOY CHUTE","COLLISION","crew-a",true,"COLLISION"),Is.Null);Assert.That(Prompt("crew-a",true,"ClawCarry",true,"CLAW CAPTURE",null,"crew-ai",true,"AI COLLISION"),Is.EqualTo("CLAW CAPTURE"));
    }
}
#endif
