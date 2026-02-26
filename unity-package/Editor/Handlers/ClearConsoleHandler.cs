using System.Reflection;
using Newtonsoft.Json.Linq;

namespace McpUnity.Handlers
{
    public class ClearConsoleHandler : IToolHandler
    {
        public string Name => "clear_console";

        public JObject Execute(JObject parameters)
        {
            // Clear Unity console via reflection (LogEntries.Clear)
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntries != null)
            {
                var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                clearMethod?.Invoke(null, null);
            }

            // Also clear our log buffer
            ConsoleLogBuffer.Instance.Clear();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Console cleared"
            };
        }
    }
}
