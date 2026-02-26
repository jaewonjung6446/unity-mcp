using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class UndoRedoHandler : IToolHandler
    {
        public string Name => "undo_redo";

        public JObject Execute(JObject parameters)
        {
            var action = parameters["action"]?.ToString()?.ToLower();
            if (string.IsNullOrEmpty(action))
                return McpServer.CreateError("Missing required parameter: action (undo or redo)", "validation_error");

            int steps = parameters["steps"]?.ToObject<int>() ?? 1;
            steps = System.Math.Max(1, System.Math.Min(steps, 20));

            if (action == "undo")
            {
                for (int i = 0; i < steps; i++)
                    Undo.PerformUndo();

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Performed {steps} undo operation(s)"
                };
            }
            else if (action == "redo")
            {
                for (int i = 0; i < steps; i++)
                    Undo.PerformRedo();

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Performed {steps} redo operation(s)"
                };
            }

            return McpServer.CreateError("Invalid action. Use 'undo' or 'redo'", "validation_error");
        }
    }
}
