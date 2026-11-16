#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class CurrentRiderProofIdentity
        {
            public string scope="READ_ONLY_EXACT_CURRENT_SCENE_ORIGINAL_DRAGON_AND_ORIGINAL_NWH_REFERENCED_ASSET_IDENTITY_AGAINST_ACTUAL_GEOMETRY_AND_RUNTIME_RECEIPTS_NOT_PRODUCTION_COURSE_OR_COLD_LOAD_ACCEPTANCE",geometryFolder,runtimeFolder,failure;
            public bool exactGeometryInputs,exactRuntimeInputs,sceneUnchanged,nwhUnchanged,productionAdoptionAllowed;
            public List<RiderNativeSourceBaseline> actualNativeSourceBaselines=new List<RiderNativeSourceBaseline>();
            public List<FlightSourceHash> geometryDependencies=new List<FlightSourceHash>(),runtimeDependencies=new List<FlightSourceHash>(),originalNwhReferencedDependencies=new List<FlightSourceHash>();
        }
        [Serializable]sealed class RiderNativeSourceBaseline
        {
            public string scope="EXACT_PRELAUNCH_HOST_INPUT_AND_ACTUAL_PASSED_NATIVE_POSTIMPORT_BASELINES_NO_ASSET_OR_META_EXEMPTION",path,prelaunchSha256,hostInputSha256,actualNativeSha256,currentNativeSha256;
            public int agreeingPassedNativeProfiles;public bool nativeImportNormalization;
        }
        static string RiderEvidenceHostRoot(string folder)
        {
            for(var directory=new DirectoryInfo(Path.GetFullPath(folder));directory!=null;directory=directory.Parent)if(Directory.Exists(Path.Combine(directory.FullName,"Assets"))&&Directory.Exists(Path.Combine(directory.FullName,"ProjectSettings")))return directory.FullName;
            throw new InvalidOperationException("Actual evidence must retain its original authoring-project ancestry for dual input/native provenance.");
        }
        static void RequireActualNativeCapturedDependency(FlightSourceHash[] prelaunch,IEnumerable<FlightSourceHash[]> actualPassedCaptures,string path,string hostRoot,List<RiderNativeSourceBaseline> output,string context)
        {
            string normalized=path.Replace('\\','/');var original=prelaunch.Where(row=>string.Equals(row.path.Replace('\\','/'),normalized,StringComparison.OrdinalIgnoreCase)).ToArray();if(original.Length!=1)throw new InvalidOperationException(context+" missing or ambiguous prelaunch input: "+path);
            var captured=actualPassedCaptures.Select(capture=>capture.Where(row=>string.Equals(row.path.Replace('\\','/'),normalized,StringComparison.OrdinalIgnoreCase)).ToArray()).ToArray();if(captured.Length<2||captured.Any(rows=>rows.Length!=1)||captured.Select(rows=>rows[0].sha256.ToUpperInvariant()).Distinct().Count()!=1)throw new InvalidOperationException(context+" requires the SAME exact source baseline in both actually passed native profiles: "+path);
            string expected=captured[0][0].sha256,actual=Hash(CurrentRiderPhysicalDependency(path));if(!string.Equals(actual,expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(context+" current bytes differ from genuinely measured native source baseline: "+path+" native="+expected+" current="+actual);
            string host=Path.Combine(hostRoot,path),hostHash=Hash(host);if(!string.Equals(hostHash,original[0].sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(context+" preserved original host input differs from actual prelaunch manifest: "+path);
            if(!output.Any(row=>string.Equals(row.path,path,StringComparison.OrdinalIgnoreCase)))output.Add(new RiderNativeSourceBaseline{path=path,prelaunchSha256=original[0].sha256,hostInputSha256=hostHash,actualNativeSha256=expected,currentNativeSha256=actual,agreeingPassedNativeProfiles=captured.Length,nativeImportNormalization=!string.Equals(expected,original[0].sha256,StringComparison.OrdinalIgnoreCase)});
        }
        static FlightSourceHash[] RequireRiderPassedManifest(string folder,string test)
        {
            var xml=new XmlDocument();xml.Load(Path.Combine(folder,"tests.xml"));var rows=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(row=>row.Attributes["fullname"].Value==test).ToArray();if(rows.Length!=1||rows[0].Attributes["result"].Value!="Passed")throw new InvalidOperationException("Exact genuinely passed native case required: "+test);
            return JsonUtility.FromJson<FlightSourceRows>("{\"rows\":"+File.ReadAllText(Path.Combine(folder,"input-manifest.json"))+"}").rows;
        }
        static string CurrentRiderPhysicalDependency(string path)
        {
            path=path.Replace('\\','/');
            // These TWO files configure the project; they are not virtual
            // package assets. Resolve them against this actual Unity project,
            // never against PackageCache or a caller's unrelated working tree.
            string configuration=string.Equals(path,"Packages/packages-lock.json",StringComparison.OrdinalIgnoreCase)?"packages-lock.json":string.Equals(path,"Packages/manifest.json",StringComparison.OrdinalIgnoreCase)?"manifest.json":null;
            if(configuration!=null){string physical=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Packages",configuration);if(!File.Exists(physical))throw new InvalidOperationException("Required actual project package configuration is missing: "+physical);return physical;}
            if(!path.StartsWith("Packages/",StringComparison.OrdinalIgnoreCase))return path;
            var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);if(package==null||string.IsNullOrEmpty(package.resolvedPath))throw new InvalidOperationException("No actual Unity package resolution for "+path);
            string prefix="Packages/"+package.name+"/";if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Package resolution identity/path mismatch: "+path);return Path.Combine(package.resolvedPath,path.Substring(prefix.Length));
        }
        public static bool VerifyActualProjectConfigurationAndStrictPackageSourceResolution()
        {
            foreach(string name in new[]{"packages-lock.json","manifest.json"}){string logical="Packages/"+name,expected=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Packages",name),actual=CurrentRiderPhysicalDependency(logical);if(!File.Exists(expected)||!string.Equals(Path.GetFullPath(actual),Path.GetFullPath(expected),StringComparison.OrdinalIgnoreCase)||Hash(actual)!=Hash(expected))throw new InvalidOperationException("Exact actual project configuration identity failed: "+logical);}
            const string source="Packages/com.unity.render-pipelines.universal/Editor/AssetVersion.cs";var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(source);if(package==null)throw new InvalidOperationException("Installed actual URP package source required for strict resolution control.");string resolved=CurrentRiderPhysicalDependency(source),expectedSource=Path.Combine(package.resolvedPath,"Editor/AssetVersion.cs");if(!File.Exists(resolved)||!string.Equals(Path.GetFullPath(resolved),Path.GetFullPath(expectedSource),StringComparison.OrdinalIgnoreCase)||Hash(resolved)!=Hash(expectedSource))throw new InvalidOperationException("Real package source must retain exact installed package resolution.");
            foreach(string invalid in new[]{"Packages/not-a-project-configuration.json","Packages/com.lws.nonexistent/NotSource.cs"}){bool rejected=false;try{CurrentRiderPhysicalDependency(invalid);}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new InvalidOperationException("Unknown package/configuration path must not receive a filesystem fallback: "+invalid);}return true;
        }
        static void RecordCurrentRiderDependencies(FlightSourceHash[] manifest,IEnumerable<string> dependencies,List<FlightSourceHash> output,string context)
        {
            void Record(string path,string physical)
            {
                if(output.Any(row=>string.Equals(row.path,path,StringComparison.OrdinalIgnoreCase)))return;
                if(!File.Exists(physical))throw new InvalidOperationException(context+" external dependency has no readable resolved source bytes: "+path+" -> "+physical);
                RequireRecordedStartupSourceHash(manifest,path,context);output.Add(new FlightSourceHash{path=path,sha256=Hash(physical)});
            }
            foreach(string path in dependencies.Where(path=>!string.IsNullOrEmpty(path)).Select(path=>path.Replace('\\','/')).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path=>path,StringComparer.Ordinal))
            {
                // Package asset paths are virtual. Their real resolved bytes
                // remain subject to the established pinned native authority;
                // physical File.Exists must not reject them before resolution.
                string physical=CurrentRiderPhysicalDependency(path);Record(path,physical);if(File.Exists(physical+".meta"))Record(path+".meta",physical+".meta");
            }
        }
        static IEnumerable<string> CurrentOriginalNwhReferences(DragonRidersMatch match)
        {
            foreach(var truck in match.trucks)
            {
                var source=PrefabUtility.GetCorrespondingObjectFromSource(truck.gameObject);if(source!=null){string path=AssetDatabase.GetAssetPath(source);if(!string.IsNullOrEmpty(path))foreach(var dependency in AssetDatabase.GetDependencies(path,true))yield return dependency;}
                foreach(var component in truck.GetComponentsInChildren<Component>(true).Where(value=>value!=null))
                {
                    var serialized=new SerializedObject(component);var property=serialized.GetIterator();bool enter=true;while(property.Next(enter))
                    {
                        enter=true;if(property.propertyType!=SerializedPropertyType.ObjectReference||property.objectReferenceValue==null)continue;string path=AssetDatabase.GetAssetPath(property.objectReferenceValue);
                        if(string.IsNullOrEmpty(path)||path=="Resources/unity_builtin_extra"||path=="Library/unity default resources")continue;
                        foreach(var dependency in AssetDatabase.GetDependencies(path,true))yield return dependency;
                    }
                }
            }
        }
        public static string InspectCurrentFireQualifiedProofIdentityWithoutSavingAssets(string geometryFolder,string runtimeFolder)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes.");var report=new CurrentRiderProofIdentity{geometryFolder=geometryFolder,runtimeFolder=runtimeFolder};string before=Hash(DragonRidersAuthoring.ScenePath),physics=null;var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonRidersMatch match=null;
            try
            {
                var geometry=RequireRiderPassedManifest(geometryFolder,"DragonRiderProvenHeadSizeTests.AnalyticallyDerivedFireNeck60HeadMinus15RetainsEvery537OriginalActionLayerAndWholeNwhGeometryGate");
                // The exact fixture fullname is read from the actual runtime
                // receipt, but must be the existing qualified60/-15case.
                var xml=new XmlDocument();xml.Load(Path.Combine(runtimeFolder,"tests.xml"));var runtimeCases=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(row=>row.Attributes["fullname"].Value.Contains("QualifiedFire")&&row.Attributes["fullname"].Value.Contains("HeadLever")).ToArray();if(runtimeCases.Length!=1)throw new InvalidOperationException("One original qualified Fire head-lever runtime fixture required.");var runtime=RequireRiderPassedManifest(runtimeFolder,runtimeCases[0].Attributes["fullname"].Value);
                var proof=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(geometryFolder,"rendered-evidence"),"precise-proven-fire-head-lever-size-derivation.json",SearchOption.AllDirectories).Single()));var row=proof.profiles.Single(value=>value.species=="dragon-fire"&&value.strictMaterialPatch&&value.provenFireHeadLeverRecipe);
                if(!proof.sceneBytesExact||!proof.nwhExact||!proof.sourceBytesExact||row.actualPoses!=537||row.invalidAnatomicalPoses!=0||row.candidateSatPairs!=0||!row.exactOriginalBoxAcceptance||!row.originalFootprintFits||row.candidateWheelContactPoses!=537||row.maximumActualWheelGap>.03f||row.actualCandidateSkinScaleError>=.001f||row.derivedNeckPitch!=60||row.derivedHeadPitch!= -15||Mathf.Abs(row.roundedCandidateFactor-1.67f)>.000001f)throw new InvalidOperationException("Exact complete qualified Fire full-native geometry required.");
                RecordCurrentRiderDependencies(geometry,new[]{DragonRidersAuthoring.ScenePath},report.geometryDependencies,"Current Fire geometry whole-scene identity");var passedProfiles=proof.profiles.Where(profile=>profile.species=="dragon-fire"&&profile.exactOriginalBoxAcceptance&&profile.originalFootprintFits&&profile.actualPoses==537&&profile.invalidAnatomicalPoses==0&&profile.candidateSatPairs==0&&profile.sourceMotionUnchanged).ToArray();if(passedProfiles.Length!=2)throw new InvalidOperationException("Both actual positive Fire profiles required for dual-baseline provenance.");foreach(string path in row.sourceFiles.Select(value=>value.path).Distinct())RequireActualNativeCapturedDependency(geometry,passedProfiles.Select(profile=>profile.sourceFiles.ToArray()),path,RiderEvidenceHostRoot(geometryFolder),report.actualNativeSourceBaselines,"Current Fire actual measured source identity");report.exactGeometryInputs=true;
                var runtimePaths=new[]{DragonRidersAuthoring.ScenePath,"Assets/TruckDragonRiders/Scripts/DragonRiderRacer.cs","Assets/TruckDragonRiders/Scripts/DragonRidersMatch.cs","Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.MountedSizeLease.cs","Assets/TruckDragonKingdom/Tests/Native/NativeDragonInputTests.cs"};RecordCurrentRiderDependencies(runtime,runtimePaths,report.runtimeDependencies,"Current Fire actual runtime/scene identity");report.exactRuntimeInputs=true;
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);RecordCurrentRiderDependencies(runtime,CurrentOriginalNwhReferences(match),report.originalNwhReferencedDependencies,"Current original full NWH referenced geometry/configuration/source identity");
                // Deliberately false: these receipts establish geometry and
                // isolated runtime/cabs, not current course/cold-load adoption.
                report.productionAdoptionAllowed=false;
            }
            catch(Exception error){report.failure=error.ToString();}
            finally
            {
                report.nwhUnchanged=match!=null&&PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sceneUnchanged=Hash(DragonRidersAuthoring.ScenePath)==before;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","CurrentProofIdentity");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"current-qualified-fire-source-world-identity.json"),JsonUtility.ToJson(report,true));}
            }
            if(!string.IsNullOrEmpty(report.failure)||!report.sceneUnchanged||!report.nwhUnchanged)throw new InvalidOperationException("Current Fire proof identity not established: "+report.failure);return JsonUtility.ToJson(report,true);
        }
        [Serializable]sealed class CurrentOriginalNwhClosure
        {
            public string scope="FRESH_READ_ONLY_COMPLETE_CURRENT_ORIGINAL_NWH_REFERENCED_DEPENDENCY_INVENTORY_BEFORE_REQUALIFICATION_NOT_HISTORICAL_NATIVE_OR_PRODUCTION_ACCEPTANCE",hostRoot,failure;
            public bool sceneUnchanged,nwhUnchanged,dependencyBytesUnchanged,historicalAcceptanceClaimed,productionAdoptionAllowed;
            public string hostPackageLockSha256,nativePackageLockSha256;public CurrentOriginalNwhDependency[] dependencies;
        }
        [Serializable]sealed class CurrentOriginalNwhDependency
        {
            public string path,nativeResolvedPath,hostSourcePath,hostSha256,nativeSha256,assetGuid,packageName,packageVersion;
            public bool hostBytesAvailable,nativeMatchesHost,explicitNativeImportRepresentationDifference;
        }
        static CurrentOriginalNwhDependency[] CaptureCurrentOriginalNwhClosure(DragonRidersMatch match,string hostRoot)
        {
            var logical=new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(string path in CurrentOriginalNwhReferences(match).Select(path=>path.Replace('\\','/')).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                logical.Add(path);string physical=CurrentRiderPhysicalDependency(path);if(File.Exists(physical+".meta"))logical.Add(path+".meta");
            }
            return logical.OrderBy(path=>path,StringComparer.Ordinal).Select(path=>
            {
                string physical=CurrentRiderPhysicalDependency(path),host=Path.Combine(hostRoot,path);if(!File.Exists(physical))throw new InvalidOperationException("Complete current NWH closure has unreadable native dependency: "+path+" -> "+physical);
                var row=new CurrentOriginalNwhDependency{path=path,nativeResolvedPath=physical,nativeSha256=Hash(physical),hostSourcePath=host,hostBytesAvailable=File.Exists(host),assetGuid=AssetDatabase.AssetPathToGUID(path.EndsWith(".meta",StringComparison.Ordinal)?path.Substring(0,path.Length-5):path)};
                if(row.hostBytesAvailable){row.hostSha256=Hash(host);row.nativeMatchesHost=string.Equals(row.hostSha256,row.nativeSha256,StringComparison.OrdinalIgnoreCase);row.explicitNativeImportRepresentationDifference=!row.nativeMatchesHost;}
                if(path.StartsWith("Packages/",StringComparison.OrdinalIgnoreCase)){var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);if(package==null)throw new InvalidOperationException("Current NWH package dependency lacks actual Unity resolution: "+path);row.packageName=package.name;row.packageVersion=package.version;}
                return row;
            }).ToArray();
        }
        public static string InspectCurrentOriginalNwhClosureBeforeFreshRequalificationWithoutSavingAssets(string hostRoot)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes.");hostRoot=Path.GetFullPath(hostRoot);if(!Directory.Exists(Path.Combine(hostRoot,"Assets"))||!Directory.Exists(Path.Combine(hostRoot,"ProjectSettings")))throw new InvalidOperationException("Explicit original authoring project required; do not substitute validation copies for host provenance.");
            var report=new CurrentOriginalNwhClosure{hostRoot=hostRoot};string before=Hash(DragonRidersAuthoring.ScenePath),physics=null;var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonRidersMatch match=null;
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);report.hostPackageLockSha256=Hash(Path.Combine(hostRoot,"Packages","packages-lock.json"));report.nativePackageLockSha256=Hash(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Packages","packages-lock.json"));report.dependencies=CaptureCurrentOriginalNwhClosure(match,hostRoot);
                if(report.dependencies.Length==0||!report.dependencies.Any(row=>row.path=="Assets/LWS/TruckTaxi/Scripts/Garage/TruckTaxiTruckModel.cs"))throw new InvalidOperationException("Complete actual NWH dependency closure must include the genuinely referenced truck model configuration source; no script or vendor exclusions.");
                var after=CaptureCurrentOriginalNwhClosure(match,hostRoot);report.dependencyBytesUnchanged=report.dependencies.Length==after.Length&&report.dependencies.Zip(after,(a,b)=>a.path==b.path&&a.nativeSha256==b.nativeSha256&&a.hostBytesAvailable==b.hostBytesAvailable&&a.hostSha256==b.hostSha256).All(value=>value);
            }
            catch(Exception error){report.failure=error.ToString();}
            finally
            {
                report.nwhUnchanged=match!=null&&PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sceneUnchanged=Hash(DragonRidersAuthoring.ScenePath)==before;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","CurrentProofIdentity");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"current-original-nwh-complete-closure-for-fresh-requalification.json"),JsonUtility.ToJson(report,true));}
            }
            if(!string.IsNullOrEmpty(report.failure)||!report.sceneUnchanged||!report.nwhUnchanged||!report.dependencyBytesUnchanged)throw new InvalidOperationException("Fresh original NWH closure inventory failed or changed source: "+report.failure);return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
