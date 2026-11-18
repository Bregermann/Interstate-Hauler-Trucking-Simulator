using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    public static class RocketFlightPresentationAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active rocket parachutes and impact presentation")]
        public static void UpgradeActiveRocketFlightPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/Trailerblazers.unity") throw new InvalidOperationException("Open the saved Trailerblazers scene explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TrailerblazersMatch>(true)).Single();
            var canopyMesh = Require<Mesh>("Assets/TruckBaseJumping/Materials/Parachute canopy.asset");
            var stripe = Require<Material>("Assets/TruckBaseJumping/Materials/Canopy participant 0.mat");
            var ivory = Require<Material>("Assets/TruckBaseJumping/Materials/Canopy ivory.mat");
            var crashSource = Require<GameObject>("Assets/TruckBaseJumping/Prefabs/Crash blast.prefab");
            var airBrake = Require<AudioClip>("Assets/NWH/Vehicle Physics 2/Sound/Brakes/Air Brake - Strong.wav");
            match.flightPresentation = new RocketFlightPresentation[match.trucks.Length];
            for (int slot = 0; slot < match.trucks.Length; slot++)
            {
                var truck = match.trucks[slot];
                var presentation = truck.GetComponent<RocketFlightPresentation>();
                if (presentation == null) presentation = truck.gameObject.AddComponent<RocketFlightPresentation>();
                presentation.Match = match; presentation.Slot = slot;
                if (presentation.Canopy == null)
                {
                    var go = new GameObject("Existing domed striped recovery parachute", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(match.transform, false);
                    go.GetComponent<MeshFilter>().sharedMesh = canopyMesh;
                    var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterials = new[] { stripe, ivory };
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    presentation.Canopy = go.transform;
                }
                if (presentation.Harness == null || presentation.Harness.Length != 4)
                {
                    presentation.Harness = new LineRenderer[4];
                    for (int n = 0; n < 4; n++)
                    {
                        var line = new GameObject("Recovery parachute suspension line " + slot + "/" + n).AddComponent<LineRenderer>();
                        line.transform.SetParent(match.transform, false); line.useWorldSpace = true; line.positionCount = 2;
                        line.startWidth = line.endWidth = .045f; line.sharedMaterial = ivory; line.enabled = false;
                        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        presentation.Harness[n] = line;
                    }
                }
                var bounds = HitchAuthoring.Bounds(truck.gameObject);
                presentation.HarnessAnchors = new Vector3[4];
                for (int n = 0; n < 4; n++) presentation.HarnessAnchors[n] = new Vector3(bounds.center.x + (n % 2 == 0 ? -1 : 1) * bounds.extents.x * .7f, bounds.max.y * .85f, bounds.center.z + (n < 2 ? -1 : 1) * bounds.extents.z * .4f);
                if (presentation.Crash == null)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(crashSource, scene);
                    go.name = "Existing pooled rocket hard-impact blast " + slot; go.transform.SetParent(match.transform, true); go.transform.localScale *= 2;
                    presentation.Crash = go.GetComponent<ParticleSystem>();
                    foreach (var particles in go.GetComponentsInChildren<ParticleSystem>(true)) { var main = particles.main; main.playOnAwake = false; main.stopAction = ParticleSystemStopAction.None; }
                }
                foreach (var particles in presentation.Crash.GetComponentsInChildren<ParticleSystem>(true)) { var main = particles.main; main.playOnAwake = false; main.stopAction = ParticleSystemStopAction.None; }
                if (presentation.OpeningSound == null)
                {
                    var source = new GameObject("Installed NWH parachute air release " + slot).AddComponent<AudioSource>();
                    source.transform.SetParent(truck.transform, false); source.clip = airBrake; source.playOnAwake = false;
                    source.spatialBlend = 1; source.dopplerLevel = 0; source.minDistance = 12; source.maxDistance = 180;
                    presentation.OpeningSound = source;
                }
                foreach (var body in truck.GetComponentsInChildren<Rigidbody>(true))
                {
                    var relay = body.GetComponent<RocketImpactRelay>();
                    if (relay == null) relay = body.gameObject.AddComponent<RocketImpactRelay>();
                    relay.Presentation = presentation; EditorUtility.SetDirty(relay);
                }
                presentation.ResetForRound(match.setup.players[slot].color);
                match.flightPresentation[slot] = presentation; EditorUtility.SetDirty(presentation);
            }
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static T Require<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Existing owned source missing: " + path);
    }
}
