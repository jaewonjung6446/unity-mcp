using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetTerrainInfoHandler : IToolHandler
    {
        public string Name => "get_terrain_info";

        public JObject Execute(JObject parameters)
        {
            Terrain terrain = null;

            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var _);
            if (go != null)
                terrain = go.GetComponent<Terrain>();

            if (terrain == null)
            {
#if UNITY_2022_1_OR_NEWER
                terrain = Object.FindFirstObjectByType<Terrain>();
#else
                terrain = Object.FindObjectOfType<Terrain>();
#endif
            }

            if (terrain == null)
                return McpServer.CreateError("No Terrain found in scene", "not_found_error");

            var data = terrain.terrainData;
            if (data == null)
                return McpServer.CreateError("Terrain has no TerrainData", "not_found_error");

            // Terrain layers
            var layers = new JArray();
            if (data.terrainLayers != null)
            {
                foreach (var layer in data.terrainLayers)
                {
                    if (layer == null) continue;
                    layers.Add(new JObject
                    {
                        ["name"] = layer.name,
                        ["tileSize"] = new JObject { ["x"] = layer.tileSize.x, ["y"] = layer.tileSize.y },
                        ["diffuseTexture"] = layer.diffuseTexture != null ? layer.diffuseTexture.name : null
                    });
                }
            }

            // Tree prototypes
            var trees = new JArray();
            foreach (var tree in data.treePrototypes)
            {
                trees.Add(new JObject
                {
                    ["prefab"] = tree.prefab != null ? tree.prefab.name : null
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Terrain info for '{terrain.gameObject.name}'",
                ["instanceId"] = terrain.gameObject.GetInstanceID(),
                ["size"] = new JObject { ["x"] = data.size.x, ["y"] = data.size.y, ["z"] = data.size.z },
                ["heightmapResolution"] = data.heightmapResolution,
                ["alphamapResolution"] = data.alphamapResolution,
                ["detailResolution"] = data.detailResolution,
                ["terrainLayers"] = layers,
                ["treePrototypes"] = trees,
                ["treeInstanceCount"] = data.treeInstanceCount,
                ["position"] = new JObject
                {
                    ["x"] = terrain.transform.position.x,
                    ["y"] = terrain.transform.position.y,
                    ["z"] = terrain.transform.position.z
                }
            };
        }
    }
}
