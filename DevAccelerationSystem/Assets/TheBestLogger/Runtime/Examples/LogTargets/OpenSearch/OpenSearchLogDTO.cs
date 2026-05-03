using System;
using System.Runtime.InteropServices;
using System.Text;
using TheBestLogger.Core.Utilities;
using UnityEngine;
#if THEBESTLOGGER_ZSTRING_ENABLED
using Cysharp.Text;
#endif

namespace TheBestLogger.Examples.LogTargets
{
    /// <summary>
    /// Explicit marker for DTOs that support the custom low-allocation batch serializer.
    /// DTOs without this interface are serialized through Unity JsonUtility on batch path.
    /// </summary>
    public interface IOpenSearchBatchJsonSerializable
    {
        void WriteJson(ref OpenSearchPayloadBuilder sb);
    }

    [Serializable]
    public class OpenSearchLogDTO
    {
        public string GameVersion;
        public string UUID;
        public string DeviceModel;
        public string OS;
        public string Platform;
        public string LogLevel;
        public string Category;
        public string Message;

        public string Stacktrace;

        //default key used as @timestamp, but you can reconfigure it in index dashboards of ipensearch
        public string TimeUTC;
        public string Attributes;
        public bool DebugMode;
        public string[] Tags;

        public virtual void PrepareForJsonSerialization()
        {
            if (this is ISerializationCallbackReceiver serializationCallbackReceiver)
            {
                serializationCallbackReceiver.OnBeforeSerialize();
            }
        }
    }

    /// <summary>
    /// Consumer base class for DTOs that want custom low-allocation batch serialization.
    /// Derive from this class only when you need the manual batch writer path.
    /// </summary>
    [Serializable]
    public abstract class OpenSearchBatchCompatibleLogDTO : OpenSearchLogDTO, IOpenSearchBatchJsonSerializable
    {
        private const string GameVersionField = "\"" + nameof(GameVersion) + "\":";
        private const string UUIDField = "\"" + nameof(UUID) + "\":";
        private const string DeviceModelField = "\"" + nameof(DeviceModel) + "\":";
        private const string OSField = "\"" + nameof(OS) + "\":";
        private const string PlatformField = "\"" + nameof(Platform) + "\":";
        private const string LogLevelField = "\"" + nameof(LogLevel) + "\":";
        private const string CategoryField = "\"" + nameof(Category) + "\":";
        private const string MessageField = "\"" + nameof(Message) + "\":";
        private const string StacktraceField = "\"" + nameof(Stacktrace) + "\":";
        private const string TimeUTCField = "\"" + nameof(TimeUTC) + "\":";
        private const string AttributesField = "\"" + nameof(Attributes) + "\":";
        private const string DebugModeField = "\"" + nameof(DebugMode) + "\":";
        private const string TagsField = "\"" + nameof(Tags) + "\":";

        public void WriteJson(ref OpenSearchPayloadBuilder sb)
        {
            var wroteField = false;
            sb.Append('{');
            var writer = new OpenSearchObjectWriter(ref sb, ref wroteField);
            WriteBaseFields(ref writer);
            WriteAdditionalFields(ref writer);
            sb.Append('}');
        }

        protected virtual void WriteBaseFields(ref OpenSearchObjectWriter writer)
        {
            writer.WriteStringFieldLiteral(GameVersionField, GameVersion);
            writer.WriteStringFieldLiteral(UUIDField, UUID);
            writer.WriteStringFieldLiteral(DeviceModelField, DeviceModel);
            writer.WriteStringFieldLiteral(OSField, OS);
            writer.WriteStringFieldLiteral(PlatformField, Platform);
            writer.WriteStringFieldLiteral(LogLevelField, LogLevel);
            writer.WriteStringFieldLiteral(CategoryField, Category);
            writer.WriteStringFieldLiteral(MessageField, Message);
            writer.WriteStringFieldLiteral(StacktraceField, Stacktrace);
            writer.WriteStringFieldLiteral(TimeUTCField, TimeUTC);
            writer.WriteStringFieldLiteral(AttributesField, Attributes);
            writer.WriteBooleanFieldLiteral(DebugModeField, DebugMode);
            writer.WriteStringArrayFieldLiteral(TagsField, Tags);
        }

        /// <summary>
        /// Write every custom serializable field that should be present in OpenSearch documents for batch path.
        /// Use writer.WriteStringField / WriteBooleanField / WriteStringArrayField and similar helpers here.
        /// </summary>
        protected abstract void WriteAdditionalFields(ref OpenSearchObjectWriter writer);
    }

