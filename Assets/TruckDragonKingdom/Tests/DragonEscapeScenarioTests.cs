#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

// Runtime modes remain in Assembly-CSharp. Follow the existing Sports reflected
// test pattern rather than making the test assembly a runtime dependency.
public sealed class DragonEscapeScenarioTests
{
    const string Prefix = "LWS.TruckDragonKingdom.";
    static Type Runtime(string name) => Type.GetType(Prefix + name + ", Assembly-CSharp", true);
    static object Get(object value, string property) => value.GetType().GetProperty(property).GetValue(value);
    static object Make(string type, params object[] fields)
    {
        object value = Activator.CreateInstance(Runtime(type));
        for (int i = 0; i < fields.Length; i += 2) value.GetType().GetField((string)fields[i]).SetValue(value, fields[i + 1]);
        return value;
    }
    static Array Typed(string type, params object[] values)
    {
        var array = Array.CreateInstance(Runtime(type), values.Length);
        for (int i = 0; i < values.Length; i++) array.SetValue(values[i], i);
        return array;
    }
    static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    static string[] Strings(object value) => Items(value).Cast<string>().ToArray();
    static Array Starts(params string[] participants) => Typed("DragonEscapeStartDefinition", Make("DragonEscapeStartDefinition",
        "id", "village-start", "regionId", "village", "nodeId", "S", "participantNodeIds", participants));
    static Array Extractions()
    {
        return Typed("DragonExtractionDefinition", Extraction("fortress", "F", 0), Extraction("mountain", "M", 1), Extraction("boat", "H", 2));
    }
    static object Extraction(string id, string node, int kind) => Make("DragonExtractionDefinition",
        "id", id, "nodeId", node, "kind", Enum.ToObject(Runtime("DragonExtractionKind"), kind),
        "position", Position(node), "parkingBounds", new Bounds(Position(node), new Vector3(30, 10, 20)));
    static Array Plans(params string[] closures) => Typed("DragonEscapeClosurePlan", Make("DragonEscapeClosurePlan", "id", "planned", "edgeIds", closures));
    static bool Create(LwsRoadGraph graph, int seed, Array starts, Array extractions, Array plans, object previous, out object result, out string reason)
    {
        var args = new object[] { graph, seed, starts, extractions, plans, previous, null, null };
        bool success = (bool)Runtime("DragonEscapeScenario").GetMethod("TryCreate").Invoke(null, args);
        result = args[6]; reason = (string)args[7]; return success;
    }
    static object CreateValid(LwsRoadGraph graph, int seed = 0, object previous = null, Array extractions = null, Array plans = null)
    {
        Assert.That(Create(graph, seed, Starts("S", "S2"), extractions ?? Extractions(), plans ?? Plans(), previous, out var result, out var reason), Is.True, reason);
        return result;
    }
    static bool Replan(object scenario, string[] nodes, string[] closures, out object routes, out string reason)
    {
        var args = new object[] { scenario, nodes, closures, null, null };
        bool success = (bool)Runtime("DragonEscapeScenario").GetMethod("TryReplan").Invoke(null, args);
        routes = args[3]; reason = (string)args[4]; return success;
    }
    static Vector3 Position(string id)
    {
        switch (id)
        {
            case "S": return Vector3.zero;
            case "S2": return new Vector3(-100, 0, 0);
            case "A": return new Vector3(100, 0, 100);
            case "B": return new Vector3(100, 0, -100);
            case "F": return new Vector3(300, 0, 0);
            case "M": return new Vector3(450, 0, 150);
            case "H": return new Vector3(450, 0, -150);
            default: throw new ArgumentException(id);
        }
    }
    static LwsRoadGraph Graph()
    {
        var graph = LwsRoadGraph.CreateEmpty("dragon-test-authored-road-graph");
        foreach (string id in new[] { "S", "S2", "A", "B", "F", "M", "H" })
            graph.nodes.Add(new LwsRoadNode { nodeId = id, position = Position(id), regionId = "village" });
        Add(graph, "s2-s", "S2", "S"); Add(graph, "s-a", "S", "A"); Add(graph, "a-f", "A", "F");
        Add(graph, "s-b", "S", "B"); Add(graph, "b-f", "B", "F"); Add(graph, "f-m", "F", "M"); Add(graph, "f-h", "F", "H");
        return graph;
    }
    static void Add(LwsRoadGraph graph, string id, string from, string to)
    {
        var origin = Position(from); var destination = Position(to); float distance = Vector3.Distance(origin, destination);
        graph.edges.Add(new LwsRoadEdge { edgeId = id, roadId = "road-" + id, segmentId = "segment-" + id,
            fromNodeId = from, toNodeId = to, distanceMeters = distance, travelCost = distance,
            samples = new List<LwsRoadSample> {
                new LwsRoadSample { position = origin, forward = (destination - origin).normalized },
                new LwsRoadSample { position = destination, forward = (destination - origin).normalized, distanceFromStartMeters = distance }
            }});
    }

