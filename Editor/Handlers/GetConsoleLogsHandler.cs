using Newtonsoft.Json.Linq;

namespace McpUnity.Handlers
{
    public class GetConsoleLogsHandler : IToolHandler
    {
        public string Name => "get_console_logs";

        public JObject Execute(JObject parameters)
        {
            var filter = parameters["filter"]?.ToString();
            var sinceMs = parameters["since"]?.ToObject<long>() ?? 0;
            var clear = parameters["clear"]?.ToObject<bool>() ?? false;
            var maxCount = parameters["maxCount"]?.ToObject<int>() ?? 100;

            if (maxCount <= 0) maxCount = 100;
            if (maxCount > 500) maxCount = 500;

            var logs = ConsoleLogBuffer.GetLogs(filter, sinceMs, maxCount);

            if (clear)
                ConsoleLogBuffer.Clear();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Retrieved {logs.Count} log entries",
                ["logs"] = logs,
                ["totalBuffered"] = ConsoleLogBuffer.Count
            };
        }
    }
}
