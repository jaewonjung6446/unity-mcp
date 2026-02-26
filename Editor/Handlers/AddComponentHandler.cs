using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class AddComponentHandler : IToolHandler
    {
        public string Name => "add_component";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var componentType = parameters["componentType"]?.ToString();
            if (string.IsNullOrEmpty(componentType))
                return McpServer.CreateError("Missing required parameter: componentType", "validation_error");

            var type = FindComponentType(componentType);
            if (type == null)
                return McpServer.CreateError($"Unknown component type: {componentType}", "validation_error");

            Undo.AddComponent(go, type);
            var component = go.GetComponent(type);

            if (component == null)
                return McpServer.CreateError($"Failed to add component {componentType}", "execution_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Added {type.Name} to '{go.name}'",
                ["componentInstanceId"] = component.GetInstanceID(),
                ["componentType"] = type.FullName
            };
        }

        private static Type FindComponentType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName, false, true);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }

            string[] prefixes = { "UnityEngine.", "UnityEngine.UI.", "TMPro.", "UnityEngine.Audio." };
            foreach (var prefix in prefixes)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(prefix + typeName, false, true);
                    if (type != null && typeof(Component).IsAssignableFrom(type))
                        return type;
                }
            }

            return null;
        }
    }
}
