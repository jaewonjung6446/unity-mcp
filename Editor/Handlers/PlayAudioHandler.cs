using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class PlayAudioHandler : IToolHandler
    {
        public string Name => "play_audio";

        public JObject Execute(JObject parameters)
        {
            var action = parameters["action"]?.ToString()?.ToLower() ?? "play";

            // If an AudioSource is specified, control it directly
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var _);
            if (go != null)
            {
                var source = go.GetComponent<AudioSource>();
                if (source == null)
                    return McpServer.CreateError($"No AudioSource on '{go.name}'", "not_found_error");

                switch (action)
                {
                    case "play":
                        source.Play();
                        return new JObject
                        {
                            ["success"] = true,
                            ["type"] = "text",
                            ["message"] = $"Playing audio on '{go.name}' (clip: {source.clip?.name})"
                        };
                    case "stop":
                        source.Stop();
                        return new JObject
                        {
                            ["success"] = true,
                            ["type"] = "text",
                            ["message"] = $"Stopped audio on '{go.name}'"
                        };
                    case "pause":
                        source.Pause();
                        return new JObject
                        {
                            ["success"] = true,
                            ["type"] = "text",
                            ["message"] = $"Paused audio on '{go.name}'"
                        };
                    case "unpause":
                        source.UnPause();
                        return new JObject
                        {
                            ["success"] = true,
                            ["type"] = "text",
                            ["message"] = $"Unpaused audio on '{go.name}'"
                        };
                }
            }

            // Set clip by path
            var clipPath = parameters["clipPath"]?.ToString();
            if (!string.IsNullOrEmpty(clipPath) && go != null)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null)
                    return McpServer.CreateError($"AudioClip not found at '{clipPath}'", "not_found_error");

                var source = go.GetComponent<AudioSource>();
                if (source == null)
                    source = go.AddComponent<AudioSource>();

                source.clip = clip;
                if (action == "play") source.Play();

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Set clip '{clip.name}' on '{go.name}' and {action}"
                };
            }

            return McpServer.CreateError("Provide gameObjectPath/instanceId of AudioSource, or clipPath with target", "validation_error");
        }
    }
}
