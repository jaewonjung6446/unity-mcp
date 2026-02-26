using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace McpUnity.Handlers
{
    public class GetVolumeSettingsHandler : IToolHandler
    {
        public string Name => "get_volume_settings";

        public JObject Execute(JObject parameters)
        {
            var instanceId = parameters["instanceId"]?.ToObject<int?>();
            var gameObjectPath = parameters["gameObjectPath"]?.ToString();

            var volumes = new List<Volume>();

            if (instanceId.HasValue)
            {
                var obj = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                if (obj == null)
                    return McpServer.CreateError($"GameObject not found with instanceId: {instanceId.Value}", "not_found_error");
                var vol = obj.GetComponent<Volume>();
                if (vol == null)
                    return McpServer.CreateError($"No Volume component on GameObject: {obj.name}", "not_found_error");
                volumes.Add(vol);
            }
            else if (!string.IsNullOrEmpty(gameObjectPath))
            {
                var obj = GameObject.Find(gameObjectPath);
                if (obj == null)
                    return McpServer.CreateError($"GameObject not found at path: {gameObjectPath}", "not_found_error");
                var vol = obj.GetComponent<Volume>();
                if (vol == null)
                    return McpServer.CreateError($"No Volume component on GameObject: {obj.name}", "not_found_error");
                volumes.Add(vol);
            }
            else
            {
                // Scan all volumes in active scene
                var scene = EditorSceneManager.GetActiveScene();
                if (!scene.IsValid())
                    return McpServer.CreateError("No active scene", "scene_error");

                foreach (var root in scene.GetRootGameObjects())
                {
                    volumes.AddRange(root.GetComponentsInChildren<Volume>(true));
                }
            }

            var volumesArray = new JArray();
            foreach (var vol in volumes)
            {
                var profile = vol.sharedProfile;
                var componentsArray = new JArray();

                if (profile != null)
                {
                    foreach (var comp in profile.components)
                    {
                        var propsObj = new JObject();
                        var fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                        foreach (var field in fields)
                        {
                            if (!typeof(VolumeParameter).IsAssignableFrom(field.FieldType))
                                continue;

                            var param = field.GetValue(comp) as VolumeParameter;
                            if (param == null)
                                continue;

                            var paramValue = GetParameterValue(param);
                            propsObj[field.Name] = new JObject
                            {
                                ["value"] = paramValue,
                                ["overrideState"] = param.overrideState
                            };
                        }

                        componentsArray.Add(new JObject
                        {
                            ["type"] = comp.GetType().Name,
                            ["active"] = comp.active,
                            ["properties"] = propsObj
                        });
                    }
                }

                var profilePath = profile != null ? AssetDatabase.GetAssetPath(profile) : null;

                volumesArray.Add(new JObject
                {
                    ["gameObjectName"] = vol.gameObject.name,
                    ["gameObjectPath"] = GetGameObjectPath(vol.gameObject),
                    ["instanceId"] = vol.gameObject.GetInstanceID(),
                    ["isGlobal"] = vol.isGlobal,
                    ["priority"] = vol.priority,
                    ["profilePath"] = profilePath,
                    ["components"] = componentsArray
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {volumes.Count} Volume(s)",
                ["volumes"] = volumesArray
            };
        }

        private static JToken GetParameterValue(VolumeParameter param)
        {
            // Use reflection to get the generic value
            var valueProp = param.GetType().GetProperty("value",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (valueProp == null)
                return null;

            var val = valueProp.GetValue(param);
            if (val == null)
                return JValue.CreateNull();

            if (val is float f) return f;
            if (val is int i) return i;
            if (val is bool b) return b;
            if (val is string s) return s;
            if (val is Color c)
                return new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
            if (val is Vector2 v2)
                return new JObject { ["x"] = v2.x, ["y"] = v2.y };
            if (val is Vector3 v3)
                return new JObject { ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z };
            if (val is Vector4 v4)
                return new JObject { ["x"] = v4.x, ["y"] = v4.y, ["z"] = v4.z, ["w"] = v4.w };
            if (val is System.Enum e)
                return e.ToString();
            if (val is Texture tex)
                return AssetDatabase.GetAssetPath(tex) ?? tex.name;
            if (val is AnimationCurve)
                return "[AnimationCurve]";
            if (val is TextureCurve)
                return "[TextureCurve]";

            return val.ToString();
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
