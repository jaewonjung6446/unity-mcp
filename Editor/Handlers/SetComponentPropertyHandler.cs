using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetComponentPropertyHandler : IToolHandler
    {
        public string Name => "set_component_property";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var componentType = parameters["componentType"]?.ToString();
            if (string.IsNullOrEmpty(componentType))
                return McpServer.CreateError("Missing required parameter: componentType", "validation_error");

            var properties = parameters["properties"] as JObject;
            if (properties == null || properties.Count == 0)
                return McpServer.CreateError("Missing required parameter: properties", "validation_error");

            Component target = null;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                if (string.Equals(c.GetType().Name, componentType, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.GetType().FullName, componentType, StringComparison.OrdinalIgnoreCase))
                {
                    target = c;
                    break;
                }
            }

            if (target == null)
                return McpServer.CreateError($"Component '{componentType}' not found on '{go.name}'", "not_found_error");

            Undo.RecordObject(target, $"Set properties on {target.GetType().Name}");

            var changed = new JArray();
            var errors = new JArray();

            // Try SerializedObject first for reliable field access
            var so = new SerializedObject(target);

            foreach (var prop in properties)
            {
                var fieldName = prop.Key;
                var value = prop.Value;

                // Try SerializedProperty first
                var sp = so.FindProperty(fieldName);
                if (sp != null)
                {
                    if (TrySetSerializedProperty(sp, value))
                    {
                        changed.Add(fieldName);
                        continue;
                    }
                }

                // Fallback to reflection
                if (TrySetViaReflection(target, fieldName, value))
                {
                    changed.Add(fieldName);
                }
                else
                {
                    errors.Add($"Failed to set '{fieldName}'");
                }
            }

            so.ApplyModifiedProperties();
            so.Dispose();

            EditorUtility.SetDirty(target);

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Set {changed.Count} properties on {target.GetType().Name}",
                ["changed"] = changed
            };
            if (errors.Count > 0)
                result["errors"] = errors;

            return result;
        }

        private static bool TrySetSerializedProperty(SerializedProperty sp, JToken value)
        {
            try
            {
                switch (sp.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        sp.intValue = value.ToObject<int>();
                        return true;
                    case SerializedPropertyType.Boolean:
                        sp.boolValue = value.ToObject<bool>();
                        return true;
                    case SerializedPropertyType.Float:
                        sp.floatValue = value.ToObject<float>();
                        return true;
                    case SerializedPropertyType.String:
                        sp.stringValue = value.ToString();
                        return true;
                    case SerializedPropertyType.Color:
                        var c = value as JObject;
                        if (c != null)
                        {
                            sp.colorValue = new Color(
                                c["r"]?.ToObject<float>() ?? 0f,
                                c["g"]?.ToObject<float>() ?? 0f,
                                c["b"]?.ToObject<float>() ?? 0f,
                                c["a"]?.ToObject<float>() ?? 1f
                            );
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector2:
                        var v2 = value as JObject;
                        if (v2 != null)
                        {
                            sp.vector2Value = new Vector2(
                                v2["x"]?.ToObject<float>() ?? 0f,
                                v2["y"]?.ToObject<float>() ?? 0f
                            );
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector3:
                        var v3 = value as JObject;
                        if (v3 != null)
                        {
                            sp.vector3Value = new Vector3(
                                v3["x"]?.ToObject<float>() ?? 0f,
                                v3["y"]?.ToObject<float>() ?? 0f,
                                v3["z"]?.ToObject<float>() ?? 0f
                            );
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Vector4:
                        var v4 = value as JObject;
                        if (v4 != null)
                        {
                            sp.vector4Value = new Vector4(
                                v4["x"]?.ToObject<float>() ?? 0f,
                                v4["y"]?.ToObject<float>() ?? 0f,
                                v4["z"]?.ToObject<float>() ?? 0f,
                                v4["w"]?.ToObject<float>() ?? 0f
                            );
                            return true;
                        }
                        return false;
                    case SerializedPropertyType.Enum:
                        if (value.Type == JTokenType.Integer)
                            sp.enumValueIndex = value.ToObject<int>();
                        else
                        {
                            var enumStr = value.ToString();
                            for (int i = 0; i < sp.enumNames.Length; i++)
                            {
                                if (string.Equals(sp.enumNames[i], enumStr, StringComparison.OrdinalIgnoreCase))
                                {
                                    sp.enumValueIndex = i;
                                    break;
                                }
                            }
                        }
                        return true;
                    case SerializedPropertyType.ObjectReference:
                        var assetPath = value.ToString();
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                            if (obj != null)
                            {
                                sp.objectReferenceValue = obj;
                                return true;
                            }
                        }
                        return false;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySetViaReflection(Component target, string fieldName, JToken value)
        {
            try
            {
                var type = target.GetType();
                var flags = BindingFlags.Public | BindingFlags.Instance;

                var field = type.GetField(fieldName, flags);
                if (field != null)
                {
                    field.SetValue(target, ConvertValue(value, field.FieldType));
                    return true;
                }

                var prop = type.GetProperty(fieldName, flags);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(target, ConvertValue(value, prop.PropertyType));
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static object ConvertValue(JToken value, Type targetType)
        {
            if (targetType == typeof(float)) return value.ToObject<float>();
            if (targetType == typeof(int)) return value.ToObject<int>();
            if (targetType == typeof(bool)) return value.ToObject<bool>();
            if (targetType == typeof(string)) return value.ToString();
            if (targetType == typeof(double)) return value.ToObject<double>();
            if (targetType == typeof(Vector2))
            {
                var obj = value as JObject;
                return new Vector2(obj["x"]?.ToObject<float>() ?? 0, obj["y"]?.ToObject<float>() ?? 0);
            }
            if (targetType == typeof(Vector3))
            {
                var obj = value as JObject;
                return new Vector3(obj["x"]?.ToObject<float>() ?? 0, obj["y"]?.ToObject<float>() ?? 0, obj["z"]?.ToObject<float>() ?? 0);
            }
            if (targetType == typeof(Color))
            {
                var obj = value as JObject;
                return new Color(obj["r"]?.ToObject<float>() ?? 0, obj["g"]?.ToObject<float>() ?? 0, obj["b"]?.ToObject<float>() ?? 0, obj["a"]?.ToObject<float>() ?? 1);
            }
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value.ToString(), true);
            return value.ToObject(targetType);
        }
    }
}
