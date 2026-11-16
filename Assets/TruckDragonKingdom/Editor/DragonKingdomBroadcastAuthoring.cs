#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CompassNavigatorPro;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Bind the existing collection camera authority to the new actual roster.
    // No camera algorithm, participant simulation or legacy scene migration.
    public static class DragonKingdomBroadcastAuthoring
    {
        public static bool Install(SportsEventSession session,SportsEventTruck[] trucks,SportsEventCamera camera)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||session==null||camera==null||trucks==null||trucks.Length!=4||trucks.Any(truck=>truck==null||truck.session!=session))
                throw new InvalidOperationException("Explicit stopped four-crew broadcast composition required.");
            var framing=camera.GetComponent<SportsBroadcastFraming>();
            if(framing!=null)
            {
                if(framing.session!=session||framing.cameraRig!=camera||!framing.trucks.SequenceEqual(trucks)||camera.broadcastFraming!=framing||framing.GetComponent<SportsBroadcastGuidance>()==null)
                    throw new InvalidOperationException("Existing designer broadcast bindings need an explicit reviewed repair.");
                return false;
            }
            var profile=AssetDatabase.LoadAssetAtPath<SportsBroadcastProfile>("Assets/TruckSports/Settings/Keepaway broadcast framing.asset");
            var compassPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckSports/Resources/Broadcast objective compass.prefab");
            if(profile==null||compassPrefab==null)throw new InvalidOperationException("Existing collection Cinemachine profile and Compass presentation required.");
            framing=camera.gameObject.AddComponent<SportsBroadcastFraming>();framing.session=session;framing.cameraRig=camera;framing.trucks=trucks;
            framing.sharedDefaults=profile;framing.useSharedDefaults=true;framing.useOrthographic=false;framing.authoringVersion=7;camera.broadcastFraming=framing;
            var fit=camera.GetComponent<SportsCameraFit>();if(fit!=null){fit.broadcastFraming=framing;EditorUtility.SetDirty(fit);}
            var guide=camera.gameObject.AddComponent<SportsBroadcastGuidance>();guide.framing=framing;
            var root=(GameObject)PrefabUtility.InstantiatePrefab(compassPrefab);root.name="Kingdom shared action guidance";root.transform.SetParent(session.transform,false);root.SetActive(false);
            guide.compass=root.GetComponent<SportsBroadcastCompass>();guide.targets=new CompassProPOI[trucks.Length+1];
            for(int i=0;i<guide.targets.Length;i++)
            {
                var marker=new GameObject(i==0?"Kingdom broadcast objective":"Kingdom broadcast participant "+i);marker.transform.SetParent(session.transform,false);marker.SetActive(false);
                var poi=marker.AddComponent<CompassProPOI>();poi.canBeVisited=poi.hideWhenVisited=poi.dontDestroyOnLoad=poi.showOnScreenIndicator=poi.playAudioClipWhenVisited=false;
                poi.showOffScreenIndicator=poi.ignoreAreaOfInterest=true;poi.radius=0;poi.compassGroupMask=1<<3;
                poi.iconNonVisited=i==0?AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"):AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png");guide.targets[i]=poi;
            }
            guide.compass.permittedTargets=guide.targets;
            if(PrefabUtility.IsPartOfPrefabInstance(guide.compass))PrefabUtility.RecordPrefabInstancePropertyModifications(guide.compass);
            EditorUtility.SetDirty(camera);EditorUtility.SetDirty(framing);EditorUtility.SetDirty(guide);return true;
        }

        [Serializable]sealed class Fact {public string scene,before,after;public bool changed,physicsPreserved;}
        [Serializable]sealed class Receipt {public string scope="EXPLICIT_DRAGON_SCENE_CAMERA_BINDING_NOT_RUNTIME_ACCEPTANCE";public Fact[] scenes;}
        static string Hash(string path){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
        static string PhysicsState(Scene scene)=>string.Join("\n",scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Where(component=>component is Rigidbody||component is Collider).Select(component=>component.GetEntityId()+":"+EditorJsonUtility.ToJson(component)).OrderBy(value=>value,StringComparer.Ordinal));
        public static void UpgradeSavedModes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var facts=new List<Fact>();
            foreach(string mode in new[]{"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonEscape","TruckDragonRiders"})
            {
                string path="Assets/"+mode+"/Scenes/"+mode+".unity";if(!File.Exists(path))continue;
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var session=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                var driving=session.GetComponent<SportsDrivingViews>();var trucks=driving.rally.trucks;string before=Hash(path),physics=PhysicsState(scene);
                bool changed=Install(session,trucks,driving.rally.broadcast);
                if(physics!=PhysicsState(scene))throw new InvalidOperationException("Camera binding changed original physical component data: "+mode);
                if(changed){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Camera scene did not save: "+mode);}
                string after=Hash(path);if(!changed&&before!=after)throw new InvalidOperationException("Repeated camera binding rewrote existing scene: "+mode);
                facts.Add(new Fact{scene=path,before=before,after=after,changed=changed,physicsPreserved=true});
            }
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Explicit unique dragon output directory required.");
            string output=Path.Combine(args[at+1],"BroadcastBindings");Directory.CreateDirectory(output);string receipt=Path.Combine(output,"saved-bindings.json");if(File.Exists(receipt))throw new IOException("Preserve prior broadcast binding receipt.");
            File.WriteAllText(receipt,JsonUtility.ToJson(new Receipt{scenes=facts.ToArray()},true));AssetDatabase.SaveAssets();
        }
    }
}
#endif
