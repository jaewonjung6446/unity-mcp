using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace McpUnity.Handlers
{
    public class GetScenesListHandler : IToolHandler
    {
        public string Name => "get_scenes_list";

        public JObject Execute(JObject parameters)
        {
            // Build settings scenes
            var buildScenes = new JArray();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                buildScenes.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled,
                    ["guid"] = scene.guid.ToString()
                });
            }

            // Currently loaded scenes
            var loadedScenes = new JArray();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                loadedScenes.Add(new JObject
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded,
                    ["isDirty"] = scene.isDirty,
                    ["rootCount"] = scene.rootCount,
                    ["buildIndex"] = scene.buildIndex
                });
            }

            // All scene files in project
            var allSceneGuids = AssetDatabase.FindAssets("t:Scene");
            var allScenes = new JArray();
            foreach (var guid in allSceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                allScenes.Add(path);
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Build scenes: {buildScenes.Count}, Loaded: {loadedScenes.Count}, All: {allScenes.Count}",
                ["buildScenes"] = buildScenes,
                ["loadedScenes"] = loadedScenes,
                ["allScenes"] = allScenes
            };
        }
    }
}
