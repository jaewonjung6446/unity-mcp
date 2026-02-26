using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity
{
    [InitializeOnLoad]
    public static class ConsoleLogBuffer
    {
        private const int MaxEntries = 500;

        private struct LogEntry
        {
            public string message;
            public string stackTrace;
            public LogType type;
            public long timestampMs;
        }

        private static readonly List<LogEntry> _buffer = new();
        private static readonly object _lock = new();

        static ConsoleLogBuffer()
        {
            Application.logMessageReceived -= OnLogMessage;
            Application.logMessageReceived += OnLogMessage;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                message = message,
                stackTrace = stackTrace,
                type = type,
                timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            lock (_lock)
            {
                _buffer.Add(entry);
                if (_buffer.Count > MaxEntries)
                    _buffer.RemoveAt(0);
            }
        }

        public static JArray GetLogs(string filter = null, long sinceMs = 0, int maxCount = 100)
        {
            var result = new JArray();
            lock (_lock)
            {
                for (int i = _buffer.Count - 1; i >= 0 && result.Count < maxCount; i--)
                {
                    var entry = _buffer[i];

                    if (sinceMs > 0 && entry.timestampMs < sinceMs)
                        continue;

                    if (!string.IsNullOrEmpty(filter))
                    {
                        var typeName = entry.type.ToString().ToLower();
                        if (!string.Equals(filter, typeName, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    result.Add(new JObject
                    {
                        ["message"] = entry.message,
                        ["stackTrace"] = entry.stackTrace,
                        ["type"] = entry.type.ToString(),
                        ["timestampMs"] = entry.timestampMs
                    });
                }
            }
            return result;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }

        public static int Count
        {
            get { lock (_lock) { return _buffer.Count; } }
        }
    }
}
