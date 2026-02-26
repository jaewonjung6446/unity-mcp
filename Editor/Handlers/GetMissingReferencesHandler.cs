using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class GetMissingReferencesHandler : IToolHandler
    {
        public string Name => "get_missing_references";

        public JObject Execute(JObject parameters)
        {
            var scope = parameters["scope"]?.ToString()?.ToLower() ?? "scene";
            var results = new JArray();

            if (scope == "scene" || scope == "all")
            {
                // Check all GameObjects in the scene
#if UNITY_2022_1_OR_NEWER
                var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
#endif
                foreach (var go in allObjects)
                {
                    if (go.hideFlags != HideFlags.None) continue;

                    // Check for missing scripts
                    var components = go.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            results.Add(new JObject
                            {
                                ["type"] = "missing_script",
                                ["gameObject"] = go.name,
                                ["path"] = GetGameObjectHandler.GetPath(go),
                                ["instanceId"] = go.GetInstanceID(),
                                ["componentIndex"] = i
                            });
                        }
                    }

                    // Check for missing references in components
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        var so = new SerializedObject(comp);
                        var prop = so.GetIterator();
                        while (prop.NextVisible(true))
                        {
                            if (prop.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                                {
                                    results.Add(new JObject
                                    {
                                        ["type"] = "missing_reference",
                                        ["gameObject"] = go.name,
                                        ["path"] = GetGameObjectHandler.GetPath(go),
                                        ["instanceId"] = go.GetInstanceID(),
                                        ["component"] = comp.GetType().Name,
                                        ["property"] = prop.propertyPath
                                    });
                                }
                            }
                        }
                        so.Dispose();
                    }

                    if (results.Count >= 500) break;
                }
            }

            if (scope == "assets" || scope == "all")
            {
                // Check prefabs and scriptable objects
                var guids = AssetDatabase.FindAssets("t:Prefab t:ScriptableObject");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in assets)
                    {
                        if (asset == null)
                        {
                            results.Add(new JObject
                            {
                                ["type"] = "missing_subasset",
                                ["assetPath"] = path
                            });
                            continue;
                        }

                        var so = new SerializedObject(asset);
                        var prop = so.GetIterator();
                        while (prop.NextVisible(true))
                        {
                            if (prop.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                                {
                                    results.Add(new JObject
                                    {
                                        ["type"] = "missing_asset_reference",
                                        ["assetPath"] = path,
                                        ["objectName"] = asset.name,
                                        ["objectType"] = asset.GetType().Name,
                                        ["property"] = prop.propertyPath
                                    });
                                }
                            }
                        }
                        so.Dispose();
                    }

                    if (results.Count >= 500) break;
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {results.Count} missing references (scope: {scope})",
                ["missingReferences"] = results,
                ["capped"] = results.Count >= 500
            };
        }
    }
}
