#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        static string[] CanonicalPhysicalCourseCommandArguments(string[] args)
        {
            const string marker="-tsRiderApplyCanonicalPhysicalCourse";
            var flags=new[]{"-tsRiderCourseCanonicalMountReceipt","-tsRiderProductionCourseEvidence","-tsRiderPhysicalCourseBridgeEvidence","-tsRiderCanonicalPhysicalCourseOutput"};
            if(args==null||args.Count(value=>value==marker)!=1)throw new ArgumentException("Exactly one explicit physical-course Apply marker is required; ordinary tests/builds never apply courses.");
            var values=flags.Select(flag=>{int at=Array.IndexOf(args,flag);if(args.Count(value=>value==flag)!=1||at<0||at+1>=args.Length||string.IsNullOrWhiteSpace(args[at+1])||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("Exactly one explicit evidence/output value required: "+flag);return args[at+1];}).ToArray();
            if(values.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=4)throw new ArgumentException("Mount receipt, physical-course evidence, bridge-native evidence and output must have distinct exact roles.");return values;
        }
        // Explicit executeMethod only. No initialization or build callback.
        public static void ApplyCanonicalPhysicalCourseFromCommandLine()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Explicit batch command required; interactive tools must use guarded Plan/Apply directly.");var values=CanonicalPhysicalCourseCommandArguments(Environment.GetCommandLineArgs());
            if(!File.Exists(values[0])||!File.Exists(Path.Combine(values[1],"tests.xml"))||!File.Exists(Path.Combine(values[2],"tests.xml")))throw new InvalidOperationException("Actual mount receipt and both specific passed native evidence roles are required before preparing a runner scene.");
            string output=Path.GetFullPath(values[3]),permitted=Path.Combine(RiderEvidenceHostRoot(values[2]),"Logs","TruckSports","DragonKingdom")+Path.DirectorySeparatorChar,planPath=output+".plan.json";
            if(!(output+Path.DirectorySeparatorChar).StartsWith(permitted,StringComparison.OrdinalIgnoreCase)||output.TrimEnd(Path.DirectorySeparatorChar)==permitted.TrimEnd(Path.DirectorySeparatorChar)||Directory.Exists(output)||File.Exists(output)||File.Exists(planPath))throw new InvalidOperationException("Fresh private DragonKingdom Logs output and sibling plan required; preserve all prior evidence.");
            if(SceneManager.sceneCount==1)
            {
                var incoming=SceneManager.GetSceneAt(0);if(string.IsNullOrEmpty(incoming.path)&&!incoming.isDirty&&incoming.rootCount==0)
                {
                    const string folder="Assets/TruckDragonRiders/ReviewCandidates/CanonicalPhysicalCourseRunner/";Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,folder));AssetDatabase.Refresh();if(!EditorSceneManager.SaveScene(incoming,folder+"Isolated-"+Guid.NewGuid().ToString("N")+".unity"))throw new InvalidOperationException("Could not preserve the empty batch runner scene.");
                }
            }
            RequirePromotionEditorContext();string plan=BuildCanonicalPhysicalCoursePromotionPlan(values[0],values[1],values[2]);Directory.CreateDirectory(Path.GetDirectoryName(planPath));
            using(var stream=new FileStream(planPath,FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream))writer.Write(plan);
            // Apply owns its fresh transaction directory and exact rollback;
            // the sibling plan is durable before any canonical mutation.
            var receipt=JsonUtility.FromJson<CanonicalCourseApply>(ApplyCanonicalPhysicalCoursePromotion(plan,output));Debug.Log("Explicit physical-course promotion: "+receipt.status+"; actual world/flight acceptance remains separate. Receipt: "+Path.Combine(output,"physical-course-promotion.json"));
        }
    }
}
#endif
