using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiRaceTests
    {
        private static readonly Vector3[] Gates = {
            new Vector3(0,0,0), new Vector3(20,0,0),
            new Vector3(20,0,20), new Vector3(0,0,20) };

        private static void Cross(TruckTaxiRaceProgress progress, int gate, float time)
        {
            Vector3 forward = (Gates[(gate+1)%4]-Gates[(gate+3)%4]).normalized;
            progress.Tick(Gates[gate]-forward*2,time);
            progress.Tick(Gates[gate]+forward*2,time+.1f);
        }

        [Test]
        public void OrderedDirectedGatesCompleteLapAndRecordBest()
        {
            var settings = new TruckTaxiRaceSettings { maximumStepMeters = 50 };
            var progress = new TruckTaxiRaceProgress(Gates,settings);
            progress.Begin(Gates[0]-Vector3.right*2,0);
            Cross(progress,0,1); Cross(progress,1,3); Cross(progress,2,5); Cross(progress,3,7);
            Assert.That(progress.CompletedLaps,Is.Zero);
            Assert.That(progress.NextCheckpoint,Is.Zero);
            Cross(progress,0,9);
            Assert.That(progress.CompletedLaps,Is.EqualTo(1));
            Assert.That(progress.PassedCheckpoints,Is.EqualTo(5));
            Assert.That(progress.BestLapSeconds,Is.GreaterThan(9));
            Assert.That(progress.NextCheckpoint,Is.EqualTo(1));
        }

        [Test]
        public void SkippedReverseAndTeleportGatesDoNotCount()
        {
            var settings = new TruckTaxiRaceSettings { maximumStepMeters = 10 };
            var progress = new TruckTaxiRaceProgress(Gates,settings);
            progress.Begin(Gates[0]-Vector3.right*2,0);
            Cross(progress,1,1);
            Assert.That(progress.PassedCheckpoints,Is.Zero);
            progress.Tick(Gates[0]+Vector3.right*2,2);
            progress.Tick(Gates[0]-Vector3.right*2,2.1f);
            Assert.That(progress.PassedCheckpoints,Is.Zero);
            progress.Tick(new Vector3(100,0,100),3);
            Assert.That(progress.PassedCheckpoints,Is.Zero);
            Assert.That(progress.NextCheckpoint,Is.Zero);
        }

        [Test]
        public void ResetInvalidatesPartialLapWithoutErasingBestLap()
        {
            var settings = new TruckTaxiRaceSettings { maximumStepMeters = 50 };
            var progress = new TruckTaxiRaceProgress(Gates,settings);
            progress.Begin(Gates[0]-Vector3.right*2,0);
            Cross(progress,0,1); Cross(progress,1,3); Cross(progress,2,5); Cross(progress,3,7); Cross(progress,0,9);
            float best = progress.BestLapSeconds;
            Cross(progress,1,11);
            progress.Invalidate(Gates[1],12);
            Assert.That(progress.CompletedLaps,Is.EqualTo(1));
            Assert.That(progress.PassedCheckpoints,Is.EqualTo(4));
            Assert.That(progress.NextCheckpoint,Is.Zero);
            Assert.That(progress.BestLapSeconds,Is.EqualTo(best));
        }

        [Test]
        public void ModesAndMedalsUseConfiguredValues()
        {
            var settings = new TruckTaxiRaceSettings();
            Assert.That(settings.Laps(TruckTaxiRaceMode.Practice,5),Is.Zero);
            Assert.That(settings.Laps(TruckTaxiRaceMode.TimeTrial,5),Is.EqualTo(5));
            Assert.That(settings.Laps(TruckTaxiRaceMode.EventRace,1),Is.EqualTo(5));
            Assert.That(settings.Prize(TruckTaxiRaceMode.TimeTrial,1,0),Is.Zero);
            Assert.That(settings.Medal(60),Is.EqualTo("GOLD"));
            Assert.That(settings.Prize(TruckTaxiRaceMode.TruckRace,1,100),Is.EqualTo(5000));
            Assert.That(settings.Prize(TruckTaxiRaceMode.TruckRace,4,100),Is.Zero);
            Assert.That(TruckTaxiRaceSettings.Personality(0),Is.EqualTo(TruckTaxiRacePersonality.Cautious));
            Assert.That(TruckTaxiRaceSettings.Personality(4),Is.EqualTo(TruckTaxiRacePersonality.Reckless));
            Assert.That(TruckTaxiRaceSettings.SpeedScale(TruckTaxiRacePersonality.Reckless),
                Is.GreaterThan(TruckTaxiRaceSettings.SpeedScale(TruckTaxiRacePersonality.Normal)));
        }
    }
}
