using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UwpOsu;

// Independent of the renderer so upload and failure handling can be tested on a PC.
internal sealed class ImportInbox
{
    private readonly string folder;
    private readonly Action<string> log;
    private readonly Dictionary<string, (long Length, DateTime Modified)> observed = new();
    private int scanning;
    private static readonly HashSet<string> extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".osz", ".osk", ".osr", ".olz"
    };

    public ImportInbox(string folder, Action<string> log)
    {
        this.folder = folder;
        this.log = log;
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "failed"));
        Directory.CreateDirectory(Path.Combine(folder, "done"));
    }

    public async Task ScanAsync(Func<string, Task> import)
    {
        if (Interlocked.Exchange(ref scanning, 1) != 0)
            return;

        try
        {
            var files = Directory.EnumerateFiles(folder)
                                 .Where(f => extensions.Contains(Path.GetExtension(f)))
                                 .ToArray();
            foreach (string stale in observed.Keys.Except(files).ToArray())
                observed.Remove(stale);

            foreach (string file in files)
            {
                try
                {
                    var info = new FileInfo(file);
                    var stamp = (info.Length, info.LastWriteTimeUtc);
                    bool stable = observed.TryGetValue(file, out var previous) && previous == stamp;
                    observed[file] = stamp;
                    if (!stable || DateTime.UtcNow - stamp.LastWriteTimeUtc < TimeSpan.FromSeconds(3))
                        continue;

                    // Portal uploads must also have released their writer handle.
                    using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }

                try
                {
                    await import(file).ConfigureAwait(false);
                    // The game normally consumes the source; preserve anything it leaves behind.
                    if (File.Exists(file))
                        archive(file, "done");
                    log("import finished: " + Path.GetFileName(file));
                }
                catch (Exception ex)
                {
                    log("import failed: " + Path.GetFileName(file) + ": " + ex);
                    if (File.Exists(file))
                    {
                        string target = archive(file, "failed");
                        File.WriteAllText(target + ".error.txt", ex.ToString());
                    }
                }
                finally
                {
                    observed.Remove(file);
                }
            }
        }
        finally
        {
            Volatile.Write(ref scanning, 0);
        }
    }

    private string archive(string source, string destination)
    {
        string target = Path.Combine(folder, destination, Path.GetFileName(source));
        if (File.Exists(target))
            target = Path.Combine(folder, destination,
                Path.GetFileNameWithoutExtension(source) + "-" + Guid.NewGuid().ToString("N") + Path.GetExtension(source));
        File.Move(source, target); // Never overwrite an earlier upload.
        return target;
    }
}
