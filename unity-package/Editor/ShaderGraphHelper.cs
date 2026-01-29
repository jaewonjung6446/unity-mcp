using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace McpUnity
{
    public static class ShaderGraphHelper
    {
        // --- GUID generation ---
        public static string NewGuid() => Guid.NewGuid().ToString("N");

        // --- Template types ---
        public static readonly Dictionary<string, string> MasterNodeTypes = new Dictionary<string, string>
        {
            ["urp_lit"] = "UnityEditor.ShaderGraph.PBRMasterNode",
            ["urp_unlit"] = "UnityEditor.ShaderGraph.UnlitMasterNode"
        };

        // Default master node slot definitions per template
        public static JArray GetMasterNodeSlots(string templateType)
        {
            if (templateType == "urp_lit")
            {
                return new JArray
                {
                    CreateSlot("Albedo", 0, "Color", true, new JObject { ["r"] = 0.5, ["g"] = 0.5, ["b"] = 0.5, ["a"] = 1 }),
                    CreateSlot("Normal", 1, "Vector3", true, new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 1 }),
                    CreateSlot("Emission", 2, "Color", true, new JObject { ["r"] = 0, ["g"] = 0, ["b"] = 0, ["a"] = 1 }),
                    CreateSlot("Metallic", 3, "Float", true, 0.0),
                    CreateSlot("Smoothness", 4, "Float", true, 0.5),
                    CreateSlot("Occlusion", 5, "Float", true, 1.0),
                    CreateSlot("Alpha", 6, "Float", true, 1.0),
                    CreateSlot("AlphaClipThreshold", 7, "Float", true, 0.5)
                };
            }
            // urp_unlit
            return new JArray
            {
                CreateSlot("Color", 0, "Color", true, new JObject { ["r"] = 0.5, ["g"] = 0.5, ["b"] = 0.5, ["a"] = 1 }),
                CreateSlot("Alpha", 1, "Float", true, 1.0),
                CreateSlot("AlphaClipThreshold", 2, "Float", true, 0.5)
            };
        }

        private static JObject CreateSlot(string name, int id, string valueType, bool isInput, object defaultValue)
        {
            var slot = new JObject
            {
                ["m_Id"] = id,
                ["m_DisplayName"] = name,
                ["m_SlotType"] = isInput ? 0 : 1,
                ["m_Priority"] = 2147483647,
                ["m_Hidden"] = false,
                ["m_ShaderOutputName"] = name,
                ["m_StageCapability"] = 3,
                ["m_ValueType"] = valueType
            };
            if (defaultValue is JObject jVal)
                slot["m_DefaultValue"] = jVal;
            else
                slot["m_DefaultValue"] = JToken.FromObject(defaultValue);
            return slot;
        }

        // --- Node type mappings ---
        public static readonly Dictionary<string, string> NodeTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SampleTexture2D"] = "UnityEditor.ShaderGraph.SampleTexture2DNode",
            ["Color"] = "UnityEditor.ShaderGraph.ColorNode",
            ["Multiply"] = "UnityEditor.ShaderGraph.MultiplyNode",
            ["Add"] = "UnityEditor.ShaderGraph.AddNode",
            ["Subtract"] = "UnityEditor.ShaderGraph.SubtractNode",
            ["Lerp"] = "UnityEditor.ShaderGraph.LerpNode",
            ["UV"] = "UnityEditor.ShaderGraph.UVNode",
            ["Time"] = "UnityEditor.ShaderGraph.TimeNode",
            ["Float"] = "UnityEditor.ShaderGraph.Vector1Node",
            ["Vector2"] = "UnityEditor.ShaderGraph.Vector2Node",
            ["Vector3"] = "UnityEditor.ShaderGraph.Vector3Node",
            ["Vector4"] = "UnityEditor.ShaderGraph.Vector4Node",
            ["Normalize"] = "UnityEditor.ShaderGraph.NormalizeNode",
            ["DotProduct"] = "UnityEditor.ShaderGraph.DotProductNode",
            ["CrossProduct"] = "UnityEditor.ShaderGraph.CrossProductNode",
            ["Split"] = "UnityEditor.ShaderGraph.SplitNode",
            ["Combine"] = "UnityEditor.ShaderGraph.CombineNode",
            ["Fresnel"] = "UnityEditor.ShaderGraph.FresnelEffectNode",
            ["OneMinus"] = "UnityEditor.ShaderGraph.OneMinusNode",
            ["Saturate"] = "UnityEditor.ShaderGraph.SaturateNode",
            ["Clamp"] = "UnityEditor.ShaderGraph.ClampNode",
            ["Step"] = "UnityEditor.ShaderGraph.StepNode",
            ["SmoothStep"] = "UnityEditor.ShaderGraph.SmoothstepNode",
            ["Power"] = "UnityEditor.ShaderGraph.PowerNode",
            ["SquareRoot"] = "UnityEditor.ShaderGraph.SquareRootNode",
            ["Absolute"] = "UnityEditor.ShaderGraph.AbsoluteNode",
            ["Negate"] = "UnityEditor.ShaderGraph.NegateNode",
            ["Sin"] = "UnityEditor.ShaderGraph.SineNode",
            ["Cos"] = "UnityEditor.ShaderGraph.CosineNode",
            ["Tan"] = "UnityEditor.ShaderGraph.TangentNode",
            ["Noise"] = "UnityEditor.ShaderGraph.SimpleNoiseNode",
            ["Voronoi"] = "UnityEditor.ShaderGraph.VoronoiNode",
            ["Gradient"] = "UnityEditor.ShaderGraph.GradientNode",
            ["NormalUnpack"] = "UnityEditor.ShaderGraph.NormalUnpackNode",
            ["Tiling"] = "UnityEditor.ShaderGraph.TilingAndOffsetNode",
            ["Preview"] = "UnityEditor.ShaderGraph.PreviewNode"
        };

        // Default slot definitions for common node types
        public static JArray GetDefaultSlots(string nodeType)
        {
            switch (nodeType)
            {
                case "SampleTexture2D":
                    return new JArray
                    {
                        MakeSlot("RGBA", 0, false, "Vector4"),
                        MakeSlot("R", 4, false, "Float"),
                        MakeSlot("G", 5, false, "Float"),
                        MakeSlot("B", 6, false, "Float"),
                        MakeSlot("A", 7, false, "Float"),
                        MakeSlot("Texture", 1, true, "Texture2D"),
                        MakeSlot("UV", 2, true, "Vector2"),
                        MakeSlot("Sampler", 3, true, "SamplerState")
                    };
                case "Color":
                    return new JArray
                    {
                        MakeSlot("Out", 0, false, "Color")
                    };
                case "Multiply":
                case "Add":
                case "Subtract":
                    return new JArray
                    {
                        MakeSlot("A", 0, true, "DynamicVector"),
                        MakeSlot("B", 1, true, "DynamicVector"),
                        MakeSlot("Out", 2, false, "DynamicVector")
                    };
                case "Lerp":
                    return new JArray
                    {
                        MakeSlot("A", 0, true, "DynamicVector"),
                        MakeSlot("B", 1, true, "DynamicVector"),
                        MakeSlot("T", 2, true, "DynamicVector"),
                        MakeSlot("Out", 3, false, "DynamicVector")
                    };
                case "UV":
                    return new JArray
                    {
                        MakeSlot("Out", 0, false, "Vector4")
                    };
                case "Time":
                    return new JArray
                    {
                        MakeSlot("Time", 0, false, "Float"),
                        MakeSlot("SineTime", 1, false, "Float"),
                        MakeSlot("CosineTime", 2, false, "Float"),
                        MakeSlot("DeltaTime", 3, false, "Float"),
                        MakeSlot("SmoothDelta", 4, false, "Float")
                    };
                case "Float":
                    return new JArray
                    {
                        MakeSlot("X", 0, true, "Float"),
                        MakeSlot("Out", 1, false, "Float")
                    };
                case "Split":
                    return new JArray
                    {
                        MakeSlot("In", 0, true, "DynamicVector"),
                        MakeSlot("R", 1, false, "Float"),
                        MakeSlot("G", 2, false, "Float"),
                        MakeSlot("B", 3, false, "Float"),
                        MakeSlot("A", 4, false, "Float")
                    };
                case "Combine":
                    return new JArray
                    {
                        MakeSlot("R", 0, true, "Float"),
                        MakeSlot("G", 1, true, "Float"),
                        MakeSlot("B", 2, true, "Float"),
                        MakeSlot("A", 3, true, "Float"),
                        MakeSlot("RGBA", 4, false, "Vector4"),
                        MakeSlot("RGB", 5, false, "Vector3"),
                        MakeSlot("RG", 6, false, "Vector2")
                    };
                default:
                    // For unrecognized types, return minimal input/output slots
                    return new JArray
                    {
                        MakeSlot("In", 0, true, "DynamicVector"),
                        MakeSlot("Out", 1, false, "DynamicVector")
                    };
            }
        }

        private static JObject MakeSlot(string name, int id, bool isInput, string valueType)
        {
            return new JObject
            {
                ["m_Id"] = id,
                ["m_DisplayName"] = name,
                ["m_SlotType"] = isInput ? 0 : 1,
                ["m_Priority"] = 2147483647,
                ["m_Hidden"] = false,
                ["m_ShaderOutputName"] = name,
                ["m_StageCapability"] = 3,
                ["m_ValueType"] = valueType
            };
        }

        // --- Property type mappings ---
        public static readonly Dictionary<string, int> PropertyTypeIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Color"] = 0,
            ["Float"] = 1,
            ["Vector2"] = 2,
            ["Vector3"] = 3,
            ["Vector4"] = 4,
            ["Texture2D"] = 5,
            ["Boolean"] = 6,
            ["Integer"] = 7
        };

        // --- Shader graph file operations ---

        public static JObject CreateEmptyGraph(string templateType, string shaderName)
        {
            if (!MasterNodeTypes.TryGetValue(templateType, out var masterType))
                masterType = MasterNodeTypes["urp_lit"];

            var masterNodeGuid = NewGuid();

            var graph = new JObject
            {
                ["m_SerializedVersion"] = 0,
                ["m_SGVersion"] = 3,
                ["m_Type"] = "UnityEditor.ShaderGraph.MaterialGraph",
                ["m_ObjectId"] = NewGuid(),
                ["m_Properties"] = new JArray(),
                ["m_Nodes"] = new JArray
                {
                    new JObject
                    {
                        ["m_Id"] = masterNodeGuid,
                        ["m_Type"] = masterType,
                        ["m_ObjectId"] = masterNodeGuid,
                        ["m_Name"] = shaderName ?? "New Shader",
                        ["m_DrawState"] = new JObject
                        {
                            ["m_Expanded"] = true,
                            ["m_Position"] = new JObject { ["x"] = 0, ["y"] = 0, ["width"] = 200, ["height"] = 400 }
                        },
                        ["m_Slots"] = GetMasterNodeSlots(templateType)
                    }
                },
                ["m_Edges"] = new JArray(),
                ["m_Path"] = "Shader Graphs",
                ["m_ShaderName"] = shaderName ?? "Custom/NewShader"
            };

            return graph;
        }

        public static JObject LoadGraph(string fullPath)
        {
            var text = File.ReadAllText(fullPath);
            return JObject.Parse(text);
        }

        public static void SaveGraph(string fullPath, JObject graph)
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, graph.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        public static JObject CreateNodeObject(string nodeType, float posX, float posY, JObject properties)
        {
            var nodeId = NewGuid();
            var internalType = NodeTypeMap.ContainsKey(nodeType) ? NodeTypeMap[nodeType] : nodeType;

            var node = new JObject
            {
                ["m_Id"] = nodeId,
                ["m_Type"] = internalType,
                ["m_ObjectId"] = nodeId,
                ["m_Name"] = nodeType,
                ["m_DrawState"] = new JObject
                {
                    ["m_Expanded"] = true,
                    ["m_Position"] = new JObject { ["x"] = posX, ["y"] = posY, ["width"] = 200, ["height"] = 150 }
                },
                ["m_Slots"] = GetDefaultSlots(nodeType)
            };

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    node[prop.Key] = prop.Value;
                }
            }

            return node;
        }

        public static JObject CreateEdge(string sourceNodeId, int sourceSlotId, string targetNodeId, int targetSlotId)
        {
            return new JObject
            {
                ["m_OutputSlot"] = new JObject
                {
                    ["m_Node"] = sourceNodeId,
                    ["m_SlotId"] = sourceSlotId
                },
                ["m_InputSlot"] = new JObject
                {
                    ["m_Node"] = targetNodeId,
                    ["m_SlotId"] = targetSlotId
                }
            };
        }

        public static JObject CreatePropertyObject(string propertyName, string propertyType, string referenceName, JToken defaultValue)
        {
            var propId = NewGuid();

            if (!PropertyTypeIds.TryGetValue(propertyType, out var typeId))
                typeId = 1; // default to Float

            var prop = new JObject
            {
                ["m_Id"] = propId,
                ["m_Name"] = propertyName,
                ["m_Type"] = typeId,
                ["m_ReferenceName"] = referenceName ?? ("_" + propertyName.Replace(" ", "")),
                ["m_DefaultValue"] = defaultValue ?? GetPropertyDefault(propertyType)
            };

            return prop;
        }

        public static JObject CreatePropertyNode(string propertyId, string propertyName, string propertyType, float posX, float posY)
        {
            var nodeId = NewGuid();
            string slotType;
            switch (propertyType.ToLower())
            {
                case "color": slotType = "Color"; break;
                case "float": slotType = "Float"; break;
                case "vector2": slotType = "Vector2"; break;
                case "vector3": slotType = "Vector3"; break;
                case "vector4": slotType = "Vector4"; break;
                case "texture2d": slotType = "Texture2D"; break;
                case "boolean": slotType = "Boolean"; break;
                case "integer": slotType = "Float"; break;
                default: slotType = "Float"; break;
            }

            return new JObject
            {
                ["m_Id"] = nodeId,
                ["m_Type"] = "UnityEditor.ShaderGraph.PropertyNode",
                ["m_ObjectId"] = nodeId,
                ["m_Name"] = propertyName,
                ["m_DrawState"] = new JObject
                {
                    ["m_Expanded"] = true,
                    ["m_Position"] = new JObject { ["x"] = posX, ["y"] = posY, ["width"] = 200, ["height"] = 100 }
                },
                ["m_Slots"] = new JArray
                {
                    MakeSlot("Out", 0, false, slotType)
                },
                ["m_PropertyId"] = propertyId
            };
        }

        private static JToken GetPropertyDefault(string propertyType)
        {
            switch (propertyType.ToLower())
            {
                case "color":
                    return new JObject { ["r"] = 1, ["g"] = 1, ["b"] = 1, ["a"] = 1 };
                case "float":
                case "integer":
                    return 0;
                case "vector2":
                    return new JObject { ["x"] = 0, ["y"] = 0 };
                case "vector3":
                    return new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 0 };
                case "vector4":
                    return new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 0, ["w"] = 0 };
                case "boolean":
                    return false;
                default:
                    return 0;
            }
        }
    }
}
