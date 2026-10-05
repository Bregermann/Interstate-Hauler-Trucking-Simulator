using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [Serializable]
    public sealed class TruckTaxiAnalyticsLocalConfig
    {
        public bool remoteEnabled;
        public string endpoint = "";
        public int performanceSeconds = 20;
        public bool HasValidEndpoint => remoteEnabled && endpoint != null && endpoint.Length <= 2048 &&
            Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment);
        public int PerformanceSeconds => Math.Max(10, Math.Min(30, performanceSeconds));
    }

    public enum TruckTaxiAnalyticsUploadResult { NotConfigured, Ready, Sending, Delivered, RetryScheduled, InvalidConfiguration }
    public enum TruckTaxiAnalyticsLocalResult { Starting, Ready, IoUnavailable, Exported, ExportFailed, Closed }

    // Single background owner for files; no filesystem or network wait occurs on the Unity thread.
    public sealed class TruckTaxiAnalyticsStorage : IDisposable
    {
        public const int MemoryLimit = 512, BatchLimit = 64, JournalFileLimit = 32, PendingFileLimit = 64;
        public const int FileByteLimit = 256 * 1024, LineLimit = 4096;
        private readonly BlockingCollection<TruckTaxiAnalyticsEvent> incoming = new BlockingCollection<TruckTaxiAnalyticsEvent>(MemoryLimit);
        private readonly string root, configPath;
        private readonly Func<string, CancellationToken, Task<bool>> delivery;
        private readonly Thread worker;
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private volatile bool stopping;
        private int exportRequested, flushRequested, dropped, pendingEvents, sampleSeconds = 20;
        private volatile TruckTaxiAnalyticsUploadResult uploadResult;
        private volatile TruckTaxiAnalyticsLocalResult localResult;
        private string installId, journal;
        private long journalBytes;
        private double nextUpload = 30, retryNotBefore;
        private int failures;
        private Task<bool> upload;
        private string uploadingFile;
        private int uploadingCount;
        private HttpClient http;
        private TruckTaxiAnalyticsLocalConfig config;
        public int DroppedEvents => Volatile.Read(ref dropped);
        public int PendingUploadEvents => Math.Max(0, Volatile.Read(ref pendingEvents));
        public int PerformanceSeconds => Volatile.Read(ref sampleSeconds);
        public TruckTaxiAnalyticsUploadResult LastUploadResult => uploadResult;
        public TruckTaxiAnalyticsLocalResult LastLocalResult => localResult;
        public Task Completion => completion.Task;
        public string DirectoryPath => root;

        public TruckTaxiAnalyticsStorage(string persistentDataPath, Func<string, CancellationToken, Task<bool>> delivery = null)
        {
            this.delivery = delivery;
            root = Path.Combine(persistentDataPath, "Analytics");
            configPath = Path.Combine(persistentDataPath, "analytics.local.json");
            worker = new Thread(Run) { IsBackground = true, Name = "TruckTaxiAnalytics" };
            worker.Start();
        }

        public bool TryRecord(TruckTaxiAnalyticsEvent item)
        {
            if (item == null || stopping) return false;
            // Install identity is owned by the writer. Snapshot mutable caller data before enqueue.
            var snapshot = item.Snapshot();
            snapshot.installId = "11111111111111111111111111111111";
            var safe = snapshot.Sanitize();
            if (safe == null) { Interlocked.Increment(ref dropped); return false; }
            if (incoming.TryAdd(safe)) return true;
            Interlocked.Increment(ref dropped); return false;
        }
        public void RequestBatch() => Interlocked.Exchange(ref flushRequested, 1);
        public void RequestCsvExport() => Interlocked.Exchange(ref exportRequested, 1);
        public void Dispose() { stopping = true; cancel.Cancel(); }

        private void Run()
        {
            FileStream lease = null;
            try
            {
                Directory.CreateDirectory(root);
                lease = new FileStream(Path.Combine(root, "writer.lock"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                installId = ReadInstallId(); config = ReadConfig();
                sampleSeconds = config.PerformanceSeconds;
                uploadResult = config.HasValidEndpoint ? TruckTaxiAnalyticsUploadResult.Ready : config.remoteEnabled ?
                    TruckTaxiAnalyticsUploadResult.InvalidConfiguration : TruckTaxiAnalyticsUploadResult.NotConfigured;
                if (config.HasValidEndpoint && delivery == null)
                    http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseDefaultCredentials = false })
                    { Timeout = TimeSpan.FromSeconds(10) };
                TrimFiles("events-*.jsonl", JournalFileLimit);
                TrimFiles("pending-*.jsonl", PendingFileLimit);
                RefreshPending(); localResult = TruckTaxiAnalyticsLocalResult.Ready;
                while (!stopping || incoming.Count > 0)
                {
                    var batch = new List<TruckTaxiAnalyticsEvent>(BatchLimit);
                    if (incoming.TryTake(out var first, stopping ? 0 : 500)) batch.Add(first);
                    while (batch.Count < BatchLimit && incoming.TryTake(out var next)) batch.Add(next);
                    if (batch.Count > 0) WriteBatch(batch);
                    if (Interlocked.Exchange(ref exportRequested, 0) != 0)
                    {
                        try { TruckTaxiAnalyticsCsv.Export(root); localResult = TruckTaxiAnalyticsLocalResult.Exported; }
                        catch (Exception) { localResult = TruckTaxiAnalyticsLocalResult.ExportFailed; }
                    }
                    if (!stopping) PumpUpload();
                }
            }
            catch (Exception) { localResult = TruckTaxiAnalyticsLocalResult.IoUnavailable; }
            finally
            {
                stopping = true; cancel.Cancel(); http?.Dispose(); lease?.Dispose();
                while (incoming.TryTake(out _)) Interlocked.Increment(ref dropped);
                completion.TrySetResult(true);
            }
        }

        private string ReadInstallId()
        {
            string path = Path.Combine(root, "install.id");
            if (File.Exists(path) && new FileInfo(path).Length <= 64)
            {
                string value = TruckTaxiAnalyticsPrivacy.RandomId(File.ReadAllText(path).Trim());
                if (value != "") return value;
            }
            string id = Guid.NewGuid().ToString("N");
            AtomicWrite(path, id); return id;
        }
        private TruckTaxiAnalyticsLocalConfig ReadConfig()
        {
            try
            {
                if (File.Exists(configPath) && new FileInfo(configPath).Length <= 4096)
                    return JsonUtility.FromJson<TruckTaxiAnalyticsLocalConfig>(File.ReadAllText(configPath)) ?? new TruckTaxiAnalyticsLocalConfig();
            }
            catch (Exception) { }
            return new TruckTaxiAnalyticsLocalConfig();
        }

        private void WriteBatch(List<TruckTaxiAnalyticsEvent> batch)
        {
            var text = new StringBuilder();
            foreach (var e in batch)
            {
                e.installId = installId;
                string line = JsonUtility.ToJson(e);
                if (line.Length > LineLimit) { Interlocked.Increment(ref dropped); continue; }
                text.Append(line).Append('\n');
            }
            string payload = text.ToString(); int bytes = Encoding.UTF8.GetByteCount(payload);
            if (bytes == 0) return;
            try
            {
                if (journal == null || journalBytes + bytes > FileByteLimit)
                {
                    TrimFiles("events-*.jsonl", JournalFileLimit - 1);
                    journal = NewPath("events"); journalBytes = 0;
                }
                using (var stream = new FileStream(journal, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    byte[] data = Encoding.UTF8.GetBytes(payload);
                    stream.Write(data, 0, data.Length); stream.Flush(true);
                }
                journalBytes += bytes;
                localResult = TruckTaxiAnalyticsLocalResult.Ready;
                // No retroactive upload of local-only history when remote delivery is later enabled.
                if (config.HasValidEndpoint)
                {
                    TrimFiles("pending-*.jsonl", PendingFileLimit - 1);
                    AtomicWrite(NewPath("pending"), payload);
                    Interlocked.Add(ref pendingEvents, payload.Count(c => c == '\n'));
                }
            }
            catch (Exception) { Interlocked.Add(ref dropped, batch.Count); localResult = TruckTaxiAnalyticsLocalResult.IoUnavailable; }
        }
        private string NewPath(string prefix) => Path.Combine(root, prefix + "-" + DateTime.UtcNow.Ticks.ToString("D19") + "-" + Guid.NewGuid().ToString("N") + ".jsonl");
        public static void AtomicWrite(string path, string text)
        {
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] data = Encoding.UTF8.GetBytes(text); stream.Write(data, 0, data.Length); stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }
        private void TrimFiles(string pattern, int maximum)
        {
            var files = Directory.GetFiles(root, pattern).OrderBy(x => x, StringComparer.Ordinal).ToList();
            int remaining = files.Count;
            foreach (string file in files)
            {
                if (remaining <= maximum) break;
                if (file == uploadingFile) continue;
                int lost = pattern.StartsWith("pending", StringComparison.Ordinal) ? ReadEvents(file).Count : 0;
                File.Delete(file); remaining--;
                if (lost > 0) { Interlocked.Add(ref dropped, lost); Interlocked.Add(ref pendingEvents, -lost); }
            }
        }
        public static List<TruckTaxiAnalyticsEvent> ReadEvents(string path)
        {
            var result = new List<TruckTaxiAnalyticsEvent>();
            if (!File.Exists(path) || new FileInfo(path).Length > FileByteLimit) return result;
            foreach (string line in File.ReadLines(path))
            {
                if (line.Length > LineLimit || result.Count >= 512) continue;
                try
                {
                    var safe = JsonUtility.FromJson<TruckTaxiAnalyticsEvent>(line)?.Sanitize();
                    if (safe != null) result.Add(safe);
                }
                catch (Exception) { } // Truncated/corrupt lines never become logs or upload payloads.
            }
            return result;
        }
        private void RefreshPending()
        {
            int count = 0;
            foreach (string file in Directory.GetFiles(root, "pending-*.jsonl")) count += ReadEvents(file).Count;
            Volatile.Write(ref pendingEvents, count);
        }
        public static double RetryDelay(int attempts) => Math.Min(300, 5 * Math.Pow(2, Math.Max(0, Math.Min(6, attempts - 1))));

        private void PumpUpload()
        {
            if (!config.HasValidEndpoint) return;
            double now = clock.Elapsed.TotalSeconds;
            if (upload != null)
            {
                if (!upload.IsCompleted) return;
                bool ok = upload.Status == TaskStatus.RanToCompletion && upload.Result;
                upload = null;
                if (ok)
                {
                    try { File.Delete(uploadingFile); Interlocked.Add(ref pendingEvents, -uploadingCount); failures = 0; uploadResult = TruckTaxiAnalyticsUploadResult.Delivered; }
                    catch (Exception) { ok = false; }
                }
                if (!ok)
                {
                    failures++; retryNotBefore = now + RetryDelay(failures);
                    uploadResult = TruckTaxiAnalyticsUploadResult.RetryScheduled;
                }
                uploadingFile = null; nextUpload = ok ? now + 1 : retryNotBefore;
            }
            bool urgent = Interlocked.Exchange(ref flushRequested, 0) != 0;
            if (now < retryNotBefore || (!urgent && now < nextUpload)) return;
            nextUpload = now + 30;
            string file = Directory.GetFiles(root, "pending-*.jsonl").OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            if (file == null) return;
            var records = ReadEvents(file);
            if (records.Count == 0) { File.Delete(file); RefreshPending(); return; }
            // Re-serialize the fixed DTO so unexpected fields in externally edited queue files cannot escape.
            string body = string.Join("\n", records.Take(BatchLimit).Select(e => JsonUtility.ToJson(e))) + "\n";
            // Queue files are produced in batches of at most 64. Reject external oversized batches.
            if (records.Count > BatchLimit) { Interlocked.Add(ref dropped, records.Count); File.Delete(file); RefreshPending(); return; }
            uploadingFile = file; uploadingCount = records.Count;
            uploadResult = TruckTaxiAnalyticsUploadResult.Sending;
            upload = Send(body);
        }
        private async Task<bool> Send(string body)
        {
            try
            {
                if (delivery != null) return await delivery(body, cancel.Token).ConfigureAwait(false);
                using (var request = new HttpRequestMessage(HttpMethod.Post, config.endpoint))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/x-ndjson");
                    using (var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel.Token).ConfigureAwait(false))
                        return response.IsSuccessStatusCode;
                }
            }
            catch (Exception) { return false; } // Never retain endpoint, response body, exception text or stack trace.
        }
    }
}
