using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetAnimatorStateHandler : IToolHandler
    {
        public string Name => "get_animator_state";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var path = GetGameObjectHandler.GetPath(go);
            var animator = go.GetComponent<Animator>();
            if (animator == null)
                return McpServer.CreateError($"No Animator component found on '{path}'", "not_found_error");

            var layerIndex = parameters["layerIndex"]?.ToObject<int>() ?? 0;
            if (layerIndex < 0 || layerIndex >= animator.layerCount)
                return McpServer.CreateError(
                    $"Layer index {layerIndex} out of range (0-{animator.layerCount - 1})",
                    "validation_error");

            // Current state info
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            var currentState = new JObject
            {
                ["nameHash"] = stateInfo.shortNameHash,
                ["fullPathHash"] = stateInfo.fullPathHash,
                ["normalizedTime"] = stateInfo.normalizedTime,
                ["length"] = stateInfo.length,
                ["loop"] = stateInfo.loop,
                ["speed"] = stateInfo.speed,
                ["speedMultiplier"] = stateInfo.speedMultiplier
            };

            // Transition info
            var isInTransition = animator.IsInTransition(layerIndex);
            JObject transitionInfo = null;
            if (isInTransition)
            {
                var transInfo = animator.GetAnimatorTransitionInfo(layerIndex);
                var nextState = animator.GetNextAnimatorStateInfo(layerIndex);
                transitionInfo = new JObject
                {
                    ["normalizedTime"] = transInfo.normalizedTime,
                    ["duration"] = transInfo.duration,
                    ["nextStateHash"] = nextState.fullPathHash
                };
            }

            // Parameters
            var paramArray = new JArray();
            foreach (var param in animator.parameters)
            {
                var p = new JObject
                {
                    ["name"] = param.name,
                    ["type"] = param.type.ToString()
                };
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        p["value"] = animator.GetFloat(param.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        p["value"] = animator.GetInteger(param.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        p["value"] = animator.GetBool(param.name);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        p["value"] = animator.GetBool(param.name);
                        break;
                }
                paramArray.Add(p);
            }

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Retrieved Animator state for '{path}'",
                ["currentState"] = currentState,
                ["isInTransition"] = isInTransition,
                ["parameters"] = paramArray,
                ["layerCount"] = animator.layerCount,
                ["isActiveAndEnabled"] = animator.isActiveAndEnabled,
                ["speed"] = animator.speed,
                ["hasRootMotion"] = animator.hasRootMotion
            };

            if (transitionInfo != null)
                result["transitionInfo"] = transitionInfo;

            return result;
        }
    }
}
