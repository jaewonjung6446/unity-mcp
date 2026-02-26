using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace McpUnity.Handlers
{
    public class CloseSceneHandler : IToolHandler
    {
        public string Name => "close_scene";

        public JObject Execute(JObject parameters)
        {
            var sceneName = parameters["sceneName"]?.ToString();
            if (string.IsNullOrEmpty(sceneName))
                return McpServer.CreateError("Missing required parameter: sceneName", "validation_error");

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName)
                {
                    if (scene.isDirty)
                        EditorSceneManager.SaveScene(scene);
                    EditorSceneManager.CloseScene(scene, true);
                    return new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = $"Closed scene '{sceneName}'"
                    };
                }
            }

            return McpServer.CreateError($"Scene '{sceneName}' not found", "not_found_error");
        }
    }
}
