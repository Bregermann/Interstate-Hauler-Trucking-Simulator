using System;
using System.Linq;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    // Adds the actual runtime-requested stations; never generates a course at
    // launch or changes the native rig/jack poses that passed the relay test.
    public static class HandoffVariantsAuthoring
    {
        const string RootName = "Authored Handoff extended and technical stations";
        const string BarrierArt = "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab";
        const string ConeArt = "Assets/RoadPropsPack/URP/Prefabs/TrafficCone_URP.prefab";
        const string SignArt = "Assets/RoadPropsPack/URP/Prefabs/RoadSign_URP.prefab";

        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Handoff stations and routes")]
        public static void UpgradeActiveHandoffStationsAndRoutes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/HitchHandoff.unity") throw new InvalidOperationException("Open the saved HitchHandoff scene explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HandoffMatch>(true)).Single();
            var existing = match.transform.Find(RootName);
            if (match.transform.Find(RootName + " (partial)") != null) throw new InvalidOperationException("A partial Handoff upgrade is retained; inspect it before retrying.");
            if (existing != null)
            {
                if (existing.Find("Extended four-leg relay") != null && existing.Find("Reverse-hitch receiving bays") != null && existing.Find("Obstacle relay production route") != null)
                { Debug.Log("HH10_HANDOFF_VARIANTS_PRESERVED"); return; }
                throw new InvalidOperationException("Partial/customized Handoff additions retained; inspect before explicit repair.");
            }
            foreach (string path in new[] { BarrierArt, ConeArt, SignArt }) if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new InvalidOperationException("Installed road presentation asset missing: " + path);
            var actors = match.trucks.Select(t => t.transform).Concat(match.trailers.Select(t => t.transform)).ToArray();
            var actorPoses = actors.Select(t => t.localToWorldMatrix).ToArray();
            var stands = match.trailers.Select(t => t.Module.module.trailerStand.transform).ToArray(); var jackPositions = stands.Select(t => t.localPosition).ToArray();
            var finishPoses = match.finishes.Select(t => t.localToWorldMatrix).ToArray();
            var font = SportsBroadcastAuthoring.Theme().headingFont;
            var lineMaterial = match.transferZones[0].GetComponent<Renderer>().sharedMaterial;
            var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckHitchSports/Materials/Hauling venue contact.asset");
            if(contact==null)throw new InvalidOperationException("Existing hauling venue contact material required.");
            var root = new GameObject(RootName + " (partial)").transform; root.SetParent(match.transform, false);
            root.gameObject.SetActive(false);
            var extended = ModeRoot(root, "Extended four-leg relay", 1 << 3);
            var reverse = ModeRoot(root, "Reverse-hitch receiving bays", 1 << 2);
            var obstacles = ModeRoot(root, "Obstacle relay production route", 1 << 1);
            // The original finish objects keep their identity and exact pose.
            // Parenting lets the existing mode-mask adapter hide their physical
            // surfaces only while the extended finish at+160m is in use.
            var ordinaryFinish = ModeRoot(root, "Original two-leg receiving surfaces", (1 << 0) | (1 << 1) | (1 << 2));
            foreach (var finish in match.finishes) finish.SetParent(ordinaryFinish, true);
            for (int team = 0; team < 2; team++)
            {
                var color = team == 0 ? new Color(.25f, .85f, 1) : new Color(1, .57f, .19f);
                var station = match.transferZones[team];
                for (int leg = 0; leg < 3; leg++)
                {
                    Vector3 position = station.position + Vector3.forward * (leg * 80);
                    if (leg > 0)
                    {
                        var pad = UnityEngine.Object.Instantiate(station.gameObject, extended); pad.name = "Extended team " + (team + 1) + " transfer station " + (leg + 1); pad.transform.position = position;
                    }
                    Bay(extended, font, position, "TEAM " + (team + 1) + " · HANDOFF " + (leg + 1) + "/3", color, lineMaterial);
                    Label(extended, font, "STOP · UNHITCH · CLEAR · PHYSICALLY RECOUPLE", position + new Vector3(-18, 4.5f, -10), color);
                }
                var finishClone = UnityEngine.Object.Instantiate(match.finishes[team].gameObject, extended); finishClone.name = "Extended team " + (team + 1) + " four-leg finish"; finishClone.transform.position = match.finishes[team].position + Vector3.forward * 160;
                Bay(extended, font, finishClone.transform.position, "TEAM " + (team + 1) + " · FOUR LEGS COMPLETE", color, lineMaterial);

                Vector3 bay = station.position;
                Bay(reverse, font, bay, "TEAM " + (team + 1) + " · REVERSE ENTRY ONLY", color, lineMaterial);
                Label(reverse, font, "TOW PAST THIS LINE · THEN BACK THE TRAILER", bay + Vector3.forward * 15 + new Vector3(-18, 4.5f, 0), color);
                Arrow(reverse, bay + Vector3.forward * 22 + Vector3.up * .12f, -Vector3.forward, lineMaterial);
                Arrow(reverse, bay + Vector3.forward * 5 + Vector3.up * .12f, -Vector3.forward, lineMaterial);
                Label(reverse, font, "STOP INSIDE BAY · ACTION TO RELEASE", bay + new Vector3(-18, 4.5f, -10), color);

                float x = station.position.x;
                AddBarrier(obstacles, new Vector3(x + 28, 1.5f, 85), new Vector3(8, 3, 4), "Outer obstacle relay gate", contact);
                AddBarrier(obstacles, new Vector3(x - 9, 1.5f, 135), new Vector3(10, 3, 4), "Trailer-clearance return gate", contact);
                Label(obstacles, font, "OFFSET RIGHT · KEEP THE LOAD CLEAR", new Vector3(x + 17, 4.5f, 30), color);
                for (int n = 0; n < 4; n++) Model(obstacles, ConeArt, new Vector3(x + (n % 2 == 0 ? 10 : 24), 0, n < 2 ? 30 : 68), new Vector3(1, 1.5f, 1));
            }
            // Skin the already-existing functional chicane; its collider and
            // mode mask are preserved exactly, using real owned barrier art.
            foreach (var original in match.GetComponentsInChildren<HitchModeObstacle>(true).Where(o => o.name == "Obstacle relay offset chicane"))
            {
                var art = new GameObject("Owned roadblock mesh over original physical chicane").transform; art.SetParent(original.transform, false);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(art, BarrierArt, Vector3.one, -.5f, true, "TruckHitchSports");
                var renderer = original.GetComponent<Renderer>(); if (renderer != null) { renderer.enabled = false; EditorUtility.SetDirty(renderer); }
            }
            foreach (var group in new[] { extended, reverse, obstacles }) group.gameObject.SetActive(false);
            root.gameObject.SetActive(true);
            for (int i = 0; i < actors.Length; i++) if (actors[i].localToWorldMatrix != actorPoses[i]) throw new InvalidOperationException("A proven native rig start changed unexpectedly.");
            for (int i = 0; i < stands.Length; i++) if (stands[i].localPosition != jackPositions[i]) throw new InvalidOperationException("A proven native landing-jack height changed unexpectedly.");
            for (int i = 0; i < match.finishes.Length; i++) if (match.finishes[i].localToWorldMatrix != finishPoses[i]) throw new InvalidOperationException("Original finish surface pose changed unexpectedly.");
            root.name = RootName;
            Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_HANDOFF_VARIANTS_AUTHORED four-leg stations exactly match runtime105/+80/+160 and existing finish+160; default rig/jack/finish poses preserved.");
        }
        static Transform ModeRoot(Transform parent, string name, int mask)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<HitchModeObstacle>().ModeMask = mask; return go.transform;
        }
        static void Bay(Transform parent, TMP_FontAsset font, Vector3 point, string title, Color color, Material material)
        {
            Label(parent, font, title, point + new Vector3(-18, 4.5f, 0), color);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int n = 0; n < 3; n++) Model(parent, ConeArt, point + new Vector3(side * 14, -.1f, (n - 1) * 14), new Vector3(1, 1.5f, 1));
                var line = new GameObject("Visible receiving lane edge").AddComponent<LineRenderer>(); line.transform.SetParent(parent, false); line.useWorldSpace = true; line.positionCount = 2; line.startWidth = line.endWidth = .12f; line.sharedMaterial = material;
                line.SetPosition(0, point + new Vector3(side * 10, .13f, -16)); line.SetPosition(1, point + new Vector3(side * 10, .13f, 16));
            }
        }
        static void AddBarrier(Transform parent, Vector3 point, Vector3 size, string name, PhysicsMaterial contact)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = point;
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(go.transform, BarrierArt, size, -size.y / 2, true, "TruckHitchSports");
            var collider = go.AddComponent<BoxCollider>(); collider.size = size; collider.sharedMaterial = contact;
        }
        static void Arrow(Transform parent, Vector3 point, Vector3 direction, Material material)
        {
            var line = new GameObject("Reverse direction arrow").AddComponent<LineRenderer>(); line.transform.SetParent(parent, false); line.useWorldSpace = true; line.positionCount = 5; line.startWidth = line.endWidth = .2f; line.sharedMaterial = material;
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            line.SetPosition(0, point - direction * 5); line.SetPosition(1, point + direction * 5); line.SetPosition(2, point + direction * 2 + right * 2); line.SetPosition(3, point + direction * 5); line.SetPosition(4, point + direction * 2 - right * 2);
        }
        static void Model(Transform parent, string source, Vector3 point, Vector3 size)
        {
            var root = new GameObject("Owned receiving-lane production prop").transform; root.SetParent(parent, false); root.position = point;
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root, source, size, 0, false, "TruckHitchSports");
        }
        static void Label(Transform parent, TMP_FontAsset font, string text, Vector3 point, Color color)
        {
            Model(parent, SignArt, new Vector3(point.x, 0, point.z), new Vector3(2, 4.5f, 1));
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.position = point;
            var label = go.AddComponent<TextMeshPro>(); label.font = font; label.text = text; label.fontSize = 6; label.richText = false; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(22, 3); label.color = color;
            var billboard = go.AddComponent<SportsPerCameraBillboard>(); billboard.distanceSizing = true; billboard.referenceDistance = 50; billboard.maximumPixelWidth = 310; billboard.maximumPixelHeight = 30; billboard.maximumLabelDistance = 190;
        }
    }
}
