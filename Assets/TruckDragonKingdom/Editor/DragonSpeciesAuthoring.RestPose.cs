#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    public static partial class DragonSpeciesAuthoring
    {
        struct JointPose
        {
            public Vector3 position;public Quaternion rotation;
            public JointPose(Transform bone){position=bone.localPosition;rotation=bone.localRotation;}
            public void Apply(Transform bone){bone.SetLocalPositionAndRotation(position,rotation);}
        }
        [Serializable] sealed class RestUpgradeReceipt
        {
            public string scope,species,source,sourceSha256;
            public string[] selectedPaths,beforeHashes,afterHashes;
            public int actualJoints,blendShapes;
            public float headHeightBefore,headHeightRest,pelvisHeightBefore,pelvisHeightRest,wingSpanBefore,wingSpanRest;
            public bool unrelatedAuthoredClipsPreserved;
        }

        // Deliberately separate from catalog creation and ordinary play/build.
        // Existing action GUIDs/controllers remain valid; only these selected
        // project-owned derivative clips and their mapping notes are changed.
        public static DragonSpeciesCatalog UpgradeInfernoSleepWake()=>UpgradeGroundRestActions("dragon-inferno");
        public static DragonSpeciesCatalog UpgradeGroundRestActions(string speciesId)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Explicit stopped selected-action authoring required.");
            if(speciesId!="dragon-inferno")throw new InvalidOperationException("This reviewed anatomical rest authoring targets only the actual Inferno guardian; other rigs require their own visual review.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(CatalogPath);
            if(catalog==null||!catalog.TryGet(speciesId,out var species)||species.visualPrefab==null||!species.Supports(DragonSpeciesRole.Ground))throw new InvalidOperationException("Existing actual ground guardian catalog required.");
            string folder=DragonRoot+"Dragon Inferno Cute Series/FBX";
            var source=AssetDatabase.FindAssets(string.Empty,new[]{folder}).Select(AssetDatabase.GUIDToAssetPath).Where(path=>Path.GetFileNameWithoutExtension(path).EndsWith("@Idle",StringComparison.Ordinal)).SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().FirstOrDefault(clip=>!clip.name.StartsWith("__",StringComparison.Ordinal));
            if(source==null)throw new InvalidOperationException("Actual owned Inferno Idle reference clip required.");
            var selected=new[]{DragonAnimationAction.Sleep,DragonAnimationAction.Wake};var mappings=new DragonAnimationMapping[2];
            for(int i=0;i<selected.Length;i++)
            {
                if(!species.TryGetAnimation(selected[i],out mappings[i]))throw new InvalidOperationException("Existing selected mapping required: "+selected[i]);
                string expected=AuthoredRoot+"/"+speciesId+"/"+selected[i]+".anim";
                if(AssetDatabase.GetAssetPath(mappings[i].clip)!=expected)throw new InvalidOperationException("Refusing to replace a source/vendor or alternate designer clip: "+selected[i]);
            }
            var allPaths=Directory.GetFiles(AuthoredRoot,"*.anim",SearchOption.AllDirectories).Select(path=>path.Replace('\\','/')).ToArray();
            var before=allPaths.ToDictionary(path=>path,FileHash,StringComparer.Ordinal);
            var instance=UnityEngine.Object.Instantiate(species.visualPrefab);instance.hideFlags=HideFlags.HideAndDontSave;instance.SetActive(false);
            AnimationClip sleep=null,wake=null;
            try
            {
                if(!SupportsInfernoRestPose(instance))throw new InvalidOperationException("Actual Inferno pelvis, neck, wing and limb chains are required.");
                var bones=RestJoints(instance);source.SampleAnimation(instance,0);var names=NamedRig(instance);float floor=FootFloor(instance,names);
                var receipt=new RestUpgradeReceipt{scope="Explicit selected actual-rig Sleep/Wake generation only; rendered posture/contact/eyes/native playback not verified by authoring. No raw FBX/rig/material changes.",species=speciesId,source=AssetDatabase.GetAssetPath(source),sourceSha256=FileHash(AssetDatabase.GetAssetPath(source)),actualJoints=bones.Length,blendShapes=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(skin=>skin.sharedMesh==null?0:skin.sharedMesh.blendShapeCount),headHeightBefore=Height(instance,names["RigHead"].position)-floor,pelvisHeightBefore=Height(instance,names["RigPelvis"].position)-floor,wingSpanBefore=Vector3.Distance(names["RigLWing04"].position,names["RigRWing04"].position)};
                sleep=CreateInfernoRestClip(instance,source,DragonAnimationAction.Sleep,4,true);wake=CreateInfernoRestClip(instance,source,DragonAnimationAction.Wake,2.6f,false);
                sleep.SampleAnimation(instance,0);var rest=bones.ToDictionary(bone=>bone,bone=>new JointPose(bone));
                receipt.headHeightRest=Height(instance,names["RigHead"].position)-floor;receipt.pelvisHeightRest=Height(instance,names["RigPelvis"].position)-floor;receipt.wingSpanRest=Vector3.Distance(names["RigLWing04"].position,names["RigRWing04"].position);
                if(receipt.headHeightRest>=receipt.headHeightBefore*.55f||receipt.pelvisHeightRest>=receipt.pelvisHeightBefore*.8f||receipt.wingSpanRest>=receipt.wingSpanBefore*.8f)throw new InvalidOperationException("Actual rest landmark gate failed before saving: head "+receipt.headHeightBefore+"→"+receipt.headHeightRest+", pelvis "+receipt.pelvisHeightBefore+"→"+receipt.pelvisHeightRest+", wing span "+receipt.wingSpanBefore+"→"+receipt.wingSpanRest);
                wake.SampleAnimation(instance,0);
                foreach(var bone in bones)if(Quaternion.Angle(rest[bone].rotation,bone.localRotation)>.02f||Vector3.Distance(rest[bone].position,bone.localPosition)>.0001f)throw new InvalidOperationException("Wake must begin at the exact authored Sleep rest pose: "+bone.name);
                var clips=new[]{sleep,wake};receipt.selectedPaths=mappings.Select(mapping=>AssetDatabase.GetAssetPath(mapping.clip)).ToArray();receipt.beforeHashes=receipt.selectedPaths.Select(path=>before[path]).ToArray();
                for(int i=0;i<clips.Length;i++)
                {
                    clips[i].name=mappings[i].clip.name;EditorUtility.CopySerialized(clips[i],mappings[i].clip);EditorUtility.SetDirty(mappings[i].clip);mappings[i].loop=i==0;mappings[i].playbackSpeed=1;
                    mappings[i].sourceNote="LWS SELECTED ANATOMICAL REST V2: actual Inferno Idle reference, world-axis torso lowering, actual limb IK/head support, folded wing chains and subtle breathing; Wake begins at that exact rest pose. Painted eyes have no invented eyelid control; rendered verification pending.";
                }
                EditorUtility.SetDirty(catalog);foreach(var mapping in mappings)AssetDatabase.SaveAssetIfDirty(mapping.clip);AssetDatabase.SaveAssetIfDirty(catalog);receipt.afterHashes=receipt.selectedPaths.Select(FileHash).ToArray();
                receipt.unrelatedAuthoredClipsPreserved=before.Where(pair=>!receipt.selectedPaths.Contains(pair.Key)).All(pair=>FileHash(pair.Key)==pair.Value);
                if(!receipt.unrelatedAuthoredClipsPreserved)throw new InvalidOperationException("An unrelated authored action changed during selected rest upgrade.");
                const string report="Documentation/TruckSports/DragonKingdom/RestPoseUpgradeReceipt.json";Directory.CreateDirectory(Path.GetDirectoryName(report));File.WriteAllText(report,JsonUtility.ToJson(receipt,true));
                Debug.Log("INFERNO_SLEEP_WAKE_SELECTED_UPGRADE authored2 preservedOther="+receipt.unrelatedAuthoredClipsPreserved+" head="+receipt.headHeightBefore+"→"+receipt.headHeightRest+" wingSpan="+receipt.wingSpanBefore+"→"+receipt.wingSpanRest+" blendShapes="+receipt.blendShapes+" · native rendered review remains required.");return catalog;
            }
            finally{if(sleep!=null)UnityEngine.Object.DestroyImmediate(sleep);if(wake!=null)UnityEngine.Object.DestroyImmediate(wake);UnityEngine.Object.DestroyImmediate(instance);}
        }
        static string FileHash(string path){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
        static bool SupportsInfernoRestPose(GameObject model)
        {
            if(!model.name.StartsWith("Dragon Inferno",StringComparison.Ordinal))return false;
            var names=new HashSet<string>(model.GetComponentsInChildren<Transform>(true).Select(bone=>bone.name));
            return new[]{"RigPelvis","RigSpine1","RigSpine2","RigRibcage","RigNeck1","RigNeck2","RigNeck3","RigHead","RigJaw","RigLWing04","RigRWing04","RigLLegAnkle","RigRLegAnkle","RigLArmPalm","RigRArmPalm"}.All(names.Contains);
        }
        static Dictionary<string,Transform> NamedRig(GameObject model)=>model.GetComponentsInChildren<Transform>(true).Where(bone=>bone.name.StartsWith("Rig",StringComparison.Ordinal)&&!bone.name.EndsWith("Gizmo",StringComparison.Ordinal)).ToDictionary(bone=>bone.name,StringComparer.Ordinal);
        static Transform[] RestJoints(GameObject model)=>model.GetComponentsInChildren<Transform>(true).Single(bone=>bone.name=="RigPelvis").GetComponentsInChildren<Transform>(true).Where(bone=>!bone.name.EndsWith("Gizmo",StringComparison.Ordinal)).ToArray();
        static float Height(GameObject model,Vector3 worldPoint)=>Vector3.Dot(worldPoint-model.transform.position,model.transform.up);
        static float FootFloor(GameObject model,Dictionary<string,Transform> names)=>new[]{"RigLLegToes2","RigRLegToes2"}.Where(names.ContainsKey).Select(name=>Height(model,names[name].position)).DefaultIfEmpty(0).Min();
        static AnimationClip CreateInfernoRestClip(GameObject model,AnimationClip source,DragonAnimationAction action,float duration,bool loop)
        {
            var output=new AnimationClip{frameRate=30,legacy=false,wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever};var bindings=CopyRestReferenceCurves(output,source,duration);
            // Sampling an optimized FBX clip does not restore unkeyed joints.
            // Start each generation from the exact original persistent prefab
            // pose so prior temporary action authoring cannot accumulate.
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(DragonRoot+"Dragon Inferno Cute Series/Prefabs/Dragon Inferno.prefab");
            if(original==null)throw new InvalidOperationException("Original owned Inferno rig reference required.");
            var reference=NamedRig(original);foreach(var pair in NamedRig(model))if(reference.TryGetValue(pair.Key,out var originalBone))pair.Value.SetLocalPositionAndRotation(originalBone.localPosition,originalBone.localRotation);
            var bones=RestJoints(model);source.SampleAnimation(model,0);var neutral=bones.ToDictionary(bone=>bone,bone=>new JointPose(bone));var names=NamedRig(model);
            float floor=FootFloor(model,names),height=Height(model,names["RigHead"].position)-floor;
            if(!float.IsFinite(height)||height<.2f)throw new InvalidOperationException("Finite actual anatomical reference height required.");
            PoseInfernoRest(model,names,floor,height);var resting=bones.ToDictionary(bone=>bone,bone=>new JointPose(bone));
            var rotations=bones.ToDictionary(bone=>bone,bone=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()});var positions=bones.ToDictionary(bone=>bone,bone=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()});
            int count=Mathf.CeilToInt(duration*30);
            for(int index=0;index<=count;index++)
            {
                float t=index/(float)count,time=t*duration;
                foreach(var pair in neutral)pair.Value.Apply(pair.Key);source.SampleAnimation(model,t*source.length);
                var target=bones.ToDictionary(bone=>bone,bone=>new JointPose(bone));
                foreach(var bone in bones)
                {
                    float blend=action==DragonAnimationAction.Sleep?0:RestRise(bone.name,t);
                    bone.SetLocalPositionAndRotation(Vector3.Lerp(resting[bone].position,target[bone].position,blend),Quaternion.Slerp(resting[bone].rotation,target[bone].rotation,blend));
                }
                float breathing=Mathf.Sin(t*Mathf.PI*2)*(action==DragonAnimationAction.Sleep?1:1-Mathf.SmoothStep(0,1,t));
                names["RigPelvis"].position+=model.transform.up*(height*.0018f*breathing);
                names["RigRibcage"].rotation=Quaternion.AngleAxis(.35f*breathing,model.transform.right)*names["RigRibcage"].rotation;
                foreach(var bone in bones)
                {
                    var q=bone.localRotation;var p=bone.localPosition;var r=rotations[bone];var v=positions[bone];r[0].AddKey(time,q.x);r[1].AddKey(time,q.y);r[2].AddKey(time,q.z);r[3].AddKey(time,q.w);v[0].AddKey(time,p.x);v[1].AddKey(time,p.y);v[2].AddKey(time,p.z);
                }
            }
            foreach(var bone in bones)
            {
                string path=AnimationUtility.CalculateTransformPath(bone,model.transform);
                foreach(var binding in bindings.Where(binding=>binding.path==path&&binding.type==typeof(Transform)&&binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0))AnimationUtility.SetEditorCurve(output,binding,null);
                string[] axes={"x","y","z","w"};for(int index=0;index<4;index++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+axes[index]),rotations[bone][index]);
                for(int index=0;index<3;index++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalPosition."+axes[index]),positions[bone][index]);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(output);settings.loopTime=loop;settings.loopBlend=loop;AnimationUtility.SetAnimationClipSettings(output,settings);output.EnsureQuaternionContinuity();return output;
        }
        static float RestRise(string bone,float t)
        {
            float start=.15f,end=.9f;
            if(bone.StartsWith("RigNeck",StringComparison.Ordinal)||bone=="RigHead"||bone=="RigJaw"){start=.03f;end=.58f;}
            else if(bone.Contains("Wing")){start=.42f;end=1;}
            else if(bone.StartsWith("RigTail",StringComparison.Ordinal)){start=.25f;end=.95f;}
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,t));
        }
        static EditorCurveBinding[] CopyRestReferenceCurves(AnimationClip output,AnimationClip source,float duration)
        {
            var bindings=AnimationUtility.GetCurveBindings(source);if(bindings.Length==0)throw new InvalidOperationException("Actual source curves required.");float scale=duration/Mathf.Max(.001f,source.length);
            foreach(var binding in bindings){var curve=AnimationUtility.GetEditorCurve(source,binding);var keys=curve.keys;for(int index=0;index<keys.Length;index++){keys[index].time*=scale;keys[index].inTangent/=scale;keys[index].outTangent/=scale;}curve.keys=keys;AnimationUtility.SetEditorCurve(output,binding,curve);}return bindings;
        }
        static void PoseInfernoRest(GameObject model,Dictionary<string,Transform> bones,float floor,float height)
        {
            Vector3 up=model.transform.up,forward=model.transform.forward,right=model.transform.right,origin=model.transform.position;var pelvis=bones["RigPelvis"];
            var pelvisReference=model.transform.InverseTransformPoint(pelvis.position);pelvisReference.y=floor+height*.22f;pelvisReference.z-=height*.10f;pelvis.position=model.transform.TransformPoint(pelvisReference);
            Aim(pelvis,bones["RigSpine1"],forward+up*.12f);
            // Real chains bend on their actual longitudinal axes, unlike local
            // Euler-X offsets which mostly twist this export's -X bone axes.
            foreach(string side in new[]{"L","R"})
            {
                float sign=side=="L"?-1:1;
                Vector3 ankle=origin+up*(floor+height*.06f)+right*(sign*height*.24f)+forward*(pelvisReference.z-height*.13f);
                SolveTwoBone(bones["Rig"+side+"Leg1"],bones["Rig"+side+"Leg2"],bones["Rig"+side+"LegAnkle"],ankle,right*sign+forward*.2f);
                if(bones.TryGetValue("Rig"+side+"LegToes1",out var toes1)){Aim(bones["Rig"+side+"LegAnkle"],toes1,forward-up*.1f);if(bones.TryGetValue("Rig"+side+"LegToes2",out var toes2))Aim(toes1,toes2,forward);}
                Vector3 palm=origin+up*(floor+height*.075f)+right*(sign*height*.23f)+forward*(pelvisReference.z+height*.42f);
                SolveTwoBone(bones["Rig"+side+"Arm1"],bones["Rig"+side+"Arm2"],bones["Rig"+side+"ArmPalm"],palm,right*sign-up*.2f);
                AimAxis(bones["Rig"+side+"ArmPalm"],forward-up*.06f);
                Aim(bones["Rig"+side+"Wing01"],bones["Rig"+side+"Wing02"],-forward+right*(sign*.35f)-up*.2f);
                Aim(bones["Rig"+side+"Wing02"],bones["Rig"+side+"Wing03"],forward+right*(sign*.05f)-up*.3f);
                Aim(bones["Rig"+side+"Wing03"],bones["Rig"+side+"Wing04"],-forward+right*(sign*.2f)-up*.2f);
                AimAxis(bones["Rig"+side+"Wing04"],-forward+right*(sign*.3f)-up*.1f);
            }
            var head=bones["RigHead"];Vector3 headGoal=origin+up*(floor+height*.16f)+forward*(pelvisReference.z+height*.51f)+right*(height*.035f);
            var neck=new[]{bones["RigNeck1"],bones["RigNeck2"],bones["RigNeck3"]};
            for(int iteration=0;iteration<14;iteration++)for(int index=neck.Length-1;index>=0;index--)
            {var joint=neck[index];joint.rotation=Quaternion.FromToRotation(head.position-joint.position,headGoal-joint.position)*joint.rotation;}
            Aim(head,bones["RigJaw"],forward+right*.15f+up*.03f);bones["RigJaw"].rotation=Quaternion.AngleAxis(-8,right)*bones["RigJaw"].rotation;
            var tails=bones.Values.Where(bone=>bone.name.StartsWith("RigTail",StringComparison.Ordinal)).OrderBy(bone=>Depth(bone)).ToArray();
            for(int index=0;index<tails.Length;index++)
            {
                Vector3 direction=Quaternion.AngleAxis(12+index*32,up)*-forward;direction+=up*Mathf.Clamp((floor+height*.075f-Height(model,tails[index].position))*2,-.4f,.4f);AimAxis(tails[index],direction);
            }
            // Skin geometry, rather than a guessed renderer height on an
            // inactive prefab, establishes the lowest actual contact plane.
            float lowest=LowestSkin(model);float correction=floor+height*.008f-lowest;
            if(!float.IsFinite(correction)||Mathf.Abs(correction)>height*.35f)throw new InvalidOperationException("Rest skin support correction failed: lowest="+lowest+", floor="+floor+", height="+height+", correction="+correction+"; inspect actual pose before saving.");
            if(correction>0)pelvis.position+=up*correction;
        }
        static int Depth(Transform bone){int depth=0;while(bone.parent!=null){depth++;bone=bone.parent;}return depth;}
        static void Aim(Transform bone,Transform child,Vector3 direction){if(direction.sqrMagnitude>.000001f)bone.rotation=Quaternion.FromToRotation(child.position-bone.position,direction.normalized)*bone.rotation;}
        static void AimAxis(Transform bone,Vector3 direction){if(direction.sqrMagnitude>.000001f)bone.rotation=Quaternion.FromToRotation(bone.TransformDirection(Vector3.left),direction.normalized)*bone.rotation;}
        static void SolveTwoBone(Transform root,Transform middle,Transform end,Vector3 target,Vector3 pole)
        {
            float first=Vector3.Distance(root.position,middle.position),second=Vector3.Distance(middle.position,end.position);Vector3 delta=target-root.position;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(first-second)+.001f,first+second-.001f);Vector3 axis=delta.normalized,bend=Vector3.ProjectOnPlane(pole,axis).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(axis,Vector3.up).normalized;
            float along=(first*first-second*second+distance*distance)/(2*distance),across=Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
            Vector3 knee=root.position+axis*along+bend*across;Aim(root,middle,knee-root.position);Aim(middle,end,target-middle.position);
        }
        static float LowestSkin(GameObject model)
        {
            var mesh=new Mesh();float lowest=float.PositiveInfinity;
            try{foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)){if(skin.sharedMesh==null)continue;mesh.Clear();skin.BakeMesh(mesh);foreach(var point in mesh.vertices)lowest=Mathf.Min(lowest,Height(model,skin.transform.TransformPoint(point)));}}
            finally{UnityEngine.Object.DestroyImmediate(mesh);}return lowest;
        }
    }
}
#endif
