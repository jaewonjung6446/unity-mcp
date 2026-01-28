using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;

namespace McpUnity.Handlers
{
    public class SaveSceneHandler : IToolHandler
    {
        public string Name => "save_scene";

        public JObject Execute(JObject parameters)
        {
            var scene = EditorSceneManager.GetActiveScene();
            bool saved = EditorSceneManager.SaveScene(scene);
            return new JObject
            {
                ["success"] = saved,
                ["type"] = "text",
                ["message"] = saved ? $"Saved scene '{scene.name}'" : "Failed to save scene"
            };
        }
    }
}
