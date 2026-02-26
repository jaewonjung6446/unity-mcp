using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Handlers
{
    public class GetSceneHierarchyHandler : IToolHandler
    {
        public string Name => "get_scene_hierarchy";

        public JObject Execute(JObject parameters)
        {
            var rootPath = parameters["rootPath"]?.ToString();
            var maxDepth = parameters["maxDepth"]?.ToObject<int>() ?? 10;
            var includeInactive = parameters["includeInactive"]?.ToObject<bool>() ?? true;

            if (maxDepth <= 0) maxDepth = 1;
            if (maxDepth > 50) maxDepth = 50;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return McpServer.CreateError("No active scene loaded", "invalid_state");

            var hierarchy = new JArray();

            if (!string.IsNullOrEmpty(rootPath))
            {
                var rootGo = GameObject.Find(rootPath);
                if (rootGo == null)
                    return McpServer.CreateError($"Root object '{rootPath}' not found", "not_found_error");

                hierarchy.Add(SerializeNode(rootGo, maxDepth, includeInactive));
            }
            else
            {
                var rootObjects = scene.GetRootGameObjects();
                foreach (var go in rootObjects)
                {
                    if (!includeInactive && !go.activeSelf) continue;
                    hierarchy.Add(SerializeNode(go, maxDepth, includeInactive));
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Scene hierarchy for '{scene.name}'",
                ["sceneName"] = scene.name,
                ["hierarchy"] = hierarchy
            };
        }

        private static JObject SerializeNode(GameObject go, int depthRemaining, bool includeInactive)
        {
            var components = go.GetComponents<Component>();
            var componentNames = new JArray();
            foreach (var c in components)
            {
                if (c != null)
                    componentNames.Add(c.GetType().Name);
            }

            var node = new JObject
            {
                ["name"] = go.name,
                ["instanceId"] = go.GetInstanceID(),
                ["activeSelf"] = go.activeSelf,
                ["components"] = componentNames,
                ["childCount"] = go.transform.childCount
            };

            if (depthRemaining > 1 && go.transform.childCount > 0)
            {
                var children = new JArray();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i).gameObject;
                    if (!includeInactive && !child.activeSelf) continue;
                    children.Add(SerializeNode(child, depthRemaining - 1, includeInactive));
                }
                node["children"] = children;
            }

            return node;
        }
    }
}
