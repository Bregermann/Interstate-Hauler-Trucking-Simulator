using System;
using System.Reflection;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class LwsEasyRoadsExportOptions
    {
        public float sampleSpacingMeters = 10f;
        public bool includeLeftAndRightEdges;
        public bool includeIntersections = true;
        public bool allowTerrainMutation;
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
            MethodInfo getRoadsMethod = networkType.GetMethod("GetRoads", BindingFlags.Instance | BindingFlags.Public);
            return new LwsEasyRoadsExportReport
            {
                succeeded = getRoadsMethod != null,
                message = getRoadsMethod != null
                    ? "EasyRoads network appears exportable by reflection."
                    : $"EasyRoads network type {networkType.FullName} does not expose an expected GetRoads method."
            };
        }

        public LwsRoadGraph ExportRoadGraph(object easyRoadsNetwork, LwsEasyRoadsExportOptions options)
        {
            // Prompt 002 establishes the boundary only. Prompt 004+ can bind this
            // to concrete EasyRoads APIs after render/project setup is stable.
            if (easyRoadsNetwork == null)
            {
                return LwsRoadGraph.CreateEmpty("easyroads-null");
            }

            string graphId = $"easyroads-{easyRoadsNetwork.GetType().Name}";
            return LwsRoadGraph.CreateEmpty(graphId);
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
