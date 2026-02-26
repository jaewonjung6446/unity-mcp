using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class CreateGameObjectHandler : IToolHandler
    {
        public string Name => "create_game_object";

        public JObject Execute(JObject parameters)
        {
            var name = parameters["name"]?.ToString() ?? "GameObject";
            var primitiveType = parameters["primitiveType"]?.ToString();
            var parentPath = parameters["parentPath"]?.ToString();

            GameObject go;

            if (!string.IsNullOrEmpty(primitiveType))
            {
                if (!System.Enum.TryParse<PrimitiveType>(primitiveType, true, out var pt))
                    return McpServer.CreateError($"Unknown primitive type: {primitiveType}. Valid: Sphere, Capsule, Cylinder, Cube, Plane, Quad", "validation_error");
                go = GameObject.CreatePrimitive(pt);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            // Set parent
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                {
                    Undo.SetTransformParent(go.transform, parent.transform, $"Set parent of {name}");
                }
            }

            // Set position
            var position = parameters["position"] as JObject;
            if (position != null)
            {
                var pos = new Vector3(
                    position["x"]?.ToObject<float>() ?? 0f,
                    position["y"]?.ToObject<float>() ?? 0f,
                    position["z"]?.ToObject<float>() ?? 0f
                );
                go.transform.localPosition = pos;
            }

            // Set rotation
            var rotation = parameters["rotation"] as JObject;
            if (rotation != null)
            {
                go.transform.localEulerAngles = new Vector3(
                    rotation["x"]?.ToObject<float>() ?? 0f,
                    rotation["y"]?.ToObject<float>() ?? 0f,
                    rotation["z"]?.ToObject<float>() ?? 0f
                );
            }

            // Set scale
            var scale = parameters["scale"] as JObject;
            if (scale != null)
            {
                go.transform.localScale = new Vector3(
                    scale["x"]?.ToObject<float>() ?? 1f,
                    scale["y"]?.ToObject<float>() ?? 1f,
                    scale["z"]?.ToObject<float>() ?? 1f
                );
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created GameObject '{go.name}'",
                ["instanceId"] = go.GetInstanceID(),
                ["path"] = GetGameObjectHandler.GetPath(go)
            };
        }
    }
}
