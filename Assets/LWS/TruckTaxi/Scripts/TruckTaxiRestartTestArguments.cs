using System;
using System.IO;

namespace LWS.TruckTaxi
{
    // Pure parsing: no directory creation, vendor access, or player state changes.
    public sealed class TruckTaxiRestartTestArguments
    {
        public const string ModeFlag = "-truck-taxi-restart-test";
        public const string RunFlag = "-truck-taxi-test-run";
        public const string DriveFlag = "-truck-taxi-test-drive";
        public const string ProfileName = "ASTRA_SANDBOX_TEST";
        public string Mode { get; private set; }
        public string RunId { get; private set; }
        public string Root { get; private set; }
        public bool Drive { get; private set; }

        public static bool IsRequested(string[] args)
        {
            if (args == null) return false;
            foreach (string arg in args)
                if (arg != null && (arg.StartsWith(ModeFlag, StringComparison.OrdinalIgnoreCase) ||
                    arg.StartsWith(RunFlag, StringComparison.OrdinalIgnoreCase) ||
                    arg.StartsWith(DriveFlag, StringComparison.OrdinalIgnoreCase))) return true;
            return false;
        }

        public static bool TryParse(string[] args, string temporaryCachePath,
            out TruckTaxiRestartTestArguments request, out string failure)
        {
            request = null; failure = null;
            if (!IsRequested(args)) return true;
            string mode = null, run = null;
            bool drive = false;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i] ?? "";
                if (arg == DriveFlag && !drive) drive = true;
                else if (arg == ModeFlag || arg == RunFlag)
                {
                    if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith("-", StringComparison.Ordinal))
                    { failure = "Missing restart-test argument value."; return false; }
                    if (arg == ModeFlag && mode == null) mode = args[i];
                    else if (arg == RunFlag && run == null) run = args[i];
                    else { failure = "Duplicate restart-test flag."; return false; }
                }
                else if (arg.StartsWith("-truck-taxi-", StringComparison.OrdinalIgnoreCase))
                { failure = "Malformed or conflicting Truck Taxi test flag: " + arg; return false; }
            }
            if (mode != "seed" && mode != "continue" && mode != "manual" && mode != "cleanup" && mode != "soak")
            { failure = "Mode must be seed, continue, manual, cleanup, or soak."; return false; }
            if (drive && mode != "seed" && mode != "continue" && mode != "manual")
            { failure = "Drive regression supports seed, continue, or manual only."; return false; }
            if (run == null || run.Length != 32 || !Guid.TryParseExact(run, "N", out var id) || id == Guid.Empty)
            { failure = "Run must be a nonzero 32-hex GUID, never a path."; return false; }
            try
            {
                if (string.IsNullOrWhiteSpace(temporaryCachePath) || !Path.IsPathRooted(temporaryCachePath))
                    throw new ArgumentException("Temporary cache path must be absolute.");
                string parent = Path.GetFullPath(Path.Combine(temporaryCachePath, "TruckTaxiRestartTests"));
                string root = Path.GetFullPath(Path.Combine(parent, id.ToString("N")));
                if (!string.Equals(Path.GetDirectoryName(root), parent, StringComparison.Ordinal))
                    throw new ArgumentException("Test root escaped the fixed directory.");
                request = new TruckTaxiRestartTestArguments { Mode = mode, RunId = id.ToString("N"), Root = root, Drive = drive };
                return true;
            }
            catch (Exception ex) { failure = "Invalid isolated root: " + ex.Message; return false; }
        }

        public void VerifyFilesystemBoundary()
        {
            // Reject junction/symlink ancestors before either our mkdir or any vendor operation.
            for (var dir = new DirectoryInfo(Root); dir != null; dir = dir.Parent)
            {
                if (File.Exists(dir.FullName)) throw new IOException("A file occupies a test directory path.");
                if (dir.Exists && (dir.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Reparse points are not allowed in the isolated test path.");
            }
            if (!Directory.Exists(Root)) return;
            // The vendor disk storer uses flat files; never traverse nested directories.
            foreach (string entry in Directory.GetFileSystemEntries(Root))
                if ((File.GetAttributes(entry) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                    throw new IOException("Unexpected directory or reparse point in the isolated vendor root.");
        }
    }
}
