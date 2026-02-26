using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetRenderSettingsHandler : IToolHandler
    {
        public string Name => "set_render_settings";

        public JObject Execute(JObject parameters)
        {
            var changed = new JArray();

            // Fog
            var fog = parameters["fog"];
            if (fog != null) { RenderSettings.fog = fog.ToObject<bool>(); changed.Add("fog"); }

            var fogColor = parameters["fogColor"] as JObject;
            if (fogColor != null) { RenderSettings.fogColor = JsonToColor(fogColor); changed.Add("fogColor"); }

            var fogMode = parameters["fogMode"]?.ToString();
            if (!string.IsNullOrEmpty(fogMode) && System.Enum.TryParse<FogMode>(fogMode, true, out var fm))
            { RenderSettings.fogMode = fm; changed.Add("fogMode"); }

            var fogDensity = parameters["fogDensity"];
            if (fogDensity != null) { RenderSettings.fogDensity = fogDensity.ToObject<float>(); changed.Add("fogDensity"); }

            var fogStart = parameters["fogStartDistance"];
            if (fogStart != null) { RenderSettings.fogStartDistance = fogStart.ToObject<float>(); changed.Add("fogStartDistance"); }

            var fogEnd = parameters["fogEndDistance"];
            if (fogEnd != null) { RenderSettings.fogEndDistance = fogEnd.ToObject<float>(); changed.Add("fogEndDistance"); }

            // Ambient
            var ambientMode = parameters["ambientMode"]?.ToString();
            if (!string.IsNullOrEmpty(ambientMode) && System.Enum.TryParse<UnityEngine.Rendering.AmbientMode>(ambientMode, true, out var am))
            { RenderSettings.ambientMode = am; changed.Add("ambientMode"); }

            var ambientSkyColor = parameters["ambientSkyColor"] as JObject;
            if (ambientSkyColor != null) { RenderSettings.ambientSkyColor = JsonToColor(ambientSkyColor); changed.Add("ambientSkyColor"); }

            var ambientEquatorColor = parameters["ambientEquatorColor"] as JObject;
            if (ambientEquatorColor != null) { RenderSettings.ambientEquatorColor = JsonToColor(ambientEquatorColor); changed.Add("ambientEquatorColor"); }

            var ambientGroundColor = parameters["ambientGroundColor"] as JObject;
            if (ambientGroundColor != null) { RenderSettings.ambientGroundColor = JsonToColor(ambientGroundColor); changed.Add("ambientGroundColor"); }

            var ambientIntensity = parameters["ambientIntensity"];
            if (ambientIntensity != null) { RenderSettings.ambientIntensity = ambientIntensity.ToObject<float>(); changed.Add("ambientIntensity"); }

            // Reflection
            var reflectionIntensity = parameters["reflectionIntensity"];
            if (reflectionIntensity != null) { RenderSettings.reflectionIntensity = reflectionIntensity.ToObject<float>(); changed.Add("reflectionIntensity"); }

            var reflectionBounces = parameters["reflectionBounces"];
            if (reflectionBounces != null) { RenderSettings.reflectionBounces = reflectionBounces.ToObject<int>(); changed.Add("reflectionBounces"); }

            // Halo / Flare
            var haloStrength = parameters["haloStrength"];
            if (haloStrength != null) { RenderSettings.haloStrength = haloStrength.ToObject<float>(); changed.Add("haloStrength"); }

            var flareStrength = parameters["flareStrength"];
            if (flareStrength != null) { RenderSettings.flareStrength = flareStrength.ToObject<float>(); changed.Add("flareStrength"); }

            // Skybox material
            var skyboxPath = parameters["skyboxMaterialPath"]?.ToString();
            if (!string.IsNullOrEmpty(skyboxPath))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                if (mat != null) { RenderSettings.skybox = mat; changed.Add("skybox"); }
            }

            EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Updated {changed.Count} render settings",
                ["changed"] = changed
            };
        }

        private static Color JsonToColor(JObject c) =>
            new Color(c["r"]?.ToObject<float>() ?? 0, c["g"]?.ToObject<float>() ?? 0, c["b"]?.ToObject<float>() ?? 0, c["a"]?.ToObject<float>() ?? 1);
    }
}
