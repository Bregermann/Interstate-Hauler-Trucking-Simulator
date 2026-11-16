#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        static string FireMountedActionName(DragonAnimationAction action)=>action switch
        {
            DragonAnimationAction.TurnLeft=>"Mounted Turn Left",
            DragonAnimationAction.TurnRight=>"Mounted Turn Right",
            DragonAnimationAction.Land=>"Mounted Land",
            DragonAnimationAction.Breath=>"Mounted Breath",
            _=>throw new InvalidOperationException("Explicit selected Fire carrier action required.")
        };
        static void ConfigureFireMountedActions(DragonCharacter actor,DragonSpeciesDefinition species,AnimatorOverrideController controller)
        {
            if(species.id!="dragon-fire"||!species.TryGetAnimation(DragonAnimationAction.Fly,out var forward))throw new InvalidOperationException("Actual configured Fire forward-flight reference required.");
            var selected=new[]{DragonAnimationAction.TurnLeft,DragonAnimationAction.TurnRight,DragonAnimationAction.Land,DragonAnimationAction.Breath};
            var mappings=selected.ToDictionary(action=>action,action=>{if(!species.TryGetAnimation(action,out var mapping))throw new InvalidOperationException("Actual original Fire "+action+" source required.");return mapping;});
            var sources=mappings.Values.Select(mapping=>AssetDatabase.GetAssetPath(mapping.clip)).Concat(new[]{AssetDatabase.GetAssetPath(forward.clip)}).Distinct().ToDictionary(path=>path,Hash);
            bool changed=false;
            foreach(var action in selected)
            {
                var source=mappings[action].clip;
                // Same actual-rig four-joint retarget and independently tested
                // connected world-aim recipe as Inferno. Preserve the existing
                // accepted Hover/Takeoff mappings, all original wing/root/limb
                // channels and the original Fire7/NWH physical dimensions.
                var expected=CreateMountedSupportAction(species,source,forward.clip,action,out var receipt);var legacy=UnityEngine.Object.Instantiate(expected);ApplySelectedForwardTailRoot(species,source,forward.clip,expected,receipt);var output=RetainExactCarrierClip(FireHoverFolder+"/"+FireMountedActionName(action)+".anim",expected,legacy);
                var previous=controller[source];
                if(previous!=null&&previous!=source&&previous!=output)throw new InvalidOperationException("Preserve alternate designer Fire mounted "+action+" override.");
                if(previous!=output){controller[source]=output;changed=true;}
                RetainMappedWingObservation(actor,source,output);
                receipt.output=AssetDatabase.GetAssetPath(output);receipt.sourceBytesUnchanged=sources.All(pair=>Hash(pair.Key)==pair.Value);
                if(!receipt.sourceBytesUnchanged)throw new InvalidOperationException("Fire selected mounted actions modified an original source.");
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
                if(at>=0&&at+1<args.Length){string directory=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"mounted-fire-"+action.ToString().ToLowerInvariant()+".json"),JsonUtility.ToJson(receipt,true));}
            }
            if(changed){EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);}
            actor.motor.animator.runtimeAnimatorController=controller;
        }
    }
}
#endif
