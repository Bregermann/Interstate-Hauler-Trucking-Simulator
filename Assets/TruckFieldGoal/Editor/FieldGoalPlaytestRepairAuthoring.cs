using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckFieldGoal.Editor
{
    public static class FieldGoalPlaytestRepairAuthoring
    {
        public static void UpgradeAudio()
        {
            EditorSceneManager.OpenScene(FieldGoalAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<FieldGoalMatch>();
            if (match.playtestAudioRepairVersion >= 1) return;
            const string thumpPath = "Assets/NWH/Vehicle Physics 2/Sound/Suspension/SuspensionThump.wav";
            var thump = AssetDatabase.LoadAssetAtPath<AudioClip>(thumpPath);
            if (thump == null) throw new InvalidOperationException("Audited existing soft physical thump is required.");
            if (match.feedback.clips.Length < 8) Array.Resize(ref match.feedback.clips, 8);
            match.feedback.clips[7] = thump; match.kickSoundIndex = 7;
            match.playtestAudioRepairVersion = 1;
            EditorUtility.SetDirty(match); EditorUtility.SetDirty(match.feedback);
            EditorSceneManager.MarkSceneDirty(match.gameObject.scene); EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("FIELD_AUDIO_REPAIR one crowd owner, bounded post contacts and dedicated native thump: " + thumpPath);
        }
        public static void UpgradeHolder()
        {
            EditorSceneManager.OpenScene(FieldGoalAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<FieldGoalMatch>();
            var existing = match.GetComponent<FieldGoalHolderPresentation>();
            if (existing != null && existing.authoredVersion >= 1) return;
            var holder = match.transform.Find("Stationary football holder truck");
            if (holder == null) throw new InvalidOperationException("Existing holder must be preserved.");
            Transform model = null;
            foreach (Transform child in holder) if (child.name.Contains("NWH truck model")) model = child;
            if (model == null) throw new InvalidOperationException("Reuse the actual authored holder model.");
            bool first = true; Bounds b = default;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var part = LWS.TruckSports.Editor.SportsRallyAuthoring.BoundsIn(filter, holder);
                if (first) { b = part; first = false; } else b.Encapsulate(part);
            }
            if (first) throw new InvalidOperationException("Holder render model missing.");
            // The old clone zeroed a required imported-model centering offset.
            // Recenter measured geometry instead of counter-moving only its Y.
            model.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            holder.rotation = Quaternion.identity;
            var ball = match.football.shape.bounds;
            holder.position = new Vector3(ball.center.x + ball.extents.x + b.extents.x + .5f, .04f, ball.center.z - .45f);
            var oldArm = holder.Find("Holder padded steadying arm");
            if (oldArm != null) UnityEngine.Object.DestroyImmediate(oldArm.gameObject);
            var arm = new GameObject("Holder padded steadying arm").transform; arm.SetParent(holder, false);
            var rubber = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Kicking tee rubber.mat");
            var start = holder.position + new Vector3(-b.extents.x * .85f, b.size.y * .76f, .45f);
            var end = new Vector3(ball.center.x + .18f, ball.max.y - .03f, ball.center.z);
            FieldGoalModels.Cylinder(arm, "Slim holder arm", holder.InverseTransformPoint(start), holder.InverseTransformPoint(end), .055f, rubber);
            var presentation = existing ?? match.gameObject.AddComponent<FieldGoalHolderPresentation>();
            presentation.match = match; presentation.holder = holder; presentation.authoredVersion = 1;
            EditorUtility.SetDirty(presentation); EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
            EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("FIELD_HOLDER_REPAIR position=" + holder.position + " orientation=" + holder.eulerAngles + " modelCentering=" + model.localPosition);
        }
        public static void Audit()
        {
            EditorSceneManager.OpenScene(FieldGoalAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<FieldGoalMatch>();
            var text = new System.Text.StringBuilder();
            foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (source.gameObject.scene != match.gameObject.scene) continue;
                text.AppendLine("SOURCE " + source.name + " clip=" + AssetDatabase.GetAssetPath(source.clip) + " loop=" + source.loop + " gain=" + source.volume + " pitch=" + source.pitch + " spatial=" + source.spatialBlend + " mixer=" + source.outputAudioMixerGroup);
            }
            for (int i = 0; i < match.feedback.clips.Length; i++) text.AppendLine("FEEDBACK " + i + " " + AssetDatabase.GetAssetPath(match.feedback.clips[i]));
            var holder = match.transform.Find("Stationary football holder truck");
            if (holder == null) throw new InvalidOperationException("Preserve the original holder presentation; it must exist in the saved scene.");
            var envelope = LWS.TruckTrailerPlug.Editor.TrailerPlugRepairAuthoring.ModelBounds(holder.gameObject);
            text.AppendLine("HOLDER position=" + holder.position.ToString("F4") + " rotation=" + holder.eulerAngles + " localModelBounds=" + envelope.ToString("F4"));
            text.AppendLine("BALL pose=" + match.football.body.position.ToString("F4") + " bounds=" + match.football.shape.bounds.ToString("F4"));
            text.AppendLine("KICKER shape=" + match.truck.shape.size + " center=" + match.truck.shape.center);
            text.AppendLine("KICKER visual local pose=" + match.truck.visuals.localPosition.ToString("F4") + " scale=" + match.truck.visuals.localScale);
            foreach (Transform child in holder) if (child.name.Contains("NWH truck model"))
                text.AppendLine("HOLDER model local pose=" + child.localPosition.ToString("F4") + " scale=" + child.localScale + " bounds=" + LWS.TruckTrailerPlug.Editor.TrailerPlugRepairAuthoring.ModelBounds(child.gameObject).ToString("F4"));
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");
            File.WriteAllText("Logs/TruckSports/PlaytestRepair/field-goal-audio-holder-audit.txt", text.ToString());
            Debug.Log(text.ToString());
        }
    }
}
