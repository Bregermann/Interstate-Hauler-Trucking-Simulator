using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiDemoPassengerPreparation
    {
        public const string AuditionPassenger="passenger-d9f5d0d1a6a6";
        [MenuItem("Truck Taxi/Passenger Factory Tools/Prepare Neutral Demo Audition")]
        public static void GenerateAudition()
        {
            var p=TruckTaxiPassengerDialogueAuthoring.AllPassengers().Single(x=>x.passengerId==AuditionPassenger);
            TruckTaxiPassengerFactoryBuilder.BuildMissing(p);
            // Explicit demo casting only. Never automatically cast the rest of the roster.
            p.voiceProfile.neutralReference=Path.Combine(TruckTaxiChatterboxSettings.instance.workHere,"voice_refs/neutralvoicesample.wav");
            p.voiceProfile.voiceProfileId="demo-neutral-original-al-lee";
            EditorUtility.SetDirty(p.voiceProfile); AssetDatabase.SaveAssets();
            TruckTaxiChatterboxQueue.Generate(new[]{p},true);
        }
        [MenuItem("Truck Taxi/Passenger Factory Tools/Prepare Shared Humanoid Library")]
        public static void PrepareHumanoidLibrary()
        {
            const string folder=TruckTaxiPassengerFactoryBuilder.Root+"/Animations/Humanoid";
            TruckTaxiPassengerDialogueAuthoring.EnsureFolder(folder);
            foreach(string sex in new[]{"Mans/business01_m","Womans/business01_f_highpoly"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UTS_FullPack/Models/People/Prefabs/"+sex+".prefab");
                var animator=prefab.GetComponentInChildren<Animator>();
                string source=AssetDatabase.GetAssetPath(animator.avatar);
                string target=folder+"/"+Path.GetFileName(source);
                if(!File.Exists(target)) AssetDatabase.CopyAsset(source,target);
                var importer=(ModelImporter)AssetImporter.GetAtPath(target);
                importer.animationType=ModelImporterAnimationType.Human; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(target);
                var avatar=model.GetComponent<Animator>().avatar;
                if(avatar==null || !avatar.isValid || !avatar.isHuman) throw new System.InvalidOperationException("Humanoid mapping requires review: "+target);
                Debug.Log("TAXI HUMANOID VERIFIED: "+target);
            }
            var shared=TruckTaxiPassengerFactoryBuilder.SharedAnimations();
            foreach(var binding in shared.animations)
            {
                string source=AssetDatabase.GetAssetPath(binding.clip),target=folder+"/"+Path.GetFileName(source);
                if(!File.Exists(target)) AssetDatabase.CopyAsset(source,target);
                var importer=(ModelImporter)AssetImporter.GetAtPath(target);
                if(importer.animationType!=ModelImporterAnimationType.Human)
                { importer.animationType=ModelImporterAnimationType.Human; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; importer.SaveAndReimport(); }
                binding.clip=AssetDatabase.LoadAllAssetsAtPath(target).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            }
            var controller=(UnityEditor.Animations.AnimatorController)shared.controller;
            foreach(var state in controller.layers[0].stateMachine.states)
            { var binding=shared.animations.First(b=>b.stateName==state.state.name); state.state.motion=binding.clip; }
            EditorUtility.SetDirty(shared); AssetDatabase.SaveAssets();
            foreach(var p in TruckTaxiPassengerDialogueAuthoring.AllPassengers()) TruckTaxiPassengerFactoryBuilder.BuildPrefab(p);
            TruckTaxiPassengerFactoryBuilder.WriteReports();
        }
    }
}
