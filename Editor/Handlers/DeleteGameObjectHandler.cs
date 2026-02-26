using Newtonsoft.Json.Linq;
using UnityEditor;

namespace McpUnity.Handlers
{
    public class DeleteGameObjectHandler : IToolHandler
    {
        public string Name => "delete_game_object";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var name = go.name;
            var path = GetGameObjectHandler.GetPath(go);

            Undo.DestroyObjectImmediate(go);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Deleted GameObject '{name}' at '{path}'"
            };
        }
    }
}
