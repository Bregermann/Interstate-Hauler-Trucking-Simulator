using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiSnowTests
    {
        private static TruckTaxiSnowRegion Region()
        {
            var region = new TruckTaxiSnowRegion(.6f);
            region.AddLane(new LwsTrafficLaneDefinition
            {
                laneId = "test", laneWidthMeters = 5,
                centerline = new[] { Vector3.zero, new Vector3(0, 0, 12) }
            }, 32);
            return region;
        }

        [Test] public void BlizzardDepthAccumulatesAndCapsAtSixTenths()
        {
            var region = Region();
            Assert.IsTrue(region.Accumulate(.2f));
            Assert.AreEqual(.2f, region.DepthAt(new Vector3(0, 0, 2)), .001f);
            Assert.IsTrue(region.Accumulate(1f));
            Assert.AreEqual(.6f, region.DepthAt(new Vector3(0, 0, 2)), .001f);
            Assert.IsFalse(region.Accumulate(1f));
            Assert.AreEqual(TruckTaxiPlowState.Unplowed, region.Cells[0].State(.6f));
        }

        [Test] public void PlowClearsOnlySweptRoadStripAndSnowCanReturn()
        {
            var region = Region();
            region.Accumulate(.6f);
            Assert.Greater(region.ClearSweep(new Vector3(0, 0, 1), new Vector3(0, 0, 5), 4.5f, .04f), 0);
            Assert.AreEqual(.04f, region.DepthAt(new Vector3(0, 0, 2)), .001f);
            Assert.AreEqual(TruckTaxiPlowState.Plowed, region.Cells[0].State(.6f));
            Assert.AreEqual(.6f, region.DepthAt(new Vector3(0, 0, 11)), .001f);
            Assert.AreEqual(0, region.DepthAt(new Vector3(30, 0, 2)));
            Assert.AreEqual(0, region.ClearSweep(new Vector3(30, 0, 1), new Vector3(30, 0, 5), 4.5f, .04f));
            region.Accumulate(.1f);
            Assert.AreEqual(.14f, region.DepthAt(new Vector3(0, 0, 2)), .001f);
            Assert.AreEqual(TruckTaxiPlowState.PartiallyPlowed, region.Cells[0].State(.6f));
        }

        [Test] public void LocalTractionRecoversOnClearedCells()
        {
            var region = Region();
            region.Accumulate(.6f);
            float deepGrip = TruckTaxiSnowTraction.GripForDepth(region.DepthAt(new Vector3(0, 0, 2)), region.MaximumDepth);
            float deepRolling = TruckTaxiSnowTraction.RollingForDepth(.6f, region.MaximumDepth);
            region.ClearSweep(new Vector3(0, 0, 1), new Vector3(0, 0, 5), 4.5f, .04f);
            float clearedGrip = TruckTaxiSnowTraction.GripForDepth(region.DepthAt(new Vector3(0, 0, 2)), region.MaximumDepth);
            Assert.Less(deepGrip, .5f);
            Assert.Greater(deepRolling, 2f);
            Assert.Greater(clearedGrip, deepGrip);
        }

        [Test] public void ClearWeatherMeltsDepthAndDryMultipliersAreNeutral()
        {
            var region = Region();
            region.Accumulate(.6f);
            Assert.IsTrue(region.Melt(.2f));
            Assert.AreEqual(.4f, region.DepthAt(new Vector3(0, 0, 2)), .001f);
            Assert.IsTrue(region.Melt(.6f));
            Assert.AreEqual(0, region.DepthAt(new Vector3(0, 0, 2)), .001f);
            Assert.IsFalse(region.Melt(.1f));
            Assert.AreEqual(1, TruckTaxiSnowTraction.GripForDepth(0, .6f), .001f);
            Assert.AreEqual(1, TruckTaxiSnowTraction.RollingForDepth(0, .6f), .001f);
        }

        [Test] public void TiresCompressLessThanPlowAndSnowfallRefillsBoth()
        {
            var region = Region();
            region.Accumulate(.6f);
            Assert.Greater(region.CompressSweep(new Vector3(-1.25f, 0, 1), new Vector3(-1.25f, 0, 5), .5f, .01f), 0);
            Assert.AreEqual(.59f, region.DepthAt(new Vector3(-1.25f, 0, 2)), .001f);
            region.ClearSweep(new Vector3(0, 0, 1), new Vector3(0, 0, 5), 4.5f, .04f);
            Assert.AreEqual(.04f, region.DepthAt(new Vector3(-1.25f, 0, 2)), .001f);
            region.Accumulate(.1f);
            Assert.AreEqual(.14f, region.DepthAt(new Vector3(-1.25f, 0, 2)), .001f);
        }

        [Test] public void SnowRateMigrationOnlyChangesLegacyDefaults()
        {
            var settings = ScriptableObject.CreateInstance<TruckTaxiEnvironmentSettings>();
            try
            {
                settings.lightSnowMetersPerSecond = .0005f;
                settings.heavySnowMetersPerSecond = .002f;
                settings.blizzardMetersPerSecond = .005f;
                settings.snowMeltMetersPerSecond = 0;
                settings.tireCompressionMetersPerMeter = 0;
                Assert.IsTrue(TruckTaxiEditorSnowSetup.MigrateSnowRates(settings));
                Assert.AreEqual(.6f / 90f, settings.lightSnowMetersPerSecond, .00001f);
                Assert.AreEqual(.6f / 35f, settings.heavySnowMetersPerSecond, .00001f);
                Assert.AreEqual(.6f / 25f, settings.blizzardMetersPerSecond, .00001f);
                Assert.IsFalse(TruckTaxiEditorSnowSetup.MigrateSnowRates(settings));
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test] public void CellBudgetAndBlizzardIdDoNotChangeGlobalCatalog()
        {
            var region = new TruckTaxiSnowRegion(.6f);
            region.AddLane(new LwsTrafficLaneDefinition
            {
                laneId = "long", laneWidthMeters = 5,
                centerline = new[] { Vector3.zero, new Vector3(0, 0, 1000) }
            }, 10);
            Assert.LessOrEqual(region.Count, 10);
            Assert.IsTrue(TruckTaxiEnvironmentCoordinator.IsSupportedWeather(TruckTaxiSnow.BlizzardId));
            Assert.IsFalse(LwsWeatherPresetCatalog.TryGetBuiltInPreset(TruckTaxiSnow.BlizzardId, out _));
            var blizzard = TruckTaxiEnvironmentCoordinator.CreateBlizzardPreset();
            Assert.AreEqual(TruckTaxiSnow.BlizzardId, blizzard.presetId);
            Assert.AreEqual(LwsPrecipitationType.Snow, blizzard.precipitationType);
            Assert.AreEqual(400, blizzard.visibilityMeters);
            Assert.IsTrue(blizzard.Validate(out _));
        }
    }
}
