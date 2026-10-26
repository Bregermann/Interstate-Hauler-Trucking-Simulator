using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckFishing.Editor
{
    // Explicit intake from an already downloaded owned package. Only the six
    // required clips enter the private boundary; the installed rig is reused.
    public static class FishingCarnivalWildlifeIntake
    {
        public const string Folder = "Assets/TruckFishing/PrivateLicensedWildlife/";
        public const string Model = "Assets/TruckCheeseRolling/Art/Animals/WolfLite.fbx";
        static readonly string[] Files = { "WL_Idles.FBX", "WL_Walk.anim", "WL_Walk Left.anim", "WL_Walk Right.anim", "WL_Swim.anim", "WL_Swim_Idle.anim" };
        [MenuItem("Truck Sports/Fishing/Import Missing Owned Wolf Clips Privately")]
        public static void ImportMissingOwnedClips()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            string root = Directory.GetParent(Application.dataPath).FullName;
            var ignores = File.ReadAllLines(Path.Combine(root, ".gitignore")).Select(line => line.Trim()).ToArray();
            if (!ignores.Contains("/" + Folder) || !ignores.Contains("/" + Folder.TrimEnd('/') + ".meta")) throw new InvalidOperationException("The exact private wildlife folder and its meta must be excluded from publication first.");
            if (!File.Exists(Model) || Digest(File.ReadAllBytes(Model)) != "9c8e7f5b74cc5ee143c00af39f4e2e7e837670564a6171c99523a7cb63691667")
                throw new InvalidOperationException("Preserve the existing wolf rig; these clips require the audited installed Malbers v2 model.");
            string package = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Asset Store-5.x/MalberS Animations/Editor ExtensionsAnimation/Animal Controller Malbers Character Controller.unitypackage");
            if (!File.Exists(package)) throw new FileNotFoundException("Owned downloaded Malbers Animal Controller package required.", package);
            const string source = "Assets/Malbers Animations/Animal Controller/Wolf Lite/Animations/";
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            ReadTar(package, (path, bytes) =>
            {
                if (!path.EndsWith("/pathname", StringComparison.Ordinal)) return;
                string original = Encoding.UTF8.GetString(bytes).Split('\n', '\r')[0].TrimEnd('\0');
                foreach (string file in Files) if (original == source + file) names[path.Split('/')[0]] = file;
            }, path => path.EndsWith("/pathname", StringComparison.Ordinal));
            if (names.Count != Files.Length) throw new InvalidOperationException("The owned package does not contain all audited wolf clips.");
            var records = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            ReadTar(package, (path, bytes) =>
            {
                var parts = path.Split('/'); if (parts.Length != 2 || !names.TryGetValue(parts[0], out var file)) return;
                if (parts[1] == "asset") records[file] = bytes;
                else if (parts[1] == "asset.meta")
                {
                    string meta = Encoding.UTF8.GetString(bytes);
                    meta = Regex.Replace(meta, @"(?m)^guid:\s+[a-f0-9]{32}", "guid: " + Digest(Encoding.UTF8.GetBytes("FishingCarnival/" + file)).Substring(0, 32));
                    // The animation-only FBX owns an independent generic avatar;
                    // the original Cheese rig importer and avatar remain intact.
                    if (file.EndsWith(".FBX", StringComparison.Ordinal))
                    { meta = Regex.Replace(meta, @"(?m)^(\s*)avatarSetup:\s*\d+", "$1avatarSetup: 1"); meta = Regex.Replace(meta, @"(?m)^(\s*)sourceAvatar:.*", "$1sourceAvatar: {fileID: 0}"); }
                    records[file + ".meta"] = Encoding.UTF8.GetBytes(meta);
                }
            }, path => names.ContainsKey(path.Split('/')[0]) && (path.EndsWith("/asset", StringComparison.Ordinal) || path.EndsWith("/asset.meta", StringComparison.Ordinal)));
            if (records.Count != Files.Length * 2) throw new InvalidOperationException("The package's required clip data/meta records are incomplete.");
            // Preflight every destination before the first write. No arbitrary
            // archive path is ever extracted, and differing owner data is retained.
            foreach (var record in records)
            {
                string target = Folder + record.Key;
                if (File.Exists(target) && !record.Key.EndsWith(".meta", StringComparison.Ordinal) && Digest(File.ReadAllBytes(target)) != Digest(record.Value))
                    throw new InvalidOperationException("Preserve differing licensed local content: " + target);
                if (File.Exists(target) && record.Key.EndsWith(".meta", StringComparison.Ordinal) && GuidOf(File.ReadAllText(target)) != GuidOf(Encoding.UTF8.GetString(record.Value)))
                    throw new InvalidOperationException("Preserve the existing local importer GUID: " + target);
            }
            Directory.CreateDirectory(Folder);
            foreach (var record in records) if (!File.Exists(Folder + record.Key)) File.WriteAllBytes(Folder + record.Key, record.Value);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("FISHING_WILDLIFE_PRIVATE_INTAKE reused identical installed WolfLite v2 rig; six genuine owned locomotion/idle clips imported privately. Bear model/clips were not present and remain missing.");
        }
        static string GuidOf(string text) => Regex.Match(text, @"(?m)^guid:\s*([a-f0-9]{32})").Groups[1].Value;
        static string Digest(byte[] bytes) { using (var hash = SHA256.Create()) return string.Concat(hash.ComputeHash(bytes).Select(value => value.ToString("x2"))); }
        static void ReadTar(string package, Action<string, byte[]> consume, Func<string, bool> wanted)
        {
            using (var file = File.OpenRead(package)) using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                var header = new byte[512]; var discard = new byte[8192];
                while (Read(gzip, header, 512) == 512 && header.Any(value => value != 0))
                {
                    string name = Encoding.ASCII.GetString(header, 0, 100).TrimEnd('\0');
                    string octal = Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' '); long size = string.IsNullOrEmpty(octal) ? 0 : Convert.ToInt64(octal, 8);
                    if (size < 0 || size > 512 * 1024 * 1024) throw new InvalidDataException("Unexpected Unity package record size.");
                    if (wanted(name)) { var bytes = new byte[(int)size]; if (Read(gzip, bytes, bytes.Length) != bytes.Length) throw new EndOfStreamException(); consume(name, bytes); }
                    else Skip(gzip, size, discard);
                    Skip(gzip, (512 - size % 512) % 512, discard);
                }
            }
        }
        static int Read(Stream source, byte[] bytes, int count) { int total = 0, value; while (total < count && (value = source.Read(bytes, total, count - total)) > 0) total += value; return total; }
        static void Skip(Stream source, long count, byte[] buffer) { while (count > 0) { int value = source.Read(buffer, 0, (int)Math.Min(count, buffer.Length)); if (value <= 0) throw new EndOfStreamException(); count -= value; } }
    }
}
