using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RblxExecutorUI;

public sealed record UiChoice(string Id, string Label);
public static class UiCatalog
{
    public static readonly UiChoice[] Languages = {
        new("pt-BR", "Português — Brasil"), new("en-US", "English — United States"),
        new("es-ES", "Español — España"), new("fr-FR", "Français — France"),
        new("de-DE", "Deutsch — Deutschland"), new("it-IT", "Italiano — Italia"),
        new("ja-JP", "日本語 — 日本"), new("ko-KR", "한국어 — 대한민국"),
        new("zh-CN", "简体中文 — 中国"), new("ru-RU", "Русский — Россия")
    };
    public static readonly string[] UiFonts = { "Segoe UI", "Arial", "Calibri", "Tahoma", "Verdana", "Trebuchet MS" };
    public static readonly string[] CodeFonts = { "Consolas", "Cascadia Mono", "Courier New" };
    public static readonly IReadOnlyDictionary<string, Dictionary<string, string>> Strings = Load();
    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        using var stream = typeof(UiCatalog).Assembly.GetManifestResourceStream("cesi.Localization.json")
            ?? throw new InvalidOperationException("Missing localization catalog.");
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
    }
    public static string Get(string key, string locale) => Strings.TryGetValue(key, out var values)
        ? values.GetValueOrDefault(locale, values["en-US"]) : key;
}
