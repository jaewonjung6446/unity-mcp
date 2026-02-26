using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetGameObjectMaterialHandler : IToolHandler
    {
        public string Name => "set_game_object_material";

        public JObject Execute(JObject parameters)
        {
            var materialPath = parameters["materialPath"]?.ToString();
            if (string.IsNullOrEmpty(materialPath))
                return McpServer.CreateError("Missing required parameter: materialPath", "validation_error");

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return McpServer.CreateError($"Material not found at '{materialPath}'", "not_found_error");

            // Find the target GameObject by instanceId or gameObjectPath
            GameObject go = null;
            var instanceIdToken = parameters["instanceId"];
            var gameObjectPath = parameters["gameObjectPath"]?.ToString();

            if (instanceIdToken != null)
            {
                int instanceId = instanceIdToken.ToObject<int>();
                go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            }
            else if (!string.IsNullOrEmpty(gameObjectPath))
            {
                go = GameObject.Find(gameObjectPath);
            }
            else
            {
                return McpServer.CreateError("Must provide either instanceId or gameObjectPath", "validation_error");
            }

            if (go == null)
                return McpServer.CreateError("GameObject not found", "not_found_error");

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return McpServer.CreateError($"GameObject '{go.name}' has no Renderer component", "validation_error");

            int materialIndex = parameters["materialIndex"]?.ToObject<int>() ?? 0;
            var materials = renderer.sharedMaterials;

            if (materialIndex < 0 || materialIndex >= materials.Length)
                return McpServer.CreateError($"materialIndex {materialIndex} out of range (0-{materials.Length - 1})", "validation_error");

            var previousMaterial = materials[materialIndex];
            string previousName = previousMaterial != null ? previousMaterial.name : "None";

            materials[materialIndex] = material;
            renderer.sharedMaterials = materials;

            // Mark scene dirty so changes can be saved
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Applied material '{material.name}' to '{go.name}' (index {materialIndex})",
                ["gameObjectName"] = go.name,
                ["previousMaterial"] = previousName,
                ["newMaterial"] = material.name
            };
        }
    }
}
