using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct Lws18SpeedRatioMapping
    {
        public Lws18SpeedGearId gearId;
        public LwsTruckShifterGate physicalGate;
        public LwsTruckRange range;
        public LwsTruckSplitter splitter;
        public int logicalRatioIndex;
        public int nwhGearIndex;
        public float gearRatio;
        public string displayLabel;
        public bool valid;
    }

    [CreateAssetMenu(
        menuName = "Interstate Hauler/Vehicles/18-Speed Transmission Definition",
        fileName = "IH_18SpeedTransmissionDefinition")]
    public sealed class Lws18SpeedTransmissionDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = "ih.transmission.eaton18.g29-development";
        [SerializeField] private string displayName = "Eaton-Style 18-Speed G29 Development";
        [SerializeField] private LwsManualShiftAssistMode defaultAssistMode = LwsManualShiftAssistMode.AssistedManual;
        [SerializeField] private LwsTruckShifterGate lowCrawlerGate = LwsTruckShifterGate.Gate2;
        [SerializeField] private LwsTruckShifterGate mainPosition1Gate = LwsTruckShifterGate.Gate3;
        [SerializeField] private LwsTruckShifterGate mainPosition2Gate = LwsTruckShifterGate.Gate4;
        [SerializeField] private LwsTruckShifterGate mainPosition3Gate = LwsTruckShifterGate.Gate5;
        [SerializeField] private LwsTruckShifterGate mainPosition4Gate = LwsTruckShifterGate.Gate6;
        [SerializeField] private LwsTruckShifterGate reverseGate = LwsTruckShifterGate.Reverse;
        [SerializeField] private LwsTruckShifterGate[] unusedForwardGates = { LwsTruckShifterGate.Gate1 };
        [SerializeField] private int reverseNwhGearIndex = -1;
        [SerializeField] private float reverseGearRatio = -12.85f;
        [SerializeField] private float nwhFinalDriveRatio = 4f;
        [SerializeField] private bool configureNwhRuntimeGears = true;
        [SerializeField, Range(0f, 1f)] private float clutchDepressedThreshold = 0.75f;
        [SerializeField] private float floatShiftRpmTolerance = 150f;
        [SerializeField] private float assistedRpmTolerance = 700f;
        [SerializeField] private float severeOverspeedMultiplier = 1.08f;
        [SerializeField] private float catastrophicOverspeedMultiplier = 1.25f;
        [SerializeField] private float reverseSpeedLimitMetersPerSecond = 0.75f;
        [SerializeField] private List<Lws18SpeedRatioMapping> forwardMappings = new List<Lws18SpeedRatioMapping>();

        public string StableId => stableId;
        public string DisplayName => displayName;
        public LwsManualShiftAssistMode DefaultAssistMode => defaultAssistMode;
        public LwsTruckShifterGate LowCrawlerGate => lowCrawlerGate;
        public LwsTruckShifterGate MainPosition1Gate => mainPosition1Gate;
        public LwsTruckShifterGate MainPosition2Gate => mainPosition2Gate;
        public LwsTruckShifterGate MainPosition3Gate => mainPosition3Gate;
        public LwsTruckShifterGate MainPosition4Gate => mainPosition4Gate;
        public LwsTruckShifterGate ReverseGate => reverseGate;
        public IReadOnlyList<LwsTruckShifterGate> UnusedForwardGates => unusedForwardGates;
        public int ReverseNwhGearIndex => reverseNwhGearIndex;
        public float ReverseGearRatio => reverseGearRatio;
        public float NwhFinalDriveRatio => nwhFinalDriveRatio;
        public bool ConfigureNwhRuntimeGears => configureNwhRuntimeGears;
        public float ClutchDepressedThreshold => clutchDepressedThreshold;
        public float FloatShiftRpmTolerance => floatShiftRpmTolerance;
        public float AssistedRpmTolerance => assistedRpmTolerance;
        public float SevereOverspeedMultiplier => severeOverspeedMultiplier;
        public float CatastrophicOverspeedMultiplier => catastrophicOverspeedMultiplier;
        public float ReverseSpeedLimitMetersPerSecond => reverseSpeedLimitMetersPerSecond;
        public IReadOnlyList<Lws18SpeedRatioMapping> ForwardMappings => forwardMappings;

        public IReadOnlyList<float> ForwardRatios => forwardMappings
            .Where(mapping => mapping.valid)
            .OrderBy(mapping => mapping.nwhGearIndex)
            .Select(mapping => mapping.gearRatio)
            .ToArray();

        public IReadOnlyList<float> ReverseRatios => new[] { reverseGearRatio };

        private void Reset()
        {
            ConfigureG29DevelopmentPreset();
        }

        public void ConfigureG29DevelopmentPreset()
        {
            stableId = "ih.transmission.eaton18.g29-development";
            displayName = "Eaton-Style 18-Speed G29 Development";
            defaultAssistMode = LwsManualShiftAssistMode.AssistedManual;
            lowCrawlerGate = LwsTruckShifterGate.Gate2;
            mainPosition1Gate = LwsTruckShifterGate.Gate3;
            mainPosition2Gate = LwsTruckShifterGate.Gate4;
            mainPosition3Gate = LwsTruckShifterGate.Gate5;
            mainPosition4Gate = LwsTruckShifterGate.Gate6;
            reverseGate = LwsTruckShifterGate.Reverse;
            unusedForwardGates = new[] { LwsTruckShifterGate.Gate1 };
            reverseNwhGearIndex = -1;
            reverseGearRatio = -12.85f;
            nwhFinalDriveRatio = 4f;
            configureNwhRuntimeGears = true;
            clutchDepressedThreshold = 0.75f;
            floatShiftRpmTolerance = 150f;
            assistedRpmTolerance = 700f;
            severeOverspeedMultiplier = 1.08f;
            catastrophicOverspeedMultiplier = 1.25f;
            reverseSpeedLimitMetersPerSecond = 0.75f;
            forwardMappings = BuildG29DevelopmentMappings();
        }

        public static Lws18SpeedTransmissionDefinition CreateTransientG29DevelopmentPreset()
        {
            Lws18SpeedTransmissionDefinition definition = CreateInstance<Lws18SpeedTransmissionDefinition>();
            definition.ConfigureG29DevelopmentPreset();
            return definition;
        }

        public bool TryResolveForward(
            LwsTruckShifterGate gate,
            LwsTruckRange range,
            LwsTruckSplitter splitter,
            out Lws18SpeedResolvedGear resolvedGear)
        {
            foreach (Lws18SpeedRatioMapping mapping in forwardMappings)
            {
                if (!mapping.valid ||
                    mapping.physicalGate != gate ||
                    mapping.range != range ||
                    mapping.splitter != splitter)
                {
                    continue;
                }

                resolvedGear = new Lws18SpeedResolvedGear
                {
                    valid = true,
                    gearId = mapping.gearId,
                    physicalGate = mapping.physicalGate,
                    range = mapping.range,
                    splitter = mapping.splitter,
                    logicalRatioIndex = mapping.logicalRatioIndex,
                    nwhGearIndex = mapping.nwhGearIndex,
                    gearRatio = mapping.gearRatio,
                    displayLabel = mapping.displayLabel
                };
                return true;
            }

            resolvedGear = Lws18SpeedResolvedGear.Invalid(
                gate,
                range,
                splitter,
                IsCrawlerGate(gate) && range == LwsTruckRange.High
                    ? LwsShiftRejectionReason.InvalidRangeGateCombination
                    : LwsShiftRejectionReason.GearUnavailable);
            return false;
        }

        public Lws18SpeedResolvedGear ResolveReverse()
        {
            return new Lws18SpeedResolvedGear
            {
                valid = true,
                gearId = Lws18SpeedGearId.Reverse1,
                physicalGate = reverseGate,
                nwhGearIndex = reverseNwhGearIndex,
                gearRatio = reverseGearRatio,
                displayLabel = "R",
                reverse = true
            };
        }

        public bool TryGetMapping(Lws18SpeedGearId gearId, out Lws18SpeedRatioMapping mapping)
        {
            foreach (Lws18SpeedRatioMapping candidate in forwardMappings)
            {
                if (candidate.gearId == gearId)
                {
                    mapping = candidate;
                    return true;
                }
            }

            mapping = default;
            return false;
        }

        public bool TryGetMappingForNwhGear(int nwhGear, out Lws18SpeedRatioMapping mapping)
        {
            foreach (Lws18SpeedRatioMapping candidate in forwardMappings)
            {
                if (candidate.nwhGearIndex == nwhGear)
                {
                    mapping = candidate;
                    return true;
                }
            }

            mapping = default;
            return false;
        }

        public bool IsCrawlerGate(LwsTruckShifterGate gate)
        {
            return gate == lowCrawlerGate;
        }

        public bool IsKnownForwardGate(LwsTruckShifterGate gate)
        {
            return gate == lowCrawlerGate ||
                   gate == mainPosition1Gate ||
                   gate == mainPosition2Gate ||
                   gate == mainPosition3Gate ||
                   gate == mainPosition4Gate;
        }

        public bool ValidateDefinition(out string message)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                message = "18-speed transmission definition requires a stable ID.";
                return false;
            }

            if (forwardMappings == null)
            {
                message = "18-speed transmission definition has no mapping list.";
                return false;
            }

            List<Lws18SpeedRatioMapping> validMappings = forwardMappings.Where(mapping => mapping.valid).ToList();
            if (validMappings.Count != 18)
            {
                message = $"18-speed transmission definition must contain exactly 18 valid forward mappings; found {validMappings.Count}.";
                return false;
            }

            if (validMappings.Select(mapping => mapping.gearId).Distinct().Count() != 18)
            {
                message = "18-speed transmission definition contains duplicate logical gear IDs.";
                return false;
            }

            if (validMappings.Select(mapping => mapping.nwhGearIndex).Distinct().Count() != 18 ||
                validMappings.Any(mapping => mapping.nwhGearIndex < 1 || mapping.nwhGearIndex > 18))
            {
                message = "18-speed transmission definition must map forward gears to unique NWH gear indexes 1 through 18.";
                return false;
            }

            if (validMappings.Select(mapping => mapping.logicalRatioIndex).Distinct().Count() != 18 ||
                validMappings.Any(mapping => mapping.logicalRatioIndex < 1 || mapping.logicalRatioIndex > 18))
            {
                message = "18-speed transmission definition must expose unique logical ratio indexes 1 through 18.";
                return false;
            }

            if (validMappings.Any(mapping => mapping.gearRatio <= 0f))
            {
                message = "18-speed transmission forward ratios must all be positive.";
                return false;
            }

            if (TryResolveForward(lowCrawlerGate, LwsTruckRange.High, LwsTruckSplitter.Low, out _))
            {
                message = "High range must not define the low/crawler physical gate as a forward gear.";
                return false;
            }

            if (clutchDepressedThreshold < 0f || clutchDepressedThreshold > 1f ||
                floatShiftRpmTolerance <= 0f ||
                assistedRpmTolerance <= 0f ||
                catastrophicOverspeedMultiplier <= severeOverspeedMultiplier)
            {
                message = "18-speed shift behavior thresholds are invalid.";
                return false;
            }

            message = $"18-speed definition {stableId} is valid with 18 unique forward ratios.";
            return true;
        }

        private List<Lws18SpeedRatioMapping> BuildG29DevelopmentMappings()
        {
            var mappings = new List<Lws18SpeedRatioMapping>(18);
            AddMapping(mappings, Lws18SpeedGearId.LowLow, lowCrawlerGate, LwsTruckRange.Low, LwsTruckSplitter.Low, 1, 1, 14.40f, "LO-L");
            AddMapping(mappings, Lws18SpeedGearId.LowHigh, lowCrawlerGate, LwsTruckRange.Low, LwsTruckSplitter.High, 2, 2, 12.29f, "LO-H");

            float[] ratios =
            {
                8.56f, 7.30f,
                6.05f, 5.16f,
                4.38f, 3.74f,
                3.20f, 2.73f,
                2.29f, 1.95f,
                1.62f, 1.38f,
                1.17f, 1.00f,
                0.86f, 0.73f
            };

            LwsTruckShifterGate[] mainGates =
            {
                mainPosition1Gate,
                mainPosition2Gate,
                mainPosition3Gate,
                mainPosition4Gate
            };

            int ratioCursor = 0;
            int index = 3;
            for (int gearNumber = 1; gearNumber <= 8; gearNumber++)
            {
                LwsTruckRange range = gearNumber <= 4 ? LwsTruckRange.Low : LwsTruckRange.High;
                LwsTruckShifterGate gate = mainGates[(gearNumber - 1) % 4];
                AddMapping(
                    mappings,
                    GearIdFor(gearNumber, LwsTruckSplitter.Low),
                    gate,
                    range,
                    LwsTruckSplitter.Low,
                    index,
                    index,
                    ratios[ratioCursor++],
                    $"{gearNumber}L");
                index++;
                AddMapping(
                    mappings,
                    GearIdFor(gearNumber, LwsTruckSplitter.High),
                    gate,
                    range,
                    LwsTruckSplitter.High,
                    index,
                    index,
                    ratios[ratioCursor++],
                    $"{gearNumber}H");
                index++;
            }

            return mappings;
        }

        private static void AddMapping(
            List<Lws18SpeedRatioMapping> mappings,
            Lws18SpeedGearId gearId,
            LwsTruckShifterGate gate,
            LwsTruckRange range,
            LwsTruckSplitter splitter,
            int logicalRatioIndex,
            int nwhGearIndex,
            float ratio,
            string label)
        {
            mappings.Add(new Lws18SpeedRatioMapping
            {
                gearId = gearId,
                physicalGate = gate,
                range = range,
                splitter = splitter,
                logicalRatioIndex = logicalRatioIndex,
                nwhGearIndex = nwhGearIndex,
                gearRatio = ratio,
                displayLabel = label,
                valid = true
            });
        }

        private static Lws18SpeedGearId GearIdFor(int gearNumber, LwsTruckSplitter splitter)
        {
            int offset = (gearNumber - 1) * 2 + (splitter == LwsTruckSplitter.Low ? 0 : 1);
            return (Lws18SpeedGearId)((int)Lws18SpeedGearId.Gear1Low + offset);
        }
    }
}
