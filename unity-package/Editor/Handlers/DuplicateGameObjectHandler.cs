using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class DuplicateGameObjectHandler : IToolHandler
    {
        public string Name => "duplicate_game_object";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var count = parameters["count"]?.ToObject<int>() ?? 1;
            count = System.Math.Max(1, System.Math.Min(count, 100));

            var results = new JArray();

            for (int i = 0; i < count; i++)
            {
                var clone = Object.Instantiate(go, go.transform.parent);
                clone.name = go.name + (count > 1 ? $" ({i + 1})" : "");
                Undo.RegisterCreatedObjectUndo(clone, $"Duplicate {go.name}");

                var offset = parameters["offset"] as JObject;
                if (offset != null)
                {
                    clone.transform.localPosition += new Vector3(
                        (offset["x"]?.ToObject<float>() ?? 0f) * (i + 1),
                        (offset["y"]?.ToObject<float>() ?? 0f) * (i + 1),
                        (offset["z"]?.ToObject<float>() ?? 0f) * (i + 1)
                    );
                }

                results.Add(new JObject
                {
                    ["name"] = clone.name,
                    ["instanceId"] = clone.GetInstanceID(),
                    ["path"] = GetGameObjectHandler.GetPath(clone)
                });
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Duplicated '{go.name}' {count} time(s)",
                ["duplicates"] = results
            };
        }
    }
}
