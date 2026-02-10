using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class ScrollUiHandler : IToolHandler
    {
        public string Name => "scroll_ui";

        public JObject Execute(JObject parameters)
        {
            if (!Application.isPlaying)
                return McpServer.CreateError("scroll_ui requires Play Mode", "invalid_state");

            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var path = GetGameObjectHandler.GetPath(go);

            // Parse scroll delta
            float deltaX = 0f, deltaY = 0f;
            var delta = parameters["delta"];
            if (delta != null)
            {
                deltaX = delta["x"]?.ToObject<float>() ?? 0f;
                deltaY = delta["y"]?.ToObject<float>() ?? 0f;
            }

            var scrollDelta = parameters["scrollDelta"];
            if (scrollDelta != null)
            {
                deltaY = scrollDelta.ToObject<float>();
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return McpServer.CreateError("No EventSystem found in scene", "invalid_state");

            var pointerData = new PointerEventData(eventSystem)
            {
                scrollDelta = new Vector2(deltaX, deltaY)
            };

            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.scrollHandler);

            // Get normalized position if ScrollRect exists
            var scrollRect = go.GetComponent<ScrollRect>();
            if (scrollRect == null)
                scrollRect = go.GetComponentInParent<ScrollRect>();

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Scrolled '{path}' by ({deltaX}, {deltaY})"
            };

            if (scrollRect != null)
            {
                result["normalizedPosition"] = new JObject
                {
                    ["x"] = scrollRect.horizontalNormalizedPosition,
                    ["y"] = scrollRect.verticalNormalizedPosition
                };
            }

            return result;
        }
    }
}