    [Test]
    public void DeterministicSeedsSelectRealRoutesAndAllThreeExtractionTypes()
    {
        var graph = Graph(); string original = graph.ToJson(); var kinds = new HashSet<int>(); var configurations = new HashSet<string>();
        for (int seed = 0; seed < 96; seed++)
        {
            object a = CreateValid(graph, seed), b = CreateValid(graph, seed);
            Assert.That(Get(a, "ConfigurationId"), Is.EqualTo(Get(b, "ConfigurationId")));
            var extraction = Get(a, "Extraction"); kinds.Add(Convert.ToInt32(Get(extraction, "Kind")));
            configurations.Add((string)Get(a, "ConfigurationId"));
            var routes = Items(Get(a, "Routes")); Assert.That(routes.Length, Is.EqualTo(2));
            foreach (var route in routes)
            {
                Assert.That(Get(route, "DestinationId"), Is.EqualTo(Get(extraction, "Id")));
                Assert.That(Get(route, "DestinationNodeId"), Is.EqualTo(Get(extraction, "NodeId")));
                Assert.That(Strings(Get(route, "EdgeIds")), Is.Not.Empty);
                Assert.That(Items(Get(route, "Waypoints")).Length, Is.GreaterThanOrEqualTo(2));
            }
        }
        CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, kinds); Assert.That(configurations.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(graph.ToJson(), Is.EqualTo(original), "Scenario generation cannot alter authored roads or Taxi graph metadata.");
    }

    [Test]
    public void ConsecutiveScenariosAvoidPriorDestinationAndAlternativeRouteConfiguration()
    {
        var graph = Graph(); object previous = null;
        for (int match = 0; match < 16; match++)
        {
            object next = CreateValid(graph, 42, previous);
            if (previous != null) Assert.That(Get(Get(next, "Extraction"), "Id"), Is.Not.EqualTo(Get(Get(previous, "Extraction"), "Id")));
            previous = next;
        }
        var onlyFortress = Typed("DragonExtractionDefinition", Extraction("fortress", "F", 0));
        var plans = Typed("DragonEscapeClosurePlan",
            Make("DragonEscapeClosurePlan", "id", "north-closed", "edgeIds", new[] { "s-a" }),
            Make("DragonEscapeClosurePlan", "id", "south-closed", "edgeIds", new[] { "s-b" }));
        object first = CreateValid(graph, 7, null, onlyFortress, plans);
        object second = CreateValid(graph, 7, first, onlyFortress, plans);
        Assert.That(Get(second, "ConfigurationId"), Is.Not.EqualTo(Get(first, "ConfigurationId")));
        Assert.That(Strings(Get(Items(Get(first, "Routes"))[0], "EdgeIds")), Is.Not.EqualTo(Strings(Get(Items(Get(second, "Routes"))[0], "EdgeIds"))));
    }

    [Test]
    public void RoadClosureReplansAllActualParticipantsWithoutChangingDestinationOrSharedGraph()
    {
        var graph = Graph(); string original = graph.ToJson(); object scenario = CreateValid(graph, extractions: Typed("DragonExtractionDefinition", Extraction("fortress", "F", 0)));
        string[] initial = Strings(Get(Items(Get(scenario, "Routes"))[0], "EdgeIds"));
        Assert.That(initial, Does.Contain("s-a"));
        Assert.That(Replan(scenario, new[] { "S", "S2" }, new[] { "s-a" }, out var routes, out var reason), Is.True, reason);
        foreach (var route in Items(routes))
        {
            var edges = Strings(Get(route, "EdgeIds")); Assert.That(edges, Does.Not.Contain("s-a")); Assert.That(edges, Does.Contain("s-b"));
            Assert.That(Get(route, "DestinationId"), Is.EqualTo("fortress"));
        }
        Assert.That(Replan(scenario, new[] { "S", "S2" }, new[] { "s-a", "s-b" }, out var failed, out _), Is.False);
        Assert.That(failed, Is.Null); Assert.That(Get(Get(scenario, "Extraction"), "Id"), Is.EqualTo("fortress"));
        Assert.That(graph.ToJson(), Is.EqualTo(original));
        CollectionAssert.AreEqual(initial, Strings(Get(Items(Get(scenario, "Routes"))[0], "EdgeIds")), "A rejected candidate cannot erase the active route.");
    }

