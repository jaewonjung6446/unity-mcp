using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class InspectUiLayoutHandler : IToolHandler
    {
        public string Name => "inspect_ui_layout";

        public JObject Execute(JObject parameters)
        {
            float screenW = parameters["screenWidth"]?.ToObject<float>() ?? 1920f;
            float screenH = parameters["screenHeight"]?.ToObject<float>() ?? 1080f;
            var screenBounds = new Rect(0, 0, screenW, screenH);

            var canvases = Object.FindObjectsOfType<Canvas>(true);
            var issues = new JArray();

            // Collect all selectables with their rects
            var selectableInfos = new List<(Selectable sel, Rect rect, string path, Transform transform)>();

            foreach (var canvas in canvases)
            {
                var rects = canvas.GetComponentsInChildren<RectTransform>(true);
                foreach (var rt in rects)
                {
                    if (rt.gameObject == canvas.gameObject) continue;
                    if (!rt.gameObject.activeInHierarchy) continue;

                    var go = rt.gameObject;
                    var rect = FindUiElementsHandler.GetScreenRect(rt, canvas);
                    var path = GetGameObjectHandler.GetPath(go);

                    // Off-screen check
                    if (!screenBounds.Overlaps(rect))
                    {
                        issues.Add(new JObject
                        {
                            ["type"] = "off_screen",
                            ["gameObjectPath"] = path,
                            ["instanceId"] = go.GetInstanceID(),
                            ["rect"] = RectToJson(rect),
                            ["message"] = $"Element is outside screen bounds ({screenW}x{screenH})"
                        });
                    }

                    // Touch target too small (< 88px, i.e. 44dp * 2x)
                    var selectable = go.GetComponent<Selectable>();
                    if (selectable != null && selectable.interactable)
                    {
                        selectableInfos.Add((selectable, rect, path, rt));
                        if (rect.width < 88f || rect.height < 88f)
                        {
                            issues.Add(new JObject
                            {
                                ["type"] = "touch_target_too_small",
                                ["gameObjectPath"] = path,
                                ["instanceId"] = go.GetInstanceID(),
                                ["rect"] = RectToJson(rect),
                                ["message"] = $"Touch target too small ({rect.width:F0}x{rect.height:F0}px, minimum 88x88)"
                            });
                        }
                    }

                    // Text overflow check
                    var text = go.GetComponent<Text>();
                    if (text != null)
                    {
                        var gen = text.cachedTextGenerator;
                        if (gen != null)
                        {
                            float prefW = gen.GetPreferredWidth(text.text, text.GetGenerationSettings(rt.rect.size));
                            float prefH = gen.GetPreferredHeight(text.text, text.GetGenerationSettings(rt.rect.size));
                            if (prefW > rt.rect.width + 1f || prefH > rt.rect.height + 1f)
                            {
                                issues.Add(new JObject
                                {
                                    ["type"] = "text_overflow",
                                    ["gameObjectPath"] = path,
                                    ["instanceId"] = go.GetInstanceID(),
                                    ["preferred"] = new JObject { ["width"] = prefW, ["height"] = prefH },
                                    ["actual"] = new JObject { ["width"] = rt.rect.width, ["height"] = rt.rect.height },
                                    ["message"] = $"Text overflows container ({prefW:F0}x{prefH:F0} > {rt.rect.width:F0}x{rt.rect.height:F0})"
                                });
                            }
                        }
                    }

                    // TMP text overflow (reflection-based to avoid hard TMPro dependency)
                    foreach (var c in go.GetComponents<Component>())
                    {
                        if (c != null && FindUiElementsHandler.IsTmpText(c))
                        {
                            var ct = c.GetType();
                            var pwProp = ct.GetProperty("preferredWidth");
                            var phProp = ct.GetProperty("preferredHeight");
                            if (pwProp != null && phProp != null)
                            {
                                float prefW = (float)pwProp.GetValue(c);
                                float prefH = (float)phProp.GetValue(c);
                                if (prefW > rt.rect.width + 1f || prefH > rt.rect.height + 1f)
                                {
                                    issues.Add(new JObject
                                    {
                                        ["type"] = "text_overflow",
                                        ["gameObjectPath"] = path,
                                        ["instanceId"] = go.GetInstanceID(),
                                        ["preferred"] = new JObject { ["width"] = prefW, ["height"] = prefH },
                                        ["actual"] = new JObject { ["width"] = rt.rect.width, ["height"] = rt.rect.height },
                                        ["message"] = $"Text overflows container ({prefW:F0}x{prefH:F0} > {rt.rect.width:F0}x{rt.rect.height:F0})"
                                    });
                                }
                            }
                            break;
                        }
                    }
                }
            }

            // Overlap detection between selectables (exclude parent-child)
            for (int i = 0; i < selectableInfos.Count; i++)
            {
                for (int j = i + 1; j < selectableInfos.Count; j++)
                {
                    var a = selectableInfos[i];
                    var b = selectableInfos[j];
                    if (!a.rect.Overlaps(b.rect)) continue;
                    if (IsParentChild(a.transform, b.transform)) continue;

                    issues.Add(new JObject
                    {
                        ["type"] = "overlap",
                        ["elementA"] = new JObject { ["gameObjectPath"] = a.path, ["rect"] = RectToJson(a.rect) },
                        ["elementB"] = new JObject { ["gameObjectPath"] = b.path, ["rect"] = RectToJson(b.rect) },
                        ["message"] = $"Selectable elements overlap: '{a.path}' and '{b.path}'"
                    });
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {issues.Count} layout issues",
                ["issues"] = issues
            };
        }

        private static bool IsParentChild(Transform a, Transform b)
        {
            return a.IsChildOf(b) || b.IsChildOf(a);
        }

        private static JObject RectToJson(Rect r)
        {
            return new JObject
            {
                ["x"] = r.x, ["y"] = r.y,
                ["width"] = r.width, ["height"] = r.height
            };
        }
    }
}
