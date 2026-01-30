using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetGameObjectHandler : IToolHandler
    {
        public string Name => "get_game_object";

        public JObject Execute(JObject parameters)
        {
            var idOrName = parameters["idOrName"]?.ToString();
            if (string.IsNullOrEmpty(idOrName))
                return McpServer.CreateError("Missing required parameter: idOrName", "validation_error");

            GameObject go = null;
            if (int.TryParse(idOrName, out int instanceId))
                go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            else
                go = GameObject.Find(idOrName);

            if (go == null)
                return McpServer.CreateError($"GameObject '{idOrName}' not found", "not_found_error");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Retrieved GameObject '{go.name}'",
                ["gameObject"] = SerializeGameObject(go),
                ["instanceId"] = go.GetInstanceID()
            };
        }

        public static JObject SerializeGameObject(GameObject go, bool includeComponents = true)
        {
            var obj = new JObject
            {
                ["name"] = go.name,
                ["instanceId"] = go.GetInstanceID(),
                ["activeSelf"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["isStatic"] = go.isStatic,
                ["path"] = GetPath(go)
            };

            var t = go.transform;
            obj["transform"] = new JObject
            {
                ["position"] = Vec3(t.position),
                ["localPosition"] = Vec3(t.localPosition),
                ["rotation"] = Vec3(t.eulerAngles),
                ["localRotation"] = Vec3(t.localEulerAngles),
                ["localScale"] = Vec3(t.localScale)
            };

            if (includeComponents)
            {
                var comps = new JArray();
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null) continue;
                    var cObj = new JObject
                    {
                        ["type"] = c.GetType().Name,
                        ["instanceId"] = c.GetInstanceID()
                    };
                    if (c is Behaviour b)
                        cObj["enabled"] = b.enabled;
                    comps.Add(cObj);
                }
                obj["components"] = comps;
            }

            var children = new JArray();
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                children.Add(new JObject
                {
                    ["name"] = child.name,
                    ["instanceId"] = child.GetInstanceID(),
                    ["activeSelf"] = child.activeSelf
                });
            }
            obj["children"] = children;

            return obj;
        }

        private static JObject Vec3(Vector3 v) => new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };

        public static string GetPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        /// <summary>
        /// Resolve a GameObject from JObject containing instanceId or gameObjectPath.
        /// Returns null and sets error if not found.
        /// </summary>
        public static GameObject ResolveGameObject(JObject parameters, out JObject error)
        {
            error = null;
            var instanceId = parameters["instanceId"];
            var goPath = parameters["gameObjectPath"]?.ToString();

            GameObject go = null;
            if (instanceId != null)
                go = EditorUtility.InstanceIDToObject(instanceId.ToObject<int>()) as GameObject;
            else if (!string.IsNullOrEmpty(goPath))
                go = GameObject.Find(goPath);

            if (go == null)
            {
                error = McpServer.CreateError(
                    $"GameObject not found. instanceId={instanceId}, gameObjectPath={goPath}",
                    "not_found_error");
            }
            return go;
        }
    }
}
