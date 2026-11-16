#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderCurrentNwhClosureTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public string failure;public bool sceneUnchanged,nwhUnchanged,dependencyBytesUnchanged,historicalAcceptanceClaimed,productionAdoptionAllowed;public Dependency[] dependencies;}
    [Serializable]sealed class Dependency{public string path,hostSha256,nativeSha256;public bool hostBytesAvailable,nativeMatchesHost,explicitNativeImportRepresentationDifference;}
#pragma warning restore CS0649
    [Test,Timeout(180000)]
    public void CompleteCurrentOriginalNwhDependencyClosureIsCapturedBeforeFreshGeometryAndRuntimeQualification()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderNwhHostRoot");Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit preserved original host root required.");var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)type.GetMethod("InspectCurrentOriginalNwhClosureBeforeFreshRequalificationWithoutSavingAssets").Invoke(null,new object[]{args[at+1]}));Assert.That(report.failure,Is.Null.Or.Empty);Assert.That(report.sceneUnchanged&&report.nwhUnchanged&&report.dependencyBytesUnchanged,Is.True);Assert.That(report.historicalAcceptanceClaimed||report.productionAdoptionAllowed,Is.False);Assert.That(report.dependencies.Length,Is.GreaterThan(5));var model=report.dependencies.Single(row=>row.path=="Assets/LWS/TruckTaxi/Scripts/Garage/TruckTaxiTruckModel.cs");Assert.That(model.hostBytesAvailable&&model.nativeMatchesHost,Is.True);Assert.That(model.nativeSha256,Is.EqualTo("2B0E191EBDBBCB2F1108958CF389FA2171542DA40F609CA68B7D404E026BC69E"));
        TestContext.WriteLine("CURRENT_ORIGINAL_NWH_FULL_CLOSURE entries="+report.dependencies.Length+" nativeHostRepresentationDifferences="+report.dependencies.Count(row=>row.explicitNativeImportRepresentationDifference)+" packageOrOtherHostVirtualPaths="+report.dependencies.Count(row=>!row.hostBytesAvailable)+" NO_HISTORICAL_OR_GEOMETRY_RUNTIME_ADOPTION_ACCEPTANCE");
    }
}
#endif
