using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetMaterialPropertyHandler : IToolHandler
    {
        public string Name => "set_material_property";

        public JObject Execute(JObject parameters)
        {
            var assetPath = parameters["assetPath"]?.ToString();
            var propertyName = parameters["propertyName"]?.ToString();
            var propertyType = parameters["propertyType"]?.ToString();
            var value = parameters["value"];

            if (string.IsNullOrEmpty(assetPath))
                return McpServer.CreateError("Missing required parameter: assetPath", "validation_error");
            if (string.IsNullOrEmpty(propertyName))
                return McpServer.CreateError("Missing required parameter: propertyName", "validation_error");
            if (string.IsNullOrEmpty(propertyType))
                return McpServer.CreateError("Missing required parameter: propertyType", "validation_error");
            if (value == null)
                return McpServer.CreateError("Missing required parameter: value", "validation_error");

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
                return McpServer.CreateError($"Material not found: {assetPath}", "not_found_error");

            if (!material.HasProperty(propertyName))
                return McpServer.CreateError($"Material does not have property: {propertyName}", "validation_error");

            switch (propertyType.ToLower())
            {
                case "color":
                    var c = value as JObject;
                    if (c != null)
                    {
                        material.SetColor(propertyName, new Color(
                            c["r"]?.ToObject<float>() ?? 0,
                            c["g"]?.ToObject<float>() ?? 0,
                            c["b"]?.ToObject<float>() ?? 0,
                            c["a"]?.ToObject<float>() ?? 1));
                    }
                    else
                    {
                        return McpServer.CreateError("Color value must be an object with r, g, b, a fields", "validation_error");
                    }
                    break;

                case "float":
                    material.SetFloat(propertyName, value.ToObject<float>());
                    break;

                case "int":
                    material.SetInt(propertyName, value.ToObject<int>());
                    break;

                case "vector":
                    var v = value as JObject;
                    if (v != null)
                    {
                        material.SetVector(propertyName, new Vector4(
                            v["x"]?.ToObject<float>() ?? 0,
                            v["y"]?.ToObject<float>() ?? 0,
                            v["z"]?.ToObject<float>() ?? 0,
                            v["w"]?.ToObject<float>() ?? 0));
                    }
                    else
                    {
                        return McpServer.CreateError("Vector value must be an object with x, y, z, w fields", "validation_error");
                    }
                    break;

                case "texture":
                    var texPath = value.ToString();
                    var texture = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                    if (texture == null)
                        return McpServer.CreateError($"Texture not found: {texPath}", "not_found_error");
                    material.SetTexture(propertyName, texture);
                    break;

                default:
                    return McpServer.CreateError($"Unknown property type: {propertyType}. Supported: color, float, int, vector, texture", "validation_error");
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Set {propertyType} property '{propertyName}' on material '{assetPath}'"
            };
        }
    }
}
