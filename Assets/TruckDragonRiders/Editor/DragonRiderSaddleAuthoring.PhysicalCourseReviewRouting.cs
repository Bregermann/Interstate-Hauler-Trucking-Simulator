#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        const string PhysicalCourseReceiptArgument="-tsDragonCanonicalPhysicalCourseReceipt";
        sealed class PhysicalCourseReviewLease{public string receipt,production,scene,sha256;}
        static PhysicalCourseReviewLease pendingPhysicalCourseReview;
        static bool physicalCourseReviewActive;
        sealed class PhysicalCourseReviewScope:IDisposable
        {bool disposed;public void Dispose(){if(disposed)return;disposed=true;pendingPhysicalCourseReview=null;physicalCourseReviewActive=false;}}
        public static IDisposable OpenCertifiedPhysicalCourseReviewScope()
        {
            if(physicalCourseReviewActive)throw new InvalidOperationException("Nested owned course review is not allowed.");var canonical=SceneManager.GetSceneByPath(DragonRidersAuthoring.ScenePath);if(canonical.IsValid()&&canonical.isLoaded)throw new InvalidOperationException("Preserve user-owned canonical scenes; close them before the explicit isolated review.");physicalCourseReviewActive=true;return new PhysicalCourseReviewScope();
        }
        static string ExplicitPhysicalCourseReceipt(string[] args)
        {
            int count=args?.Count(value=>value==PhysicalCourseReceiptArgument)??0;if(count==0)return null;int at=Array.IndexOf(args,PhysicalCourseReceiptArgument);
            if(count!=1||at+1>=args.Length||string.IsNullOrWhiteSpace(args[at+1])||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("One explicit actual physical-course receipt value is required; no mount-only or ambiguous authority.");return Path.GetFullPath(args[at+1]);
        }
        static string PhysicalCourseReviewSourcePath(string productionEvidence)
        {
            string receipt=ExplicitPhysicalCourseReceipt(Environment.GetCommandLineArgs());if(receipt==null)return ReadQualifiedCourseProduction(productionEvidence).candidateScene;
            if(!physicalCourseReviewActive)throw new InvalidOperationException("An explicit scoped native world/course review must own canonical source selection.");
            var course=UnityEngine.JsonUtility.FromJson<CanonicalCourseApply>(File.ReadAllText(receipt));if(course?.plan==null||course.target!=DragonRidersAuthoring.ScenePath||course.plan.testOnly||!course.canonicalTarget)throw new InvalidOperationException("Only the explicit actual canonical course target may be selected.");var mount=UnityEngine.JsonUtility.FromJson<CanonicalMountApply>(File.ReadAllText(course.plan.mountReceipt));if(Path.GetFullPath(mount.productionEvidence)!=Path.GetFullPath(productionEvidence))throw new InvalidOperationException("Explicit course/production evidence belong to different profiles.");
            var loaded=SceneManager.GetSceneByPath(DragonRidersAuthoring.ScenePath);if(loaded.IsValid()&&loaded.isLoaded)throw new InvalidOperationException("Close user-owned canonical Riders before selecting the isolated certified review source.");
            // Source selection is not acceptance. Full source/geometry/NWH
            // qualification occurs once below, with the owned source closed,
            // before the existing world clearance query can run.
            pendingPhysicalCourseReview=new PhysicalCourseReviewLease{receipt=receipt,production=Path.GetFullPath(productionEvidence),scene=course.target,sha256=Hash(course.target)};return course.target;
        }
        public static QualifiedCourseSource ValidateQualifiedMountedCourseSource(ref Scene candidate,string productionEvidence,string physicalCourseEvidence)
        {
            string receipt=ExplicitPhysicalCourseReceipt(Environment.GetCommandLineArgs());if(receipt==null)return ValidateQualifiedMountedCourseSourceLegacy(ref candidate,productionEvidence,physicalCourseEvidence);
            var lease=pendingPhysicalCourseReview;pendingPhysicalCourseReview=null;
            if(!physicalCourseReviewActive||lease==null||lease.receipt!=receipt||lease.production!=Path.GetFullPath(productionEvidence)||!candidate.IsValid()||!candidate.isLoaded||candidate.isDirty||candidate.path!=lease.scene||!CourseCertificateSame(Hash(candidate.path),lease.sha256))throw new InvalidOperationException("Only the clean review-owned scene opened after certified source selection may be closed/rebound.");
            if(!EditorSceneManager.CloseScene(candidate,true))throw new InvalidOperationException("Could not close the isolated owned source before certificate requalification.");candidate=default;
            var course=UnityEngine.JsonUtility.FromJson<CanonicalCourseApply>(File.ReadAllText(receipt));if(Path.GetFullPath(course.plan.courseEvidence)!=Path.GetFullPath(physicalCourseEvidence))throw new InvalidOperationException("The selected course and actual physical recipe differ.");var authority=ReadCanonicalPhysicalCourseSourceAuthority(receipt);candidate=EditorSceneManager.OpenScene(authority.proof.target,OpenSceneMode.Additive);var result=CourseRecordedSourceMetadata(candidate,productionEvidence,physicalCourseEvidence,authority);result.canonicalSourceReview=true;result.canonicalSourceCertificate=UnityEngine.JsonUtility.ToJson(authority.proof,true);return result;
        }
        // The later ROOT-only mount-only API stays fail-closed. The qualified
        // legacy C500 production-candidate path above remains unchanged.
        public static QualifiedCourseSource ValidateQualifiedCanonicalMountedCourseSource(ref Scene candidate,string productionEvidence,string physicalCourseEvidence,string mountReceipt)
            =>throw new InvalidOperationException("Mount-only source authority preserves32m rings. Use the explicit actual physical-course certificate and isolated review routing.");
    }
}
#endif
