using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Loqui.Editor
{
    public static class LocalizationCSharpScanner
    {
        private const string ReceiverPattern =
            @"(?<receiver>[A-Za-z_][A-Za-z0-9_]*(?:\s*\[[^\]]+\])?" +
            @"(?:\.[A-Za-z_][A-Za-z0-9_]*(?:\s*\[[^\]]+\])?)*)";

        private static readonly Regex AssignPattern = new(
            ReceiverPattern + @"\.text\s*(?<op>=|\+=)\s*(?<value>[^;\r\n]+)",
            RegexOptions.Compiled);

        private static readonly Regex SetTextPattern = new(
            ReceiverPattern + @"\.SetText\s*\(\s*(?<value>[^,\)\r\n]+)",
            RegexOptions.Compiled);

        public static void ExtractCandidates(string sourceText, string filePath, List<LocalizationScanItem> buffer)
        {
            if (string.IsNullOrEmpty(sourceText) || buffer == null)
            {
                return;
            }

            var scannable = StripCommentsPreservingLayout(sourceText);
            AddMatches(AssignPattern, scannable, filePath, buffer);
            AddMatches(SetTextPattern, scannable, filePath, buffer);
        }

        private static string StripCommentsPreservingLayout(string source)
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

                if (c == '@' && i + 1 < n && source[i + 1] == '"')
                {
                    sb.Append('@');
                    sb.Append('"');
                    i += 2;
                    while (i < n)
                    {
                        if (source[i] == '"')
                        {
                            if (i + 1 < n && source[i + 1] == '"')
                            {
                                sb.Append("\"\"");
                                i += 2;
                                continue;
                            }

                            sb.Append('"');
                            i++;
                            break;
                        }

                        sb.Append(source[i]);
                        i++;
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

        private static void AddMatches(Regex pattern, string sourceText, string filePath, List<LocalizationScanItem> buffer)
        {
            foreach (Match match in pattern.Matches(sourceText))
            {
                var expression = match.Groups["value"].Value;
                var hasLiteral = TryReadSimpleStringLiteral(expression, out var literal);
                var receiver = match.Groups["receiver"].Value;
                var lineNumber = GetLineNumber(sourceText, match.Index);
                var group = GroupFromPath(filePath);

                buffer.Add(new LocalizationScanItem
                {
                    Source = LocalizationScanSource.CSharpLiteral,
                    AssetPath = filePath,
                    LineNumber = lineNumber,
                    ContainerName = filePath,
                    ContainerKind = "Script",
                    ComponentType = "CSharpLiteral",
                    EnglishSource = literal,
                    ProposedKey = hasLiteral ? LocalizationKeyGenerator.Generate(group, literal) : string.Empty,
                    Group = group,
                    PlatformDefault = literal,
                    IsCandidate = hasLiteral,
                    ExclusionReason = hasLiteral ? string.Empty : "Advisory: code mutates text from a non-literal expression",
                    RecommendedApproach = LocalizationRecommendedApproaches.CodeApi,
                    CodeMutatorHint = NormalizeReceiver(receiver),
                    MutationEvidence = BuildEvidence(filePath, lineNumber, receiver),
                    RequiresReview = true,
                    Context = "C# text mutator. Localize at the call site or presenter/view-data owner."
                });
            }
        }

        private static bool TryReadSimpleStringLiteral(string expression, out string literal)
        {
            literal = string.Empty;
            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            expression = expression.TrimStart();
            if (expression.Length < 2 || expression[0] != '"')
            {
                return false;
            }

            var escaped = false;
            for (var i = 1; i < expression.Length; i++)
            {
                var c = expression[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    literal = expression.Substring(1, i - 1);
                    return !string.IsNullOrWhiteSpace(literal);
                }
            }

            return false;
        }

        private static int GetLineNumber(string sourceText, int index)
        {
            var line = 1;
            var limit = Math.Min(index, sourceText.Length);
            for (var i = 0; i < limit; i++)
            {
                if (sourceText[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }

        private static string BuildEvidence(string filePath, int lineNumber, string receiver)
        {
            return string.IsNullOrEmpty(receiver)
                ? filePath + ":" + lineNumber.ToString()
                : filePath + ":" + lineNumber.ToString() + " " + receiver;
        }

        private static string GroupFromPath(string filePath)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return string.IsNullOrEmpty(name) ? string.Empty : name.ToLowerInvariant();
        }

        private static string NormalizeReceiver(string receiver)
        {
            if (string.IsNullOrEmpty(receiver))
            {
                return string.Empty;
            }

            var dot = receiver.LastIndexOf('.');
            if (dot >= 0 && dot + 1 < receiver.Length)
            {
                receiver = receiver.Substring(dot + 1);
            }

            var bracket = receiver.IndexOf('[');
            if (bracket >= 0)
            {
                receiver = receiver.Substring(0, bracket);
            }

            return receiver.Trim();
        }
    }
}
