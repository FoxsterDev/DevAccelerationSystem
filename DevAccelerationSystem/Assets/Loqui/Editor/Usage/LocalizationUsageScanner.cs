using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Loqui.Editor
{
    public enum LocalizationUsageView
    {
        ByKey,
        ByModule,
        ByGroup,
        GenericApi
    }

    public sealed class LocalizationUsageSite
    {
        public string FileName;
        public string AssetPath;
        public int Line;
        public string Module;
        public string Snippet;
    }

    public sealed class LocalizationKeyUsage
    {
        public string Key;
        public string Group;
        public string English;
        public bool IsBool;
        public bool HasOverride;
        public readonly List<LocalizationUsageSite> Sites = new();
        public readonly SortedSet<string> Modules = new(StringComparer.Ordinal);
        public readonly SortedSet<string> Files = new(StringComparer.Ordinal);

        public int CallCount => Sites.Count;
        public bool Used => Sites.Count > 0;
    }

    public sealed class LocalizationUsageReport
    {
        public bool Completed;
        public string Status = "Not scanned yet.";
        public readonly List<LocalizationKeyUsage> Keys = new();
        public readonly List<LocalizationUsageSite> Generic = new();
        public readonly SortedSet<string> Modules = new(StringComparer.Ordinal);
        public int TotalCalls;
        public int UsedCount;
        public int CatalogKeyCount;
        public int OverrideCount;

        public int UnusedCount => Mathf.Max(0, CatalogKeyCount - UsedCount);
    }

    /// <summary>
    /// On-demand, incremental project scan of localization key usage. Driven a batch at a time from
    /// <see cref="Step"/> so the editor never blocks. Resolves literal keys, `const string` key
    /// references, and prefab/scene <see cref="LocalizedText"/> bindings.
    /// </summary>
    public sealed class LocalizationUsageScanner
    {
        const int FilesPerStep = 40;

        static readonly Regex CallRegex = new(@"\bLoc\s*\.\s*(?:Get|TryGet|GetBool|TryGetBool)\s*\(", RegexOptions.Compiled);
        static readonly Regex ConstRegex = new(@"(?:const|static\s+readonly)\s+string\s+(\w+)\s*=\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);

        readonly struct PendingCall
        {
            public readonly string Module;
            public readonly string FileName;
            public readonly string AssetPath;
            public readonly int Line;
            public readonly bool IsLiteral;
            public readonly string LiteralKey;
            public readonly string IdentifierName;
            public readonly string Snippet;

            public PendingCall(string module, string fileName, string assetPath, int line, bool isLiteral, string literalKey, string identifierName, string snippet)
            {
                Module = module;
                FileName = fileName;
                AssetPath = assetPath;
                Line = line;
                IsLiteral = isLiteral;
                LiteralKey = literalKey;
                IdentifierName = identifierName;
                Snippet = snippet;
            }
        }

        string[] _files;
        int _index;
        List<(string dir, string name)> _asmdefs;
        string _localizedTextGuid;
        readonly Dictionary<string, string> _consts = new(StringComparer.Ordinal);
        readonly List<PendingCall> _calls = new();
        LocalizationCatalog _catalog;

        public bool IsRunning { get; private set; }
        public float Progress { get; private set; }
        public LocalizationUsageReport Report { get; private set; }

        public void Begin(LocalizationCatalog catalog)
        {
            _catalog = catalog;
            _consts.Clear();
            _calls.Clear();
            _index = 0;
            Progress = 0f;
            Report = new LocalizationUsageReport { Status = "Scanning…" };

            _asmdefs = CollectAsmdefs();
            _localizedTextGuid = FindLocalizedTextGuid();

            var assetsRoot = Application.dataPath;
            var cs = Directory.GetFiles(assetsRoot, "*.cs", SearchOption.AllDirectories);
            var prefabs = Directory.GetFiles(assetsRoot, "*.prefab", SearchOption.AllDirectories);
            var scenes = Directory.GetFiles(assetsRoot, "*.unity", SearchOption.AllDirectories);
            var all = new List<string>(cs.Length + prefabs.Length + scenes.Length);
            all.AddRange(cs);
            all.AddRange(prefabs);
            all.AddRange(scenes);
            _files = all.ToArray();

            IsRunning = true;
        }

        public void Cancel()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            if (Report != null)
            {
                Report.Status = "Scan stopped.";
            }
        }

        public void Step()
        {
            if (!IsRunning || _files == null)
            {
                return;
            }

            var end = Mathf.Min(_index + FilesPerStep, _files.Length);
            for (; _index < end; _index++)
            {
                try
                {
                    ScanFile(_files[_index]);
                }
                catch
                {
                    // a single unreadable/locked file must not abort the scan
                }
            }

            Progress = _files.Length == 0 ? 1f : (float)_index / _files.Length;
            if (_index >= _files.Length)
            {
                Finish();
            }
        }

        void ScanFile(string absolutePath)
        {
            var assetPath = ToAssetPath(absolutePath);
            var ext = Path.GetExtension(absolutePath);
            if (ext == ".cs")
            {
                ScanCSharp(absolutePath, assetPath);
            }
            else
            {
                ScanYaml(absolutePath, assetPath);
            }
        }

        void ScanCSharp(string absolutePath, string assetPath)
        {
            var raw = File.ReadAllText(absolutePath);
            var text = StripComments(raw);
            var module = ModuleFor(absolutePath);
            var fileName = Path.GetFileName(absolutePath);

            foreach (Match constMatch in ConstRegex.Matches(text))
            {
                _consts[constMatch.Groups[1].Value] = constMatch.Groups[2].Value;
            }

            foreach (Match call in CallRegex.Matches(text))
            {
                var parenIndex = call.Index + call.Length - 1;
                ReadFirstArgument(text, parenIndex, out var isLiteral, out var literal, out var identifier);
                var line = LineNumber(text, call.Index);
                _calls.Add(new PendingCall(module, fileName, assetPath, line, isLiteral, literal, identifier, null));
            }
        }

        void ScanYaml(string absolutePath, string assetPath)
        {
            if (string.IsNullOrEmpty(_localizedTextGuid))
            {
                return;
            }

            var text = File.ReadAllText(absolutePath);
            if (text.IndexOf(_localizedTextGuid, StringComparison.Ordinal) < 0)
            {
                return;
            }

            var module = Path.GetExtension(absolutePath) == ".unity" ? "(scene)" : "(prefab)";
            var fileName = Path.GetFileName(absolutePath);

            var blocks = text.Split(new[] { "--- !u!" }, StringSplitOptions.None);
            foreach (var block in blocks)
            {
                if (block.IndexOf(_localizedTextGuid, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var m = Regex.Match(block, @"_key:\s*(.*)");
                if (!m.Success)
                {
                    continue;
                }

                var key = m.Groups[1].Value.Trim().Trim('"', '\'');
                if (!string.IsNullOrEmpty(key))
                {
                    _calls.Add(new PendingCall(module, fileName, assetPath, 0, true, key, null, "LocalizedText"));
                }
            }
        }

        void Finish()
        {
            var report = Report;
            var byKey = new Dictionary<string, LocalizationKeyUsage>(StringComparer.Ordinal);

            if (_catalog != null)
            {
                if (_catalog.Texts != null)
                {
                    foreach (var entry in _catalog.Texts)
                    {
                        if (entry != null && !string.IsNullOrEmpty(entry.Key) && !byKey.ContainsKey(entry.Key))
                        {
                            var usage = new LocalizationKeyUsage
                            {
                                Key = entry.Key,
                                Group = entry.Group,
                                English = entry.EnglishFallback,
                                IsBool = false,
                                HasOverride = TextEntryHasOverride(entry)
                            };
                            byKey[entry.Key] = usage;
                            report.Keys.Add(usage);
                        }
                    }
                }

                if (_catalog.Bools != null)
                {
                    foreach (var entry in _catalog.Bools)
                    {
                        if (entry != null && !string.IsNullOrEmpty(entry.Key) && !byKey.ContainsKey(entry.Key))
                        {
                            var usage = new LocalizationKeyUsage
                            {
                                Key = entry.Key,
                                Group = "(bool)",
                                English = entry.Values != null ? entry.Values.Default.ToString() : null,
                                IsBool = true,
                                HasOverride = BoolEntryHasOverride(entry)
                            };
                            byKey[entry.Key] = usage;
                            report.Keys.Add(usage);
                        }
                    }
                }
            }

            report.CatalogKeyCount = report.Keys.Count;

            foreach (var call in _calls)
            {
                string key = null;
                if (call.IsLiteral)
                {
                    key = call.LiteralKey;
                }
                else if (!string.IsNullOrEmpty(call.IdentifierName) && _consts.TryGetValue(call.IdentifierName, out var resolved))
                {
                    key = resolved;
                }

                if (key != null && byKey.TryGetValue(key, out var usage))
                {
                    usage.Sites.Add(new LocalizationUsageSite
                    {
                        FileName = call.FileName,
                        AssetPath = call.AssetPath,
                        Line = call.Line,
                        Module = call.Module,
                        Snippet = call.Snippet
                    });
                    usage.Modules.Add(call.Module);
                    usage.Files.Add(call.FileName);
                    report.Modules.Add(call.Module);
                    report.TotalCalls++;
                }
                else if (key == null)
                {
                    report.Generic.Add(new LocalizationUsageSite
                    {
                        FileName = call.FileName,
                        AssetPath = call.AssetPath,
                        Line = call.Line,
                        Module = call.Module,
                        Snippet = call.IdentifierName
                    });
                    report.Modules.Add(call.Module);
                }
            }

            foreach (var usage in report.Keys)
            {
                if (usage.Used)
                {
                    report.UsedCount++;
                }

                if (usage.HasOverride)
                {
                    report.OverrideCount++;
                }
            }

            report.Keys.Sort((a, b) => b.CallCount != a.CallCount ? b.CallCount.CompareTo(a.CallCount) : string.CompareOrdinal(a.Key, b.Key));
            report.Completed = true;
            report.Status = "Scan complete";
            IsRunning = false;
            Progress = 1f;
        }

        static bool TextEntryHasOverride(LocalizationEntry entry)
        {
            if (entry.Languages == null)
            {
                return false;
            }

            if (entry.Languages.Count > 1)
            {
                return true;
            }

            foreach (var lang in entry.Languages)
            {
                var values = lang != null ? lang.Values : null;
                if (values == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(values.IOS) && values.IOS != values.Default)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(values.Android) && values.Android != values.Default)
                {
                    return true;
                }
            }

            return false;
        }

        static bool BoolEntryHasOverride(LocalizationBoolEntry entry)
        {
            return entry.Values != null &&
                   (entry.Values.IOS != LocalizationBoolOverride.Inherit || entry.Values.Android != LocalizationBoolOverride.Inherit);
        }

        static void ReadFirstArgument(string text, int parenIndex, out bool isLiteral, out string literal, out string identifier)
        {
            isLiteral = false;
            literal = null;
            identifier = null;

            var i = parenIndex + 1;
            var n = text.Length;
            while (i < n && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (i >= n)
            {
                return;
            }

            var c = text[i];
            if (c == '"')
            {
                isLiteral = true;
                literal = ReadStringLiteral(text, i + 1, false);
                return;
            }

            if (c == '@' && i + 1 < n && text[i + 1] == '"')
            {
                isLiteral = true;
                literal = ReadStringLiteral(text, i + 2, true);
                return;
            }

            if (c == '$')
            {
                return; // interpolated → dynamic
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.'))
                {
                    i++;
                }

                var token = text.Substring(start, i - start);
                var dot = token.LastIndexOf('.');
                identifier = dot >= 0 ? token.Substring(dot + 1) : token;
            }
        }

        static string ReadStringLiteral(string text, int start, bool verbatim)
        {
            var sb = new StringBuilder();
            var i = start;
            var n = text.Length;
            while (i < n)
            {
                var c = text[i];
                if (verbatim)
                {
                    if (c == '"')
                    {
                        if (i + 1 < n && text[i + 1] == '"')
                        {
                            sb.Append('"');
                            i += 2;
                            continue;
                        }

                        break;
                    }

                    sb.Append(c);
                    i++;
                    continue;
                }

                if (c == '\\' && i + 1 < n)
                {
                    sb.Append(c);
                    sb.Append(text[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == '"')
                {
                    break;
                }

                sb.Append(c);
                i++;
            }

            // unescape the common sequences so the literal matches the catalog key text
            return sb.ToString().Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        static string StripComments(string source)
        {
            var sb = new StringBuilder(source.Length);
            var i = 0;
            var n = source.Length;
            while (i < n)
            {
                var c = source[i];
                if (c == '/' && i + 1 < n && source[i + 1] == '/')
                {
                    while (i < n && source[i] != '\n')
                    {
                        sb.Append(source[i] == '\r' ? '\r' : ' ');
                        i++;
                    }

                    continue;
                }

                if (c == '/' && i + 1 < n && source[i + 1] == '*')
                {
                    sb.Append("  ");
                    i += 2;
                    while (i < n && !(source[i] == '*' && i + 1 < n && source[i + 1] == '/'))
                    {
                        sb.Append(source[i] == '\n' ? '\n' : source[i] == '\r' ? '\r' : ' ');
                        i++;
                    }

                    if (i < n)
                    {
                        sb.Append("  ");
                        i += 2;
                    }

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    var quote = c;
                    sb.Append(quote);
                    i++;
                    while (i < n)
                    {
                        var d = source[i];
                        sb.Append(d);
                        i++;
                        if (d == '\\' && i < n)
                        {
                            sb.Append(source[i]);
                            i++;
                            continue;
                        }

                        if (d == quote)
                        {
                            break;
                        }
                    }

                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        static int LineNumber(string text, int index)
        {
            var line = 1;
            var limit = Mathf.Min(index, text.Length);
            for (var i = 0; i < limit; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }

        static List<(string dir, string name)> CollectAsmdefs()
        {
            var list = new List<(string dir, string name)>();
            foreach (var guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asmdef = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
                var name = Path.GetFileNameWithoutExtension(path);
                if (asmdef != null)
                {
                    var parsed = JsonUtility.FromJson<AsmdefName>(asmdef.text);
                    if (parsed != null && !string.IsNullOrEmpty(parsed.name))
                    {
                        name = parsed.name;
                    }
                }

                var dir = Path.GetDirectoryName(path)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(dir))
                {
                    list.Add((dir, name));
                }
            }

            list.Sort((a, b) => b.dir.Length.CompareTo(a.dir.Length));
            return list;
        }

        string ModuleFor(string absolutePath)
        {
            var assetDir = Path.GetDirectoryName(ToAssetPath(absolutePath))?.Replace("\\", "/") ?? string.Empty;
            foreach (var (dir, name) in _asmdefs)
            {
                if (assetDir == dir || assetDir.StartsWith(dir + "/", StringComparison.Ordinal))
                {
                    return name;
                }
            }

            return "Assembly-CSharp";
        }

        static string FindLocalizedTextGuid()
        {
            foreach (var guid in AssetDatabase.FindAssets("LocalizedText t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "LocalizedText")
                {
                    return AssetDatabase.AssetPathToGUID(path);
                }
            }

            return null;
        }

        static string ToAssetPath(string absolutePath)
        {
            var normalized = absolutePath.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            return normalized.StartsWith(dataPath, StringComparison.Ordinal)
                ? "Assets" + normalized.Substring(dataPath.Length)
                : normalized;
        }

        [Serializable]
        private sealed class AsmdefName
        {
            public string name;
        }
    }
}
