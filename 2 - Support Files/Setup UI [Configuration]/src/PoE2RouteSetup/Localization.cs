using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PoE2RouteSetup;

public sealed record UiLanguage(string Code, string DisplayName);

public static class Localization
{
    // SetupUI intentionally uses the exact same language set as the current
    // Path of Exile 2 international client. Keeping this list derived from the
    // game-language catalog prevents the two Settings drop-downs from drifting
    // apart again when support changes.
    public static readonly IReadOnlyList<UiLanguage> Languages = PoE2GameLanguages.All
        .Select(x => new UiLanguage(x.Code, x.DisplayName))
        .ToList();

    private sealed class ControlTextState
    {
        public string OriginalText { get; set; } = "";
        public string LastAppliedText { get; set; } = "";
        public string OriginalPlaceholder { get; set; } = "";
        public string LastAppliedPlaceholder { get; set; } = "";
    }

    private sealed class StringItemsState
    {
        public List<object?> Originals { get; } = new();
        public List<string?> LastApplied { get; } = new();
        public int OriginalCount { get; set; } = -1;
    }

    private sealed class ColumnTextState
    {
        public string OriginalText { get; set; } = "";
        public string LastAppliedText { get; set; } = "";
    }

    private static readonly ConditionalWeakTable<Control, ControlTextState> ControlStates = new();
    private static readonly ConditionalWeakTable<ListControl, StringItemsState> ListStates = new();
    private static readonly ConditionalWeakTable<DataGridViewColumn, ColumnTextState> ColumnStates = new();
    private static readonly Dictionary<string, Dictionary<string, string>> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Dictionary<string, string>> ProperNounCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> AuthoritativeProperNounLanguages = new(
        PoE2GameLanguages.All
            .Where(x => !string.Equals(x.Code, "en", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Code),
        StringComparer.OrdinalIgnoreCase);
    private static string _currentCode = "en";
    private static bool _isApplying;

    public static string CurrentCode => _currentCode;

    public static bool IsSupported(string? code)
        => !string.IsNullOrWhiteSpace(code) && Languages.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string? code)
        => IsSupported(code) ? Languages.First(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)).Code : "en";

    public static void SetLanguage(string? code)
    {
        _currentCode = Normalize(code);
        _ = GetDictionary(_currentCode);
        _ = GetProperNounDictionary(_currentCode);
    }

    public static string Translate(string? english)
    {
        if (string.IsNullOrEmpty(english) || string.Equals(_currentCode, "en", StringComparison.OrdinalIgnoreCase))
            return english ?? "";

        var dict = GetDictionary(_currentCode);
        if (dict.TryGetValue(english, out var translated) && !string.IsNullOrWhiteSpace(translated))
            return translated;

        // Preserve common suffixes/prefixes while translating the stable visible portion.
        if (english.StartsWith("PoE2 Route AutoSplitter Setup — v", StringComparison.Ordinal))
        {
            var suffix = english["PoE2 Route AutoSplitter Setup".Length..];
            return Translate("PoE2 Route AutoSplitter Setup") + suffix;
        }

        return english;
    }

    public static string TranslateFormat(string englishFormat, params object[] args)
        => string.Format(Translate(englishFormat), args);

    public static string TranslateProperNoun(string? english)
    {
        if (string.IsNullOrWhiteSpace(english)) return english ?? "";
        var value = english!;

        // Synthetic Trial of Chaos milestones are SetupUI route labels, not GGG proper
        // nouns. Localize the wrapper while preserving the encounter number.
        var chaosBoss = Regex.Match(value, @"^Chaos Boss\s+(\d+)$", RegexOptions.CultureInvariant);
        if (chaosBoss.Success)
            return TranslateFormat("Chaos Boss {0}", chaosBoss.Groups[1].Value);

        // Some route display strings decorate an authoritative area name with route-only
        // structure. Resolve the canonical area portion instead of trying to look up the
        // complete decorated string in the authoritative proper-noun catalog.
        var actPrefix = Regex.Match(value, @"^(Act\s+\d+)\s*(?:—|-)\s*(.+)$", RegexOptions.CultureInvariant);
        if (actPrefix.Success)
            return Translate(actPrefix.Groups[1].Value) + " — " + TranslateProperNoun(actPrefix.Groups[2].Value);

        const string blockedSuffix = " (blocked)";
        if (value.EndsWith(blockedSuffix, StringComparison.OrdinalIgnoreCase)
            && value.Length > blockedSuffix.Length)
            return TranslateProperNoun(value[..^blockedSuffix.Length]) + blockedSuffix;

        // Proper-noun localization is intentionally separate from general SetupUI
        // localization. Only languages backed by the authoritative PoE2 proper-noun
        // catalog are allowed to replace canonical game names; all other UI languages
        // fall back to the canonical English game name rather than inventing a name.
        if (!string.Equals(_currentCode, "en", StringComparison.OrdinalIgnoreCase)
            && AuthoritativeProperNounLanguages.Contains(_currentCode))
        {
            var properNouns = GetProperNounDictionary(_currentCode);
            if (properNouns.TryGetValue(value, out var translated) && !string.IsNullOrWhiteSpace(translated))
            {
                var cleaned = CleanAuthoritativeProperNoun(translated);
                if (!string.IsNullOrWhiteSpace(cleaned)) return cleaned;
            }
        }

        // Composite route labels are built from canonical PoE2 names. Translate each
        // canonical name independently so localization never changes runtime IDs.
        if (value.Contains(" + ", StringComparison.Ordinal))
            return string.Join(" + ", value.Split(" + ", StringSplitOptions.None).Select(TranslateProperNoun));

        var killMarker = " — Kill ";
        var killAt = value.LastIndexOf(killMarker, StringComparison.Ordinal);
        if (killAt > 0)
        {
            var objective = value[(killAt + killMarker.Length)..];
            return TranslateProperNoun(value[..killAt]) + " — " + Translate("Kill") + " " + TranslateProperNoun(objective);
        }

        // Synthetic route wrappers are UI text around a canonical game name. Keep the
        // wrapper localized while resolving the contained proper noun through the
        // authoritative catalog.
        const string exitPrefix = "Exit ";
        if (value.StartsWith(exitPrefix, StringComparison.Ordinal) && value.Length > exitPrefix.Length)
            return Translate("Exit") + " " + TranslateProperNoun(value[exitPrefix.Length..]);

        return value;
    }

    private static string CleanAuthoritativeProperNoun(string translated)
    {
        var value = translated.Trim();
        if (value.Length == 0) return "";

        // PoE2DB area pages can place an area heading and its metadata table inside the
        // same HTML heading container. A broad HTML text extraction then yields strings
        // such as "Localized Name Localized Name Id: G2_3a Act: 2 Connections: ...".
        // Never expose that metadata as a game name.
        var metadata = Regex.Match(value, @"\s+(?:Id|Act|Area\s+Level|Connections)\s*:",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var hadMetadata = metadata.Success;
        if (hadMetadata) value = value[..metadata.Index].Trim();

        // The affected heading shape duplicates the localized name before the metadata.
        // Collapse an exact duplicated word sequence only when the metadata signature was
        // present, avoiding any changes to legitimate repeated words in authoritative names.
        if (hadMetadata)
        {
            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts.Length % 2 == 0)
            {
                var half = parts.Length / 2;
                var first = string.Join(" ", parts.Take(half));
                var second = string.Join(" ", parts.Skip(half));
                if (string.Equals(first, second, StringComparison.Ordinal)) value = first;
            }
        }

        if (value.Length == 0 || value.Length > 160) return "";
        if (value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0) return "";
        if (Regex.IsMatch(value, @"(?:^|\s)(?:Id|Connections)\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return "";
        return value;
    }

    public static string TranslateDynamic(string english)
    {
        var exact = Translate(english);
        if (!string.Equals(exact, english, StringComparison.Ordinal)) return exact;

        // Some UI strings contain a runtime number but otherwise have a stable
        // translatable sentence. Resolve the stable format key first so Apply() can
        // re-localize the control when SetupUI language changes.
        const string unorderedBossFormat = "Dynamic/unordered boss mode: selected bosses form the eligible pool and repeated kills count as separate encounters. Default {0}, matching the current minimum campaign-required boss baseline.";
        const string unorderedBossPrefix = "Dynamic/unordered boss mode: selected bosses form the eligible pool and repeated kills count as separate encounters. Default ";
        const string unorderedBossSuffix = ", matching the current minimum campaign-required boss baseline.";
        if (english.StartsWith(unorderedBossPrefix, StringComparison.Ordinal)
            && english.EndsWith(unorderedBossSuffix, StringComparison.Ordinal)
            && english.Length >= unorderedBossPrefix.Length + unorderedBossSuffix.Length)
        {
            var countText = english.Substring(
                unorderedBossPrefix.Length,
                english.Length - unorderedBossPrefix.Length - unorderedBossSuffix.Length);
            return TranslateFormat(unorderedBossFormat, countText);
        }

        var generatedObjectives = Regex.Match(english, @"^(\d+) generated objectives$", RegexOptions.CultureInvariant);
        if (generatedObjectives.Success)
            return TranslateFormat("{0} generated objectives", generatedObjectives.Groups[1].Value);

        const string specificAreaStartPrefix = "Specific area entry auto start — ";
        if (english.StartsWith(specificAreaStartPrefix, StringComparison.Ordinal)
            && english.Length > specificAreaStartPrefix.Length)
        {
            var areaName = english[specificAreaStartPrefix.Length..];
            return Translate("Specific area entry auto start") + " — " + TranslateProperNoun(areaName);
        }

        // Translate common preview/status fragments while leaving IDs, numbers, file
        // names, and user-entered text untouched. This keeps dynamic Run Rules and
        // Route Preview text localized even when the sentence contains runtime values.
        var result = english;
        if (result.StartsWith("Sekhemas: ", StringComparison.Ordinal))
            result = Translate("Sekhemas") + result["Sekhemas".Length..];
        if (result.StartsWith("Chaos: ", StringComparison.Ordinal))
            result = Translate("Chaos") + result["Chaos".Length..];
        result = result.Replace("; Chaos: ", "; " + Translate("Chaos") + ": ", StringComparison.Ordinal);

        string[] fragments =
        {
            "Automatic", "Start", "Finish", "Completion", "Timing", "Splits", "Order", "Selected length:",
            "Dynamic / unordered", "Ordered", "Dive Number", "First Death", "Deathless mode", "terminal",
            "No death tracking", "Required", "Not required", "not entered", "not read",
            "planned untimed", "planned timed", "hard maximum", "planned dive", "planned dives",
            "finish after dive", "finalized map", "finalized maps", "Specific Pinnacle boss defeat",
            "Until first tracked death", "Manual finish hotkey", "Track all Death [x] rows; continue",
            "Track Death [x] rows", "Real map exit boundary", "Not the run endpoint",
            "Expected map boss defeated, then exit map = SUCCESS", "first Temple Dive entry",
            "Vaal Ruins setup is excluded", "first new map instance entry", "UI/policy only",
            "not generated in this iteration", "Full Trial", "First active trial chamber",
            "Boss policy", "Exit policy", "Final boss only", "Each boss kill",
            "Trial completion / exit only", "floor", "floors", "round", "rounds", "Level",
            "Vaal Ruins", "Atziri's Temple", "Royal Architect", "Atziri", "The Trialmaster",
            "No stage selected", "None (opt-in)",
            "Riverbank auto start", "Specific area entry auto start", "Manual Start",
            "AREA", "BOSS", "BOSS PAIR", "DYNAMIC BOSS", "BOSS POOL"
        };
        var properNounFragments = new HashSet<string>(StringComparer.Ordinal)
        {
            "Vaal Ruins", "Atziri's Temple", "Royal Architect", "Atziri", "The Trialmaster"
        };
        foreach (var fragment in fragments.OrderByDescending(x => x.Length))
        {
            var replacement = properNounFragments.Contains(fragment) ? TranslateProperNoun(fragment) : Translate(fragment);
            if (!string.Equals(replacement, fragment, StringComparison.Ordinal))
                result = result.Replace(fragment, replacement, StringComparison.Ordinal);
        }
        return result;
    }

    public static void SetDynamicText(Control control, string english)
    {
        var state = ControlStates.GetOrCreateValue(control);
        state.OriginalText = english;
        var translated = TranslateDynamic(english);
        control.Text = translated;
        state.LastAppliedText = translated;
    }

    public static void SetProperNounText(Control control, string english)
    {
        var state = ControlStates.GetOrCreateValue(control);
        state.OriginalText = english;
        var translated = TranslateProperNoun(english);
        control.Text = translated;
        state.LastAppliedText = translated;
    }

    public static void Apply(Control root)
    {
        if (_isApplying) return;
        _isApplying = true;
        try
        {
            ApplyControl(root);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private static void ApplyControl(Control control)
    {
        var state = ControlStates.GetOrCreateValue(control);
        // TextBox.Text is user/runtime data (paths, filters, character names). Never translate it.
        // TextBox.PlaceholderText is UI chrome and is localized separately below.
        if (control is not TextBox)
        {
            if (string.IsNullOrEmpty(state.OriginalText) && !string.IsNullOrEmpty(control.Text))
            {
                state.OriginalText = control.Text;
            }
            else if (!string.IsNullOrEmpty(control.Text) && control.Text != state.LastAppliedText && control.Text != state.OriginalText)
            {
                // A dynamic UI update wrote a fresh English value after localization was applied.
                state.OriginalText = control.Text;
            }

            if (!string.IsNullOrEmpty(state.OriginalText))
            {
                var translated = TranslateDynamic(state.OriginalText);
                control.Text = translated;
                state.LastAppliedText = translated;
            }
        }

        if (control is TextBox textBox)
        {
            if (string.IsNullOrEmpty(state.OriginalPlaceholder) && !string.IsNullOrEmpty(textBox.PlaceholderText))
                state.OriginalPlaceholder = textBox.PlaceholderText;
            else if (!string.IsNullOrEmpty(textBox.PlaceholderText) && textBox.PlaceholderText != state.LastAppliedPlaceholder && textBox.PlaceholderText != state.OriginalPlaceholder)
                state.OriginalPlaceholder = textBox.PlaceholderText;

            if (!string.IsNullOrEmpty(state.OriginalPlaceholder))
            {
                var translatedPlaceholder = Translate(state.OriginalPlaceholder);
                textBox.PlaceholderText = translatedPlaceholder;
                state.LastAppliedPlaceholder = translatedPlaceholder;
            }
        }

        if (control is ComboBox comboBox)
            ApplyListItems(comboBox);
        else if (control is CheckedListBox checkedListBox)
            ApplyCheckedListItems(checkedListBox);
        else if (control is ListBox listBox)
            ApplyListBoxItems(listBox);

        if (control is DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                var colState = ColumnStates.GetOrCreateValue(column);
                if (string.IsNullOrEmpty(colState.OriginalText)) colState.OriginalText = column.HeaderText;
                else if (column.HeaderText != colState.LastAppliedText && column.HeaderText != colState.OriginalText) colState.OriginalText = column.HeaderText;
                var translated = Translate(colState.OriginalText);
                column.HeaderText = translated;
                colState.LastAppliedText = translated;
            }
        }

        foreach (Control child in control.Controls)
            ApplyControl(child);
    }

    private static void ApplyListItems(ComboBox combo)
    {
        var state = ListStates.GetOrCreateValue(combo);
        CaptureListSourceIfChanged(combo.Items.Cast<object?>().ToList(), state);

        var selected = combo.SelectedIndex;
        EnsureLastAppliedSize(state);
        for (var i = 0; i < state.Originals.Count; i++)
        {
            if (state.Originals[i] is string original)
            {
                var translated = TranslateDynamic(original);
                if (!string.Equals(combo.Items[i]?.ToString(), translated, StringComparison.Ordinal))
                    combo.Items[i] = translated;
                state.LastApplied[i] = translated;
            }
            else
            {
                state.LastApplied[i] = null;
            }
        }
        if (selected >= 0 && selected < combo.Items.Count) combo.SelectedIndex = selected;
    }

    private static void ApplyListBoxItems(ListBox list)
    {
        var state = ListStates.GetOrCreateValue(list);
        CaptureListSourceIfChanged(list.Items.Cast<object?>().ToList(), state);

        var selected = list.SelectedIndex;
        EnsureLastAppliedSize(state);
        for (var i = 0; i < state.Originals.Count; i++)
        {
            if (state.Originals[i] is string original)
            {
                var translated = TranslateDynamic(original);
                if (!string.Equals(list.Items[i]?.ToString(), translated, StringComparison.Ordinal))
                    list.Items[i] = translated;
                state.LastApplied[i] = translated;
            }
            else
            {
                state.LastApplied[i] = null;
            }
        }
        if (selected >= 0 && selected < list.Items.Count) list.SelectedIndex = selected;
    }

    private static void ApplyCheckedListItems(CheckedListBox list)
    {
        var state = ListStates.GetOrCreateValue(list);
        CaptureListSourceIfChanged(list.Items.Cast<object?>().ToList(), state);

        var checks = Enumerable.Range(0, list.Items.Count).Select(list.GetItemChecked).ToArray();
        var selected = list.SelectedIndex;
        EnsureLastAppliedSize(state);
        for (var i = 0; i < state.Originals.Count; i++)
        {
            if (state.Originals[i] is string original)
            {
                var translated = TranslateDynamic(original);
                if (!string.Equals(list.Items[i]?.ToString(), translated, StringComparison.Ordinal))
                    list.Items[i] = translated;
                state.LastApplied[i] = translated;
            }
            else
            {
                state.LastApplied[i] = null;
            }
        }
        for (var i = 0; i < checks.Length && i < list.Items.Count; i++) list.SetItemChecked(i, checks[i]);
        if (selected >= 0 && selected < list.Items.Count) list.SelectedIndex = selected;
    }

    private static void CaptureListSourceIfChanged(IReadOnlyList<object?> currentItems, StringItemsState state)
    {
        var recapture = state.OriginalCount != currentItems.Count;
        if (!recapture && state.Originals.Count == currentItems.Count)
        {
            EnsureLastAppliedSize(state);
            for (var i = 0; i < currentItems.Count; i++)
            {
                if (state.Originals[i] is not string original) continue;
                var current = currentItems[i]?.ToString();
                var lastApplied = state.LastApplied[i];
                if (!string.Equals(current, original, StringComparison.Ordinal)
                    && !string.Equals(current, lastApplied, StringComparison.Ordinal))
                {
                    recapture = true;
                    break;
                }
            }
        }

        if (!recapture) return;
        state.Originals.Clear();
        state.LastApplied.Clear();
        foreach (var item in currentItems)
        {
            state.Originals.Add(item);
            state.LastApplied.Add(null);
        }
        state.OriginalCount = currentItems.Count;
    }

    private static void EnsureLastAppliedSize(StringItemsState state)
    {
        while (state.LastApplied.Count < state.Originals.Count) state.LastApplied.Add(null);
        if (state.LastApplied.Count > state.Originals.Count)
            state.LastApplied.RemoveRange(state.Originals.Count, state.LastApplied.Count - state.Originals.Count);
    }

    private static Dictionary<string, string> GetDictionary(string code)
    {
        if (string.Equals(code, "en", StringComparison.OrdinalIgnoreCase))
            return new Dictionary<string, string>(StringComparer.Ordinal);
        if (Cache.TryGetValue(code, out var cached)) return cached;

        var assembly = Assembly.GetExecutingAssembly();
        var suffix = $".Locales.{code}.json";
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);
            Cache[code] = empty;
            return empty;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);
            Cache[code] = empty;
            return empty;
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        }) ?? new Dictionary<string, string>();
        var dict = new Dictionary<string, string>(parsed, StringComparer.Ordinal);
        Cache[code] = dict;
        return dict;
    }
    private static Dictionary<string, string> GetProperNounDictionary(string code)
    {
        if (string.Equals(code, "en", StringComparison.OrdinalIgnoreCase)
            || !AuthoritativeProperNounLanguages.Contains(code))
            return new Dictionary<string, string>(StringComparer.Ordinal);
        if (ProperNounCache.TryGetValue(code, out var cached)) return cached;

        var assembly = Assembly.GetExecutingAssembly();
        var suffix = $".ProperNouns.{code}.json";
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);
            ProperNounCache[code] = empty;
            return empty;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);
            ProperNounCache[code] = empty;
            return empty;
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        }) ?? new Dictionary<string, string>();
        var dict = new Dictionary<string, string>(parsed, StringComparer.Ordinal);
        ProperNounCache[code] = dict;
        return dict;
    }

}
