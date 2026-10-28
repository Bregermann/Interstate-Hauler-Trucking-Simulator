using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static class SportsImpossibleBullPresentationAuthoring
    {
        public static void InstallReadableYogaView()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending edits before explicit Yoga presentation repair.");
            var scene = EditorSceneManager.OpenScene("Assets/TruckYoga/Scenes/TruckYoga.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var session = roots.SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
            var framing = roots.SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Single(item => item.session == session);
            if (framing.sharedDefaults == null || AssetDatabase.GetAssetPath(framing.sharedDefaults) != "Assets/TruckSports/Settings/Keepaway broadcast framing.asset")
                throw new InvalidOperationException("Original new-scene profile required; customized Yoga camera retained.");
            var expected = JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(framing.sharedDefaults.settings));
            expected.minimumTruckHeight = .12f; expected.preferredTruckHeight = .15f;
            expected.elevation = 38; expected.azimuth = 0; expected.primaryWeight = 1;
            expected.maximumDistanceInTruckLengths = 8; expected.nearbyTruckLengths = 7;
            var prior = JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(expected));
            prior.minimumTruckHeight = .14f; prior.preferredTruckHeight = .18f;
            if (!framing.useSharedDefaults && JsonUtility.ToJson(framing.settings) != JsonUtility.ToJson(expected) &&
                JsonUtility.ToJson(framing.settings) != JsonUtility.ToJson(prior))
                throw new InvalidOperationException("Preserve customized Yoga framing values.");
            framing.settings = expected; framing.useSharedDefaults = false;
            EditorUtility.SetDirty(framing); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Could not save Yoga presentation repair.");
            Debug.Log("IMPOSSIBLE_YOGA_READABILITY_CONFIGURED closer lower equal-weight four-mat composition; original Cinemachine authority, physics, instructor and shared profile retained. Visual acceptance pending.");
        }

        public static void InstallReadableBullView()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending edits before explicit Bull presentation repair.");
            var scene = EditorSceneManager.OpenScene("Assets/TruckMechanicalBull/Scenes/TruckMechanicalBull.unity", OpenSceneMode.Single);
            var all = scene.GetRootGameObjects();
            var session = all.SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
            var framing = all.SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Single(item => item.session == session);
            if (framing.sharedDefaults == null || AssetDatabase.GetAssetPath(framing.sharedDefaults) != "Assets/TruckSports/Settings/Keepaway broadcast framing.asset")
                throw new InvalidOperationException("Original new-scene shared profile required; customized camera retained for review.");
            var expected = JsonUtility.FromJson<SportsBroadcastSettings>(JsonUtility.ToJson(framing.sharedDefaults.settings));
            expected.minimumTruckHeight = .14f; expected.preferredTruckHeight = .22f;
            expected.elevation = 38; expected.azimuth = -25;
            expected.maximumDistanceInTruckLengths = 8; expected.nearbyTruckLengths = 2;
            if (!framing.useSharedDefaults && JsonUtility.ToJson(framing.settings) != JsonUtility.ToJson(expected))
                throw new InvalidOperationException("Preserve customized per-sport framing values.");
            var sign = all.SelectMany(root => root.GetComponentsInChildren<TextMesh>(true)).Single(item => item.name == "Rodeo enamel sign");
            if (Vector3.Distance(sign.transform.position, new Vector3(0, 10, 31)) > .001f ||
                sign.text != "IRON LONGHORN\nRIDE THE IMPOSSIBLE" ||
                Quaternion.Angle(sign.transform.rotation, Quaternion.identity) > .02f && Quaternion.Angle(sign.transform.rotation, Quaternion.Euler(0, 180, 0)) > .02f)
                throw new InvalidOperationException("Customized rodeo sign retained for review.");
            framing.settings = expected; framing.useSharedDefaults = false;
            sign.transform.rotation = Quaternion.identity;
            EditorUtility.SetDirty(framing); EditorUtility.SetDirty(sign.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Could not save Bull presentation repair.");
            Debug.Log("IMPOSSIBLE_BULL_READABILITY_CONFIGURED .14minimum/.22preferred closer side view; original Cinemachine authority, physics, scoring and shared profile retained. Native visual acceptance pending.");
        }
    }
}
