using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Explicit, reviewed ACTUAL WINDOWS opening captures only. This does not
    // capture, launch, rebuild, modify scenes or install collection entries.
    public static class SportsImpossiblePreviewAuthoring
    {
        const string PreviewFolder = "Assets/TruckSports/Art/Previews/";
        const string ProvenancePath = "Tools/TruckSports/ArtworkSources/Impossible07Previews/windows-openings-provenance.json";
        const string Scope = "Actual Windows opening gameplay after the probe's seven-second wait; not signature-mechanic, full-match, human, hardware, listening or performance acceptance.";

        [Serializable] public sealed class ReviewedCapture
        {
            public string key, scene, sourceFile, sha256;
        }
        [Serializable] public sealed class ReviewedInputs
        {
            public int schema;
            public bool reviewed;
            public string reviewId, receiptSha256, progressSha256;
            public ReviewedCapture[] captures;
        }
        [Serializable] sealed class ImportedCapture
        {
            public string key, scene, sourceFile, sourceSha256, previewPath, previewSha256, importerGuid, importerMetaSha256;
        }
        [Serializable] sealed class Provenance
        {
            public int schema = 1;
            public string scope, reviewId, reviewedManifestSha256, receiptSha256, progressSha256;
            public ImportedCapture[] captures;
        }

        public static void ImportReviewedWindowsPreviewsFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            string Argument(string name)
            {
                int index = Array.IndexOf(args, name);
                if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Provide " + name + ".");
                return args[index + 1];
            }
            ImportReviewedWindowsPreviews(Argument("-tsImpossiblePreviewRun"), Argument("-tsImpossiblePreviewReview"));
        }

        public static void ImportReviewedWindowsPreviews(string runFolder, string reviewedManifestPath)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required for explicit reviewed preview import.");
            var folder = Path.GetFullPath(runFolder);
            var reviewPath = Path.GetFullPath(reviewedManifestPath);
            string receiptPath = Path.Combine(folder, "result.txt"), progressPath = Path.Combine(folder, "progress.txt");
            var inputs = ValidateReviewedMappings(File.ReadAllText(reviewPath));
            if (Hash(receiptPath) != inputs.receiptSha256 || Hash(progressPath) != inputs.progressSha256)
                throw new InvalidOperationException("Completed Windows receipt/progress differ from the reviewed input hashes.");
            ValidateReceipt(File.ReadAllText(receiptPath), File.ReadAllText(progressPath));

            // All inputs and all targets are checked BEFORE the first asset write.
            foreach (var capture in inputs.captures)
            {
                string source = Path.Combine(folder, capture.sourceFile);
                if (Hash(source) != capture.sha256 || !HasExactPreviewHeader(File.ReadAllBytes(source)))
                    throw new InvalidOperationException("Reviewed actual 1280x720 PNG required: " + capture.sourceFile);
            }
            string reviewHash = Hash(reviewPath);
            var targets = inputs.captures.Select(capture => PreviewFolder + capture.scene + ".png").ToArray();
            if (File.Exists(ProvenancePath))
            {
                var prior = JsonUtility.FromJson<Provenance>(File.ReadAllText(ProvenancePath));
                if (prior == null || prior.schema != 1 || prior.reviewedManifestSha256 != reviewHash || prior.scope != Scope ||
                    prior.captures == null || prior.captures.Length != 21)
                    throw new InvalidOperationException("Preserve existing preview provenance; it belongs to different or unknown reviewed inputs.");
                foreach (var capture in inputs.captures)
                {
                    var saved = prior.captures.SingleOrDefault(item => item.scene == capture.scene);
                    string target = PreviewFolder + capture.scene + ".png";
                    if (saved == null || saved.key != capture.key || saved.sourceFile != capture.sourceFile || saved.sourceSha256 != capture.sha256 ||
                        saved.previewPath != target || saved.previewSha256 != capture.sha256 || Hash(target) != saved.previewSha256 ||
                        Hash(target + ".meta") != saved.importerMetaSha256 || AssetDatabase.AssetPathToGUID(target) != saved.importerGuid || !Configured(target))
                        throw new InvalidOperationException("Preserve changed/custom/partial preview assets; explicit review required: " + target);
                }
                Debug.Log("IMPOSSIBLE_WINDOWS_PREVIEWS_PRESERVED=21; same reviewed bytes/importer GUIDs; " + Scope);
                return;
            }
            foreach (var target in targets)
                if (File.Exists(target) || File.Exists(target + ".meta") || AssetDatabase.LoadMainAssetAtPath(target) != null)
                    throw new InvalidOperationException("New previews only; refuse existing, custom or partial unowned target: " + target);
            if (!Directory.Exists(PreviewFolder)) throw new DirectoryNotFoundException("Preserve the existing preview folder; do not recreate legacy artwork.");
            var oldFiles = Directory.GetFiles(PreviewFolder, "*", SearchOption.AllDirectories).ToDictionary(path => path, Hash);
            var provenance = new Provenance { scope = Scope, reviewId = inputs.reviewId, reviewedManifestSha256 = reviewHash,
                receiptSha256 = inputs.receiptSha256, progressSha256 = inputs.progressSha256, captures = new ImportedCapture[21] };
            for (int index = 0; index < inputs.captures.Length; index++)
            {
                var capture = inputs.captures[index]; string target = targets[index];
                File.Copy(Path.Combine(folder, capture.sourceFile), target, false);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(target) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Original new PNG texture importer required: " + target);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
                if (!Configured(target) || Hash(target) != capture.sha256)
                    throw new InvalidOperationException("New preview must retain reviewed screenshot bytes and exact import configuration: " + target);
                provenance.captures[index] = new ImportedCapture { key = capture.key, scene = capture.scene, sourceFile = capture.sourceFile,
                    sourceSha256 = capture.sha256, previewPath = target, previewSha256 = Hash(target),
                    importerGuid = AssetDatabase.AssetPathToGUID(target), importerMetaSha256 = Hash(target + ".meta") };
            }
            foreach (var old in oldFiles)
                if (!File.Exists(old.Key) || Hash(old.Key) != old.Value)
                    throw new InvalidOperationException("An existing legacy preview/importer changed unexpectedly: " + old.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(ProvenancePath));
            if (File.Exists(ProvenancePath)) throw new IOException("Preserve concurrent preview provenance.");
            File.WriteAllText(ProvenancePath, JsonUtility.ToJson(provenance, true));
            Debug.Log("IMPOSSIBLE_REVIEWED_WINDOWS_PREVIEWS_IMPORTED=21; collection installation remains a separate explicit action; " + Scope);
        }

        static bool Configured(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            return importer != null && importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                !importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Clamp && importer.maxTextureSize == 1024 &&
                AssetDatabase.LoadAssetAtPath<Sprite>(path) != null && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path));
        }
        static string Hash(string path)
        {
            using (var file = File.OpenRead(path)) using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
        }
        static bool ValidHash(string value) => value != null && Regex.IsMatch(value, "\\A[0-9a-f]{64}\\z", RegexOptions.CultureInvariant);

        // Pure preflight APIs: also exercised without importing any assets.
        public static ReviewedInputs ValidateReviewedMappings(string json)
        {
            var inputs = JsonUtility.FromJson<ReviewedInputs>(json);
            if (inputs == null || inputs.schema != 1 || !inputs.reviewed || string.IsNullOrWhiteSpace(inputs.reviewId) ||
                !ValidHash(inputs.receiptSha256) || !ValidHash(inputs.progressSha256) || inputs.captures == null || inputs.captures.Length != 21)
                throw new InvalidOperationException("Explicit reviewed21-capture manifest, review identity and exact receipt/progress SHA256 required.");
            if (inputs.captures.Any(capture => capture == null) || inputs.captures.Select(capture => capture.scene).Distinct().Count() != 21 ||
                inputs.captures.Select(capture => capture.sha256).Distinct().Count() != 21)
                throw new InvalidOperationException("All21 distinct natural gameplay inputs required; no reused old image or duplicate sport.");
            foreach (var entry in SportsImpossibleGames.All)
            {
                var capture = inputs.captures.SingleOrDefault(item => item.scene == entry.scene);
                if (capture == null || capture.key != entry.key || capture.sourceFile != entry.scene + "-natural-gameplay-1280.png" || !ValidHash(capture.sha256))
                    throw new InvalidOperationException("Exact current sport key/scene and flat actual natural-gameplay1280 filename required: " + entry.scene);
            }
            return inputs;
        }

        public static void ValidateReceipt(string receipt, string progress)
        {
            if (string.IsNullOrWhiteSpace(progress) || !receipt.StartsWith(progress, StringComparison.Ordinal))
                throw new InvalidOperationException("The hashed final receipt must retain the exact hashed progress history.");
            var lines = receipt.Replace("\r", "").Split('\n');
            if (lines.Any(line => line.StartsWith("FAIL ", StringComparison.Ordinal) || line.StartsWith("ERROR ", StringComparison.Ordinal) ||
                line.StartsWith("RUNNING", StringComparison.Ordinal)) || progress.Replace("\r", "").Split('\n').Any(line =>
                line.StartsWith("FAIL ", StringComparison.Ordinal) || line.StartsWith("ERROR ", StringComparison.Ordinal)))
                throw new InvalidOperationException("Preserve failed/pending Windows runs; no previews can be imported from them.");
            var summaries = lines.Where(line => line.StartsWith("CHECKS=", StringComparison.Ordinal)).ToArray();
            if (summaries.Length != 1) throw new InvalidOperationException("One completed actual Windows result summary required.");
            var summary = Regex.Match(summaries[0], "\\ACHECKS=([0-9]+) ERRORS=0 SCENES=21 SCREENSHOTS=([0-9]+) INTERRUPTED=False\\z", RegexOptions.CultureInvariant);
            if (!summary.Success || !int.TryParse(summary.Groups[1].Value, out int checks) || checks <= 0 ||
                checks != lines.Count(line => line.StartsWith("PASS ", StringComparison.Ordinal)) ||
                !int.TryParse(summary.Groups[2].Value, out int screenshots) || screenshots < 86 ||
                !lines.Any(line => line.StartsWith("Actual Windows composed frames, ordinary saved setup/AI opening", StringComparison.Ordinal)))
                throw new InvalidOperationException("Successful complete21-scene Windows receipt and actual screenshot/check counts required.");
            foreach (var entry in SportsImpossibleGames.All)
            {
                foreach (string required in new[] { "existing navigation opens correct saved authority", "ordinary AI setup starts the actual match",
                    "actual match remains in a valid gameplay/results state", "actual active camera renders into the player backbuffer",
                    "natural-gameplay has actual 1280x720 backbuffer and PNG dimensions" })
                {
                    string pass = "PASS " + entry.scene + " " + required;
                    if (lines.Count(line => line == pass) != 1 || progress.Replace("\r", "").Split('\n').Count(line => line == pass) != 1)
                        throw new InvalidOperationException("Each reviewed input needs the actual single successful saved-scene opening/capture: " + pass);
                }
            }
        }

        public static bool HasExactPreviewHeader(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 33) return false;
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            for (int i = 0; i < signature.Length; i++) if (bytes[i] != signature[i]) return false;
            return bytes[8] == 0 && bytes[9] == 0 && bytes[10] == 0 && bytes[11] == 13 && bytes[12] == 'I' && bytes[13] == 'H' &&
                bytes[14] == 'D' && bytes[15] == 'R' && bytes[16] == 0 && bytes[17] == 0 && bytes[18] == 5 && bytes[19] == 0 &&
                bytes[20] == 0 && bytes[21] == 0 && bytes[22] == 2 && bytes[23] == 208 && bytes[24] == 8 &&
                (bytes[25] == 2 || bytes[25] == 6) && bytes[26] == 0 && bytes[27] == 0 && bytes[28] == 0;
        }
    }
}
