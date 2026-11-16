#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyRoads3Dv3;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomWorldAuthoring
    {
        public const string NativeGradeProfileFlag="-tsDragonNativeGradeProfile",NativeWithoutHairpinApronsProfile="SourceRoadsWithoutHairpinAprons01";
        [Serializable]public sealed class NativeGradeSelectionEvidence
        {
            public string scope="PUBLIC_SINGLE_BUILD_SUBSET_OMITS_ONLY_EXPLICITLY_TRACKED_TEMPORARY_HAIRPIN_TERRAIN_AUTHORIZERS_NOT_PHYSICAL_PAVEMENT_GRAPH_OR_SOURCE_MARKERS",profile=NativeWithoutHairpinApronsProfile,status="IN_PROGRESS";
            public int actualCreatedRoads,selectedRoads,excludedHairpinGrades;public string[] createdRoadNames,selectedRoadNames,excludedHairpinNames,originalMainRoadNames;
            public bool allOriginalMainRoadsSelected,allOtherCreatedGradesSelected,onlyTrackedHairpinGradesExcluded,originalBuildChannelFlagsPreserved;
        }
        public static NativeGradeSelectionEvidence LastNativeGradeSelectionEvidence{get;private set;}
        public static bool NativeHairpinGradePolicyRequested()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,NativeGradeProfileFlag);if(at<0)return false;
            if(at+1>=args.Length||args[at+1]!=NativeWithoutHairpinApronsProfile)throw new InvalidOperationException("Explicit known source hairpin grading policy required.");return true;
        }
        static void BuildNativeWithoutHairpinGradeOverwrites(ERRoadNetwork network,ERModularBase owner,IReadOnlyList<ERRoad> all,IReadOnlyList<ERRoad> trackedHairpins)
        {
            var proof=new NativeGradeSelectionEvidence{actualCreatedRoads=all.Count,excludedHairpinGrades=trackedHairpins.Count,createdRoadNames=all.Select(r=>r.gameObject.name).ToArray(),excludedHairpinNames=trackedHairpins.Select(r=>r.gameObject.name).ToArray(),originalMainRoadNames=Roads().Where(r=>r.id!="boarding-deck").Select(r=>r.id).ToArray()};LastNativeGradeSelectionEvidence=proof;
            var omitted=trackedHairpins.ToHashSet();if(omitted.Count==0||omitted.Count!=trackedHairpins.Count||trackedHairpins.Any(r=>!all.Contains(r)))throw new InvalidOperationException("Only actual separately tracked source HairpinTurns grading objects may be omitted.");
            var selected=all.Where(r=>!omitted.Contains(r)).ToArray();proof.selectedRoads=selected.Length;proof.selectedRoadNames=selected.Select(r=>r.gameObject.name).ToArray();proof.allOriginalMainRoadsSelected=proof.originalMainRoadNames.All(name=>proof.selectedRoadNames.Contains(name));proof.allOtherCreatedGradesSelected=all.Where(r=>!omitted.Contains(r)).All(selected.Contains);proof.onlyTrackedHairpinGradesExcluded=all.Count-selected.Length==omitted.Count&&selected.All(r=>!omitted.Contains(r));
            string output="Logs/TruckSports/DragonKingdom";var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length)output=args[at+1];Directory.CreateDirectory(output);string path=Path.Combine(output,"actual-native-source-grade-selection.json");
            if(File.Exists(path))throw new InvalidOperationException("Preserve an existing source-grade selection receipt; use a fresh native output directory.");
            try
            {
                if(!proof.allOriginalMainRoadsSelected||!proof.allOtherCreatedGradesSelected||!proof.onlyTrackedHairpinGradesExcluded)throw new InvalidOperationException("Never omit an original main road or unrelated regional/house/loading/water grade.");
                // Installed zero-argument Build uses false/false/false channels
                // plus heightmap=true, exactly matching this public subset call.
                network.BuildRoadNetwork(false,false,false,selected);proof.originalBuildChannelFlagsPreserved=!owner.doSplatmaps&&!owner.doTrees&&!owner.doDetail&&owner.doHeightmap&&!owner.selectedRoadsOnly;if(!proof.originalBuildChannelFlagsPreserved)throw new InvalidOperationException("Public subset Build must retain original height-only channels and restore its selection state.");proof.status="ACTUAL_SOURCE_MAIN_GRADES_BUILT_WITHOUT_REDUNDANT_HAIRPIN_OVERWRITE_REQUIRES_FULL_WORLD_GATES";
            }
            finally{File.WriteAllText(path,JsonUtility.ToJson(proof,true));}
        }
    }
}
#endif
