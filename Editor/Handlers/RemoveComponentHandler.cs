using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class RemoveComponentHandler : IToolHandler
    {
        public string Name => "remove_component";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var componentType = parameters["componentType"]?.ToString();
            if (string.IsNullOrEmpty(componentType))
                return McpServer.CreateError("Missing required parameter: componentType", "validation_error");

            Component target = null;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                if (string.Equals(c.GetType().Name, componentType, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.GetType().FullName, componentType, StringComparison.OrdinalIgnoreCase))
                {
                    target = c;
                    break;
                }
            }

            if (target == null)
                return McpServer.CreateError($"Component '{componentType}' not found on '{go.name}'", "not_found_error");

            if (target is Transform)
                return McpServer.CreateError("Cannot remove Transform component", "validation_error");

            Undo.DestroyObjectImmediate(target);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Removed {componentType} from '{go.name}'"
            };
        }
    }
}
