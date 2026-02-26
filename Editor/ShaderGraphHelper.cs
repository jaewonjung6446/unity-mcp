using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace McpUnity
{
    /// <summary>
    /// Unity 6 .shadergraph multi-document JSON format helper.
    /// A .shadergraph file consists of multiple JSON objects separated by blank lines.
    /// The first object is always GraphData; subsequent objects are nodes, slots, targets, etc.
    /// </summary>
    public static class ShaderGraphHelper
    {
        public static string NewGuid() => Guid.NewGuid().ToString("N");

        // ─── Node type mappings ─────────────────────────────────────────

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
            ["Preview"] = "UnityEditor.ShaderGraph.PreviewNode",
            ["SamplerState"] = "UnityEditor.ShaderGraph.SamplerStateNode"
        };

        // Property type → internal serialized type string
        public static readonly Dictionary<string, string> PropertyTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Color"] = "UnityEditor.ShaderGraph.Internal.SerializableColor",
            ["Float"] = "UnityEditor.ShaderGraph.Internal.SerializableFloat",
            ["Vector2"] = "UnityEditor.ShaderGraph.Internal.SerializableVector2",
            ["Vector3"] = "UnityEditor.ShaderGraph.Internal.SerializableVector3",
            ["Vector4"] = "UnityEditor.ShaderGraph.Internal.SerializableVector4",
            ["Texture2D"] = "UnityEditor.ShaderGraph.Internal.SerializableTexture",
            ["Boolean"] = "UnityEditor.ShaderGraph.Internal.SerializableBoolean",
            ["Integer"] = "UnityEditor.ShaderGraph.Internal.SerializableInteger"
        };

        // ─── Multi-document file I/O ────────────────────────────────────

        /// <summary>
        /// Parse a .shadergraph file into a list of JObjects.
        /// The first element is always GraphData.
        /// </summary>
        public static List<JObject> LoadDocuments(string fullPath)
        {
            var text = File.ReadAllText(fullPath);
            return ParseDocuments(text);
        }

        public static List<JObject> ParseDocuments(string text)
        {
            var docs = new List<JObject>();
            // Split on blank lines between JSON objects: "}\n\n{"
            var parts = Regex.Split(text.Trim(), @"\}\s*\n\s*\n\s*\{");

            if (parts.Length == 1)
            {
                // Single document
                docs.Add(JObject.Parse(text.Trim()));
                return docs;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                // Re-add braces stripped by the split
                if (i == 0)
                    part = part + "}";
                else if (i == parts.Length - 1)
                    part = "{" + part;
                else
                    part = "{" + part + "}";

                docs.Add(JObject.Parse(part));
            }

            return docs;
        }

        /// <summary>
        /// Serialize a list of JObjects back to .shadergraph multi-document format.
        /// </summary>
        public static string SerializeDocuments(List<JObject> docs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < docs.Count; i++)
            {
                if (i > 0)
                    sb.AppendLine(); // blank line separator
                sb.AppendLine(docs[i].ToString(Formatting.Indented));
            }
            return sb.ToString();
        }

        public static void SaveDocuments(string fullPath, List<JObject> docs)
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, SerializeDocuments(docs));
        }

        /// <summary>
        /// Get the GraphData object (first document).
        /// </summary>
        public static JObject GetGraphData(List<JObject> docs)
        {
            return docs[0];
        }

        /// <summary>
        /// Find a document by its m_ObjectId.
        /// </summary>
        public static JObject FindById(List<JObject> docs, string objectId)
        {
            foreach (var doc in docs)
            {
                if (doc["m_ObjectId"]?.ToString() == objectId)
                    return doc;
            }
            return null;
        }

        // ─── Edge creation (Unity 6 format) ─────────────────────────────

        public static JObject CreateEdge(string sourceNodeId, int sourceSlotId, string targetNodeId, int targetSlotId)
        {
            return new JObject
            {
                ["m_OutputSlot"] = new JObject
                {
                    ["m_Node"] = new JObject { ["m_Id"] = sourceNodeId },
                    ["m_SlotId"] = sourceSlotId
                },
                ["m_InputSlot"] = new JObject
                {
                    ["m_Node"] = new JObject { ["m_Id"] = targetNodeId },
                    ["m_SlotId"] = targetSlotId
                }
            };
        }

        // ─── Slot object creation (separate top-level document) ─────────

        private static JObject MakeSlotDoc(string slotType, string objectId, int slotId,
            string displayName, int slotDirection, int stageCapability, object defaultValue = null)
        {
            var slot = new JObject
            {
                ["m_SGVersion"] = 0,
                ["m_Type"] = slotType,
                ["m_ObjectId"] = objectId,
                ["m_Id"] = slotId,
                ["m_DisplayName"] = displayName,
                ["m_SlotType"] = slotDirection, // 0=input, 1=output
                ["m_Hidden"] = false,
                ["m_ShaderOutputName"] = displayName,
                ["m_StageCapability"] = stageCapability
            };
            if (defaultValue is JObject jVal)
            {
                slot["m_Value"] = jVal.DeepClone();
                slot["m_DefaultValue"] = jVal.DeepClone();
            }
            else if (defaultValue != null)
            {
                slot["m_Value"] = JToken.FromObject(defaultValue);
                slot["m_DefaultValue"] = JToken.FromObject(defaultValue);
            }
            slot["m_Labels"] = new JArray();
            return slot;
        }

        // ─── Slot definitions per node type ─────────────────────────────

        /// <summary>
        /// Slot definition: (slotType, slotId, displayName, isInput, stageCapability, defaultValue)
        /// </summary>
        public struct SlotDef
        {
            public string Type;
            public int Id;
            public string Name;
            public bool IsInput;
            public int Stage; // 3=all, 1=vertex, 2=fragment
            public object Default;

            public SlotDef(string type, int id, string name, bool isInput, int stage = 3, object def = null)
            {
                Type = type; Id = id; Name = name; IsInput = isInput; Stage = stage; Default = def;
            }
        }

        public static SlotDef[] GetSlotDefs(string nodeType)
        {
            switch (nodeType)
            {
                case "SampleTexture2D":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector4MaterialSlot", 0, "RGBA", false, 2),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 4, "R", false, 2),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 5, "G", false, 2),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 6, "B", false, 2),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 7, "A", false, 2),
                        new SlotDef("UnityEditor.ShaderGraph.Texture2DInputMaterialSlot", 1, "Texture", true),
                        new SlotDef("UnityEditor.ShaderGraph.UVMaterialSlot", 2, "UV", true),
                        new SlotDef("UnityEditor.ShaderGraph.SamplerStateMaterialSlot", 3, "Sampler", true)
                    };
                case "Color":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.ColorRGBMaterialSlot", 0, "Out", false)
                    };
                case "Multiply":
                case "Add":
                case "Subtract":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.DynamicValueMaterialSlot", 0, "A", true),
                        new SlotDef("UnityEditor.ShaderGraph.DynamicValueMaterialSlot", 1, "B", true),
                        new SlotDef("UnityEditor.ShaderGraph.DynamicValueMaterialSlot", 2, "Out", false)
                    };
                case "Lerp":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.DynamicValueMaterialSlot", 0, "A", true),
                        new SlotDef("UnityEditor.ShaderGraph.DynamicValueMaterialSlot", 1, "B", true),
                        new SlotDef("UnityEditor.ShaderGraph.DynamicValueMaterialSlot", 2, "T", true),
                        new SlotDef("UnityEditor.ShaderGraph.DynamicValueMaterialSlot", 3, "Out", false)
                    };
                case "UV":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector4MaterialSlot", 0, "Out", false)
                    };
                case "Time":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 0, "Time", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 1, "Sine Time", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 2, "Cosine Time", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 3, "Delta Time", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 4, "Smooth Delta", false)
                    };
                case "Float":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 0, "X", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 1, "Out", false)
                    };
                case "Vector2":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 1, "X", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 2, "Y", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector2MaterialSlot", 0, "Out", false)
                    };
                case "Vector3":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 1, "X", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 2, "Y", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 3, "Z", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector3MaterialSlot", 0, "Out", false)
                    };
                case "Vector4":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 1, "X", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 2, "Y", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 3, "Z", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 4, "W", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector4MaterialSlot", 0, "Out", false)
                    };
                case "Split":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.DynamicVectorMaterialSlot", 0, "In", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 1, "R", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 2, "G", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 3, "B", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 4, "A", false)
                    };
                case "Combine":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 0, "R", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 1, "G", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 2, "B", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 3, "A", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector4MaterialSlot", 4, "RGBA", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector3MaterialSlot", 5, "RGB", false),
                        new SlotDef("UnityEditor.ShaderGraph.Vector2MaterialSlot", 6, "RG", false)
                    };
                case "Fresnel":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.NormalMaterialSlot", 0, "Normal", true),
                        new SlotDef("UnityEditor.ShaderGraph.ViewDirectionMaterialSlot", 1, "View Dir", true),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 2, "Power", true, 3, 5.0),
                        new SlotDef("UnityEditor.ShaderGraph.Vector1MaterialSlot", 3, "Out", false)
                    };
                case "SamplerState":
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.SamplerStateMaterialSlot", 0, "Out", false)
                    };
                default:
                    return new[]
                    {
                        new SlotDef("UnityEditor.ShaderGraph.DynamicVectorMaterialSlot", 0, "In", true),
                        new SlotDef("UnityEditor.ShaderGraph.DynamicVectorMaterialSlot", 1, "Out", false)
                    };
            }
        }

        // ─── Node document creation ─────────────────────────────────────

        /// <summary>
        /// Creates a node document and its slot documents.
        /// Returns (nodeDoc, slotDocs, nodeId).
        /// </summary>
        public static (JObject nodeDoc, List<JObject> slotDocs, string nodeId) CreateNode(
            string nodeType, float posX, float posY, JObject extraProps = null)
        {
            var nodeId = NewGuid();
            var internalType = NodeTypeMap.ContainsKey(nodeType) ? NodeTypeMap[nodeType] : nodeType;

            var slotDefs = GetSlotDefs(nodeType);
            var slotRefs = new JArray();
            var slotDocs = new List<JObject>();

            foreach (var sd in slotDefs)
            {
                var slotObjId = NewGuid();
                slotRefs.Add(new JObject { ["m_Id"] = slotObjId });
                slotDocs.Add(MakeSlotDoc(sd.Type, slotObjId, sd.Id, sd.Name,
                    sd.IsInput ? 0 : 1, sd.Stage, sd.Default));
            }

            var node = new JObject
            {
                ["m_SGVersion"] = 0,
                ["m_Type"] = internalType,
                ["m_ObjectId"] = nodeId,
                ["m_Group"] = new JObject { ["m_Id"] = "" },
                ["m_Name"] = nodeType,
                ["m_DrawState"] = new JObject
                {
                    ["m_Expanded"] = true,
                    ["m_Position"] = new JObject
                    {
                        ["serializedVersion"] = "2",
                        ["x"] = posX,
                        ["y"] = posY,
                        ["width"] = 208.0,
                        ["height"] = 300.0
                    }
                },
                ["m_Slots"] = slotRefs,
                ["synonyms"] = new JArray(),
                ["m_Precision"] = 0,
                ["m_PreviewExpanded"] = true,
                ["m_DismissedVersion"] = 0,
                ["m_PreviewMode"] = 0,
                ["m_CustomColors"] = new JObject
                {
                    ["m_SerializableColors"] = new JArray()
                }
            };

            if (extraProps != null)
            {
                foreach (var prop in extraProps)
                    node[prop.Key] = prop.Value;
            }

            // Add node-type-specific fields
            if (nodeType == "UV")
                node["m_OutputChannel"] = 0;
            else if (nodeType == "SampleTexture2D")
            {
                node["m_TextureType"] = 0;
                node["m_NormalMapSpace"] = 0;
                node["m_EnableGlobalMipBias"] = true;
                node["m_MipSamplingMode"] = 0;
            }
            else if (nodeType == "SamplerState")
            {
                node["m_filter"] = 0;
                node["m_wrap"] = 0;
                node["m_aniso"] = 0;
            }

            return (node, slotDocs, nodeId);
        }

        // ─── Template creation (empty graph) ────────────────────────────

        /// <summary>
        /// Block node definitions for Vertex and Fragment contexts.
        /// </summary>
        private struct BlockDef
        {
            public string Descriptor;
            public string SlotType;
            public string SlotName;
            public int SlotStage;
            public object DefaultValue;
        }

        private static readonly BlockDef[] VertexBlocks = new[]
        {
            new BlockDef {
                Descriptor = "VertexDescription.Position",
                SlotType = "UnityEditor.ShaderGraph.PositionMaterialSlot",
                SlotName = "Position", SlotStage = 1,
                DefaultValue = new { x = 0.0, y = 0.0, z = 0.0 }
            },
            new BlockDef {
                Descriptor = "VertexDescription.Normal",
                SlotType = "UnityEditor.ShaderGraph.NormalMaterialSlot",
                SlotName = "Normal", SlotStage = 1,
                DefaultValue = new { x = 0.0, y = 0.0, z = 0.0 }
            },
            new BlockDef {
                Descriptor = "VertexDescription.Tangent",
                SlotType = "UnityEditor.ShaderGraph.TangentMaterialSlot",
                SlotName = "Tangent", SlotStage = 1,
                DefaultValue = new { x = 0.0, y = 0.0, z = 0.0 }
            }
        };

        private static readonly BlockDef[] CanvasFragmentBlocks = new[]
        {
            new BlockDef {
                Descriptor = "SurfaceDescription.BaseColor",
                SlotType = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot",
                SlotName = "Base Color", SlotStage = 2,
                DefaultValue = new { x = 0.5, y = 0.5, z = 0.5 }
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Emission",
                SlotType = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot",
                SlotName = "Emission", SlotStage = 2,
                DefaultValue = new { x = 0.0, y = 0.0, z = 0.0 }
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Alpha",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Alpha", SlotStage = 2,
                DefaultValue = 1.0
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.AlphaClipThreshold",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Alpha Clip Threshold", SlotStage = 2,
                DefaultValue = 0.5
            }
        };

        private static readonly BlockDef[] UnlitFragmentBlocks = new[]
        {
            new BlockDef {
                Descriptor = "SurfaceDescription.BaseColor",
                SlotType = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot",
                SlotName = "Base Color", SlotStage = 2,
                DefaultValue = new { x = 0.5, y = 0.5, z = 0.5 }
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Alpha",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Alpha", SlotStage = 2,
                DefaultValue = 1.0
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.AlphaClipThreshold",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Alpha Clip Threshold", SlotStage = 2,
                DefaultValue = 0.5
            }
        };

        private static readonly BlockDef[] LitFragmentBlocks = new[]
        {
            new BlockDef {
                Descriptor = "SurfaceDescription.BaseColor",
                SlotType = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot",
                SlotName = "Base Color", SlotStage = 2,
                DefaultValue = new { x = 0.5, y = 0.5, z = 0.5 }
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.NormalTS",
                SlotType = "UnityEditor.ShaderGraph.NormalMaterialSlot",
                SlotName = "Normal (Tangent Space)", SlotStage = 2,
                DefaultValue = new { x = 0.0, y = 0.0, z = 1.0 }
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Metallic",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Metallic", SlotStage = 2,
                DefaultValue = 0.0
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Smoothness",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Smoothness", SlotStage = 2,
                DefaultValue = 0.5
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Emission",
                SlotType = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot",
                SlotName = "Emission", SlotStage = 2,
                DefaultValue = new { x = 0.0, y = 0.0, z = 0.0 }
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Occlusion",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Ambient Occlusion", SlotStage = 2,
                DefaultValue = 1.0
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.Alpha",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Alpha", SlotStage = 2,
                DefaultValue = 1.0
            },
            new BlockDef {
                Descriptor = "SurfaceDescription.AlphaClipThreshold",
                SlotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot",
                SlotName = "Alpha Clip Threshold", SlotStage = 2,
                DefaultValue = 0.5
            }
        };

        private static (JObject blockNode, JObject slotDoc, string blockId) CreateBlockNode(BlockDef def)
        {
            var blockId = NewGuid();
            var slotId = NewGuid();

            var slotDoc = new JObject
            {
                ["m_SGVersion"] = 0,
                ["m_Type"] = def.SlotType,
                ["m_ObjectId"] = slotId,
                ["m_Id"] = 0,
                ["m_DisplayName"] = def.SlotName,
                ["m_SlotType"] = 0, // input
                ["m_Hidden"] = false,
                ["m_ShaderOutputName"] = def.Descriptor.Split('.')[1],
                ["m_StageCapability"] = def.SlotStage,
                ["m_Value"] = JToken.FromObject(def.DefaultValue),
                ["m_DefaultValue"] = JToken.FromObject(def.DefaultValue),
                ["m_Labels"] = new JArray()
            };

            // PositionMaterialSlot, NormalMaterialSlot, TangentMaterialSlot have m_Space
            if (def.SlotType.Contains("PositionMaterial") ||
                def.SlotType.Contains("NormalMaterial") ||
                def.SlotType.Contains("TangentMaterial"))
            {
                slotDoc["m_Space"] = 0;
            }

            // ColorRGBMaterialSlot has extra fields
            if (def.SlotType.Contains("ColorRGB"))
            {
                slotDoc["m_ColorMode"] = 0;
                slotDoc["m_DefaultColor"] = new JObject { ["r"] = 0.5, ["g"] = 0.5, ["b"] = 0.5, ["a"] = 1.0 };
            }

            var blockNode = new JObject
            {
                ["m_SGVersion"] = 0,
                ["m_Type"] = "UnityEditor.ShaderGraph.BlockNode",
                ["m_ObjectId"] = blockId,
                ["m_Group"] = new JObject { ["m_Id"] = "" },
                ["m_Name"] = def.Descriptor,
                ["m_DrawState"] = new JObject
                {
                    ["m_Expanded"] = true,
                    ["m_Position"] = new JObject
                    {
                        ["serializedVersion"] = "2",
                        ["x"] = 0.0,
                        ["y"] = 0.0,
                        ["width"] = 0.0,
                        ["height"] = 0.0
                    }
                },
                ["m_Slots"] = new JArray { new JObject { ["m_Id"] = slotId } },
                ["synonyms"] = new JArray(),
                ["m_Precision"] = 0,
                ["m_PreviewExpanded"] = true,
                ["m_DismissedVersion"] = 0,
                ["m_PreviewMode"] = 0,
                ["m_CustomColors"] = new JObject { ["m_SerializableColors"] = new JArray() },
                ["m_SerializedDescriptor"] = def.Descriptor
            };

            return (blockNode, slotDoc, blockId);
        }

        /// <summary>
        /// Create a complete empty shader graph as a list of documents.
        /// </summary>
        public static List<JObject> CreateEmptyGraph(string templateType, string shaderName)
        {
            var docs = new List<JObject>();

            bool isLit = templateType == "urp_lit";
            bool isCanvas = templateType == "urp_canvas";
            var graphId = NewGuid();
            var categoryId = NewGuid();
            var targetId = NewGuid();
            var subTargetId = NewGuid();

            // Build block nodes
            var vertexBlockIds = new JArray();
            var fragmentBlockIds = new JArray();
            var nodeRefs = new JArray();
            var allBlockDocs = new List<JObject>();
            var allSlotDocs = new List<JObject>();

            // Canvas graphs have no vertex blocks
            if (!isCanvas)
            {
                foreach (var def in VertexBlocks)
                {
                    var (block, slot, id) = CreateBlockNode(def);
                    vertexBlockIds.Add(new JObject { ["m_Id"] = id });
                    nodeRefs.Add(new JObject { ["m_Id"] = id });
                    allBlockDocs.Add(block);
                    allSlotDocs.Add(slot);
                }
            }

            var fragBlocks = isCanvas ? CanvasFragmentBlocks : isLit ? LitFragmentBlocks : UnlitFragmentBlocks;
            foreach (var def in fragBlocks)
            {
                var (block, slot, id) = CreateBlockNode(def);
                fragmentBlockIds.Add(new JObject { ["m_Id"] = id });
                nodeRefs.Add(new JObject { ["m_Id"] = id });
                allBlockDocs.Add(block);
                allSlotDocs.Add(slot);
            }

            // GraphData (first document)
            var graphData = new JObject
            {
                ["m_SGVersion"] = 3,
                ["m_Type"] = "UnityEditor.ShaderGraph.GraphData",
                ["m_ObjectId"] = graphId,
                ["m_Properties"] = new JArray(),
                ["m_Keywords"] = new JArray(),
                ["m_Dropdowns"] = new JArray(),
                ["m_CategoryData"] = new JArray { new JObject { ["m_Id"] = categoryId } },
                ["m_Nodes"] = nodeRefs,
                ["m_GroupDatas"] = new JArray(),
                ["m_StickyNoteDatas"] = new JArray(),
                ["m_Edges"] = new JArray(),
                ["m_VertexContext"] = new JObject
                {
                    ["m_Position"] = new JObject { ["x"] = 0.0, ["y"] = 0.0 },
                    ["m_Blocks"] = vertexBlockIds
                },
                ["m_FragmentContext"] = new JObject
                {
                    ["m_Position"] = new JObject { ["x"] = 0.0, ["y"] = 200.0 },
                    ["m_Blocks"] = fragmentBlockIds
                },
                ["m_PreviewData"] = new JObject
                {
                    ["serializedMesh"] = new JObject
                    {
                        ["m_SerializedMesh"] = "{\"mesh\":{\"fileID\":10210,\"guid\":\"0000000000000000e000000000000000\",\"type\":0}}",
                        ["m_Guid"] = ""
                    },
                    ["preventRotation"] = false
                },
                ["m_Path"] = "Shader Graphs",
                ["m_GraphPrecision"] = 1,
                ["m_PreviewMode"] = 2,
                ["m_OutputNode"] = new JObject { ["m_Id"] = "" },
                ["m_SubDatas"] = new JArray(),
                ["m_ActiveTargets"] = new JArray { new JObject { ["m_Id"] = targetId } }
            };
            docs.Add(graphData);

            // CategoryData
            docs.Add(new JObject
            {
                ["m_SGVersion"] = 0,
                ["m_Type"] = "UnityEditor.ShaderGraph.CategoryData",
                ["m_ObjectId"] = categoryId,
                ["m_Name"] = "",
                ["m_ChildObjectList"] = new JArray()
            });

            // Target
            docs.Add(new JObject
            {
                ["m_SGVersion"] = 1,
                ["m_Type"] = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget",
                ["m_ObjectId"] = targetId,
                ["m_Datas"] = new JArray(),
                ["m_ActiveSubTarget"] = new JObject { ["m_Id"] = subTargetId },
                ["m_AllowMaterialOverride"] = false,
                ["m_SurfaceType"] = 0,
                ["m_ZTestMode"] = 4,
                ["m_ZWriteControl"] = 0,
                ["m_AlphaMode"] = 0,
                ["m_RenderFace"] = 2,
                ["m_AlphaClip"] = false,
                ["m_CastShadows"] = true,
                ["m_ReceiveShadows"] = true,
                ["m_DisableTint"] = false,
                ["m_AdditionalMotionVectorMode"] = 0,
                ["m_AlembicMotionVectors"] = false,
                ["m_SupportsLODCrossFade"] = false,
                ["m_CustomEditorGUI"] = "",
                ["m_SupportVFX"] = false
            });

            // SubTarget
            string subTargetType;
            if (isCanvas)
                subTargetType = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalCanvasSubTarget";
            else if (isLit)
                subTargetType = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalLitSubTarget";
            else
                subTargetType = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget";
            docs.Add(new JObject
            {
                ["m_SGVersion"] = 2,
                ["m_Type"] = subTargetType,
                ["m_ObjectId"] = subTargetId
            });

            // Block nodes and their slots
            docs.AddRange(allBlockDocs);
            docs.AddRange(allSlotDocs);

            return docs;
        }

        // ─── Property creation ──────────────────────────────────────────

        /// <summary>
        /// Create a property document and its PropertyNode + slot documents.
        /// Returns (propertyDoc, propertyNodeDoc, slotDocs, propertyId, nodeId).
        /// </summary>
        public static (JObject propDoc, JObject nodeDoc, List<JObject> slotDocs, string propId, string nodeId)
            CreateProperty(string propertyName, string propertyType, string referenceName, JToken defaultValue, float posX, float posY)
        {
            var propId = NewGuid();
            var nodeId = NewGuid();
            var refName = referenceName ?? ("_" + propertyName.Replace(" ", ""));

            // Property document
            if (!PropertyTypeMap.TryGetValue(propertyType, out var internalPropType))
                internalPropType = "UnityEditor.ShaderGraph.Internal.SerializableFloat";

            var propDoc = new JObject
            {
                ["m_SGVersion"] = 0,
                ["m_Type"] = internalPropType,
                ["m_ObjectId"] = propId,
                ["m_Guid"] = new JObject { ["m_GuidSerialized"] = Guid.NewGuid().ToString() },
                ["m_Name"] = propertyName,
                ["m_DefaultReferenceName"] = refName,
                ["m_OverrideReferenceName"] = refName,
                ["m_GeneratePropertyBlock"] = true,
                ["m_UseCustomSlotLabel"] = false,
                ["m_CustomSlotLabel"] = "",
                ["m_Precision"] = 0
            };

            if (defaultValue != null)
                propDoc["m_Value"] = defaultValue;
            else
                propDoc["m_Value"] = GetPropertyDefault(propertyType);

            // PropertyNode + slots
            string slotType;
            switch (propertyType.ToLower())
            {
                case "color": slotType = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot"; break;
                case "float": slotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot"; break;
                case "integer": slotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot"; break;
                case "vector2": slotType = "UnityEditor.ShaderGraph.Vector2MaterialSlot"; break;
                case "vector3": slotType = "UnityEditor.ShaderGraph.Vector3MaterialSlot"; break;
                case "vector4": slotType = "UnityEditor.ShaderGraph.Vector4MaterialSlot"; break;
                case "texture2d": slotType = "UnityEditor.ShaderGraph.Texture2DMaterialSlot"; break;
                case "boolean": slotType = "UnityEditor.ShaderGraph.BooleanMaterialSlot"; break;
                default: slotType = "UnityEditor.ShaderGraph.Vector1MaterialSlot"; break;
            }

            var slotObjId = NewGuid();
            var slotDoc = MakeSlotDoc(slotType, slotObjId, 0, "Out", 1, 3); // output slot
            var slotDocs = new List<JObject> { slotDoc };

            var nodeDoc = new JObject
            {
                ["m_SGVersion"] = 0,
                ["m_Type"] = "UnityEditor.ShaderGraph.PropertyNode",
                ["m_ObjectId"] = nodeId,
                ["m_Group"] = new JObject { ["m_Id"] = "" },
                ["m_Name"] = "Property",
                ["m_DrawState"] = new JObject
                {
                    ["m_Expanded"] = true,
                    ["m_Position"] = new JObject
                    {
                        ["serializedVersion"] = "2",
                        ["x"] = posX,
                        ["y"] = posY,
                        ["width"] = 200.0,
                        ["height"] = 100.0
                    }
                },
                ["m_Slots"] = new JArray { new JObject { ["m_Id"] = slotObjId } },
                ["synonyms"] = new JArray(),
                ["m_Precision"] = 0,
                ["m_PreviewExpanded"] = true,
                ["m_DismissedVersion"] = 0,
                ["m_PreviewMode"] = 0,
                ["m_CustomColors"] = new JObject { ["m_SerializableColors"] = new JArray() },
                ["m_Property"] = new JObject { ["m_Id"] = propId }
            };

            return (propDoc, nodeDoc, slotDocs, propId, nodeId);
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
