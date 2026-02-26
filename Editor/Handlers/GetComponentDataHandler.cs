
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetComponentDataHandler : IToolHandler
    {
        public string Name => "get_component_data";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var componentType = parameters["componentType"]?.ToString();
            if (string.IsNullOrEmpty(componentType))
                return McpServer.CreateError("Missing required parameter: componentType", "validation_error");

            // Find the component by type name
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

            var data = SerializeComponent(target);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Component data for {target.GetType().Name} on '{go.name}'",
                ["gameObject"] = GetGameObjectHandler.GetPath(go),
                ["componentType"] = target.GetType().FullName,
                ["instanceId"] = target.GetInstanceID(),
                ["data"] = data
            };
        }

        private static JObject SerializeComponent(Component component)
        {
            var result = new JObject();
            var type = component.GetType();

            // Use SerializedObject for reliable field reading
            try
            {
                var so = new SerializedObject(component);
                var prop = so.GetIterator();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        try
                        {
                            result[prop.name] = SerializeProperty(prop);
                        }
                        catch
                        {
                            result[prop.name] = $"<error reading {prop.propertyType}>";
                        }
                    }
                    while (prop.NextVisible(false));
                }
                so.Dispose();
            }
            catch
            {
                // Fallback to reflection for runtime-only components
                SerializeViaReflection(component, type, result);
            }

            return result;
        }

        private static JToken SerializeProperty(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.Enum:
                    return prop.enumNames.Length > prop.enumValueIndex && prop.enumValueIndex >= 0
                        ? prop.enumNames[prop.enumValueIndex]
                        : prop.enumValueIndex.ToString();
                case SerializedPropertyType.Vector2:
                    var v2 = prop.vector2Value;
                    return new JObject { ["x"] = v2.x, ["y"] = v2.y };
                case SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    return new JObject { ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z };
                case SerializedPropertyType.Vector4:
                    var v4 = prop.vector4Value;
                    return new JObject { ["x"] = v4.x, ["y"] = v4.y, ["z"] = v4.z, ["w"] = v4.w };
                case SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    return new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
                case SerializedPropertyType.Rect:
                    var r = prop.rectValue;
                    return new JObject { ["x"] = r.x, ["y"] = r.y, ["width"] = r.width, ["height"] = r.height };
                case SerializedPropertyType.Bounds:
                    var b = prop.boundsValue;
                    return new JObject
                    {
                        ["center"] = new JObject { ["x"] = b.center.x, ["y"] = b.center.y, ["z"] = b.center.z },
                        ["size"] = new JObject { ["x"] = b.size.x, ["y"] = b.size.y, ["z"] = b.size.z }
                    };
                case SerializedPropertyType.ObjectReference:
                    if (prop.objectReferenceValue != null)
                        return new JObject
                        {
                            ["name"] = prop.objectReferenceValue.name,
                            ["instanceId"] = prop.objectReferenceValue.GetInstanceID(),
                            ["type"] = prop.objectReferenceValue.GetType().Name
                        };
                    return JValue.CreateNull();
                case SerializedPropertyType.LayerMask:
                    return prop.intValue;
                case SerializedPropertyType.ArraySize:
                    return prop.intValue;
                default:
                    return $"<{prop.propertyType}>";
            }
        }

        private static void SerializeViaReflection(Component component, Type type, JObject result)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;
            foreach (var field in type.GetFields(flags))
            {
                try
                {
                    var value = field.GetValue(component);
                    result[field.Name] = SerializeValue(value);
                }
                catch
                {
                    result[field.Name] = "<error>";
                }
            }

            foreach (var prop in type.GetProperties(flags))
            {
                if (prop.GetIndexParameters().Length > 0) continue;
                if (!prop.CanRead) continue;
                // Skip common heavy/recursive properties
                if (prop.Name == "gameObject" || prop.Name == "transform" || prop.Name == "tag") continue;
                try
                {
                    var value = prop.GetValue(component);
                    result[prop.Name] = SerializeValue(value);
                }
                catch
                {
                    // Skip properties that throw
                }
            }
        }

        private static JToken SerializeValue(object value)
        {
            if (value == null) return JValue.CreateNull();
            if (value is int i) return i;
            if (value is float f) return f;
            if (value is double d) return d;
            if (value is bool b) return b;
            if (value is string s) return s;
            if (value is Vector2 v2) return new JObject { ["x"] = v2.x, ["y"] = v2.y };
            if (value is Vector3 v3) return new JObject { ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z };
            if (value is Vector4 v4) return new JObject { ["x"] = v4.x, ["y"] = v4.y, ["z"] = v4.z, ["w"] = v4.w };
            if (value is Color c) return new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
            if (value is Quaternion q) return new JObject { ["x"] = q.x, ["y"] = q.y, ["z"] = q.z, ["w"] = q.w };
            if (value is UnityEngine.Object obj)
                return new JObject { ["name"] = obj.name, ["instanceId"] = obj.GetInstanceID(), ["type"] = obj.GetType().Name };
            if (value is Enum e) return e.ToString();
            return value.ToString();
        }
    }
}