    [Test]
    public void PlannedDestructionMustPreserveRoutesAfterAllEventsAndForEveryStart()
    {
        var graph = Graph(); var extraction = Typed("DragonExtractionDefinition", Extraction("fortress", "F", 0));
        Assert.That(Create(graph, 3, Starts("S", "S2"), extraction, Plans("s-a", "s-b"), null, out var missing, out var reason), Is.False);
        Assert.That(missing, Is.Null); Assert.That(reason, Does.Contain("every participant"));
        Assert.That(Create(graph, 3, Starts("S", "S2"), extraction, Plans("s-a", "s2-s"), null, out _, out _), Is.False,
            "A safe route for the first player cannot hide a stranded second player.");
        Assert.That(Create(graph, 3, Starts("S", "S2"), extraction, Plans("s-a"), null, out var safe, out reason), Is.True, reason);
        foreach (var route in Items(Get(safe, "Routes"))) Assert.That(Strings(Get(route, "EdgeIds")), Does.Not.Contain("s-a"));
    }

    [Test]
    public void SnapshotsDetachAllAuthoringAndMutablePresentationData()
    {
        var graph = Graph(); var definitions = Extractions(); object scenario = CreateValid(graph, extractions: definitions);
        string id = (string)Get(Get(scenario, "Extraction"), "Id");
        foreach (var value in definitions) value.GetType().GetField("id").SetValue(value, "mutated-source");
        graph.edges.Clear();
        Assert.That(Get(Get(scenario, "Extraction"), "Id"), Is.EqualTo(id));
        Assert.That(Replan(scenario, new[] { "S" }, Array.Empty<string>(), out var routes, out var reason), Is.True, reason);
        object route = Items(routes)[0]; var presentation = (LwsRouteResult)route.GetType().GetMethod("CreatePresentationRoute").Invoke(route, null);
        presentation.destinationId = "wrong"; presentation.edgeIds.Clear(); presentation.waypoints.Clear();
        Assert.That(Get(route, "DestinationId"), Is.EqualTo(id)); Assert.That(Strings(Get(route, "EdgeIds")), Is.Not.Empty);
        Assert.Throws<NotSupportedException>(() => ((IList)Get(route, "EdgeIds"))[0] = "changed");
    }

    [Test]
    public void InvalidGeometryUnknownClosureAndTruckForbiddenRoutesAreRejected()
    {
        var graph = Graph(); graph.edges[0].samples[0].position = new Vector3(float.NaN, 0, 0);
        Assert.That(Create(graph, 1, Starts(), Extractions(), Plans(), null, out _, out _), Is.False);
        graph = Graph(); graph.edges[1].samples.Clear();
        Assert.That(Create(graph, 1, Starts(), Extractions(), Plans(), null, out _, out _), Is.False);
        graph = Graph(); Assert.That(Create(graph, 1, Starts(), Extractions(), Plans("missing"), null, out _, out _), Is.False);
        graph = Graph(); graph.edges.Find(edge => edge.edgeId == "s-a").restrictions.Add(new LwsRoadRestriction { truckAllowed = false });
        graph.edges.Find(edge => edge.edgeId == "s-b").restrictions.Add(new LwsRoadRestriction { truckAllowed = false });
        Assert.That(Create(graph, 1, Starts(), Extractions(), Plans(), null, out _, out _), Is.False);
        graph = Graph(); var bad = Extraction("boat", "H", 2); bad.GetType().GetField("minimumBoatDepartureDistance").SetValue(bad, 0f);
        Assert.That(Create(graph, 1, Starts(), Typed("DragonExtractionDefinition", bad), Plans(), null, out _, out _), Is.False);
    }
}
#endif
