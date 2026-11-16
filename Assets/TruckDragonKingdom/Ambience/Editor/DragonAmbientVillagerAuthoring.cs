#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Ambience
{
    // Taxi/UTS already supplies the real Generic avatar, Bip01 rig and exact
    // matching idle/walk animation. Preserve it, rather than retarget a human.
    public static class DragonAmbientVillagerAuthoring
    {
        const string Controller="Assets/UTS_FullPack/Models/People/Animations/Girl no Heel controller.controller";
        const string AnimationRoot="Assets/UTS_FullPack/Models/People/Animations/Girl no Heel/";
        static readonly string[] Profiles={"Assets/LWS/TruckTaxi/Prefabs/Pedestrians/Taxi_Girl_11.prefab","Assets/LWS/TruckTaxi/Prefabs/Pedestrians/Taxi_Girl_22.prefab"};
        [Serializable] sealed class ClipProof
        {
            public string state,clip;
            public int transformBindings,resolvedBindings;
            public float leftLegDegrees,rightLegDegrees;
        }
        [Serializable] sealed class ProfileProof
        {
            public string profile,avatar,controller;
            public bool validGenericAvatar;
            public bool originalPrefabAvatarPreserved;
            public string[] skinnedModels;
            public ClipProof[] clips;
            public string[] dependencies,sha256;
        }
        [Serializable] sealed class Receipt
        {
            public string scope="Actual original Taxi/UTS Generic rigs and same-rig source clips; stopped authoring samples prove bindings/articulation, not native living-world playback or human acceptance.";
            public ProfileProof[] profiles;
        }
        public static Animator SpawnOriginal(string profile,Transform parent,Vector3 position,Vector3 focus)
        {
            if(!Profiles.Contains(profile))throw new InvalidOperationException("Only the audited original Taxi profiles are authorized here.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(profile);if(prefab==null)throw new InvalidOperationException("Actual original Taxi prefab missing: "+profile);
            // Null means retain its exact originally configured controller.
            var animator=SportsAudienceAuthoring.Spawn(prefab,parent,position,focus,null,1);
            ValidateRig(animator,profile,false);return animator;
        }
        // Parent may execute this narrow preflight before another full compose.
        // Install also invokes it before habitat searches/NavMesh mutations.
        public static void ValidateOriginalProfiles()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Explicit stopped authoring only.");
            var proofs=new List<ProfileProof>();var failures=new List<string>();
            foreach(string profile in Profiles)
            {
                GameObject holder=null;
                try
                {
                    holder=new GameObject("Temporary exact Taxi rig audit"){hideFlags=HideFlags.HideAndDontSave};
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(profile);if(prefab==null)throw new InvalidOperationException("Actual Taxi profile missing.");
                    var animator=SportsAudienceAuthoring.Spawn(prefab,holder.transform,Vector3.zero,Vector3.forward,null,1);
                    proofs.Add(ValidateRig(animator,profile,true));
                }
                catch(Exception exception){failures.Add(profile+": "+exception.Message);}
                finally{if(holder!=null)UnityEngine.Object.DestroyImmediate(holder);}
            }
            string output=Output();File.WriteAllText(output,JsonUtility.ToJson(new Receipt{profiles=proofs.ToArray()},true)+"\n");
            if(failures.Count>0)throw new InvalidOperationException("Both original Generic villager profiles audited; no fake Humanoid fallback. "+string.Join("\n",failures)+" receipt="+output);
            Debug.Log("DRAGON_AMBIENT_ORIGINAL_GENERIC_RIGS_VERIFIED profiles="+proofs.Count+" receipt="+output+"; actual live scene playback pending.");
        }
        static ProfileProof ValidateRig(Animator animator,string profile,bool sample)
        {
            if(animator==null||animator.avatar==null||!animator.avatar.isValid||animator.avatar.isHuman||AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)!=Controller)
                throw new InvalidOperationException("The original valid Generic Taxi avatar and exact UTS no-heel controller must be preserved: "+profile);
            string avatar=AssetDatabase.GetAssetPath(animator.avatar);
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(profile)?.GetComponentInChildren<Animator>(true);
            if(original==null||original.avatar!=animator.avatar||original.runtimeAnimatorController!=animator.runtimeAnimatorController)
                throw new InvalidOperationException("The exact original prefab Avatar/controller references must remain unchanged; shared Generic avatars are valid.");
            var originalSkins=original.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var actualSkins=animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(originalSkins.Length==0||actualSkins.Length!=originalSkins.Length)throw new InvalidOperationException("Preserve all original skinned model/LOD bindings.");
            foreach(var skin in originalSkins)
            {
                string path=AnimationUtility.CalculateTransformPath(skin.transform,original.transform);
                var actual=actualSkins.SingleOrDefault(item=>AnimationUtility.CalculateTransformPath(item.transform,animator.transform)==path);
                if(actual==null||actual.sharedMesh!=skin.sharedMesh)throw new InvalidOperationException("Original skin mesh binding changed: "+path);
            }
            string[] models=originalSkins.Select(skin=>AssetDatabase.GetAssetPath(skin.sharedMesh)).Distinct().ToArray();
            foreach(string model in models.Concat(new[]{avatar}).Distinct())
                if(string.IsNullOrEmpty(model)||!(AssetImporter.GetAtPath(model) is ModelImporter importer)||importer.animationType!=ModelImporterAnimationType.Generic)
                    throw new InvalidOperationException("Actual original skin/Avatar model importer must be Generic: "+model);
            var controller=animator.runtimeAnimatorController as AnimatorController;
            if(controller==null||controller.layers.Length!=1)throw new InvalidOperationException("Original single-layer source controller required.");
            var states=controller.layers[0].stateMachine.states;var proofs=new List<ClipProof>();
            foreach(string stateName in new[]{"idle1","walk"})
            {
                var state=states.SingleOrDefault(item=>item.state.name==stateName).state;
                if(state==null||!(state.motion is AnimationClip clip)||clip.humanMotion||!clip.isLooping||AssetDatabase.GetAssetPath(clip)!=AnimationRoot+stateName+"_f.FBX")
                    throw new InvalidOperationException("Original compatible looping Generic "+stateName+" clip required.");
                var bindings=AnimationUtility.GetCurveBindings(clip).Where(binding=>binding.type==typeof(Transform)).ToArray();
                var unresolved=bindings.Where(binding=>!string.IsNullOrEmpty(binding.path)&&animator.transform.Find(binding.path)==null).Select(binding=>binding.path).Distinct().ToArray();
                if(bindings.Length<15||unresolved.Length>0)throw new InvalidOperationException("Actual Generic transform binding mismatch: "+stateName+" bindings="+bindings.Length+" missing="+string.Join(";",unresolved));
                var proof=new ClipProof{state=stateName,clip=AssetDatabase.GetAssetPath(clip),transformBindings=bindings.Length,resolvedBindings=bindings.Length};
                if(sample)
                {
                    var left=animator.GetComponentsInChildren<Transform>(true).SingleOrDefault(item=>item.name=="Bip01 L Thigh");
                    var right=animator.GetComponentsInChildren<Transform>(true).SingleOrDefault(item=>item.name=="Bip01 R Thigh");
                    if(left==null||right==null)throw new InvalidOperationException("Actual original paired Bip01 leg chains required.");
                    clip.SampleAnimation(animator.gameObject,0);Quaternion startL=left.localRotation,startR=right.localRotation;
                    for(int index=1;index<=24;index++)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*index/24f);
                        proof.leftLegDegrees=Mathf.Max(proof.leftLegDegrees,Quaternion.Angle(startL,left.localRotation));
                        proof.rightLegDegrees=Mathf.Max(proof.rightLegDegrees,Quaternion.Angle(startR,right.localRotation));
                    }
                    if(stateName=="walk"&&(proof.leftLegDegrees<5||proof.rightLegDegrees<5))throw new InvalidOperationException("Actual same-rig bilateral walking leg motion not demonstrated: "+profile+" left="+proof.leftLegDegrees+" right="+proof.rightLegDegrees);
                }
                proofs.Add(proof);
            }
            string[] dependencies=AssetDatabase.GetDependencies(profile,true).Concat(models).Concat(new[]{profile,avatar,Controller,AnimationRoot+"walk_f.FBX",AnimationRoot+"idle1_f.FBX"}).Distinct().Where(File.Exists).OrderBy(path=>path,StringComparer.Ordinal).ToArray();
            return new ProfileProof{profile=profile,avatar=avatar,controller=Controller,validGenericAvatar=true,originalPrefabAvatarPreserved=true,skinnedModels=models,clips=proofs.ToArray(),dependencies=dependencies,sha256=dependencies.Select(Hash).ToArray()};
        }
        static string Hash(string path){using(var sha=SHA256.Create())using(var source=File.OpenRead(path))return string.Concat(sha.ComputeHash(source).Select(value=>value.ToString("x2")));}
        static string Output()
        {
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
            string path=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom","AmbienceArt","original-villager-rig-bindings.json"));Directory.CreateDirectory(Path.GetDirectoryName(path));return path;
        }
    }
}
#endif
