using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace McpUnity
{
    public static class McpSettings
    {
        private static string ConfigPath => Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "ProjectSettings", "McpUnitySettings.json");

        public static int Port
        {
            get
            {
                if (File.Exists(ConfigPath))
                {
                    try
                    {
                        var json = JObject.Parse(File.ReadAllText(ConfigPath));
                        return json["Port"]?.ToObject<int>() ?? 8090;
                    }
                    catch { }
                }
                return 8090;
            }
        }
    }
}
