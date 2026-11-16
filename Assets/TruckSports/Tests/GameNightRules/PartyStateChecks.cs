using System;
using LWS.TruckSports;
public static class PartyStateChecks
{
    static int checks;
    static void Check(bool value,string message) { checks++; if(!value) throw new Exception(message); }
    static SportsGameNightState Ready()
    {
        var s=new SportsGameNightState();s.Join("pad1");s.Join("pad2");s.SelectProfile(0,"alice",false);s.SelectProfile(1,"bob",false);s.SetReady(0,true);s.SetReady(1,true);return s;
    }
    static void Start(SportsGameNightState s,string match)
    {
        Check(s.OpenSelection("pad1"),"Host selection");Check(s.ConfigureSport("pad1","TruckMusicalParking","standard"),"Configure");Check(s.ConfirmMatchStarted(match,new[]{"alice","bob"}),"Begin actual match");
    }
    public static int Run()
    {
        checks=0;var s=new SportsGameNightState();Check(!s.AllReady,"Empty lobby cannot start");Check(s.Join("pad1")==0,"First player owns slot1");Check(s.Join("pad1")==-1,"One controller cannot join twice");Check(!s.SetReady(0,true),"Profile required");s.Join("pad2");Check(s.SelectProfile(0,"same-name-id-a",false),"Profile assignment");Check(!s.SelectProfile(1,"same-name-id-a",false),"Unique identities across seats");Check(s.SelectProfile(1,"same-name-id-b",false),"Different IDs may share a display name externally");s.SetReady(0,true);s.SetReady(1,true);Check(s.AllReady,"Both humans ready");Check(!s.OpenSelection("pad2"),"Nonhost cannot select sport");s.SetConnection("pad1",false);Check(!s.AllReady&&!s.IsHost("pad1"),"Disconnect invalidates readiness and host control");Check(s.Seats[0].ProfileId=="same-name-id-a","Disconnect preserves profile");Check(s.ReplaceController(0,"replacement"),"Intentional replacement");Check(s.Seats[0].ProfileId=="same-name-id-a"&&!s.Seats[0].Ready,"Replacement retains identity but needs ready");Check(!s.ReplaceController(1,"replacement"),"No double assignment");
        s=Ready();Start(s,"m1");Check(s.Join("pad3")==-1,"No joining during match");Check(!s.Leave(0),"Do not remove live driver");Check(s.CompleteMatch("m1",new[]{new SportsGameNightOutcome("alice",false,true),new SportsGameNightOutcome("bob",false,false),new SportsGameNightOutcome("bot",true,true)}),"Official match completes");Check(s.Points["alice"]==1&&s.Points["bob"]==0&&!s.Points.ContainsKey("bot"),"Human winner gets one, AI gets no human points");Check(!s.CompleteMatch("m1",new[]{new SportsGameNightOutcome("alice",false,true)}),"Repeated result cannot award twice");Check(s.ReturnToLobby()&&!s.AllReady&&s.Points["alice"]==1,"Lobby retains points, clears ready");s.SetReady(0,true);s.SetReady(1,true);Check(s.OpenSelection("pad1")&&s.ConfigureSport("pad1","TruckBowling","standard"),"Second distinct sport");Check(s.ChangeConfiguration("hard")&&!s.AllReady&&!s.CanStart("pad1"),"Settings changes invalidate ready");s.SetReady(0,true);s.SetReady(1,true);Check(s.ConfirmMatchStarted("m2",new[]{"alice","bob"}),"Second match starts");Check(!s.CompleteMatch("m2",new[]{new SportsGameNightOutcome("alice",false,true)}),"Reject incomplete official results without awarding");Check(s.Points["alice"]==1,"Failed result cannot mutate points");Check(s.CompleteMatch("m2",new[]{new SportsGameNightOutcome("alice",false,true),new SportsGameNightOutcome("bob",false,true)}),"Shared winners");Check(s.Points["alice"]==2&&s.Points["bob"]==1,"Tied human winners each get one");s.ReturnToLobby();Check(!s.EndNight("pad2")&&s.EndNight("pad1"),"Only host ends night");Check(!s.SetReady(0,true)&&s.Join("new")==-1,"Ended night is closed");
        s=Ready();Start(s,"aborted");Check(s.AbortMatch("return-to-menu")&&s.Points["alice"]==0,"Interrupted match has no award");s.SetConnection("pad1",false);Check(s.TakeHost(1)&&s.IsHost("pad2")&&s.Seats[0].ProfileId=="bob","Explicit host takeover preserves identities");Check(s.Leave(0)&&s.Seats[0].ProfileId=="alice","Host departure promotes remaining occupied profile");
        return checks;
    }
}