    [Serializable]
    internal sealed class DefaultOpenSearchBatchCompatibleLogDTO : OpenSearchBatchCompatibleLogDTO
    {
        protected override void WriteAdditionalFields(ref OpenSearchObjectWriter writer)
        {
        }
    }

    [Serializable]
    public sealed class GameSessionOpenSearchLogDTOExample : OpenSearchBatchCompatibleLogDTO, ISerializationCallbackReceiver
    {
        public string SessionToken;
        public string
#if RELEASE
            ServerType = "Prod";
#else
            ServerType = "Dev";
#endif
        public string PlayerId;
        public string SharedAppId;
        public string AppId;

        private static string _playerId;
        private static string _sessionToken;
        private static string _sharedAppId;
        private static string _appId;
        private static string _uuid;

        public static void SetAppId(string gameId, string uuid)
        {
            _appId = gameId;
            _uuid = uuid;
        }

        public static void SetPlayerIdAndSession(string playerId, string sessionToken)
        {
            _playerId = playerId;
            _sessionToken = sessionToken;
        }

        public static void SetSharedAppId(string sharedAppId)
        {
            _sharedAppId = sharedAppId;
        }

        public void OnBeforeSerialize()
        {
            PlayerId = _playerId;
            SessionToken = _sessionToken;
            SharedAppId = _sharedAppId;
            AppId = _appId;
            UUID = _uuid;
        }

        public void OnAfterDeserialize()
        {
        }

        protected override void WriteAdditionalFields(ref OpenSearchObjectWriter writer)
        {
            writer.WriteStringField(nameof(SessionToken), SessionToken);
            writer.WriteStringField(nameof(ServerType), ServerType);
            writer.WriteStringField(nameof(PlayerId), PlayerId);
            writer.WriteStringField(nameof(SharedAppId), SharedAppId);
            writer.WriteStringField(nameof(AppId), AppId);
        }
    }

    /// <summary>
    /// Example of a consumer DTO that relies on plain Unity JsonUtility serialization only.
    /// Additional fields still work on single-log path, and batch path falls back to JsonUtility
    /// when the target detects that this DTO does not implement IOpenSearchBatchJsonSerializable.
    /// </summary>
    [Serializable]
    public sealed class GameSessionOpenSearchLogDTOJsonUtilityFallbackExample : OpenSearchLogDTO, ISerializationCallbackReceiver
    {
        public string SessionToken;
        public string PlayerId;

        private static string _playerId;
        private static string _sessionToken;

        public static void SetPlayerIdAndSession(string playerId, string sessionToken)
        {
            _playerId = playerId;
            _sessionToken = sessionToken;
        }

        public void OnBeforeSerialize()
        {
            PlayerId = _playerId;
            SessionToken = _sessionToken;
        }

        public void OnAfterDeserialize()
        {
        }
    }

    public ref struct OpenSearchObjectWriter
    {
        private readonly Span<OpenSearchPayloadBuilder> _builders;
        private readonly Span<bool> _wroteFields;

        public OpenSearchObjectWriter(ref OpenSearchPayloadBuilder builder, ref bool wroteField)
        {
            _builders = MemoryMarshal.CreateSpan(ref builder, 1);
            _wroteFields = MemoryMarshal.CreateSpan(ref wroteField, 1);
        }

        public void WriteStringField(string fieldName, string value)
        {
            OpenSearchJsonWriter.AppendStringField(ref _builders[0], ref _wroteFields[0], fieldName, value);
        }

        public void WriteBooleanField(string fieldName, bool value)
        {
            OpenSearchJsonWriter.AppendBooleanField(ref _builders[0], ref _wroteFields[0], fieldName, value);
        }

        public void WriteInt64Field(string fieldName, long value)
        {
            OpenSearchJsonWriter.AppendInt64Field(ref _builders[0], ref _wroteFields[0], fieldName, value);
        }

        public void WriteStringArrayField(string fieldName, string[] values)
        {
            OpenSearchJsonWriter.AppendStringArrayField(ref _builders[0], ref _wroteFields[0], fieldName, values);
        }

        internal void WriteStringFieldLiteral(string fieldNameLiteral, string value)
        {
            OpenSearchJsonWriter.AppendStringFieldLiteral(ref _builders[0], ref _wroteFields[0], fieldNameLiteral, value);
        }

        internal void WriteBooleanFieldLiteral(string fieldNameLiteral, bool value)
        {
            OpenSearchJsonWriter.AppendBooleanFieldLiteral(ref _builders[0], ref _wroteFields[0], fieldNameLiteral, value);
        }

        internal void WriteStringArrayFieldLiteral(string fieldNameLiteral, string[] values)
        {
            OpenSearchJsonWriter.AppendStringArrayFieldLiteral(ref _builders[0], ref _wroteFields[0], fieldNameLiteral, values);
        }
    }

