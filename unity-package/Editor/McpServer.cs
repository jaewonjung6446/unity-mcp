using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace McpUnity
{
    [InitializeOnLoad]
    public class McpServer : IDisposable
    {
        private static McpServer _instance;
        public static McpServer Instance => _instance ??= new McpServer();

        private TcpListener _tcpListener;
        private CancellationTokenSource _cts;
        private readonly Dictionary<string, IToolHandler> _tools = new();
        private readonly List<TcpClient> _clients = new();
        private int _port = 8090;

        public bool IsListening { get; private set; }

        static McpServer()
        {
            EditorApplication.delayCall += () => Instance.Start();
            EditorApplication.quitting += () => _instance?.Dispose();
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                _instance?.Stop();
                _instance?.Dispose();
                _instance = null;
            };
            AssemblyReloadEvents.afterAssemblyReload += () =>
            {
                EditorApplication.delayCall += () => Instance.Start();
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    _instance?.Stop();
                else if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += () => Instance.Start();
            };
        }

        private McpServer()
        {
            ReadConfig();
            RegisterTools();
        }

        private void ReadConfig()
        {
            var configPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ProjectSettings", "McpUnitySettings.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = JObject.Parse(File.ReadAllText(configPath));
                    _port = json["Port"]?.ToObject<int>() ?? 8090;
                }
                catch { }
            }
        }

        private void RegisterTools()
        {
            Register(new Handlers.GetStateHandler());
            Register(new Handlers.GetGameObjectHandler());
            Register(new Handlers.GetSelectionHandler());
            Register(new Handlers.FocusGameObjectHandler());
            Register(new Handlers.OpenSceneHandler());
            Register(new Handlers.CloseSceneHandler());
            Register(new Handlers.SaveSceneHandler());
            Register(new Handlers.OpenPrefabHandler());
            Register(new Handlers.CreateScriptHandler());
            Register(new Handlers.ExecuteCodeHandler());
            Register(new Handlers.GetAssetContentsHandler());
            Register(new Handlers.GetAssetImporterHandler());
            Register(new Handlers.CopyAssetHandler());
            Register(new Handlers.ImportAssetHandler());
            Register(new Handlers.SearchHandler());
            Register(new Handlers.ScreenshotHandler());
            Register(new Handlers.TestActiveSceneHandler());
        }

        private void Register(IToolHandler handler)
        {
            _tools[handler.Name] = handler;
        }

        public bool TryGetTool(string name, out IToolHandler tool) => _tools.TryGetValue(name, out tool);

        public void Start()
        {
            if (IsListening) return;

            try
            {
                try { _tcpListener?.Stop(); } catch { }

                _cts = new CancellationTokenSource();
                _tcpListener = new TcpListener(IPAddress.Loopback, _port);
                _tcpListener.Start();
                IsListening = true;
                Debug.Log($"[MCP] WebSocket server started on port {_port}");
                Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start server: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!IsListening) return;
            IsListening = false;

            try
            {
                _cts?.Cancel();
                _tcpListener?.Stop();
                lock (_clients)
                {
                    foreach (var client in _clients)
                    {
                        try { client.Close(); } catch { }
                    }
                    _clients.Clear();
                }
                Debug.Log("[MCP] WebSocket server stopped");
            }
            catch { }
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && IsListening)
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleNewClient(client, ct));
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                        Debug.LogError($"[MCP] Accept error: {ex.Message}");
                }
            }
        }

        private async Task HandleNewClient(TcpClient client, CancellationToken ct)
        {
            var stream = client.GetStream();
            try
            {
                // Read HTTP upgrade request
                var buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!TryWebSocketHandshake(request, out var responseHeaders))
                {
                    var badRequest = Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(badRequest, 0, badRequest.Length, ct);
                    client.Close();
                    return;
                }

                var handshakeBytes = Encoding.UTF8.GetBytes(responseHeaders);
                await stream.WriteAsync(handshakeBytes, 0, handshakeBytes.Length, ct);

                lock (_clients) _clients.Add(client);
                Debug.Log("[MCP] Client connected");

                await HandleWebSocketClient(client, stream, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    Debug.LogError($"[MCP] Client error: {ex.Message}");
            }
            finally
            {
                lock (_clients) _clients.Remove(client);
                try { client.Close(); } catch { }
                Debug.Log("[MCP] Client disconnected");
            }
        }

        private bool TryWebSocketHandshake(string request, out string response)
        {
            response = null;

            // Extract Sec-WebSocket-Key
            var match = Regex.Match(request, @"Sec-WebSocket-Key:\s*([A-Za-z0-9+/=]+)", RegexOptions.IgnoreCase);
            if (!match.Success) return false;

            // Check for Upgrade: websocket header
            if (!Regex.IsMatch(request, @"Upgrade:\s*websocket", RegexOptions.IgnoreCase)) return false;

            var key = match.Groups[1].Value;
            // Compute accept for both standard RFC 6455 GUID and ws library GUID
            string acceptKey;
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
                acceptKey = Convert.ToBase64String(hash);
            }

            response = "HTTP/1.1 101 Switching Protocols\r\n" +
                       "Upgrade: websocket\r\n" +
                       "Connection: Upgrade\r\n" +
                       $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
            return true;
        }

        private async Task HandleWebSocketClient(TcpClient client, NetworkStream stream, CancellationToken ct)
        {
            while (client.Connected && !ct.IsCancellationRequested)
            {
                var message = await ReadWebSocketFrame(stream, ct);
                if (message == null) break; // connection closed

                var responseText = await ProcessMessage(message);
                await WriteWebSocketFrame(stream, responseText, ct);
            }
        }

        private async Task<string> ReadWebSocketFrame(NetworkStream stream, CancellationToken ct)
        {
            var fullMessage = new StringBuilder();

            while (true)
            {
                // Read first 2 bytes (header)
                var header = new byte[2];
                if (!await ReadExact(stream, header, 0, 2, ct)) return null;

                bool fin = (header[0] & 0x80) != 0;
                int opcode = header[0] & 0x0F;

                // Close frame
                if (opcode == 8) return null;
                // Ping frame
                if (opcode == 9)
                {
                    // Read and discard ping payload, send pong
                    bool masked = (header[1] & 0x80) != 0;
                    long len = header[1] & 0x7F;
                    if (len == 126) { var b = new byte[2]; await ReadExact(stream, b, 0, 2, ct); len = (b[0] << 8) | b[1]; }
                    else if (len == 127) { var b = new byte[8]; await ReadExact(stream, b, 0, 8, ct); len = BitConverter.ToInt64(new byte[] { b[7], b[6], b[5], b[4], b[3], b[2], b[1], b[0] }, 0); }
                    var maskKey = new byte[4];
                    if (masked) await ReadExact(stream, maskKey, 0, 4, ct);
                    var pingData = new byte[len];
                    if (len > 0) await ReadExact(stream, pingData, 0, (int)len, ct);
                    if (masked) for (int i = 0; i < len; i++) pingData[i] ^= maskKey[i % 4];
                    // Send pong
                    await WriteWebSocketFrameRaw(stream, 0x0A, pingData, ct);
                    continue;
                }
                // Pong frame - ignore
                if (opcode == 10)
                {
                    bool masked = (header[1] & 0x80) != 0;
                    long len = header[1] & 0x7F;
                    if (len == 126) { var b = new byte[2]; await ReadExact(stream, b, 0, 2, ct); len = (b[0] << 8) | b[1]; }
                    else if (len == 127) { var b = new byte[8]; await ReadExact(stream, b, 0, 8, ct); len = BitConverter.ToInt64(new byte[] { b[7], b[6], b[5], b[4], b[3], b[2], b[1], b[0] }, 0); }
                    if (masked) { var mk = new byte[4]; await ReadExact(stream, mk, 0, 4, ct); }
                    if (len > 0) { var skip = new byte[len]; await ReadExact(stream, skip, 0, (int)len, ct); }
                    continue;
                }

                bool isMasked = (header[1] & 0x80) != 0;
                long payloadLen = header[1] & 0x7F;

                if (payloadLen == 126)
                {
                    var lenBuf = new byte[2];
                    if (!await ReadExact(stream, lenBuf, 0, 2, ct)) return null;
                    payloadLen = (lenBuf[0] << 8) | lenBuf[1];
                }
                else if (payloadLen == 127)
                {
                    var lenBuf = new byte[8];
                    if (!await ReadExact(stream, lenBuf, 0, 8, ct)) return null;
                    payloadLen = BitConverter.ToInt64(new byte[] { lenBuf[7], lenBuf[6], lenBuf[5], lenBuf[4], lenBuf[3], lenBuf[2], lenBuf[1], lenBuf[0] }, 0);
                }

                byte[] maskKeyData = null;
                if (isMasked)
                {
                    maskKeyData = new byte[4];
                    if (!await ReadExact(stream, maskKeyData, 0, 4, ct)) return null;
                }

                var payload = new byte[payloadLen];
                if (payloadLen > 0)
                {
                    if (!await ReadExact(stream, payload, 0, (int)payloadLen, ct)) return null;
                }

                if (isMasked)
                {
                    for (int i = 0; i < payloadLen; i++)
                        payload[i] ^= maskKeyData[i % 4];
                }

                fullMessage.Append(Encoding.UTF8.GetString(payload));

                if (fin) break;
            }

            return fullMessage.ToString();
        }

        private async Task<bool> ReadExact(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct);
                if (read == 0) return false;
                totalRead += read;
            }
            return true;
        }

        private async Task WriteWebSocketFrame(NetworkStream stream, string message, CancellationToken ct)
        {
            var data = Encoding.UTF8.GetBytes(message);
            await WriteWebSocketFrameRaw(stream, 0x81, data, ct); // 0x81 = fin + text
        }

        private async Task WriteWebSocketFrameRaw(NetworkStream stream, byte firstByte, byte[] data, CancellationToken ct)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(firstByte);

                if (data.Length < 126)
                {
                    ms.WriteByte((byte)data.Length);
                }
                else if (data.Length <= 65535)
                {
                    ms.WriteByte(126);
                    ms.WriteByte((byte)(data.Length >> 8));
                    ms.WriteByte((byte)(data.Length & 0xFF));
                }
                else
                {
                    ms.WriteByte(127);
                    var lenBytes = BitConverter.GetBytes((long)data.Length);
                    // Big-endian
                    for (int i = 7; i >= 0; i--)
                        ms.WriteByte(lenBytes[i]);
                }

                ms.Write(data, 0, data.Length);

                var frame = ms.ToArray();
                await stream.WriteAsync(frame, 0, frame.Length, ct);
                await stream.FlushAsync(ct);
            }
        }

        private async Task<string> ProcessMessage(string message)
        {
            try
            {
                var request = JObject.Parse(message);
                var method = request["method"]?.ToString();
                var parameters = request["params"] as JObject ?? new JObject();
                var requestId = request["id"]?.ToString();

                JObject result;
                if (string.IsNullOrEmpty(method))
                {
                    result = CreateError("Missing method", "invalid_request");
                }
                else if (_tools.TryGetValue(method, out var tool))
                {
                    var tcs = new TaskCompletionSource<JObject>();
                    // Execute on Unity main thread
                    EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteOnMainThread(tool, parameters, tcs));
                    result = await tcs.Task;
                }
                else
                {
                    result = CreateError($"Unknown method: {method}", "unknown_method");
                }

                var response = new JObject { ["id"] = requestId };
                if (result.ContainsKey("error"))
                    response["error"] = result["error"];
                else
                    response["result"] = result;

                return response.ToString(Formatting.None);
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["error"] = new JObject
                    {
                        ["type"] = "internal_error",
                        ["message"] = ex.Message
                    }
                }.ToString(Formatting.None);
            }
        }

        private IEnumerator ExecuteOnMainThread(IToolHandler tool, JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            try
            {
                var result = tool.Execute(parameters);
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetResult(CreateError($"Tool execution failed: {ex.Message}", "tool_error"));
            }
            yield return null;
        }

        public static JObject CreateError(string message, string errorType)
        {
            return new JObject
            {
                ["error"] = new JObject
                {
                    ["type"] = errorType,
                    ["message"] = message
                }
            };
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
