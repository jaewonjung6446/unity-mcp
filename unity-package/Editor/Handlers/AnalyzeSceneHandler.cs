using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class AnalyzeSceneHandler : IToolHandler
    {
        public string Name => "analyze_scene";

        public JObject Execute(JObject parameters)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
                return McpServer.CreateError("No active scene", "scene_error");

            var rootObjects = scene.GetRootGameObjects();
            var allRenderers = new List<Renderer>();
            var allLights = new List<Light>();

            foreach (var root in rootObjects)
            {
                allRenderers.AddRange(root.GetComponentsInChildren<Renderer>(true));
                allLights.AddRange(root.GetComponentsInChildren<Light>(true));
            }

            var renderersArray = new JArray();
            var uniqueShaders = new HashSet<string>();
            var allMaterials = new HashSet<string>();
            int missingMaterials = 0;

            foreach (var renderer in allRenderers)
            {
                var materialsArray = new JArray();
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                    {
                        missingMaterials++;
                        materialsArray.Add(new JObject
                        {
                            ["name"] = null,
                            ["shaderName"] = null,
                            ["assetPath"] = null
                        });
                        continue;
                    }

                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(mat);
                    var shaderName = mat.shader != null ? mat.shader.name : "None";
                    uniqueShaders.Add(shaderName);
                    allMaterials.Add(mat.name);

                    materialsArray.Add(new JObject
                    {
                        ["name"] = mat.name,
                        ["shaderName"] = shaderName,
                        ["assetPath"] = string.IsNullOrEmpty(assetPath) ? null : assetPath
                    });
                }

                renderersArray.Add(new JObject
                {
                    ["gameObjectName"] = renderer.gameObject.name,
                    ["gameObjectPath"] = GetGameObjectPath(renderer.gameObject),
                    ["instanceId"] = renderer.gameObject.GetInstanceID(),
                    ["rendererType"] = renderer.GetType().Name,
                    ["materials"] = materialsArray
                });
            }

            var lightsArray = new JArray();
            foreach (var light in allLights)
            {
                lightsArray.Add(new JObject
                {
                    ["gameObjectName"] = light.gameObject.name,
                    ["type"] = light.type.ToString(),
                    ["color"] = new JObject
                    {
                        ["r"] = light.color.r,
                        ["g"] = light.color.g,
                        ["b"] = light.color.b
                    },
                    ["intensity"] = light.intensity
                });
            }

            var summary = new JObject
            {
                ["totalRenderers"] = allRenderers.Count,
                ["totalMaterials"] = allMaterials.Count,
                ["uniqueShaders"] = new JArray(uniqueShaders.ToArray()),
                ["missingMaterials"] = missingMaterials,
                ["totalLights"] = allLights.Count
            };

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["sceneName"] = scene.name,
                ["scenePath"] = scene.path,
                ["renderers"] = renderersArray,
                ["lights"] = lightsArray,
                ["summary"] = summary
            };
        }

        private static string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
