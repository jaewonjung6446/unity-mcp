using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace McpUnity.Handlers
{
    public class GetPerformanceStatsHandler : IToolHandler
    {
        public string Name => "get_performance_stats";

        public JObject Execute(JObject parameters)
        {
            var stats = new JObject
            {
                ["fps"] = EditorApplication.isPlaying ? (1f / Time.unscaledDeltaTime) : 0f,
                ["deltaTime"] = Time.deltaTime,
                ["unscaledDeltaTime"] = Time.unscaledDeltaTime,
                ["timeScale"] = Time.timeScale,
                ["frameCount"] = Time.frameCount,
                ["realtimeSinceStartup"] = Time.realtimeSinceStartup
            };

            // Memory stats
            var memory = new JObject
            {
                ["totalAllocatedMemoryMB"] = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                ["totalReservedMemoryMB"] = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f),
                ["totalUnusedReservedMemoryMB"] = Profiler.GetTotalUnusedReservedMemoryLong() / (1024f * 1024f),
                ["monoHeapSizeMB"] = Profiler.GetMonoHeapSizeLong() / (1024f * 1024f),
                ["monoUsedSizeMB"] = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f),
                ["gcCollectionCount"] = System.GC.CollectionCount(0)
            };
            stats["memory"] = memory;

            // Object counts
            var objectCounts = new JObject
            {
#if UNITY_2022_1_OR_NEWER
                ["gameObjects"] = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length,
                ["transforms"] = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,
                ["renderers"] = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,
                ["rigidbodies"] = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length,
#else
                ["gameObjects"] = Object.FindObjectsOfType<GameObject>().Length,
                ["transforms"] = Object.FindObjectsOfType<Transform>().Length,
                ["renderers"] = Object.FindObjectsOfType<Renderer>().Length,
                ["rigidbodies"] = Object.FindObjectsOfType<Rigidbody>().Length,
#endif
                ["totalUnityObjects"] = Resources.FindObjectsOfTypeAll<Object>().Length
            };
            stats["objectCounts"] = objectCounts;

            // Rendering info
            if (EditorApplication.isPlaying)
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    stats["rendering"] = new JObject
                    {
                        ["screenWidth"] = Screen.width,
                        ["screenHeight"] = Screen.height,
                        ["currentResolution"] = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}@{Screen.currentResolution.refreshRateRatio}"
                    };
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = "Performance stats retrieved",
                ["stats"] = stats
            };
        }
    }
}
