#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class MountedSizeLeaseReceipt
        {
            public string scope="TRANSIENT_SCENE_OWNED_FIRE_1_POINT87_FROM_ACTUAL_V313_FULL_GEOMETRY_NO_SAVED_ASSETS_OR_RELEASE_ACCEPTANCE",evidence,evidenceSha256;
            public float factor;public int actors,observations;public bool unchangedControllers,unchangedDeckWorldDimensions,unchangedOriginalNwhGeometry,sourceBytesExact,restored;
            public bool authorizedOwnedNormalFamily,strictTorsoSupport,expectedOwnedControllersRetained,authorizedProvenHeadLeverFamily;public float neckPitch,headPitch;public string expectedRuntimeController;
            public List<MountedSizeLiveGeometry> liveGeometry=new List<MountedSizeLiveGeometry>();
            public string originalNwhGeometrySignature,actualNwhGeometrySignature;
            public bool exactAuthoredNwhConfiguration,finiteWorldBoxDimensionsPreserved;
            public float maximumFiniteWorldBoxDimensionDrift;
            public FixedNwhField[] originalNwhFields,actualNwhFields;
            public List<FixedNwhDifference> originalNwhFieldDifferences=new List<FixedNwhDifference>();
        }
        [Serializable]sealed class FixedNwhField{public string path,floatBits;public float[] values;}
        [Serializable]sealed class FixedNwhDifference{public string path,beforeBits,afterBits;public float[] before,after;public float maximumAbsoluteDifference;}
        [Serializable]sealed class MountedSizeLiveGeometry
        {
            public string scope="READ_ONLY_AFTER_REAL_FINAL_ATTACHMENT_NATIVE_BREATH_POSE_COMPLETE_SKIN_AND_ORIGINAL_NWH_TIMBER_NOT_EXTERNAL_TARGET_DAMAGE",phase,action;
            public int owner,frame,finalAttachmentFrame,triangles,boxes,satPairs,wheels;
            public float actualNormalized,clock,maximumActualWheelGap;
            public bool physicallyMounted,allActualWheelsFound;
            public List<SavedTriangleGeometry> firstIntersections=new List<SavedTriangleGeometry>();
        }
        sealed class MountedSizeLease:IDisposable
        {
            readonly DragonRidersMatch match;readonly DragonCharacter[] actors;readonly DragonRiderSaddle[] saddles;
            readonly Vector3[] rootScales,deckScales,deckWorldDimensions;readonly RuntimeAnimatorController[] controllers;
            readonly RuntimeAnimatorController[] expectedControllers;readonly Calibration[] originalCalibrations;readonly List<UnityEngine.Object> transientFamilyObjects=new List<UnityEngine.Object>();
            readonly Dictionary<string,string> protectedFiles;readonly int originalFileCount;readonly string originalNwh;bool disposed;
            public readonly MountedSizeLeaseReceipt receipt;
            public MountedSizeLease(DragonRidersMatch owner,MountedSizeProfile proof,string evidence,bool normalFamilyStrict=false,ProvenFireHeadLever headLever=null)
            {
                match=owner;actors=match.racers.SelectMany(racer=>racer.choices).Where(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId=="dragon-fire").ToArray();
                if(actors.Length!=4||actors.Any(actor=>AssetDatabase.Contains(actor)||AssetDatabase.GetAssetPath(actor.motor.animator.runtimeAnimatorController)!=proof.controller||Vector3.Distance(actor.transform.localScale,proof.originalActorScale)>.000001f||Vector3.Distance(actor.motor.transform.localScale,proof.originalMotorScale)>.000001f))throw new InvalidOperationException("The four original saved scene-owned Fire choices must match the actual positive native geometry control; preserve unknown designer configurations.");
                saddles=actors.Select(actor=>actor.GetComponent<DragonRiderSaddle>()).ToArray();rootScales=actors.Select(actor=>actor.transform.localScale).ToArray();deckScales=saddles.Select(frame=>frame.supportPresentation.localScale).ToArray();deckWorldDimensions=saddles.Select(frame=>frame.supportPresentation.lossyScale).ToArray();controllers=actors.Select(actor=>actor.motor.animator.runtimeAnimatorController).ToArray();
                expectedControllers=(RuntimeAnimatorController[])controllers.Clone();originalCalibrations=saddles.Select(CaptureFrameCalibration).ToArray();
                var modeFiles=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);originalFileCount=modeFiles.Length;
                protectedFiles=modeFiles.Concat(proof.sourceFiles.Select(row=>row.path)).Concat(new[]{DragonRidersAuthoring.ScenePath,DragonRidersAuthoring.ScenePath+".meta"}).Distinct().Where(File.Exists).ToDictionary(path=>path,Hash);originalNwh=FixedOriginalNwhGeometry(match);
                receipt=new MountedSizeLeaseReceipt{evidence=evidence,evidenceSha256=Hash(evidence),factor=proof.roundedCandidateFactor,actors=actors.Length,originalNwhGeometrySignature=originalNwh,originalNwhFields=CaptureFixedNwhFields(match)};
                if(normalFamilyStrict){receipt.scope="TRANSIENT_SCENE_OWNED_V325_FIRE_V279_NORMAL_FAMILY_STRICT_TORSO_1_POINT67_NO_SAVED_ASSETS_OR_RELEASE_ACCEPTANCE";receipt.authorizedOwnedNormalFamily=receipt.strictTorsoSupport=true;}
                if(headLever!=null){receipt.scope="TRANSIENT_SCENE_OWNED_V346_PROVEN_FIRE_NECK60_HEAD_MINUS15_STRICT_TORSO_1_POINT67_NOT_RELEASE_ACCEPTANCE";receipt.authorizedProvenHeadLeverFamily=true;receipt.neckPitch=headLever.neck;receipt.headPitch=headLever.head;}
                try
                {
                    if(normalFamilyStrict)
                    {
                        var generated=headLever!=null?CreateProvenFireHeadLeverEnvelope(actors[0],headLever):CreateTransientCabFlightEnvelope(actors[0],"dragon-fire",.5f);transientFamilyObjects.AddRange(generated);var controller=generated.OfType<UnityEngine.AnimatorOverrideController>().Single();receipt.expectedRuntimeController=controller.name;
                        for(int i=0;i<actors.Length;i++){actors[i].motor.animator.runtimeAnimatorController=controller;expectedControllers[i]=controller;ApplyQualifiedStrictFireMaterialProof(saddles[i],proof);}
                    }
                    for(int i=0;i<actors.Length;i++){actors[i].transform.localScale=rootScales[i]*receipt.factor;saddles[i].supportPresentation.localScale=deckScales[i]/receipt.factor;}
                    Observe();
                }
                catch{Dispose();throw;}
            }
            public string Observe()
            {
                if(disposed)throw new ObjectDisposedException(nameof(MountedSizeLease));receipt.observations++;
                receipt.unchangedControllers=Enumerable.Range(0,actors.Length).All(i=>actors[i]!=null&&actors[i].motor.animator.runtimeAnimatorController==controllers[i]);receipt.expectedOwnedControllersRetained=Enumerable.Range(0,actors.Length).All(i=>actors[i]!=null&&actors[i].motor.animator.runtimeAnimatorController==expectedControllers[i]&&Vector3.Distance(actors[i].transform.localScale,rootScales[i]*receipt.factor)<.000001f);
                receipt.unchangedDeckWorldDimensions=Enumerable.Range(0,saddles.Length).All(i=>saddles[i]!=null&&Vector3.Distance(saddles[i].supportPresentation.lossyScale,deckWorldDimensions[i])<.00001f);
                receipt.actualNwhGeometrySignature=match!=null?FixedOriginalNwhGeometry(match):"MATCH_MISSING";receipt.exactAuthoredNwhConfiguration=match!=null&&receipt.actualNwhGeometrySignature==originalNwh;
                receipt.actualNwhFields=CaptureFixedNwhFields(match);receipt.originalNwhFieldDifferences.Clear();var baseline=receipt.originalNwhFields.ToDictionary(field=>field.path);
                foreach(var actual in receipt.actualNwhFields)
                    if(baseline.TryGetValue(actual.path,out var original)&&actual.floatBits!=original.floatBits)receipt.originalNwhFieldDifferences.Add(new FixedNwhDifference{path=actual.path,beforeBits=original.floatBits,afterBits=actual.floatBits,before=original.values,after=actual.values,maximumAbsoluteDifference=actual.values.Select((value,index)=>Mathf.Abs(value-original.values[index])).Max()});
                receipt.finiteWorldBoxDimensionsPreserved=FiniteWorldBoxDimensionsAgree(receipt.originalNwhFields,receipt.actualNwhFields,out receipt.maximumFiniteWorldBoxDimensionDrift);
                receipt.unchangedOriginalNwhGeometry=receipt.exactAuthoredNwhConfiguration&&receipt.finiteWorldBoxDimensionsPreserved;
                Write();
                if(!receipt.expectedOwnedControllersRetained||!receipt.unchangedDeckWorldDimensions||!receipt.unchangedOriginalNwhGeometry)throw new InvalidOperationException("The transient Fire profile must retain its explicitly authorized owned controller, full fixed NWH geometry and timber world dimensions across ordinary starts/rematches.");
                Write();return JsonUtility.ToJson(receipt,true);
            }
            public string ObserveGeometry(int owner,string phase)
            {
                Observe();if(receipt.liveGeometry.Count>=32)throw new InvalidOperationException("The explicit native Breath geometry observation budget is32samples, not an expensive per-frame runtime search.");
                var racer=match.racers[owner];var actor=racer.Dragon;if(actor==null||racer.Species.id!="dragon-fire"||!actors.Contains(actor)||!actor.Prepared)throw new InvalidOperationException("Actual prepared selected Fire participant required.");
                var row=CaptureActualMountedLiveGeometry(match,owner,phase);receipt.liveGeometry.Add(row);Write();return JsonUtility.ToJson(row,true);
            }
            public void Dispose()
            {
                if(disposed)return;Exception failure=null;
                try{Observe();}catch(Exception error){failure=error;}
                finally
                {
                    for(int i=0;i<actors.Length;i++){if(actors[i]!=null){actors[i].transform.localScale=rootScales[i];actors[i].motor.animator.runtimeAnimatorController=controllers[i];}if(saddles[i]!=null){saddles[i].supportPresentation.localScale=deckScales[i];ApplyFrameCalibration(saddles[i],originalCalibrations[i]);}}
                    foreach(var item in transientFamilyObjects)if(item!=null&&!AssetDatabase.Contains(item))UnityEngine.Object.DestroyImmediate(item);transientFamilyObjects.Clear();
                    disposed=true;receipt.restored=Enumerable.Range(0,actors.Length).All(i=>actors[i]!=null&&actors[i].transform.localScale==rootScales[i]&&actors[i].motor.animator.runtimeAnimatorController==controllers[i]&&saddles[i].supportPresentation.localScale==deckScales[i]&&JsonUtility.ToJson(CaptureFrameCalibration(saddles[i]))==JsonUtility.ToJson(originalCalibrations[i]));
                    receipt.sourceBytesExact=originalFileCount==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&protectedFiles.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);Write();
                }
                if(!receipt.restored||!receipt.sourceBytesExact)throw new InvalidOperationException("Transient Fire size cleanup did not restore all original actor/deck scales or protected original source/scene bytes.");
                if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
            void Write()
            {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","MountedSize");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"transient-fire-size-lease.json"),JsonUtility.ToJson(receipt,true));
            }
        }
        static MountedSizeLiveGeometry CaptureActualMountedLiveGeometry(DragonRidersMatch match,int owner,string phase)
        {
            var racer=match.racers[owner];var actor=racer.Dragon;if(actor==null||!actor.Prepared)throw new InvalidOperationException("Actual prepared mounted native participant required.");
                var frame=actor.GetComponent<DragonRiderSaddle>();var truck=racer.truck;var row=new MountedSizeLiveGeometry{owner=owner,phase=phase,frame=Time.frameCount,finalAttachmentFrame=racer.FinalAttachmentFrame,actualNormalized=actor.motor.ActionNormalizedTime,action=actor.motor.CurrentAction.ToString(),clock=match.Clock,physicallyMounted=racer.PhysicallyMounted};
                var boxes=truck.GetComponentsInChildren<Collider>(true).Where(shape=>shape.enabled&&!shape.isTrigger&&shape.gameObject.activeInHierarchy).ToArray();if(boxes.Length!=2||boxes.Any(shape=>shape is not BoxCollider))throw new InvalidOperationException("Every original complete live NWH solid box must remain present.");row.boxes=boxes.Length;var mesh=new Mesh();
                try
                {
                    foreach(var skin in actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(skin=>skin.sharedMesh!=null))
                    {
                        skin.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(skin.transform.TransformPoint).ToArray();var indices=skin.sharedMesh.triangles;row.triangles+=indices.Length/3;
                        foreach(var box in boxes.Cast<BoxCollider>())
                        {
                            var local=vertices.Select(point=>box.transform.InverseTransformPoint(point)-box.center).ToArray();var half=box.size*.5f;
                            for(int i=0;i<indices.Length;i+=3)
                            {
                                var a=local[indices[i]];var b=local[indices[i+1]];var c=local[indices[i+2]];if(SeparatedByBoxAxes(a,b,c,half)||!ActualTriangleBoxSat(a,b,c,half,out _))continue;row.satPairs++;
                                if(row.firstIntersections.Count<6)row.firstIntersections.Add(new SavedTriangleGeometry{box=AnimationUtility.CalculateTransformPath(box.transform,truck.transform),skin=AnimationUtility.CalculateTransformPath(skin.transform,actor.motor.transform),triangle=i/3,worldA=vertices[indices[i]],worldB=vertices[indices[i+1]],worldC=vertices[indices[i+2]],boxLocalA=a,boxLocalB=b,boxLocalC=c,boxCenter=box.center,boxSize=box.size,actualBoxLocalToWorld=box.transform.localToWorldMatrix});
                            }
                        }
                    }
                    var seating=MeasureActualDeckSeating(truck,frame.supportPresentation,frame.ReadPose().rotation*Vector3.up,"ACTUAL_NATIVE_PLAYMODE_ORIGINAL_PARTICIPANT");row.wheels=seating.wheels.Length;row.allActualWheelsFound=seating.wheels.All(wheel=>wheel.actualVisualFound&&wheel.deckFaceFound);row.maximumActualWheelGap=seating.wheels.Max(wheel=>Mathf.Abs(wheel.actualGap));
                    return row;
                }
                finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        // V320 proves root localPosition/localRotation are ordinary WORLD
        // driving poses when this NWH body is unparented; the cab also has its
        // original dynamic suspension. Freeze authored dimensions/config, not
        // motion. Actual finite world edge dimensions remain an extra gate.
        static string FixedOriginalNwhGeometry(DragonRidersMatch match)=>FixedOriginalNwhGeometry(match.trucks);
        static string FixedOriginalNwhGeometry(LWS.TruckSports.SportsEventTruck[] trucks)=>string.Join("|",trucks.Select(truck=>truck.GetEntityId()+":"+truck.transform.localScale.ToString("R")+":"+string.Join(";",truck.GetComponentsInChildren<Rigidbody>(true).Select(body=>body.GetEntityId()+":"+body.mass.ToString("R")))+":"+string.Join(";",truck.GetComponentsInChildren<BoxCollider>(true).Select(box=>box.GetEntityId()+":"+box.center.ToString("R")+":"+box.size.ToString("R")+":"+box.transform.localScale.ToString("R")+":"+box.isTrigger+":"+(box.sharedMaterial!=null?box.sharedMaterial.GetEntityId().ToString():"null")))));
        static bool FiniteWorldBoxDimensionsAgree(FixedNwhField[] original,FixedNwhField[] actual,out float maximumDrift)
        {
            var before=original.Where(field=>field.path.EndsWith("/actualFiniteWorldBoxSideLengths",StringComparison.Ordinal)).ToDictionary(field=>field.path);var after=actual.Where(field=>field.path.EndsWith("/actualFiniteWorldBoxSideLengths",StringComparison.Ordinal)).ToArray();maximumDrift=0;
            if(before.Count==0||before.Count!=after.Length||after.Any(field=>!before.ContainsKey(field.path)))return false;
            foreach(var field in after)for(int i=0;i<3;i++){float difference=Mathf.Abs(field.values[i]-before[field.path].values[i]);if(!float.IsFinite(difference))return false;maximumDrift=Mathf.Max(maximumDrift,difference);}
            return maximumDrift<=.00001f;
        }
        static FixedNwhField[] CaptureFixedNwhFields(DragonRidersMatch match)
        {
            return match!=null?CaptureFixedNwhFields(match.trucks):Array.Empty<FixedNwhField>();
        }
        static FixedNwhField[] CaptureFixedNwhFields(LWS.TruckSports.SportsEventTruck[] trucks)
        {
            var fields=new List<FixedNwhField>();
            void Add(string path,params float[] values)=>fields.Add(new FixedNwhField{path=path,values=values,floatBits=string.Join(" ",values.Select(value=>BitConverter.ToInt32(BitConverter.GetBytes(value),0).ToString("X8")))});
            void Point(string path,Vector3 point)=>Add(path,point.x,point.y,point.z);
            foreach(var truck in trucks)
            {
                string owner=truck.GetEntityId()+"/"+truck.name;Point(owner+"/authoredRootLocalScale",truck.transform.localScale);Point(owner+"/representedRootLossyScale",truck.transform.lossyScale);Add(owner+"/originalBodyMass",truck.body.mass);
                foreach(var box in truck.GetComponentsInChildren<BoxCollider>(true))
                {
                    string path=owner+"/"+AnimationUtility.CalculateTransformPath(box.transform,truck.transform)+"/"+box.GetEntityId();Point(path+"/center",box.center);Point(path+"/size",box.size);Point(path+"/localPosition",box.transform.localPosition);Point(path+"/localScale",box.transform.localScale);var q=box.transform.localRotation;Add(path+"/localRotation",q.x,q.y,q.z,q.w);
                    var matrix=box.transform.localToWorldMatrix;var lengths=new Vector3(((Vector3)matrix.GetColumn(0)).magnitude,((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude);Point(path+"/actualWorldBasisLengths",lengths);Point(path+"/actualFiniteWorldBoxSideLengths",Vector3.Scale(box.size,lengths));
                }
            }
            return fields.ToArray();
        }
        public static IDisposable BeginTransientFireMountedSizeFromNativeEvidence(DragonRidersMatch match,string evidencePath)
        {
            if(!Application.isPlaying||EditorApplication.isCompiling||match==null||!File.Exists(evidencePath))throw new InvalidOperationException("Actual native PlayMode, saved match and explicit positive mounted-size evidence required.");
            var report=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(evidencePath));var proof=report.profiles.Single(row=>row.species=="dragon-fire"&&row.profile=="EXISTING_SAVED_MOUNTED_CONTROLLER_NO_NEW_CURVE_DERIVATIVES");
            if(!report.intervalControlsPassed||!report.sourceBytesExact||!report.sceneBytesExact||!report.nwhExact||proof.status!="DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED_REQUIRES_CAB_RUNTIME_WORLD_AND_EXPLICIT_VARIANT"||!proof.sourceMotionUnchanged||!proof.hasFiniteUniformSize||!proof.exactOriginalBoxAcceptance||!proof.originalFootprintFits||proof.actualPoses<537||proof.invalidAnatomicalPoses!=0||proof.candidateSatPairs!=0||proof.candidateWheelContactPoses!=proof.actualPoses||proof.maximumActualWheelGap>.03f||proof.actualCandidateSkinScaleError>=.001f||Mathf.Abs(proof.roundedCandidateFactor-1.87f)>.000001f||proof.sourceFiles.Count==0||proof.sourceFiles.Any(row=>!File.Exists(row.path)||!string.Equals(Hash(row.path),row.sha256,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Only the actual complete preserved original Fire1.87 native geometry proof may authorize this unsaved runtime candidate; no guessed factor or stale source proof.");
            return new MountedSizeLease(match,proof,evidencePath);
        }
        public static string ObserveTransientFireMountedSize(IDisposable lease)=>lease is MountedSizeLease actual?actual.Observe():throw new InvalidOperationException("Actual native transient Fire size lease required.");
        public static string ObserveTransientFireMountedGeometry(IDisposable lease,int owner,string phase)=>lease is MountedSizeLease actual?actual.ObserveGeometry(owner,phase):throw new InvalidOperationException("Actual native transient Fire size lease required.");
        static void ApplyQualifiedStrictFireMaterialProof(DragonRiderSaddle frame,MountedSizeProfile proof)
        {
            var points=proof.strictPatchProof;if(points==null||points.Length!=5||points.Any(point=>point.vertexInfluences==null||point.vertexInfluences.Length!=3||point.mesh!=AssetDatabase.GetAssetPath(frame.surfaceRenderer.sharedMesh)||point.torsoSkinWeight<=.5f||point.barycentric.x<0||point.barycentric.y<0||point.barycentric.z<0||point.upwardNormal<.1f||point.cpuSkinningRmse>=.0001f))throw new InvalidOperationException("Exact positively proven five strict torso material points on the original source skin required.");
            var anchors=points.Select(point=>new DragonRiderSurfaceAnchor{a=point.vertexInfluences[0].vertex,b=point.vertexInfluences[1].vertex,c=point.vertexInfluences[2].vertex,barycentric=point.barycentric}).ToArray();frame.ReleaseGeometrySnapshot();frame.surfaceVertexA=anchors[0].a;frame.surfaceVertexB=anchors[0].b;frame.surfaceVertexC=anchors[0].c;frame.surfaceBarycentric=anchors[0].barycentric;frame.surfacePatchAnchors=anchors;frame.Validate(frame.actor);
        }
        public static IDisposable BeginTransientFireHeadLeverSizeFromNativeEvidence(DragonRidersMatch match,string evidencePath)
        {
            if(!Application.isPlaying||EditorApplication.isCompiling||match==null||!File.Exists(evidencePath))throw new InvalidOperationException("Actual native PlayMode and complete V346 Fire strict-torso head-lever geometry evidence required.");
            var report=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(evidencePath));var proof=report.profiles.Single(row=>row.species=="dragon-fire"&&row.strictMaterialPatch&&row.provenFireHeadLeverRecipe);
            if(!report.intervalControlsPassed||!report.sourceBytesExact||!report.sceneBytesExact||!report.nwhExact||proof.status!="DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED_REQUIRES_CAB_RUNTIME_WORLD_AND_EXPLICIT_VARIANT"||proof.derivedNeckPitch!=60||proof.derivedHeadPitch!= -15||!proof.precisePlanning||proof.unsafeExpandedPlanningMisses!=0||!proof.sourceMotionUnchanged||!proof.hasFiniteUniformSize||!proof.exactOriginalBoxAcceptance||!proof.originalFootprintFits||proof.actualPoses!=537||proof.invalidAnatomicalPoses!=0||proof.candidateSatPairs!=0||proof.candidateWheelContactPoses!=537||proof.maximumActualWheelGap>.03f||proof.actualCandidateSkinScaleError>=.001f||Mathf.Abs(proof.roundedCandidateFactor-1.67f)>.000001f||proof.sourceFiles.Count==0||proof.sourceFiles.Any(row=>!File.Exists(row.path)||!string.Equals(Hash(row.path),row.sha256,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Exact complete unchanged V346 Fire60/-15strict1.67native geometry proof required; no guessed recipe or factor.");
            string folder=Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(evidencePath).FullName).FullName).FullName).FullName;var xml=new System.Xml.XmlDocument();xml.Load(Path.Combine(folder,"tests.xml"));var tests=xml.SelectNodes("//test-case").Cast<System.Xml.XmlNode>().Where(node=>node.Attributes["fullname"].Value=="DragonRiderProvenHeadSizeTests.AnalyticallyDerivedFireNeck60HeadMinus15RetainsEvery537OriginalActionLayerAndWholeNwhGeometryGate").ToArray();if(tests.Length!=1||tests[0].Attributes["result"].Value!="Passed")throw new InvalidOperationException("Actual full native Fire head-lever geometry test must pass before any transient runtime cab attempt.");
            return new MountedSizeLease(match,proof,evidencePath,true,RequireProvenActualFireHeadLever(proof.headLeverEvidence));
        }
        public static IDisposable BeginTransientFireNormalFamilySizeFromNativeEvidence(DragonRidersMatch match,string evidencePath)
        {
            if(!Application.isPlaying||EditorApplication.isCompiling||match==null||!File.Exists(evidencePath))throw new InvalidOperationException("Actual native PlayMode, saved match and explicit positive V325 family geometry evidence required.");
            var report=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(evidencePath));var proof=report.profiles.Single(row=>row.species=="dragon-fire"&&row.profile=="V279_FIRE_NORMAL_FAMILY_HALF_FLAP_MEASURED_STRICT_TORSO_REAR_MINUS_POINT15_RADIUS_POINT065");
            if(!report.intervalControlsPassed||!report.sourceBytesExact||!report.sceneBytesExact||!report.nwhExact||proof.status!="DERIVED_SINGLE_OWNED_SIZE_NATIVE_GEOMETRY_PASSED_REQUIRES_CAB_RUNTIME_WORLD_AND_EXPLICIT_VARIANT"||!proof.precisePlanning||!proof.strictMaterialPatch||proof.unsafeExpandedPlanningMisses!=0||!proof.sourceMotionUnchanged||!proof.hasFiniteUniformSize||!proof.exactOriginalBoxAcceptance||!proof.originalFootprintFits||proof.actualPoses!=537||proof.invalidAnatomicalPoses!=0||proof.candidateSatPairs!=0||proof.candidateWheelContactPoses!=537||proof.maximumActualWheelGap>.03f||proof.actualCandidateSkinScaleError>=.001f||Mathf.Abs(proof.roundedCandidateFactor-1.67f)>.000001f||proof.sourceFiles.Count==0||proof.sourceFiles.Any(row=>!File.Exists(row.path)||!string.Equals(Hash(row.path),row.sha256,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Only actual complete preserved Fire normal-family strict-torso1.67native geometry proof may authorize this unsaved runtime candidate.");
            return new MountedSizeLease(match,proof,evidencePath,true);
        }
    }
}
#endif
