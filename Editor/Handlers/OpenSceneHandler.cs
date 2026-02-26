using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace McpUnity.Handlers
{
    public class OpenSceneHandler : IToolHandler
    {
        public string Name => "open_scene";

        public JObject Execute(JObject parameters)
        {
            var path = parameters["scenePath"]?.ToString();
            if (string.IsNullOrEmpty(path))
                return McpServer.CreateError("Missing required parameter: scenePath", "validation_error");

            var mode = parameters["additive"]?.ToObject<bool>() == true
                ? OpenSceneMode.Additive
                : OpenSceneMode.Single;

            if (EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            var scene = EditorSceneManager.OpenScene(path, mode);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Opened scene '{scene.name}'"
            };
        }
    }
}
