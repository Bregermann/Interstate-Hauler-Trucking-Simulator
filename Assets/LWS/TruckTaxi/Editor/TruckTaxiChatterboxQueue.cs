using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LWS.TruckTaxi.Editor
{
    // Editor-only transport. WorkHere still owns inference, model loading and reference validation.
    [InitializeOnLoad]
    public static class TruckTaxiChatterboxQueue
    {
        public const string AudioRoot = "Assets/LWS/TruckTaxi/Passengers/GeneratedAudio";
        public const string ToolRoot = "Tools/TruckTaxiPassengerFactory";
        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static bool IsRunning => process != null;
        public static string Status { get; private set; } = "Ready";
        public static string Log => log.ToString();
        public static event Action Changed;
        private static Process process;
        private static string activeJob;
        private static bool reloadLocked;
        private static readonly ConcurrentQueue<string> output = new ConcurrentQueue<string>();
        private static readonly StringBuilder log = new StringBuilder();

        [Serializable] public sealed class Job { public int schema = 1; public List<Item> items = new List<Item>(); }
        [Serializable] public sealed class Item
        {
            public string passengerId, lineId, text, voiceProfileId, emotion, referencePath;
            public float intensity;
            public string model, preset, device, requestHash, dialogueGuid, passengerGuid;
            public int seed;
            public bool regenerate;
            public string referenceSha256, installationStamp;
        }
        [Serializable] public sealed class Result { public string status, error; public List<ResultItem> items; }
        [Serializable] public sealed class ResultItem
        {
            public string passengerId, lineId, requestHash, hash, status, outputPath, error;
        }

        static TruckTaxiChatterboxQueue()
        {
            EditorApplication.update += Update;
            EditorApplication.quitting += OnQuitting;
        }

        public static bool IsSafeId(string value) => !string.IsNullOrEmpty(value) &&
            Regex.IsMatch(value, @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,99}$") && !value.EndsWith(".", StringComparison.Ordinal) &&
            !Regex.IsMatch(value.Split('.')[0], @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase);

        public static Item CreateItem(PassengerProfile passenger, TruckTaxiDialogueLine line, bool regenerate = false)
        {
            if (passenger == null || line == null || !IsSafeId(passenger.passengerId) || !IsSafeId(line.lineId))
                throw new InvalidOperationException("Passenger and line require stable ASCII IDs (not filenames/paths).");
            if (line.passengerId != passenger.passengerId) throw new InvalidOperationException("Dialogue line belongs to another passenger.");
            if (string.IsNullOrWhiteSpace(line.text)) throw new InvalidOperationException("Dialogue text is empty.");
            var voice = line.voiceProfile != null ? line.voiceProfile : passenger.voiceProfile;
            if (voice == null || !IsSafeId(voice.voiceProfileId)) throw new InvalidOperationException("Assign a voice profile with a stable ID.");
            string reference = string.IsNullOrWhiteSpace(line.preferredReference)
                ? voice.ResolveReference(line.emotion, out _) : line.preferredReference;
            if (string.IsNullOrWhiteSpace(reference)) throw new InvalidOperationException("No requested, closest, or neutral voice reference assigned.");
            reference = Path.GetFullPath(reference);
            if (!File.Exists(reference)) throw new FileNotFoundException("Voice reference not found. Existing clips are retained.", reference);
            if (!string.Equals(Path.GetExtension(reference), ".wav", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Voice reference must be a WAV file.");
            var item = new Item
            {
                passengerId = passenger.passengerId, lineId = line.lineId, text = line.text.Trim(),
                voiceProfileId = voice.voiceProfileId, emotion = line.emotion.ToString(), intensity = line.intensity,
                referencePath = reference, referenceSha256 = HashFile(reference), model = voice.model,
                preset = voice.preset, device = voice.device, seed = voice.seed,
                installationStamp = InstallationStamp(TruckTaxiChatterboxSettings.instance.workHere)
            };
            item.requestHash = Hash(JsonUtility.ToJson(item));
            item.regenerate = regenerate;
            item.dialogueGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(passenger.authoredDialogue));
            item.passengerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(passenger));
            return item;
        }

        private static string InstallationStamp(string workHere)
        {
            var builder = new StringBuilder();
            foreach (string name in new[] { "config/model-lock.json", "config/presets.json", "app/engine.py", "app/runtime.py" })
            {
                string path = Path.Combine(workHere, name);
                builder.Append(name).Append(File.Exists(path) ? HashFile(path) : "MISSING");
            }
            string adapter = Path.Combine(ProjectRoot, ToolRoot, "Chatterbox/generate_dialogue.py");
            builder.Append(File.Exists(adapter) ? HashFile(adapter) : "MISSING ADAPTER");
            return Hash(builder.ToString());
        }

        public static Job BuildJob(IEnumerable<PassengerProfile> passengers, bool missingOnly, bool regenerate,
            TruckTaxiDialogueLine selectedLine = null)
        {
            var job = new Job();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var passenger in passengers.Where(p => p != null).Distinct())
            {
                if (!ids.Add(passenger.passengerId ?? "")) throw new InvalidOperationException("Duplicate passenger ID: " + passenger.passengerId);
                if (passenger.authoredDialogue == null) { Append(passenger.name + ": no authored dialogue."); continue; }
                var lines = passenger.authoredDialogue.lines ?? Array.Empty<TruckTaxiDialogueLine>();
                var duplicate = lines.Where(l => l != null).GroupBy(l => l.lineId, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
                if (duplicate != null) { Append(passenger.name + ": duplicate line ID " + duplicate.Key + "; skipped."); continue; }
                foreach (var line in lines)
                {
                    if (line == null || (selectedLine != null && !ReferenceEquals(line, selectedLine))) continue;
                    try
                    {
                        var item = CreateItem(passenger, line, regenerate);
                        if (!missingOnly || line.generatedAudio == null || line.generationRequestHash != item.requestHash)
                            job.items.Add(item);
                    }
                    catch (Exception ex) { Append(passenger.name + "/" + line.lineId + ": " + ex.Message); }
                }
            }
            return job;
        }

        public static void Generate(IEnumerable<PassengerProfile> passengers, bool missingOnly = true,
            bool regenerate = false, TruckTaxiDialogueLine selectedLine = null)
        {
            if (IsRunning) throw new InvalidOperationException("A Truck Taxi audio queue is already running.");
            var job = BuildJob(passengers, missingOnly, regenerate, selectedLine);
            if (job.items.Count == 0) { Status = "No eligible missing/changed lines. See log for missing references."; Changed?.Invoke(); return; }
            string directory = Path.Combine(ProjectRoot, ToolRoot, "Jobs");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(job, true), new UTF8Encoding(false));
            Start(path);
        }

        public static void Resume()
        {
            if (IsRunning) return;
            string path = TruckTaxiChatterboxSettings.instance.lastJob;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("No previous queue to resume.");
            ValidateJobPath(path);
            // A forced regeneration is one-shot. A resumed queue reuses completed, hash-verified WAVs.
            var job = JsonUtility.FromJson<Job>(File.ReadAllText(path));
            foreach (var item in job.items) item.regenerate = false;
            File.WriteAllText(path, JsonUtility.ToJson(job, true), new UTF8Encoding(false));
            Start(path);
        }

        private static void Start(string path)
        {
            ValidateJobPath(path);
            var settings = TruckTaxiChatterboxSettings.instance;
            string workHere = Path.GetFullPath(settings.workHere);
            string python = Path.Combine(workHere, ".venv/Scripts/python.exe");
            string adapter = Path.Combine(ProjectRoot, ToolRoot, "Chatterbox/generate_dialogue.py");
            if (!File.Exists(python) || !File.Exists(Path.Combine(workHere, "app/engine.py")) || !File.Exists(adapter))
                throw new FileNotFoundException("Existing WorkHere venv/engine or Truck Taxi adapter not found. Nothing will be installed.");
            string cancel = Path.ChangeExtension(path, ".cancel");
            if (File.Exists(cancel)) File.Delete(cancel);
            var info = new ProcessStartInfo(python)
            {
                Arguments = "-B " + Quote(adapter) + " --job " + Quote(path) + " --workhere " + Quote(workHere) + " --project " + Quote(ProjectRoot),
                WorkingDirectory = ProjectRoot, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            if (settings.offline) info.EnvironmentVariables["HF_HUB_OFFLINE"] = "1";
            process = new Process { StartInfo = info };
            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.Enqueue(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.Enqueue(e.Data); };
            try
            {
                process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
                activeJob = path; settings.lastJob = path; settings.Persist();
                EditorApplication.LockReloadAssemblies(); reloadLocked = true;
                Status = "Generating with existing WorkHere (cancel finishes current line)";
                Append("Started " + path);
            }
            catch { process.Dispose(); process = null; throw; }
            Changed?.Invoke();
        }

        private static string Quote(string value)
        {
            if (value.Contains("\"") || value.Contains("\n") || value.Contains("\r")) throw new ArgumentException("Invalid path.");
            return "\"" + value.TrimEnd('\\', '/') + "\"";
        }

        private static void ValidateJobPath(string path)
        {
            string allowed = Path.GetFullPath(Path.Combine(ProjectRoot, ToolRoot, "Jobs")) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Queue must be in the Truck Taxi Jobs directory.");
        }

        public static void Cancel()
        {
            if (!IsRunning) return;
            File.WriteAllText(Path.ChangeExtension(activeJob, ".cancel"), "Cancel after current line.");
            Status = "Cancellation requested; current line will finish safely.";
            Changed?.Invoke();
        }

        private static void Update()
        {
            while (output.TryDequeue(out var message)) Append(message);
            if (process == null || !process.HasExited) return;
            try
            {
                process.WaitForExit();
                while (output.TryDequeue(out var message)) Append(message);
                string result = Path.ChangeExtension(activeJob, ".result.json");
                Status = File.Exists(result) ? ImportResult(activeJob, result) : "Generation failed (exit " + process.ExitCode + "). See log.";
                Directory.CreateDirectory(Path.Combine(ProjectRoot, ToolRoot, "Logs"));
                File.WriteAllText(Path.Combine(ProjectRoot, ToolRoot, "Logs", Path.GetFileNameWithoutExtension(activeJob) + ".log"), Log);
            }
            catch (Exception ex) { Status = "Import failed: " + ex.Message; Debug.LogException(ex); }
            finally
            {
                process.Dispose(); process = null;
                if (reloadLocked) { EditorApplication.UnlockReloadAssemblies(); reloadLocked = false; }
                Changed?.Invoke();
            }
        }

        public static string ImportResult(string jobPath, string resultPath)
        {
            var job = JsonUtility.FromJson<Job>(File.ReadAllText(jobPath));
            var result = JsonUtility.FromJson<Result>(File.ReadAllText(resultPath));
            int assigned = 0, skipped = 0;
            foreach (var completed in result.items ?? new List<ResultItem>())
            {
                if (completed.status != "complete" && completed.status != "cached") { Append(completed.lineId + ": " + completed.error); skipped++; continue; }
                try
                {
                    var item = job.items.Single(i => i.passengerId == completed.passengerId && i.lineId == completed.lineId);
                    var passenger = AssetDatabase.LoadAssetAtPath<PassengerProfile>(AssetDatabase.GUIDToAssetPath(item.passengerGuid));
                    var dialogue = AssetDatabase.LoadAssetAtPath<TruckTaxiDialogueSet>(AssetDatabase.GUIDToAssetPath(item.dialogueGuid));
                    if (passenger == null || dialogue == null || passenger.authoredDialogue != dialogue) throw new InvalidOperationException("Passenger/dialogue was removed or replaced.");
                    var line = dialogue.lines.Single(l => l != null && l.lineId == item.lineId && l.passengerId == item.passengerId);
                    if (item.requestHash != completed.requestHash || CreateItem(passenger, line).requestHash != item.requestHash)
                        throw new InvalidOperationException("Content/reference changed during generation; stale result not assigned. Generate again.");
                    string assetPath = ValidateOutputPath(completed.outputPath, item.passengerId, item.lineId, completed.hash);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                    var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                    if (importer == null) throw new InvalidOperationException("Generated WAV could not be imported.");
                    importer.forceToMono = true;
                    var sample = importer.defaultSampleSettings;
                    sample.loadType = AudioClipLoadType.DecompressOnLoad;
                    sample.compressionFormat = AudioCompressionFormat.Vorbis; sample.quality = .75f;
                    importer.defaultSampleSettings = sample; importer.SaveAndReimport();
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip == null || clip.samples <= 0) throw new InvalidOperationException("Empty generated AudioClip.");
                    Undo.RecordObject(dialogue, "Assign Truck Taxi generated dialogue");
                    line.generatedAudio = clip; line.generationHash = completed.hash; line.generationRequestHash = item.requestHash;
                    EditorUtility.SetDirty(dialogue); assigned++;
                }
                catch (Exception ex) { Append(completed.passengerId + "/" + completed.lineId + ": " + ex.Message); skipped++; }
            }
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(result.error)) Append(result.error);
            return result.status + ": " + assigned + " assigned, " + skipped + " skipped/failed.";
        }

        public static string ValidateOutputPath(string path, string passengerId, string lineId, string hash)
        {
            if (!IsSafeId(passengerId) || !IsSafeId(lineId) || string.IsNullOrEmpty(hash) || !Regex.IsMatch(hash, "^[a-f0-9]{64}$"))
                throw new InvalidOperationException("Invalid output identity/hash.");
            string expected = AudioRoot + "/" + passengerId + "/" + lineId + "_" + hash.Substring(0, 24) + ".wav";
            if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(Path.Combine(ProjectRoot, expected)), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Output must be the named line inside Truck Taxi GeneratedAudio.");
            return expected;
        }

        public static string HashFile(string path)
        {
            using (var input = File.OpenRead(path)) using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(input));
        }
        private static string Hash(string value) { using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(value))); }
        private static string Hex(byte[] value) => BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
        private static void Append(string message)
        {
            if (log.Length > 40000) log.Remove(0, log.Length - 30000);
            log.AppendLine(message); Changed?.Invoke();
        }
        private static void OnQuitting()
        {
            if (!IsRunning) return;
            Cancel();
            // Stop only this adapter process on Editor exit; completed atomic WAVs remain resumable.
            if (!process.WaitForExit(1000)) process.Kill();
            process.Dispose(); process = null;
        }
    }
}
