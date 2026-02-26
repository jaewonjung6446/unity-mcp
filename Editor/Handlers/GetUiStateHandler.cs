using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class GetUiStateHandler : IToolHandler
    {
        public string Name => "get_ui_state";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["gameObjectPath"] = GetGameObjectHandler.GetPath(go),
                ["instanceId"] = go.GetInstanceID(),
                ["activeSelf"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy
            };

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                var canvas = go.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var screenRect = FindUiElementsHandler.GetScreenRect(rt, canvas);
                    result["rect"] = new JObject
                    {
                        ["x"] = screenRect.x,
                        ["y"] = screenRect.y,
                        ["width"] = screenRect.width,
                        ["height"] = screenRect.height
                    };
                }
            }

            // Button
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                result["button"] = new JObject
                {
                    ["interactable"] = btn.interactable
                };
            }

            // Toggle
            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                result["toggle"] = new JObject
                {
                    ["isOn"] = toggle.isOn,
                    ["interactable"] = toggle.interactable
                };
            }

            // Slider
            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                result["slider"] = new JObject
                {
                    ["value"] = slider.value,
                    ["minValue"] = slider.minValue,
                    ["maxValue"] = slider.maxValue
                };
            }

            // InputField (legacy)
            var inputField = go.GetComponent<InputField>();
            if (inputField != null)
            {
                result["inputField"] = new JObject
                {
                    ["text"] = inputField.text
                };
            }

            // TMP_InputField (reflection)
            var tmpInput = GetTmpInputField(go);
            if (tmpInput != null)
            {
                result["inputField"] = new JObject
                {
                    ["text"] = tmpInput
                };
            }

            // Dropdown (legacy)
            var dropdown = go.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                var opts = new JArray();
                foreach (var o in dropdown.options)
                    opts.Add(o.text);
                result["dropdown"] = new JObject
                {
                    ["value"] = dropdown.value,
                    ["options"] = opts
                };
            }

            // TMP_Dropdown (reflection)
            PopulateTmpDropdown(go, result);

            // Text content
            var text = go.GetComponent<Text>();
            if (text != null)
                result["textContent"] = text.text;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c != null && FindUiElementsHandler.IsTmpText(c))
                {
                    result["textContent"] = c.GetType().GetProperty("text")?.GetValue(c) as string;
                    break;
                }
            }

            return result;
        }

        private static string GetTmpInputField(GameObject go)
        {
            var comp = go.GetComponent("TMP_InputField");
            if (comp == null) return null;
            var prop = comp.GetType().GetProperty("text");
            return prop?.GetValue(comp) as string;
        }

        private static void PopulateTmpDropdown(GameObject go, JObject result)
        {
            var comp = go.GetComponent("TMP_Dropdown");
            if (comp == null) return;
            var t = comp.GetType();
            var valueProp = t.GetProperty("value");
            var optionsProp = t.GetProperty("options");
            if (valueProp == null || optionsProp == null) return;

            var val = (int)valueProp.GetValue(comp);
            var options = optionsProp.GetValue(comp) as System.Collections.IList;
            var opts = new JArray();
            if (options != null)
            {
                foreach (var o in options)
                {
                    var textProp = o.GetType().GetProperty("text");
                    opts.Add(textProp?.GetValue(o)?.ToString() ?? "");
                }
            }
            result["dropdown"] = new JObject
            {
                ["value"] = val,
                ["options"] = opts
            };
        }
    }
}
