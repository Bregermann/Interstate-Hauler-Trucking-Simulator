#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Xml;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        sealed class ProvenFireHeadLever{public string folder;public float neck,head;}
        static ProvenFireHeadLever RequireProvenActualFireHeadLever(string folder)
        {
            var xml=new XmlDocument();xml.Load(Path.Combine(folder,"tests.xml"));var cases=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(node=>node.Attributes["fullname"].Value=="DragonRiderHeadLeverPlanningTests.ActualQualifiedFireCabFramesDeriveBoundedConnectedHeadLeverWithoutMovingCameraOrAssets").ToArray();if(cases.Length!=1||cases[0].Attributes["result"].Value!="Passed")throw new InvalidOperationException("Passed actual-source analytic head-lever native receipt required.");
            var report=JsonUtility.FromJson<HeadLeverReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(folder,"rendered-evidence"),"actual-frame-analytic-head-lever.json",SearchOption.AllDirectories).Single()));
            if(!report.sourceExact||!report.finiteCandidate||report.totalPitch!=45||report.candidateExtraDegrees!=45||report.candidateNeckDegrees!=60||report.candidateHeadDegrees!= -15||report.poses.Count!=2||report.poses.Any(row=>!row.baselineCenterHead||row.baselineCenterDistanceError>.001f||row.candidateCenterHead||row.candidateOwnHeadHits>5||row.sourceVertices!=2064||row.sourceTriangles!=2698)||!string.Equals(Hash(report.input),report.inputSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Only the exact derived60/-15normal-family candidate with genuine original-source baseline and cleared actual center/grid rays is authorized.");
            var manifest=JsonUtility.FromJson<FlightSourceRows>("{\"rows\":"+File.ReadAllText(Path.Combine(folder,"input-manifest.json"))+"}").rows;
            foreach(string path in new[]{"Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.HeadLeverPlanning.cs","Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.HeadLeverPlanning.cs.meta","Assets/TruckDragonRiders/Tests/Authoring/DragonRiderHeadLeverPlanningTests.cs","Assets/TruckDragonRiders/Tests/Authoring/DragonRiderHeadLeverPlanningTests.cs.meta"}.Concat(AssetDatabase.GetDependencies(ActorPath("dragon-fire"),true)).Where(File.Exists).Distinct())
            {RequireRecordedStartupSourceHash(manifest,path,"Actual head-lever");}
            return new ProvenFireHeadLever{folder=folder,neck=60,head=-15};
        }
        static UnityEngine.Object[] CreateProvenFireHeadLeverEnvelope(DragonCharacter actor,ProvenFireHeadLever proof)=>CreateTransientCabFlightControllerCore(actor,"dragon-fire",proof.neck,proof.head,0,true,.5f,provenFireHeadLever:true);
        public static string InspectPreciseProvenFireHeadLeverMountedSizeWithoutSavingAssets(string evidenceFolder)=>InspectMountedSizeProfiles(true,true,true,null,RequireProvenActualFireHeadLever(evidenceFolder));
    }
}
#endif
