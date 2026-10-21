using NUnit.Framework;

namespace LWS.TruckKickball.Tests
{
    public sealed class KickballHomeRunRegressionTests
    {
        [Test]public void AlreadyCrossedRunIsIncludedOnceAndThirdOutCannotBecomeHomeRun()
        {
            var rules=new KickballRules();rules.Reset(6,3);rules.Pitch();rules.Kick();int batter=rules.Batter;
            for(int i=1;i<=4;i++)Assert.That(rules.TouchBase(batter,i));
            Assert.That(rules.PendingRuns,Is.EqualTo(1));Assert.That(rules.HomeRun());Assert.That(rules.Scores[0],Is.EqualTo(1));Assert.That(rules.HomeRunRuns,Is.EqualTo(1));
            rules.Reset(6,3);for(int outNumber=0;outNumber<3;outNumber++){rules.Pitch();rules.Kick();Assert.That(rules.Tag(rules.Batter,false));if(outNumber<2)Assert.That(rules.FinishPlay());}
            Assert.That(rules.Outs,Is.EqualTo(3));Assert.That(rules.HomeRun(),Is.False);Assert.That(rules.Scores[0],Is.Zero);
        }
        [Test]public void ConfirmedHomeRunCreditsTheRunBeforeAnyCelebrationOrBaseDriving()
        {
            var rules=new KickballRules();rules.Reset(6,3);rules.Pitch();rules.Kick();int batter=rules.Batter;
            Assert.That(rules.HomeRun());
            Assert.That(rules.Scores[0],Is.EqualTo(1),"The playtest directive supersedes forced physical home-run laps.");
            Assert.That(rules.Runner(batter),Is.False,"A credited home-run batter is no longer a live defensive target.");
            Assert.That(rules.Tag(batter,false),Is.False);Assert.That(rules.Outs,Is.Zero);
            Assert.That(rules.HomeRun(),Is.False,"Repeated fence observations cannot duplicate runs.");
            Assert.That(rules.FinishPlay());Assert.That(rules.Scores[0],Is.EqualTo(1));
        }
        [Test]public void BasesLoadedHomeRunCreditsAllFourRunsAtomically()
        {
            var rules=new KickballRules();rules.Reset(6,3);
            for(int walk=0;walk<3;walk++)
            {
                for(int pitch=0;pitch<4;pitch++){rules.Pitch();rules.PitchPassed(false);}
                for(int b=3;b>=0;b--)for(int i=0;i<rules.Count;i++)if(rules.Runner(i)&&rules.Base[i]==b&&rules.ForcedTo[i]>b)Assert.That(rules.TouchBase(i,b+1));
                Assert.That(rules.FinishPlay());
            }
            rules.Pitch();rules.Kick();Assert.That(rules.HomeRun());
            Assert.That(rules.Scores[0],Is.EqualTo(4));Assert.That(rules.PendingRuns,Is.Zero);
            for(int i=0;i<rules.Count;i++){Assert.That(rules.Runner(i),Is.False);Assert.That(rules.Tag(i,false),Is.False);}
            Assert.That(rules.FinishPlay());Assert.That(rules.Scores[0],Is.EqualTo(4));Assert.That(rules.CompletedPlays,Is.EqualTo(4));
        }
    }
}
