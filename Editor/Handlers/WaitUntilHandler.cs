using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class WaitUntilHandler : ICoroutineToolHandler
    {
        public string Name => "wait_until";

        public JObject Execute(JObject parameters)
        {
            return McpServer.CreateError("wait_until requires coroutine execution", "invalid_state");
        }

        public IEnumerator ExecuteCoroutine(JObject parameters, Action<JObject> onComplete)
        {
            var condition = parameters["condition"]?.ToString();
            if (string.IsNullOrEmpty(condition))
            {
                onComplete(McpServer.CreateError("Missing required parameter: condition", "validation_error"));
                yield break;
            }

            var timeout = parameters["timeout"]?.ToObject<float>() ?? 10f;
            var pollInterval = parameters["pollInterval"]?.ToObject<float>() ?? 0.2f;
            timeout = Mathf.Clamp(timeout, 0.1f, 30f);
            pollInterval = Mathf.Clamp(pollInterval, 0.05f, 5f);

            var startTime = Time.realtimeSinceStartup;

            while (true)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;

                bool conditionMet;
                try
                {
                    conditionMet = CheckCondition(parameters, condition);
                }
                catch (Exception ex)
                {
                    onComplete(McpServer.CreateError($"Condition check error: {ex.Message}", "tool_error"));
                    yield break;
                }

                if (conditionMet)
                {
                    elapsed = Time.realtimeSinceStartup - startTime;
                    onComplete(new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = $"Condition '{condition}' met after {elapsed:F2}s",
                        ["elapsed"] = Math.Round(elapsed, 2)
                    });
                    yield break;
                }

                if (elapsed >= timeout)
                {
                    onComplete(McpServer.CreateError(
                        $"Timeout ({timeout}s) waiting for condition '{condition}'",
                        "timeout_error"));
                    yield break;
                }

                // Wait for pollInterval
                var waitUntil = Time.realtimeSinceStartup + pollInterval;
                while (Time.realtimeSinceStartup < waitUntil)
                    yield return null;
            }
        }

        private bool CheckCondition(JObject parameters, string condition)
        {
            switch (condition)
            {
                case "gameObjectActive":
                    return CheckGameObjectActive(parameters, true);
                case "gameObjectInactive":
                    return CheckGameObjectActive(parameters, false);
                case "objectExists":
                    return CheckObjectExists(parameters, true);
                case "objectDestroyed":
                    return CheckObjectExists(parameters, false);
                case "uiTextEquals":
                    return CheckUiText(parameters, false);
                case "uiTextContains":
                    return CheckUiText(parameters, true);
                case "componentValue":
                    return CheckComponentValue(parameters);
                default:
                    throw new ArgumentException($"Unknown condition: {condition}");
            }
        }

        private bool CheckGameObjectActive(JObject parameters, bool expectActive)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out _);
            if (go == null) return !expectActive;
            return go.activeInHierarchy == expectActive;
        }

        private bool CheckObjectExists(JObject parameters, bool expectExists)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out _);
            return (go != null) == expectExists;
        }

        private bool CheckUiText(JObject parameters, bool contains)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out _);
            if (go == null) return false;

            var expectedValue = parameters["expectedValue"]?.ToString();
            if (expectedValue == null)
                throw new ArgumentException("expectedValue required for uiText conditions");

            // Check legacy Text
            var text = go.GetComponent<Text>();
            if (text != null)
            {
                return contains
                    ? text.text.Contains(expectedValue)
                    : text.text == expectedValue;
            }

            // Check TMP_Text via reflection
            foreach (var c in go.GetComponents<Component>())
            {
                if (c != null && FindUiElementsHandler.IsTmpText(c))
                {
                    var textValue = c.GetType().GetProperty("text")?.GetValue(c) as string;
                    if (textValue != null)
                    {
                        return contains
                            ? textValue.Contains(expectedValue)
                            : textValue == expectedValue;
                    }
                }
            }

            return false;
        }

        private bool CheckComponentValue(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out _);
            if (go == null) return false;

            var componentType = parameters["componentType"]?.ToString();
            var fieldName = parameters["fieldName"]?.ToString();
            var expectedValue = parameters["expectedValue"]?.ToString();

            if (string.IsNullOrEmpty(componentType) || string.IsNullOrEmpty(fieldName))
                throw new ArgumentException("componentType and fieldName required for componentValue condition");

            var comp = go.GetComponent(componentType);
            if (comp == null) return false;

            var type = comp.GetType();

            // Try property first, then field
            var prop = type.GetProperty(fieldName);
            if (prop != null)
            {
                var val = prop.GetValue(comp);
                return val?.ToString() == expectedValue;
            }

            var field = type.GetField(fieldName);
            if (field != null)
            {
                var val = field.GetValue(comp);
                return val?.ToString() == expectedValue;
            }

            return false;
        }
    }
}