    public struct OpenSearchPayloadBuilder : IDisposable
    {
#if THEBESTLOGGER_ZSTRING_ENABLED
        private Utf8ValueStringBuilder _builder;
#else
        private readonly PooledStringBuilder _builder;
        private static readonly UTF8Encoding Utf8NoBom = new(false);
#endif

        public OpenSearchPayloadBuilder(bool notNested = true)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder = StringOperations.CreateStringBuilder(512, notNested);
#else
            _builder = StringOperations.CreateStringBuilder(512, notNested);
#endif
        }

        public void Append(char value)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder.Append(value);
#else
            _builder.StringBuilder.Append(value);
#endif
        }

        public void Append(string value)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder.Append(value);
#else
            _builder.StringBuilder.Append(value);
#endif
        }

        public void Append(ReadOnlySpan<char> value)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder.Append(value);
#else
            _builder.StringBuilder.Append(value);
#endif
        }

        public void Append(bool value)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder.Append(value);
#else
            _builder.StringBuilder.Append(value);
#endif
        }

        public void Append(long value)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder.Append(value);
#else
            _builder.StringBuilder.Append(value);
#endif
        }

        public void AppendLine()
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder.AppendLine();
#else
            _builder.StringBuilder.AppendLine();
#endif
        }

        public byte[] ToUtf8Bytes()
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            var payload = new byte[_builder.Length];
            _builder.AsSpan().CopyTo(payload);
            return payload;
#else
            return Utf8NoBom.GetBytes(_builder.ToString());
#endif
        }

        public override string ToString()
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            var payload = ToUtf8Bytes();
            return Encoding.UTF8.GetString(payload, 0, payload.Length);
#else
            return _builder.ToString();
#endif
        }

        public void Dispose()
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            _builder.Dispose();
#else
            _builder.Dispose();
