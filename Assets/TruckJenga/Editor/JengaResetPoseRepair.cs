using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckJenga.Editor
{
    // Explicit repair of the first authored scene only. Backups precede edits; caller saves.
    public static class JengaResetPoseRepair
    {
        public const string BrokenSceneSha256 = "af22f0ba7909886813afdd1a278ce91838636f703a24f29f717510ebacf3e38c";

        public static void RepairKnownZeroResetPoseMetadata()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || scene.path != JengaAuthoring.Scene || scene.isDirty)
                throw new InvalidOperationException("Open the exact saved, stopped Jenga scene without unsaved changes.");
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(scene.path))).Replace("-", "").ToLowerInvariant();
            if (hash != BrokenSceneSha256) throw new InvalidOperationException("Repair refuses any scene except the known first zero-reset-pose author output: " + hash);
            var matches = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<JengaMatch>(true)).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Exactly one original Jenga authority required.");
            var match = matches[0];
            if (match.initialPositions.Length != 18 || match.initialRotations.Length != 18 ||
                match.initialPositions.Any(p => p != Vector3.zero) || match.initialRotations.Any(q => q != Quaternion.identity))
                throw new InvalidOperationException("Repair requires all 18 known zero/identity metadata entries.");
            ValidateOriginalTower(match, false);
            string backup = Path.GetFullPath("Logs/TruckSports/Impossible07/JengaV3RepairBackup/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            if (!File.Exists(scene.path + ".meta")) throw new InvalidOperationException("Scene GUID metadata must exist before repair.");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path, Path.Combine(backup, "TruckJenga.unity"));
            File.Copy(scene.path + ".meta", Path.Combine(backup, "TruckJenga.unity.meta"));
            if (!File.ReadAllBytes(scene.path).SequenceEqual(File.ReadAllBytes(Path.Combine(backup, "TruckJenga.unity"))) ||
                !File.ReadAllBytes(scene.path + ".meta").SequenceEqual(File.ReadAllBytes(Path.Combine(backup, "TruckJenga.unity.meta"))))
                throw new IOException("Exact scene/meta backup verification failed; no metadata changed.");
            // The physical scene is already correctly authored. Only its reset metadata is wrong.
            match.initialPositions = match.containers.Select(b => b.transform.position).ToArray();
            match.initialRotations = match.containers.Select(b => b.transform.rotation).ToArray();
            ValidateOriginalTower(match);
            EditorUtility.SetDirty(match);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("JENGA_V3_RESET_METADATA_REPAIRED fields=initialPositions,initialRotations entries=18 saved=false originalSceneSha=" + hash + " backup=" + backup);
        }

        public static void ValidateOriginalTower(JengaMatch match, bool validateMetadata = true)
        {
            if (match == null || match.containers.Length != 18 || match.shapes.Length != 18 ||
                (match.pieceSize - new Vector3(3.2f, 2.4f, 9.6f)).sqrMagnitude > 1e-10f)
                throw new InvalidOperationException("Original 18-piece dimensions required.");
            if (validateMetadata && (match.initialPositions.Length != 18 || match.initialRotations.Length != 18))
                throw new InvalidOperationException("All authored reset poses required.");
            for (int i = 0; i < 18; i++)
            {
                var body = match.containers[i]; var box = match.shapes[i];
                Vector3 expected = match.Slot(i / 3, i % 3); Quaternion rotation = match.Rotation(i / 3);
                if (body == null || box == null || box.attachedRigidbody != body || body.isKinematic || body.mass != 180 ||
                    body.name != "Actual dynamic Jenga container " + (i + 1) ||
                    (body.transform.position - expected).magnitude > .00001f || Quaternion.Angle(body.transform.rotation, rotation) > .001f ||
                    (body.transform.lossyScale - Vector3.one).magnitude > .00001f || box.center != Vector3.zero ||
                    (box.size - new Vector3(3.175f, 2.4f, 9.575f)).magnitude > .00001f ||
                    body.GetComponentsInChildren<Collider>(true).Length != 1)
                    throw new InvalidOperationException("Original physical tower geometry differs at piece " + i);
                if (validateMetadata && ((match.initialPositions[i] - expected).magnitude > .00001f ||
                    Quaternion.Angle(match.initialRotations[i], rotation) > .001f))
                    throw new InvalidOperationException("Reset metadata must match the actual authored slot at piece " + i);
                for (int j = 0; j < i; j++)
                    if (Physics.ComputePenetration(box, body.transform.position, body.transform.rotation,
                        match.shapes[j], match.containers[j].transform.position, match.containers[j].transform.rotation, out _, out float depth) && depth > .0001f)
                        throw new InvalidOperationException("Authored containers overlap: " + i + "/" + j + " depth=" + depth);
            }
            var deck = match.GetComponentsInChildren<BoxCollider>(true).Single(b => b.name == "One continuous physical dock deck");
            if (deck.attachedRigidbody != null || (deck.transform.position - new Vector3(0, -.7f, 0)).magnitude > .00001f ||
                (Vector3.Scale(deck.size, deck.transform.lossyScale) - new Vector3(110, 1.4f, 110)).magnitude > .00001f)
                throw new InvalidOperationException("Original fixed dock support required; no support geometry repair is permitted.");
        }
    }
}
