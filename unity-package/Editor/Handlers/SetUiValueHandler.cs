using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class SetUiValueHandler : IToolHandler
    {
        public string Name => "set_ui_value";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var path = GetGameObjectHandler.GetPath(go);
            var value = parameters["value"];
            if (value == null)
                return McpServer.CreateError("Missing required parameter: value", "validation_error");

            // Try Slider
            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                float v = value.ToObject<float>();
                slider.value = v;
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Set Slider value to {v} on '{path}'"
                };
            }

            // Try Toggle
            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                bool v = value.ToObject<bool>();
                toggle.isOn = v;
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Set Toggle to {v} on '{path}'"
                };
            }

            // Try Dropdown (legacy)
            var dropdown = go.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                int v = value.ToObject<int>();
                dropdown.value = v;
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Set Dropdown value to {v} on '{path}'"
                };
            }

            // Try TMP_Dropdown via reflection
            var tmpDropdown = go.GetComponent("TMP_Dropdown");
            if (tmpDropdown != null)
            {
                int v = value.ToObject<int>();
                var valueProp = tmpDropdown.GetType().GetProperty("value");
                if (valueProp != null)
                {
                    valueProp.SetValue(tmpDropdown, v);
                    return new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = $"Set TMP_Dropdown value to {v} on '{path}'"
                    };
                }
            }

            // Try Scrollbar
            var scrollbar = go.GetComponent<Scrollbar>();
            if (scrollbar != null)
            {
                float v = value.ToObject<float>();
                scrollbar.value = v;
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Set Scrollbar value to {v} on '{path}'"
                };
            }

            return McpServer.CreateError(
                $"No supported UI component (Slider, Toggle, Dropdown, TMP_Dropdown, Scrollbar) found on '{path}'",
                "not_found_error");
        }
    }
}
