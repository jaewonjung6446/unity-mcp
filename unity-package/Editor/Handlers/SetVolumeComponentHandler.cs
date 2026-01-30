using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace McpUnity.Handlers
{
    public class SetVolumeComponentHandler : IToolHandler
    {
        public string Name => "set_volume_component";

        public JObject Execute(JObject parameters)
        {
            var instanceId = parameters["instanceId"]?.ToObject<int?>();
            var gameObjectPath = parameters["gameObjectPath"]?.ToString();
            var componentType = parameters["componentType"]?.ToString();
            var properties = parameters["properties"] as JObject;
            var addIfMissing = parameters["addIfMissing"]?.ToObject<bool>() ?? true;

            if (string.IsNullOrEmpty(componentType))
                return McpServer.CreateError("Missing required parameter: componentType", "validation_error");
            if (properties == null || properties.Count == 0)
                return McpServer.CreateError("Missing required parameter: properties", "validation_error");

            // Find the Volume GameObject
            GameObject go = null;
            if (instanceId.HasValue)
            {
                go = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                if (go == null)
                    return McpServer.CreateError($"GameObject not found with instanceId: {instanceId.Value}", "not_found_error");
            }
            else if (!string.IsNullOrEmpty(gameObjectPath))
            {
                go = GameObject.Find(gameObjectPath);
                if (go == null)
                    return McpServer.CreateError($"GameObject not found at path: {gameObjectPath}", "not_found_error");
            }
            else
            {
                return McpServer.CreateError("Must provide instanceId or gameObjectPath", "validation_error");
            }

            var volume = go.GetComponent<Volume>();
            if (volume == null)
                return McpServer.CreateError($"No Volume component on GameObject: {go.name}", "not_found_error");

            // Ensure we have a profile (use sharedProfile to avoid runtime instantiation)
            var profile = volume.sharedProfile;
            if (profile == null)
                return McpServer.CreateError("Volume has no profile assigned", "validation_error");

            // Find the VolumeComponent type by name
            Type volCompType = FindVolumeComponentType(componentType);
            if (volCompType == null)
                return McpServer.CreateError($"Unknown volume component type: {componentType}", "validation_error");

            // Try to get the component from the profile
            VolumeComponent comp = null;
            foreach (var c in profile.components)
            {
                if (c.GetType() == volCompType)
                {
                    comp = c;
                    break;
                }
            }

            if (comp == null)
            {
                if (!addIfMissing)
                    return McpServer.CreateError($"Component {componentType} not found in profile and addIfMissing is false", "not_found_error");

                comp = (VolumeComponent)ScriptableObject.CreateInstance(volCompType);
                comp.active = true;
                profile.components.Add(comp);
            }

            // Set properties via reflection
            var changesArray = new JArray();
            foreach (var prop in properties)
            {
                var fieldName = prop.Key;
                var newValue = prop.Value;

                var field = volCompType.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                {
                    changesArray.Add(new JObject
                    {
                        ["property"] = fieldName,
                        ["error"] = $"Field '{fieldName}' not found on {componentType}"
                    });
                    continue;
                }

                if (!typeof(VolumeParameter).IsAssignableFrom(field.FieldType))
                {
                    changesArray.Add(new JObject
                    {
                        ["property"] = fieldName,
                        ["error"] = $"Field '{fieldName}' is not a VolumeParameter"
                    });
                    continue;
                }

                var param = field.GetValue(comp) as VolumeParameter;
                if (param == null)
                {
                    changesArray.Add(new JObject
                    {
                        ["property"] = fieldName,
                        ["error"] = $"VolumeParameter '{fieldName}' is null"
                    });
                    continue;
                }

                // Get the value property via reflection
                var valueProp = param.GetType().GetProperty("value",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (valueProp == null)
                {
                    changesArray.Add(new JObject
                    {
                        ["property"] = fieldName,
                        ["error"] = "Cannot access value property"
                    });
                    continue;
                }

                var oldValue = valueProp.GetValue(param);
                var oldValueToken = SerializeValue(oldValue);

                try
                {
                    var convertedValue = ConvertValue(newValue, valueProp.PropertyType);
                    valueProp.SetValue(param, convertedValue);
                    param.overrideState = true;

                    changesArray.Add(new JObject
                    {
                        ["property"] = fieldName,
                        ["oldValue"] = oldValueToken,
                        ["newValue"] = SerializeValue(convertedValue),
                        ["success"] = true
                    });
                }
                catch (Exception ex)
                {
                    changesArray.Add(new JObject
                    {
                        ["property"] = fieldName,
                        ["error"] = $"Failed to set value: {ex.Message}"
                    });
                }
            }

            // Save
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Updated {componentType} on Volume '{go.name}'",
                ["componentType"] = componentType,
                ["changes"] = changesArray
            };
        }

        private static Type FindVolumeComponentType(string typeName)
        {
            // Search all assemblies for the VolumeComponent subclass
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Check common URP namespaces first
                var type = assembly.GetType($"UnityEngine.Rendering.Universal.{typeName}");
                if (type != null && typeof(VolumeComponent).IsAssignableFrom(type))
                    return type;

                type = assembly.GetType($"UnityEngine.Rendering.{typeName}");
                if (type != null && typeof(VolumeComponent).IsAssignableFrom(type))
                    return type;
            }

            // Fallback: search all types
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == typeName && typeof(VolumeComponent).IsAssignableFrom(type))
                        return type;
                }
            }

            return null;
        }

        private static object ConvertValue(JToken token, Type targetType)
        {
            if (targetType == typeof(float))
                return token.ToObject<float>();
            if (targetType == typeof(int))
                return token.ToObject<int>();
            if (targetType == typeof(bool))
                return token.ToObject<bool>();
            if (targetType == typeof(Color))
            {
                var obj = token as JObject;
                if (obj != null)
                    return new Color(
                        obj["r"]?.ToObject<float>() ?? 0,
                        obj["g"]?.ToObject<float>() ?? 0,
                        obj["b"]?.ToObject<float>() ?? 0,
                        obj["a"]?.ToObject<float>() ?? 1);
                return token.ToObject<Color>();
            }
            if (targetType == typeof(Vector2))
            {
                var obj = token as JObject;
                if (obj != null)
                    return new Vector2(
                        obj["x"]?.ToObject<float>() ?? 0,
                        obj["y"]?.ToObject<float>() ?? 0);
                return token.ToObject<Vector2>();
            }
            if (targetType == typeof(Vector3))
            {
                var obj = token as JObject;
                if (obj != null)
                    return new Vector3(
                        obj["x"]?.ToObject<float>() ?? 0,
                        obj["y"]?.ToObject<float>() ?? 0,
                        obj["z"]?.ToObject<float>() ?? 0);
                return token.ToObject<Vector3>();
            }
            if (targetType == typeof(Vector4))
            {
                var obj = token as JObject;
                if (obj != null)
                    return new Vector4(
                        obj["x"]?.ToObject<float>() ?? 0,
                        obj["y"]?.ToObject<float>() ?? 0,
                        obj["z"]?.ToObject<float>() ?? 0,
                        obj["w"]?.ToObject<float>() ?? 0);
                return token.ToObject<Vector4>();
            }
            if (targetType.IsEnum)
                return Enum.Parse(targetType, token.ToString(), true);

            // Fallback
            return token.ToObject(targetType);
        }

        private static JToken SerializeValue(object val)
        {
            if (val == null) return JValue.CreateNull();
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
            if (val is Enum e)
                return e.ToString();
            return val.ToString();
        }
    }
}
