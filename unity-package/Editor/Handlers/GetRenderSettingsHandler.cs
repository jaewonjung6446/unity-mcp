using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace McpUnity.Handlers
{
    public class GetRenderSettingsHandler : IToolHandler
    {
        public string Name => "get_render_settings";

        public JObject Execute(JObject parameters)
        {
            var settings = new JObject
            {
                ["ambientMode"] = RenderSettings.ambientMode.ToString(),
                ["ambientSkyColor"] = ColorToJson(RenderSettings.ambientSkyColor),
                ["ambientEquatorColor"] = ColorToJson(RenderSettings.ambientEquatorColor),
                ["ambientGroundColor"] = ColorToJson(RenderSettings.ambientGroundColor),
                ["ambientIntensity"] = RenderSettings.ambientIntensity,
                ["ambientLight"] = ColorToJson(RenderSettings.ambientLight),
                ["fog"] = RenderSettings.fog,
                ["fogColor"] = ColorToJson(RenderSettings.fogColor),
                ["fogMode"] = RenderSettings.fogMode.ToString(),
                ["fogDensity"] = RenderSettings.fogDensity,
                ["fogStartDistance"] = RenderSettings.fogStartDistance,
                ["fogEndDistance"] = RenderSettings.fogEndDistance,
                ["skybox"] = RenderSettings.skybox != null ? RenderSettings.skybox.name : null,
                ["sun"] = RenderSettings.sun != null ? RenderSettings.sun.gameObject.name : null,
                ["subtractiveShadowColor"] = ColorToJson(RenderSettings.subtractiveShadowColor),
                ["reflectionIntensity"] = RenderSettings.reflectionIntensity,
                ["reflectionBounces"] = RenderSettings.reflectionBounces,
                ["defaultReflectionResolution"] = RenderSettings.defaultReflectionResolution,
                ["defaultReflectionMode"] = RenderSettings.defaultReflectionMode.ToString(),
                ["haloStrength"] = RenderSettings.haloStrength,
                ["flareStrength"] = RenderSettings.flareStrength,
                ["flareFadeSpeed"] = RenderSettings.flareFadeSpeed
            };

            // Render pipeline info
            settings["renderPipeline"] = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.name : "Built-in";

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Render settings retrieved",
                ["settings"] = settings
            };
        }

        private static JObject ColorToJson(Color c) =>
            new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
    }
}
