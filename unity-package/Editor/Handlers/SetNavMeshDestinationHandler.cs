using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace McpUnity.Handlers
{
    public class SetNavMeshDestinationHandler : IToolHandler
    {
        public string Name => "set_navmesh_destination";

        public JObject Execute(JObject parameters)
        {
            if (!EditorApplication.isPlaying)
                return McpServer.CreateError("NavMesh navigation requires Play Mode", "invalid_state");

            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null)
                return McpServer.CreateError($"No NavMeshAgent found on '{go.name}'", "not_found_error");

            if (!agent.isOnNavMesh)
                return McpServer.CreateError($"NavMeshAgent on '{go.name}' is not on the NavMesh", "invalid_state");

            var destination = parameters["destination"] as JObject;
            var stop = parameters["stop"]?.ToObject<bool>() ?? false;

            if (stop)
            {
                agent.isStopped = true;
                agent.ResetPath();
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Stopped NavMeshAgent on '{go.name}'"
                };
            }

            if (destination == null)
                return McpServer.CreateError("Missing required parameter: destination", "validation_error");

            var dest = new Vector3(
                destination["x"]?.ToObject<float>() ?? 0f,
                destination["y"]?.ToObject<float>() ?? 0f,
                destination["z"]?.ToObject<float>() ?? 0f
            );

            agent.isStopped = false;
            bool success = agent.SetDestination(dest);

            return new JObject
            {
                ["success"] = success,
                ["type"] = "text",
                ["message"] = success
                    ? $"Set destination for '{go.name}' to ({dest.x:F1}, {dest.y:F1}, {dest.z:F1})"
                    : "Failed to set destination (invalid path)"
            };
        }
    }
}
