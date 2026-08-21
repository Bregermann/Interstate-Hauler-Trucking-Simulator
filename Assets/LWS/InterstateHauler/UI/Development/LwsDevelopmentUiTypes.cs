using System;
using System.Collections.Generic;
using System.Linq;

namespace LWS.InterstateHauler
{
    public enum LwsDevelopmentUiTab
    {
        Overview,
        Truck,
        Transmission,
        InputWheel,
        Traffic,
        GpsNavigation,
        Weather,
        RoadConditions,
        Streaming,
        FloatingOrigin,
        FiftyMileTest,
        Performance,
        Systems
    }

    public readonly struct LwsDevelopmentUiTabDefinition
    {
        public LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab tab, string id, string label)
        {
            Tab = tab;
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public LwsDevelopmentUiTab Tab { get; }
        public string Id { get; }
        public string Label { get; }
    }

    public static class LwsDevelopmentUiCatalog
    {
        private static readonly LwsDevelopmentUiTabDefinition[] TabDefinitions =
        {
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Overview, "overview", "Overview"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Truck, "truck", "Truck"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Transmission, "transmission", "Transmission"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.InputWheel, "input-wheel", "Input / Wheel"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Traffic, "traffic", "Traffic"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.GpsNavigation, "gps-navigation", "GPS / Navigation"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Weather, "weather", "Weather"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.RoadConditions, "road-conditions", "Road Conditions"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Streaming, "streaming", "Streaming"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.FloatingOrigin, "floating-origin", "Floating Origin"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.FiftyMileTest, "fifty-mile-test", "50-Mile Test"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Performance, "performance", "Performance"),
            new LwsDevelopmentUiTabDefinition(LwsDevelopmentUiTab.Systems, "systems", "Systems")
        };

        public static IReadOnlyList<LwsDevelopmentUiTabDefinition> Tabs => TabDefinitions;

        public static bool TryGet(LwsDevelopmentUiTab tab, out LwsDevelopmentUiTabDefinition definition)
        {
            for (int i = 0; i < TabDefinitions.Length; i++)
            {
                if (TabDefinitions[i].Tab == tab)
                {
                    definition = TabDefinitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static bool ValidateTabs(out string message)
        {
            bool uniqueIds = TabDefinitions.Select(t => t.Id).Distinct(StringComparer.Ordinal).Count() == TabDefinitions.Length;
            bool uniqueTabs = TabDefinitions.Select(t => t.Tab).Distinct().Count() == TabDefinitions.Length;
            bool labelsPresent = TabDefinitions.All(t => !string.IsNullOrWhiteSpace(t.Label));
            if (!uniqueIds)
            {
                message = "Development UI tab IDs must be unique.";
                return false;
            }

            if (!uniqueTabs)
            {
                message = "Development UI tab enum entries must be unique.";
                return false;
            }

            if (!labelsPresent)
            {
                message = "Development UI tab labels must be present.";
                return false;
            }

            message = "Development UI tabs are valid.";
            return true;
        }
    }
}
