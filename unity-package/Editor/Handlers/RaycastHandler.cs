using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class RaycastHandler : IToolHandler
    {
        public string Name => "raycast";

        public JObject Execute(JObject parameters)
        {
            var origin = parameters["origin"] as JObject;
            var direction = parameters["direction"] as JObject;
            var screenPoint = parameters["screenPoint"] as JObject;
            var maxDistance = parameters["maxDistance"]?.ToObject<float>() ?? 1000f;
            var layerMask = parameters["layerMask"]?.ToObject<int>() ?? -1;

            Ray ray;

            if (screenPoint != null)
            {
                // Raycast from screen point through camera
                var camera = Camera.main;
                if (camera == null)
                    return McpServer.CreateError("No main camera found in scene", "not_found_error");

                var screenPos = new Vector3(
                    screenPoint["x"]?.ToObject<float>() ?? 0f,
                    screenPoint["y"]?.ToObject<float>() ?? 0f,
                    0f
                );
                ray = camera.ScreenPointToRay(screenPos);
            }
            else if (origin != null && direction != null)
            {
                ray = new Ray(
                    new Vector3(
                        origin["x"]?.ToObject<float>() ?? 0f,
                        origin["y"]?.ToObject<float>() ?? 0f,
                        origin["z"]?.ToObject<float>() ?? 0f
                    ),
                    new Vector3(
                        direction["x"]?.ToObject<float>() ?? 0f,
                        direction["y"]?.ToObject<float>() ?? -1f,
                        direction["z"]?.ToObject<float>() ?? 0f
                    ).normalized
                );
            }
            else
            {
                return McpServer.CreateError("Provide either screenPoint or both origin and direction", "validation_error");
            }

            var allHits = parameters["all"]?.ToObject<bool>() ?? false;

            if (allHits)
            {
                var hits = Physics.RaycastAll(ray, maxDistance, layerMask);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                var results = new JArray();
                foreach (var hit in hits)
                    results.Add(SerializeHit(hit));

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Raycast hit {hits.Length} objects",
                    ["hits"] = results
                };
            }
            else
            {
                if (Physics.Raycast(ray, out var hit, maxDistance, layerMask))
                {
                    return new JObject
                    {
                        ["success"] = true,
                        ["type"] = "text",
                        ["message"] = $"Raycast hit '{hit.collider.gameObject.name}'",
                        ["hit"] = SerializeHit(hit)
                    };
                }

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = "Raycast hit nothing",
                    ["hit"] = null
                };
            }
        }

        private static JObject SerializeHit(RaycastHit hit)
        {
            return new JObject
            {
                ["gameObject"] = hit.collider.gameObject.name,
                ["instanceId"] = hit.collider.gameObject.GetInstanceID(),
                ["path"] = GetGameObjectHandler.GetPath(hit.collider.gameObject),
                ["point"] = new JObject { ["x"] = hit.point.x, ["y"] = hit.point.y, ["z"] = hit.point.z },
                ["normal"] = new JObject { ["x"] = hit.normal.x, ["y"] = hit.normal.y, ["z"] = hit.normal.z },
                ["distance"] = hit.distance,
                ["colliderType"] = hit.collider.GetType().Name,
                ["tag"] = hit.collider.tag,
                ["layer"] = LayerMask.LayerToName(hit.collider.gameObject.layer)
            };
        }
    }
}
