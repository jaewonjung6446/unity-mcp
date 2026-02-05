using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class FindObjectsByCriteriaHandler : IToolHandler
    {
        public string Name => "find_objects_by_criteria";

        public JObject Execute(JObject parameters)
        {
            var tag = parameters["tag"]?.ToString();
            var layerName = parameters["layer"]?.ToString();
            var componentType = parameters["componentType"]?.ToString();
            var nameContains = parameters["nameContains"]?.ToString();

            if (string.IsNullOrEmpty(tag) && string.IsNullOrEmpty(layerName) &&
                string.IsNullOrEmpty(componentType) && string.IsNullOrEmpty(nameContains))
            {
                return McpServer.CreateError(
                    "At least one search criterion required: tag, layer, componentType, or nameContains",
                    "validation_error");
            }

            int? layerIndex = null;
            if (!string.IsNullOrEmpty(layerName))
            {
                layerIndex = LayerMask.NameToLayer(layerName);
                if (layerIndex == -1)
                {
                    // Try parsing as integer
                    if (int.TryParse(layerName, out int parsed))
                        layerIndex = parsed;
                    else
                        return McpServer.CreateError($"Unknown layer: {layerName}", "validation_error");
                }
            }

            // Find component type if specified
            Type compTypeFilter = null;
            if (!string.IsNullOrEmpty(componentType))
            {
                compTypeFilter = FindType(componentType);
                if (compTypeFilter == null)
                    return McpServer.CreateError($"Unknown component type: {componentType}", "validation_error");
            }

            // Get all GameObjects
            GameObject[] allObjects;
            if (!string.IsNullOrEmpty(tag))
            {
                try
                {
                    allObjects = GameObject.FindGameObjectsWithTag(tag);
                }
                catch
                {
                    return McpServer.CreateError($"Invalid tag: {tag}", "validation_error");
                }
            }
            else
            {
#if UNITY_2022_1_OR_NEWER
                allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
                allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
#endif
            }

            var results = new JArray();
            foreach (var go in allObjects)
            {
                // Apply filters
                if (layerIndex.HasValue && go.layer != layerIndex.Value)
                    continue;

                if (!string.IsNullOrEmpty(nameContains) &&
                    go.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (compTypeFilter != null && go.GetComponent(compTypeFilter) == null)
                    continue;

                var entry = new JObject
                {
                    ["name"] = go.name,
                    ["instanceId"] = go.GetInstanceID(),
                    ["path"] = GetGameObjectHandler.GetPath(go),
                    ["activeSelf"] = go.activeSelf,
                    ["tag"] = go.tag,
                    ["layer"] = LayerMask.LayerToName(go.layer),
                    ["position"] = new JObject
                    {
                        ["x"] = go.transform.position.x,
                        ["y"] = go.transform.position.y,
                        ["z"] = go.transform.position.z
                    }
                };

                results.Add(entry);

                // Cap results to prevent huge payloads
                if (results.Count >= 200)
                    break;
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} objects matching criteria",
                ["results"] = results,
                ["capped"] = results.Count >= 200
            };
        }

        private static Type FindType(string typeName)
        {
            // Search across all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Try exact match
                var type = assembly.GetType(typeName, false, true);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }

            // Try common Unity namespaces
            string[] prefixes = { "UnityEngine.", "UnityEngine.UI.", "TMPro." };
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
