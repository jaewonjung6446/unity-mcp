using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetPackagesHandler : IToolHandler
    {
        public string Name => "get_packages";

        public JObject Execute(JObject parameters)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

            if (!File.Exists(manifestPath))
                return McpServer.CreateError("manifest.json not found", "not_found_error");

            var manifest = JObject.Parse(File.ReadAllText(manifestPath));
            var deps = manifest["dependencies"] as JObject;

            var packages = new JArray();
            if (deps != null)
            {
                foreach (var prop in deps.Properties())
                {
                    packages.Add(new JObject
                    {
                        ["name"] = prop.Name,
                        ["version"] = prop.Value.ToString()
                    });
                }
            }

            // Also check packages-lock.json for resolved versions
            var lockPath = Path.Combine(projectRoot, "Packages", "packages-lock.json");
            JObject resolved = null;
            if (File.Exists(lockPath))
            {
                var lockJson = JObject.Parse(File.ReadAllText(lockPath));
                resolved = lockJson["dependencies"] as JObject;
            }

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {packages.Count} packages in manifest",
                ["packages"] = packages
            };

            if (resolved != null)
            {
                var resolvedPackages = new JArray();
                foreach (var prop in resolved.Properties())
                {
                    var pkg = prop.Value as JObject;
                    resolvedPackages.Add(new JObject
                    {
                        ["name"] = prop.Name,
                        ["version"] = pkg?["version"]?.ToString(),
                        ["source"] = pkg?["source"]?.ToString()
                    });
                }
                result["resolvedPackages"] = resolvedPackages;
            }

            return result;
        }
    }
}
