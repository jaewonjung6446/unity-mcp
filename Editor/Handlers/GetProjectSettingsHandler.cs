using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace McpUnity.Handlers
{
    public class GetProjectSettingsHandler : IToolHandler
    {
        public string Name => "get_project_settings";

        public JObject Execute(JObject parameters)
        {
            var category = parameters["category"]?.ToString()?.ToLower();

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text"
            };

            if (string.IsNullOrEmpty(category) || category == "all")
            {
                result["player"] = GetPlayerSettings();
                result["quality"] = GetQualitySettings();
                result["physics"] = GetPhysicsSettings();
                result["time"] = GetTimeSettings();
                result["tags_and_layers"] = GetTagsAndLayers();
                result["message"] = "Retrieved all project settings";
            }
            else
            {
                switch (category)
                {
                    case "player":
                        result["player"] = GetPlayerSettings();
                        result["message"] = "Retrieved player settings";
                        break;
                    case "quality":
                        result["quality"] = GetQualitySettings();
                        result["message"] = "Retrieved quality settings";
                        break;
                    case "physics":
                        result["physics"] = GetPhysicsSettings();
                        result["message"] = "Retrieved physics settings";
                        break;
                    case "time":
                        result["time"] = GetTimeSettings();
                        result["message"] = "Retrieved time settings";
                        break;
                    case "tags_and_layers":
                        result["tags_and_layers"] = GetTagsAndLayers();
                        result["message"] = "Retrieved tags and layers";
                        break;
                    default:
                        return McpServer.CreateError($"Unknown category: {category}. Valid: player, quality, physics, time, tags_and_layers, all", "validation_error");
                }
            }

            return result;
        }

        private static JObject GetPlayerSettings()
        {
            return new JObject
            {
                ["companyName"] = PlayerSettings.companyName,
                ["productName"] = PlayerSettings.productName,
                ["bundleVersion"] = PlayerSettings.bundleVersion,
                ["defaultScreenWidth"] = PlayerSettings.defaultScreenWidth,
                ["defaultScreenHeight"] = PlayerSettings.defaultScreenHeight,
                ["fullscreenMode"] = PlayerSettings.fullScreenMode.ToString(),
                ["runInBackground"] = PlayerSettings.runInBackground,
                ["colorSpace"] = PlayerSettings.colorSpace.ToString(),
                ["apiCompatibilityLevel"] = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup).ToString(),
                ["scriptingBackend"] = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString(),
                ["activeBuildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["activeScriptCompilationDefines"] = new JArray(PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').Where(s => !string.IsNullOrEmpty(s)).ToArray())
            };
        }

        private static JObject GetQualitySettings()
        {
            var names = QualitySettings.names;
            return new JObject
            {
                ["currentLevel"] = QualitySettings.GetQualityLevel(),
                ["currentName"] = names.Length > QualitySettings.GetQualityLevel() ? names[QualitySettings.GetQualityLevel()] : "Unknown",
                ["levels"] = new JArray(names),
                ["vSyncCount"] = QualitySettings.vSyncCount,
                ["antiAliasing"] = QualitySettings.antiAliasing,
                ["shadowResolution"] = QualitySettings.shadowResolution.ToString(),
                ["shadowDistance"] = QualitySettings.shadowDistance,
                ["renderPipeline"] = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in"
            };
        }

        private static JObject GetPhysicsSettings()
        {
            return new JObject
            {
                ["gravity"] = new JObject
                {
                    ["x"] = Physics.gravity.x,
                    ["y"] = Physics.gravity.y,
                    ["z"] = Physics.gravity.z
                },
                ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                ["defaultSolverVelocityIterations"] = Physics.defaultSolverVelocityIterations,
                ["bounceThreshold"] = Physics.bounceThreshold,
                ["defaultContactOffset"] = Physics.defaultContactOffset,
                ["autoSimulation"] = Physics.simulationMode.ToString()
            };
        }

        private static JObject GetTimeSettings()
        {
            return new JObject
            {
                ["fixedDeltaTime"] = Time.fixedDeltaTime,
                ["maximumDeltaTime"] = Time.maximumDeltaTime,
                ["timeScale"] = Time.timeScale,
                ["maximumParticleDeltaTime"] = Time.maximumParticleDeltaTime
            };
        }

        private static JObject GetTagsAndLayers()
        {
            var tags = new JArray();
            foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
                tags.Add(tag);

            var layers = new JObject();
            for (int i = 0; i < 32; i++)
            {
                var name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                    layers[i.ToString()] = name;
            }

            var sortingLayers = new JArray();
            foreach (var sl in SortingLayer.layers)
                sortingLayers.Add(new JObject { ["name"] = sl.name, ["id"] = sl.id, ["value"] = sl.value });

            return new JObject
            {
                ["tags"] = tags,
                ["layers"] = layers,
                ["sortingLayers"] = sortingLayers
            };
        }
    }
}
