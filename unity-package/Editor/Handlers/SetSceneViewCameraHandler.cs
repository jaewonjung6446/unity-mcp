using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetSceneViewCameraHandler : IToolHandler
    {
        public string Name => "set_scene_view_camera";

        public JObject Execute(JObject parameters)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return McpServer.CreateError("No active Scene View found", "not_found_error");

            var changed = new JArray();

            // Position (pivot)
            var position = parameters["position"] as JObject;
            if (position != null)
            {
                sceneView.pivot = new Vector3(
                    position["x"]?.ToObject<float>() ?? sceneView.pivot.x,
                    position["y"]?.ToObject<float>() ?? sceneView.pivot.y,
                    position["z"]?.ToObject<float>() ?? sceneView.pivot.z
                );
                changed.Add("position");
            }

            // Rotation
            var rotation = parameters["rotation"] as JObject;
            if (rotation != null)
            {
                sceneView.rotation = Quaternion.Euler(
                    rotation["x"]?.ToObject<float>() ?? 0f,
                    rotation["y"]?.ToObject<float>() ?? 0f,
                    rotation["z"]?.ToObject<float>() ?? 0f
                );
                changed.Add("rotation");
            }

            // Size (zoom level)
            var size = parameters["size"];
            if (size != null)
            {
                sceneView.size = size.ToObject<float>();
                changed.Add("size");
            }

            // Orthographic
            var orthographic = parameters["orthographic"];
            if (orthographic != null)
            {
                sceneView.orthographic = orthographic.ToObject<bool>();
                changed.Add("orthographic");
            }

            // Preset views
            var preset = parameters["preset"]?.ToString()?.ToLower();
            if (!string.IsNullOrEmpty(preset))
            {
                switch (preset)
                {
                    case "top":
                        sceneView.rotation = Quaternion.Euler(90, 0, 0);
                        sceneView.orthographic = true;
                        changed.Add("preset:top");
                        break;
                    case "bottom":
                        sceneView.rotation = Quaternion.Euler(-90, 0, 0);
                        sceneView.orthographic = true;
                        changed.Add("preset:bottom");
                        break;
                    case "front":
                        sceneView.rotation = Quaternion.Euler(0, 0, 0);
                        sceneView.orthographic = true;
                        changed.Add("preset:front");
                        break;
                    case "back":
                        sceneView.rotation = Quaternion.Euler(0, 180, 0);
                        sceneView.orthographic = true;
                        changed.Add("preset:back");
                        break;
                    case "left":
                        sceneView.rotation = Quaternion.Euler(0, 90, 0);
                        sceneView.orthographic = true;
                        changed.Add("preset:left");
                        break;
                    case "right":
                        sceneView.rotation = Quaternion.Euler(0, -90, 0);
                        sceneView.orthographic = true;
                        changed.Add("preset:right");
                        break;
                    case "perspective":
                        sceneView.rotation = Quaternion.Euler(30, -45, 0);
                        sceneView.orthographic = false;
                        changed.Add("preset:perspective");
                        break;
                }
            }

            sceneView.Repaint();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Updated Scene View camera ({changed.Count} changes)",
                ["changed"] = changed,
                ["current"] = new JObject
                {
                    ["pivot"] = new JObject { ["x"] = sceneView.pivot.x, ["y"] = sceneView.pivot.y, ["z"] = sceneView.pivot.z },
                    ["rotation"] = new JObject { ["x"] = sceneView.rotation.eulerAngles.x, ["y"] = sceneView.rotation.eulerAngles.y, ["z"] = sceneView.rotation.eulerAngles.z },
                    ["size"] = sceneView.size,
                    ["orthographic"] = sceneView.orthographic
                }
            };
        }
    }
}
