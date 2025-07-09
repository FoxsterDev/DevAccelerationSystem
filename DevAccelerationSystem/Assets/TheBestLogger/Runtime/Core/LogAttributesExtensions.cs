using TheBestLogger.Core.Utilities;

namespace TheBestLogger
{
    internal static class LogAttributesExtensions
    {
        public static string ToRegularString(this LogAttributes attributes, bool includeImportance = false, bool includeStackTrace = false)
        {
            using var sb = StringOperations.CreateStringBuilder(512, false);
            sb.AppendLine("\n[LogAttributes]");
            if (includeImportance)
            {
                sb.AppendLine($"  Importance: {attributes.LogImportance}");
            }

            if (attributes.Tags != null && attributes.Tags.Length > 0)
            {
                sb.Append("  Tags: ");
                var first = true;
                foreach (var tag in attributes.Tags)
                {
                    if (!first)
                        sb.Append(", ");
                    sb.Append(tag);
                    first = false;
                }
                sb.AppendLine();
            }

            if (attributes.Props != null && attributes.Props.Count > 0)
            {
                sb.AppendLine("  Props:");
                foreach (var kvp in attributes.Props)
                {
                    sb.AppendLine($"   - {kvp.Key}: {kvp.Value}");
                }
            }

            if (attributes.UnityContextObject != null)
            {
                sb.AppendLine($"  Context: {attributes.UnityContextObject.name} ({attributes.UnityContextObject.GetType().Name})");
            }

            if (includeStackTrace)
            {
                if (!string.IsNullOrEmpty(attributes.StackTrace))
                {
                    sb.AppendLine("  StackTrace: (trimmed)\n" + attributes.StackTrace.Split('\n')[0] + " ...");
                }
            }

            return sb.ToString();
        }

        public static string ToFlatString(this LogAttributes attributes, bool includeImportance = false, bool includeStackTrace = false)
        {
            using var sb = StringOperations.CreateStringBuilder(512, false);
            sb.Append($" [LogAttributes]");
            if (includeImportance)
            {
                sb.Append(" Importance: {attributes.LogImportance}");
            }

            if (attributes.Tags != null && attributes.Tags.Length > 0)
            {
                sb.Append(" Tags: ");
                var first = true;
                foreach (var tag in attributes.Tags)
                {
                    if (!first)
                        sb.Append(", ");
                    sb.Append(tag);
                    first = false;
                }
            }

            if (attributes.Props != null && attributes.Props.Count > 0)
            {
                sb.Append(" Props:");
                foreach (var kvp in attributes.Props)
                {
                    sb.Append($" - {kvp.Key}: {kvp.Value}");
                }
            }

            if (attributes.UnityContextObject != null)
            {
                sb.Append($" Context: {attributes.UnityContextObject.name} ({attributes.UnityContextObject.GetType().Name})");
            }

            if (includeStackTrace)
            {
                if (!string.IsNullOrEmpty(attributes.StackTrace))
                {
                    sb.Append(" StackTrace: (trimmed)\n" + attributes.StackTrace.Split('\n')[0] + " ...");
                }
            }
            return sb.ToString();
        }
    }
}
