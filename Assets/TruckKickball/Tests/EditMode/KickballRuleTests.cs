using NUnit.Framework;

namespace LWS.TruckKickball.Tests
{
    public sealed class KickballRuleTests
    {
        KickballRules rules;
        [SetUp] public void Setup() { rules = new KickballRules(); rules.Reset(6, 3); }
        void StrikeOut()
        {
            for (int i = 0; i < 3; i++) { Assert.That(rules.Pitch()); Assert.That(rules.PitchPassed(true)); }
            Assert.That(rules.FinishPlay());
        }
        void Walk()
        {
            for (int i = 0; i < 4; i++) { Assert.That(rules.Pitch()); Assert.That(rules.PitchPassed(false)); }
        }
        void Homer()
        {
            Assert.That(rules.Pitch()); Assert.That(rules.Kick()); Assert.That(rules.HomeRun());
            for (int n = 3; n >= 0; n--) for (int i = 0; i < rules.Count; i++)
                if (rules.Runner(i) && rules.Base[i] == n)
                    for (int b = n + 1; b <= 4; b++) Assert.That(rules.TouchBase(i, b));
            Assert.That(rules.FinishPlay());
        }

        [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void TeamSizesCreateActualCompetitorCounts(int size)
        { rules.Reset(size, 3); Assert.That(rules.Count, Is.EqualTo(size * 2)); }
        [TestCase(1)] [TestCase(3)] [TestCase(5)] [TestCase(7)]
        public void SupportedInningsRemainSelectable(int innings)
        { rules.Reset(6, innings); Assert.That(rules.RegulationInnings, Is.EqualTo(innings)); }
        [Test] public void OnlyThreeStrikesRetireBatter()
        { StrikeOut(); Assert.That(rules.Outs, Is.EqualTo(1)); Assert.That(rules.Strikes, Is.Zero); }
        [Test] public void ThreeOutsSwitchSidesOnce()
        { StrikeOut(); StrikeOut(); StrikeOut(); Assert.That(rules.BattingTeam, Is.EqualTo(1)); Assert.That(rules.Inning, Is.EqualTo(1)); Assert.That(rules.Outs, Is.Zero); }
        [Test] public void SixOutsAdvanceAnInning()
        { for (int i = 0; i < 6; i++) StrikeOut(); Assert.That(rules.Inning, Is.EqualTo(2)); Assert.That(rules.BattingTeam, Is.Zero); }
        [Test] public void FourBallsRequirePhysicalFirstBaseArrival()
        { Walk(); Assert.That(rules.Phase, Is.EqualTo(KickballPhase.Walk)); Assert.That(rules.FinishPlay(), Is.False); Assert.That(rules.TouchBase(rules.Batter, 1)); Assert.That(rules.FinishPlay()); }
        [Test] public void BaseOrderCannotBeSkipped()
        { rules.Pitch(); rules.Kick(); Assert.That(rules.TouchBase(rules.Batter, 2), Is.False); Assert.That(rules.Scores[0], Is.Zero); }
        [Test] public void CrossingHomeTwiceCannotDuplicateRun()
        { rules.Pitch(); rules.Kick(); int b = rules.Batter; for (int n = 1; n <= 4; n++) Assert.That(rules.TouchBase(b, n)); Assert.That(rules.TouchBase(b, 4), Is.False); Assert.That(rules.Scores[0], Is.Zero); rules.FinishPlay(); Assert.That(rules.Scores[0], Is.EqualTo(1)); }
        [Test] public void CompletedPlayCannotBeFinalizedTwice()
        { Homer(); Assert.That(rules.FinishPlay(), Is.False); Assert.That(rules.Scores[0], Is.EqualTo(1)); }
        [Test] public void RepeatedFoulsDoNotCreateThirdStrike()
        { for (int n = 0; n < 4; n++) { Assert.That(rules.Pitch()); rules.Kick(); rules.Foul(); rules.FinishFoulReturn(); } Assert.That(rules.Strikes, Is.EqualTo(2)); Assert.That(rules.Outs, Is.Zero); }
        [Test] public void FoulRestoresRunnersWithoutAwardingRuns()
        { Walk(); int runner = rules.Batter; rules.TouchBase(runner, 1); rules.FinishPlay(); rules.Pitch(); rules.Kick(); rules.TouchBase(runner, 2); rules.Foul(); Assert.That(rules.Base[runner], Is.EqualTo(1)); Assert.That(rules.FinishFoulReturn(), Is.False); rules.TouchBase(runner, 1); Assert.That(rules.FinishFoulReturn()); Assert.That(rules.Scores[0], Is.Zero); }
        [Test] public void ForceOutNeedsAForcedRunner()
        { rules.Pitch(); rules.Kick(); Assert.That(rules.ForceAt(2), Is.EqualTo(-1)); Assert.That(rules.ForceAt(1), Is.EqualTo(rules.Batter)); Assert.That(rules.ForceAt(1), Is.EqualTo(-1)); Assert.That(rules.Outs, Is.EqualTo(1)); }
        [Test] public void SafeRunnerCannotBeTaggedOut()
        { rules.Pitch(); rules.Kick(); rules.TouchBase(rules.Batter, 1); Assert.That(rules.Tag(rules.Batter, true), Is.False); Assert.That(rules.Tag(rules.Batter, false)); }
        [Test] public void FlyOutRetiresKickerAndRequiresPhysicalTagUp()
        { Walk(); int runner = rules.Batter; rules.TouchBase(runner, 1); rules.FinishPlay(); rules.Pitch(); rules.Kick(); rules.FlyCaught(); Assert.That(rules.Outs, Is.EqualTo(1)); Assert.That(rules.TouchBase(runner, 2), Is.False); Assert.That(rules.TouchBase(runner, 1)); Assert.That(rules.TouchBase(runner, 2)); }
        [Test] public void HomeRunSealsWithoutRequiringPhysicalBaseDriving()
        { rules.Pitch(); rules.Kick(); rules.HomeRun(); Assert.That(rules.Scores[0], Is.EqualTo(1)); Assert.That(rules.TouchBase(rules.Batter, 1), Is.False); Assert.That(rules.FinishPlay()); Assert.That(rules.Scores[0], Is.EqualTo(1)); }
        [Test] public void ThirdForceOutCancelsSamePlayRuns()
        { StrikeOut(); StrikeOut(); Walk(); int runner = rules.Batter; rules.TouchBase(runner, 1); rules.FinishPlay(); rules.Pitch(); rules.Kick(); rules.TouchBase(runner, 2); rules.TouchBase(runner, 3); rules.TouchBase(runner, 4); Assert.That(rules.PendingRuns, Is.EqualTo(1)); rules.ForceAt(1); rules.FinishPlay(); Assert.That(rules.Scores[0], Is.Zero); }
        [Test] public void EarlierRunSurvivesThirdNonForceTag()
        { StrikeOut(); StrikeOut(); Walk(); int runner = rules.Batter; rules.TouchBase(runner, 1); rules.FinishPlay(); rules.Pitch(); rules.Kick(); rules.TouchBase(runner, 2); Assert.That(rules.TouchBase(rules.Batter, 1)); for (int n = 3; n <= 4; n++) rules.TouchBase(runner, n); rules.Tag(rules.Batter, false); rules.FinishPlay(); Assert.That(rules.Scores[0], Is.EqualTo(1)); }
        [Test] public void TiedRegulationContinuesIntoExtras()
        { rules.Reset(6, 1); for (int i = 0; i < 6; i++) StrikeOut(); Assert.That(rules.Complete, Is.False); Assert.That(rules.Inning, Is.EqualTo(2)); }
        [Test] public void HomeTeamLeadDoesNotRequireUnnecessaryBottomHalf()
        { rules.Reset(6, 1); for (int i = 0; i < 3; i++) StrikeOut(); Homer(); Assert.That(rules.Complete); Assert.That(rules.Winner, Is.EqualTo(1)); }
        [Test] public void AwayWinRequiresBottomHalfToEnd()
        { rules.Reset(6, 1); Homer(); for (int i = 0; i < 6; i++) StrikeOut(); Assert.That(rules.Complete); Assert.That(rules.Winner, Is.Zero); }
        [Test] public void ResetClearsMatchAndRunnerHistory()
        { Homer(); rules.Reset(3, 5); Assert.That(rules.Scores[0], Is.Zero); Assert.That(rules.CompletedPlays, Is.Zero); Assert.That(rules.LegalKicks, Is.Zero); Assert.That(rules.Inning, Is.EqualTo(1)); foreach (bool value in rules.Active) Assert.That(value, Is.False); }
        [Test] public void GroundRuleDoubleStillNeedsBothPhysicalBases()
        { rules.Pitch(); rules.Kick(); rules.GroundRuleDouble(); Assert.That(rules.FinishPlay(), Is.False); rules.TouchBase(rules.Batter, 1); Assert.That(rules.FinishPlay(), Is.False); rules.TouchBase(rules.Batter, 2); Assert.That(rules.FinishPlay()); Assert.That(rules.Scores[0], Is.Zero); }
        [Test] public void EarlyRunBeforeFlyCatchMustReturnAndTag()
        { Walk(); int runner = rules.Batter; rules.TouchBase(runner, 1); rules.FinishPlay(); rules.Pitch(); rules.Kick(); for (int n = 2; n <= 4; n++) rules.TouchBase(runner, n); Assert.That(rules.PendingRuns, Is.EqualTo(1)); rules.FlyCaught(); Assert.That(rules.PendingRuns, Is.Zero); Assert.That(rules.Active[runner]); Assert.That(rules.Base[runner], Is.EqualTo(1)); Assert.That(rules.TagRequired[runner]); }
        [Test] public void ThreeASideLoadedBasesNeverInventGhostRunner()
        {
            rules.Reset(3, 1); Walk(); int a = rules.Batter; rules.TouchBase(a, 1); rules.FinishPlay();
            Walk(); int b = rules.Batter; rules.TouchBase(a, 2); rules.TouchBase(b, 1); rules.FinishPlay();
            Walk(); int c = rules.Batter; rules.TouchBase(a, 3); rules.TouchBase(b, 2); rules.TouchBase(c, 1);
            Assert.That(rules.FinishPlay(), Is.False); Assert.That(rules.Phase, Is.EqualTo(KickballPhase.Live));
            rules.TouchBase(a, 4); Assert.That(rules.FinishPlay()); Assert.That(rules.Count, Is.EqualTo(6)); Assert.That(rules.Scores[0], Is.EqualTo(1));
        }
    }
}
