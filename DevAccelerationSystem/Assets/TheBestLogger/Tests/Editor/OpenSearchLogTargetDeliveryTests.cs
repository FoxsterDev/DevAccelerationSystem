using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using TheBestLogger.Examples.LogTargets;
using UnityEditor;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class OpenSearchLogTargetDeliveryTests
    {
        private string _tempRootAssetPath;

        [SetUp]
        public void SetUp()
        {
            LogManager.Dispose();
            ResetLogManagerState();
            _tempRootAssetPath = $"Assets/TheBestLogger/Tests/Editor/Generated/{Guid.NewGuid():N}";
        }

        [TearDown]
        public void TearDown()
        {
            LogManager.Dispose();
            ResetLogManagerState();

            if (!string.IsNullOrEmpty(_tempRootAssetPath))
            {
                FileUtil.DeleteFileOrDirectory(_tempRootAssetPath);
                FileUtil.DeleteFileOrDirectory(_tempRootAssetPath + ".meta");
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void Log_SendsSinglePayloadWithExpectedHeadersAndFields()
        {
            using var server = new LocalHttpRequestCaptureServer();
            var target = CreateTarget(server, apiKey: "single-key");
            var attributes = new LogAttributes()
                                 .Add("attempt", 7)
                                 .Add("region", "eu");
            attributes.StackTrace = "stack-line";
            attributes.TimeStampFormatted = "2026-04-26T10:11:12.3456789Z";
            attributes.Tags = new[] { "prod", "critical" };

            target.Log(LogLevel.Warning, "Gameplay", "single-message", attributes, null);

            var request = server.WaitForRequestOrThrow();
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Path, Is.EqualTo("/logs"));
            Assert.That(request.Headers["x-api-key"], Is.EqualTo("single-key"));
            Assert.That(request.Headers["Content-Type"], Is.EqualTo("application/json"));

            var lines = SplitBulkPayloadLines(request.Body);
            Assert.That(lines.Length, Is.EqualTo(2));
            StringAssert.Contains("\"_index\" : \"thebestlogger-", lines[0]);

            var dto = JsonUtility.FromJson<OpenSearchLogDTO>(lines[1]);
            Assert.That(dto.LogLevel, Is.EqualTo("Warn"));
            Assert.That(dto.Category, Is.EqualTo("Gameplay"));
            Assert.That(dto.Message, Is.EqualTo("single-message"));
            Assert.That(dto.Stacktrace, Is.EqualTo("stack-line"));
            Assert.That(dto.TimeUTC, Is.EqualTo("2026-04-26T10:11:12.3456789Z"));
            Assert.That(dto.Attributes, Is.EqualTo("{\"attempt\":7,\"region\":\"eu\"}"));
            Assert.That(dto.Tags, Is.EqualTo(new[] { "prod", "critical" }));
        }

        [Test]
        public void LogBatch_SendsBulkPayloadForEveryEntry()
        {
            using var server = new LocalHttpRequestCaptureServer();
            var target = CreateTarget(server, apiKey: "batch-key");
            var batch = new List<LogEntry>
            {
                CreateLogEntry(LogLevel.Info, "first", "2026-04-26T10:00:00.0000000Z", "session-1", new[] { "startup" }),
                CreateLogEntry(LogLevel.Error, "second", "2026-04-26T10:00:01.0000000Z", "session-2", new[] { "combat", "boss" })
            };

            target.LogBatch(batch);

            var request = server.WaitForRequestOrThrow();
            Assert.That(request.Headers["x-api-key"], Is.EqualTo("batch-key"));

            var lines = SplitBulkPayloadLines(request.Body);
            Assert.That(lines.Length, Is.EqualTo(4));

            var firstDto = JsonUtility.FromJson<OpenSearchLogDTO>(lines[1]);
            var secondDto = JsonUtility.FromJson<OpenSearchLogDTO>(lines[3]);

            Assert.That(firstDto.LogLevel, Is.EqualTo("Info"));
            Assert.That(firstDto.Message, Is.EqualTo("first"));
            Assert.That(firstDto.Attributes, Is.EqualTo("{\"session\":\"session-1\"}"));
            Assert.That(firstDto.Tags, Is.EqualTo(new[] { "startup" }));

            Assert.That(secondDto.LogLevel, Is.EqualTo("Error"));
            Assert.That(secondDto.Message, Is.EqualTo("second"));
            Assert.That(secondDto.Attributes, Is.EqualTo("{\"session\":\"session-2\"}"));
            Assert.That(secondDto.Tags, Is.EqualTo(new[] { "combat", "boss" }));
        }

        [Test]
        public void Log_WhenDebugModeDisabled_SendsDebugModeFalse()
        {
            using var server = new LocalHttpRequestCaptureServer();
            var target = CreateTarget(server, apiKey: "debug-false-key");

            target.Log(LogLevel.Error, "Gameplay", "debug-false", CreateAttributes("2026-04-26T10:00:00.0000000Z"), null);

            var request = server.WaitForRequestOrThrow();
            var lines = SplitBulkPayloadLines(request.Body);
            var dto = JsonUtility.FromJson<OpenSearchLogDTO>(lines[1]);

            Assert.That(dto.DebugMode, Is.False);
        }

        [Test]
        public void Log_WhenDebugModeEnabled_SendsDebugModeTrue()
        {
            using var server = new LocalHttpRequestCaptureServer();
            var target = CreateTarget(server, apiKey: "debug-true-key");
            ((ILogTarget) target).DebugModeEnabled = true;

            target.Log(LogLevel.Error, "Gameplay", "debug-true", CreateAttributes("2026-04-26T10:00:00.0000000Z"), null);

            var request = server.WaitForRequestOrThrow();
            var lines = SplitBulkPayloadLines(request.Body);
            var dto = JsonUtility.FromJson<OpenSearchLogDTO>(lines[1]);

            Assert.That(dto.DebugMode, Is.True);
        }

        [Test]
        public void ApplyConfiguration_AfterRemoteConfigMerge_UsesUpdatedApiKeyOnNextRequest()
        {
            using var server = new LocalHttpRequestCaptureServer();
            var target = CreateTarget(server, apiKey: "old-key");

            target.Log(LogLevel.Info, "Gameplay", "before-update", CreateAttributes("2026-04-26T10:00:00.0000000Z"), null);
            var firstRequest = server.WaitForRequestOrThrow();
            Assert.That(firstRequest.Headers["x-api-key"], Is.EqualTo("old-key"));

            var local = new OpenSearchLogTargetConfiguration
            {
                OpenSearchHostUrl = server.BaseUrl,
                OpenSearchSingleLogMethod = "/logs",
                IndexPrefix = "thebestlogger-",
                ApiKey = "old-key"
            };
            var remote = new OpenSearchLogTargetConfiguration { ApiKey = "new-key" };
            local.Merge(remote);
            target.ApplyConfiguration(local);

            target.Log(LogLevel.Info, "Gameplay", "after-update", CreateAttributes("2026-04-26T10:00:01.0000000Z"), null);
            var secondRequest = server.WaitForRequestOrThrow(requestIndex: 1);
            Assert.That(secondRequest.Headers["x-api-key"], Is.EqualTo("new-key"));
        }

        [Test]
        public void LogManager_RawJsonPartialUpdate_UpdatesOpenSearchTargetEndToEnd()
        {
            using var server = new LocalHttpRequestCaptureServer();
            CreateOpenSearchConfigurationAssets("OpenSearchLogManagerRawJson",
                                                CreateConfiguration(server, "old-key"));

            var target = new OpenSearchLogTarget();
            LogManager.Initialize(new LogTarget[] { target }, "OpenSearchLogManagerRawJson/", CancellationToken.None, "debug-user");

            LogManager.UpdateLogTargetConfiguration(nameof(OpenSearchLogTargetConfiguration), "{\"ApiKey\":\"new-key\"}");
            LogManager.CreateLogger("Gameplay").LogInfo("after-raw-json-update");

            var request = server.WaitForRequestOrThrow();
            Assert.That(request.Path, Is.EqualTo("/logs"));
            Assert.That(request.Headers["x-api-key"], Is.EqualTo("new-key"));
            StringAssert.Contains("after-raw-json-update", request.Body);
        }

        [Test]
        public void ConnectionRefused_DoesNotPoisonSubsequentValidRequests()
        {
            var invalidPort = GetUnusedTcpPort();
            var target = new OpenSearchLogTarget();
            target.ApplyConfiguration(new OpenSearchLogTargetConfiguration
            {
                OpenSearchHostUrl = $"http://127.0.0.1:{invalidPort}",
                OpenSearchSingleLogMethod = "/logs",
                IndexPrefix = "thebestlogger-",
                ApiKey = "invalid-host-key",
                MinLogLevel = LogLevel.Debug,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            });

            Assert.DoesNotThrow(() => target.Log(LogLevel.Info, "Gameplay", "will-fail", CreateAttributes("2026-04-26T10:00:00.0000000Z"), null));
            Thread.Sleep(300);

            using var server = new LocalHttpRequestCaptureServer();
            target.ApplyConfiguration(CreateConfiguration(server, "valid-key"));
            target.Log(LogLevel.Info, "Gameplay", "recovered", CreateAttributes("2026-04-26T10:00:01.0000000Z"), null);

            var recoveredRequest = server.WaitForRequestOrThrow();
            Assert.That(recoveredRequest.Headers["x-api-key"], Is.EqualTo("valid-key"));
            StringAssert.Contains("recovered", recoveredRequest.Body);
        }

        [Test]
        public void Server4xx_DoesNotPoisonSubsequentRequests()
        {
            using var server = new LocalHttpRequestCaptureServer();
            server.EnqueueResponse(HttpStatusCode.BadRequest, "{\"error\":\"bad-request\"}");
            server.EnqueueResponse(HttpStatusCode.OK, "{\"ok\":true}");

            var target = CreateTarget(server, apiKey: "4xx-key");
            target.Log(LogLevel.Info, "Gameplay", "first-request", CreateAttributes("2026-04-26T10:00:00.0000000Z"), null);
            target.Log(LogLevel.Info, "Gameplay", "second-request", CreateAttributes("2026-04-26T10:00:01.0000000Z"), null);

            var requests = server.WaitForRequestsOrThrow(2);
            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(requests.Any(request => request.Body.Contains("first-request")), Is.True);
            Assert.That(requests.Any(request => request.Body.Contains("second-request")), Is.True);
        }

        [Test]
        public void Server5xx_DoesNotPoisonSubsequentRequests()
        {
            using var server = new LocalHttpRequestCaptureServer();
            server.EnqueueResponse(HttpStatusCode.InternalServerError, "{\"error\":\"server-error\"}");
            server.EnqueueResponse(HttpStatusCode.OK, "{\"ok\":true}");

            var target = CreateTarget(server, apiKey: "5xx-key");
            target.Log(LogLevel.Info, "Gameplay", "first-request", CreateAttributes("2026-04-26T10:00:00.0000000Z"), null);
            target.Log(LogLevel.Info, "Gameplay", "second-request", CreateAttributes("2026-04-26T10:00:01.0000000Z"), null);

            var requests = server.WaitForRequestsOrThrow(2);
            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(requests.Any(request => request.Body.Contains("first-request")), Is.True);
            Assert.That(requests.Any(request => request.Body.Contains("second-request")), Is.True);
        }

        [Test]
        public void DelayedResponse_DoesNotPreventSubsequentRequestsAfterRecovery()
        {
            using var server = new LocalHttpRequestCaptureServer();
            server.EnqueueResponse(HttpStatusCode.OK, "{\"ok\":true}", delayMs: 2600);
            server.EnqueueResponse(HttpStatusCode.OK, "{\"ok\":true}");

            var target = CreateTarget(server, apiKey: "delayed-key");
            target.Log(LogLevel.Info, "Gameplay", "slow-request", CreateAttributes("2026-04-26T10:00:00.0000000Z"), null);
            Assert.That(server.WaitForRequestCount(1, TimeSpan.FromSeconds(2)), Is.True, "Slow request was not captured by local server.");

            Thread.Sleep(2800);

            target.Log(LogLevel.Info, "Gameplay", "after-delay", CreateAttributes("2026-04-26T10:00:01.0000000Z"), null);
            var requests = server.WaitForRequestsOrThrow(2);

            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(requests.Any(request => request.Body.Contains("slow-request")), Is.True);
            Assert.That(requests.Any(request => request.Body.Contains("after-delay")), Is.True);
        }

        private static OpenSearchLogTarget CreateTarget(LocalHttpRequestCaptureServer server, string apiKey)
        {
            var target = new OpenSearchLogTarget();
            target.ApplyConfiguration(CreateConfiguration(server, apiKey));
            return target;
        }

        private static OpenSearchLogTargetConfiguration CreateConfiguration(LocalHttpRequestCaptureServer server, string apiKey)
        {
            return new OpenSearchLogTargetConfiguration
            {
                OpenSearchHostUrl = server.BaseUrl,
                OpenSearchSingleLogMethod = "/logs",
                IndexPrefix = "thebestlogger-",
                ApiKey = apiKey,
                MinLogLevel = LogLevel.Debug,
                DebugMode = new DebugModeConfiguration
                {
                    Enabled = true,
                    IDs = new[] { "debug-user" }
                },
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };
        }

        private void CreateOpenSearchConfigurationAssets(string resourceSubFolderName,
                                                         OpenSearchLogTargetConfiguration openSearchConfiguration)
        {
            var absoluteResourcesPath = Path.Combine(Application.dataPath,
                                                     _tempRootAssetPath.Replace("Assets/", string.Empty),
                                                     "Resources",
                                                     resourceSubFolderName);
            Directory.CreateDirectory(absoluteResourcesPath);
            AssetDatabase.Refresh();

            var openSearchConfigSo = ScriptableObject.CreateInstance<OpenSearchLogTargetConfigurationSO>();
            openSearchConfigSo.SpecificConfiguration = openSearchConfiguration;
            AssetDatabase.CreateAsset(openSearchConfigSo,
                                      $"{_tempRootAssetPath}/OpenSearchLogTargetConfigurationSO_{resourceSubFolderName}.asset");

            var logManagerConfiguration = ScriptableObject.CreateInstance<LogManagerConfiguration>();
            logManagerConfiguration.DefaultUnityLogsCategoryName = "DefaultCategory";
            logManagerConfiguration.MessageMaxLength = 512;
            logManagerConfiguration.MinTimestampPeriodMs = 0;
            logManagerConfiguration.MinUpdatesPeriodMs = 1000;
            logManagerConfiguration.StackTraceFormatterConfiguration = new StackTraceFormatterConfiguration();
            logManagerConfiguration.UniTaskConfiguration = new UniTaskConfiguration();
            logManagerConfiguration.LogTargetConfigs = new LogTargetConfigurationSO[] { openSearchConfigSo };
            SetEditorLogSourceFlag(logManagerConfiguration, "_unityDebugLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unityApplicationLogMessageReceivedSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unityApplicationLogMessageReceivedThreadedSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unobservedSystemTaskExceptionLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_unobservedUniTaskExceptionLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_systemDiagnosticsDebugLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_systemDiagnosticsConsoleLogSourceForUnityEditor", false);
            SetEditorLogSourceFlag(logManagerConfiguration, "_currentDomainUnhandledExceptionLogSourceForUnityEditor", false);

            AssetDatabase.CreateAsset(logManagerConfiguration,
                                      $"{_tempRootAssetPath}/Resources/{resourceSubFolderName}/LogManagerConfiguration.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SetEditorLogSourceFlag(LogManagerConfiguration configuration, string fieldName, bool value)
        {
            var field = typeof(LogManagerConfiguration).GetField(fieldName,
                                                                 System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(configuration, value);
        }

        private static void ResetLogManagerState()
        {
            SetStaticField("_wasDisposed", false);
            SetStaticField("_isInitialized", false);
            SetStaticField("_isRunningUpdates", false);
            SetStaticField("_configuration", null);
            SetStaticField("_lastIncomingLogTargetConfigurationPatchesForCache", null);
            SetStaticField("_utilitySupplier", null);
            SetStaticField("_loggers", null);
            SetStaticField("_logSources", Array.Empty<ILogSource>());
            SetStaticField("_decoratedLogTargets", Array.Empty<ILogTarget>());
            SetStaticField("_originalLogTargets", Array.Empty<LogTarget>());
            SetStaticField("_targetUpdates", new List<IScheduledUpdate>());
            SetStaticField("_minUpdatesPeriodMs", (uint) 0);
            SetStaticField("_timeStampPrevious", default(DateTime));
            SetStaticField("_timeStampPreviousString", null);
            SetStaticField("_currentDebugId", null);
            SetStaticField("_debugModeRequestedState", false);
        }

        private static void SetStaticField(string fieldName, object value)
        {
            var field = typeof(LogManager).GetField(fieldName,
                                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(null, value);
        }

        private static LogEntry CreateLogEntry(LogLevel level,
                                               string message,
                                               string timestamp,
                                               string sessionId,
                                               string[] tags)
        {
            return new LogEntry(level,
                                "Gameplay",
                                message,
                                CreateAttributes(timestamp, sessionId, tags),
                                null);
        }

        private static LogAttributes CreateAttributes(string timestamp,
                                                      string sessionId = "default-session",
                                                      string[] tags = null)
        {
            return new LogAttributes("session", sessionId)
            {
                StackTrace = "stack-line",
                TimeStampFormatted = timestamp,
                TimeUtc = DateTime.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Tags = tags ?? new[] { "default" }
            };
        }

        private static string[] SplitBulkPayloadLines(string body)
        {
            return body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static int GetUnusedTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private sealed class LocalHttpRequestCaptureServer : IDisposable
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new();
            private readonly ConcurrentQueue<CapturedRequest> _requests = new();
            private readonly ConcurrentQueue<ResponsePlan> _responsePlans = new();
            private readonly TcpListener _listener;
            private readonly Task _acceptLoopTask;

            public string BaseUrl { get; }

            public LocalHttpRequestCaptureServer()
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                var port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                BaseUrl = $"http://127.0.0.1:{port}";
                _acceptLoopTask = Task.Run(() => AcceptLoopAsync(_cancellationTokenSource.Token));
            }

            public void EnqueueResponse(HttpStatusCode statusCode, string body, int delayMs = 0)
            {
                _responsePlans.Enqueue(new ResponsePlan(statusCode, body, delayMs));
            }

            public bool WaitForRequestCount(int count, TimeSpan timeout)
            {
                var deadline = DateTime.UtcNow.Add(timeout);
                while (DateTime.UtcNow < deadline)
                {
                    if (_requests.Count >= count)
                    {
                        return true;
                    }

                    Thread.Sleep(25);
                }

                return _requests.Count >= count;
            }

            public CapturedRequest WaitForRequestOrThrow(int requestIndex = 0)
            {
                var requests = WaitForRequestsOrThrow(requestIndex + 1);
                return requests[requestIndex];
            }

            public IReadOnlyList<CapturedRequest> WaitForRequestsOrThrow(int count)
            {
                Assert.That(WaitForRequestCount(count, TimeSpan.FromSeconds(5)),
                            Is.True,
                            $"Expected {count} request(s), captured {_requests.Count}.");
                return _requests.ToArray();
            }

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _listener.Stop();

                try
                {
                    _acceptLoopTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    // ignore shutdown races from disposed listener
                }

                _cancellationTokenSource.Dispose();
            }

            private async Task AcceptLoopAsync(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await _listener.AcceptTcpClientAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        break;
                    }

                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                }
            }

            private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true))
                {
                    var requestLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(requestLine))
                    {
                        return;
                    }

                    var requestLineParts = requestLine.Split(' ');
                    var method = requestLineParts[0];
                    var path = requestLineParts[1];
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string line;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                    {
                        var separatorIndex = line.IndexOf(':');
                        if (separatorIndex <= 0)
                        {
                            continue;
                        }

                        var key = line.Substring(0, separatorIndex).Trim();
                        var value = line.Substring(separatorIndex + 1).Trim();
                        headers[key] = value;
                    }

                    var body = string.Empty;
                    if (headers.TryGetValue("Content-Length", out var contentLengthRaw) &&
                        int.TryParse(contentLengthRaw, out var contentLength) &&
                        contentLength > 0)
                    {
                        var buffer = new char[contentLength];
                        var totalRead = 0;
                        while (totalRead < contentLength)
                        {
                            var read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                            if (read <= 0)
                            {
                                break;
                            }

                            totalRead += read;
                        }

                        body = new string(buffer, 0, totalRead);
                    }

                    _requests.Enqueue(new CapturedRequest(method, path, headers, body));

                    var plan = _responsePlans.TryDequeue(out var configuredPlan)
                                   ? configuredPlan
                                   : new ResponsePlan(HttpStatusCode.OK, "{\"ok\":true}", 0);

                    if (plan.DelayMs > 0)
                    {
                        await Task.Delay(plan.DelayMs, cancellationToken);
                    }

                    var responseBody = Encoding.UTF8.GetBytes(plan.Body);
                    var responseHeader =
                        $"HTTP/1.1 {(int) plan.StatusCode} {plan.StatusCode}\r\nContent-Type: application/json\r\nContent-Length: {responseBody.Length}\r\nConnection: close\r\n\r\n";
                    var headerBytes = Encoding.ASCII.GetBytes(responseHeader);

                    try
                    {
                        await stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
                        await stream.WriteAsync(responseBody, 0, responseBody.Length, cancellationToken);
                        await stream.FlushAsync(cancellationToken);
                    }
                    catch (IOException)
                    {
                        // client may have timed out and aborted the socket
                    }
                    catch (ObjectDisposedException)
                    {
                        // shutdown path
                    }
                }
            }

            private readonly struct ResponsePlan
            {
                public readonly HttpStatusCode StatusCode;
                public readonly string Body;
                public readonly int DelayMs;

                public ResponsePlan(HttpStatusCode statusCode, string body, int delayMs)
                {
                    StatusCode = statusCode;
                    Body = body;
                    DelayMs = delayMs;
                }
            }
        }

        private readonly struct CapturedRequest
        {
            public readonly string Method;
            public readonly string Path;
            public readonly IReadOnlyDictionary<string, string> Headers;
            public readonly string Body;

            public CapturedRequest(string method,
                                   string path,
                                   IReadOnlyDictionary<string, string> headers,
                                   string body)
            {
                Method = method;
                Path = path;
                Headers = headers;
                Body = body;
            }
        }
    }
}
