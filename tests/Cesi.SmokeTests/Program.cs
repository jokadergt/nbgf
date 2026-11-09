using RblxExecutorUI;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var time = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
var schedule = new AutoInjectSchedule();
Check(!schedule.TrySchedule(0, 0, time), "No process");
Check(schedule.TrySchedule(123, 0, time), "First process");
Check(!schedule.TrySchedule(123, 0, time.AddSeconds(29)), "Failed attempts are throttled");
Check(schedule.TrySchedule(123, 0, time.AddSeconds(30)), "Retry after cooldown");
Check(!schedule.TrySchedule(123, 123, time.AddMinutes(1)), "Connected process is skipped");
Check(schedule.TrySchedule(456, 0, time.AddSeconds(31)), "New process connects immediately");
schedule.Reset();
Check(schedule.TrySchedule(456, 0, time.AddSeconds(32)), "Re-enabling resets cooldown");
Check(!schedule.TrySchedule(0, 0, time.AddSeconds(33)), "Process exit resets schedule");
Check(schedule.TrySchedule(456, 0, time.AddSeconds(34)), "Reopened process can connect");

var folder = Path.Combine(Path.GetTempPath(), "cesi-tests-" + Guid.NewGuid());
var path = Path.Combine(folder, "settings.json");
try
{
    var initial = UserPreferences.Load(path);
    Check(!initial.AutoInject && !initial.StreamMode, "Defaults are off");
    new UserPreferences { AutoInject = true, StreamMode = true }.Save(path);
    var restored = UserPreferences.Load(path);
    Check(restored.AutoInject && restored.StreamMode, "Settings survive restart");
    new UserPreferences().Save(path);
    Check(!UserPreferences.Load(path).AutoInject, "Existing preferences are replaced");
    Check(!File.Exists(path + ".tmp"), "Atomic save leaves no temp file");
    File.WriteAllText(path, "{broken");
    Check(!UserPreferences.Load(path).StreamMode, "Malformed settings use defaults");
}
finally { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
Check(UiCatalog.Languages.Length >= 10, "At least ten regions");
foreach (var language in UiCatalog.Languages)
{
    System.Globalization.CultureInfo.GetCultureInfo(language.Id);
    foreach (var key in UiCatalog.Strings.Keys)
        Check(UiCatalog.Strings[key].TryGetValue(language.Id, out var value) && !string.IsNullOrWhiteSpace(value),
            $"Translation completeness: {key}/{language.Id}");
    Check(string.Format(UiCatalog.Get("Count", language.Id), 12).Contains("12"), "Localized count placeholder");
}
var extraFolder = Path.Combine(Path.GetTempPath(), "cesi-appearance-" + Guid.NewGuid());
var extraPath = Path.Combine(extraFolder, "settings.json");
try
{
    new UserPreferences { Theme = "Light", Language = "ja-JP", WindowPreset = "MacOS",
        UiFont = "Arial", EditorFont = "Courier New" }.Save(extraPath);
    var restored = UserPreferences.Load(extraPath);
    Check(restored.Theme == "Light" && restored.Language == "ja-JP" && restored.WindowPreset == "MacOS"
        && restored.UiFont == "Arial" && restored.EditorFont == "Courier New", "Appearance roundtrip");
    File.WriteAllText(extraPath, "{\"Theme\":null,\"Language\":\"invalid\",\"WindowPreset\":\"other\",\"UiFont\":null,\"EditorFont\":null}");
    restored = UserPreferences.Load(extraPath);
    Check(restored.Theme == "Dark" && restored.Language == "pt-BR" && restored.WindowPreset == "Classic"
        && restored.UiFont == "Segoe UI" && restored.EditorFont == "Consolas", "Invalid preferences normalize");
    File.WriteAllText(extraPath, "{\"AutoInject\":true,\"StreamMode\":true}");
    restored = UserPreferences.Load(extraPath);
    Check(restored.AutoInject && restored.StreamMode && restored.Theme == "Dark", "Old settings migration");
}
finally { if (Directory.Exists(extraFolder)) Directory.Delete(extraFolder, recursive: true); }
var unicodeScript = "print(\"olá capivara 日本 🦫\")";
var payload = ExecutionPayload.Encode(unicodeScript);
Check(System.Text.Encoding.UTF8.GetString(payload) == unicodeScript, "UTF-8 payload roundtrip");
Check(payload.Length > unicodeScript.Length, "Native source length counts bytes");
Check(ExecutionPayload.Encode("").Length == 0, "Empty payload");
await ProfileChecks.Run(Check);
if (args.Length > 0)
{
    var native = System.Runtime.InteropServices.NativeLibrary.Load(Path.GetFullPath(args[0]));
    System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(RblxCore).Assembly,
        (name, assembly, searchPath) => name == "Syntax.dll" ? native : IntPtr.Zero);
    Check(RblxCore.Initialize(), "Native initialization ABI");
    Check(RblxCore.GetOffsetVersion() == "version-bc1bf755618240e8", "Native profile metadata");
    Check(RblxCore.GetExpectedExeSha256().Length == 64, "Native hash metadata");
    Check(!RblxCore.IsExecutionReady(), "Structural build reports execution pending");
    Check(!RblxCore.Connect(0), "Invalid native PID returns false with correct bool ABI");
    Check(!RblxCore.GetClientInfo(new System.Text.StringBuilder(512), 512), "Missing session returns false");
    Check(RblxCore.ExecuteScript("print('fixture')") == -9, "Structural build reports pending execution");
    Check(RblxCore.GetLastError().Contains("No script executed"), "Native execution status is explicit");
    RblxCore.Disconnect();
}
Console.WriteLine($"Passed {checks} checks.");
