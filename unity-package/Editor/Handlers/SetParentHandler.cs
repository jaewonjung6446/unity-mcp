using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetParentHandler : IToolHandler
    {
        public string Name => "set_parent";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var parentPath = parameters["parentPath"]?.ToString();
            var parentInstanceId = parameters["parentInstanceId"];
            var worldPositionStays = parameters["worldPositionStays"]?.ToObject<bool>() ?? true;

            Transform newParent = null;

            if (parentInstanceId != null)
            {
                var parentGo = EditorUtility.InstanceIDToObject(parentInstanceId.ToObject<int>()) as GameObject;
                if (parentGo != null) newParent = parentGo.transform;
            }
            else if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGo = GameObject.Find(parentPath);
                if (parentGo != null) newParent = parentGo.transform;
            }
            // If both are null/empty, we unparent (set to root)

            Undo.SetTransformParent(go.transform, newParent, $"Set parent of {go.name}");

            if (!worldPositionStays)
            {
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = newParent != null
                    ? $"Set parent of '{go.name}' to '{newParent.name}'"
                    : $"Unparented '{go.name}' to scene root",
                ["instanceId"] = go.GetInstanceID(),
                ["path"] = GetGameObjectHandler.GetPath(go)
            };
        }
    }
}
