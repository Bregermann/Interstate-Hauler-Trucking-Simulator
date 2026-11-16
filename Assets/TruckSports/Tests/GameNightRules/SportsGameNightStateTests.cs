using NUnit.Framework;
public sealed class SportsGameNightStateTests
{
    [Test] public void TwoSportNightPreservesOwnershipReadinessAndExactlyOnceHumanAwards() { Assert.That(PartyStateChecks.Run(),Is.EqualTo(38)); }
    [Test] public void RestartConsumesInterruptedMatchAndRequiresFreshReadyCheck()
    {
        var state = new LWS.TruckSports.SportsGameNightState();
        state.Join("pad1"); state.SelectProfile(0, "driver", false); state.SetReady(0, true);
        state.OpenSelection("pad1"); state.ConfigureSport("pad1", "TruckMusicalParking", "clear"); state.SetMatchParticipants(new[] { "driver" });
        Assert.That(state.ConfirmMatchStarted("interrupted", new[] { "driver" }), Is.True);
        Assert.That(state.RestartSetup("other"), Is.False);
        Assert.That(state.RestartSetup("pad1"), Is.True);
        Assert.That(state.Phase, Is.EqualTo(LWS.TruckSports.SportsGameNightPhase.Configuration));
        Assert.That(state.AllReady, Is.False); Assert.That(state.Points["driver"], Is.Zero);
        Assert.That(state.CompleteMatch("interrupted", new[] { new LWS.TruckSports.SportsGameNightOutcome("driver", false, true) }), Is.False);
        state.SetReady(0, true); Assert.That(state.ConfirmMatchStarted("rematch", new[] { "driver" }), Is.True);
        Assert.That(state.CompleteMatch("rematch", new[] { new LWS.TruckSports.SportsGameNightOutcome("driver", false, true) }), Is.True);
        Assert.That(state.Points["driver"], Is.EqualTo(1));
    }
}
