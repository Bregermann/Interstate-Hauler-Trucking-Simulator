using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class Lws18SpeedTransmissionEditModeTests
    {
        [Test]
        public void DevelopmentDefinitionContainsEighteenUniqueForwardRatios()
        {
            Lws18SpeedTransmissionDefinition definition = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset();

            Assert.IsTrue(definition.ValidateDefinition(out string message), message);
            Assert.AreEqual(18, definition.ForwardMappings.Count(mapping => mapping.valid));
            Assert.AreEqual(18, definition.ForwardMappings.Select(mapping => mapping.logicalRatioIndex).Distinct().Count());
            Assert.AreEqual(18, definition.ForwardMappings.Select(mapping => mapping.nwhGearIndex).Distinct().Count());
            Assert.AreEqual(18, definition.ForwardRatios.Count);
        }

        [Test]
        public void LowRangeMapsCrawlerAndOneThroughFourInBothSplits()
        {
            Lws18SpeedTransmissionDefinition definition = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset();

            AssertResolved(definition, LwsTruckShifterGate.Gate2, LwsTruckRange.Low, LwsTruckSplitter.Low, Lws18SpeedGearId.LowLow, 1, "LO-L");
            AssertResolved(definition, LwsTruckShifterGate.Gate2, LwsTruckRange.Low, LwsTruckSplitter.High, Lws18SpeedGearId.LowHigh, 2, "LO-H");
            AssertResolved(definition, LwsTruckShifterGate.Gate3, LwsTruckRange.Low, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear1Low, 3, "1L");
            AssertResolved(definition, LwsTruckShifterGate.Gate3, LwsTruckRange.Low, LwsTruckSplitter.High, Lws18SpeedGearId.Gear1High, 4, "1H");
            AssertResolved(definition, LwsTruckShifterGate.Gate4, LwsTruckRange.Low, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear2Low, 5, "2L");
            AssertResolved(definition, LwsTruckShifterGate.Gate4, LwsTruckRange.Low, LwsTruckSplitter.High, Lws18SpeedGearId.Gear2High, 6, "2H");
            AssertResolved(definition, LwsTruckShifterGate.Gate5, LwsTruckRange.Low, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear3Low, 7, "3L");
            AssertResolved(definition, LwsTruckShifterGate.Gate5, LwsTruckRange.Low, LwsTruckSplitter.High, Lws18SpeedGearId.Gear3High, 8, "3H");
            AssertResolved(definition, LwsTruckShifterGate.Gate6, LwsTruckRange.Low, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear4Low, 9, "4L");
            AssertResolved(definition, LwsTruckShifterGate.Gate6, LwsTruckRange.Low, LwsTruckSplitter.High, Lws18SpeedGearId.Gear4High, 10, "4H");
        }

        [Test]
        public void HighRangeMapsFiveThroughEightInBothSplits()
        {
            Lws18SpeedTransmissionDefinition definition = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset();

            AssertResolved(definition, LwsTruckShifterGate.Gate3, LwsTruckRange.High, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear5Low, 11, "5L");
            AssertResolved(definition, LwsTruckShifterGate.Gate3, LwsTruckRange.High, LwsTruckSplitter.High, Lws18SpeedGearId.Gear5High, 12, "5H");
            AssertResolved(definition, LwsTruckShifterGate.Gate4, LwsTruckRange.High, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear6Low, 13, "6L");
            AssertResolved(definition, LwsTruckShifterGate.Gate4, LwsTruckRange.High, LwsTruckSplitter.High, Lws18SpeedGearId.Gear6High, 14, "6H");
            AssertResolved(definition, LwsTruckShifterGate.Gate5, LwsTruckRange.High, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear7Low, 15, "7L");
            AssertResolved(definition, LwsTruckShifterGate.Gate5, LwsTruckRange.High, LwsTruckSplitter.High, Lws18SpeedGearId.Gear7High, 16, "7H");
            AssertResolved(definition, LwsTruckShifterGate.Gate6, LwsTruckRange.High, LwsTruckSplitter.Low, Lws18SpeedGearId.Gear8Low, 17, "8L");
            AssertResolved(definition, LwsTruckShifterGate.Gate6, LwsTruckRange.High, LwsTruckSplitter.High, Lws18SpeedGearId.Gear8High, 18, "8H");
        }

        [Test]
        public void HighRangeCrawlerGateIsRejected()
        {
            Lws18SpeedTransmissionDefinition definition = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset();

            bool resolved = definition.TryResolveForward(
                LwsTruckShifterGate.Gate2,
                LwsTruckRange.High,
                LwsTruckSplitter.Low,
                out Lws18SpeedResolvedGear invalid);

            Assert.IsFalse(resolved);
            Assert.AreEqual(LwsShiftRejectionReason.InvalidRangeGateCombination, invalid.invalidReason);
        }

        [Test]
        public void NeutralAndReverseRemainSemanticInputs()
        {
            LwsTruckGearIntent neutralIntent = LwsWheelCalibrationUtility.ToValidationGearIntent(LwsHPatternShifterState.Neutral(), false);
            LwsTruckGearIntent reverseIntent = LwsWheelCalibrationUtility.ToValidationGearIntent(
                new LwsHPatternShifterState
                {
                    activeGate = LwsTruckShifterGate.Reverse,
                    reverse = true
                },
                false);
            Lws18SpeedResolvedGear reverse = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset().ResolveReverse();

            Assert.IsTrue(neutralIntent.neutralRequested);
            Assert.IsTrue(reverseIntent.reverseRequested);
            Assert.AreEqual(Lws18SpeedGearId.Reverse1, reverse.gearId);
            Assert.AreEqual(-1, reverse.nwhGearIndex);
        }

        [Test]
        public void DisabledPrompt005ValidationMappingPreservesPhysicalGate()
        {
            LwsTruckGearIntent intent = LwsWheelCalibrationUtility.ToValidationGearIntent(
                new LwsHPatternShifterState
                {
                    activeGate = LwsTruckShifterGate.Gate6,
                    range = LwsTruckRange.High,
                    splitter = LwsTruckSplitter.High
                },
                false);

            Assert.AreEqual(LwsTruckShifterGate.Gate6, intent.physicalGate);
            Assert.AreEqual(LwsTruckRange.High, intent.range);
            Assert.AreEqual(LwsTruckSplitter.High, intent.splitter);
            Assert.AreEqual(0, intent.requestedLogicalGear);
            Assert.IsFalse(intent.neutralRequested);
            Assert.IsFalse(intent.reverseRequested);
        }

        [Test]
        public void ClutchThresholdUsesLwsPressedConvention()
        {
            Lws18SpeedTransmissionDefinition definition = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset();

            Assert.IsFalse(Lws18SpeedShiftEvaluation.IsClutchDepressed(0f, definition.ClutchDepressedThreshold));
            Assert.IsFalse(Lws18SpeedShiftEvaluation.IsClutchDepressed(0.5f, definition.ClutchDepressedThreshold));
            Assert.IsTrue(Lws18SpeedShiftEvaluation.IsClutchDepressed(1f, definition.ClutchDepressedThreshold));
        }

        [Test]
        public void FloatShiftSynchronizationUsesPredictedRpm()
        {
            float predicted = Lws18SpeedShiftEvaluation.PredictRpmFromRatioChange(
                currentEngineRpm: 1500f,
                currentTotalRatio: 8f,
                targetTotalRatio: 4f,
                currentNwhGear: 6);

            Assert.AreEqual(750f, predicted, 0.001f);
            Assert.IsTrue(Lws18SpeedShiftEvaluation.IsRpmSynchronized(800f, predicted, 75f));
            Assert.IsFalse(Lws18SpeedShiftEvaluation.IsRpmSynchronized(1000f, predicted, 75f));
        }

        [Test]
        public void OverspeedClassificationSeparatesSevereAndCatastrophic()
        {
            Assert.AreEqual(
                LwsTransmissionAbuseSeverity.Severe,
                Lws18SpeedShiftEvaluation.ClassifyOverspeed(2900f, 2600f, 1.08f, 1.25f));
            Assert.AreEqual(
                LwsTransmissionAbuseSeverity.CatastrophicRisk,
                Lws18SpeedShiftEvaluation.ClassifyOverspeed(3300f, 2600f, 1.08f, 1.25f));
        }

        [Test]
        public void AutomaticSelectorSafeSwitchingRejectsDriveReverseChangesAtSpeed()
        {
            Assert.IsFalse(Lws18SpeedTransmissionController.CanChangeAutomaticSelectorAtSpeed(
                LwsAutomaticTransmissionSelector.Drive,
                LwsAutomaticTransmissionSelector.Reverse,
                2.0f,
                0.35f,
                out string reverseMessage));
            StringAssert.Contains("STOP VEHICLE BEFORE SELECTING REVERSE", reverseMessage);

            Assert.IsFalse(Lws18SpeedTransmissionController.CanChangeAutomaticSelectorAtSpeed(
                LwsAutomaticTransmissionSelector.Reverse,
                LwsAutomaticTransmissionSelector.Drive,
                -2.0f,
                0.35f,
                out string driveMessage));
            StringAssert.Contains("STOP VEHICLE BEFORE SELECTING DRIVE", driveMessage);

            Assert.IsTrue(Lws18SpeedTransmissionController.CanChangeAutomaticSelectorAtSpeed(
                LwsAutomaticTransmissionSelector.Drive,
                LwsAutomaticTransmissionSelector.Reverse,
                0.1f,
                0.35f,
                out _));
            Assert.IsTrue(Lws18SpeedTransmissionController.CanChangeAutomaticSelectorAtSpeed(
                LwsAutomaticTransmissionSelector.Reverse,
                LwsAutomaticTransmissionSelector.Neutral,
                -4.0f,
                0.35f,
                out _));
        }

        [Test]
        public void AutomaticSelectorLabelsExposeTruckStyleDriveNeutralReverse()
        {
            Assert.AreEqual("DRIVE", Lws18SpeedTransmissionController.GetAutomaticSelectorLabel(LwsAutomaticTransmissionSelector.Drive));
            Assert.AreEqual("NEUTRAL", Lws18SpeedTransmissionController.GetAutomaticSelectorLabel(LwsAutomaticTransmissionSelector.Neutral));
            Assert.AreEqual("REVERSE", Lws18SpeedTransmissionController.GetAutomaticSelectorLabel(LwsAutomaticTransmissionSelector.Reverse));
        }

        [Test]
        public void SavePayloadSerializesLogicalTransmissionStateOnly()
        {
            var payload = new Lws18SpeedTransmissionSavePayload
            {
                schemaVersion = 1,
                mode = LwsTransmissionMode.Automatic,
                logicalGear = Lws18SpeedGearId.Gear8High,
                logicalRatioIndex = 18,
                nwhGear = 18,
                physicalGate = LwsTruckShifterGate.Gate6,
                requestedRange = LwsTruckRange.High,
                engagedRange = LwsTruckRange.High,
                requestedSplitter = LwsTruckSplitter.High,
                engagedSplitter = LwsTruckSplitter.High,
                automaticSelector = LwsAutomaticTransmissionSelector.Reverse,
                shiftState = LwsTransmissionShiftState.Engaged
            };

            string json = JsonUtility.ToJson(payload);
            Lws18SpeedTransmissionSavePayload copy = JsonUtility.FromJson<Lws18SpeedTransmissionSavePayload>(json);

            Assert.AreEqual(LwsTransmissionMode.Automatic, copy.mode);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, copy.automaticSelector);
            Assert.AreEqual(Lws18SpeedGearId.Gear8High, copy.logicalGear);
            Assert.AreEqual(18, copy.logicalRatioIndex);
            Assert.IsFalse(json.IndexOf("DirectInput", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.IsFalse(json.IndexOf("DIManager", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.IsFalse(json.IndexOf("Logitech", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void AssertResolved(
            Lws18SpeedTransmissionDefinition definition,
            LwsTruckShifterGate gate,
            LwsTruckRange range,
            LwsTruckSplitter splitter,
            Lws18SpeedGearId expectedGear,
            int expectedNwhGear,
            string expectedLabel)
        {
            bool resolved = definition.TryResolveForward(gate, range, splitter, out Lws18SpeedResolvedGear gear);

            Assert.IsTrue(resolved, $"{gate}/{range}/{splitter} did not resolve.");
            Assert.AreEqual(expectedGear, gear.gearId);
            Assert.AreEqual(expectedNwhGear, gear.nwhGearIndex);
            Assert.AreEqual(expectedLabel, gear.displayLabel);
        }
    }
}
