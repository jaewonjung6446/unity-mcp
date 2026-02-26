using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace McpUnity.Handlers
{
    public class GetNavMeshInfoHandler : IToolHandler
    {
        public string Name => "get_navmesh_info";

        public JObject Execute(JObject parameters)
        {
            var triangulation = NavMesh.CalculateTriangulation();

            // NavMesh agents
#if UNITY_2022_1_OR_NEWER
            var agents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            var obstacles = Object.FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None);
#else
            var agents = Object.FindObjectsOfType<NavMeshAgent>();
            var obstacles = Object.FindObjectsOfType<NavMeshObstacle>();
#endif

            var agentArray = new JArray();
            foreach (var agent in agents)
            {
                var agentInfo = new JObject
                {
                    ["gameObject"] = agent.gameObject.name,
                    ["instanceId"] = agent.gameObject.GetInstanceID(),
                    ["path"] = GetGameObjectHandler.GetPath(agent.gameObject),
                    ["enabled"] = agent.enabled,
                    ["speed"] = agent.speed,
                    ["angularSpeed"] = agent.angularSpeed,
                    ["acceleration"] = agent.acceleration,
                    ["stoppingDistance"] = agent.stoppingDistance,
                    ["radius"] = agent.radius,
                    ["height"] = agent.height,
                    ["isOnNavMesh"] = agent.isOnNavMesh,
                    ["hasPath"] = agent.hasPath,
                    ["isStopped"] = agent.isStopped,
                    ["remainingDistance"] = agent.isOnNavMesh ? agent.remainingDistance : -1f,
                    ["position"] = new JObject
                    {
                        ["x"] = agent.transform.position.x,
                        ["y"] = agent.transform.position.y,
                        ["z"] = agent.transform.position.z
                    }
                };

                if (agent.isOnNavMesh && agent.hasPath)
                {
                    agentInfo["destination"] = new JObject
                    {
                        ["x"] = agent.destination.x,
                        ["y"] = agent.destination.y,
                        ["z"] = agent.destination.z
                    };
                }

                agentArray.Add(agentInfo);
            }

            var obstacleArray = new JArray();
            foreach (var obs in obstacles)
            {
                obstacleArray.Add(new JObject
                {
                    ["gameObject"] = obs.gameObject.name,
                    ["instanceId"] = obs.gameObject.GetInstanceID(),
                    ["carving"] = obs.carving,
                    ["shape"] = obs.shape.ToString(),
                    ["radius"] = obs.radius,
                    ["height"] = obs.height,
                    ["size"] = new JObject { ["x"] = obs.size.x, ["y"] = obs.size.y, ["z"] = obs.size.z }
                });
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"NavMesh info: {agents.Length} agents, {obstacles.Length} obstacles, {triangulation.vertices.Length} vertices",
                ["hasNavMesh"] = triangulation.vertices.Length > 0,
                ["vertexCount"] = triangulation.vertices.Length,
                ["triangleCount"] = triangulation.indices.Length / 3,
                ["agents"] = agentArray,
                ["obstacles"] = obstacleArray
            };
        }
    }
}
