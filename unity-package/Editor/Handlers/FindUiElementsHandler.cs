using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class FindUiElementsHandler : IToolHandler
    {
        public string Name => "find_ui_elements";

        public JObject Execute(JObject parameters)
        {
            var filter = parameters["filter"]?.ToString();
            var canvases = Object.FindObjectsOfType<Canvas>(true);
            var elements = new JArray();

            foreach (var canvas in canvases)
            {
                var rects = canvas.GetComponentsInChildren<RectTransform>(true);
                foreach (var rt in rects)
                {
                    if (rt.gameObject == canvas.gameObject) continue;

                    var go = rt.gameObject;
                    var componentTypes = new JArray();
                    bool interactable = false;
                    string textContent = null;

                    foreach (var c in go.GetComponents<Component>())
                    {
                        if (c == null) continue;
                        var typeName = c.GetType().Name;
                        componentTypes.Add(typeName);

                        if (c is Selectable s)
                            interactable = s.interactable && s.IsInteractable();
                        if (c is Text txt)
                            textContent = txt.text;
                        else if (IsTmpText(c))
                            textContent = c.GetType().GetProperty("text")?.GetValue(c) as string;
                    }

                    if (!string.IsNullOrEmpty(filter))
                    {
                        bool match = false;
                        foreach (var ct in componentTypes)
                        {
                            if (ct.ToString().Contains(filter)) { match = true; break; }
                        }
                        if (!match) continue;
                    }

                    var screenRect = GetScreenRect(rt, canvas);
                    var path = GetGameObjectHandler.GetPath(go);

                    var elem = new JObject
                    {
                        ["gameObjectPath"] = path,
                        ["instanceId"] = go.GetInstanceID(),
                        ["rect"] = new JObject
                        {
                            ["x"] = screenRect.x,
                            ["y"] = screenRect.y,
                            ["width"] = screenRect.width,
                            ["height"] = screenRect.height
                        },
                        ["componentTypes"] = componentTypes,
                        ["interactable"] = interactable,
                        ["depth"] = GetDepth(rt)
                    };
                    if (textContent != null)
                        elem["textContent"] = textContent;

                    elements.Add(elem);
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Found {elements.Count} UI elements",
                ["elements"] = elements
            };
        }

        public static Rect GetScreenRect(RectTransform rt, Canvas canvas)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Camera cam = null;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera ?? Camera.main;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint;
                if (cam != null)
                    screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                else
                    screenPoint = new Vector2(corners[i].x, corners[i].y);

                if (screenPoint.x < minX) minX = screenPoint.x;
                if (screenPoint.y < minY) minY = screenPoint.y;
                if (screenPoint.x > maxX) maxX = screenPoint.x;
                if (screenPoint.y > maxY) maxY = screenPoint.y;
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static int GetDepth(RectTransform rt)
        {
            int depth = 0;
            var t = rt.parent;
            while (t != null) { depth++; t = t.parent; }
            return depth;
        }

        /// <summary>
        /// Check if a Component is a TMP_Text subclass without a hard reference to TMPro.
        /// </summary>
        public static bool IsTmpText(Component c)
        {
            var t = c.GetType();
            while (t != null)
            {
                if (t.FullName == "TMPro.TMP_Text") return true;
                t = t.BaseType;
            }
            return false;
        }
    }
}
