using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsEasyRoadsExportOptions
    {
        public string graphId = "easyroads-export";
        public string defaultRoadIdPrefix = "IH_ER";
        public float sampleSpacingMeters = 25f;
        public bool includeLeftAndRightEdges;
        public bool includeIntersections = true;
        public bool allowTerrainMutation;
        public LwsRoadClass defaultRoadClass = LwsRoadClass.Interstate;
        public LwsRoadDirection defaultDirection = LwsRoadDirection.Bidirectional;
        public LwsRoadSurfaceType defaultSurfaceType = LwsRoadSurfaceType.AsphaltInterstate;
        public float defaultSpeedLimitMph = 65f;
        public int defaultLaneCount = 2;
        public float defaultLaneWidthMeters = 3.7f;
        public float defaultRoadWidthMeters = 10.4f;
    }

    [Serializable]
    public sealed class LwsEasyRoadsExportReport
    {
        public bool succeeded;
        public string message;
        public string sourceRoadName;
        public int sampledPointCount;
    }

    public interface ILwsEasyRoadsRoadGraphExporter
    {
        LwsEasyRoadsExportReport CanExport(object easyRoadsNetwork);
        LwsRoadGraph ExportRoadGraph(object easyRoadsNetwork, LwsEasyRoadsExportOptions options);
    }

    public sealed class LwsEasyRoadsExportBoundary : ILwsEasyRoadsRoadGraphExporter
    {
        public LwsEasyRoadsExportReport CanExport(object easyRoadsNetwork)
        {
            if (easyRoadsNetwork == null)
            {
                return new LwsEasyRoadsExportReport
                {
                    succeeded = false,
                    message = "EasyRoads network reference is null."
                };
            }

            Type networkType = easyRoadsNetwork.GetType();
            MethodInfo getRoadsMethod = FindNoArgMethod(networkType, "GetRoads") ?? FindNoArgMethod(networkType, "GetRoadObjects");
            return new LwsEasyRoadsExportReport
            {
                succeeded = getRoadsMethod != null,
                message = getRoadsMethod != null
                    ? $"EasyRoads network {networkType.FullName} exposes {getRoadsMethod.Name} for reflected export."
                    : $"EasyRoads network type {networkType.FullName} does not expose GetRoads or GetRoadObjects."
            };
        }

        public LwsRoadGraph ExportRoadGraph(object easyRoadsNetwork, LwsEasyRoadsExportOptions options)
        {
            if (easyRoadsNetwork == null)
            {
                return LwsRoadGraph.CreateEmpty("easyroads-null");
            }

            options ??= new LwsEasyRoadsExportOptions();
            var graph = new LwsRoadGraph
            {
                graphId = string.IsNullOrWhiteSpace(options.graphId)
                    ? $"easyroads-{easyRoadsNetwork.GetType().Name}"
                    : options.graphId
            };

            object[] roadObjects = ReadRoadObjects(easyRoadsNetwork);
            for (int i = 0; i < roadObjects.Length; i++)
            {
                object roadObject = roadObjects[i];
                if (roadObject == null)
                {
                    continue;
                }

                List<Vector3> centerline = ReadCenterline(roadObject);
                if (centerline.Count < 2)
                {
                    continue;
                }

                string sourceName = ReadRoadName(roadObject, i);
                string stableName = SanitizeForId(sourceName);
                string roadId = $"{options.defaultRoadIdPrefix}_{stableName}";
                string segmentId = $"{roadId}_SEGMENT";
                string edgeId = $"{segmentId}_EDGE";
                string startNodeId = $"{segmentId}_START";
                string endNodeId = $"{segmentId}_END";

                graph.nodes.Add(new LwsRoadNode { nodeId = startNodeId, position = centerline[0], regionId = "easyroads-export", sceneChunkId = "local" });
                graph.nodes.Add(new LwsRoadNode { nodeId = endNodeId, position = centerline[centerline.Count - 1], regionId = "easyroads-export", sceneChunkId = "local" });

                var edge = new LwsRoadEdge
                {
                    roadId = roadId,
                    segmentId = segmentId,
                    edgeId = edgeId,
                    fromNodeId = startNodeId,
                    toNodeId = endNodeId,
                    roadClass = options.defaultRoadClass,
                    direction = options.defaultDirection,
                    surfaceType = options.defaultSurfaceType,
                    oneWay = options.defaultDirection != LwsRoadDirection.Bidirectional,
                    distanceMeters = ComputePolylineLength(centerline),
                    travelCost = ComputePolylineLength(centerline),
                    speedLimitMph = options.defaultSpeedLimitMph,
                    laneCount = Mathf.Max(1, options.defaultLaneCount),
                    laneWidthMeters = Mathf.Max(0.1f, options.defaultLaneWidthMeters),
                    rightShoulderWidthMeters = Mathf.Max(0f, (options.defaultRoadWidthMeters - options.defaultLaneWidthMeters * options.defaultLaneCount) * 0.5f),
                    leftShoulderWidthMeters = Mathf.Max(0f, (options.defaultRoadWidthMeters - options.defaultLaneWidthMeters * options.defaultLaneCount) * 0.5f),
                    medianWidthMeters = 0f,
                    stateOrRegionId = "easyroads-export",
                    sceneChunkId = "local"
                };

                AddLaneOffsets(edge);
                PopulateSamples(edge, centerline, Mathf.Max(options.defaultRoadWidthMeters, edge.laneCount * edge.laneWidthMeters), Mathf.Max(5f, options.sampleSpacingMeters));
                graph.edges.Add(edge);
            }

            return graph;
        }

        private static object[] ReadRoadObjects(object easyRoadsNetwork)
        {
            MethodInfo getRoads = FindNoArgMethod(easyRoadsNetwork.GetType(), "GetRoads") ??
                                  FindNoArgMethod(easyRoadsNetwork.GetType(), "GetRoadObjects");
            if (getRoads == null)
            {
                return Array.Empty<object>();
            }

            object value = getRoads.Invoke(easyRoadsNetwork, Array.Empty<object>());
            if (value == null)
            {
                return Array.Empty<object>();
            }

            if (value is object[] objectArray)
            {
                return objectArray;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var objects = new List<object>();
                foreach (object item in enumerable)
                {
                    objects.Add(item);
                }

                return objects.ToArray();
            }

            return new[] { value };
        }

        private static List<Vector3> ReadCenterline(object roadObject)
        {
            string[] methodNames =
            {
                "GetMarkerPositions",
                "GetSplinePointsCenter",
                "GetSplinePoints",
                "GetCenterPoints"
            };

            for (int i = 0; i < methodNames.Length; i++)
            {
                MethodInfo method = FindNoArgMethod(roadObject.GetType(), methodNames[i]);
                if (method == null)
                {
                    continue;
                }

                List<Vector3> points = ConvertToVector3List(method.Invoke(roadObject, Array.Empty<object>()));
                if (points.Count >= 2)
                {
                    return points;
                }
            }

            return new List<Vector3>();
        }

        private static List<Vector3> ConvertToVector3List(object value)
        {
            var points = new List<Vector3>();
            if (value == null)
            {
                return points;
            }

            if (value is Vector3[] vectorArray)
            {
                points.AddRange(vectorArray);
                return points;
            }

            if (value is IList<Vector3> vectorList)
            {
                points.AddRange(vectorList);
                return points;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (object item in enumerable)
                {
                    if (item is Vector3 vector)
                    {
                        points.Add(vector);
                    }
                }
            }

            return points;
        }

        private static string ReadRoadName(object roadObject, int fallbackIndex)
        {
            string[] members = { "roadName", "name", "Name" };
            for (int i = 0; i < members.Length; i++)
            {
                object value = ReadMember(roadObject, members[i]);
                if (value is string text && !string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return $"Road_{fallbackIndex:00}";
        }

        private static object ReadMember(object target, string memberName)
        {
            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            return property != null && property.CanRead ? property.GetValue(target) : null;
        }

        private static MethodInfo FindNoArgMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
        }

        private static void AddLaneOffsets(LwsRoadEdge edge)
        {
            edge.laneCenterOffsetsMeters.Clear();
            for (int i = 0; i < edge.laneCount; i++)
            {
                float laneOffset = ((edge.laneCount - 1) * -0.5f + i) * edge.laneWidthMeters;
                edge.laneCenterOffsetsMeters.Add(laneOffset);
            }
        }

        private static void PopulateSamples(LwsRoadEdge edge, List<Vector3> centerline, float roadWidthMeters, float sampleSpacingMeters)
        {
            float distance = 0f;
            for (int i = 0; i < centerline.Count - 1; i++)
            {
                Vector3 start = centerline[i];
                Vector3 end = centerline[i + 1];
                Vector3 segment = end - start;
                float length = segment.magnitude;
                if (length <= 0.01f)
                {
                    continue;
                }

                int steps = Mathf.Max(1, Mathf.CeilToInt(length / sampleSpacingMeters));
                for (int step = 0; step < steps; step++)
                {
                    float t = step / (float)steps;
                    edge.samples.Add(CreateSample(edge, Vector3.Lerp(start, end, t), segment.normalized, distance + length * t, roadWidthMeters));
                }

                distance += length;
            }

            Vector3 finalForward = (centerline[centerline.Count - 1] - centerline[centerline.Count - 2]).normalized;
            edge.samples.Add(CreateSample(edge, centerline[centerline.Count - 1], finalForward, distance, roadWidthMeters));
        }

        private static LwsRoadSample CreateSample(LwsRoadEdge edge, Vector3 position, Vector3 forward, float distance, float roadWidthMeters)
        {
            return new LwsRoadSample
            {
                roadId = edge.roadId,
                segmentId = edge.segmentId,
                distanceFromStartMeters = distance,
                position = position,
                forward = forward,
                up = Vector3.up,
                direction = edge.direction,
                roadWidthMeters = roadWidthMeters,
                laneWidthMeters = edge.laneWidthMeters,
                laneCount = edge.laneCount,
                speedLimitMph = edge.speedLimitMph
            };
        }

        private static float ComputePolylineLength(IReadOnlyList<Vector3> centerline)
        {
            float total = 0f;
            for (int i = 0; i < centerline.Count - 1; i++)
            {
                total += Vector3.Distance(centerline[i], centerline[i + 1]);
            }

            return total;
        }

        private static string SanitizeForId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "ROAD";
            }

            char[] chars = value.ToUpperInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars).Trim('_');
        }
    }

    [DisallowMultipleComponent]
    public sealed class LwsEasyRoadsExportAdapter : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Object easyRoadsNetworkObject;
        [SerializeField] private LwsEasyRoadsExportOptions exportOptions = new LwsEasyRoadsExportOptions();

        private readonly LwsEasyRoadsExportBoundary _boundary = new LwsEasyRoadsExportBoundary();

        public LwsEasyRoadsExportReport ValidateBoundary()
        {
            return _boundary.CanExport(easyRoadsNetworkObject);
        }

        public LwsRoadGraph ExportShell()
        {
            return _boundary.ExportRoadGraph(easyRoadsNetworkObject, exportOptions);
        }
    }
}
