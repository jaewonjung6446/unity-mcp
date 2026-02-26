using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class FocusGameObjectHandler : IToolHandler
    {
        public string Name => "focus_game_object";

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

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            SceneView.lastActiveSceneView?.FrameSelected();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Focused on '{go.name}'"
            };
        }
    }
}