#endif
        }
    }

    public static class OpenSearchJsonWriter
    {
        private const string HexDigits = "0123456789ABCDEF";

        public static void AppendStringField(ref OpenSearchPayloadBuilder sb,
                                             ref bool wroteField,
                                             string fieldName,
                                             string value)
        {
            AppendFieldName(ref sb, ref wroteField, fieldName);
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            AppendEscapedString(ref sb, value);
        }

        public static void AppendStringFieldLiteral(ref OpenSearchPayloadBuilder sb,
                                                    ref bool wroteField,
                                             string fieldNameLiteral,
                                             string value)
        {
            AppendFieldNameLiteral(ref sb, ref wroteField, fieldNameLiteral);
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            AppendEscapedString(ref sb, value);
        }

        public static void AppendBooleanField(ref OpenSearchPayloadBuilder sb,
                                              ref bool wroteField,
                                              string fieldName,
                                              bool value)
        {
            AppendFieldName(ref sb, ref wroteField, fieldName);
            sb.Append(value ? "true" : "false");
        }

        public static void AppendBooleanFieldLiteral(ref OpenSearchPayloadBuilder sb,
                                                     ref bool wroteField,
                                              string fieldNameLiteral,
                                              bool value)
        {
            AppendFieldNameLiteral(ref sb, ref wroteField, fieldNameLiteral);
            sb.Append(value ? "true" : "false");
        }

        public static void AppendInt64Field(ref OpenSearchPayloadBuilder sb,
                                            ref bool wroteField,
                                            string fieldName,
                                            long value)
        {
            AppendFieldName(ref sb, ref wroteField, fieldName);
            sb.Append(value);
        }

        public static void AppendInt64FieldLiteral(ref OpenSearchPayloadBuilder sb,
                                                   ref bool wroteField,
                                            string fieldNameLiteral,
                                            long value)
        {
            AppendFieldNameLiteral(ref sb, ref wroteField, fieldNameLiteral);
            sb.Append(value);
        }

        public static void AppendStringArrayField(ref OpenSearchPayloadBuilder sb,
                                                  ref bool wroteField,
                                                  string fieldName,
                                                  string[] values)
        {
            AppendFieldName(ref sb, ref wroteField, fieldName);
            if (values == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('[');
            for (var index = 0; index < values.Length; index++)
            {
                if (index > 0)
                {
                    sb.Append(',');
                }

                var value = values[index];
                if (value == null)
                {
                    sb.Append("null");
                }
                else
                {
                    AppendEscapedString(ref sb, value);
                }
            }

            sb.Append(']');
        }

        public static void AppendStringArrayFieldLiteral(ref OpenSearchPayloadBuilder sb,
                                                         ref bool wroteField,
                                                  string fieldNameLiteral,
                                                  string[] values)
        {
            AppendFieldNameLiteral(ref sb, ref wroteField, fieldNameLiteral);
            if (values == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('[');
            for (var index = 0; index < values.Length; index++)
            {
                if (index > 0)
                {
                    sb.Append(',');
                }

                var value = values[index];
                if (value == null)
                {
                    sb.Append("null");
                }
                else
                {
                    AppendEscapedString(ref sb, value);
                }
            }

            sb.Append(']');
        }

        public static void AppendEscapedString(ref OpenSearchPayloadBuilder sb, string value)
        {
            sb.Append('\"');
            var segmentStart = 0;
            for (var index = 0; index < value.Length; index++)
            {
                var ch = value[index];
                switch (ch)
                {
                    case '\"':
                        AppendUnescapedSegment(ref sb, value, segmentStart, index);
                        sb.Append("\\\"");
                        segmentStart = index + 1;
                        break;
                    case '\\':
                        AppendUnescapedSegment(ref sb, value, segmentStart, index);
                        sb.Append("\\\\");
                        segmentStart = index + 1;
                        break;
                    case '\b':
                        AppendUnescapedSegment(ref sb, value, segmentStart, index);
                        sb.Append("\\b");
                        segmentStart = index + 1;
                        break;
                    case '\f':
                        AppendUnescapedSegment(ref sb, value, segmentStart, index);
                        sb.Append("\\f");
                        segmentStart = index + 1;
                        break;
                    case '\n':
                        AppendUnescapedSegment(ref sb, value, segmentStart, index);
                        sb.Append("\\n");
                        segmentStart = index + 1;
                        break;
                    case '\r':
                        AppendUnescapedSegment(ref sb, value, segmentStart, index);
                        sb.Append("\\r");
                        segmentStart = index + 1;
                        break;
                    case '\t':
                        AppendUnescapedSegment(ref sb, value, segmentStart, index);
                        sb.Append("\\t");
                        segmentStart = index + 1;
                        break;
                    default:
                        if (ch < ' ')
                        {
                            AppendUnescapedSegment(ref sb, value, segmentStart, index);
                            AppendUnicodeEscape(ref sb, ch);
                            segmentStart = index + 1;
                        }

                        break;
                }
            }

            AppendUnescapedSegment(ref sb, value, segmentStart, value.Length);
            sb.Append('\"');
        }

        private static void AppendFieldNameLiteral(ref OpenSearchPayloadBuilder sb,
                                                   ref bool wroteField,
                                                   string fieldNameLiteral)
        {
            if (wroteField)
            {
                sb.Append(',');
            }

            wroteField = true;
            sb.Append(fieldNameLiteral);
        }

        private static void AppendFieldName(ref OpenSearchPayloadBuilder sb,
                                            ref bool wroteField,
                                            string fieldName)
        {
            if (wroteField)
            {
                sb.Append(',');
            }

            wroteField = true;
            AppendEscapedString(ref sb, fieldName);
            sb.Append(':');
        }

        private static void AppendUnescapedSegment(ref OpenSearchPayloadBuilder sb,
                                                   string value,
                                                   int segmentStart,
                                                   int segmentEndExclusive)
        {
            var segmentLength = segmentEndExclusive - segmentStart;
            if (segmentLength <= 0)
            {
                return;
            }

            sb.Append(value.AsSpan(segmentStart, segmentLength));
        }

        private static void AppendUnicodeEscape(ref OpenSearchPayloadBuilder sb, char ch)
        {
            sb.Append("\\u00");
            sb.Append(HexDigits[(ch >> 4) & 0xF]);
            sb.Append(HexDigits[ch & 0xF]);
        }
    }
}
