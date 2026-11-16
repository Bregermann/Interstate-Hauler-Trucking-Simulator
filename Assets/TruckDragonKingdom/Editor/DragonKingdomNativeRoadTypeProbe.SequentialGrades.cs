#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomNativeRoadTypeProbe
    {
        [Serializable] public sealed class SequentialGradeStage
        {
            public string selectedSource, selectedContourSha256;
            public int originalFirstMarker, originalLastMarker;
            public bool[] actualAuxiliaryMarkerDeformation; public int[] actualActiveNativeContourMarkers;
            public string[] beforeHeightSha256, afterHeightSha256;
            public bool currentInputChainExact, allRoadContoursExact, settingsExact;
            public ApronIsolationField[] fields;
        }
        [Serializable] public sealed class SequentialGradeCase
        {
            public string order;
            public string[] originalInputs, createdContours;
            public bool creationHeightfieldsExact, randomStartExact;
            public FragmentGroundTargetComparison lowerTargets, upperTargets;
            public FragmentGroundTargetComparison[] fullContextLegTargets;
            public List<SequentialGradeStage> stages = new List<SequentialGradeStage>();
        }
        [Serializable] public sealed class SequentialGradeEvidence
        {
            public string status = "IN_PROGRESS", error; public bool fullContextMasks;
            public string scope = "ORIGINAL_RAW_VISTA_MAIN_ONE_BUILD_VS_PUBLIC_CURRENT_FIELD_SEQUENTIAL_SOURCE_FRAGMENT_BUILDS_BOTH_ORDERS_NOT_WORLD_ACCEPTANCE";
            public bool sameInputs, sameCreatedContours, sourceBytesExact, incomingContextExact, globalRandomStateRestored;
            public DragonKingdomWorldAuthoring.NativeGradingSourcePlan originalPlan;
            public List<SequentialGradeCase> cases = new List<SequentialGradeCase>();
        }
        public static SequentialGradeEvidence LastSequentialGradeEvidence { get; private set; }
        static void SaveSequentialGrade(SequentialGradeEvidence proof)
        {
            Directory.CreateDirectory(OutputDirectory());
            File.WriteAllText(Path.Combine(OutputDirectory(), proof.fullContextMasks?"actual-original-vista-full-context-leg-grades.json":"actual-original-vista-sequential-grades.json"), JsonUtility.ToJson(proof, true));
        }
        static ERRoad CreateExactGradeFragment(ERRoadNetwork network, ERModularBase owner, ERRoadType type,
            ERRoad main, DragonKingdomWorldAuthoring.NativeGradingSourcePlan plan, int first, int last)
        {
            if (plan.intervals.Any(v => v.marker >= first && v.marker < last && v.excluded))
                throw new InvalidOperationException("A sequential grade must never enable an excluded original interval.");
            var points = plan.actualGradingMarkers.Skip(first).Take(last - first + 1).ToArray();
            var fragment = network.CreateRoad("Exact source grade " + first + " to " + last, type, points,
                Enumerable.Range(first, points.Length).Select(main.GetMarkerControlType).ToArray());
            fragment.SetTerrainDeformation(true, false); fragment.SnapToTerrain(false, false); fragment.SetResolution(.35f, false);
            for (int i = 0; i < points.Length; i++)
            {
                float indent = fragment.SetIndent(28, i), surrounding = fragment.SetSurrounding(110, i);
                if (indent != 28 || surrounding != 110) throw new InvalidOperationException("Actual native fragment marker settings did not match28/110.");
            }
            DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(fragment, type, owner, 28, 110);
            fragment.Refresh();
            if (!fragment.GetMarkerPositions().SequenceEqual(points)) throw new InvalidOperationException("A fragment changed original source marker geometry.");
            return fragment;
        }

        static ERRoad CreateFullContextGradeLeg(ERRoadNetwork network,ERModularBase owner,ERRoadType type,ERRoad main,DragonKingdomWorldAuthoring.NativeGradingSourcePlan plan,int first,int last)
        {
            if(plan.intervals.Any(v=>v.marker>=first&&v.marker<last&&v.excluded))throw new InvalidOperationException("Never enable an original excluded source interval in an auxiliary grade mask.");
            var points=plan.actualGradingMarkers;var leg=network.CreateRoad("Auxiliary full17-context grade "+first+" to "+last,type,points,Enumerable.Range(0,points.Length).Select(main.GetMarkerControlType).ToArray());leg.SetTerrainDeformation(true,false);leg.SnapToTerrain(false,false);leg.SetResolution(.35f,false);
            for(int i=0;i<points.Length;i++){float indent=leg.SetIndent(28,i),surround=leg.SetSurrounding(110,i);if(indent!=28||surround!=110)throw new InvalidOperationException("Exact full-context native marker28/110 settings required.");}
            foreach(var interval in plan.intervals)
            {
                bool enabled=!interval.excluded&&interval.marker>=first&&interval.marker<last;leg.SetTerrainDeformation(interval.marker,enabled);
                if(interval.excluded){leg.SetStartLevelDistance(interval.marker,16,false);leg.SetEndLevelDistance(interval.marker,16,false);}
                else if(!enabled){leg.SetStartLevelDistance(interval.marker,0,false);leg.SetEndLevelDistance(interval.marker,0,false);}
            }
            DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(leg,type,owner,28,110);leg.Refresh();
            if(!leg.GetMarkerPositions().SequenceEqual(points)||plan.intervals.Any(v=>leg.roadScript.markersExt[v.marker].bridgeObject==(!v.excluded&&v.marker>=first&&v.marker<last)))throw new InvalidOperationException("Auxiliary-only full-context public mask or original geometry mismatch.");return leg;
        }
        static ApronIsolationField[] CaptureSequentialGradeFields(List<Terrain> terrains, List<TerrainData> data,
            string[] sources, string[] before, float[][,] beforeHeights, string tag)
        {
            var fields = new List<ApronIsolationField>();
            for (int i = 0; i < data.Count; i++)
            {
                var after = data[i].GetHeights(0, 0, 513, 513); var bytes = new byte[after.Length * 4];
                Buffer.BlockCopy(after, 0, bytes, 0, bytes.Length);
                var row = new ApronIsolationField { source = sources[i], inputSha256 = before[i], outputSha256 = Heights(data[i]), origin = terrains[i].transform.position };
                for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(after[z,x]!=beforeHeights[i][z,x])row.changed++;
                row.outputFloat32 = Path.Combine(OutputDirectory(), "sequential-grade-" + tag + "-" + i + ".height.f32"); File.WriteAllBytes(row.outputFloat32, bytes);
                var authority = terrains[i].GetComponent<ERTerrain>();
                if (authority == null) throw new InvalidOperationException("Each real public Build must produce actual ERTerrain evidence.");
                var protection = new int[513 * 513]; row.publicXStart = authority.xStart; row.publicZStart = authority.zStart; row.publicProtectionEntries = authority.terrainChanges.Count;
                foreach (var change in authority.terrainChanges)
                {
                    if (change.index < 0 || change.index >= protection.Length) throw new InvalidOperationException("Actual public mask index outside source field.");
                    protection[change.index] = change.value;
                }
                row.protectedRoadValue2 = protection.Count(v => v == 2); var mask = new byte[protection.Length * 4]; Buffer.BlockCopy(protection, 0, mask, 0, mask.Length);
                row.protectionSha256 = SingleRoadDigest(mask); row.protectionInt32 = Path.Combine(OutputDirectory(), "sequential-grade-" + tag + "-" + i + ".protection.i32"); File.WriteAllBytes(row.protectionInt32, mask); fields.Add(row);
            }
            return fields.ToArray();
        }
        public static void CaptureOriginalVistaSequentialGradesFromCommandLine() => CaptureOriginalVistaSequentialGrades(false);
        public static void CaptureOriginalVistaFullContextLegGradesFromCommandLine() => CaptureOriginalVistaSequentialGrades(true);
        static void CaptureOriginalVistaSequentialGrades(bool fullContextMasks)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Terrain.activeTerrains.Length != 0 || UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Sequential grading requires the stopped isolated native scene.");
            const string networkPath = "Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab", materialPath = "Assets/EasyRoads3D/Resources/Materials/roads/road material.mat", foundation = "Assets/TruckDragonKingdom/ReviewCandidates/RoundedVista01/Terrain/";
            var sources = new[] { foundation + "Kingdom 1 0.asset", foundation + "Kingdom 1 1.asset" };
            var watch = sources.SelectMany(p => AssetDatabase.GetDependencies(p, true)).Concat(sources).Concat(new[] { networkPath, materialPath, "Assets/EasyRoads3D/lib/EasyRoads3Dv3.dll", "Assets/TruckDragonKingdom/Editor/DragonKingdomNativeRoadTypeProbe.SequentialGrades.cs", "Assets/TruckDragonKingdom/Editor/DragonKingdomNativeRoadTypeProbe.ApronIsolation.cs", "Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAuthoring.cs", "Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAuthoring.NativeGradingPlan.cs" }).SelectMany(p => new[] { p, p + ".meta" }).Distinct().Where(File.Exists).ToDictionary(p => p, Sha);
            foreach (var source in sources.Concat(new[] { networkPath, materialPath })) if (!watch.ContainsKey(source)) throw new InvalidOperationException("Required original source missing:" + source);
            PrepareIsolatedBatchRunner(new Evidence(), watch);
            var incoming = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(s => new { s, dirty = s.isDirty, poses = JsonUtility.ToJson(new RunnerPoseSet { poses = RunnerPoses(s) }) }).ToArray();
            var prior = SceneManager.GetActiveScene(); var random = UnityEngine.Random.state;
            var proof = new SequentialGradeEvidence { fullContextMasks=fullContextMasks,originalPlan = DragonKingdomWorldAuthoring.InspectNativeMainRoadGradingPlan("caverns-volcano", DragonKingdomWorldAuthoring.CavernUndergroundBounds(), true) };
            if(fullContextMasks)proof.scope="AUXILIARY_ONLY_FULL_ORIGINAL17_MARKER_GEOMETRY_PUBLIC_PER_MARKER_DEFORMATION_MASKS_FOUR_PHYSICAL_STRAIGHT_LEGS_BOTH_ORDERS_NOT_WORLD_ACCEPTANCE";LastSequentialGradeEvidence = proof; SaveSequentialGrade(proof);
            try
            {
                // Both fragments cover every non-excluded original source interval, exactly once.
                if (proof.originalPlan.actualGradingMarkers.Length != 17 || proof.originalPlan.intervals.Any(v => v.marker < 5 ? !v.excluded : v.excluded)) throw new InvalidOperationException("Original interval inventory differs; never infer a new grading plan.");
                for (int mode = 0; mode < 3; mode++)
                {
                    var row = new SequentialGradeCase { order = fullContextMasks?(mode==0?"ORIGINAL_MAIN_SINGLE_BUILD":mode==1?"FULL_CONTEXT_LEGS13_10_7_5_DESCENDING":"FULL_CONTEXT_LEGS5_7_10_13_ASCENDING"):mode == 0 ? "ORIGINAL_MAIN_SINGLE_BUILD" : mode == 1 ? "UPPER13_TO16_THEN_LOWER5_TO13" : "LOWER5_TO13_THEN_UPPER13_TO16" }; proof.cases.Add(row);
                    Scene scene = default; var data = new List<TerrainData>(); var paths = new List<string>();
                    try
                    {
                        UnityEngine.Random.state = random; row.randomStartExact = UnityEngine.Random.state.Equals(random);
                        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); EditorSceneManager.SetActiveScene(scene); var terrains = new List<Terrain>();
                        for (int i = 0; i < 2; i++)
                        {
                            var original = AssetDatabase.LoadAssetAtPath<TerrainData>(sources[i]); if (original == null || original.heightmapResolution != 513 || original.size != new Vector3(1600, 600, 1600)) throw new InvalidOperationException("Exact original raw Vista source required.");
                            var clone = UnityEngine.Object.Instantiate(original); if(Heights(clone)!=Heights(original))throw new InvalidOperationException("Cloned input must exactly equal actual original source field."); data.Add(clone); const string generated = "Assets/TruckDragonKingdom/Tests/World/Generated"; Directory.CreateDirectory(generated); string path = generated + "/SequentialGrade-" + Guid.NewGuid().ToString("N") + ".asset"; paths.Add(path); AssetDatabase.CreateAsset(clone, path);
                            var go = Terrain.CreateTerrainGameObject(clone); go.transform.position = new Vector3(0, -20, i * 1600 - 800); terrains.Add(go.GetComponent<Terrain>());
                        }
                        row.originalInputs = data.Select(Heights).ToArray(); Physics.SyncTransforms();
                        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(networkPath), scene); var owner = root.GetComponent<ERModularBase>();
                        if (owner.preserveTerrainFloat != 1) throw new InvalidOperationException("Keep actual original vendor preserve setting1.");
                        owner.terrainRaycastHeight = 1000; owner.RoadNetworkInit(); var network = new ERRoadNetwork(owner);
                        var type = new ERRoadType { roadTypeName = "caverns-volcano", roadWidth = proof.originalPlan.width, hasMeshCollider = true, roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath) };
                        DragonKingdomWorldAuthoring.RegisterOwnedNativeGradingType(type, owner, 28, 110);
                        var main = network.CreateRoad("Original complete caverns-volcano", type, proof.originalPlan.actualGradingMarkers, Enumerable.Repeat(ERMarkerControlType.StraightXZY, 17).ToArray()); main.SetTerrainDeformation(true, false); main.SnapToTerrain(false, false); main.SetResolution(.35f, false);
                        for (int i = 0; i < 17; i++) { float indent = main.SetIndent(28, i), surrounding = main.SetSurrounding(110, i); if (indent != 28 || surrounding != 110) throw new InvalidOperationException("Original actual marker globals changed."); }
                        foreach (var interval in proof.originalPlan.intervals.Where(v => v.excluded)) { main.SetTerrainDeformation(interval.marker, false); main.SetStartLevelDistance(interval.marker, 16); main.SetEndLevelDistance(interval.marker, 16); }
                        DragonKingdomWorldAuthoring.ConfigureOwnedNativeRoadGlobals(main, type, owner, 28, 110); main.Refresh();
                        ERRoad lower=null,upper=null;ERRoad[] legs=null;var legFirst=new[]{5,7,10,13};var legLast=new[]{7,10,13,16};ERRoad[] roads;
                        if(fullContextMasks){legs=Enumerable.Range(0,4).Select(i=>CreateFullContextGradeLeg(network,owner,type,main,proof.originalPlan,legFirst[i],legLast[i])).ToArray();roads=new[]{main}.Concat(legs).ToArray();row.fullContextLegTargets=legs.Select(r=>CompareFragmentGroundTargets(main.roadScript,r.roadScript,0,true)).ToArray();}
                        else{lower=CreateExactGradeFragment(network,owner,type,main,proof.originalPlan,5,13);upper=CreateExactGradeFragment(network,owner,type,main,proof.originalPlan,13,16);roads=new[]{main,lower,upper};row.lowerTargets=CompareFragmentGroundTargets(main.roadScript,lower.roadScript,5);row.upperTargets=CompareFragmentGroundTargets(main.roadScript,upper.roadScript,13);}
                        row.createdContours = roads.Select(r => NativeApronContourDigest(r.roadScript)).ToArray();
                        row.creationHeightfieldsExact = row.originalInputs.SequenceEqual(data.Select(Heights));
                        if (!row.randomStartExact || !row.creationHeightfieldsExact) throw new InvalidOperationException("Source creation changed native input or controlled random state.");
                        // Target failures remain visible, but capture both genuine orders before failing acceptance.
                        var selected = fullContextMasks?(mode==0?new[]{main}:mode==1?legs.Reverse().ToArray():legs):mode == 0 ? new[] { main } : mode == 1 ? new[] { upper, lower } : new[] { lower, upper };
                        var expectedInput = row.originalInputs;
                        for (int stageIndex = 0; stageIndex < selected.Length; stageIndex++)
                        {
                            var beforeHeights=data.Select(d=>d.GetHeights(0,0,513,513)).ToArray();
                            var road = selected[stageIndex]; var stage = new SequentialGradeStage { selectedSource = road.gameObject.name, originalFirstMarker = road==main?0:fullContextMasks?legFirst[Array.IndexOf(legs,road)]:road==lower?5:13, originalLastMarker = road==main?16:fullContextMasks?legLast[Array.IndexOf(legs,road)]:road==lower?13:16,actualAuxiliaryMarkerDeformation=road.roadScript.markersExt.Select(m=>!m.bridgeObject).ToArray(),actualActiveNativeContourMarkers=Enumerable.Range(0,road.roadScript.bridgeElement.Count).Where(i=>!road.roadScript.bridgeElement[i]).Select(i=>road.roadScript.markerInts[i]).Distinct().OrderBy(i=>i).ToArray(), beforeHeightSha256 = data.Select(Heights).ToArray(), selectedContourSha256 = NativeApronContourDigest(road.roadScript) }; row.stages.Add(stage);
                            stage.currentInputChainExact = expectedInput.SequenceEqual(stage.beforeHeightSha256); if (!stage.currentInputChainExact) throw new InvalidOperationException("Sequential SDK stage input does not equal actual preceding output."); SaveSequentialGrade(proof);
                            // Supported public subset Build only. No Refresh, restore, cache mutation or height kernel between calls.
                            network.BuildRoadNetwork(false, false, false, new[] { road });
                            stage.afterHeightSha256 = data.Select(Heights).ToArray(); stage.allRoadContoursExact = row.createdContours.SequenceEqual(roads.Select(r => NativeApronContourDigest(r.roadScript)));
                            stage.settingsExact = owner.preserveTerrainFloat == 1 && owner.terrainRaycastHeight == 1000 && roads.All(r => r.roadScript.indent == 28 && r.roadScript.surrounding == 110 && r.roadScript.rt.defaultIndent == 28 && r.roadScript.rt.defaultSurrounding == 110) && !owner.selectedRoadsOnly;
                            stage.fields = CaptureSequentialGradeFields(terrains, data, sources, stage.beforeHeightSha256, beforeHeights, (fullContextMasks?"full-context-":"")+mode + "-" + stageIndex); expectedInput = stage.afterHeightSha256;
                            if (!stage.allRoadContoursExact || !stage.settingsExact) throw new InvalidOperationException("Sequential public Build changed original configured grade source/contours/settings."); SaveSequentialGrade(proof);
                        }
                    }
                    finally { if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); if (prior.IsValid() && prior.isLoaded) EditorSceneManager.SetActiveScene(prior); foreach (string path in paths) AssetDatabase.DeleteAsset(path); SaveSequentialGrade(proof); }
                }
                proof.sameInputs = proof.cases.Count == 3 && proof.cases.All(c => c.originalInputs.SequenceEqual(proof.cases[0].originalInputs));
                proof.sameCreatedContours = proof.cases.Count == 3 && proof.cases.All(c => c.createdContours.SequenceEqual(proof.cases[0].createdContours));
                if (!proof.sameInputs || !proof.sameCreatedContours) throw new InvalidOperationException("Both orders require identical source fields and actual created contour geometry.");
                proof.status = fullContextMasks?(proof.cases.All(c=>c.fullContextLegTargets!=null&&c.fullContextLegTargets.Length==4&&c.fullContextLegTargets.All(t=>t.passed))?"FULL_CONTEXT_PUBLIC_MASK_STAGES_CAPTURED_TARGET_CORRESPONDENCE_PASSED_REQUIRES_PHYSICS_AND_QUALITY":"FULL_CONTEXT_PUBLIC_MASK_STAGES_CAPTURED_TARGET_CORRESPONDENCE_REJECTED"):proof.cases.All(c => c.lowerTargets.passed && c.upperTargets.passed) ? "ACTUAL_SEQUENTIAL_PUBLIC_BUILDS_CAPTURED_TARGET_CORRESPONDENCE_PASSED_REQUIRES_PHYSICAL_AND_QUALITY_ANALYSIS" : "ACTUAL_SEQUENTIAL_BUILDS_CAPTURED_FRAGMENT_TARGET_CORRESPONDENCE_REJECTED";
            }
            catch (Exception error) { proof.status = "REJECTED_SEQUENTIAL_GRADING_CONTROL"; proof.error = error.ToString(); throw; }
            finally
            {
                UnityEngine.Random.state = random; proof.globalRandomStateRestored = UnityEngine.Random.state.Equals(random);
                proof.sourceBytesExact = watch.All(p => File.Exists(p.Key) && Sha(p.Key) == p.Value);
                proof.incomingContextExact = SceneManager.sceneCount == incoming.Length && SceneManager.GetActiveScene() == prior && incoming.All(p => p.s.IsValid() && p.s.isLoaded && p.s.isDirty == p.dirty && JsonUtility.ToJson(new RunnerPoseSet { poses = RunnerPoses(p.s) }) == p.poses);
                SaveSequentialGrade(proof); if (!proof.globalRandomStateRestored || !proof.sourceBytesExact || !proof.incomingContextExact) throw new InvalidOperationException("Sequential control changed original source or incoming context/random state.");
            }
        }
    }
}
#endif
