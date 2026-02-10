using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetAnimatorParameterHandler : IToolHandler
    {
        public string Name => "set_animator_parameter";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var path = GetGameObjectHandler.GetPath(go);
            var animator = go.GetComponent<Animator>();
            if (animator == null)
                return McpServer.CreateError($"No Animator component found on '{path}'", "not_found_error");

            var parameterName = parameters["parameterName"]?.ToString();
            if (string.IsNullOrEmpty(parameterName))
                return McpServer.CreateError("Missing required parameter: parameterName", "validation_error");

            // Find parameter
            AnimatorControllerParameter targetParam = null;
            foreach (var p in animator.parameters)
            {
                if (p.name == parameterName)
                {
                    targetParam = p;
                    break;
                }
            }

            if (targetParam == null)
                return McpServer.CreateError(
                    $"Parameter '{parameterName}' not found on Animator of '{path}'",
                    "not_found_error");

            // Determine type (user override or auto-detect)
            var typeStr = parameters["type"]?.ToString()?.ToLower();
            var paramType = targetParam.type;
            if (!string.IsNullOrEmpty(typeStr))
            {
                switch (typeStr)
                {
                    case "float": paramType = AnimatorControllerParameterType.Float; break;
                    case "int": paramType = AnimatorControllerParameterType.Int; break;
                    case "bool": paramType = AnimatorControllerParameterType.Bool; break;
                    case "trigger": paramType = AnimatorControllerParameterType.Trigger; break;
                    default:
                        return McpServer.CreateError(
                            $"Unknown type: {typeStr}. Use float, int, bool, or trigger",
                            "validation_error");
                }
            }

            var value = parameters["value"];
            string resultMsg;

            switch (paramType)
            {
                case AnimatorControllerParameterType.Float:
                    float fv = value?.ToObject<float>() ?? 0f;
                    animator.SetFloat(parameterName, fv);
                    resultMsg = $"Set Animator parameter '{parameterName}' (Float) = {fv}";
                    break;
                case AnimatorControllerParameterType.Int:
                    int iv = value?.ToObject<int>() ?? 0;
                    animator.SetInteger(parameterName, iv);
                    resultMsg = $"Set Animator parameter '{parameterName}' (Int) = {iv}";
                    break;
                case AnimatorControllerParameterType.Bool:
                    bool bv = value?.ToObject<bool>() ?? false;
                    animator.SetBool(parameterName, bv);
                    resultMsg = $"Set Animator parameter '{parameterName}' (Bool) = {bv}";
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(parameterName);
                    resultMsg = $"Set Animator trigger '{parameterName}'";
                    break;
                default:
                    return McpServer.CreateError(
                        $"Unsupported parameter type: {paramType}",
                        "validation_error");
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = resultMsg
            };
        }
    }
}
