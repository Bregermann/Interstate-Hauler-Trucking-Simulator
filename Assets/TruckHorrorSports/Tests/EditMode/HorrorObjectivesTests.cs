using NUnit.Framework;
using LWS.TruckHorrorSports;
public sealed class HorrorObjectivesTests
{
    [Test] public void CooperativeTaskIsCommittedOnceAcrossAllParticipants()
    { var r = new HorrorObjectives(); r.Begin(4, true); Assert.That(r.Commit(0, 2)); Assert.That(r.Commit(3, 2), Is.False); Assert.That(r.tasks[0], Is.EqualTo(1)); Assert.That(r.TotalTasks, Is.EqualTo(1)); Assert.That(r.Done(2, 2)); }
    [Test] public void CompetitiveParticipantsCompleteTheirOwnObjectives()
    { var r = new HorrorObjectives(); r.Begin(4, false); Assert.That(r.Commit(0, 2)); Assert.That(r.Commit(3, 2)); Assert.That(r.TotalTasks, Is.EqualTo(2)); Assert.That(r.Done(1, 2), Is.False); }
    [Test] public void LostParticipantCannotCompleteTasks()
    { var r = new HorrorObjectives(); r.Begin(4, false); r.Damage(1, 120); Assert.That(r.outcome[1], Is.EqualTo(HorrorOutcome.Lost)); Assert.That(r.Commit(1, 0), Is.False); }
    [Test] public void CooperativePlayNeverReturnsCompetitivePlace()
    { var r = new HorrorObjectives(); r.Begin(4, true); r.Commit(1, 3); r.outcome[1] = HorrorOutcome.Escaped; for (int i = 0; i < 4; i++) Assert.That(r.Place(i), Is.Zero); }
    [Test] public void EscapeAndTasksDetermineActualCompetitiveOutcome()
    { var r = new HorrorObjectives(); r.Begin(4, false); r.Commit(0, 1); r.Commit(1, 1); r.outcome[1] = HorrorOutcome.Escaped; Assert.That(r.Place(1), Is.EqualTo(1)); Assert.That(r.Place(0), Is.EqualTo(2)); }
    [Test] public void FasterEscapeWinsWhenBothParticipantsCompleteSameTasks()
    { var r = new HorrorObjectives(); r.Begin(2, false); r.Commit(0, 1); r.Commit(1, 1); r.outcome[0] = r.outcome[1] = HorrorOutcome.Escaped; r.survival[0] = 80; r.survival[1] = 120; Assert.That(r.Place(0), Is.EqualTo(1)); Assert.That(r.Place(1), Is.EqualTo(2)); }
    [Test] public void RematchClearsObjectivesOutcomesHealthAndExposure()
    { var r = new HorrorObjectives(); r.Begin(4, false); r.Commit(2, 1); r.Damage(0, 99); r.exposure[2] = 40; r.outcome[3] = HorrorOutcome.Escaped; r.Begin(2, true); Assert.That(r.TotalTasks, Is.Zero); Assert.That(r.Done(0, 1), Is.False); Assert.That(r.health[0], Is.EqualTo(100)); Assert.That(r.exposure[2], Is.Zero); Assert.That(r.outcome[3], Is.EqualTo(HorrorOutcome.Active)); }
    [TestCase(-1)][TestCase(32)] public void InvalidObjectiveCannotScore(int id)
    { var r = new HorrorObjectives(); r.Begin(4, false); Assert.That(r.Commit(0, id), Is.False); Assert.That(r.TotalTasks, Is.Zero); }
    [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-30)] public void InvalidDamageCannotCorruptHealth(float damage)
    { var r = new HorrorObjectives(); r.Begin(1, false); r.Damage(0, damage); Assert.That(r.health[0], Is.EqualTo(100)); }
    [Test] public void EveryConvoyRigMustArriveBeforeTheSharedWardCompletes()
    {
        var r = new HorrorObjectives(); r.Begin(3, true);
        Assert.That(r.RegisterConvoyArrival(0, 0)); Assert.That(r.RegisterConvoyArrival(1, 0)); Assert.That(r.AllConvoyArrived(0), Is.False);
        Assert.That(r.RegisterConvoyArrival(2, 0)); Assert.That(r.AllConvoyArrived(0)); Assert.That(r.Commit(2, 0));
        Assert.That(r.TotalTasks, Is.EqualTo(1)); Assert.That(r.convoyStops[0], Is.EqualTo(1)); Assert.That(r.convoyStops[1], Is.EqualTo(1)); Assert.That(r.convoyStops[2], Is.EqualTo(1));
        Assert.That(r.RegisterConvoyArrival(0, 0), Is.False); Assert.That(r.convoyStops[0], Is.EqualTo(1));
    }
    [Test] public void AFailedRigCannotBeCountedAsSuccessfulConvoyDelivery()
    { var r = new HorrorObjectives(); r.Begin(2, true); r.RegisterConvoyArrival(0, 0); r.RegisterConvoyArrival(1, 0); r.Damage(1, 100); Assert.That(r.AllConvoyArrived(0), Is.False); }
    [Test] public void CooperativeRescueRestoresARealLostParticipantOnlyOnce()
    { var r = new HorrorObjectives(); r.Begin(2, true); r.Damage(1, 100); Assert.That(r.RegisterRescue(0, 1)); Assert.That(r.outcome[1], Is.EqualTo(HorrorOutcome.Active)); Assert.That(r.health[1], Is.EqualTo(40)); Assert.That(r.rescues[0], Is.EqualTo(1)); Assert.That(r.RegisterRescue(0, 1), Is.False); Assert.That(r.rescues[0], Is.EqualTo(1)); Assert.That(r.Place(0), Is.Zero); }
    [Test] public void CompetitiveOpponentsCannotUseCooperativeRescue()
    { var r = new HorrorObjectives(); r.Begin(2, false); r.Damage(1, 100); Assert.That(r.RegisterRescue(0, 1), Is.False); Assert.That(r.outcome[1], Is.EqualTo(HorrorOutcome.Lost)); }
    [Test] public void SurvivalRoadGatesRequireOrderedPassagesAndAllowAnotherLap()
    { var r = new HorrorObjectives(); r.Begin(1, false); Assert.That(r.RegisterRoadGate(0, 0, 4)); Assert.That(r.RegisterRoadGate(0, 0, 4), Is.False); Assert.That(r.RegisterRoadGate(0, 2, 4), Is.False); Assert.That(r.RegisterRoadGate(0, 1, 4)); Assert.That(r.RegisterRoadGate(0, 2, 4)); Assert.That(r.RegisterRoadGate(0, 3, 4)); Assert.That(r.RegisterRoadGate(0, 0, 4)); Assert.That(r.roadGates[0], Is.EqualTo(5)); }
    [Test] public void RematchClearsModeSpecificArrivalsRescuesAndDistance()
    { var r = new HorrorObjectives(); r.Begin(2, true); r.RegisterConvoyArrival(0, 1); r.Damage(1, 100); r.RegisterRescue(0, 1); r.distance[0] = 120; r.containmentStops[0] = 2; r.wrongSignals[0] = 1; r.Begin(2, true); Assert.That(r.ConvoyArrived(0, 1), Is.False); Assert.That(r.convoyStops[0], Is.Zero); Assert.That(r.rescues[0], Is.Zero); Assert.That(r.distance[0], Is.Zero); Assert.That(r.containmentStops[0], Is.Zero); Assert.That(r.wrongSignals[0], Is.Zero); }
    [Test] public void TaskRushReopensJobsWithoutErasingEarnedWork()
    { var r = new HorrorObjectives(); r.Begin(1, false); r.Commit(0, 0); r.Commit(0, 1); Assert.That(r.BeginNextTaskCycle(0)); Assert.That(r.tasks[0], Is.EqualTo(2)); Assert.That(r.taskCycles[0], Is.EqualTo(1)); Assert.That(r.Done(0, 0), Is.False); Assert.That(r.Commit(0, 0)); Assert.That(r.tasks[0], Is.EqualTo(3)); }
    [Test] public void ACompetitiveTaskCycleCannotResetCooperativeObjectives()
    { var r = new HorrorObjectives(); r.Begin(2, true); r.Commit(0, 0); Assert.That(r.BeginNextTaskCycle(1), Is.False); Assert.That(r.Done(1, 0)); }
    [TestCase(HorrorSport.GraveyardShift)] [TestCase(HorrorSport.ScarecrowSeason)]
    [TestCase(HorrorSport.DeadEnd)] [TestCase(HorrorSport.CursedCargo)]
    public void PartialDefaultTimeoutNeverQualifiesAsAWin(HorrorSport sport)
    { var r = new HorrorObjectives(); r.Begin(1, false); r.Commit(0, 0); r.survival[0] = 240; Assert.That(r.QualifiesCompetitiveResult(sport, 0, 0, true, false, 4, 240, true), Is.False); }
    [TestCase(HorrorSport.GraveyardShift, HorrorOutcome.NightComplete)]
    [TestCase(HorrorSport.ScarecrowSeason, HorrorOutcome.Escaped)]
    [TestCase(HorrorSport.DeadEnd, HorrorOutcome.Escaped)]
    [TestCase(HorrorSport.CursedCargo, HorrorOutcome.Escaped)]
    public void DefaultCompletionRequiresThePhysicalExitReceipt(HorrorSport sport, HorrorOutcome outcome)
    { var r = new HorrorObjectives(); r.Begin(1, false); r.outcome[0] = outcome; Assert.That(r.QualifiesCompetitiveResult(sport, 0, 0, true, false, 4, 240, true), Is.False); Assert.That(r.QualifiesCompetitiveResult(sport, 0, 0, true, true, 4, 240, true)); Assert.That(r.QualifiesCompetitiveResult(sport, 0, 0, false, true, 4, 240, true), Is.False); }
    [Test] public void BankedTaskRushWorkRemainsValidAfterARealLoss()
    { var r = new HorrorObjectives(); r.Begin(1, false); Assert.That(r.QualifiesCompetitiveResult(HorrorSport.GraveyardShift, 1, 0, true, false, 6, 240, true), Is.False); r.Commit(0, 0); r.Damage(0, 100); Assert.That(r.QualifiesCompetitiveResult(HorrorSport.GraveyardShift, 1, 0, true, false, 6, 240, true)); }
    [Test] public void PursuitSurvivalRequiresAFullNightAndActualRoadProgress()
    { var r = new HorrorObjectives(); r.Begin(1, false); r.survival[0] = 240; Assert.That(r.QualifiesCompetitiveResult(HorrorSport.DeadEnd, 1, 0, true, false, 4, 240, true), Is.False); r.distance[0] = 180; r.RegisterRoadGate(0, 0, 4); Assert.That(r.QualifiesCompetitiveResult(HorrorSport.DeadEnd, 1, 0, true, false, 4, 240, true)); r.survival[0] = 20; Assert.That(r.QualifiesCompetitiveResult(HorrorSport.DeadEnd, 1, 0, true, false, 4, 240, true), Is.False); }
    [Test] public void EarlyLastSurvivorQualifiesButEliminatedAndInstantResultsDoNot()
    { var r = new HorrorObjectives(); r.Begin(2, false); r.survival[0] = 21; Assert.That(r.QualifiesCompetitiveResult(HorrorSport.ScarecrowSeason, 2, 0, true, false, 4, 240, true)); r.Damage(0, 100); Assert.That(r.QualifiesCompetitiveResult(HorrorSport.ScarecrowSeason, 2, 0, true, false, 4, 240, true), Is.False); Assert.That(r.QualifiesCompetitiveResult(HorrorSport.ScarecrowSeason, 2, 1, true, false, 4, 240, true), Is.False); }
    [Test] public void FreightSurvivalNeedsOriginalIntactLoadAndRealContainmentWork()
    { var r = new HorrorObjectives(); r.Begin(1, false); r.survival[0] = 240; Assert.That(r.QualifiesCompetitiveResult(HorrorSport.CursedCargo, 3, 0, true, false, 3, 240, true), Is.False); r.containmentStops[0] = 1; Assert.That(r.QualifiesCompetitiveResult(HorrorSport.CursedCargo, 3, 0, true, false, 3, 240, true)); Assert.That(r.QualifiesCompetitiveResult(HorrorSport.CursedCargo, 3, 0, true, false, 3, 240, false), Is.False); }
    [Test] public void ExorcismNeedsAllActualWardReceiptsAndContainedOutcome()
    { var r = new HorrorObjectives(); r.Begin(1, false); r.Commit(0, 0); r.Commit(0, 1); r.Commit(0, 2); Assert.That(r.QualifiesCompetitiveResult(HorrorSport.CursedCargo, 2, 0, true, false, 3, 240, true), Is.False); r.outcome[0] = HorrorOutcome.Contained; Assert.That(r.QualifiesCompetitiveResult(HorrorSport.CursedCargo, 2, 0, true, false, 3, 240, true)); }
    [Test] public void CooperativeSuccessNeverQualifiesForIndividualCompetitivePoints()
    { var r = new HorrorObjectives(); r.Begin(2, true); r.Commit(0, 0); r.outcome[0] = HorrorOutcome.Escaped; Assert.That(r.QualifiesCompetitiveResult(HorrorSport.ScarecrowSeason, 3, 0, true, true, 4, 240, true), Is.False); }
}
