using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class AlignGameObjectHandler : IToolHandler
    {
        public string Name => "align_game_object";

        public JObject Execute(JObject parameters)
        {
            var axis = parameters["axis"]?.ToString()?.ToLower();
            var alignTo = parameters["alignTo"]?.ToString()?.ToLower() ?? "center";

            // Get targets
            var instanceIds = parameters["instanceIds"] as JArray;
            var objects = new List<GameObject>();

            if (instanceIds != null)
            {
                foreach (var id in instanceIds)
                {
                    var go = EditorUtility.InstanceIDToObject(id.ToObject<int>()) as GameObject;
                    if (go != null) objects.Add(go);
                }
            }
            else
            {
                // Use current selection
                foreach (var go in Selection.gameObjects)
                    objects.Add(go);
            }

            if (objects.Count < 2)
                return McpServer.CreateError("Need at least 2 objects to align", "validation_error");

            if (string.IsNullOrEmpty(axis) || (axis != "x" && axis != "y" && axis != "z"))
                return McpServer.CreateError("axis must be 'x', 'y', or 'z'", "validation_error");

            // Calculate target value
            float targetValue = 0f;
            switch (alignTo)
            {
                case "min":
                    targetValue = float.MaxValue;
                    foreach (var go in objects)
                        targetValue = Mathf.Min(targetValue, GetAxis(go.transform.position, axis));
                    break;
                case "max":
                    targetValue = float.MinValue;
                    foreach (var go in objects)
                        targetValue = Mathf.Max(targetValue, GetAxis(go.transform.position, axis));
                    break;
                case "center":
                    foreach (var go in objects)
                        targetValue += GetAxis(go.transform.position, axis);
                    targetValue /= objects.Count;
                    break;
                case "first":
                    targetValue = GetAxis(objects[0].transform.position, axis);
                    break;
                default:
                    return McpServer.CreateError("alignTo must be 'min', 'max', 'center', or 'first'", "validation_error");
            }

            // Apply alignment
            foreach (var go in objects)
            {
                Undo.RecordObject(go.transform, $"Align {go.name}");
                var pos = go.transform.position;
                SetAxis(ref pos, axis, targetValue);
                go.transform.position = pos;
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Aligned {objects.Count} objects on {axis} axis to {alignTo} ({targetValue:F2})",
                ["alignedCount"] = objects.Count
            };
        }

        private float GetAxis(Vector3 v, string axis) => axis == "x" ? v.x : axis == "y" ? v.y : v.z;

        private void SetAxis(ref Vector3 v, string axis, float value)
        {
            if (axis == "x") v.x = value;
            else if (axis == "y") v.y = value;
            else v.z = value;
        }
    }
}
