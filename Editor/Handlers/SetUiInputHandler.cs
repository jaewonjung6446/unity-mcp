using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class SetUiInputHandler : IToolHandler
    {
        public string Name => "set_ui_input";

        public JObject Execute(JObject parameters)
        {
            if (!Application.isPlaying)
                return McpServer.CreateError("set_ui_input requires Play Mode", "invalid_state");

            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var text = parameters["text"]?.ToString();
            if (text == null)
                return McpServer.CreateError("Missing required parameter: text", "validation_error");

            var path = GetGameObjectHandler.GetPath(go);

            // Try legacy InputField
            var inputField = go.GetComponent<InputField>();
            if (inputField != null)
            {
                inputField.text = text;
                inputField.onValueChanged.Invoke(text);
                inputField.onEndEdit.Invoke(text);
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Set InputField text on '{path}'"
                };
            }

            // Try TMP_InputField via reflection
            var tmpComp = go.GetComponent("TMP_InputField");
            if (tmpComp != null)
            {
                var t = tmpComp.GetType();
                var textProp = t.GetProperty("text");
                if (textProp != null)
                {
                    textProp.SetValue(tmpComp, text);

                    // Invoke onValueChanged
                    var onValueChanged = t.GetProperty("onValueChanged")?.GetValue(tmpComp);
                    onValueChanged?.GetType().GetMethod("Invoke", new[] { typeof(string) })?.Invoke(onValueChanged, new object[] { text });

                    // Invoke onEndEdit
                    var onEndEdit = t.GetProperty("onEndEdit")?.GetValue(tmpComp);
                    onEndEdit?.GetType().GetMethod("Invoke", new[] { typeof(string) })?.Invoke(onEndEdit, new object[] { text });

                    return new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = $"Set TMP_InputField text on '{path}'"
                    };
                }
            }

            return McpServer.CreateError($"No InputField or TMP_InputField found on '{path}'", "not_found_error");
        }
    }
}
