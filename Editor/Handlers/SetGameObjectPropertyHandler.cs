using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetGameObjectPropertyHandler : IToolHandler
    {
        public string Name => "set_game_object_property";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            Undo.RecordObject(go, $"Set properties on {go.name}");
            Undo.RecordObject(go.transform, $"Set transform on {go.name}");

            var changes = new JArray();

            // Name
            var newName = parameters["name"]?.ToString();
            if (newName != null)
            {
                go.name = newName;
                changes.Add("name");
            }

            // Tag
            var tag = parameters["tag"]?.ToString();
            if (tag != null)
            {
                go.tag = tag;
                changes.Add("tag");
            }

            // Layer
            var layer = parameters["layer"];
            if (layer != null)
            {
                var layerStr = layer.ToString();
                int layerIndex = LayerMask.NameToLayer(layerStr);
                if (layerIndex == -1 && int.TryParse(layerStr, out int parsed))
                    layerIndex = parsed;
                if (layerIndex >= 0)
                {
                    go.layer = layerIndex;
                    changes.Add("layer");
                }
            }

            // Active
            var active = parameters["active"];
            if (active != null)
            {
                go.SetActive(active.ToObject<bool>());
                changes.Add("active");
            }

            // Static
            var isStatic = parameters["isStatic"];
            if (isStatic != null)
            {
                go.isStatic = isStatic.ToObject<bool>();
                changes.Add("isStatic");
            }

            // Position
            var position = parameters["position"] as JObject;
            if (position != null)
            {
                go.transform.localPosition = new Vector3(
                    position["x"]?.ToObject<float>() ?? go.transform.localPosition.x,
                    position["y"]?.ToObject<float>() ?? go.transform.localPosition.y,
                    position["z"]?.ToObject<float>() ?? go.transform.localPosition.z
                );
                changes.Add("position");
            }

            // Rotation
            var rotation = parameters["rotation"] as JObject;
            if (rotation != null)
            {
                go.transform.localEulerAngles = new Vector3(
                    rotation["x"]?.ToObject<float>() ?? go.transform.localEulerAngles.x,
                    rotation["y"]?.ToObject<float>() ?? go.transform.localEulerAngles.y,
                    rotation["z"]?.ToObject<float>() ?? go.transform.localEulerAngles.z
                );
                changes.Add("rotation");
            }

            // Scale
            var scale = parameters["scale"] as JObject;
            if (scale != null)
            {
                go.transform.localScale = new Vector3(
                    scale["x"]?.ToObject<float>() ?? go.transform.localScale.x,
                    scale["y"]?.ToObject<float>() ?? go.transform.localScale.y,
                    scale["z"]?.ToObject<float>() ?? go.transform.localScale.z
                );
                changes.Add("scale");
            }

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Updated {changes.Count} properties on '{go.name}'",
                ["changed"] = changes,
                ["instanceId"] = go.GetInstanceID()
            };
        }
    }
}
