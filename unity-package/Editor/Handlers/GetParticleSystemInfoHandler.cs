using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetParticleSystemInfoHandler : IToolHandler
    {
        public string Name => "get_particle_system_info";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                return McpServer.CreateError($"No ParticleSystem found on '{go.name}'", "not_found_error");

            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"ParticleSystem info for '{go.name}'",
                ["isPlaying"] = ps.isPlaying,
                ["isPaused"] = ps.isPaused,
                ["isStopped"] = ps.isStopped,
                ["particleCount"] = ps.particleCount,
                ["main"] = new JObject
                {
                    ["duration"] = main.duration,
                    ["loop"] = main.loop,
                    ["startDelay"] = main.startDelay.constant,
                    ["startLifetime"] = main.startLifetime.constant,
                    ["startSpeed"] = main.startSpeed.constant,
                    ["startSize"] = main.startSize.constant,
                    ["startColor"] = new JObject
                    {
                        ["r"] = main.startColor.color.r,
                        ["g"] = main.startColor.color.g,
                        ["b"] = main.startColor.color.b,
                        ["a"] = main.startColor.color.a
                    },
                    ["maxParticles"] = main.maxParticles,
                    ["simulationSpace"] = main.simulationSpace.ToString(),
                    ["gravityModifier"] = main.gravityModifier.constant,
                    ["playOnAwake"] = main.playOnAwake
                },
                ["emission"] = new JObject
                {
                    ["enabled"] = emission.enabled,
                    ["rateOverTime"] = emission.rateOverTime.constant,
                    ["rateOverDistance"] = emission.rateOverDistance.constant
                },
                ["shape"] = new JObject
                {
                    ["enabled"] = shape.enabled,
                    ["shapeType"] = shape.shapeType.ToString(),
                    ["radius"] = shape.radius,
                    ["angle"] = shape.angle
                }
            };

            return result;
        }
    }
}
