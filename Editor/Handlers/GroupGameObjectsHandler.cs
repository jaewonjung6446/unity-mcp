using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GroupGameObjectsHandler : IToolHandler
    {
        public string Name => "group_game_objects";

        public JObject Execute(JObject parameters)
        {
            var instanceIds = parameters["instanceIds"] as JArray;
            var gameObjectPaths = parameters["gameObjectPaths"] as JArray;
            var groupName = parameters["groupName"]?.ToString() ?? "Group";

            var objects = new List<GameObject>();

            if (instanceIds != null)
            {
                foreach (var id in instanceIds)
                {
                    var go = EditorUtility.InstanceIDToObject(id.ToObject<int>()) as GameObject;
                    if (go != null) objects.Add(go);
                }
            }
            else if (gameObjectPaths != null)
            {
                foreach (var path in gameObjectPaths)
                {
                    var go = GameObject.Find(path.ToString());
                    if (go != null) objects.Add(go);
                }
            }

            if (objects.Count == 0)
                return McpServer.CreateError("No valid GameObjects found to group", "validation_error");

            // Find common parent
            var commonParent = objects[0].transform.parent;

            // Create group
            var group = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(group, $"Create group {groupName}");

            if (commonParent != null)
                Undo.SetTransformParent(group.transform, commonParent, $"Parent group");

            // Calculate center position
            var center = Vector3.zero;
            foreach (var go in objects)
                center += go.transform.position;
            center /= objects.Count;
            group.transform.position = center;

            // Parent all objects to group
            foreach (var go in objects)
                Undo.SetTransformParent(go.transform, group.transform, $"Group {go.name}");

            EditorSceneManager.MarkSceneDirty(group.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Grouped {objects.Count} objects under '{groupName}'",
                ["groupInstanceId"] = group.GetInstanceID(),
                ["groupPath"] = GetGameObjectHandler.GetPath(group),
                ["childCount"] = objects.Count
            };
        }
    }
}
