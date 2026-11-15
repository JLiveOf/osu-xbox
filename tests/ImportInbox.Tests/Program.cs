using UwpOsu;

string root = Path.Combine(Path.GetTempPath(), "osu-inbox-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int assertions = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    assertions++;
    Console.WriteLine("PASS " + name);
}
string Upload(string folder, string name, string contents)
{
    string path = Path.Combine(folder, name);
    File.WriteAllText(path, contents);
    File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(-10));
    return path;
}
try
{
    string folder = Path.Combine(root, "inbox");
    var inbox = new ImportInbox(folder, Console.WriteLine);
    int imports = 0;
    Task Consume(string file) { imports++; File.Delete(file); return Task.CompletedTask; }
    string map = Upload(folder, "map.osz", "map");
    await inbox.ScanAsync(Consume);
    Check(imports == 0, "wait for two stable observations");
    File.AppendAllText(map, "still uploading");
    File.SetLastWriteTimeUtc(map, DateTime.UtcNow.AddSeconds(-10));
    await inbox.ScanAsync(Consume);
    Check(imports == 0, "changed upload resets readiness");
    await inbox.ScanAsync(Consume);
    Check(imports == 1 && !File.Exists(map), "stable completed upload is consumed");

    string skin = Upload(folder, "skin.OSK", "first");
    await inbox.ScanAsync(_ => Task.CompletedTask);
    await inbox.ScanAsync(_ => Task.CompletedTask);
    Upload(folder, "skin.OSK", "second");
    await inbox.ScanAsync(_ => Task.CompletedTask);
    await inbox.ScanAsync(_ => Task.CompletedTask);
    string[] archived = Directory.GetFiles(Path.Combine(folder, "done"));
    Check(archived.Length == 2 && archived.Select(File.ReadAllText).Order().SequenceEqual(new[] { "first", "second" }), "same-name leftovers are preserved without overwrite");

    Upload(folder, "broken.osz", "invalid");
    int failures = 0;
    Task Fail(string _) { failures++; throw new InvalidDataException("bad archive"); }
    await inbox.ScanAsync(Fail);
    await inbox.ScanAsync(Fail);
    await inbox.ScanAsync(Fail);
    Check(failures == 1 && File.Exists(Path.Combine(folder, "failed", "broken.osz.error.txt")), "failed import is quarantined with diagnostics and no retry loop");

    Upload(folder, "locked.osr", "replay");
    await inbox.ScanAsync(Consume);
    using (new FileStream(Path.Combine(folder, "locked.osr"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        await inbox.ScanAsync(Consume);
    Check(imports == 1, "locked upload is skipped");
    await inbox.ScanAsync(Consume);
    Check(imports == 2, "released upload is imported");

    Upload(folder, "ignored.part", "partial");
    await inbox.ScanAsync(Consume);
    await inbox.ScanAsync(Consume);
    Check(imports == 2, "partial and unsupported files are ignored");

    Upload(folder, "concurrent.osz", "map");
    await inbox.ScanAsync(Consume);
    var entered = new TaskCompletionSource();
    var release = new TaskCompletionSource();
    int concurrent = 0;
    async Task Slow(string file) { concurrent++; entered.SetResult(); await release.Task; File.Delete(file); }
    Task scan = inbox.ScanAsync(Slow);
    await entered.Task;
    await inbox.ScanAsync(Slow);
    release.SetResult();
    await scan;
    Check(concurrent == 1, "overlapping scans cannot import twice");
    var input = new ExternalInputState();
    Check(input.SetKey(90, true) && !input.SetKey(90, true), "held Z key is forwarded once without auto-repeat");
    Check(input.SetKey(90, false) && !input.SetKey(90, false), "duplicate key releases are ignored");
    Check(!input.SetKey(0xC3, true) && !input.SetKey(0xDA, true), "gamepad virtual keys are not duplicated as keyboard input");
    Check(input.SetButton(0, true) && !input.SetButton(0, true), "duplicate pointer press is forwarded once");
    input.SetKey(160, true);
    input.SetKey(161, true);
    var releasedKeys = new List<int>();
    var releasedButtons = new List<int>();
    input.ReleaseAll(releasedKeys.Add, releasedButtons.Add);
    input.ReleaseAll(releasedKeys.Add, releasedButtons.Add);
    Check(releasedKeys.Order().SequenceEqual(new[] { 160, 161 }) && releasedButtons.SequenceEqual(new[] { 0 }), "focus loss releases left/right modifiers and mouse exactly once");
    Check(input.SetKey(160, true) && input.SetButton(0, true), "new key and button presses work after focus returns");
    Check(ExternalInputState.Move(50, 50, -100, 1000, 100, 200) == (0, 199), "relative mouse movement stays inside the logical viewport");
    Check(ExternalInputState.Move(50, 50, 1, 1, 0, 0) == (0, 0), "mouse movement handles an uninitialised viewport");
    Console.WriteLine($"{assertions} assertions passed.");
}
finally
{
    Directory.Delete(root, true);
}
