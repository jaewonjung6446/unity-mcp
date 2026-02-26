using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetAudioSourcesHandler : IToolHandler
    {
        public string Name => "get_audio_sources";

        public JObject Execute(JObject parameters)
        {
#if UNITY_2022_1_OR_NEWER
            var sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var sources = Resources.FindObjectsOfTypeAll<AudioSource>();
#endif
            var results = new JArray();
            foreach (var source in sources)
            {
                if (source.gameObject.hideFlags != HideFlags.None) continue;

                var info = new JObject
                {
                    ["gameObject"] = source.gameObject.name,
                    ["instanceId"] = source.gameObject.GetInstanceID(),
                    ["path"] = GetGameObjectHandler.GetPath(source.gameObject),
                    ["enabled"] = source.enabled,
                    ["isPlaying"] = source.isPlaying,
                    ["clip"] = source.clip != null ? source.clip.name : null,
                    ["clipPath"] = source.clip != null ? AssetDatabase.GetAssetPath(source.clip) : null,
                    ["volume"] = source.volume,
                    ["pitch"] = source.pitch,
                    ["loop"] = source.loop,
                    ["playOnAwake"] = source.playOnAwake,
                    ["spatialBlend"] = source.spatialBlend,
                    ["mute"] = source.mute,
                    ["minDistance"] = source.minDistance,
                    ["maxDistance"] = source.maxDistance
                };

                if (source.isPlaying)
                {
                    info["time"] = source.time;
                    info["timeSamples"] = source.timeSamples;
                }

                results.Add(info);
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} AudioSources",
                ["audioSources"] = results
            };
        }
    }
}
