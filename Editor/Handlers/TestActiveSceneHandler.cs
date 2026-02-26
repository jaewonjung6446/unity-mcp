using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpUnity.Handlers
{
    public class TestActiveSceneHandler : IToolHandler
    {
        public string Name => "test_active_scene";

        public JObject Execute(JObject parameters)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var issues = new JArray();

            // Check for missing scripts
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        issues.Add(new JObject
                        {
                            ["type"] = "missing_script",
                            ["gameObject"] = go.name,
                            ["path"] = GetGameObjectHandler.GetPath(go),
                            ["message"] = $"Missing script on '{go.name}' at component index {i}"
                        });
                    }
                }
            }

            // Check for missing references (renderer materials)
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer.sharedMaterial == null)
                {
                    issues.Add(new JObject
                    {
                        ["type"] = "missing_material",
                        ["gameObject"] = renderer.gameObject.name,
                        ["path"] = GetGameObjectHandler.GetPath(renderer.gameObject),
                        ["message"] = $"Missing material on '{renderer.gameObject.name}'"
                    });
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = issues.Count == 0
                    ? $"Scene '{scene.name}' passed all tests"
                    : $"Scene '{scene.name}' has {issues.Count} issues",
                ["sceneName"] = scene.name,
                ["issueCount"] = issues.Count,
                ["issues"] = issues
            };
        }
    }
}
